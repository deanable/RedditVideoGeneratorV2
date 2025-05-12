// RedditVideoGenerator.Core/Services/JsonConfigurationService.cs
using Microsoft.Extensions.Options; // Required for IOptions
using RedditVideoGenerator.Core.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace RedditVideoGenerator.Core.Services
{
    public class JsonConfigurationService : IConfigurationService
    {
        private readonly ILoggerService _logger;
        private readonly string _settingsFilePath;
        private readonly IOptionsMonitor<ApplicationSettings> _settingsMonitor; // To get updated settings if appsettings.json changes

        // Settings property will now primarily reflect what was loaded by the host.
        // We might still want to update it if specific programmatic changes are made that need saving.
        public ApplicationSettings Settings { get; private set; }

        public JsonConfigurationService(IOptionsMonitor<ApplicationSettings> settingsMonitor, ILoggerService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settingsMonitor = settingsMonitor ?? throw new ArgumentNullException(nameof(settingsMonitor));

            // Get the initial settings loaded by the host
            Settings = _settingsMonitor.CurrentValue;

            // Define where the appsettings.json is located for saving.
            // AppContext.BaseDirectory points to the execution directory (e.g., bin/Debug/net8.0-windows)
            _settingsFilePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

            // Subscribe to changes if you want live updates (optional for this app's current scope)
            // _settingsMonitor.OnChange(updatedSettings =>
            // {
            //     _logger.LogInformation("Application settings reloaded due to file change.");
            //     Settings = updatedSettings;
            // });
        }

        public Task LoadSettingsAsync()
        {
            // The host's configuration system (via services.Configure in App.xaml.cs)
            // has already loaded appsettings.json into IOptions<ApplicationSettings>.
            // We retrieve the current value from IOptionsMonitor in the constructor.
            // This method can now be simpler, perhaps just ensuring Settings is not null
            // or logging the loaded settings.

            if (Settings == null)
            {
                _logger.LogWarning("ApplicationSettings were null after initial load from IOptionsMonitor. This is unexpected.", null);
                // Attempt to re-fetch or initialize defaults if absolutely necessary,
                // but IOptions should provide an instance.
                Settings = _settingsMonitor.CurrentValue ?? new ApplicationSettings();
            }

            // Ensure critical paths are initialized if they were somehow not set by the JSON binding
            if (string.IsNullOrWhiteSpace(Settings.TemporaryFilesFolderPath))
            {
                Settings.TemporaryFilesFolderPath = Path.Combine(Path.GetTempPath(), "RedditVideoGeneratorV2_Temp");
                _logger.LogInformation($"TemporaryFilesFolderPath was not set, defaulting to: {Settings.TemporaryFilesFolderPath}");
            }
            if (!Directory.Exists(Settings.TemporaryFilesFolderPath))
            {
                try
                {
                    Directory.CreateDirectory(Settings.TemporaryFilesFolderPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Could not create temporary files directory: {Settings.TemporaryFilesFolderPath}", ex);
                }
            }


            _logger.LogInformation("JsonConfigurationService: Settings initialized/verified.");
            return Task.CompletedTask;
        }

        public async Task SaveSettingsAsync()
        {
            try
            {
                // We save the current state of the Settings object.
                // This is useful if the application modifies settings programmatically at runtime
                // and wants to persist them.
                var options = new JsonSerializerOptions { WriteIndented = true };
                // We need to wrap it in the "ApplicationSettings" key to match the JSON structure
                var settingsWrapper = new { ApplicationSettings = Settings };
                string jsonString = JsonSerializer.Serialize(settingsWrapper, options);

                await File.WriteAllTextAsync(_settingsFilePath, jsonString);
                _logger.LogInformation($"Settings saved to '{_settingsFilePath}'.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving settings to '{_settingsFilePath}': {ex.Message}", ex);
            }
        }
    }
}
