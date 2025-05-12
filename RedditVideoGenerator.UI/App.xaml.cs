// RedditVideoGenerator.UI/App.xaml.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration; // For IConfiguration
using RedditVideoGenerator.Core.Models;   // For ApplicationSettings
using RedditVideoGenerator.Core.Services; // For our service interfaces and implementations
using RedditVideoGenerator.UI.ViewModels; // <<<< ADDED THIS USING DIRECTIVE
using System;
// using System.IO; // ImplicitUsings might cover this, or add if needed
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

        public App()
        {
            // Constructor
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            var host = Host;
            await host.StartAsync();

            var configService = GetService<IConfigurationService>();
            var logger = GetService<ILoggerService>();

            try
            {
                await configService.LoadSettingsAsync();
                logger.LogInformation("Application settings loaded successfully via IConfigurationService.");
                logger.LogInformation($"Default Subreddit from settings: {configService.Settings.DefaultSubreddit}");
            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to load application settings during startup.", ex);
                MessageBox.Show($"Critical Error: Failed to load application settings: {ex.Message}\nApplication will exit.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            var redditService = GetService<IRedditService>();
            try
            {
                await redditService.InitializeAsync();
                logger.LogInformation("RedditService initialized successfully.");
            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to initialize RedditService during startup.", ex);
                MessageBox.Show($"Critical Error: Failed to initialize RedditService: {ex.Message}\nCheck Reddit App ID and User Agent in appsettings.json.\nApplication will exit.", "Reddit Service Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            var mainWindow = GetService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            var configService = GetService<IConfigurationService>();
            if (configService != null)
            {
                await configService.SaveSettingsAsync();
                GetService<ILoggerService>()?.LogInformation("Application settings saved on exit.");
            }
            else
            {
                try { GetService<ILoggerService>()?.LogWarning("ConfigurationService was null during OnExit, settings not saved.", null); } catch { }
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

                    // Register MainViewModel
                    services.AddTransient<MainViewModel>(); // <<<< ENSURE THIS LINE IS PRESENT AND CORRECT

                    services.AddSingleton<MainWindow>();
                });
    }
}
