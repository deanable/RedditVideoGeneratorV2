// RedditVideoGenerator.Core/Models/RedditCommentInfo.cs
namespace RedditVideoGenerator.Core.Models
{
    public class RedditCommentInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty; // Plain text body
        public string BodyHtml { get; set; } = string.Empty; // HTML body from Reddit
        public int Score { get; set; }
        public DateTime CreatedUtc { get; set; }
        public bool IsSubmitter { get; set; }
        public List<RedditCommentInfo> Replies { get; set; } = new List<RedditCommentInfo>();

        // Optional: Awards
        public int PlatinumAwards { get; set; }
        public int GoldAwards { get; set; }
        public int SilverAwards { get; set; }
    }
}