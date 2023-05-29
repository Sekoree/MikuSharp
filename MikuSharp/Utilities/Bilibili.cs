using NYoutubeDL;

namespace MikuSharp.Utilities;

public static class Bilibili
{
	public static async Task<MemoryStream> GetBilibiliAsync(this InteractionContext ctx, string songId, ulong messageId)
	{
		try
		{
			await ctx.EditFollowupAsync(messageId, new DiscordWebhookBuilder().WithContent("Downloading video (this may take up to 5 minutes)"));

			var youtubeDlPath = OperatingSystem.IsLinux() ? "youtube-dl" : "youtube-dl.exe";
			var ffmpegPath = OperatingSystem.IsLinux() ? "ffmpeg" : "ffmpeg.exe";
			var outputFilePath = $@"{songId}.mp4";
			var audioFilePath = $@"{songId}.mp3";

			var youtubeDl = new YoutubeDL(youtubeDlPath);
			youtubeDl.Options.FilesystemOptions.Output = outputFilePath;
			youtubeDl.Options.PostProcessingOptions.ExtractAudio = true;
			youtubeDl.Options.PostProcessingOptions.FfmpegLocation = ffmpegPath;
			youtubeDl.Options.PostProcessingOptions.AudioFormat = NYoutubeDL.Helpers.Enums.AudioFormat.mp3;
			youtubeDl.Options.PostProcessingOptions.AddMetadata = true;
			youtubeDl.Options.PostProcessingOptions.KeepVideo = false;

			youtubeDl.StandardOutputEvent += (sender, output) => ctx.Client.Logger.LogDebug("{data}", output);

			youtubeDl.StandardErrorEvent += (sender, error) => ctx.Client.Logger.LogDebug("{data}", error);

			youtubeDl.VideoUrl = "https://www.bilibili.com/video/" + songId;
			await youtubeDl.DownloadAsync();

			if (File.Exists(audioFilePath))
			{
				using var audioFile = File.Open(audioFilePath, FileMode.Open);
				var ms = new MemoryStream();
				await audioFile.CopyToAsync(ms, MikuBot._canellationTokenSource.Token);
				ms.Position = 0;
				File.Delete(audioFilePath);
				return ms;
			}
			else
				return null;
		}
		catch (Exception ex)
		{
			ctx.Client.Logger.LogDebug("{msg}", ex.Message);
			ctx.Client.Logger.LogDebug("{stack}", ex.StackTrace);
			return null;
		}
	}
}
