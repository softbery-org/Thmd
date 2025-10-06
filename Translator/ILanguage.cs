// Version: 0.1.5.9
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Thmd.Consolas;
using Thmd.Controls;

namespace Thmd.Translator
{
    public interface ILanguage
    {
        string Name { get; set; }
        string Code { get; set; }
        List<ITranslation> Translations { get; set; }
    }

    public interface ITranslation
    {
        object Control { get; }
        List<Translate> Translates { get; }
    }

    public class Translate
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class Translation : ITranslation
    {
        private string _key;
        private string _value;
        private object _control;
        private List<Translate> _translations = new List<Translate>();

        /// <summary>
        /// Control witch can we change
        /// </summary>
        public object Control => _control;
        /// <summary>
        /// Translates list for Control
        /// </summary>
        public List<Translate> Translates => _translations;

        /// <summary>
        /// Class creator
        /// </summary>
        public Translation(object control)
        {
            _control = control;
            _translations = new List<Translate>();
        }
        /// <summary>
        /// Class creator with starting keys and values
        /// </summary>
        /// <param name="translation_key">string</param>
        /// <param name="translation_value">string</param>
        public Translation(object control, string translation_key, string translation_value)
        {
            _key = translation_key;
            _value = translation_value;
            _control = control;
            _translations.Add(new Translate { Key = _key, Value = _value });
        }
        /// <summary>
        /// Add translate
        /// </summary>
        /// <param name="translate">table of translate</param>
        public void AddTranslate(Translate[] translate)
        {
            try
            {
                _translations.AddRange(translate);
            }
            catch (Exception ex)
            {
                this.WriteLine(ex.ToString());
            }
        }
    }
}
