# RedditVideoGeneratorV2

**RedditVideoGeneratorV2** is a C# application that automatically creates short-form videos from Reddit threads using AI-generated voiceovers, gameplay footage, and background music — perfect for YouTube Shorts, TikTok, and Instagram Reels.

---

## 🔧 Features

- 🔥 Pulls top posts & comments from any subreddit
- 🧠 Converts text to speech with [ElevenLabs](https://www.elevenlabs.io/) API
- 🎮 Automatically overlays content on gameplay or background footage
- 🎵 Adds background music
- 🎬 Outputs high-quality vertical videos

---

## 🚀 Getting Started

### 1. Clone the Repo
```bash
git clone https://github.com/deanable/RedditVideoGeneratorV2.git
cd RedditVideoGeneratorV2
```

### 2. Install Dependencies
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [FFmpeg](https://ffmpeg.org/download.html) (add to PATH)
- Visual Studio 2022 or newer

### 3. Configure Settings

Create or edit `appsettings.json` in `RedditVideoGenerator.UI/bin/Debug/net8.0-windows/`:

```json
{
  "ApplicationSettings": {
    "RedditAppId": "your_reddit_app_id",
    "RedditAppSecret": "your_secret",
    "RedditUsername": "your_reddit_username",
    "RedditPassword": "your_reddit_password",
    "RedditUserAgent": "windows:VideoGeneratorV2:v0.1.0 (by /u/your_username)",
    "ElevenLabsApiKey": "your_elevenlabs_key",
    "DefaultElevenLabsVoiceId": "voice_id_here",
    "FfmpegPath": "C:\\ffmpeg\\bin\\ffmpeg.exe",
    "GameplayVideosFolderPath": "C:\\Path\\To\\Gameplay",
    "BackgroundMusicFolderPath": "C:\\Path\\To\\Music",
    "OutputVideoFolderPath": "RedditVideos",
    "TemporaryFilesFolderPath": "TempRedditVideos",
    "DefaultSubreddit": "AskReddit",
    "VideoTargetDurationMinutes": 10
  }
}
```

---

## 🧪 Testing Reddit OAuth

You can verify your Reddit credentials using [Postman](https://www.postman.com/) by following [this OAuth guide](https://github.com/deanable/RedditVideoGeneratorV2/issues/1#issuecomment) (optional).

---

## 📄 License

MIT — free to use and modify.
