using PropertyChanged;
using System.ComponentModel;

namespace Buff163_Check_Price.ViewModels.Base
{
    //[ImplementPropertyChanged]
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = (sender,e) => { };
    }
}