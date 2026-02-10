using System;
using System.ComponentModel;
using System.Windows.Input;

namespace ChatClientCommon.UI
{
    public class RelayCommand : BindingItem, ICommand, INotifyPropertyChanged
    {
        #region Fields

        readonly Action<object> action;
        readonly Predicate<object> canExecute;

        #endregion // Fields

        #region properties

        private bool visible;
        private object tag;
        private string image;
        private string details;
        private bool enabled = true;

        public bool Visible
        {
            get { return visible; }
            set { SetField(ref visible, value, "Visible"); }
        }

        public object Tag
        {
            get { return tag; }
            set { SetField(ref tag, value, "Tag"); }
        }

        public string Image
        {
            get { return image; }
            set { SetField(ref image, value, "Image"); }
        }

        public bool Enabled
        {
            get { return enabled; }
            set { SetField(ref enabled, value, "Enabled"); }
        }

        public string Details
        {
            get { return details; }
            set { SetField(ref details, value, nameof(Details)); }
        }

        #endregion

        #region Constructors

        public RelayCommand(Action<object> execute, string header, bool visible)
            : this(execute, null, header, visible)
        {
        }

        public RelayCommand(Action<object> execute, string header, bool visible, string image)
            : this(execute, null, header, visible)
        {
            this.image = image;
        }

        public RelayCommand(Action<object> execute, Predicate<object> canExecute, string header, bool visible)
        {
            this.Header = header;
            this.Visible = visible;
            this.action = execute;
            this.canExecute = canExecute;
        }

        #endregion // Constructors

        #region ICommand Members

        public bool CanExecute(object parameter)
        {
            return canExecute == null ? true : canExecute(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            if (enabled)
            {
                action(parameter);
            }
        }

        #endregion // ICommand Members


        internal static void CallCanExectueChanged()
        {
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }
    }
}
