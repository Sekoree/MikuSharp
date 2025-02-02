/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Interactivity.Extensions;
using DisCatSharp.Lavalink;
using DisCatSharp.Lavalink.Entities;
using DisCatSharp.Lavalink.Enums;

using FluentFTP;

using Microsoft.Extensions.Logging;

using MikuSharp.Enums;
using MikuSharp.Events;
using MikuSharp.Utilities;

namespace MikuSharp.Entities;

public class MusicInstance
{
    public MusicInstance(LavalinkSession node, int shard)
    {
        this.ShardId = shard;
        this.Session = node;
        this.UsedChannel = null;
        this.Playstate = Playstate.NotPlaying;
        this.RepeatMode = RepeatMode.Off;
        this.RepeatAllPos = 0;
        this.ShuffleMode = ShuffleMode.Off;
    }

    public int ShardId { get; set; }
    public DiscordChannel UsedChannel { get; set; }
    public DiscordChannel VoiceChannel { get; set; }
    public Playstate Playstate { get; set; }
    public RepeatMode RepeatMode { get; set; }
    public int RepeatAllPos { get; set; }
    public ShuffleMode ShuffleMode { get; set; }
    public DateTime AloneTime { get; set; }
    public CancellationTokenSource AloneCts { get; set; }
    public LavalinkSession Session { get; set; }
    public LavalinkGuildPlayer GuildConnection { get; set; }
    public QueueEntry CurrentSong { get; set; }
    public QueueEntry LastSong { get; set; }

    public async Task<LavalinkGuildPlayer> ConnectToChannel(DiscordChannel channel)
    {
        switch (channel.Type)
        {
            case ChannelType.Voice:
            {
                this.GuildConnection = await this.Session.ConnectAsync(channel);
                this.VoiceChannel = channel;
                return this.GuildConnection;
            }
            default:
                return null;
        }
    }

    public async Task<TrackResult> QueueSong(string n, InteractionContext ctx, int pos = -1)
    {
        var queue = await Database.GetQueueAsync(ctx.Guild);
        var inter = ctx.Client.GetInteractivity();

        if (n.ToLower().StartsWith("http://nicovideo.jp", StringComparison.Ordinal) ||
            n.ToLower().StartsWith("http://sp.nicovideo.jp", StringComparison.Ordinal) ||
            n.ToLower().StartsWith("https://nicovideo.jp", StringComparison.Ordinal) ||
            n.ToLower().StartsWith("https://sp.nicovideo.jp", StringComparison.Ordinal) ||
            n.ToLower().StartsWith("http://www.nicovideo.jp", StringComparison.Ordinal) ||
            n.ToLower().StartsWith("https://www.nicovideo.jp", StringComparison.Ordinal))
        {
            var msg = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Processing NND Video...").AsEphemeral());
            var split = n.Split("/".ToCharArray());
            var nndId = split.First(x => x.StartsWith("sm", StringComparison.Ordinal) || x.StartsWith("nm", StringComparison.Ordinal)).Split("?")[0];
            FtpClient client = new(MikuBot.Config.NndConfig.FtpConfig.Hostname, new NetworkCredential(MikuBot.Config.NndConfig.FtpConfig.User, MikuBot.Config.NndConfig.FtpConfig.Password));
            client.Connect();

            if (!client.FileExists($"{nndId}.mp3"))
            {
                await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Preparing download..."));
                var ex = await ctx.GetNndAsync(n, nndId, msg.Id);

                if (ex == null)
                {
                    await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Please try again or verify the link"));
                    return null;
                }

                await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Uploading"));
                client.UploadStream(ex, $"{nndId}.mp3", FtpRemoteExists.Skip, true);
            }

            var track = (await this.Session.Rest.LoadTracksAsync($"https://nnd.meek.moe/new/{nndId}.mp3")).GetResultAs<LavalinkPlaylist>();
            if (pos == -1)
                await Database.AddToQueue(ctx.Guild, ctx.Member.Id, track.Tracks.First().Encoded);
            else
                await Database.InsertToQueue(ctx.Guild, ctx.Member.Id, track.Tracks.First().Encoded, pos);
            if (this.GuildConnection.IsConnected && this.Playstate is Playstate.NotPlaying or Playstate.Stopped)
                await this.PlaySong();
            return new(track.Info, track.Tracks.First());
        }

        if (n.ToLower().StartsWith("https://www.bilibili.com", StringComparison.Ordinal) || n.ToLower().StartsWith("http://www.bilibili.com", StringComparison.Ordinal))
        {
            var msg = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Processing Bilibili Video...").AsEphemeral());
            n = n.Replace("https://www.bilibili.com/", "");
            n = n.Replace("http://www.bilibili.com/", "");
            var split = n.Split("/".ToCharArray());

            if (!split.Contains("video"))
            {
                await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Failure"));
                return null;
            }

            var nndId = split[1].Split("?")[0];
            FtpClient client = new(MikuBot.Config.NndConfig.FtpConfig.Hostname, new NetworkCredential(MikuBot.Config.NndConfig.FtpConfig.User, MikuBot.Config.NndConfig.FtpConfig.Password));
            client.Connect();

            if (!client.FileExists($"{nndId}.mp3"))
            {
                await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Preparing download..."));
                var ex = await ctx.GetBilibiliAsync(nndId, msg.Id);

                if (ex == null)
                {
                    await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Please try again or verify the link"));
                    return null;
                }

                await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Uploading..."));
                client.UploadStream(ex, $"{nndId}.mp3", FtpRemoteExists.Skip, true);
            }

            var track = (await this.Session.ConnectedPlayers[ctx.GuildId.Value].LoadTracksAsync($"https://nnd.meek.moe/new/{nndId}.mp3")).GetResultAs<LavalinkPlaylist>();
            if (pos == -1)
                await Database.AddToQueue(ctx.Guild, ctx.Member.Id, track.Tracks.First().Encoded);
            else
                await Database.InsertToQueue(ctx.Guild, ctx.Member.Id, track.Tracks.First().Encoded, pos);
            if (this.GuildConnection.IsConnected && this.Playstate is Playstate.NotPlaying or Playstate.Stopped)
                await this.PlaySong();
            return new(track.Info, track.Tracks.First());
        }

        if (n.StartsWith("http://", StringComparison.Ordinal) | n.StartsWith("https://", StringComparison.Ordinal))
            try
            {
                var s = await this.Session.ConnectedPlayers[ctx.GuildId.Value].LoadTracksAsync(n);

                switch (s.LoadType)
                {
                    case LavalinkLoadResultType.Error:
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().AddEmbed(new DiscordEmbedBuilder().WithTitle("Failed to load")
                            .WithDescription("Loading this song/playlist failed, please try again, reasons could be:\n" + "> Playlist is set to private or unlisted\n" + "> The song is unavailable/deleted").Build()));
                        return null;
                    }
                        ;
                    case LavalinkLoadResultType.Empty:
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral()
                            .AddEmbed(new DiscordEmbedBuilder().WithTitle("Failed to load").WithDescription("No song/playlist was found with this URL, please try again/a different one").Build()));
                        return null;
                    }
                        ;
                    case LavalinkLoadResultType.Playlist:
                    {
                        var pl = s.GetResultAs<LavalinkPlaylist>();

                        if (pl.Info.SelectedTrack == -1)
                        {
                            List<DiscordButtonComponent> buttons = [new(ButtonStyle.Success, "yes", "Add entire playlist"), new(ButtonStyle.Primary, "no", "Don't add")];
                            var msg = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().WithContent("Playlist link detected!").AddEmbed(new DiscordEmbedBuilder()
                                .WithDescription("Choose how to handle the playlist link")
                                .WithAuthor($"Requested by {ctx.Member.UsernameWithGlobalName} || Timeout 30 seconds", iconUrl: ctx.Member.AvatarUrl)
                                .Build()).AddComponents(buttons));
                            var resp = await inter.WaitForButtonAsync(msg, ctx.User, TimeSpan.FromSeconds(30));

                            if (resp.TimedOut)
                            {
                                buttons.ForEach(x => x.Disable());
                                await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(buttons).WithContent("Timed out!"));
                                return null;
                            }

                            if (resp.Result.Id == "yes")
                            {
                                await resp.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                                buttons.ForEach(x => x.Disable());
                                await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(buttons).WithContent("Adding entire playlist"));
                                await Database.AddToQueue(ctx.Guild, ctx.Member.Id, pl.Tracks);
                                if (this.GuildConnection.IsConnected && this.Playstate is Playstate.NotPlaying or Playstate.Stopped)
                                    await this.PlaySong();
                                return new(pl.Info, pl.Tracks);
                            }

                            await resp.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                            buttons.ForEach(x => x.Disable());
                            await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(buttons).WithContent("Canceled!"));
                            return null;
                        }
                        else
                        {
                            List<DiscordButtonComponent> buttons = [new(ButtonStyle.Primary, "yes", "Add only referred song"), new(ButtonStyle.Success, "yes", "Add the entire playlist"), new(ButtonStyle.Danger, "no", "Cancel")];
                            var msg = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().AddEmbed(new DiscordEmbedBuilder()
                                .WithTitle("Link with Playlist detected!")
                                .WithDescription("Please choose how to handle the playlist link")
                                .WithAuthor($"Requested by {ctx.Member.UsernameWithGlobalName} || Timeout 30 seconds", iconUrl: ctx.Member.AvatarUrl)
                                .Build()).AddComponents(buttons));
                            var resp = await inter.WaitForButtonAsync(msg, ctx.User, TimeSpan.FromSeconds(30));

                            if (resp.TimedOut)
                            {
                                buttons.ForEach(x => x.Disable());
                                await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(buttons).WithContent("Timed out!"));
                                return null;
                            }

                            if (resp.Result.Id == "yes")
                            {
                                await resp.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                                buttons.ForEach(x => x.Disable());
                                await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(buttons).WithContent($"Adding single song: {pl.Tracks.ElementAt(pl.Info.SelectedTrack).Info.Title}"));
                                if (pos == -1)
                                    await Database.AddToQueue(ctx.Guild, ctx.Member.Id, pl.Tracks.ElementAt(pl.Info.SelectedTrack).Encoded);
                                else
                                    await Database.InsertToQueue(ctx.Guild, ctx.Member.Id, pl.Tracks.ElementAt(pl.Info.SelectedTrack).Encoded, pos);
                                if (this.GuildConnection.IsConnected && this.Playstate is Playstate.NotPlaying or Playstate.Stopped)
                                    await this.PlaySong();
                                return new(pl.Info, pl.Tracks.ElementAt(pl.Info.SelectedTrack));
                            }

                            if (resp.Result.Id == "all")
                            {
                                await resp.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                                buttons.ForEach(x => x.Disable());
                                await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(buttons).WithContent($"Adding entire playlist: {pl.Info.Name}"));

                                if (pos == -1)
                                    await Database.AddToQueue(ctx.Guild, ctx.Member.Id, pl.Tracks);
                                else
                                {
                                    pl.Tracks.Reverse();
                                    await Database.InsertToQueue(ctx.Guild, ctx.Member.Id, pl.Tracks, pos);
                                }

                                if (this.GuildConnection.IsConnected && this.Playstate is Playstate.NotPlaying or Playstate.Stopped) await this.PlaySong();
                                return new(pl.Info, pl.Tracks);
                            }

                            await resp.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                            buttons.ForEach(x => x.Disable());
                            await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(buttons).WithContent("Canceled!"));
                            return null;
                        }
                    }
                        ;
                    default:
                    {
                        var p = s.GetResultAs<LavalinkTrack>();
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().WithContent($"Playing single song: {p.Info.Title}"));
                        if (pos == -1)
                            await Database.AddToQueue(ctx.Guild, ctx.Member.Id, p.Encoded);
                        else
                            await Database.InsertToQueue(ctx.Guild, ctx.Member.Id, p.Encoded, pos);
                        if (this.GuildConnection.IsConnected && this.Playstate is Playstate.NotPlaying or Playstate.Stopped)
                            await this.PlaySong();
                        return new(null, p);
                    }
                        ;
                }
            }
            catch (Exception ex)
            {
                ctx.Client.Logger.LogError("{ex}", ex.Message);
                ctx.Client.Logger.LogError("{ex}", ex.StackTrace);
                return null;
            }

        var type = LavalinkSearchType.Youtube;

        if (n.StartsWith("ytsearch:", StringComparison.Ordinal))
        {
            n = n.Replace("ytsearch:", "");
            type = LavalinkSearchType.Youtube;
        }
        else if (n.StartsWith("scsearch:", StringComparison.Ordinal))
        {
            n = n.Replace("ytsearch:", "");
            type = LavalinkSearchType.SoundCloud;
        }
        else if (n.StartsWith("spsearch:", StringComparison.Ordinal))
        {
            n = n.Replace("spsearch:", "");
            type = LavalinkSearchType.Spotify;
        }
        else if (n.StartsWith("amsearch:", StringComparison.Ordinal))
        {
            n = n.Replace("amsearch:", "");
            type = LavalinkSearchType.AppleMusic;
        }

        var trackLoadingResult = await this.Session.ConnectedPlayers[ctx.GuildId!.Value].LoadTracksAsync(type, n);

        switch (trackLoadingResult.LoadType)
        {
            case LavalinkLoadResultType.Error:
            {
                ctx.Client.Logger.LogDebug("Load failed");
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().AddEmbed(new DiscordEmbedBuilder().WithTitle("Failed to load")
                    .WithDescription("Loading this song/playlist failed, please try again, reason could be:\n" + "> The song is unavailable/deleted").Build()));
                return null;
            }
                ;
            case LavalinkLoadResultType.Empty:
            {
                ctx.Client.Logger.LogDebug("No matches");
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().AddEmbed(new DiscordEmbedBuilder().WithTitle("Failed to load").WithDescription("No song was found, please try again").Build()));
                return null;
            }
                ;
            default:
            {
                var s = trackLoadingResult.GetResultAs<List<LavalinkTrack>>();
                ctx.Client.Logger.LogDebug("Found something");
                var leng = s.Count;
                if (leng > 5) leng = 5;
                List<DiscordStringSelectComponentOption> selectOptions = new(leng);
                var em = new DiscordEmbedBuilder()
                    .WithTitle("Results!")
                    .WithDescription("Please select a track:\n")
                    .WithAuthor($"Requested by {ctx.Member.UsernameWithGlobalName} || Timeout 30 seconds", iconUrl: ctx.Member.AvatarUrl);

                for (var i = 0; i < leng; i++)
                {
                    em.AddField(new($"{i + 1}.{s.ElementAt(i).Info.Title} [{s.ElementAt(i).Info.Length}]", $"by {s.ElementAt(i).Info.Author} [Link]({s.ElementAt(i).Info.Uri})"));
                    selectOptions.Add(new(s.ElementAt(i).Info.Title, i.ToString(), $"by {s.ElementAt(i).Info.Author}. Length: {s.ElementAt(i).Info.Length}"));
                }

                DiscordStringSelectComponent select = new("Select song to play", selectOptions, minOptions: 1, maxOptions: 1);
                var msg = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().AddEmbed(em.Build()).AddComponents(select));
                var resp = await inter.WaitForSelectAsync(msg, ctx.User, select.CustomId, ComponentType.StringSelect, TimeSpan.FromSeconds(30));

                if (resp.TimedOut)
                {
                    select.Disable();
                    await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(select).WithContent("Timed out!"));
                    return null;
                }

                await resp.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                var trackSelect = Convert.ToInt32(resp.Result.Values.First());
                var track = s.ElementAt(trackSelect);
                select.Disable();
                await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(select).WithContent($"Chose {track.Info.Title}"));
                if (pos == -1)
                    await Database.AddToQueue(ctx.Guild, ctx.Member.Id, track.Encoded);
                else
                    await Database.InsertToQueue(ctx.Guild, ctx.Member.Id, track.Encoded, pos);
                if (this.GuildConnection.IsConnected && this.Playstate is Playstate.NotPlaying or Playstate.Stopped)
                    await this.PlaySong();
                return new(null, track);
            }
                ;
        }
    }

    public async Task<QueueEntry> PlaySong()
    {
        var queue = await Database.GetQueueAsync(this.VoiceChannel.Guild);
        var cur = this.LastSong;
        if (queue.Count != 1 && this.RepeatMode == RepeatMode.All)
            this.RepeatAllPos++;
        if (this.RepeatAllPos >= queue.Count)
            this.RepeatAllPos = 0;
        this.CurrentSong = this.ShuffleMode == ShuffleMode.Off
            ? queue[0]
            : queue[new Random().Next(0, queue.Count)];

        switch (this.RepeatMode)
        {
            case RepeatMode.All:
                this.CurrentSong = queue[this.RepeatAllPos];
                break;
            case RepeatMode.On:
                this.CurrentSong = cur;
                break;
        }

        MikuBot.ShardedClient.Logger.LogDebug(this.CurrentSong?.Track.Encoded);
        this.GuildConnection.TrackEnded += Lavalink.LavalinkTrackFinish;
        this.Playstate = Playstate.Playing;
        _ = Task.Run(async () => await this.GuildConnection.PlayAsync(this.CurrentSong.Track));
        return this.CurrentSong;
    }
}

//     B/S(｀・ω・´) ❤️ (´ω｀)U/C
*/