using Buff163_Check_Price.Models;
using Buff163_Check_Price.ViewModels.Base;
using Buff163_Check_Price.Views;
using HtmlAgilityPack;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Buff163_Check_Price.ViewModels
{
    public class ItemViewModel : BaseViewModel
    {
        public Item _SelectedItem;
        public Item SelectedItem {
            get { return _SelectedItem; }
            set
            {
                _SelectedItem = value;
                GetPriceAsync(value.Id);
                SetComboBox(value.Exterior);
            }
        }
        public string? NameSearch { get; set; }
        public static CookieContainer cookieContainer;
        public Uri baseAddress => new Uri("https://buff.163.com");
        public static HttpClientHandler handler => new HttpClientHandler() { CookieContainer = cookieContainer };
        public static HttpClient client => new HttpClient(handler);
        public float _Rate { get; set; }
        public float Rate 
        {
            get { return _Rate; }
            set
            {
                if (Rate != value)
                {
                    Total = value * _Price;
                    Profit = (Coin * RateEmpire) - Total;
                }
                _Rate = value;
            }
        }
        public float Total { get; set; }

        public float _Coin { get; set; }
        public float Coin
        {
            get { return _Coin; }
            set
            {
                if(Coin != value)
                {
                    Profit = (value * Rate) - Total;
                }
                _Coin = value;
            }
        }
        public float _RateEmpire { get; set; }
        public float RateEmpire
        {
            get { return _RateEmpire; }
            set
            {
                if(RateEmpire != value)
                {
                    Profit = (value * Coin) - Total;
                }
                _RateEmpire = value; 
            }
        }
        public float Profit { get; set; }
        public string? Name { get; set; }
        public float? Wear { get; set; }
        public string? _WearRangeSelected { get; set; }
        public string? WearRangeSelected 
        { get { return _WearRangeSelected; } 
            set 
            {
                if (WearRangeSelected != value)
                {
                    GetPriceFloatRangeAsync(SelectedItem.Id, value);
                }
                _WearRangeSelected = value;
                
            } 
        }
        public float _Price { get; set; }
        public float Price 
        {
            get { return _Price; }
            set
            {
                if (Price != value)
                {
                    Total = _Rate * value;
                    Profit = (Coin * RateEmpire) - Total;
                }
                _Price = value;
            }
        }
        public WebView2 webView { get; set; }
        public string? ImagePath { get; set; }
        public ICommand SearchName => new RelayCommand<object>(p => { return true; }, p => { SearchAsync(NameSearch); });
        public ICommand OpenWebview => new RelayCommand<object>(p => { return true; }, p => { OpenWindow("webview"); });
        public ICommand GetCookies => new RelayCommand<object>(p => { return true; }, async p =>
        {
            var cookiesList = await webView.CoreWebView2.CookieManager.GetCookiesAsync("https://buff.163.com");
            TransferCookies(cookiesList);
            CloseWindow(p, "login");

        });
        public ObservableCollection<Item> SearchList { get; set; }
        public ObservableCollection<string> WearRange { get; set; }

        public ItemViewModel()
        {
            SearchList = new ObservableCollection<Item>();
            WearRange = new ObservableCollection<string>();
            cookieContainer = new CookieContainer();
        }

        private async Task SearchAsync(string nameSearch)
        {
            //https://buff.163.com/api/market/goods?game=csgo&page_num=1&search=flip knife&use_suggestion=0&trigger=search_btn&_=1646142762661;
            //https://buff.163.com/api/market/goods?game=csgo&page_num=1&search=flip%20knife
            SearchList.Clear();
            var page_num = 1;
            string url = $"https://buff.163.com/api/market/goods?game=csgo&page_num={page_num}&search={nameSearch}";

            //var result = await client.GetFromJsonAsync<object>(url);
            var response = await client.GetAsync(url);
            var result = response.Content.ReadAsStringAsync().Result;

            JObject data = JObject.Parse(result);
            page_num = (int)data["data"]["page_num"];
            var total_page = (int)data["data"]["total_page"];
            JArray items = (JArray)data["data"]["items"];
            while (page_num <= total_page)
            {
                
                if (page_num > 1)
                {
                    url = $"https://buff.163.com/api/market/goods?game=csgo&page_num={page_num}&search={nameSearch}";
                    response = await client.GetAsync(url);
                    result = response.Content.ReadAsStringAsync().Result;

                    data = JObject.Parse(result);
                    page_num = (int)data["data"]["page_num"];
                    items = (JArray)data["data"]["items"];
                }
                foreach (var item in items)
                {
                    Item item1 = new Item();
                    //var original_icon_url = (string)item["goods_info"]["original_icon_url"];
                    item1.Id = (string)item["id"];
                    item1.Name =  (string)item["name"];
                    item1.SteamURL = (string)item["steam_market_url"];
                    item1.Exterior = item.SelectToken("goods_info.info.tags.exterior") != null ? (string)item["goods_info"]["info"]["tags"]["exterior"]["localized_name"] : null ;
                    SearchList.Add(item1);
                    //response = await client.GetAsync(original_icon_url);
                    //var stream = await response.Content.ReadAsStreamAsync();
                }
                page_num++;
            }
        }

        private async Task GetPriceAsync(string id)
        {
        //https://buff.163.com/api/market/goods/sell_order?game=csgo&goods_id=42748&page_num=1&sort_by=default&mode=&allow_tradable_cooldown=1
            var page_num = 1;
            string url = $"https://buff.163.com/api/market/goods/sell_order?game=csgo&goods_id={id}&page_num={page_num}&sort_by=default&mode=&allow_tradable_cooldown=1";

            var response = await client.GetAsync(url);
            var result = response.Content.ReadAsStringAsync().Result;

            JObject data = JObject.Parse(result);
            page_num = (int)data["data"]["page_num"];
            var total_page = (int)data["data"]["total_page"];
            JArray items = (JArray)data["data"]["items"];
            JToken item = items[0];

            Price = (float) item["price"];
            Wear = (string) item["asset_info"]["paintwear"] != "" ? (float) item["asset_info"]["paintwear"] : null;
            Total = _Rate * _Price;
        }

        private async Task GetPriceFloatRangeAsync(string id,string floatRange)
        {
            //https://buff.163.com/api/market/goods/sell_order?game=csgo&goods_id=42748&page_num=1&sort_by=default&mode=&allow_tradable_cooldown=1&min_paintwear=0.00&max_paintwear=0.01
            if(floatRange != null)
            {
                var range = floatRange.Split('-');
                var page_num = 1;
                string url = $"https://buff.163.com/api/market/goods/sell_order?game=csgo&goods_id={id}&page_num={page_num}&sort_by=default&mode=&allow_tradable_cooldown=1&min_paintwear={range[0]}&max_paintwear={range[1]}";

                var response = await client.GetAsync(url);
                var result = response.Content.ReadAsStringAsync().Result;

                JObject data = JObject.Parse(result);
                page_num = (int)data["data"]["page_num"];
                var total_page = (int)data["data"]["total_page"];
                JArray items = (JArray)data["data"]["items"];
                JToken item = items[0];

                Price = (float)item["price"];
                Wear = (float)item["asset_info"]["paintwear"];
                Total = _Rate * _Price;
            } else
            {
                GetPriceAsync(id);
            }
            
        }

        private void OpenWindow(string window)
        {
            switch(window)
            {
                case "webview":
                    Login x = new Login();
                    this.webView = x.webView;
                    x.DataContext = this;
                    x.ShowDialog();
                    break;
                default:
                    break;
            }
        }

        private void CloseWindow(object p, string type)
        {
            switch (type)
            {
                case "login":
                    Login x = p as Login;
                    x.Close();
                    MessageBox.Show("Login Succesfully!");
                    break;
                default:
                    break;
            }
        }

        private void SetComboBox(string wear)
        {
            WearRange.Clear();
            if(wear != null)
            switch(wear) {
                case "Factory New":
                    WearRange.Add("0.00-0.01");
                    WearRange.Add("0.01-0.02");
                    WearRange.Add("0.02-0.03");
                    WearRange.Add("0.03-0.04");
                    WearRange.Add("0.04-0.07");
                    break;
                case "Minimal Wear":
                    WearRange.Add("0.07-0.08");
                    WearRange.Add("0.08-0.09");
                    WearRange.Add("0.09-0.10");
                    WearRange.Add("0.10-0.11");
                    WearRange.Add("0.11-0.15");
                    break;
                case "Field-Tested":
                    WearRange.Add("0.15-0.18");
                    WearRange.Add("0.18-0.21");
                    WearRange.Add("0.21-0.24");
                    WearRange.Add("0.24-0.27");
                    WearRange.Add("0.27-0.38");
                    break;
                case "Well-Worn":
                    WearRange.Add("0.38-0.39");
                    WearRange.Add("0.39-0.40");
                    WearRange.Add("0.40-0.41");
                    WearRange.Add("0.41-0.42");
                    WearRange.Add("0.42-0.45");
                    break;
                case "Battle-Scarred":
                    WearRange.Add("0.45-0.50");
                    WearRange.Add("0.50-0.63");
                    WearRange.Add("0.63-0.76");
                    WearRange.Add("0.76-0.85");
                    break;
                default:
                    break;
                }
        }

        private void TransferCookies(IEnumerable<CoreWebView2Cookie> coreWebView2Cookies)
        {
            foreach (var cookie in coreWebView2Cookies)
            {
                cookieContainer.Add(baseAddress,new Cookie(cookie.Name,cookie.Value));
            }
        }
    }
}
