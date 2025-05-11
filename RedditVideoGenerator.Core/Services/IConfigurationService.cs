// RedditVideoGenerator.Core/Services/IConfigurationService.cs
using RedditVideoGenerator.Core.Models; // Required for ApplicationSettings

namespace RedditVideoGenerator.Core.Services
{
    public interface IConfigurationService
    {
        ApplicationSettings Settings { get; }
        Task LoadSettingsAsync();
        Task SaveSettingsAsync();
    }
}