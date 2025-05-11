// RedditVideoGenerator.Core/Services/ITextToSpeechService.cs
using RedditVideoGenerator.Core.Models; // Required for VoiceProfile and AudioDetails
using System.Threading.Tasks;

namespace RedditVideoGenerator.Core.Services
{
    public interface ITextToSpeechService
    {
        /// <summary>
        /// Generates audio from the given text using the specified voice profile.
        /// </summary>
        /// <param name="textToSpeak">The text to convert to speech.</param>
        /// <param name="voiceProfile">The voice profile specifying provider, voice ID, etc.</param>
        /// <param name="outputAudioFilePath">The full path where the generated audio file should be saved.</param>
        /// <returns>Details about the generated audio file (path and duration).</returns>
        Task<AudioDetails?> GenerateAudioAsync(string textToSpeak, VoiceProfile voiceProfile, string outputAudioFilePath);

        /// <summary>
        /// Gets a list of available voice profiles for a given provider.
        /// (This is optional and might be complex to implement universally,
        /// but good for future UI enhancements).
        /// </summary>
        /// <param name="provider">The TTS provider.</param>
        /// <returns>A list of available voice profiles.</returns>
        // Task<List<VoiceProfile>> GetAvailableVoicesAsync(TextToSpeechProvider provider);
    }
}