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
		if (instance.RepeatMode == RepeatMode.On)
			opts += DiscordEmoji.FromUnicode("🔂");
		if (instance.RepeatMode == RepeatMode.All)
			opts += DiscordEmoji.FromUnicode("🔁");
		if (instance.ShuffleMode == ShuffleMode.On)
			opts += DiscordEmoji.FromUnicode("🔀");
		return string.IsNullOrEmpty(opts) ? "None" : opts;
	}

	public static async Task ConditionalConnect(this Guild guild, InteractionContext ctx)
	{
		if (guild.MusicInstance.GuildPlayer?.IsConnected != null && guild.MusicInstance.GuildPlayer.IsConnected)
			return;
		await guild.MusicInstance.ConnectToChannel(ctx.Member.VoiceState.Channel);
	}

	public static async Task<bool> IsNotConnected(this Guild guild, InteractionContext ctx)
	{
		if (guild.MusicInstance == null || guild.MusicInstance.GuildPlayer?.IsConnected == false)
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
		if (guild.MusicInstance.CurrentSong.Track.Info.Length.Hours < 1)
		{
			time1 = guild.MusicInstance.GuildPlayer.Player.PlayerState.Position.ToString(@"mm\:ss");
			time2 = guild.MusicInstance.CurrentSong.Track.Info.Length.ToString(@"mm\:ss");
		}
		else
		{
			time1 = guild.MusicInstance.GuildPlayer.Player.PlayerState.Position.ToString(@"hh\:mm\:ss");
			time2 = guild.MusicInstance.CurrentSong.Track.Info.Length.ToString(@"hh\:mm\:ss");
		}
	}

	public static void GetPlayingState(this Entry entry, out string time)
		=> time = entry.Track.Info.Length.Hours < 1 ? entry.Track.Info.Length.ToString(@"mm\:ss") : entry.Track.Info.Length.ToString(@"hh\:mm\:ss");

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
				$"{lastPlayedSongs[0].Track.Info.Title} {lastPlayedSongs[0].Track.Info.Author}" :
				$"{guild.MusicInstance.CurrentSong.Track.Info.Title} {guild.MusicInstance.CurrentSong.Track.Info.Author}";

			var searchListRequest = youtubeService.Search.List("snippet");
			searchListRequest.Q = searchQuery;
			searchListRequest.MaxResults = 1;
			searchListRequest.Type = "video";
			var searchListResponse = await searchListRequest.ExecuteAsync(MikuBot._canellationTokenSource.Token);

			if (lastPlayedSongs == null)
			{
				var currentSong = guild.MusicInstance.CurrentSong;
				guild.GetPlayingState(out var time1, out var time2);
				builder.AddField(new DiscordEmbedField($"{currentSong.Track.Info.Title} ({time1}/{time2})", $"[Video Link]({currentSong.Track.Info.Uri})\n" +
					$"[{currentSong.Track.Info.Author}](https://www.youtube.com/channel/{searchListResponse.Items[0].Snippet.ChannelId})"));
			}
			else
			{
				var currentSong = lastPlayedSongs[0];
				currentSong.GetPlayingState(out var time);
				builder.AddField(new DiscordEmbedField($"{currentSong.Track.Info.Title} ({time})", $"[Video Link]({currentSong.Track.Info.Uri})\n" +
					$"[{currentSong.Track.Info.Author}](https://www.youtube.com/channel/{searchListResponse.Items[0].Snippet.ChannelId})"));
			}

			var description = searchListResponse.Items[0].Snippet.Description.Length > 1000 ?
				string.Concat(searchListResponse.Items[0].Snippet.Description.AsSpan(0, 1000), "...") :
				searchListResponse.Items[0].Snippet.Description;

			builder.AddField(new DiscordEmbedField("Description", description));
			builder.WithImageUrl(searchListResponse.Items[0].Snippet.Thumbnails.High.Url);
			builder.AddField(new DiscordEmbedField("Playback options", guild.MusicInstance.GetPlaybackOptions()));
		}
		catch (Exception)
		{
			if (builder.Fields.Count != 1)
			{
				if (lastPlayedSongs == null)
				{
					var currentSong = guild.MusicInstance.CurrentSong;
					guild.GetPlayingState(out var time1, out var time2);
					builder.AddField(new DiscordEmbedField($"{currentSong.Track.Info.Title} ({time1}/{time2})", $"By {currentSong.Track.Info.Author}\n[Link]({currentSong.Track.Info.Uri})\nRequested by <@{currentSong.AddedBy}>"));
				}
				else
				{
					var currentSong = lastPlayedSongs[0];
					currentSong.GetPlayingState(out var time);
					builder.AddField(new DiscordEmbedField($"{currentSong.Track.Info.Title} ({time})", $"By {currentSong.Track.Info.Author}\n[Link]({currentSong.Track.Info.Uri})"));
				}
				builder.AddField(new DiscordEmbedField("Playback options", guild.MusicInstance.GetPlaybackOptions()));
			}
		}
		return builder;
	}

	public static async Task<(DiscordEmbedBuilder Embed, Stream? File, string? FileName)> GetUrlPlayingInformationAsync(this DiscordEmbedBuilder builder, DiscordClient client, Guild guild, List<Entry>? lastPlayedSongs)
	{
		Stream? img = null;
		var entry = lastPlayedSongs != null ? lastPlayedSongs[0] : guild.MusicInstance.CurrentSong;

		try
		{
			var uriSegments = entry.Track.Info.Uri.Segments;
			var filename = $"{uriSegments[^2]}.{uriSegments[^1]}";

			using (MemoryStream d = new(await client.RestClient.GetByteArrayAsync(entry.Track.Info.Uri, MikuBot._canellationTokenSource.Token)))
			using (var e = File.Create(filename))
			{
				d.Position = 0;
				await d.CopyToAsync(e, MikuBot._canellationTokenSource.Token);
			}

			var selector = new Selector();
			var extractor = selector.SelectAlbumArtExtractor(filename);
			img = extractor.Extract(filename);
			File.Delete(filename);
		}
		catch (Exception ex)
		{
			client.Logger.LogDebug("{msg}", ex.Message);
			client.Logger.LogDebug("{stack}", ex.StackTrace);
			img = null;
		}

		builder.AddField(new DiscordEmbedField($"{entry.Track.Info.Title} ({guild.GetDynamicPlayingState(lastPlayedSongs)})", $"By {entry.Track.Info.Author}\n[Link]({entry.Track.Info.Uri})\n{(lastPlayedSongs == null ? $"Requested by <@{guild.MusicInstance.CurrentSong.AddedBy}>" : "")}"));
		builder.AddField(new DiscordEmbedField("Playback options", guild.MusicInstance.GetPlaybackOptions()));

		var attachmentName = string.Empty;

		if (img != null)
		{
			var extension = MimeGuesser.GuessExtension(img);
			attachmentName = $"{entry.Track.Info.Uri.Segments[^2]}.{extension}";
			builder.WithImageUrl($"attachment://{attachmentName}");
		}

		return (builder, img, attachmentName);
	}

	public static DiscordEmbedBuilder GetOtherPlayingInformationAsync(this DiscordEmbedBuilder builder, Guild guild, List<Entry>? lastPlayedSongs = null)
	{
		var entry = lastPlayedSongs != null ? lastPlayedSongs[0] : guild.MusicInstance.CurrentSong;
		builder.AddField(new DiscordEmbedField($"{entry.Track.Info.Title} ({guild.GetDynamicPlayingState(lastPlayedSongs)})", $"By {entry.Track.Info.Author}\n[Link]({entry.Track.Info.Uri})\n{(lastPlayedSongs == null ? $"Requested by <@{guild.MusicInstance.CurrentSong.AddedBy}>" : "")}"));
		builder.AddField(new DiscordEmbedField("Playback options", guild.MusicInstance.GetPlaybackOptions()));
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
		var entry = lastPlayedSongs != null ? lastPlayedSongs[0] : guild.MusicInstance.CurrentSong;

		if (entry == null)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("I'm not playing anything right now"));
			return;
		}

		DiscordEmbedBuilder lBuilder;

		if (entry.Track.Info.Uri.ToString().Contains("youtu"))
		{
			lBuilder = await builder.GetYoutubePlayingInformationAsync(guild, lastPlayedSongs);
		}
		else if (!entry.Track.Info.Uri.ToString().StartsWith($"https://media.{DiscordDomain.GetDomain(CoreDomain.DiscordAppMediaProxy).Domain}/attachments/") && !entry.Track.Info.Uri.ToString().StartsWith($"{DiscordDomain.GetDomain(CoreDomain.DiscordCdn).Url}/attachments/"))
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
