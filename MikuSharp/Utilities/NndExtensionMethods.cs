using System.Diagnostics;

using NicoNicoNii;

namespace MikuSharp.Utilities;

/// <summary>
///     Provides extension methods for NND-related operations.
/// </summary>
public static class NndExtensionMethods
{
	/// <summary>
	///     Gets a NND video.
	/// </summary>
	/// <param name="ctx">The interaction context.</param>
	/// <param name="name">The name of the video.</param>
	/// <param name="videoId">The ID of the video.</param>
	/// <param name="msgId">The ID of the message.</param>
	/// <returns>The video as a memory stream.</returns>
	public static async Task<MemoryStream?> GetNndAsync(this InteractionContext ctx, string name, string videoId, ulong msgId)
	{
		try
		{
			NndClient nndClient = new();
			NicoVideoClient videoClient = new(nndClient);
			var videoPage = await videoClient.GetWatchPageInfoAsync(videoId);
			var downloadExe = "nnd.exe";
			var linuxExe = "nndownload.py";
			var cmd = downloadExe;
			await ctx.EditFollowupAsync(msgId, new DiscordWebhookBuilder().WithContent("Downloading video (this may take up to 10 min)"));
			if (OperatingSystem.IsLinux())
				cmd = linuxExe;
			Process downloadProcess = new();
			downloadProcess.StartInfo.FileName = cmd;
			downloadProcess.StartInfo.Arguments = $"-g -o {$@"{videoId}"}.mp4 {$@"{name}"}";
			downloadProcess.OutputDataReceived += (_, f) => ctx.Client.Logger.LogDebug("{data}", $"\n{f.Data}\n");
			downloadProcess.Start();
			await downloadProcess.WaitForExitAsync();
			var songTitle = videoPage?.Video?.Title ?? "[NND] Unknown Title";
			var songArtist = videoPage?.Owner?.Nickname ?? "Unknown Artist";
			await ctx.EditFollowupAsync(msgId, new DiscordWebhookBuilder().WithContent("Converting"));
			Process convertProgress = new();
			convertProgress.StartInfo.FileName = "ffmpeg";
			convertProgress.StartInfo.Arguments = $"-i {$@"{videoId}"}.mp4 -metadata title=\"{songTitle}\" -metadata artist=\"{songArtist}\" {$@"{videoId}"}.mp3";
			convertProgress.OutputDataReceived += (_, f) => ctx.Client.Logger.LogDebug("{data}", f.Data);
			convertProgress.Start();
			await convertProgress.WaitForExitAsync();
			File.Delete($@"{videoId}.mp4");
			MemoryStream ms = new(await File.ReadAllBytesAsync($@"{videoId}.mp3"));
			File.Delete($@"{videoId}.mp3");
			ms.Position = 0;
			return ms;
		}
		catch (Exception ex)
		{
			ctx.Client.Logger.LogDebug("{ex}", ex.Message);
			ctx.Client.Logger.LogDebug("{ex}", ex.StackTrace);
			await ctx.EditFollowupAsync(msgId, new DiscordWebhookBuilder().WithContent("Encountered error"));
			return null;
		}
	}
}
