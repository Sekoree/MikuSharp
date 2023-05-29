using NicoNicoNii;

namespace MikuSharp.Utilities;

public static class NND
{
	public static bool IsNndUrl(this string url_or_name)
		=> url_or_name.ToLower().StartsWith("http://nicovideo.jp")
			|| url_or_name.ToLower().StartsWith("http://sp.nicovideo.jp")
			|| url_or_name.ToLower().StartsWith("https://nicovideo.jp")
			|| url_or_name.ToLower().StartsWith("https://sp.nicovideo.jp")
			|| url_or_name.ToLower().StartsWith("http://www.nicovideo.jp")
			|| url_or_name.ToLower().StartsWith("https://www.nicovideo.jp");

	public static async Task<MemoryStream> GetNNDAsync(this InteractionContext ctx, string url_or_name, string nico_nico_id, ulong messageId)
	{
		try
		{
			NNDClient nndClient = new();
			NicoVideoClient videoClient = new(nndClient);
			var videoPage = await videoClient.GetWatchPageInfoAsync(nico_nico_id);
			var download_exe = "nnd.exe";
			var linux_exe = "nndownload.py";
			var cmd = download_exe;
			await ctx.EditFollowupAsync(messageId, new DiscordWebhookBuilder().WithContent("Downloading video (this may take up to 10 min)"));

			if (OperatingSystem.IsLinux())
				cmd = linux_exe;

			using (Process downloadProcess = new())
			{
				downloadProcess.StartInfo.FileName = cmd;
				downloadProcess.StartInfo.Arguments = $"-g -o {$@"{nico_nico_id}"}.mp4 {$@"{url_or_name}"}";
				downloadProcess.OutputDataReceived += (sender, receiveArgs) => ctx.Client.Logger.LogDebug("{data}", $"\n{receiveArgs.Data}\n");

				downloadProcess.Start();
				await downloadProcess.WaitForExitAsync(MikuBot._canellationTokenSource.Token);
			}

			var songTitle = videoPage?.Video?.Title ?? "[NND] Unknown Title";
			var songArtist = videoPage?.Owner?.Nickname ?? "Unknown Artist";
			await ctx.EditFollowupAsync(messageId, new DiscordWebhookBuilder().WithContent("Converting"));

			using (Process convertProcess = new())
			{
				convertProcess.StartInfo.FileName = "ffmpeg";
				convertProcess.StartInfo.Arguments = $"-i {$@"{nico_nico_id}"}.mp4 -metadata title=\"{songTitle}\" -metadata artist=\"{songArtist}\" {$@"{nico_nico_id}"}.mp3";
				convertProcess.OutputDataReceived += (sender, receiveArgs) => ctx.Client.Logger.LogDebug("{data}", receiveArgs.Data);

				convertProcess.Start();
				await convertProcess.WaitForExitAsync(MikuBot._canellationTokenSource.Token);
			}

			MemoryStream ms = new(await File.ReadAllBytesAsync($@"{nico_nico_id}.mp3", MikuBot._canellationTokenSource.Token));
			File.Delete($@"{nico_nico_id}.mp4");
			File.Delete($@"{nico_nico_id}.mp3");
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
