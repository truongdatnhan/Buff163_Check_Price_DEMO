using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Buff163_Check_Price.Models
{
    public class Item
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public float Price { get; set; }
        public float Wear { get; set; }
        public string? Exterior { get; set; }
        public string? ImagePath { get; set; }
        public string? SteamURL { get; set; }
    }
}
