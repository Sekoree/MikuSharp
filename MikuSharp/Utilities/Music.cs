using AlbumArtExtraction;

using Google.Apis.Services;
using Google.Apis.YouTube.v3;

using HeyRed.Mime;

using MikuSharp.Entities;
using MikuSharp.Enums;

namespace MikuSharp.Utilities;

public static class Music
{
	public static ExtService GetExtService(string e)
	{
		return e switch
		{
			"Youtube" => ExtService.Youtube,
			"Soundcloud" => ExtService.Soundcloud,
			_ => ExtService.None
		};
	}

	public static string GetPlaybackOptions(this MusicInstance instance)
	{
		var opts = string.Empty;
		if (instance.repeatMode == RepeatMode.On)
			opts += DiscordEmoji.FromUnicode("🔂");
		if (instance.repeatMode == RepeatMode.All)
			opts += DiscordEmoji.FromUnicode("🔁");
		if (instance.shuffleMode == ShuffleMode.On)
			opts += DiscordEmoji.FromUnicode("🔀");
		return string.IsNullOrEmpty(opts) ? "None" : opts;
	}

	public static async Task ConditionalConnect(this Guild guild, InteractionContext ctx)
	{
		if (guild.musicInstance.guildConnection?.IsConnected != null && !guild.musicInstance.guildConnection.IsConnected)
			return;
		await guild.musicInstance.ConnectToChannel(ctx.Member.VoiceState.Channel);
	}

	public static async Task<bool> IsNotConnected(this Guild guild, InteractionContext ctx)
	{
		if (guild.musicInstance == null || guild.musicInstance.guildConnection?.IsConnected == false)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error: Not connected!"));
			return true;
		}
		return false;
	}

	public static string SearchUrlOrAttachment(this DiscordAttachment? attachment, string? search_or_url)
		=> !string.IsNullOrEmpty(search_or_url) ? search_or_url : attachment?.ProxyUrl;

	public static void GetPlayingState(this Guild guild, out string time1, out string time2)
	{
		if (guild.musicInstance.currentSong.track.Length.Hours < 1)
		{
			time1 = guild.musicInstance.guildConnection.CurrentState.PlaybackPosition.ToString(@"mm\:ss");
			time2 = guild.musicInstance.currentSong.track.Length.ToString(@"mm\:ss");
		}
		else
		{
			time1 = guild.musicInstance.guildConnection.CurrentState.PlaybackPosition.ToString(@"hh\:mm\:ss");
			time2 = guild.musicInstance.currentSong.track.Length.ToString(@"hh\:mm\:ss");
		}
	}

	public static void GetPlayingState(this Entry entry, out string time)
	{
		time = entry.track.Length.Hours < 1 ? entry.track.Length.ToString(@"mm\:ss") : entry.track.Length.ToString(@"hh\:mm\:ss");
	}

	public static async Task<DiscordEmbedBuilder> GetYoutubePlayingInformationAsync(this DiscordEmbedBuilder builder, Guild guild, List<Entry>? lastPlayedSongs = null)
	{
		try
		{
			var youtubeService = new YouTubeService(new BaseClientService.Initializer()
			{
				ApiKey = MikuBot.Config.YoutubeApiToken,
				ApplicationName = typeof(MikuBot).ToString()
			});

			var searchQuery = lastPlayedSongs != null ?
				$"{lastPlayedSongs[0].track.Title} {lastPlayedSongs[0].track.Author}" :
				$"{guild.musicInstance.currentSong.track.Title} {guild.musicInstance.currentSong.track.Author}";

			var searchListRequest = youtubeService.Search.List("snippet");
			searchListRequest.Q = searchQuery;
			searchListRequest.MaxResults = 1;
			searchListRequest.Type = "video";
			var searchListResponse = await searchListRequest.ExecuteAsync();

			if (lastPlayedSongs == null)
			{
				var currentSong = guild.musicInstance.currentSong;
				guild.GetPlayingState(out var time1, out var time2);
				builder.AddField(new DiscordEmbedField($"{currentSong.track.Title} ({time1}/{time2})", $"[Video Link]({currentSong.track.Uri})\n" +
					$"[{currentSong.track.Author}](https://www.youtube.com/channel/{searchListResponse.Items[0].Snippet.ChannelId})"));
			}
			else
			{
				var currentSong = lastPlayedSongs[0];
				currentSong.GetPlayingState(out var time);
				builder.AddField(new DiscordEmbedField($"{currentSong.track.Title} ({time})", $"[Video Link]({currentSong.track.Uri})\n" +
					$"[{currentSong.track.Author}](https://www.youtube.com/channel/{searchListResponse.Items[0].Snippet.ChannelId})"));
			}

			var description = searchListResponse.Items[0].Snippet.Description.Length > 1000 ?
				string.Concat(searchListResponse.Items[0].Snippet.Description.AsSpan(0, 1000), "...") :
				searchListResponse.Items[0].Snippet.Description;

			builder.AddField(new DiscordEmbedField("Description", description));
			builder.WithImageUrl(searchListResponse.Items[0].Snippet.Thumbnails.High.Url);
			builder.AddField(new DiscordEmbedField("Playback options", guild.musicInstance.GetPlaybackOptions()));
		}
		catch (Exception)
		{
			if (builder.Fields.Count != 1)
			{
				if (lastPlayedSongs == null)
				{
					var currentSong = guild.musicInstance.currentSong;
					guild.GetPlayingState(out var time1, out var time2);
					builder.AddField(new DiscordEmbedField($"{currentSong.track.Title} ({time1}/{time2})", $"By {currentSong.track.Author}\n[Link]({currentSong.track.Uri})\nRequested by <@{currentSong.addedBy}>"));
				}
				else
				{
					var currentSong = lastPlayedSongs[0];
					currentSong.GetPlayingState(out var time);
					builder.AddField(new DiscordEmbedField($"{currentSong.track.Title} ({time})", $"By {currentSong.track.Author}\n[Link]({currentSong.track.Uri})"));
				}
				builder.AddField(new DiscordEmbedField("Playback options", guild.musicInstance.GetPlaybackOptions()));
			}
		}
		return builder;
	}

	public static async Task<(DiscordEmbedBuilder Embed, Stream? File, string? FileName)> GetUrlPlayingInformationAsync(this DiscordEmbedBuilder builder, DiscordClient client, Guild guild, List<Entry>? lastPlayedSongs)
	{
		Stream? img = null;
		Entry entry = lastPlayedSongs != null ? lastPlayedSongs[0] : guild.musicInstance.currentSong;

		try
		{
			var uriSegments = entry.track.Uri.Segments;
			var filename = $"{uriSegments[uriSegments.Length - 2]}.{uriSegments[^1]}";

			using (MemoryStream d = new(await client.RestClient.GetByteArrayAsync(entry.track.Uri)))
			using (FileStream e = File.Create(filename))
			{
				d.Position = 0;
				await d.CopyToAsync(e);
			}

			var selector = new Selector();
			var extractor = selector.SelectAlbumArtExtractor(filename);
			img = extractor.Extract(filename);
			File.Delete(filename);
		}
		catch (Exception ex)
		{
			client.Logger.LogDebug("{ex}", ex.Message);
			client.Logger.LogDebug("{ex}", ex.StackTrace);
			img = null;
		}

		builder.AddField(new DiscordEmbedField($"{entry.track.Title} ({guild.GetDynamicPlayingState(lastPlayedSongs)})", $"By {entry.track.Author}\n[Link]({entry.track.Uri})\n{(lastPlayedSongs == null ? $"Requested by <@{guild.musicInstance.currentSong.addedBy}>" : "")}"));
		builder.AddField(new DiscordEmbedField("Playback options", guild.musicInstance.GetPlaybackOptions()));

		string attachmentName = string.Empty;

		if (img != null)
		{
			var extension = MimeGuesser.GuessExtension(img);
			attachmentName = $"{entry.track.Uri.Segments[^2]}.{extension}";
			builder.WithImageUrl($"attachment://{attachmentName}");
		}

		return (builder, img, attachmentName);
	}

	public static DiscordEmbedBuilder GetOtherPlayingInformationAsync(this DiscordEmbedBuilder builder, Guild guild, List<Entry>? lastPlayedSongs = null)
	{
		Entry entry = lastPlayedSongs != null ? lastPlayedSongs[0] : guild.musicInstance.currentSong;
		builder.AddField(new DiscordEmbedField($"{entry.track.Title} ({guild.GetDynamicPlayingState(lastPlayedSongs)})", $"By {entry.track.Author}\n[Link]({entry.track.Uri})\n{(lastPlayedSongs == null ? $"Requested by <@{guild.musicInstance.currentSong.addedBy}>" : "")}"));
		builder.AddField(new DiscordEmbedField("Playback options", guild.musicInstance.GetPlaybackOptions()));
		return builder;
	}

	public static string GetDynamicPlayingState(this Guild guild, List<Entry>? lastPlayedSongs = null)
	{
		if (lastPlayedSongs == null)
		{
			guild.GetPlayingState(out var time1, out var time2);
			return $"{time1}/{time2}";
		}
		else
		{
			lastPlayedSongs[0].GetPlayingState(out var time);
			return $"{time}";
		}
	}

	public static async Task SendPlayingInformationAsync(this InteractionContext ctx, DiscordEmbedBuilder builder, Guild guild, List<Entry>? lastPlayedSongs = null)
	{
		DiscordWebhookBuilder webhookBuilder = new();
		Entry entry = lastPlayedSongs != null ? lastPlayedSongs[0] : guild.musicInstance.currentSong;

		if (entry == null)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("I'm not playing anything right now"));
			return;
		}

		DiscordEmbedBuilder lBuilder;

		if (entry.track.Uri.ToString().Contains("youtu"))
		{
			lBuilder = await builder.GetYoutubePlayingInformationAsync(guild, lastPlayedSongs);
		}
		else if (!entry.track.Uri.ToString().StartsWith("https://media.discordapp.net/attachments/") && !entry.track.Uri.ToString().StartsWith("https://cdn.discordapp.com/attachments/"))
		{
			lBuilder = builder.GetOtherPlayingInformationAsync(guild, lastPlayedSongs);
		}
		else
		{
			var info = await builder.GetUrlPlayingInformationAsync(ctx.Client, guild, lastPlayedSongs);
			lBuilder = info.Embed;
			if (info.File != null)
			{
				webhookBuilder.AddFile(info.FileName, info.File, true);
			}
		}

		await ctx.EditResponseAsync(webhookBuilder.AddEmbed(lBuilder.Build()));
	}

}
