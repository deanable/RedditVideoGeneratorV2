// RedditVideoGenerator.Core/Models/RedditPostInfo.cs
namespace RedditVideoGenerator.Core.Models
{
    public class RedditPostInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Subreddit { get; set; } = string.Empty;
        public int Score { get; set; }
        public int CommentCount { get; set; }
        public bool IsNsfw { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string Url { get; set; } = string.Empty;
        public string SelfText { get; set; } = string.Empty; // For text posts

        // Optional: Awards (can be simple counts or more complex objects later)
        public int PlatinumAwards { get; set; }
        public int GoldAwards { get; set; }
        public int SilverAwards { get; set; }
    }
}