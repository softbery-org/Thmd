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

using Microsoft.Web.WebView2.Wpf;

namespace Thmd.Controls
{
    /// <summary>
    /// Logika interakcji dla klasy YoutubeControl.xaml
    /// </summary>
    public partial class YoutubeControl : UserControl
    {
        public YoutubeControl()
        {
            InitializeComponent();
        }

            public YoutubeControl(WebView2 webView)
            {
                _webView = webView;
            }

            public async Task InitializeAsync()
            {
                await _webView.EnsureCoreWebView2Async();
            }

            public void LoadVideo(string videoId)
            {
                // YouTube embed URL – bez reklam
                string url = $"https://www.youtube.com/embed/{videoId}?autoplay=1&modestbranding=1&rel=0";
                _webView.Source = new Uri(url);
            }
    }
}
// Version: 0.1.1.15
