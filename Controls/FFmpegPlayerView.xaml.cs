// Version: 0.1.0.20
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

namespace Thmd.Controls
{
    /// <summary>
    /// Logika interakcji dla klasy FFmpegPlayerView.xaml
    /// </summary>
    public partial class FFmpegPlayerView : UserControl
    {
        public string VideoPath { get; set; } 

        public FFmpegPlayerView()
        {
            InitializeComponent();
        }

        public void Play()
        {
            if (VideoPath != null)
            {
                VideoPath = VideoPath.Trim();
                _ffmpegPlayer.Open(new Uri(VideoPath));
                _ffmpegPlayer.Play();
            }
        }
    }
}
