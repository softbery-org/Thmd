using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Thmd.Translator
{
    public static class LanguageChanger
    {
        public static ILanguage Translation { get; private set; }
        public static IList<ILanguage> Languages { get; private set; } = new List<ILanguage>();

        /// <summary>
        /// Change or refresh language
        /// </summary>
        public static void ChangeLanguage()
        {
            Translation = Translation.UseLanguage() as ILanguage;
        }

        public static void ChangeLanguage(string language)
        {
            if (language == String.Empty)
            {
                Console.WriteLine("Don't know language: 'empty string'");
                return;
            }
            foreach (var item in Languages)
            {
                if (item.Name == language)
                    Translation = item.UseLanguage() as ILanguage;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="language"></param>
        public static void ChangeLanguage(this object obj, string language = null)
        {
            var control = obj as Control;

            if (language == null || language == String.Empty)
            {
                Console.WriteLine($"Can't change language. I don't know: '{language}' language.");
                Console.WriteLine($"Trying refresh control: {obj.GetType().Name}");
            }
            else
                ChangeLanguage(language);

            try
            {
                control.InvalidateVisual();
                Console.WriteLine($"Control refreshed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with refreshing control: {ex.Message}");
            }
        }
    }
}
// Version: 0.1.0.28
