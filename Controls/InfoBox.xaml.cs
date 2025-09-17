// Version: 0.1.5.13
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Thmd.Media;

namespace Thmd.Controls
{
    /// <summary>
    /// Logika interakcji dla klasy InfoBox.xaml
    /// </summary>
    public partial class InfoBox : UserControl
    {
        private FrameworkElement _parent;

        public Action<string> DrawInfoText;

        public InfoBox()
        {
            InitializeComponent();
            DrawInfoText += InfoBox_DrawText;
        }

        public InfoBox(FrameworkElement parent) : this()
        {
            _parent = parent;
            _parent.SizeChanged += Parent_SizeChanged;
        }

        private async void InfoBox_DrawText(string obj)
        {
                var storyboard = this.FindResource("fadeInBox") as Storyboard; //.ShowByStoryboard(this, this.FindResource("fadeInBox") as Storyboard);

                await Helpers.StoryboardHelper.RunStoryboad(this, storyboard);

                await this.Dispatcher.InvokeAsync(() =>
                {
                    _infoTextBlock.Text = obj;
                });

                await Task.Delay(7000);

                storyboard = this.FindResource("fadeOutBox") as Storyboard; //.ShowByStoryboard(this, this.FindResource("fadeInBox") as Storyboard);
                await Helpers.StoryboardHelper.RunStoryboad(this, storyboard);
        }

        private void Parent_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_parent != null)
            {
                double newFontSize = e.NewSize.Height / 25.0;
                _infoTextBlock.FontSize = ((newFontSize > 10.0) ? newFontSize : 10.0);
            }
        }
    }
}
