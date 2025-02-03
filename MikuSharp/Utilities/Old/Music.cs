/*using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using AlbumArtExtraction;

using DisCatSharp;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;

using Google.Apis.YouTube.v3;

using HeyRed.Mime;

using Microsoft.Extensions.Logging;

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
        string opts = null;

        switch (instance.RepeatMode)
        {
            case RepeatMode.On:
                opts += DiscordEmoji.FromUnicode("🔂");
                break;
            case RepeatMode.All:
                opts += DiscordEmoji.FromUnicode("🔁");
                break;
        }

        if (instance.ShuffleMode == ShuffleMode.On)
            opts += DiscordEmoji.FromUnicode("🔀");

        return opts ?? "None";
    }

    public static async Task ConditionalConnect(this Guild guild, InteractionContext ctx)
    {
        if (!guild.MusicInstance?.GuildConnection?.IsConnected != null && !guild.MusicInstance.GuildConnection.IsConnected)
            return;

        await guild.MusicInstance.ConnectToChannel(ctx.Member.VoiceState.Channel);
    }

    public static async Task<bool> IsNotConnected(this Guild guild, InteractionContext ctx)
    {
        if (guild.MusicInstance == null! || guild.MusicInstance?.GuildConnection?.IsConnected == false)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error: Not connected!"));
            return true;
        }

        return false;
    }

    public static string SearchUrlOrAttachment(this DiscordAttachment? attachment, string? searchOrUrl)
        => !string.IsNullOrEmpty(searchOrUrl)
            ? searchOrUrl
            : attachment?.ProxyUrl;

    public static void GetPlayingState(this Guild guild, out string time1, out string time2)
    {
        switch (guild.MusicInstance.CurrentSong.Track.Info.Length.Hours)
        {
            case < 1:
                time1 = guild.MusicInstance.GuildConnection.TrackPosition.ToString(@"mm\:ss");
                time2 = guild.MusicInstance.CurrentSong.Track.Info.Length.ToString(@"mm\:ss");
                break;
            default:
                time1 = guild.MusicInstance.GuildConnection.TrackPosition.ToString(@"hh\:mm\:ss");
                time2 = guild.MusicInstance.CurrentSong.Track.Info.Length.ToString(@"hh\:mm\:ss");
                break;
        }
    }

    public static void GetPlayingState(this Entry entry, out string time)
    {
        time = entry.Track.Info.Length.Hours switch
        {
            < 1 => entry.Track.Info.Length.ToString(@"mm\:ss"),
            _ => entry.Track.Info.Length.ToString(@"hh\:mm\:ss")
        };
    }

    public static async Task<DiscordEmbedBuilder> GetYoutubePlayingInformationAsync(this DiscordEmbedBuilder builder, Guild guild, List<Entry>? lastPlayedSongs = null)
    {
        try
        {
            var youtubeService = new YouTubeService(new()
            {
                ApiKey = MikuBot.Config.YoutubeApiToken,
                ApplicationName = typeof(MikuBot).ToString()
            });
            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = lastPlayedSongs != null
                ? lastPlayedSongs[0].Track.Info.Title + " " + lastPlayedSongs[0].Track.Info.Author
                : guild.MusicInstance.CurrentSong.Track.Info.Title + " " + guild.MusicInstance.CurrentSong.Track.Info.Author;
            searchListRequest.MaxResults = 1;
            searchListRequest.Type = "video";
            var searchListResponse = await searchListRequest.ExecuteAsync();

            if (lastPlayedSongs == null)
            {
                guild.GetPlayingState(out var time1, out var time2);
                builder.AddField(new($"{guild.MusicInstance.CurrentSong.Track.Info.Title} ({time1}/{time2})",
                    $"[Video Link]({guild.MusicInstance.CurrentSong.Track.Info.Uri})\n" + $"[{guild.MusicInstance.CurrentSong.Track.Info.Author}](https://www.youtube.com/channel/" + searchListResponse.Items[0].Snippet.ChannelId + ")"));
            }
            else
            {
                lastPlayedSongs[0].GetPlayingState(out var time);
                builder.AddField(new($"{lastPlayedSongs[0].Track.Info.Title} ({time})",
                    $"[Video Link]({lastPlayedSongs[0].Track.Info.Uri})\n" + $"[{lastPlayedSongs[0].Track.Info.Author}](https://www.youtube.com/channel/" + searchListResponse.Items[0].Snippet.ChannelId + ")"));
            }

            builder.AddField(new("Description", searchListResponse.Items[0].Snippet.Description.Length > 1000
                ? string.Concat(searchListResponse.Items[0].Snippet.Description.AsSpan(0, 1000), "...")
                : searchListResponse.Items[0].Snippet.Description
            ));
            builder.WithImageUrl(searchListResponse.Items[0].Snippet.Thumbnails.High.Url);
            builder.AddField(new("Playback options", guild.MusicInstance.GetPlaybackOptions()));
        }
        catch (Exception)
        {
            if (builder.Fields.Count != 1)
            {
                if (lastPlayedSongs == null)
                    builder.AddField(new($"{guild.MusicInstance.CurrentSong.Track.Info.Title} ({guild.MusicInstance.CurrentSong.Track.Info.Length})",
                        $"By {guild.MusicInstance.CurrentSong.Track.Info.Author}\n[Link]({guild.MusicInstance.CurrentSong.Track.Info.Uri})\nRequested by <@{guild.MusicInstance.CurrentSong.AddedBy}>"));
                else
                    builder.AddField(new($"{lastPlayedSongs[0].Track.Info.Title} ({lastPlayedSongs[0].Track.Info.Length})", $"By {lastPlayedSongs[0].Track.Info.Author}\n[Link]({lastPlayedSongs[0].Track.Info.Uri})"));
                builder.AddField(new("Playback options", guild.MusicInstance.GetPlaybackOptions()));
            }
        }

        return builder;
    }

    public static async Task<DiscordEmbedBuilder> GetUrlPlayingInformationAsync(this DiscordEmbedBuilder builder, DiscordClient client, Guild guild, List<Entry>? lastPlayedSongs)
    {
        Stream? img = null;
        var entry = lastPlayedSongs != null
            ? lastPlayedSongs[0]
            : guild.MusicInstance.CurrentSong;

        try
        {
            MemoryStream d = new(await client.RestClient.GetByteArrayAsync(entry.Track.Info.Uri))
            {
                Position = 0
            };
            var e = File.Create($@"{entry.Track.Info.Uri.ToString().Split('/')[^2]}.{entry.Track.Info.Uri.ToString().Split('/').Last()}");
            await d.CopyToAsync(e);
            e.Close();
            var selector = new Selector();
            var extractor = selector.SelectAlbumArtExtractor($@"{entry.Track.Info.Uri.ToString().Split('/')[^2]}.{entry.Track.Info.Uri.ToString().Split('/').Last()}");
            img = extractor.Extract($@"{entry.Track.Info.Uri.ToString().Split('/')[^2]}.{entry.Track.Info.Uri.ToString().Split('/').Last()}");
        }
        catch (Exception ex)
        {
            client.Logger.LogDebug(ex.Message);
            client.Logger.LogDebug(ex.StackTrace);
            img = null;
            File.Delete($@"{entry.Track.Info.Uri.ToString().Split('/')[^2]}.{entry.Track.Info.Uri.ToString().Split('/').Last()}");
        }

        builder.AddField(new($"{entry.Track.Info.Title} ({guild.GetDynamicPlayingState(lastPlayedSongs)})",
            $"By {entry.Track.Info.Author}\n[Link]({entry.Track.Info.Uri})\n{(lastPlayedSongs == null ? $"Requested by <@{guild.MusicInstance.CurrentSong.AddedBy}>" : "")}"));
        builder.AddField(new("Playback options", guild.MusicInstance.GetPlaybackOptions()));
        if (img != null)
            builder.WithImageUrl($"attachment://{entry.Track.Info.Uri.ToString().Split('/')[^2]}.{MimeGuesser.GuessExtension(img)}");
        return builder;
    }

    public static DiscordEmbedBuilder GetOtherPlayingInformationAsync(this DiscordEmbedBuilder builder, Guild guild, List<Entry>? lastPlayedSongs = null)
    {
        var entry = lastPlayedSongs != null
            ? lastPlayedSongs[0]
            : guild.MusicInstance.CurrentSong;
        builder.AddField(new($"{entry.Track.Info.Title} ({guild.GetDynamicPlayingState(lastPlayedSongs)})",
            $"By {entry.Track.Info.Author}\n[Link]({entry.Track.Info.Uri})\n{(lastPlayedSongs == null ? $"Requested by <@{guild.MusicInstance.CurrentSong.AddedBy}>" : "")}"));
        builder.AddField(new("Playback options", guild.MusicInstance.GetPlaybackOptions()));
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
        var entry = lastPlayedSongs != null
            ? lastPlayedSongs[0]
            : guild.MusicInstance.CurrentSong;

        if (entry == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("I don't play anything right now"));
            return;
        }

        builder = entry.Track.Info.Uri.ToString().Contains("youtu")
            ? await builder.GetYoutubePlayingInformationAsync(guild)
            : entry.Track.Info.Uri.ToString().StartsWith("https://media.discordapp.net/attachments/", StringComparison.Ordinal) || entry.Track.Info.Uri.ToString().StartsWith("https://cdn.discordapp.com/attachments/", StringComparison.Ordinal)
                ? await builder.GetUrlPlayingInformationAsync(ctx.Client, guild, lastPlayedSongs)
                : builder.GetOtherPlayingInformationAsync(guild, lastPlayedSongs);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(builder.Build()));
    }
}
*/


