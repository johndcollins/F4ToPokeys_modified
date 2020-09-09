using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Speech.Synthesis;

namespace F4ToPokeys
{
    public static class DebugUtils
    {
        [Conditional("DEBUG")]
        public static void Speak(string textToSpeak)
        {
            SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer();
            speechSynthesizer.Speak(textToSpeak);
        }
    }
}
