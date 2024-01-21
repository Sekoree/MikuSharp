using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;

using Microsoft.Extensions.Logging;

using NYoutubeDL;

using System;
using System.IO;
using System.Threading.Tasks;

namespace MikuSharp.Utilities;

public static class Bilibili
{
	public static async Task<MemoryStream> GetBilibiliAsync(this InteractionContext ctx, string s, ulong msgId)
	{
		try
		{
			await ctx.EditFollowupAsync(msgId, new DiscordWebhookBuilder().WithContent("Downloading video(this may take up to 5 min)"));
			var youtubeDl = new YoutubeDL(@"youtube-dl.exe");
			youtubeDl.Options.FilesystemOptions.Output = $@"{s}.mp4";
			youtubeDl.Options.PostProcessingOptions.ExtractAudio = true;
			youtubeDl.Options.PostProcessingOptions.FfmpegLocation = @"ffmpeg.exe";
			youtubeDl.Options.PostProcessingOptions.AudioFormat = NYoutubeDL.Helpers.Enums.AudioFormat.mp3;
			youtubeDl.Options.PostProcessingOptions.AddMetadata = true;
			youtubeDl.Options.PostProcessingOptions.KeepVideo = false;
			youtubeDl.StandardOutputEvent += (e, f) => { ctx.Client.Logger.LogDebug("{data}", f); };
			youtubeDl.StandardErrorEvent += (e, f) => { ctx.Client.Logger.LogDebug("{data}", f); };
			youtubeDl.VideoUrl = "https://www.bilibili.com/video/" + s;
			await youtubeDl.DownloadAsync();
			var ms = new MemoryStream();
			if (File.Exists($@"{s}.mp3"))
			{
				var song = File.Open($@"{s}.mp3", FileMode.Open);
				await song.CopyToAsync(ms);
				ms.Position = 0;
				song.Close();
				File.Delete($@"{s}.mp3");
			}

			return ms;
		}
		catch (Exception ex)
		{
			ctx.Client.Logger.LogDebug("{ex}", ex.Message);
			ctx.Client.Logger.LogDebug("{ex}", ex.StackTrace);
			return null;
		}
	}
}