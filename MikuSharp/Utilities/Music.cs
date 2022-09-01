using AlbumArtExtraction;

using DisCatSharp;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;

using Google.Apis.Services;
using Google.Apis.YouTube.v3;

using HeyRed.Mime;

using Microsoft.Extensions.Logging;

using MikuSharp.Entities;
using MikuSharp.Enums;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
		var opts = "";
		if (instance.repeatMode == RepeatMode.On)
			opts += DiscordEmoji.FromUnicode("🔂");
		if (instance.repeatMode == RepeatMode.All)
			opts += DiscordEmoji.FromUnicode("🔁");
		if (instance.shuffleMode == ShuffleMode.On)
			opts += DiscordEmoji.FromUnicode("🔀");
		if (opts == "")
			return "None";
		return opts;
	}

	public static async Task ConditionalConnect(this Guild guild, InteractionContext ctx)
	{
		if (!guild.musicInstance.guildConnection?.IsConnected != null && !guild.musicInstance.guildConnection.IsConnected)
			return;
		await guild.musicInstance.ConnectToChannel(ctx.Member.VoiceState.Channel);
	}

	public static async Task<bool> IsNotConnected(this Guild guild, InteractionContext ctx)
	{
		if (guild.musicInstance == null || guild.musicInstance?.guildConnection?.IsConnected == false)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error: Not connected!"));
			return true;
		}
		else
			return false;
	}

	public static string SearchUrlOrAttachment(this DiscordAttachment? attachment, string? search_or_url)
		=> !string.IsNullOrEmpty(search_or_url) ? search_or_url : attachment?.ProxyUrl;

	public static void GetPlayingState(this Guild guild, out string time1, out string time2)
	{
		switch (guild.musicInstance.currentSong.track.Length.Hours)
		{
			case < 1:
				time1 = guild.musicInstance.guildConnection.CurrentState.PlaybackPosition.ToString(@"mm\:ss");
				time2 = guild.musicInstance.currentSong.track.Length.ToString(@"mm\:ss");
				break;
			default:
				time1 = guild.musicInstance.guildConnection.CurrentState.PlaybackPosition.ToString(@"hh\:mm\:ss");
				time2 = guild.musicInstance.currentSong.track.Length.ToString(@"hh\:mm\:ss");
				break;
		}
	}

	public static void GetPlayingState(this Entry entry, out string time)
	{
		time = entry.track.Length.Hours switch
		{
			< 1 => entry.track.Length.ToString(@"mm\:ss"),
			_ => entry.track.Length.ToString(@"hh\:mm\:ss"),
		};
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
			var searchListRequest = youtubeService.Search.List("snippet");
			searchListRequest.Q = lastPlayedSongs != null ? lastPlayedSongs[0].track.Title + " " + lastPlayedSongs[0].track.Author : guild.musicInstance.currentSong.track.Title + " " + guild.musicInstance.currentSong.track.Author;
			searchListRequest.MaxResults = 1;
			searchListRequest.Type = "video";
			var searchListResponse = await searchListRequest.ExecuteAsync();

			if (lastPlayedSongs == null)
			{
				guild.GetPlayingState(out var time1, out var time2);
				builder.AddField(new DiscordEmbedField($"{guild.musicInstance.currentSong.track.Title} ({time1}/{time2})", $"[Video Link]({guild.musicInstance.currentSong.track.Uri})\n" +
			$"[{guild.musicInstance.currentSong.track.Author}](https://www.youtube.com/channel/" + searchListResponse.Items[0].Snippet.ChannelId + ")"));
			}
			else
			{
				lastPlayedSongs[0].GetPlayingState(out var time);
				builder.AddField(new DiscordEmbedField($"{lastPlayedSongs[0].track.Title} ({time})", $"[Video Link]({lastPlayedSongs[0].track.Uri})\n" +
						$"[{lastPlayedSongs[0].track.Author}](https://www.youtube.com/channel/" + searchListResponse.Items[0].Snippet.ChannelId + ")"));
			}
			builder.AddField(new DiscordEmbedField("Description", searchListResponse.Items[0].Snippet.Description.Length > 1000 ?
				string.Concat(searchListResponse.Items[0].Snippet.Description.AsSpan(0, 1000), "...") :
				searchListResponse.Items[0].Snippet.Description
			));
			builder.WithImageUrl(searchListResponse.Items[0].Snippet.Thumbnails.High.Url);
			builder.AddField(new DiscordEmbedField("Playback options", guild.musicInstance.GetPlaybackOptions()));
		}
		catch(Exception)
		{
			if (builder.Fields.Count != 1)
			{
				if (lastPlayedSongs == null)
					builder.AddField(new DiscordEmbedField($"{guild.musicInstance.currentSong.track.Title} ({guild.musicInstance.currentSong.track.Length})", $"By {guild.musicInstance.currentSong.track.Author}\n[Link]({guild.musicInstance.currentSong.track.Uri})\nRequested by <@{guild.musicInstance.currentSong.addedBy}>"));
				else
					builder.AddField(new DiscordEmbedField($"{lastPlayedSongs[0].track.Title} ({lastPlayedSongs[0].track.Length})", $"By {lastPlayedSongs[0].track.Author}\n[Link]({lastPlayedSongs[0].track.Uri})"));
				builder.AddField(new DiscordEmbedField("Playback options", guild.musicInstance.GetPlaybackOptions()));
			}
		}
		return builder;
	}

	public static async Task<DiscordEmbedBuilder> GetUrlPlayingInformationAsync(this DiscordEmbedBuilder builder, DiscordClient client, Guild guild, List<Entry>? lastPlayedSongs)
	{
		Stream? img = null;
		Entry entry = lastPlayedSongs != null ? lastPlayedSongs[0] : guild.musicInstance.currentSong;
		try
		{
			MemoryStream d = new(await client.RestClient.GetByteArrayAsync(entry.track.Uri))
			{
				Position = 0
			};
			FileStream e = File.Create($@"{entry.track.Uri.ToString().Split('/')[^2]}.{entry.track.Uri.ToString().Split('/').Last()}");
			await d.CopyToAsync(e);
			e.Close();
			var selector = new Selector();
			var extractor = selector.SelectAlbumArtExtractor($@"{entry.track.Uri.ToString().Split('/')[^2]}.{entry.track.Uri.ToString().Split('/').Last()}");
			img = extractor.Extract($@"{entry.track.Uri.ToString().Split('/')[^2]}.{entry.track.Uri.ToString().Split('/').Last()}");
		}
		catch (Exception ex)
		{
			client.Logger.LogDebug(ex.Message);
			client.Logger.LogDebug(ex.StackTrace);
			img = null;
			File.Delete($@"{entry.track.Uri.ToString().Split('/')[^2]}.{entry.track.Uri.ToString().Split('/').Last()}");
		}
		builder.AddField(new DiscordEmbedField($"{entry.track.Title} ({guild.GetDynamicPlayingState(lastPlayedSongs)})", $"By {entry.track.Author}\n[Link]({entry.track.Uri})\n{(lastPlayedSongs == null ? $"Requested by <@{guild.musicInstance.currentSong.addedBy}>" : "")}"));
		builder.AddField(new DiscordEmbedField("Playback options", guild.musicInstance.GetPlaybackOptions()));
		if (img != null)
			builder.WithImageUrl($"attachment://{entry.track.Uri.ToString().Split('/')[^2]}.{MimeGuesser.GuessExtension(img)}");
		return builder;
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
		switch (lastPlayedSongs)
		{
			case null:
				{
					guild.GetPlayingState(out var time1, out var time2);
					return $"{time1}/{time2}";
				}

			default:
				{
					lastPlayedSongs[0].GetPlayingState(out var time);
					return $"{time}";
				}
		}
	}

	public static async Task SendPlayingInformationAsync(this InteractionContext ctx, DiscordEmbedBuilder builder, Guild guild, List<Entry>? lastPlayedSongs = null)
	{
		Entry entry = lastPlayedSongs != null ? lastPlayedSongs[0] : guild.musicInstance.currentSong;
		if (entry == null)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("I don't play anything right now"));
			return;
		}
		builder = entry.track.Uri.ToString().Contains("youtu")
			? await builder.GetYoutubePlayingInformationAsync(guild)
			: entry.track.Uri.ToString().StartsWith("https://media.discordapp.net/attachments/") || entry.track.Uri.ToString().StartsWith("https://cdn.discordapp.com/attachments/")
				? await builder.GetUrlPlayingInformationAsync(ctx.Client, guild, lastPlayedSongs)
				: builder.GetOtherPlayingInformationAsync(guild, lastPlayedSongs);

		await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(builder.Build()));
	}
}
