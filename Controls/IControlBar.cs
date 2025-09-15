// Version: 0.1.2.35
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Thmd.Controls
{
    public interface IControlBar
    {

        Button BtnPlay { get; }
        Button BtnStop { get; }
        Button BtnNext { get; }
        Button BtnPrevious { get; }
        Button BtnVolumeUp { get; }
        Button BtnVolumeDown { get; }
        Button BtnMute { get; }
        Button BtnSettingsWindow { get; }
        Button BtnSubtitle { get; }
        Button BtnUpdate { get; }
        Button BtnOpen { get; }
        Button BtnPlaylist { get; }
        Button BtnFullscreen { get; }
        Button BtnClose { get; }
    }
}
