using System.Collections.ObjectModel;

namespace ChatClientCommon.UI
{
    public class ListItem : BindingItem
    {
        private string details;
        public string Details
        {
            get { return details; }
            set { SetField(ref details, value, "Details"); }
        }

        private string image;
        public string Image
        {
            get { return image; }
            set { SetField(ref image, value, "Image"); }
        }

        public object Data { get; set; }

        public RelayCommand ConnectCommand { get; set; }

    }

    public class ListItems : BindingItem
    {
        public ObservableCollection<object> Items { get; private set; }
        public ListItems()
        {
            Items = new ObservableCollection<object>();
        }
    }
}
