// Version: 0.0.0.4
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thmd.Utilities
{
    /// <summary>
    /// Metody rozszerzające dla obiektów
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Wykonuje akcję na obiekcie i zwraca ten obiekt
        /// </summary>
        /// <typeparam name="T">Typ T</typeparam>
        /// <param name="obj">Obiekt typu T</param>
        /// <param name="action">Akcja jaka zostanie wykonana na obiekcie</param>
        /// <returns>Obiekt po uruchomieniu akcji</returns>
        public static T Also<T>(this T obj, Action<T> action)
        {
            action(obj);
            return obj;
        }
    }
}
