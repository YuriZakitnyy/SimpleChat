using System.Collections.Generic;
using System.ComponentModel;

namespace ChatClientCommon.UI
{
    public class BindingItem : INotifyPropertyChanged
    {
        private string header;
        public string Header
        {
            get { return header; }
            set { SetField(ref header, value, "Header"); }
        }

        #region INotifyPropertyChanged Members
        protected bool SetField<T>(ref T field, T value, params string[] propertyNames)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            CallPropertyChanged(propertyNames);
            return true;
        }

        internal void CallPropertyChanged(params string[] propertyNames)
        {
            if (PropertyChanged != null)
                foreach (var item in propertyNames)
                    PropertyChanged(this, new PropertyChangedEventArgs(item));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
