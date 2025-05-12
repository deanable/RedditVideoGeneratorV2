// RedditVideoGenerator.Core/Services/ElevenLabsTextToSpeechService.cs
using RedditVideoGenerator.Core.Models;
using RedditVideoGenerator.Core.Services; // For ILoggerService and IConfigurationService
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json; // For JsonConvert
using NAudio.Wave; // We'll use NAudio here for duration too

namespace RedditVideoGenerator.Core.Services
{
    public class ElevenLabsTextToSpeechService : ITextToSpeechService
    {
        private readonly ILoggerService _logger;
        private readonly IConfigurationService _configurationService;
        private readonly HttpClient _httpClient;
        private const string ElevenLabsApiBaseUrl = "https://api.elevenlabs.io/v1/";

        public ElevenLabsTextToSpeechService(ILoggerService logger, IConfigurationService configurationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _httpClient = new HttpClient();
            // The API key will be set per request from configuration
        }

        public async Task<AudioDetails?> GenerateAudioAsync(string textToSpeak, VoiceProfile voiceProfile, string outputAudioFilePath)
        {
            if (voiceProfile.Provider != TextToSpeechProvider.ElevenLabs)
            {
                _logger.LogWarning($"ElevenLabsTextToSpeechService was called with an unsupported provider: {voiceProfile.Provider}.", null);
                return null;
            }

            if (string.IsNullOrWhiteSpace(textToSpeak))
            {
                _logger.LogWarning("ElevenLabs: Text to speak was empty or null. ElevenLabs does not support generating empty audio directly, returning null.", null);
                // Unlike MicrosoftTTS, ElevenLabs might not generate a file for empty text.
                // We could create a silent MP3 here if needed, but for now, returning null is simpler.
                // Or, we could call a helper to create a silent MP3 of a certain duration.
                return null;
            }

            if (string.IsNullOrWhiteSpace(voiceProfile.VoiceId))
            {
                _logger.LogError("ElevenLabs: VoiceId was not provided in VoiceProfile.", null);
                return null;
            }

            if (string.IsNullOrWhiteSpace(outputAudioFilePath))
            {
                _logger.LogError("ElevenLabs: Output audio file path was not provided.", null);
                return null;
            }

            string? apiKey = _configurationService.Settings.ElevenLabsApiKey;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogError("ElevenLabs API Key is not configured in settings.", null);
                return null;
            }

            try
            {
                string url = $"{ElevenLabsApiBaseUrl}text-to-speech/{voiceProfile.VoiceId}";

                var requestPayload = new
                {
                    text = textToSpeak,
                    model_id = "eleven_multilingual_v2", // Or make this configurable via VoiceProfile or ApplicationSettings
                    voice_settings = new
                    {
                        stability = 0.70,       // Example values, make these configurable
                        similarity_boost = 0.70,
                        style = 0.45,           // If using a model that supports style (e.g., v2 expressive)
                        use_speaker_boost = true
                    }
                };

                string jsonPayload = JsonConvert.SerializeObject(requestPayload);
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                };
                request.Headers.Add("xi-api-key", apiKey);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/mpeg")); // Request MP3 audio

                _logger.LogDebug($"Sending request to ElevenLabs API: {url}");
                HttpResponseMessage response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    // Ensure the output directory exists
                    string? directory = Path.GetDirectoryName(outputAudioFilePath);
                    if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    using (var fs = new FileStream(outputAudioFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(fs);
                    }

                    TimeSpan duration = GetMp3FileDuration(outputAudioFilePath); // We'll need this helper
                    _logger.LogInformation($"ElevenLabs: Successfully generated audio at {outputAudioFilePath} with duration {duration}.");
                    return new AudioDetails(outputAudioFilePath, duration);
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"ElevenLabs API Error: {response.StatusCode} - {errorContent}", null);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception during ElevenLabs API call: {ex.Message}", ex);
                return null;
            }
        }

        private TimeSpan GetMp3FileDuration(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                _logger.LogWarning($"GetMp3FileDuration: File not found or path empty for '{filePath}'.", null);
                return TimeSpan.Zero;
            }

            try
            {
                // NAudio can read MP3 durations
                using (var reader = new Mp3FileReader(filePath)) // Mp3FileReader is in NAudio.Wave
                {
                    return reader.TotalTime;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error reading MP3 duration for '{filePath}': {ex.Message}", ex);
                return TimeSpan.Zero;
            }
        }

        // public async Task<List<VoiceProfile>> GetAvailableVoicesAsync(TextToSpeechProvider provider)
        // {
        //     // Implementation for ElevenLabs: Call the /v1/voices endpoint
        //     // Parse the response and map to VoiceProfile objects.
        //     // Requires API key.
        //     throw new NotImplementedException();
        // }
    }
}