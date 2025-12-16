// Version: 0.0.0.2
using System.Windows;
using System.Windows.Media;

namespace Thmd.Utilities
{
    public static class VisualTreeExtensions
    {
        public static bool IsAncestorOf(this DependencyObject parent, DependencyObject child)
        {
            if (parent == null || child == null)
                return false;

            while (child != null)
            {
                if (child == parent) return true;
                child = VisualTreeHelper.GetParent(child);
            }

            return false;
        }
    }
}
