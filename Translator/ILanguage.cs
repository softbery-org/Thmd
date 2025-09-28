using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thmd.Translator
{
    public interface ILanguage
    {
        string Name { get; }
        string formSettings_btnSave { get; }
        string formSettings_btnCancel { get; }
        string formSettings_Text { get; }
        string formSettings_labelOptions { get; }

        #region VideoControlBar
        string videoControlBar_labelSubtilesOnOffText { get; }
        string videoControlBar_labelOn { get; }
        string videoControlBar_labelOff { get; }
        string videoControlBar_fullscreenOff { get; }
        string videoControlBar_fullscreenOn { get; }
        string videoControlBar_startTxt { get; }
        string videoControlBar_subtilesOn { get; }
        string videoControlBar_subtilesOff { get; }
        #endregion
    }
}
// Version: 0.1.3.34
