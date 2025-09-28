// Version: 0.1.5.62
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Thmd.Utilities
{
    public static class DragDropHelper
    {
        public static readonly DependencyProperty IsDragOverProperty = DependencyProperty.RegisterAttached(
            "IsDragOver",
            typeof(bool),
            typeof(DragDropHelper),
            new PropertyMetadata(false));

        public static bool GetIsDragOver(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsDragOverProperty);
        }

        public static void SetIsDragOver(DependencyObject obj, bool value)
        {
            obj.SetValue(IsDragOverProperty, value);
        }
    }
}
