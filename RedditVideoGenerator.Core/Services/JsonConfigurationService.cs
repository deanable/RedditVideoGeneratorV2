// RedditVideoGenerator.Core/Services/JsonConfigurationService.cs
using Microsoft.Extensions.Configuration; // From NuGet package
using RedditVideoGenerator.Core.Models;
using System;
using System.IO;
using System.Text.Json; // For JsonSerializer
using System.Threading.Tasks;

namespace RedditVideoGenerator.Core.Services
{
    public class JsonConfigurationService : IConfigurationService
    {
        private const string SettingsFileName = "appsettings.json";
        private readonly string _settingsFilePath;

        public ApplicationSettings Settings { get; private set; }

        public JsonConfigurationService()
        {
            // Determine path for appsettings.json (usually alongside the executable or in AppData)
            // For a library, it's often simpler to expect it in the application's base directory.
            // For user-specific settings, a path in Environment.SpecialFolder.ApplicationData is better.
            // We'll start with a simple approach: alongside the executable.
            _settingsFilePath = Path.Combine(AppContext.BaseDirectory, SettingsFileName);
            Settings = new ApplicationSettings(); // Initialize with defaults
        }

        public async Task LoadSettingsAsync()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    // Using Microsoft.Extensions.Configuration for robust loading
                    var configurationBuilder = new ConfigurationBuilder()
                        .SetBasePath(AppContext.BaseDirectory) // Base path for the JSON file
                        .AddJsonFile(SettingsFileName, optional: true, reloadOnChange: true);

                    IConfigurationRoot configurationRoot = configurationBuilder.Build();

                    // Bind the configuration to our ApplicationSettings object
                    var loadedSettings = new ApplicationSettings();
                    configurationRoot.Bind(loadedSettings); // This uses Microsoft.Extensions.Configuration.Binder
                    Settings = loadedSettings;

                    // Ensure critical paths are initialized if not in JSON
                    if (string.IsNullOrWhiteSpace(Settings.TemporaryFilesFolderPath))
                    {
                        Settings.TemporaryFilesFolderPath = Path.Combine(Path.GetTempPath(), "RedditVideoGeneratorV2");
                    }
                }
                else
                {
                    // Settings file doesn't exist, use defaults and perhaps save it for the first time.
                    Console.WriteLine($"Warning: Configuration file '{_settingsFilePath}' not found. Using default settings. Will create on save.");
                    // Ensure the default constructor of ApplicationSettings sets sensible values.
                    await SaveSettingsAsync(); // Optionally save a default file
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error loading settings from '{_settingsFilePath}': {ex.Message}");
                // Fallback to default settings in case of error
                Settings = new ApplicationSettings();
            }
        }

        public async Task SaveSettingsAsync()
        {
            try
            {
                // Ensure the directory exists
                string? directory = Path.GetDirectoryName(_settingsFilePath);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(Settings, options);
                await File.WriteAllTextAsync(_settingsFilePath, jsonString);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error saving settings to '{_settingsFilePath}': {ex.Message}");
            }
        }
    }
}