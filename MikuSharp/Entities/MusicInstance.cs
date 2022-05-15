using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.Entities;
using DisCatSharp.Interactivity.Extensions;
using DisCatSharp.Lavalink;

using FluentFTP;

using MikuSharp.Enums;
using MikuSharp.Events;
using MikuSharp.Utilities;

using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MikuSharp.Entities
{
    public class MusicInstance
    {
        public int shardID { get; set; }
        public DiscordChannel usedChannel { get; set; }
        public DiscordChannel voiceChannel { get; set; }
        public Playstate playstate { get; set; }
        public RepeatMode repeatMode { get; set; }
        public int repeatAllPos { get; set; }
        public ShuffleMode shuffleMode { get; set; }
        public DateTime aloneTime { get; set; }
        public CancellationTokenSource aloneCTS { get; set; }
        public LavalinkNodeConnection nodeConnection { get; set; }
        public LavalinkGuildConnection guildConnection { get; set; }
        public QueueEntry currentSong { get; set; }
        public QueueEntry lastSong { get; set; }

        public MusicInstance(LavalinkNodeConnection node, int shard)
        {
            shardID = shard;
            nodeConnection = node;
            usedChannel = null;
            playstate = Playstate.NotPlaying;
            repeatMode = RepeatMode.Off;
            repeatAllPos = 0;
            shuffleMode = ShuffleMode.Off;
        }

        public async Task<LavalinkGuildConnection> ConnectToChannel(DiscordChannel channel)
        {
            switch (channel.Type)
            {
                case ChannelType.Voice:
                    {
                        guildConnection = await nodeConnection.ConnectAsync(channel);
                        voiceChannel = channel;
                        return guildConnection;
                    }
                default: return null;
            }
        }
        public async Task<TrackResult> QueueSong(string n, CommandContext ctx, int pos = -1)
        {
            var queue = await Database.GetQueue(ctx.Guild);
            var inter = ctx.Client.GetInteractivity();
            if (n.ToLower().StartsWith("http://nicovideo.jp")
                || n.ToLower().StartsWith("http://sp.nicovideo.jp")
                || n.ToLower().StartsWith("https://nicovideo.jp")
                || n.ToLower().StartsWith("https://sp.nicovideo.jp")
                || n.ToLower().StartsWith("http://www.nicovideo.jp")
                || n.ToLower().StartsWith("https://www.nicovideo.jp"))
            {
                var msg = await ctx.RespondAsync("Processing NND Video...");
                var split = n.Split("/".ToCharArray());
                var nndID = split.First(x => x.StartsWith("sm") || x.StartsWith("nm")).Split("?")[0];
                FtpClient client = new(Bot.cfg.NndConfig.FtpConfig.Hostname, new NetworkCredential(Bot.cfg.NndConfig.FtpConfig.User, Bot.cfg.NndConfig.FtpConfig.Password));
                await client.ConnectAsync();
                if (!await client.FileExistsAsync($"{nndID}.mp3"))
                {
                    await msg.ModifyAsync("Preparing download...");
                    var ex = await Utilities.NND.GetNND(nndID, msg);
                    if (ex == null)
                    {
                        await msg.ModifyAsync("Please try again or verify the link");
                        return null;
                    }
                    await msg.ModifyAsync("Uploading");
                    await client.UploadAsync(ex, $"{nndID}.mp3", FtpRemoteExists.Skip, true);
                }
                var Track = await nodeConnection.Rest.GetTracksAsync(new Uri($"https://nnd.meek.moe/new/{nndID}.mp3"));
                if (pos == -1) await Database.AddToQueue(ctx.Guild, ctx.Member.Id, Track.Tracks.First().TrackString);
                else await Database.InsertToQueue(ctx.Guild, ctx.Member.Id, Track.Tracks.First().TrackString, pos);
                if (guildConnection.IsConnected && (playstate == Playstate.NotPlaying || playstate == Playstate.Stopped)) await PlaySong();
                return new TrackResult(Track.PlaylistInfo, Track.Tracks.First());
            }
            else if (n.ToLower().StartsWith("https://www.bilibili.com")
                || n.ToLower().StartsWith("http://www.bilibili.com"))
            {
                var msg = await ctx.RespondAsync("Processing Bilibili Video...");
                var split = n.Split("/".ToCharArray());
                var nndID = split.First(x => x.StartsWith("anime") || x.StartsWith("av")).Split("?")[0];
                FtpClient client = new(Bot.cfg.NndConfig.FtpConfig.Hostname, new NetworkCredential(Bot.cfg.NndConfig.FtpConfig.User, Bot.cfg.NndConfig.FtpConfig.Password));
                await client.ConnectAsync();
                if (!await client.FileExistsAsync($"{nndID}.mp3"))
                {
                    await msg.ModifyAsync("Preparing download...");
                    var ex = await Bilibili.GetBilibili(nndID, msg);
                    if (ex == null)
                    {
                        await msg.ModifyAsync("Please try again or verify the link");
                        return null;
                    }
                    await msg.ModifyAsync("Uploading");
                    await client.UploadAsync(ex, $"{nndID}.mp3", FtpRemoteExists.Skip, true);
                }
                var Track = await nodeConnection.Rest.GetTracksAsync(new Uri($"https://nnd.meek.moe/new/{nndID}.mp3"));
                if (pos == -1) await Database.AddToQueue(ctx.Guild, ctx.Member.Id, Track.Tracks.First().TrackString);
                else await Database.InsertToQueue(ctx.Guild, ctx.Member.Id, Track.Tracks.First().TrackString, pos);
                if (guildConnection.IsConnected && (playstate == Playstate.NotPlaying || playstate == Playstate.Stopped)) await PlaySong();
                return new TrackResult(Track.PlaylistInfo, Track.Tracks.First());
            }
            else if (n.StartsWith("http://") | n.StartsWith("https://"))
            {
                var s = await nodeConnection.Rest.GetTracksAsync(new Uri(n));
                switch (s.LoadResultType)
                {
                    case LavalinkLoadResultType.LoadFailed:
                        {
                            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Failed to load").WithDescription("Loading this song/playlist failed, please try again, reasons could be:\n" +
                                "> Playlist is set to private or unlisted\n" +
                                "> The song is unavailable/deleted").Build());
                            return null;
                        };
                    case LavalinkLoadResultType.NoMatches:
                        {
                            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Failed to load").WithDescription("No song/playlist was found with this URL, please try again/a different one").Build());
                            return null;
                        };
                    case LavalinkLoadResultType.PlaylistLoaded:
                        {
                            if (s.PlaylistInfo.SelectedTrack == -1)
                            {
                                var msg = await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
                                    .WithTitle("Playlist link detected!")
                                    .WithDescription("Please respond with either:\n" +
                                    "``yes``, ``y`` or ``1`` to add the **entire** playlist or\n" +
                                    "``no``, ``n``, ``0`` or let this time out to cancel")
                                    .WithAuthor($"Requested by {ctx.Member.Username}#{ctx.Member.Discriminator} || Timeout 25 seconds", iconUrl: ctx.Member.AvatarUrl)
                                    .Build());
                                var resp = await inter.WaitForMessageAsync(x => x.Author.Id == ctx.User.Id , TimeSpan.FromSeconds(25));
                                await msg.DeleteAsync();
                                if (resp.TimedOut)
                                {
                                    return null;
                                }
                                if (resp.Result.Content == "y" || resp.Result.Content == "yes" || resp.Result.Content == "1")
                                {
                                    await Database.AddToQueue(ctx.Guild, ctx.Member.Id, s.Tracks.ToList());
                                    if (guildConnection.IsConnected && (playstate == Playstate.NotPlaying || playstate == Playstate.Stopped)) await PlaySong();
                                    return new TrackResult(s.PlaylistInfo, s.Tracks);
                                }
                                else return null;
                            }
                            else
                            {
                                var msg = await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
                                    .WithTitle("Link with Playlist detected!")
                                    .WithDescription("Please respond with either:\n" +
                                    "``yes``, ``y`` or ``1`` to add only the referred song in the link or\n" +
                                    "``all`` or ``a`` to add the entire playlistor\n" +
                                    "``no``, ``n``, ``0`` or let this time out to cancel")
                                    .WithAuthor($"Requested by {ctx.Member.Username}#{ctx.Member.Discriminator} || Timeout 25 seconds", iconUrl: ctx.Member.AvatarUrl)
                                    .Build());
                                var resp = await inter.WaitForMessageAsync(x => x.Author.Id == ctx.User.Id, TimeSpan.FromSeconds(25));
                                await msg.DeleteAsync();
                                if (resp.TimedOut)
                                {
                                    return null;
                                }
                                if (resp.Result.Content == "y" || resp.Result.Content == "yes" || resp.Result.Content == "1")
                                {
                                    if (pos == -1) await Database.AddToQueue(ctx.Guild, ctx.Member.Id, s.Tracks.ElementAt(s.PlaylistInfo.SelectedTrack).TrackString);
                                    else await Database.InsertToQueue(ctx.Guild, ctx.Member.Id, s.Tracks.ElementAt(s.PlaylistInfo.SelectedTrack).TrackString, pos);
                                    if (guildConnection.IsConnected && (playstate == Playstate.NotPlaying || playstate == Playstate.Stopped)) await PlaySong();
                                    return new TrackResult(s.PlaylistInfo, s.Tracks.ElementAt(s.PlaylistInfo.SelectedTrack));
                                }
                                if (resp.Result.Content == "a" || resp.Result.Content == "all")
                                {
                                    if (pos == -1)
                                        await Database.AddToQueue(ctx.Guild, ctx.Member.Id, s.Tracks.ToList());
                                    else
                                        await Database.InsertToQueue(ctx.Guild, ctx.Member.Id, s.Tracks.Reverse().ToList(), pos);
                                    if (guildConnection.IsConnected && (playstate == Playstate.NotPlaying || playstate == Playstate.Stopped)) await PlaySong();
                                    return new TrackResult(s.PlaylistInfo, s.Tracks);
                                }
                                else return null;
                            }
                        };
                    default:
                        {
                            if (pos == -1) await Database.AddToQueue(ctx.Guild, ctx.Member.Id, s.Tracks.First().TrackString);
                            else await Database.InsertToQueue(ctx.Guild, ctx.Member.Id, s.Tracks.First().TrackString, pos);
                            if (guildConnection.IsConnected && (playstate == Playstate.NotPlaying || playstate == Playstate.Stopped)) await PlaySong();
                            return new TrackResult(s.PlaylistInfo, s.Tracks.First());
                        };
                }
            }
            else
            {
                var s = await nodeConnection.Rest.GetTracksAsync(n);
                switch (s.LoadResultType)
                {
                    case LavalinkLoadResultType.LoadFailed:
                        {
                            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Failed to load").WithDescription("Loading this song/playlist failed, please try again, reason could be:\n" +
                                "> The song is unavailable/deleted").Build());
                            return null;
                        };
                    case LavalinkLoadResultType.NoMatches:
                        {
                            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Failed to load").WithDescription("No song was found, please try again").Build());
                            return null;
                        };
                    default:
                        {
                            var em = new DiscordEmbedBuilder()
                                .WithTitle("Results!")
                                .WithDescription("Please select a track by responding to this with:\n")
                                .WithAuthor($"Requested by {ctx.Member.Username}#{ctx.Member.Discriminator} || Timeout 25 seconds", iconUrl: ctx.Member.AvatarUrl);
                            int leng = s.Tracks.Count();
                            if (leng > 5) leng = 5;
                            for (int i = 0; i < leng; i++)
                            {
                                em.AddField(new DiscordEmbedField($"{i + 1}.{s.Tracks.ElementAt(i).Title} [{s.Tracks.ElementAt(i).Length}]", $"by {s.Tracks.ElementAt(i).Author} [Link]({s.Tracks.ElementAt(i).Uri})"));
                            }
                            var msg = await ctx.RespondAsync(embed: em.Build());
                            var resp = await inter.WaitForMessageAsync(x => x.Author.Id == ctx.User.Id, TimeSpan.FromSeconds(25));
                            await msg.DeleteAsync();
                            if (resp.TimedOut)
                            {
                                return null;
                            }
                            if (resp.Result.Content == "1")
                            {
                                if (pos == -1) await Database.AddToQueue(ctx.Guild, ctx.Member.Id, s.Tracks.ElementAt(0).TrackString);
                                else await Database.InsertToQueue(ctx.Guild, ctx.Member.Id, s.Tracks.ElementAt(0).TrackString, pos);
                                if (guildConnection.IsConnected && (playstate == Playstate.NotPlaying || playstate == Playstate.Stopped)) await PlaySong();
                                return new TrackResult(s.PlaylistInfo, s.Tracks.ElementAt(0));
                            }
                            if (resp.Result.Content == "2")
                            {
                                if (pos == -1) await Database.AddToQueue(ctx.Guild, ctx.Member.Id, s.Tracks.ElementAt(1).TrackString);
                                else await Database.InsertToQueue(ctx.Guild, ctx.Member.Id, s.Tracks.ElementAt(1).TrackString, pos);
                                if (guildConnection.IsConnected && (playstate == Playstate.NotPlaying || playstate == Playstate.Stopped)) await PlaySong();
                                return new TrackResult(s.PlaylistInfo, s.Tracks.ElementAt(1));
                            }
                            if (resp.Result.Content == "3")
                            {
                                if (pos == -1) await Database.AddToQueue(ctx.Guild, ctx.Member.Id, s.Tracks.ElementAt(2).TrackString);
                                else await Database.InsertToQueue(ctx.Guild, ctx.Member.Id, s.Tracks.ElementAt(2).TrackString, pos);
                                if (guildConnection.IsConnected && (playstate == Playstate.NotPlaying || playstate == Playstate.Stopped)) await PlaySong();
                                return new TrackResult(s.PlaylistInfo, s.Tracks.ElementAt(2)); ;
                            }
                            if (resp.Result.Content == "4")
                            {
                                if (pos == -1) await Database.AddToQueue(ctx.Guild, ctx.Member.Id, s.Tracks.ElementAt(3).TrackString);
                                else await Database.InsertToQueue(ctx.Guild, ctx.Member.Id, s.Tracks.ElementAt(3).TrackString, pos);
                                if (guildConnection.IsConnected && (playstate == Playstate.NotPlaying || playstate == Playstate.Stopped)) await PlaySong();
                                return new TrackResult(s.PlaylistInfo, s.Tracks.ElementAt(3));
                            }
                            if (resp.Result.Content == "5")
                            {
                                if (pos == -1) await Database.AddToQueue(ctx.Guild, ctx.Member.Id, s.Tracks.ElementAt(4).TrackString);
                                else await Database.InsertToQueue(ctx.Guild, ctx.Member.Id, s.Tracks.ElementAt(4).TrackString, pos);
                                if (guildConnection.IsConnected && (playstate == Playstate.NotPlaying || playstate == Playstate.Stopped)) await PlaySong();
                                return new TrackResult(s.PlaylistInfo, s.Tracks.ElementAt(4));
                            }
                            else return null;
                        };
                }
            }
        }

        public async Task<QueueEntry> PlaySong()
        {
            var queue = await Database.GetQueue(voiceChannel.Guild);
            var cur = lastSong;
            if (queue.Count != 1 && repeatMode == RepeatMode.All)
                repeatAllPos++;
            if (repeatAllPos >= queue.Count)
                repeatAllPos = 0;
            if (shuffleMode == ShuffleMode.Off)
                currentSong = queue[0];
            else
                currentSong = queue[new Random().Next(0, queue.Count)];
            if (repeatMode == RepeatMode.All)
                currentSong = queue[repeatAllPos];
            if (repeatMode == RepeatMode.On)
                currentSong = cur;
            Console.WriteLine(queue.Count);
            Console.WriteLine(currentSong.track.TrackString);
            guildConnection.PlaybackFinished += Lavalink.LavalinkTrackFinish;
            playstate = Playstate.Playing;
            await Task.Run(async () => await guildConnection.PlayAsync(currentSong.track));
            return currentSong;
        }
    }

    //     B/S(｀・ω・´) ❤️ (´ω｀)U/C
}
