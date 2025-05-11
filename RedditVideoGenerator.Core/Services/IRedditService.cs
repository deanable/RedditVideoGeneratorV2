// RedditVideoGenerator.Core/Services/IRedditService.cs
using RedditVideoGenerator.Core.Models; // Required for RedditPostInfo, RedditCommentInfo
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RedditVideoGenerator.Core.Services
{
    public interface IRedditService
    {
        /// <summary>
        /// Initializes the Reddit client with necessary credentials or settings.
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Gets a specific Reddit post by its ID or URL.
        /// </summary>
        /// <param name="postIdOrUrl">The ID (e.g., "t3_xxxxxx") or full URL of the post.</param>
        /// <returns>Information about the post, or null if not found.</returns>
        Task<RedditPostInfo?> GetPostAsync(string postIdOrUrl);

        /// <summary>
        /// Gets a list of top posts from a specified subreddit.
        /// </summary>
        /// <param name="subredditName">The name of the subreddit (e.g., "AskReddit").</param>
        /// <param name="timeFilter">Time filter (e.g., "day", "week", "month", "year", "all").</param>
        /// <param name="limit">Maximum number of posts to return.</param>
        /// <returns>A list of top posts.</returns>
        Task<List<RedditPostInfo>> GetTopPostsAsync(string subredditName, string timeFilter = "day", int limit = 10);

        /// <summary>
        /// Gets the top-level comments for a given post.
        /// </summary>
        /// <param name="postId">The ID of the post (e.g., "xxxxxx" from "t3_xxxxxx").</param>
        /// <param name="limit">Maximum number of comments to return.</param>
        /// <param name="depth">Depth of comments to retrieve (0 for top-level only).</param>
        /// <returns>A list of comments.</returns>
        Task<List<RedditCommentInfo>> GetPostCommentsAsync(string postId, int limit = 50, int depth = 1);
    }
}