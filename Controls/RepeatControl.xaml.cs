// Version: 0.1.0.17
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Thmd.Controls;

public partial class RepeatControl : UserControl
{
    /// <summary>
    /// Occurs when a property value changes, used for data binding.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="RepeatControl"/> class.
    /// </summary>
    public RepeatControl()
	{
		InitializeComponent();
        
	}
}
