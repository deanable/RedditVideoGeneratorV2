// RedditVideoGenerator.Core/Services/RedditApiService.cs
using RedditVideoGenerator.Core.Models;
using RedditVideoGenerator.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http; // Added for potential direct check, though Reddit.NET handles it
using System.Threading.Tasks;
using Reddit;
using Reddit.Controllers;
using Reddit.Things;
using Reddit.Inputs;
using Microsoft.VisualBasic.Logging;
using System.Threading.Channels;
using System.Windows.Forms;

namespace RedditVideoGenerator.Core.Services
{
    public class RedditApiService : IRedditService
    {
        private readonly ILoggerService _logger;
        private readonly IConfigurationService _configurationService;
        private RedditClient? _redditClient;

        private static readonly List<string> _sessionFetchedPostIds = new();

        public RedditApiService(ILoggerService logger, IConfigurationService configurationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        }

        public Task InitializeAsync()
        {
            string? appId = _configurationService.Settings.RedditAppId;
            string? appSecret = _configurationService.Settings.RedditAppSecret; // Often blank for installed apps
            string? userAgent = _configurationService.Settings.RedditUserAgent;

            if (string.IsNullOrWhiteSpace(appId))
            {
                _logger.LogError("Reddit AppId is not configured in appsettings.json. Cannot initialize RedditService.", null);
                return Task.CompletedTask;
            }
            if (string.IsNullOrWhiteSpace(userAgent))
            {
                _logger.LogError("Reddit UserAgent is not configured in appsettings.json. Cannot initialize RedditService.", null);
                // Provide a default or recommend setting it.
                userAgent = $"desktop:RedditVideoGeneratorV2_UnknownUser:0.1"; // Fallback, but user should configure
                _logger.LogWarning($"Using fallback UserAgent: {userAgent}. Please configure a proper UserAgent in appsettings.json.", null);
            }

            _logger.LogInformation($"Attempting to initialize Reddit client with AppId: '{appId}', UserAgent: '{userAgent}'");
            try
            {
                // For "installed app" type (which is common for userless desktop apps),
                // appSecret is often not used or is empty.
                // Reddit.NET's constructor for userless auth primarily needs appId and userAgent.
                // A refreshToken (like a deviceId) can be provided for some auth flows.
                // The original project generated a GUID for device_id. Let's try that as refreshToken.
                string refreshToken = Guid.NewGuid().ToString();
                _logger.LogDebug($"Using generated RefreshToken (DeviceId): {refreshToken}");

                // Note: For Reddit.NET 1.5.2, the constructor order might be:
                // appId, refreshToken, accessToken, appSecret, userAgent, deviceId (optional, separate from refreshToken)
                // Or, for simple userless app-only: appId, appSecret (can be null/empty), userAgent
                // Let's try the constructor that seems most appropriate for userless installed app.
                // The library should handle fetching the initial token.
                _redditClient = new RedditClient(
                    appId: appId,
                    appSecret: appSecret, // Pass it, even if empty/null for installed app type
                    refreshToken: refreshToken, // Using a GUID as a device_id/refreshToken
                    userAgent: userAgent
                    // accessToken: null // Library will fetch this
                    );

                // A quick test to see if the client is functional (optional, but good for diagnostics)
                // This might throw if authentication failed silently in constructor.
                var me = _redditClient.Account?.Me; // This forces an authenticated call if not done already.
                _logger.LogInformation(me != null ? $"Reddit client initialized. User: {me.Name}" : "Reddit client initialized (userless or anonymous).");

            }
            catch (Exception ex)
            {
                _logger.LogCritical($"CRITICAL: Failed to initialize RedditClient. This is often due to incorrect AppId/UserAgent in appsettings.json, network issues, or Reddit API problems. Exception: {ex.Message}", ex);
                // Log the full exception details, including any inner exceptions if present.
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner Exception: {ex.InnerException.Message}", ex.InnerException);
                }
                _redditClient = null;
                // Optionally, rethrow or handle this as a critical startup failure.
                // For now, OnStartup in App.xaml.cs will catch this and show a MessageBox.
                throw; // Rethrow to be caught by App.xaml.cs OnStartup
            }
            return Task.CompletedTask;
        }

        public async Task<RedditPostInfo?> GetPostAsync(string postIdOrUrl)
        {
            if (_redditClient == null)
            {
                _logger.LogError("Reddit client is not initialized. Call InitializeAsync first.", null);
                return null;
            }
            if (string.IsNullOrWhiteSpace(postIdOrUrl))
            {
                _logger.LogWarning("Post ID or URL is null or empty.", null);
                return null;
            }

            _logger.LogDebug($"Fetching post: {postIdOrUrl}");
            try
            {
                string postId = ExtractPostId(postIdOrUrl);
                if (string.IsNullOrWhiteSpace(postId))
                {
                    _logger.LogWarning($"Could not extract a valid Post ID from '{postIdOrUrl}'.", null);
                    return null;
                }

                Reddit.Controllers.Post postController = await Task.Run(() => _redditClient.Post(postId).About());

                return (postController != null) ? MapRedditPostToPostInfo(postController) : null;
            }
            catch (Exception ex)
            {
                // Check if the exception is from Newtonsoft.Json and log the response if possible
                if (ex.Source == "Newtonsoft.Json" && ex is Newtonsoft.Json.JsonReaderException jre)
                {
                    _logger.LogError($"JSON Parsing Error fetching post '{postIdOrUrl}': {jre.Message}. Possible HTML response instead of JSON.", jre);
                }
                else if (ex.InnerException is Newtonsoft.Json.JsonReaderException innerJre) // RestSharp might wrap it
                {
                    _logger.LogError($"JSON Parsing Error (Inner) fetching post '{postIdOrUrl}': {innerJre.Message}. Possible HTML response instead of JSON.", innerJre);
                }
                else
                {
                    _logger.LogError($"Error fetching post '{postIdOrUrl}': {ex.Message}", ex);
                }
                return null;
            }
        }

        private static string ExtractPostId(string postIdOrUrl)
        {
            if (string.IsNullOrWhiteSpace(postIdOrUrl)) return string.Empty;

            if (postIdOrUrl.Contains("/comments/"))
            {
                var segments = postIdOrUrl.Split('/');
                var commentsIndex = Array.IndexOf(segments, "comments");
                if (commentsIndex != -1 && segments.Length > commentsIndex + 1)
                {
                    return segments[commentsIndex + 1];
                }
            }
            else if (postIdOrUrl.StartsWith("t3_"))
            {
                return postIdOrUrl[3..];
            }
            return postIdOrUrl;
        }

        public async Task<List<RedditPostInfo>> GetTopPostsAsync(string subredditName, string timeFilter = "day", int limit = 10)
        {
            if (_redditClient == null)
            {
                _logger.LogError("Reddit client is not initialized. Call InitializeAsync first.", null);
                return new List<RedditPostInfo>();
            }

            _logger.LogDebug($"Fetching top {limit} posts from r/{subredditName} for time filter: {timeFilter}");
            try
            {
                var subredditController = _redditClient.Subreddit(subredditName);
                List<Reddit.Controllers.Post> topPostsControllers = await Task.Run(() =>
                    subredditController.Posts.GetTop(new TimedCatSrListingInput(t: timeFilter.ToLowerInvariant(), limit: limit + 20)));

                var postInfos = new List<RedditPostInfo>();
                if (topPostsControllers != null)
                {
                    foreach (var postController in topPostsControllers)
                    {
                        if (postController != null && postController.Listing != null && !_sessionFetchedPostIds.Contains(postController.Id))
                        {
                            RedditPostInfo? mappedPost = MapRedditPostToPostInfo(postController);
                            if (mappedPost != null)
                            {
                                postInfos.Add(mappedPost);
                            }
                            if (postInfos.Count >= limit) break;
                        }
                    }
                }
                return postInfos;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching top posts from r/{subredditName}: {ex.Message}", ex);
                return new List<RedditPostInfo>();
            }
        }

        public async Task<List<RedditCommentInfo>> GetPostCommentsAsync(string postId, int limit = 50, int depth = 1)
        {
            if (_redditClient == null)
            {
                _logger.LogError("Reddit client is not initialized. Call InitializeAsync first.", null);
                return new List<RedditCommentInfo>();
            }
            string cleanPostId = postId.Replace("t3_", "");
            _logger.LogDebug($"Fetching {limit} comments for post ID: {cleanPostId} with depth: {depth}");

            try
            {
                var postController = _redditClient.Post(cleanPostId);
                List<Reddit.Controllers.Comment> commentControllers = await Task.Run(() =>
                    postController.Comments.GetComments(sort: "top", limit: limit, depth: depth));

                return commentControllers?
                           .Select(MapRedditCommentToCommentInfo)
                           .Where(c => c != null)
                           .Select(c => c!)
                           .ToList()
                       ?? new List<RedditCommentInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching comments for post ID '{cleanPostId}': {ex.Message}", ex);
                return new List<RedditCommentInfo>();
            }
        }

        public static void MarkPostAsUsedThisSession(string postId)
        {
            if (!_sessionFetchedPostIds.Contains(postId))
            {
                _sessionFetchedPostIds.Add(postId);
            }
        }

        private RedditPostInfo? MapRedditPostToPostInfo(Reddit.Controllers.Post postController)
        {
            if (postController == null) return null;
            var listingData = postController.Listing;
            if (listingData == null)
            {
                _logger.LogWarning($"Post listing data was null for post ID {postController.Id}. Cannot map post.", null);
                return null;
            }

            string selfTextContent = string.Empty;
            if (listingData.IsSelf)
            {
                selfTextContent = listingData.SelfText ?? string.Empty;
                if (string.IsNullOrWhiteSpace(selfTextContent) && !string.IsNullOrWhiteSpace(listingData.SelfTextHTML))
                {
                    selfTextContent = listingData.SelfTextHTML;
                }
            }

            return new RedditPostInfo
            {
                Id = postController.Id,
                Title = postController.Title,
                Author = postController.Author,
                Subreddit = postController.Subreddit,
                Score = postController.Score,
                CommentCount = listingData.NumComments,
                IsNsfw = listingData.Over18,
                CreatedUtc = listingData.CreatedUTC,
                Url = "https://www.reddit.com" + listingData.Permalink,
                SelfText = selfTextContent,
                PlatinumAwards = postController.Awards?.Platinum ?? 0,
                GoldAwards = postController.Awards?.Gold ?? 0,
                SilverAwards = postController.Awards?.Silver ?? 0
            };
        }

        private RedditCommentInfo? MapRedditCommentToCommentInfo(Reddit.Controllers.Comment commentController)
        {
            if (commentController == null) return null;
            var listingData = commentController.Listing;
            if (listingData == null)
            {
                _logger.LogWarning($"Comment listing data was null for comment ID {commentController.Id}. Cannot map comment.", null);
                return null;
            }

            int platinum = 0, gold = 0, silver = 0;
            if (listingData.Gildings is Dictionary<string, int> gildingsDictionary)
            {
                gildingsDictionary.TryGetValue("gid_3", out platinum);
                gildingsDictionary.TryGetValue("gid_2", out gold);
                gildingsDictionary.TryGetValue("gid_1", out silver);
            }

            var commentInfo = new RedditCommentInfo
            {
                Id = commentController.Id,
                Author = commentController.Author,
                Body = listingData.Body,
                BodyHtml = listingData.BodyHTML,
                Score = listingData.Score,
                CreatedUtc = listingData.CreatedUTC,
                IsSubmitter = listingData.IsSubmitter,
                PlatinumAwards = platinum,
                GoldAwards = gold,
                SilverAwards = silver,
                Replies = new List<RedditCommentInfo>()
            };

            if (commentController.Replies is List<Reddit.Controllers.Comment> actualReplyControllers && actualReplyControllers.Any())
            {
                foreach (var replyController in actualReplyControllers)
                {
                    if (replyController != null)
                    {
                        var mappedReply = MapRedditCommentToCommentInfo(replyController);
                        if (mappedReply != null) commentInfo.Replies.Add(mappedReply);
                    }
                }
            }
            return commentInfo;
        }
    }
}