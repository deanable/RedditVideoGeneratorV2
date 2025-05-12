// RedditVideoGenerator.UI/App.xaml.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration; // For IConfiguration
using RedditVideoGenerator.Core.Models;   // For ApplicationSettings
using RedditVideoGenerator.Core.Services; // For our service interfaces and implementations
using RedditVideoGenerator.UI.ViewModels;
using System;
using System.Windows;

namespace RedditVideoGenerator.UI
{
    public partial class App : Application
    {
        private static IHost? _host;

        public static IHost Host => _host ??= CreateHostBuilder(null).Build();
        public static IServiceProvider Services => Host.Services;

        public static T GetService<T>() where T : class
        {
            return Services.GetRequiredService<T>();
        }

        public App() { }

        protected override async void OnStartup(StartupEventArgs e)
        {
            var host = Host;
            await host.StartAsync();

            ILoggerService? logger = null; // Initialize logger to null
            IConfigurationService? configService = null;

            try
            {
                // Attempt to get logger first
                logger = GetService<ILoggerService>();
                configService = GetService<IConfigurationService>();

                await configService.LoadSettingsAsync();
                logger.LogInformation("Application settings loaded successfully via IConfigurationService.");
                logger.LogInformation($"Default Subreddit from settings: {configService.Settings.DefaultSubreddit}");
            }
            catch (Exception ex)
            {
                // Log if possible, then show MessageBox
                logger?.LogCritical("Failed to load application settings during startup.", ex);
                MessageBox.Show($"Critical Error: Failed to load application settings: {ex.Message}\nApplication will exit.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            // Initialize RedditService after settings are loaded
            var redditService = GetService<IRedditService>();
            try
            {
                await redditService.InitializeAsync(); // This might throw if Reddit auth fails
                logger.LogInformation("RedditService initialized successfully.");
            }
            catch (Exception ex) // Catch exceptions specifically from RedditService initialization
            {
                logger.LogCritical("Failed to initialize RedditService during startup.", ex);
                string detailedErrorMessage = $"Critical Error: Failed to initialize RedditService.\n\n";
                detailedErrorMessage += $"Message: {ex.Message}\n\n";
                if (ex.InnerException != null)
                {
                    detailedErrorMessage += $"Inner Exception: {ex.InnerException.Message}\n\n";
                }
                detailedErrorMessage += "This is often due to:\n";
                detailedErrorMessage += "1. Incorrect 'RedditAppId' in appsettings.json.\n";
                detailedErrorMessage += "2. Incorrect or missing 'RedditUserAgent' in appsettings.json.\n";
                detailedErrorMessage += "3. Network connectivity issues to Reddit.\n";
                detailedErrorMessage += "4. Reddit API temporary issues or rate limiting.\n\n";
                detailedErrorMessage += "Please check your appsettings.json and internet connection.\nApplication will exit.";

                MessageBox.Show(detailedErrorMessage, "Reddit Service Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            var mainWindow = GetService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            ILoggerService? logger = null;
            IConfigurationService? configService = null;
            try
            {
                // It's possible services can't be resolved if host is already shutting down
                logger = GetService<ILoggerService>();
                configService = GetService<IConfigurationService>();
            }
            catch { }


            if (configService != null)
            {
                await configService.SaveSettingsAsync();
                logger?.LogInformation("Application settings saved on exit.");
            }
            else
            {
                logger?.LogWarning("ConfigurationService was null during OnExit, settings not saved.", null);
            }

            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }
            _host = null;
            base.OnExit(e);
        }

        public static IHostBuilder CreateHostBuilder(string[]? args) =>
            Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, configBuilder) =>
                {
                    configBuilder.SetBasePath(AppContext.BaseDirectory)
                          .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    services.Configure<ApplicationSettings>(context.Configuration.GetSection(nameof(ApplicationSettings)));

                    services.AddSingleton<ILoggerService, ConsoleLoggerService>();
                    services.AddSingleton<IConfigurationService, JsonConfigurationService>();
                    services.AddSingleton<ITextToSpeechService, MicrosoftTextToSpeechService>();
                    services.AddSingleton<MicrosoftTextToSpeechService>();
                    services.AddSingleton<ElevenLabsTextToSpeechService>();
                    services.AddSingleton<IRedditService, RedditApiService>();
                    services.AddTransient<MainViewModel>();
                    services.AddSingleton<MainWindow>();
                });
    }
}
