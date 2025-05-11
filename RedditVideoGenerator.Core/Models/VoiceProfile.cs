// RedditVideoGenerator.Core/Models/VoiceProfile.cs
namespace RedditVideoGenerator.Core.Models
{
    public enum TextToSpeechProvider
    {
        MicrosoftTTS, // For the System.Speech.Synthesis
        ElevenLabs    // For ElevenLabs API
        // Add other providers here in the future if needed
    }

    public class VoiceProfile
    {
        public TextToSpeechProvider Provider { get; set; } = TextToSpeechProvider.MicrosoftTTS;
        public string? VoiceId { get; set; } // Specific voice ID (e.g., for ElevenLabs)
        // We can add other properties later, like speed, pitch, language, etc.
        // string LanguageCode { get; set; } = "en-US";
        // double Rate { get; set; } = 1.0; // Normal speed

        // Parameterless constructor for easy instantiation with defaults
        public VoiceProfile() { }

        public VoiceProfile(TextToSpeechProvider provider, string? voiceId = null)
        {
            Provider = provider;
            VoiceId = voiceId;
        }
    }
}