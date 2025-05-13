// RedditVideoGenerator.Core/Models/ApplicationSettings.cs
namespace RedditVideoGenerator.Core.Models
{
    public class ApplicationSettings
    {
        // For Reddit API (if we choose to use app-only OAuth or a script-type app)
        // For user-based OAuth, tokens will be stored differently.
        public string? RedditAppId { get; set; }
        public string? RedditAppSecret { get; set; } // Be cautious with storing secrets
        public string? RedditUserAgent { get; set; }
        public string? RedditUsername { get; set; }
        public string? RedditPassword { get; set; } // Be cautious with storing secrets

        // For ElevenLabs API
        public string? ElevenLabsApiKey { get; set; }
        public string? DefaultElevenLabsVoiceId { get; set; }

        // Paths
        public string? FfmpegPath { get; set; }
        public string? OutputVideoFolderPath { get; set; }
        public string? TemporaryFilesFolderPath { get; set; }
        public string? GameplayVideosFolderPath { get; set; }
        public string? BackgroundMusicFolderPath { get; set; }

        // Default Video Settings
        public string DefaultSubreddit { get; set; } = "AskReddit";
        public int VideoTargetDurationMinutes { get; set; } = 10; // Target duration

        public ApplicationSettings()
        {
            // Initialize with some sensible defaults or ensure they are loaded
            TemporaryFilesFolderPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "RedditVideoGeneratorV2");
        }
    }
}