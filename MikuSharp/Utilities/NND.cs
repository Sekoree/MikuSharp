using NicoNicoNii;

namespace MikuSharp.Utilities;

public static class Nnd
{
	public static bool IsNndUrl(this string urlOrName)
		=> urlOrName.ToLower().StartsWith("http://nicovideo.jp")
			|| urlOrName.ToLower().StartsWith("http://sp.nicovideo.jp")
			|| urlOrName.ToLower().StartsWith("https://nicovideo.jp")
			|| urlOrName.ToLower().StartsWith("https://sp.nicovideo.jp")
			|| urlOrName.ToLower().StartsWith("http://www.nicovideo.jp")
			|| urlOrName.ToLower().StartsWith("https://www.nicovideo.jp");

	public static async Task<MemoryStream> GetNndAsync(this InteractionContext ctx, string urlOrName, string nicoNicoId, ulong messageId)
	{
		try
		{
			NNDClient nndClient = new();
			NicoVideoClient videoClient = new(nndClient);
			var videoPage = await videoClient.GetWatchPageInfoAsync(nicoNicoId);
			var downloadExe = "nnd.exe";
			var linuxExe = "nndownload.py";
			var cmd = downloadExe;
			await ctx.EditFollowupAsync(messageId, new DiscordWebhookBuilder().WithContent("Downloading video (this may take up to 10 min)"));

			if (OperatingSystem.IsLinux())
				cmd = linuxExe;

			using (Process downloadProcess = new())
			{
				downloadProcess.StartInfo.FileName = cmd;
				downloadProcess.StartInfo.Arguments = $"-g -o {$@"{nicoNicoId}"}.mp4 {$@"{urlOrName}"}";
				downloadProcess.OutputDataReceived += (sender, receiveArgs) => ctx.Client.Logger.LogDebug("{data}", $"\n{receiveArgs.Data}\n");

				downloadProcess.Start();
				await downloadProcess.WaitForExitAsync(MikuBot.CanellationTokenSource.Token);
			}

			var songTitle = videoPage?.Video?.Title ?? "[NND] Unknown Title";
			var songArtist = videoPage?.Owner?.Nickname ?? "Unknown Artist";
			await ctx.EditFollowupAsync(messageId, new DiscordWebhookBuilder().WithContent("Converting"));

			using (Process convertProcess = new())
			{
				convertProcess.StartInfo.FileName = "ffmpeg";
				convertProcess.StartInfo.Arguments = $"-i {$@"{nicoNicoId}"}.mp4 -metadata title=\"{songTitle}\" -metadata artist=\"{songArtist}\" {$@"{nicoNicoId}"}.mp3";
				convertProcess.OutputDataReceived += (sender, receiveArgs) => ctx.Client.Logger.LogDebug("{data}", receiveArgs.Data);

				convertProcess.Start();
				await convertProcess.WaitForExitAsync(MikuBot.CanellationTokenSource.Token);
			}

			MemoryStream ms = new(await File.ReadAllBytesAsync($@"{nicoNicoId}.mp3", MikuBot.CanellationTokenSource.Token));
			File.Delete($@"{nicoNicoId}.mp4");
			File.Delete($@"{nicoNicoId}.mp3");
			ms.Position = 0;
			return ms;
		}
		catch (Exception ex)
		{
			ctx.Client.Logger.LogDebug("{msg}", ex.Message);
			ctx.Client.Logger.LogDebug("{stack}", ex.StackTrace);
			await ctx.EditFollowupAsync(messageId, new DiscordWebhookBuilder().WithContent("Encountered error"));
			return null;
		}
	}

}
