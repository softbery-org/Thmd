// Version: 0.1.4.94
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Thmd.Controls;

namespace Thmd.Translator
{
    public interface ILanguage
    {
        string Name { get; }
        Control Control { get; }
        List<Button> Buttons { get; }
        List<Translation> Values { get; }  
    }

    public class Translation
    {
        public string Name {  get; set; }
        public List<Value> Values = new List<Value>();

        public class Value
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Content { get; set; }
        }
    }
}
