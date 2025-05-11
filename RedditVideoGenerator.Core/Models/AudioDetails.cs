// RedditVideoGenerator.Core/Models/AudioDetails.cs
using System; // Required for TimeSpan

namespace RedditVideoGenerator.Core.Models
{
    public class AudioDetails
    {
        public string FilePath { get; }
        public TimeSpan Duration { get; }

        public AudioDetails(string filePath, TimeSpan duration)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));

            FilePath = filePath;
            Duration = duration;
        }
    }
}