// Version: 0.0.0.3
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Thmd.Views
{
    /// <summary>
    /// Logika interakcji dla klasy KeyboardShortcutView.xaml
    /// </summary>
    public partial class KeyboardShortcutsView : UserControl
    {
        public static readonly DependencyProperty CloseCommandProperty =
            DependencyProperty.Register("CloseCommand", typeof(ICommand), typeof(KeyboardShortcutsView));

        public ICommand CloseCommand
        {
            get => (ICommand)GetValue(CloseCommandProperty);
            set => SetValue(CloseCommandProperty, value);
        }

        public KeyboardShortcutsView()
        {
            InitializeComponent();

            InitializeCommands();
            InitializeEvents();
        }

        private void InitializeEvents()
        {
            this.Loaded += (s, e) =>
            {
                Keyboard.Focus((IInputElement)this.Parent);
            };

            this.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    Close(null);
                }
            };
        }

        private void InitializeCommands()
        {
            CloseCommand = new RelayCommand(_ =>Close(null), _ =>true);
        }

        private void Close(object parameter)
        {
            this.Visibility = Visibility.Collapsed;
        }
    }
}
