// Version: 0.1.0.35
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Markup;
using Thmd.Repeats;

namespace Thmd.Controls;

public partial class RepeatControl : UserControl
{
    /// <summary>
    /// Occurs when a property value changes, used for data binding.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private List<RepeatType> _repeatModeList = new List<RepeatType>();
    private int _index = 0;

    public string RepeatMode{ get; set; } = RepeatType.None.ToString();

    /// <summary>
    /// Initializes a new instance of the <see cref="RepeatControl"/> class.
    /// </summary>
    public RepeatControl()
	{
		InitializeComponent();
        _repeatModeList.Add(RepeatType.None);
        _repeatModeList.Add(RepeatType.Current);
        _repeatModeList.Add(RepeatType.All);
        _repeatModeList.Add(RepeatType.Random);
    }

    private void RepeatTextBlock_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_repeatModeList.Count == 0)
        {
            return;
        }
        if(e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
        {
            _index++;
            if (_index >= _repeatModeList.Count)
            {
                _index = 0;
            }
            RepeatMode = _repeatModeList[_index].ToString();
            OnPropertyChanged(nameof(RepeatMode));
        }

    }
}
