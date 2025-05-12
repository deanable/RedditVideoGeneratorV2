// RedditVideoGenerator.UI/ViewModels/MainViewModel.cs
using RedditVideoGenerator.Core.Models;
using RedditVideoGenerator.Core.Services;
using System.Collections.ObjectModel; // For ObservableCollection
using System.Threading.Tasks;
using System.Windows.Input; // For ICommand

namespace RedditVideoGenerator.UI.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly ILoggerService _logger;
        private readonly IConfigurationService _configurationService;
        private readonly IRedditService _redditService;

        private string _statusMessage = "Ready.";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private string _fetchedPostTitle = "No post fetched yet.";
        public string FetchedPostTitle
        {
            get => _fetchedPostTitle;
            set => SetProperty(ref _fetchedPostTitle, value);
        }

        public ICommand FetchTopPostCommand { get; }

        public MainViewModel(
            ILoggerService logger,
            IConfigurationService configurationService,
            IRedditService redditService)
        {
            _logger = logger;
            _configurationService = configurationService;
            _redditService = redditService;

            FetchTopPostCommand = new RelayCommand(async param => await FetchTopPostAsync(), param => true);

            _logger.LogInformation("MainViewModel initialized.");
            StatusMessage = $"Settings loaded. Default subreddit: {_configurationService.Settings.DefaultSubreddit}";
        }

        private async Task FetchTopPostAsync()
        {
            StatusMessage = "Fetching top post...";
            _logger.LogInformation("Attempting to fetch top post...");
            FetchedPostTitle = "Fetching...";

            try
            {
                // Ensure RedditService is initialized (App.xaml.cs should have done this)
                // If not, InitializeAsync might need to be callable from here or ensured earlier.
                // For now, assume App.xaml.cs handles initialization.

                var posts = await _redditService.GetTopPostsAsync(_configurationService.Settings.DefaultSubreddit, "day", 1);
                if (posts != null && posts.Count > 0)
                {
                    FetchedPostTitle = posts[0].Title;
                    StatusMessage = "Top post fetched successfully!";
                    _logger.LogInformation($"Fetched post: {posts[0].Title}");
                }
                else
                {
                    FetchedPostTitle = "Failed to fetch post or no posts found.";
                    StatusMessage = "Failed to fetch post.";
                    _logger.LogWarning("No posts returned or failed to fetch.");
                }
            }
            catch (System.Exception ex)
            {
                FetchedPostTitle = "Error fetching post.";
                StatusMessage = "Error fetching post.";
                _logger.LogError("Exception while fetching top post.", ex);
            }
        }
    }
}
