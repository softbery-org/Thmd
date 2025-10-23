// Version: 0.1.0.31
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
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Thmd.Translator;

namespace Thmd.Controls
{
    /// <summary>
    /// Logika interakcji dla klasy AddStreamView.xaml
    /// </summary>
    public partial class AddStreamView : UserControl
    {
        public string ReturnUrl;

        public AddStreamView()
        {
            InitializeComponent();
        }

        private void CancelButton(object sender, RoutedEventArgs e)
        {
            ReturnUrl = String.Empty;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            XmlLanguage language = XmlLanguage.GetLanguage("pl");
            var l = Translator.Language.LoadLanguage("langs");
            if (l != null)
            {
                foreach (var item in l)
                {
                    if (item.Code == "pl_pl")
                    {
                        foreach (var item_translation in item.Translations)
                        {
                            if (item_translation.Control.GetType() == typeof(ControlBar))
                            {
                                
                            }
                        }
                    }
                }
            }
            
            

            ReturnUrl = _textBox.Text;
            this.Visibility = Visibility.Hidden;
        }
    }
}
