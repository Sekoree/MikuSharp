/*using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;

using Microsoft.Extensions.Logging;

using NicoNicoNii;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace MikuSharp.Utilities;

public static class NND
{
	public static async Task<MemoryStream> GetNNDAsync(this InteractionContext ctx,string n, string s, ulong msg_id)
	{
		try
		{
			NNDClient nndClient = new();
			NicoVideoClient videoClient = new(nndClient);
			var videoPage = await videoClient.GetWatchPageInfoAsync(s);
			var download_exe = "nnd.exe";
			var linux_exe = "nndownload.py";
			string cmd = download_exe;
			await ctx.EditFollowupAsync(msg_id, new DiscordWebhookBuilder().WithContent("Downloading video (this may take up to 10 min)"));
			if (OperatingSystem.IsLinux())
				cmd = linux_exe;
			Process downloadProcess = new();
			downloadProcess.StartInfo.FileName = cmd;
			downloadProcess.StartInfo.Arguments = $"-g -o {$@"{s}"}.mp4 {$@"{n}"}";
			downloadProcess.OutputDataReceived += (d, f) =>
			{
				ctx.Client.Logger.LogDebug("{data}", $"\n{f.Data}\n");
			};
			downloadProcess.Start();
			await downloadProcess.WaitForExitAsync();
			var songTitle = videoPage?.Video?.Title ?? "[NND] Unknown Title";
			var songArtist = videoPage?.Owner?.Nickname ?? "Unknown Artist";
			await ctx.EditFollowupAsync(msg_id, new DiscordWebhookBuilder().WithContent("Converting"));
			Process convertProgress = new();
			convertProgress.StartInfo.FileName = "ffmpeg";
			convertProgress.StartInfo.Arguments = $"-i {$@"{s}"}.mp4 -metadata title=\"{songTitle}\" -metadata artist=\"{songArtist}\" {$@"{s}"}.mp3";
			convertProgress.OutputDataReceived += (d, f) =>
			{
				ctx.Client.Logger.LogDebug("{data}", f.Data);
			};
			convertProgress.Start();
			await convertProgress.WaitForExitAsync();
			File.Delete($@"{s}.mp4");
			MemoryStream ms = new(await File.ReadAllBytesAsync($@"{s}.mp3"));
			File.Delete($@"{s}.mp3");
			ms.Position = 0;
			return ms;
		}
		catch (Exception ex)
		{
			ctx.Client.Logger.LogDebug("{ex}", ex.Message);
			ctx.Client.Logger.LogDebug("{ex}", ex.StackTrace);
			await ctx.EditFollowupAsync(msg_id, new DiscordWebhookBuilder().WithContent("Encountered error"));
			return null;
		}
	}

}
*/