using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Thmd.Translator;

namespace Thmd.Translator;
    public static class Language
    {
        private static IList<ILanguage> _langs = new List<ILanguage>();
        private static Dictionary<string, string> _translations = new Dictionary<string, string>();

        private static string _path = "Languages/";

        public static string LanguageDir { get => _path; }

        public static IList<ILanguage> LoadLanguage(string dir_path = null)
        {
            if (dir_path != null)
            {
                SetLanguagesDir(dir_path);
            }

            if (!Directory.Exists(_path))
            {
                Directory.CreateDirectory(_path);
            }

            var files = Directory.GetFiles(_path, "*.dll");

            if (files != null)
            {
                foreach (var file in files)
                {
                    var assembly = Assembly.LoadFrom(file);
                    var types = assembly.GetExportedTypes();

                    foreach (Type type in types)
                    {
                        if (type.GetInterfaces().Contains(typeof(ILanguage)))
                        {
                            var obj = Activator.CreateInstance(type);
                            _langs.Add(obj as ILanguage);
                        }
                    }
                }
            }
            return _langs;
        }

        private static void SetLanguagesDir(string dir)
        {
            var dir_path = new DirectoryInfo(dir);
            if (dir_path.Exists)
            {
                _path = dir_path.FullName;
            }
        }

        public static object UseLanguage<T>(this T obj)
        {
            if (_langs.Contains((ILanguage)obj))
                return (T)Activator.CreateInstance(obj.GetType());
            else
                return null;
        }

    /// <summary>
    /// Rekurencyjnie t�umaczy teksty w kontrolkach drzewa wizualnego.
    /// Obs�uguje TextBlock, Button (Translate), Label, MenuItem (Header) itp.
    /// </summary>
    /// <param name="obj">Obiekt DependencyObject do przetworzenia (kontrolka lub kontener).</param>
    public static void TranslateControls(DependencyObject obj)
    {
        if (obj == null) return;

        // Przetwarzaj bie��cy obiekt
        TranslateSingleControl(obj);

        // Przetwarzaj dzieci rekurencyjnie
        int childrenCount = VisualTreeHelper.GetChildrenCount(obj);
        for (int i = 0; i < childrenCount; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(obj, i);
            TranslateControls(child);
        }
    }

    /// <summary>
    /// T�umaczy tekst w pojedynczej kontrolce na podstawie jej typu.
    /// </summary>
    /// <param name="control">Kontrolka do przet�umaczenia.</param>
    public static void TranslateSingleControl(DependencyObject control)
    {
        switch (control)
        {
            case TextBlock textBlock:
                if (_translations.TryGetValue(textBlock.Text, out string translatedTextBlock))
                {
                    textBlock.Text = translatedTextBlock;
                }
                break;

            case Button button:
                if (button.Content is string buttonContent && _translations.TryGetValue(buttonContent, out string translatedButton))
                {
                    button.Content = translatedButton;
                }
                break;

            case Label label:
                if (label.Content is string labelContent && _translations.TryGetValue(labelContent, out string translatedLabel))
                {
                    label.Content = translatedLabel;
                }
                break;

            case MenuItem menuItem:
                if (menuItem.Header is string menuHeader && _translations.TryGetValue(menuHeader, out string translatedMenu))
                {
                    menuItem.Header = translatedMenu;
                }
                break;

            //Dodaj obs�ug� innych typ�w kontrolek, np. CheckBox, ToolTip itp., je�li potrzeba
            case CheckBox checkBox:
                 if (checkBox.Content is string cbContent && _translations.TryGetValue(cbContent, out string translatedCb))
                 {
                     checkBox.Content = translatedCb;
                 }
                 break;

            default:
                // Ignoruj inne typy
                break;
        }
    }
}
// Version: 0.1.5.37
