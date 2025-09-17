// Version: 0.1.6.15
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

using Thmd.Media;

namespace Thmd.Controls
{
    /// <summary>
    /// Logika interakcji dla klasy TimerBox.xaml
    /// </summary>
    public partial class TimerBox : UserControl
    {
        private IPlayer _player;
        private string _timer = "00:00:00";

        public event PropertyChangedEventHandler PropertyChanged;

        public string Timer
        {
            get => _timer;
            set
            {
                _timer = value;
                _timerTextBlock.Text = value;
                OnPropertyChanged(nameof(Timer));
            }
        }

        public TimerBox()
        {
            InitializeComponent();
        }

        public TimerBox(IPlayer player) : this()
        {
            _player = player;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
