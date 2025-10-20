// Version: 0.1.10.45
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
        public string BtnPlay { get; }       
        public string BtnStop { get; }      
        public string BtnNext { get; }    
        public string BtnPrevious { get; }        
        public string BtnMute { get; }        
        public string BtnSubtitle { get; }       
        public string BtnOpen { get; }        
        public string BtnPlaylist { get; }    
        public string SliderVolume { get; }        
        public string BtnStream { get; }   
    }
}
