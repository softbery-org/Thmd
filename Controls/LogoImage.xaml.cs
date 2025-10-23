// Version: 0.1.0.95
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

using Thmd.Controls.Effects;

namespace Thmd.Controls
{
    /// <summary>
    /// Logika interakcji dla klasy LogoImage.xaml
    /// </summary>
    public partial class LogoImage : UserControl, INotifyPropertyChanged
    {
        private ImageSource _imageSource;
        private IEffect _effect;

        public ImageSource ImageSource
        {
            get => _imageSource;
            set
            {
                _imageSource = value;
                OnPropertyChanged(nameof(ImageSource));
            }
        }

        public IEffect Effect
        {
            get=>_effect;
            set
            {
                _effect = value;
                OnPropertyChanged(nameof(Effect));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public LogoImage()
        {
            InitializeComponent();
        }
    }
}
