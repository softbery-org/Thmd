// Version: 0.0.0.8
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Synthesis;
using Thmd.Consolas;

namespace Thmd.Lectors
{
    public class VideoLectore
    {
        private SpeechSynthesizer _synthesizer = new SpeechSynthesizer();
        private PromptBuilder _promptBuilder = new PromptBuilder();

        public VideoLectore(string voice_name)
        {
            foreach (var item in _synthesizer.GetInstalledVoices())
            {
                if (voice_name == item.VoiceInfo.Name)
                {
                    try
                    {
                        _synthesizer.SelectVoice(voice_name);
                    }
                    catch (Exception ex)
                    {
                        this.WriteLine(ex.Message);
                    }
                }
                this.WriteLine(item.VoiceInfo.Name);
            }
        }

        /// <summary>
        /// Ustawianie lectora z System.Speech.Synthesis
        /// </summary>
        /// <param name="name"></param>
        public void LectoreSet(string name)
        {
            foreach (var voice in _synthesizer.GetInstalledVoices())
            {
                if (voice!=null && voice.VoiceInfo.Name == name)
                {
                    _synthesizer.SelectVoice(name);
                }
            }
        }

        /// <summary>
        /// Odczytywanie tekstu
        /// </summary>
        /// <param name="text_to_read">Tre�� kt�ra ma zosta� odczytana przez lektora</param>
        public void LectoreRead(string text_to_read)
        {
            try
            {
                _synthesizer.Speak(text_to_read);
            }
            catch (Exception ex)
            {
                this.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Odczytywanie tekstu
        /// </summary>
        /// <param name="text_to_read">Tre�� kt�ra ma zosta� odczytana przez lektora</param>
        public void LectoreReadAsync(string text_to_read)
        {
            try
            {
                _synthesizer.SpeakAsync(text_to_read);
            }
            catch (Exception ex)
            {
                this.WriteLine(ex.Message);
            }
        }
    }
}
