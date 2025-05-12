// RedditVideoGenerator.Core/Services/MicrosoftTextToSpeechService.cs
using NAudio.Wave; // For WaveFileReader
using RedditVideoGenerator.Core.Models;
using System;
using System.IO;
using System.Speech.Synthesis; // From System.Speech
using System.Threading.Tasks;

// Required for NAudio if you use it for duration, or another way to get WAV duration.
// If you haven't added NAudio yet, we can do that or use a simpler duration placeholder.
// For now, let's add a placeholder for NAudio for duration.
// We'll install NAudio properly in a later step when we need more audio manipulation.
// using NAudio.Wave;


namespace RedditVideoGenerator.Core.Services
{
    public class MicrosoftTextToSpeechService : ITextToSpeechService
    {
        private readonly ILoggerService _logger;

        // Constructor to inject the logger
        public MicrosoftTextToSpeechService(ILoggerService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AudioDetails?> GenerateAudioAsync(string textToSpeak, VoiceProfile voiceProfile, string outputAudioFilePath)
        {
            if (voiceProfile.Provider != TextToSpeechProvider.MicrosoftTTS)
            {
                _logger.LogWarning($"MicrosoftTextToSpeechService was called with an unsupported provider: {voiceProfile.Provider}. It only supports MicrosoftTTS.", null);
                return null;
            }

            if (string.IsNullOrWhiteSpace(textToSpeak))
            {
                _logger.LogWarning("Text to speak was empty or null.", null);
                // Create a short silent WAV file as per original logic
                return await CreateSilentWavAsync(outputAudioFilePath, TimeSpan.FromMilliseconds(200));
            }

            if (string.IsNullOrWhiteSpace(outputAudioFilePath))
            {
                _logger.LogError("Output audio file path was not provided.", null);
                return null;
            }

            try
            {
                // Ensure the output directory exists
                string? directory = Path.GetDirectoryName(outputAudioFilePath);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // SpeechSynthesizer needs to run on an STA thread if called from a non-STA thread.
                // For simplicity in a library, we'll run it synchronously within a Task.Run
                // to avoid blocking if this method itself is awaited from an STA thread.
                // A better approach for WPF would be to ensure this is called appropriately
                // or use a dedicated thread/task if long synthesis operations are expected.
                await Task.Run(() =>
                {
                    using (var synthesizer = new SpeechSynthesizer())
                    {
                        synthesizer.SetOutputToWaveFile(outputAudioFilePath);

                        // voiceProfile.VoiceId could be used here to select a specific installed voice
                        // if (voiceProfile.VoiceId != null)
                        // {
                        //     try { synthesizer.SelectVoice(voiceProfile.VoiceId); }
                        //     catch (Exception ex) { _logger.LogWarning($"Could not select voice '{voiceProfile.VoiceId}'. Using default.", ex); }
                        // }
                        // synthesizer.Rate = voiceProfile.Rate; // If we add Rate to VoiceProfile

                        synthesizer.Speak(textToSpeak);
                    }
                });

                // Get audio duration
                TimeSpan duration = GetWavFileDuration(outputAudioFilePath);

                if (duration == TimeSpan.Zero && !string.IsNullOrWhiteSpace(textToSpeak))
                {
                    _logger.LogWarning($"Generated audio for '{textToSpeak.Substring(0, Math.Min(textToSpeak.Length, 20))}(...)' has zero duration. Creating a silent fallback.", null);
                    return await CreateSilentWavAsync(outputAudioFilePath, TimeSpan.FromMilliseconds(200));
                }

                _logger.LogInformation($"Successfully generated audio at {outputAudioFilePath} with duration {duration}.");
                return new AudioDetails(outputAudioFilePath, duration);

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating speech with Microsoft TTS: {ex.Message}", ex);
                return null;
            }
        }

        private async Task<AudioDetails?> CreateSilentWavAsync(string outputAudioFilePath, TimeSpan duration)
        {
            try
            {
                await Task.Run(() =>
                {
                    using (var synthesizer = new SpeechSynthesizer())
                    {
                        synthesizer.SetOutputToWaveFile(outputAudioFilePath);
                        PromptBuilder emptyPrompt = new PromptBuilder();
                        // Append a tiny bit of silence. A single space or period might also work.
                        // Or a very short break.
                        emptyPrompt.AppendBreak(duration);
                        // emptyPrompt.AppendText("."); // Original approach had this
                        synthesizer.Speak(emptyPrompt);
                    }
                });
                _logger.LogInformation($"Generated silent WAV at {outputAudioFilePath} with duration {duration}.");
                return new AudioDetails(outputAudioFilePath, duration);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating silent WAV: {ex.Message}", ex);
                return null;
            }
        }


        // RedditVideoGenerator.Core/Services/MicrosoftTextToSpeechService.cs

        // ... (other using statements and class definition) ...

        private TimeSpan GetWavFileDuration(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _logger.LogWarning("GetWavFileDuration: File path is null or empty.", null);
                return TimeSpan.Zero;
            }

            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"GetWavFileDuration: File not found at '{filePath}'.", null);
                return TimeSpan.Zero;
            }

            try
            {
                using (var waveFileReader = new WaveFileReader(filePath))
                {
                    return waveFileReader.TotalTime;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error reading WAV file duration for '{filePath}': {ex.Message}", ex);
                // Return a small default or rethrow, depending on desired error handling.
                // For now, returning zero if it can't be read.
                return TimeSpan.Zero;
            }
        }

        // ... (rest of the class) ...
        // We will later implement GetAvailableVoicesAsync if needed.
        // public Task<List<VoiceProfile>> GetAvailableVoicesAsync(TextToSpeechProvider provider)
        // {
        //     // Implementation to list installed Microsoft TTS voices
        //     throw new NotImplementedException();
        // }
    }
}