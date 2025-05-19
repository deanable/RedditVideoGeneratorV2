using RedditVideoGenerator.Core.Models;
using RedditVideoGenerator.Core.Services;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RedditVideoGenerator.Core.Services
{
    public class RedditApiService : IRedditService
    {
        private readonly ILoggerService _logger;
        private readonly IConfigurationService _configurationService;
        private HttpClient _client = new();
        private static readonly List<string> _sessionFetchedPostIds = new();
        private string? _accessToken;

        public RedditApiService(ILoggerService logger, IConfigurationService configurationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        }

        public async Task InitializeAsync()
        {
            string? appId = _configurationService.Settings.RedditAppId;
            string? appSecret = _configurationService.Settings.RedditAppSecret;
            string? username = _configurationService.Settings.RedditUsername;
            string? password = _configurationService.Settings.RedditPassword;
            string? userAgent = _configurationService.Settings.RedditUserAgent;

            if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(appSecret) ||
                string.IsNullOrWhiteSpace(userAgent) || string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                _logger.LogError("Reddit credentials are not fully configured. Please check appsettings.json.");
                return;
            }

            _logger.LogInformation($"Authenticating Reddit client for user '{username}'...");

            try
            {
                var authClient = new HttpClient();
                authClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
                var byteArray = System.Text.Encoding.ASCII.GetBytes($"{appId}:{appSecret}");
                authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                var request = new HttpRequestMessage(HttpMethod.Post, "https://www.reddit.com/api/v1/access_token")
                {
                    Content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("grant_type", "password"),
                        new KeyValuePair<string, string>("username", username),
                        new KeyValuePair<string, string>("password", password)
                    })
                };

                var response = await authClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                response.EnsureSuccessStatusCode();

                var json = JsonDocument.Parse(body);
                _accessToken = json.RootElement.GetProperty("access_token").GetString();

                _client = new HttpClient();
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

                _logger.LogDebug($"Using bearer token: {_accessToken?.Substring(0, 20)}...");


                _client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);

                _logger.LogInformation("Reddit authentication successful.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to authenticate with Reddit.", ex);
            }
        }

        public async Task<List<RedditPostInfo>> GetTopPostsAsync(string subredditName, string timeFilter = "day", int limit = 10)
        {
            var posts = new List<RedditPostInfo>();

            if (string.IsNullOrWhiteSpace(_accessToken))
            {
                _logger.LogError("Reddit is not authenticated. Call InitializeAsync first.", null);
                return posts;
            }

            try
            {
                string url = $"https://oauth.reddit.com/r/{subredditName}/top?limit={limit}&t={timeFilter}";
                var response = await _client.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                _logger.LogDebug($"Reddit API response: {json}");
                _logger.LogDebug($"Request URL: {url}");


                //response.EnsureSuccessStatusCode();

                var document = JsonDocument.Parse(json);
                var children = document.RootElement.GetProperty("data").GetProperty("children");

                foreach (var child in children.EnumerateArray())
                {
                    var data = child.GetProperty("data");

                    if (_sessionFetchedPostIds.Contains(item: data.GetProperty("id").GetString()))
                    {
                        continue;
                    }

                    var post = new RedditPostInfo
                    {
                        Id = data.GetProperty("id").GetString() ?? "",
                        Title = data.GetProperty("title").GetString() ?? "",
                        Author = data.GetProperty("author").GetString() ?? "",
                        Subreddit = data.GetProperty("subreddit").GetString() ?? "",
                        Score = data.GetProperty("score").GetInt32(),
                        CommentCount = data.GetProperty("num_comments").GetInt32(),
                        IsNsfw = data.GetProperty("over_18").GetBoolean(),
                        CreatedUtc = DateTimeOffset.FromUnixTimeSeconds(data.GetProperty("created_utc").GetInt64()).UtcDateTime,
                        Url = "https://www.reddit.com" + data.GetProperty("permalink").GetString(),
                        SelfText = data.GetProperty("selftext").GetString() ?? "",
                        GoldAwards = data.TryGetProperty("total_awards_received", out var gold) ? gold.GetInt32() : 0
                    };

                    posts.Add(post);
                    _sessionFetchedPostIds.Add(post.Id);

                    if (posts.Count >= limit) break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching top posts from r/{subredditName}: {ex.Message}", ex);
            }

            return posts;
        }

        public Task<RedditPostInfo?> GetPostAsync(string postIdOrUrl)
        {
            _logger.LogWarning("GetPostAsync is not implemented with direct API access.");
            return Task.FromResult<RedditPostInfo?>(null);
        }

        public Task<List<RedditCommentInfo>> GetPostCommentsAsync(string postId, int limit = 50, int depth = 1)
        {
            _logger.LogWarning("GetPostCommentsAsync is not implemented with direct API access.");
            return Task.FromResult(new List<RedditCommentInfo>());
        }

        public static void MarkPostAsUsedThisSession(string postId)
        {
            if (!_sessionFetchedPostIds.Contains(postId))
            {
                _sessionFetchedPostIds.Add(postId);
            }
        }
    }
}
