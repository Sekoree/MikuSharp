using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using MikuSharp.Attributes;
using MikuSharp.Entities;
using MikuSharp.Enums;
using MikuSharp.Events;
using MikuSharp.Utilities;
using AlbumArtExtraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using HeyRed.Mime;
using DSharpPlus.CommandsNext.Converters;

namespace MikuSharp.Commands
{
    class Music : BaseCommandModule
    {
        [Command("join")]
        [Description("Joins the voice cahnnel you're in")]
        [RequireUserVoicechatConnection]
        public async Task Join(CommandContext ctx)
        {
            if (!Bot.Guilds.Any(x => x.Key == ctx.Guild.Id))
            {
                Bot.Guilds.TryAdd(ctx.Guild.Id, new Guild(ctx.Client.ShardId));
            }
            var g = Bot.Guilds[ctx.Guild.Id];
            if (g.musicInstance == null)
            {
                g.musicInstance = new Entities.MusicInstance(Bot.LLEU[ctx.Client.ShardId], ctx.Client.ShardId);
            }
            if (!g.musicInstance.guildConnection?.IsConnected == null || !g.musicInstance.guildConnection.IsConnected == false) await g.musicInstance.ConnectToChannel(ctx.Member.VoiceState.Channel);
            g.musicInstance.usedChannel = ctx.Channel;
            await ctx.RespondAsync($"Heya {ctx.Member.Mention}!");
        }

        [Command("leave")]
        [Description("leaves the channel and optionally keeps the Queue")]
        [Usage("|-> I leave and the current queue will be removed",
            "keep |-> I leave and keep the current queue saved")]
        [RequireUserVoicechatConnection]
        public async Task Leave(CommandContext ctx, string Options = null)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            if (g.musicInstance == null) return;
            if (Options?.ToLower() == "k" || Options?.ToLower() == "keep")
            {
                g.musicInstance.playstate = Playstate.NotPlaying;
                await Task.Run(() => g.musicInstance.guildConnection.Stop());
                await Task.Run(() => g.musicInstance.guildConnection.Disconnect());
                g.musicInstance.usedChannel = null;
                await ctx.RespondAsync("cya! 💙");
            }
            else
            {
                g.musicInstance.playstate = Playstate.NotPlaying;
                await Task.Run(() => g.musicInstance.guildConnection.Disconnect());
                await Task.Delay(500);
                await Database.ClearQueue(ctx.Guild);
                g.musicInstance = null;
                await ctx.RespondAsync("cya! 💙");
            }
        }

        [Command("play"), Aliases("p")]
        [Description("Play or Queue a song!")]
        [Usage("songname |-> Searches for a Song with that name (on youtube)",
            "link |-> Play a song directly from a link",
            "``with a music file attached`` |-> Play the songfile you just sent")]
        [RequireUserVoicechatConnection]
        public async Task Play(CommandContext ctx, [RemainingText]string song = null)
        {
            if (!Bot.Guilds.Any(x => x.Key == ctx.Guild.Id))
            {
                Bot.Guilds.TryAdd(ctx.Guild.Id, new Guild(ctx.Client.ShardId));
            }
            var g = Bot.Guilds[ctx.Guild.Id];
            if (g.musicInstance == null)
            {
                g.musicInstance = new MusicInstance(Bot.LLEU[ctx.Client.ShardId], ctx.Client.ShardId);
            }
            if (!g.musicInstance.guildConnection?.IsConnected == null || !g.musicInstance.guildConnection.IsConnected == false) await g.musicInstance.ConnectToChannel(ctx.Member.VoiceState.Channel);
            if (ctx.Message.Attachments.Count == 0 && song == null) return;
            g.musicInstance.usedChannel = ctx.Channel;
            if (song == null)
            {
                await Task.Delay(2500);
                song = ctx.Message.Attachments.First().ProxyUrl;
            }
            var oldState = g.musicInstance.playstate;
            var q = await g.musicInstance.QueueSong(song, ctx);
            if (q == null) return;
            var emb = new DiscordEmbedBuilder();
            if (oldState == Playstate.Playing)
            {
                emb.AddField(q.Tracks.First().Title + "[" + (q.Tracks.First().Length.Hours != 0 ? q.Tracks.First().Length.ToString(@"hh\:mm\:ss") : q.Tracks.First().Length.ToString(@"mm\:ss")) + "]", $"by {q.Tracks.First().Author}\n" +
                    $"Requested by {ctx.Member.Mention}");
                if (q.Tracks.Count != 1)
                {
                    emb.AddField("Playlist added:", $"added {q.Tracks.Count - 1} more");
                }
                await ctx.RespondAsync(embed: emb.WithTitle("Added").Build());
            }
            else
            {
                if (q.PlaylistInfo.SelectedTrack == -1 || q.PlaylistInfo.Name == null) emb.AddField(q.Tracks.First().Title + "[" + (q.Tracks.First().Length.Hours != 0 ? q.Tracks.First().Length.ToString(@"hh\:mm\:ss") : q.Tracks.First().Length.ToString(@"mm\:ss")) + "]", $"by {q.Tracks.First().Author}\nRequested by {ctx.Member.Mention}");
                else emb.AddField(q.Tracks[q.PlaylistInfo.SelectedTrack].Title + "[" + (q.Tracks[q.PlaylistInfo.SelectedTrack].Length.Hours != 0 ? q.Tracks[q.PlaylistInfo.SelectedTrack].Length.ToString(@"hh\:mm\:ss") : q.Tracks[q.PlaylistInfo.SelectedTrack].Length.ToString(@"mm\:ss")) + "]", $"by {q.Tracks[q.PlaylistInfo.SelectedTrack].Author}\nRequested by {ctx.Member.Mention}");
                if (q.Tracks.Count != 1)
                {
                    emb.AddField("Playlist added:", $"added {q.Tracks.Count - 1} more");
                }
                await ctx.RespondAsync(embed: emb.WithTitle("Playing").Build());
            }
        }

        [Command("playinsert"), Aliases("insertplay", "ip")]
        [Description("Queue a song at a specific position!")]
        [Usage("(number) songname |-> Searches for a Song with that name (on youtube)",
            "(number) link |-> Play a song directly from a link",
            "(number) ``with a music file attached`` |-> Play the songfile you just sent")]
        [RequireUserVoicechatConnection]
        public async Task InsertPlay(CommandContext ctx, int pos, [RemainingText]string song = null)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            if (pos < 1) return;
            if (g.musicInstance == null)
            {
                g.musicInstance = new MusicInstance(Bot.LLEU[ctx.Client.ShardId], ctx.Client.ShardId);
            }
            if (!g.musicInstance.guildConnection?.IsConnected == null || !g.musicInstance.guildConnection.IsConnected == false) await g.musicInstance.ConnectToChannel(ctx.Member.VoiceState.Channel);
            if (ctx.Message.Attachments.Count == 0 && song == null) return;
            g.musicInstance.usedChannel = ctx.Channel;
            if (song == null)
            {
                await Task.Delay(2500);
                song = ctx.Message.Attachments.First().ProxyUrl;
            }
            var oldState = g.musicInstance.playstate;
            var q = await g.musicInstance.QueueSong(song, ctx, pos);
            if (q == null) return;
            var emb = new DiscordEmbedBuilder();
            if (oldState == Playstate.Playing)
            {
                emb.AddField(q.Tracks.First().Title + "[" + (q.Tracks.First().Length.Hours != 0 ? q.Tracks.First().Length.ToString(@"hh\:mm\:ss") : q.Tracks.First().Length.ToString(@"mm\:ss")) + "]", $"by {q.Tracks.First().Author}\n" +
                    $"Requested by {ctx.Member.Mention}\nAt position: {pos}");
                if (q.Tracks.Count != 1)
                {
                    emb.AddField("Playlist added:", $"added {q.Tracks.Count - 1} more");
                }
                await ctx.RespondAsync(embed: emb.WithTitle("Playing").Build());
            }
            else
            {
                if (q.PlaylistInfo.SelectedTrack == -1 || q.PlaylistInfo.Name == null) emb.AddField(q.Tracks.First().Title + "[" + (q.Tracks.First().Length.Hours != 0 ? q.Tracks.First().Length.ToString(@"hh\:mm\:ss") : q.Tracks.First().Length.ToString(@"mm\:ss")) + "]", $"by {q.Tracks.First().Author}\nRequested by {ctx.Member.Mention}");
                else emb.AddField(q.Tracks[q.PlaylistInfo.SelectedTrack].Title + "[" + (q.Tracks[q.PlaylistInfo.SelectedTrack].Length.Hours != 0 ? q.Tracks[q.PlaylistInfo.SelectedTrack].Length.ToString(@"hh\:mm\:ss") : q.Tracks[q.PlaylistInfo.SelectedTrack].Length.ToString(@"mm\:ss")) + "]", $"by {q.Tracks[q.PlaylistInfo.SelectedTrack].Author}\nRequested by {ctx.Member.Mention}At position: {pos}");
                if (q.Tracks.Count != 1)
                {
                    emb.AddField("Playlist added:", $"added {q.Tracks.Count - 1} more");
                }
                await ctx.RespondAsync(embed: emb.WithTitle("Added").Build());
            }
        }

        [Command("skip")]
        [Description("Skip the current song")]
        [RequireUserVoicechatConnection]
        public async Task Skip(CommandContext ctx)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            var lastPlayedSongs = await Database.GetLPL(ctx.Guild);
            var queue = await Database.GetQueue(ctx.Guild);
            if (g.musicInstance == null || g.musicInstance?.guildConnection?.IsConnected == false) return;
            g.musicInstance.usedChannel = ctx.Channel;
            g.musicInstance.guildConnection.PlaybackFinished -= Lavalink.LavalinkTrackFinish;
            if (g.musicInstance.repeatMode != RepeatMode.On && g.musicInstance.repeatMode != RepeatMode.All) await Database.RemoveFromQueue(g.musicInstance.currentSong.position, ctx.Guild);
            if (lastPlayedSongs.Count == 0)
            {
                await Database.AddToLPL(ctx.Guild.Id, g.musicInstance.currentSong.track.TrackString);
            }
            else if (lastPlayedSongs[0]?.track.Uri != g.musicInstance.currentSong.track.Uri)
            {
                await Database.AddToLPL(ctx.Guild.Id, g.musicInstance.currentSong.track.TrackString);
            }
            g.musicInstance.lastSong = g.musicInstance.currentSong;
            g.musicInstance.currentSong = null;
            if (queue.Count != 0) await g.musicInstance.PlaySong();
            else
            {
                g.musicInstance.playstate = Playstate.NotPlaying;
                g.musicInstance.guildConnection.Stop();
            }
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription($"**Skipped:**\n{g.musicInstance.lastSong.track.Title}").Build());
        }

        [Command("stop")]
        [Description("Stop Playback")]
        [RequireUserVoicechatConnection]
        public async Task Stop(CommandContext ctx)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            if (g.musicInstance == null || g.musicInstance?.guildConnection?.IsConnected == false) return;
            g.musicInstance.usedChannel = ctx.Channel;
            await Task.Run(() => g.musicInstance.guildConnection.Stop());
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription("**Stopped**\n(use m%%resume to start playback again)").Build());
        }

        [Command("volume"), Aliases("vol")]
        [Description("Change the music volume")]
        [Usage("(0-150) |-> Changed the volume to the specified amount",
            "|-> Changes the volume to the default setting of ``100``")]
        [RequireUserVoicechatConnection]
        public async Task Volume(CommandContext ctx, int vol = 100)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            if (g.musicInstance == null || g.musicInstance?.guildConnection?.IsConnected == false) return;
            g.musicInstance.usedChannel = ctx.Channel;
            if (vol > 150) vol = 150;
            g.musicInstance.guildConnection.SetVolume(vol);
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription($"**Set volume to {vol}**").Build());
        }

        [Command("pause")]
        [Description("Pauses or unpauses playback")]
        [RequireUserVoicechatConnection]
        public async Task Pause(CommandContext ctx)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            if (g.musicInstance == null || g.musicInstance?.guildConnection?.IsConnected == false) return;
            g.musicInstance.usedChannel = ctx.Channel;
            if (g.musicInstance.playstate == Playstate.Stopped)
            {
                await g.musicInstance.PlaySong();
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription("**Started Playback**").Build());
            }
            else if (g.musicInstance.playstate == Playstate.Playing)
            {
                g.musicInstance.guildConnection.Pause();
                g.musicInstance.playstate = Playstate.Paused;
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription("**Paused**").Build());
            }
            else
            {
                g.musicInstance.guildConnection.Resume();
                g.musicInstance.playstate = Playstate.Playing;
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription("**Resumed**").Build());
            }
        }

        [Command("resume"), Aliases("unpause")]
        [Description("Resumes paused playback")]
        [RequireUserVoicechatConnection]
        public async Task Resume(CommandContext ctx)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            if (g.musicInstance == null || g.musicInstance?.guildConnection?.IsConnected == false) return;
            g.musicInstance.usedChannel = ctx.Channel;
            if (g.musicInstance.playstate == Playstate.Stopped)
            {
                await g.musicInstance.PlaySong();
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription("**Started Playback**").Build());
            }
            else
            {
                g.musicInstance.guildConnection.Resume();
                g.musicInstance.playstate = Playstate.Playing;
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription("**Resumed**").Build());
            }
        }

        [Command("queuerclear"), Aliases("qc")]
        [Description("Clears the queue")]
        [RequireUserVoicechatConnection]
        public async Task QueuecClear(CommandContext ctx)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            if (g.musicInstance == null || g.musicInstance?.guildConnection?.IsConnected == false) return;
            g.musicInstance.usedChannel = ctx.Channel;
            await Database.ClearQueue(ctx.Guild);
            await Database.AddToQueue(ctx.Guild, g.musicInstance.currentSong.addedBy, g.musicInstance.currentSong.track.TrackString);
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription("**Cleared queue!**").Build());
        }

        [Command("queuemove"), Aliases("qm", "qmv")]
        [Description("Moves a specific song in the queue")]
        [Usage("(positionOfSong(number)) (newPosition(number)) |-> Moves a desired song to the specified position (refer to m%queue for position numbers)")]
        [RequireUserVoicechatConnection]
        public async Task QueueMove(CommandContext ctx, int old, int newpos)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            var queue = await Database.GetQueue(ctx.Guild);
            if (g.musicInstance == null || g.musicInstance?.guildConnection?.IsConnected == false) return;
            g.musicInstance.usedChannel = ctx.Channel;
            if (old < 1 || newpos < 1 || old == newpos || newpos >= queue.Count) return;
            var oldSong = queue[old];
            await Database.MoveQueueItems(ctx.Guild, old, newpos);
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription($"**Moved**:\n" +
                $"**{oldSong.track.Title}**\nby {oldSong.track.Author}\n" +
                $"from position **{old}** to **{newpos}**!"));
        }

        [Command("queueremove"), Aliases("qr")]
        [Description("Removes a song from queue")]
        [Usage("(number) |-> Removes a song from queue, you can get the number from the queue list command")]
        [RequireUserVoicechatConnection]
        public async Task QueueRemove(CommandContext ctx, int r)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            var queue = await Database.GetQueue(ctx.Guild);
            if (g.musicInstance == null || g.musicInstance?.guildConnection?.IsConnected == false) return;
            g.musicInstance.usedChannel = ctx.Channel;
            var old = queue[r];
            await Database.RemoveFromQueue(r, ctx.Guild);
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription($"**Removed:\n{old.track.Title}**\nby {old.track.Author}").Build());
        }

        [Command("repeat"), Aliases("r")]
        [Description("Repeat the current song or the entire queue")]
        [Usage("|-> If Repeatmode is on it will be turned off, in any other case it will be turned to single song repeat mode",
            "(0,1,2) |-> 0:Off 1:Repeat only the current song 2:Repeat the entire queue",
            "(off, on, all) |-> off:Off on:Repeat only the current song all:Repeat the entire queue")]
        [RequireUserVoicechatConnection]
        public async Task Repeat(CommandContext ctx, int e)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            var queue = await Database.GetQueue(ctx.Guild);
            if (g.musicInstance == null || g.musicInstance?.guildConnection?.IsConnected == false) return;
            g.musicInstance.usedChannel = ctx.Channel;
            switch (e)
            {
                case 0: g.musicInstance.repeatMode = RepeatMode.Off; break;
                case 1: g.musicInstance.repeatMode = RepeatMode.On; break;
                case 2:
                    {
                        g.musicInstance.repeatMode = RepeatMode.All;
                        if (queue.Count != 0 && g.musicInstance.playstate == Playstate.Playing)
                            g.musicInstance.repeatAllPos = queue.FindIndex(x => x.track.Uri == g.musicInstance.currentSong.track.Uri);
                        else
                            g.musicInstance.repeatAllPos = 0;
                        break;
                    }
                default: g.musicInstance.repeatMode = RepeatMode.Off; break;
            }
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription($"Set repeatmode to:\n**{g.musicInstance.repeatMode}**").Build());
        }

        [Command("repeat")]
        public async Task Repeat(CommandContext ctx, string e)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            var queue = await Database.GetQueue(ctx.Guild);
            if (g.musicInstance == null || g.musicInstance?.guildConnection?.IsConnected == false) return;
            g.musicInstance.usedChannel = ctx.Channel;
            switch (e)
            {
                case "off": g.musicInstance.repeatMode = RepeatMode.Off; g.musicInstance.repeatAllPos = 0;  break;
                case "on": g.musicInstance.repeatMode = RepeatMode.On; g.musicInstance.repeatAllPos = 0; break;
                case "all":
                    {
                        g.musicInstance.repeatMode = RepeatMode.All;
                        if (queue.Count != 0 && g.musicInstance.playstate == Playstate.Playing)
                            g.musicInstance.repeatAllPos = queue.FindIndex(x => x.track.Uri == g.musicInstance.currentSong.track.Uri);
                        else
                            g.musicInstance.repeatAllPos = 0;
                        break;
                    }
                default: g.musicInstance.repeatMode = RepeatMode.Off; break;
            }
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription($"Set repeatmode to:\n**{g.musicInstance.repeatMode}**").Build());
        }

        [Command("repeat")]
        public async Task Repeat(CommandContext ctx)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            if (g.musicInstance == null || g.musicInstance?.guildConnection?.IsConnected == false) return;
            g.musicInstance.usedChannel = ctx.Channel;
            if (g.musicInstance.repeatMode != RepeatMode.On) g.musicInstance.repeatMode = RepeatMode.On;
            else g.musicInstance.repeatMode = RepeatMode.Off;
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription($"Set repeatmode to:\n**{g.musicInstance.repeatMode}**").Build());
        }

        [Command("repeatall"), Aliases("ra")]
        [Description("Repeat the entire queue")]
        [Usage("|-> If Repeatmode is set to all it will be turned off, in any other case it will be turned to \"all\" repeat mode")]
        [RequireUserVoicechatConnection]
        public async Task RepeatAll(CommandContext ctx)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            if (g.musicInstance == null || g.musicInstance?.guildConnection?.IsConnected == false) return;
            g.musicInstance.usedChannel = ctx.Channel;
            if (g.musicInstance.repeatMode != RepeatMode.All) g.musicInstance.repeatMode = RepeatMode.All;
            else g.musicInstance.repeatMode = RepeatMode.Off;
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription($"Set repeatmode to:\n**{g.musicInstance.repeatMode}**").Build());
        }

        [Command("shuffle"), Aliases("s")]
        [Description("Play the queue in shuffle mode")]
        [RequireUserVoicechatConnection]
        public async Task Shuffle(CommandContext ctx)
        {
            var g = Bot.Guilds[ctx.Guild.Id];
            if (g.musicInstance == null || g.musicInstance?.guildConnection?.IsConnected == false) return;
            g.musicInstance.usedChannel = ctx.Channel;
            if (g.musicInstance.shuffleMode == ShuffleMode.Off) g.musicInstance.shuffleMode = ShuffleMode.On;
            else g.musicInstance.shuffleMode = ShuffleMode.Off;
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription($"Set Shufflemode to:\n**{g.musicInstance.shuffleMode}**").Build());
        }

        [Command("queue"), Aliases("q")]
        [Description("Show the current queue")]
        public async Task Queue(CommandContext ctx)
        {
            var queue = await Database.GetQueue(ctx.Guild);
            try
            {
                var g = Bot.Guilds[ctx.Guild.Id];
                if (queue.Count == 0)
                {
                    await ctx.RespondAsync("Queue empty");
                    return;
                }
                var inter = ctx.Client.GetInteractivity();
                int songsPerPage = 0;
                int currentPage = 1;
                int songAmount = 0;
                int totalP = queue.Count / 5;
                if ((queue.Count % 5) != 0) totalP++;
                var emb = new DiscordEmbedBuilder();
                List<Page> Pages = new List<Page>();
                if (g.musicInstance.repeatMode == RepeatMode.All)
                {
                    songAmount = g.musicInstance.repeatAllPos;
                    foreach (var Track in queue)
                    {
                        if (songsPerPage == 0 && currentPage == 1)
                        {
                            emb.WithTitle("Current Song");
                            string time = "";
                            if (g.musicInstance.currentSong.track.Length.Hours < 1) time = g.musicInstance.currentSong.track.Length.ToString(@"mm\:ss");
                            else time = g.musicInstance.currentSong.track.Length.ToString(@"hh\:mm\:ss");
                            string time2 = "";
                            if (g.musicInstance.guildConnection.CurrentState.PlaybackPosition.Hours < 1) time2 = g.musicInstance.guildConnection.CurrentState.PlaybackPosition.ToString(@"mm\:ss");
                            else time2 = g.musicInstance.guildConnection.CurrentState.PlaybackPosition.ToString(@"hh\:mm\:ss");
                            emb.AddField($"**{songAmount}.{g.musicInstance.currentSong.track.Title.Replace("*", "").Replace("|", "")}** by {g.musicInstance.currentSong.track.Author.Replace("*", "").Replace("|", "")} [{time2}/{time}]",
                                $"Requested by <@{g.musicInstance.currentSong.addedBy}> [Link]({g.musicInstance.currentSong.track.Uri.AbsoluteUri})\nˉˉˉˉˉ");
                        }
                        else
                        {
                            string time = "";
                            if (queue.ElementAt(songAmount).track.Length.Hours < 1) time = queue.ElementAt(songAmount).track.Length.ToString(@"mm\:ss");
                            else time = queue.ElementAt(songAmount).track.Length.ToString(@"hh\:mm\:ss");
                            emb.AddField($"**{songAmount}.{queue.ElementAt(songAmount).track.Title.Replace("*", "").Replace("|", "")}** by {queue.ElementAt(songAmount).track.Author.Replace("*", "").Replace("|", "")} [{time}]",
                                $"Requested by <@{queue.ElementAt(songAmount).addedBy}> [Link]({queue.ElementAt(songAmount).track.Uri.AbsoluteUri})");
                        }
                        songsPerPage++;
                        songAmount++;
                        if (songAmount == queue.Count)
                        {
                            songAmount = 0;
                        }
                        if (songsPerPage == 5)
                        {
                            songsPerPage = 0;
                            var opts = "";
                            if (g.musicInstance.repeatMode == RepeatMode.On) opts += DiscordEmoji.FromUnicode("🔂");
                            if (g.musicInstance.repeatMode == RepeatMode.All) opts += DiscordEmoji.FromUnicode("🔁");
                            if (g.musicInstance.shuffleMode == ShuffleMode.On) opts += DiscordEmoji.FromUnicode("🔀");
                            if (opts != "")
                            {
                                emb.AddField("Playback Options", opts);
                            }
                            emb.WithFooter($"Page {currentPage}/{totalP}");
                            Pages.Add(new Page(embed: emb));
                            emb.ClearFields();
                            emb.WithTitle("more™");
                            currentPage++;
                        }
                        if (songAmount == g.musicInstance.repeatAllPos)
                        {
                            var opts = "";
                            if (g.musicInstance.repeatMode == RepeatMode.On) opts += DiscordEmoji.FromUnicode("🔂");
                            if (g.musicInstance.repeatMode == RepeatMode.All) opts += DiscordEmoji.FromUnicode("🔁");
                            if (g.musicInstance.shuffleMode == ShuffleMode.On) opts += DiscordEmoji.FromUnicode("🔀");
                            if (opts != "")
                            {
                                emb.AddField("Playback Options", opts);
                            }
                            emb.WithFooter($"Page {currentPage}/{totalP}");
                            Pages.Add(new Page(embed: emb));
                            emb.ClearFields();
                        }
                    }
                }
                else
                {
                    foreach (var Track in queue)
                    {
                        if (songsPerPage == 0 && currentPage == 1)
                        {
                            emb.WithTitle("Current Song");
                            string time = "";
                            if (g.musicInstance.currentSong.track.Length.Hours < 1) time = g.musicInstance.currentSong.track.Length.ToString(@"mm\:ss");
                            else time = g.musicInstance.currentSong.track.Length.ToString(@"hh\:mm\:ss");
                            string time2 = "";
                            if (g.musicInstance.guildConnection.CurrentState.PlaybackPosition.Hours < 1) time2 = g.musicInstance.guildConnection.CurrentState.PlaybackPosition.ToString(@"mm\:ss");
                            else time2 = g.musicInstance.guildConnection.CurrentState.PlaybackPosition.ToString(@"hh\:mm\:ss");
                            emb.AddField($"**{g.musicInstance.currentSong.track.Title.Replace("*", "").Replace("|", "")}** by {g.musicInstance.currentSong.track.Author.Replace("*", "").Replace("|", "")} [{time2}/{time}]",
                                $"Requested by <@{g.musicInstance.currentSong.addedBy}> [Link]({g.musicInstance.currentSong.track.Uri.AbsoluteUri})\nˉˉˉˉˉ");
                        }
                        else
                        {
                            string time = "";
                            if (Track.track.Length.Hours < 1) time = Track.track.Length.ToString(@"mm\:ss");
                            else time = Track.track.Length.ToString(@"hh\:mm\:ss");
                            emb.AddField($"**{songAmount}.{Track.track.Title.Replace("*", "").Replace("|", "")}** by {Track.track.Author.Replace("*", "").Replace("|", "")} [{time}]",
                                $"Requested by <@{Track.addedBy}> [Link]({Track.track.Uri.AbsoluteUri})");
                        }
                        songsPerPage++;
                        songAmount++;
                        if (songsPerPage == 5)
                        {
                            songsPerPage = 0;
                            emb.WithFooter($"Page {currentPage}/{totalP}");
                            var opts = "";
                            if (g.musicInstance.repeatMode == RepeatMode.On) opts += DiscordEmoji.FromUnicode("🔂");
                            if (g.musicInstance.repeatMode == RepeatMode.All) opts += DiscordEmoji.FromUnicode("🔁");
                            if (g.musicInstance.shuffleMode == ShuffleMode.On) opts += DiscordEmoji.FromUnicode("🔀");
                            if (opts != "")
                            {
                                emb.AddField("Playback Options", opts);
                            }
                            Pages.Add(new Page(embed: emb));
                            emb.ClearFields();
                            emb.WithTitle("more™");
                            currentPage++;
                        }
                        if (songAmount == queue.Count)
                        {
                            emb.WithFooter($"Page {currentPage}/{totalP}");
                            var opts = "";
                            if (g.musicInstance.repeatMode == RepeatMode.On) opts += DiscordEmoji.FromUnicode("🔂");
                            if (g.musicInstance.repeatMode == RepeatMode.All) opts += DiscordEmoji.FromUnicode("🔁");
                            if (g.musicInstance.shuffleMode == ShuffleMode.On) opts += DiscordEmoji.FromUnicode("🔀");
                            if (opts != "")
                            {
                                emb.AddField("Playback Options", opts);
                            }
                            Pages.Add(new Page(embed: emb));
                            //Console.WriteLine(emb.Fields.Count);
                            emb.ClearFields();
                        }
                    }
                }
                if (currentPage == 1)
                {
                    var opts = "";
                    if (g.musicInstance.repeatMode == RepeatMode.On) opts += DiscordEmoji.FromUnicode("🔂");
                    if (g.musicInstance.repeatMode == RepeatMode.All) opts += DiscordEmoji.FromUnicode("🔁");
                    if (g.musicInstance.shuffleMode == ShuffleMode.On) opts += DiscordEmoji.FromUnicode("🔀");
                    if (opts != "")
                    {
                        emb.AddField("Playback Options", opts);
                    }
                    //Console.WriteLine(emb.Fields.Count);
                    await ctx.RespondAsync(embed: Pages.First().Embed);
                    return;
                }
                else if (currentPage == 2 && songsPerPage == 0)
                {
                    await ctx.RespondAsync(embed: Pages.First().Embed);
                    return;
                }
                foreach (var eP in Pages.Where(x => x.Embed.Fields.Where(y => y.Name != "Playback Options").Count() == 0).ToList())
                {
                    Pages.Remove(eP);
                }
                await inter.SendPaginatedMessageAsync(ctx.Channel, ctx.User, Pages, timeoutoverride: TimeSpan.FromMinutes(5));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        [Command("nowplaying"), Aliases("np")]
        [Description("Show whats currently playing")]
        public async Task NowPlayling(CommandContext ctx)
        {
            var lastPlayedSongs = await Database.GetLPL(ctx.Guild);
            Stream img = null;
            FileStream e = null;
            var g = Bot.Guilds[ctx.Guild.Id];
            g.shardId = ctx.Client.ShardId;
            var eb = new DiscordEmbedBuilder();
            eb.WithTitle("Now Playing");
            eb.WithDescription("**__Current Song:__**");
            if (g.musicInstance.currentSong.track.Uri.ToString().Contains("youtu"))
            {
                try
                {
                    var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                    {
                        ApiKey = Bot.cfg.YoutubeApiToken,
                        ApplicationName = this.GetType().ToString()
                    });
                    var searchListRequest = youtubeService.Search.List("snippet");
                    searchListRequest.Q = g.musicInstance.currentSong.track.Title + " " + g.musicInstance.currentSong.track.Author;
                    searchListRequest.MaxResults = 1;
                    searchListRequest.Type = "video";
                    string time1, time2;
                    if (g.musicInstance.currentSong.track.Length.Hours < 1)
                    {
                        time1 = g.musicInstance.guildConnection.CurrentState.PlaybackPosition.ToString(@"mm\:ss");
                        time2 = g.musicInstance.currentSong.track.Length.ToString(@"mm\:ss");
                    }
                    else
                    {
                        time1 = g.musicInstance.guildConnection.CurrentState.PlaybackPosition.ToString(@"hh\:mm\:ss");
                        time2 = g.musicInstance.currentSong.track.Length.ToString(@"hh\:mm\:ss");
                    }
                    var searchListResponse = await searchListRequest.ExecuteAsync();
                    eb.AddField($"{g.musicInstance.currentSong.track.Title} ({time1}/{time2})", $"[Video Link]({g.musicInstance.currentSong.track.Uri})\n" +
                        $"[{g.musicInstance.currentSong.track.Author}](https://www.youtube.com/channel/" + searchListResponse.Items[0].Snippet.ChannelId + ")");
                    if (searchListResponse.Items[0].Snippet.Description.Length > 500) eb.AddField("Description", searchListResponse.Items[0].Snippet.Description.Substring(0, 500) + "...");
                    else eb.AddField("Description", searchListResponse.Items[0].Snippet.Description);
                    eb.WithImageUrl(searchListResponse.Items[0].Snippet.Thumbnails.High.Url);
                    var opts = "";
                    if (g.musicInstance.repeatMode == RepeatMode.On) opts += DiscordEmoji.FromUnicode("🔂");
                    if (g.musicInstance.repeatMode == RepeatMode.All) opts += DiscordEmoji.FromUnicode("🔁");
                    if (g.musicInstance.shuffleMode == ShuffleMode.On) opts += DiscordEmoji.FromUnicode("🔀");
                    if (opts != "")
                    {
                        eb.AddField("Playback Options", opts);
                    }
                }
                catch
                {
                    if (eb.Fields.Count != 1)
                    {
                        eb.AddField($"{g.musicInstance.currentSong.track.Title} ({g.musicInstance.currentSong.track.Length})", $"By {g.musicInstance.currentSong.track.Author}\n[Link]({g.musicInstance.currentSong.track.Uri})\nRequested by <@{g.musicInstance.currentSong.addedBy}>");
                        var opts = "";
                        if (g.musicInstance.repeatMode == RepeatMode.On) opts += DiscordEmoji.FromUnicode("🔂");
                        if (g.musicInstance.repeatMode == RepeatMode.All) opts += DiscordEmoji.FromUnicode("🔁");
                        if (g.musicInstance.shuffleMode == ShuffleMode.On) opts += DiscordEmoji.FromUnicode("🔀");
                        if (opts != "")
                        {
                            eb.AddField("Playback Options", opts);
                        }
                    }
                }
            }
            else if (g.musicInstance.currentSong.track.Uri.ToString().StartsWith("https://media.discordapp.net/attachments/") || g.musicInstance.currentSong.track.Uri.ToString().StartsWith("https://cdn.discordapp.com/attachments/"))
            {
                try
                {
                    var c = new HttpClient();
                    MemoryStream d = new MemoryStream(await c.GetByteArrayAsync(g.musicInstance.currentSong.track.Uri))
                    {
                        Position = 0
                    };
                    e = File.Create($@"{g.musicInstance.currentSong.track.Uri.ToString().Split('/')[g.musicInstance.currentSong.track.Uri.ToString().Split('/').Count() - 2]}.{g.musicInstance.currentSong.track.Uri.ToString().Split('/').Last()}");
                    await d.CopyToAsync(e);
                    e.Close();
                    var selector = new Selector();
                    var extractor = selector.SelectAlbumArtExtractor($@"{g.musicInstance.currentSong.track.Uri.ToString().Split('/')[g.musicInstance.currentSong.track.Uri.ToString().Split('/').Count() - 2]}.{ g.musicInstance.currentSong.track.Uri.ToString().Split('/').Last()}");
                    img = extractor.Extract($@"{g.musicInstance.currentSong.track.Uri.ToString().Split('/')[g.musicInstance.currentSong.track.Uri.ToString().Split('/').Count() - 2]}.{ g.musicInstance.currentSong.track.Uri.ToString().Split('/').Last()}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    img = null;
                    File.Delete($@"{g.musicInstance.currentSong.track.Uri.ToString().Split('/')[g.musicInstance.currentSong.track.Uri.ToString().Split('/').Count() - 2]}.{lastPlayedSongs[0].track.Uri.ToString().Split('/').Last()}");
                }
                string time1, time2;
                if (g.musicInstance.currentSong.track.Length.Hours < 1)
                {
                    time1 = g.musicInstance.guildConnection.CurrentState.PlaybackPosition.ToString(@"mm\:ss");
                    time2 = g.musicInstance.currentSong.track.Length.ToString(@"mm\:ss");
                }
                else
                {
                    time1 = g.musicInstance.guildConnection.CurrentState.PlaybackPosition.ToString(@"hh\:mm\:ss");
                    time2 = g.musicInstance.currentSong.track.Length.ToString(@"hh\:mm\:ss");
                }
                eb.AddField($"{g.musicInstance.currentSong.track.Title} ({time1}/{time2})", $"By {g.musicInstance.currentSong.track.Author}\n[Link]({g.musicInstance.currentSong.track.Uri})\nRequested by <@{g.musicInstance.currentSong.addedBy}>");
                var opts = "";
                if (g.musicInstance.repeatMode == RepeatMode.On) opts += DiscordEmoji.FromUnicode("🔂");
                if (g.musicInstance.repeatMode == RepeatMode.All) opts += DiscordEmoji.FromUnicode("🔁");
                if (g.musicInstance.shuffleMode == ShuffleMode.On) opts += DiscordEmoji.FromUnicode("🔀");
                if (opts != "")
                {
                    eb.AddField("Playback Options", opts);
                }
                if (img != null)
                {
                    eb.WithImageUrl($"attachment://{g.musicInstance.currentSong.track.Uri.ToString().Split('/')[g.musicInstance.currentSong.track.Uri.ToString().Split('/').Count() - 2]}.{MimeGuesser.GuessExtension(img)}");
                }
            }
            else
            {
                string time1, time2;
                if (g.musicInstance.currentSong.track.Length.Hours < 1)
                {
                    time1 = g.musicInstance.guildConnection.CurrentState.PlaybackPosition.ToString(@"mm\:ss");
                    time2 = g.musicInstance.currentSong.track.Length.ToString(@"mm\:ss");
                }
                else
                {
                    time1 = g.musicInstance.guildConnection.CurrentState.PlaybackPosition.ToString(@"hh\:mm\:ss");
                    time2 = g.musicInstance.currentSong.track.Length.ToString(@"hh\:mm\:ss");
                }
                eb.AddField($"{g.musicInstance.currentSong.track.Title} ({time1}/{time2})", $"By {g.musicInstance.currentSong.track.Author}\n[Link]({g.musicInstance.currentSong.track.Uri})\nRequested by <@{g.musicInstance.currentSong.addedBy}>");
                var opts = "";
                if (g.musicInstance.repeatMode == RepeatMode.On) opts += DiscordEmoji.FromUnicode("🔂");
                if (g.musicInstance.repeatMode == RepeatMode.All) opts += DiscordEmoji.FromUnicode("🔁");
                if (g.musicInstance.shuffleMode == ShuffleMode.On) opts += DiscordEmoji.FromUnicode("🔀");
                if (opts != "")
                {
                    eb.AddField("Playback Options", opts);
                }
            }
            if (e == null)
            {
                await ctx.RespondAsync(embed: eb.Build());
            }
            else
            {
                await ctx.RespondWithFileAsync(embed: eb.Build(), fileData: img, fileName: $"{g.musicInstance.currentSong.track.Uri.ToString().Split('/')[g.musicInstance.currentSong.track.Uri.ToString().Split('/').Count() - 2]}.{MimeGuesser.GuessExtension(img)}");
                File.Delete($@"{g.musicInstance.currentSong.track.Uri.ToString().Split('/')[g.musicInstance.currentSong.track.Uri.ToString().Split('/').Count() - 2]}.{g.musicInstance.currentSong.track.Uri.ToString().Split('/').Last()}");
            }
        }

        [Command("lastplaying"), Aliases("lp")]
        [Description("Show what played before")]
        public async Task LastPlayling(CommandContext ctx)
        {
            var lastPlayedSongs = await Database.GetLPL(ctx.Guild);
            Stream img = null;
            FileStream e = null;
            var g = Bot.Guilds[ctx.Guild.Id];
            g.shardId = ctx.Client.ShardId;
            var eb = new DiscordEmbedBuilder();
            eb.WithTitle("Now Playing");
            eb.WithDescription("**__Current Song:__**");
            if (lastPlayedSongs[0].track.Uri.ToString().Contains("youtu"))
            {
                try
                {
                    var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                    {
                        ApiKey = Bot.cfg.YoutubeApiToken,
                        ApplicationName = this.GetType().ToString()
                    });
                    var searchListRequest = youtubeService.Search.List("snippet");
                    searchListRequest.Q = lastPlayedSongs[0].track.Title + " " + lastPlayedSongs[0].track.Author;
                    searchListRequest.MaxResults = 1;
                    searchListRequest.Type = "video";
                    string time2 = "";
                    if (lastPlayedSongs[0].track.Length.Hours < 1)
                    {
                        time2 = lastPlayedSongs[0].track.Length.ToString(@"mm\:ss");
                    }
                    else
                    {
                        time2 = lastPlayedSongs[0].track.Length.ToString(@"hh\:mm\:ss");
                    }
                    var searchListResponse = await searchListRequest.ExecuteAsync();
                    eb.AddField($"{lastPlayedSongs[0].track.Title} ({time2})", $"[Video Link]({lastPlayedSongs[0].track.Uri})\n" +
                        $"[{lastPlayedSongs[0].track.Author}](https://www.youtube.com/channel/" + searchListResponse.Items[0].Snippet.ChannelId + ")");
                    if (searchListResponse.Items[0].Snippet.Description.Length > 500) eb.AddField("Description", searchListResponse.Items[0].Snippet.Description.Substring(0, 500) + "...");
                    else eb.AddField("Description", searchListResponse.Items[0].Snippet.Description);
                    eb.WithImageUrl(searchListResponse.Items[0].Snippet.Thumbnails.High.Url);
                    var opts = "";
                    if (g.musicInstance.repeatMode == RepeatMode.On) opts += DiscordEmoji.FromUnicode("🔂");
                    if (g.musicInstance.repeatMode == RepeatMode.All) opts += DiscordEmoji.FromUnicode("🔁");
                    if (g.musicInstance.shuffleMode == ShuffleMode.On) opts += DiscordEmoji.FromUnicode("🔀");
                    if (opts != "")
                    {
                        eb.AddField("Playback Options", opts);
                    }
                }
                catch
                {
                    if (eb.Fields.Count != 1)
                    {
                        eb.AddField($"{lastPlayedSongs[0].track.Title} ({lastPlayedSongs[0].track.Length})", $"By {lastPlayedSongs[0].track.Author}\n[Link]({lastPlayedSongs[0].track.Uri})");
                        var opts = "";
                        if (g.musicInstance.repeatMode == RepeatMode.On) opts += DiscordEmoji.FromUnicode("🔂");
                        if (g.musicInstance.repeatMode == RepeatMode.All) opts += DiscordEmoji.FromUnicode("🔁");
                        if (g.musicInstance.shuffleMode == ShuffleMode.On) opts += DiscordEmoji.FromUnicode("🔀");
                        if (opts != "")
                        {
                            eb.AddField("Playback Options", opts);
                        }
                    }
                }
            }
            else if (lastPlayedSongs[0].track.Uri.ToString().StartsWith("https://media.discordapp.net/attachments/") || lastPlayedSongs[0].track.Uri.ToString().StartsWith("https://cdn.discordapp.com/attachments/"))
            {
                try
                {
                    var c = new HttpClient();
                    MemoryStream d = new MemoryStream(await c.GetByteArrayAsync(lastPlayedSongs[0].track.Uri))
                    {
                        Position = 0
                    };
                    e = File.Create($@"{lastPlayedSongs[0].track.Uri.ToString().Split('/')[lastPlayedSongs[0].track.Uri.ToString().Split('/').Count() - 2]}.{lastPlayedSongs[0].track.Uri.ToString().Split('/').Last()}");
                    await d.CopyToAsync(e);
                    e.Close();
                    var selector = new Selector();
                    var extractor = selector.SelectAlbumArtExtractor($@"{lastPlayedSongs[0].track.Uri.ToString().Split('/')[lastPlayedSongs[0].track.Uri.ToString().Split('/').Count() - 2]}.{lastPlayedSongs[0].track.Uri.ToString().Split('/').Last()}");
                    img = extractor.Extract($@"{lastPlayedSongs[0].track.Uri.ToString().Split('/')[lastPlayedSongs[0].track.Uri.ToString().Split('/').Count() - 2]}.{lastPlayedSongs[0].track.Uri.ToString().Split('/').Last()}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    img = null;
                    File.Delete($@"{lastPlayedSongs[0].track.Uri.ToString().Split('/')[lastPlayedSongs[0].track.Uri.ToString().Split('/').Count() - 2]}.{lastPlayedSongs[0].track.Uri.ToString().Split('/').Last()}");
                }
                string time2 = "";
                if (lastPlayedSongs[0].track.Length.Hours < 1)
                {
                    time2 = lastPlayedSongs[0].track.Length.ToString(@"mm\:ss");
                }
                else
                {
                    time2 = lastPlayedSongs[0].track.Length.ToString(@"hh\:mm\:ss");
                }
                eb.AddField($"{lastPlayedSongs[0].track.Title} ({time2})", $"By {lastPlayedSongs[0].track.Author}\n[Link]({lastPlayedSongs[0].track.Uri})");
                var opts = "";
                if (g.musicInstance.repeatMode == RepeatMode.On) opts += DiscordEmoji.FromUnicode("🔂");
                if (g.musicInstance.repeatMode == RepeatMode.All) opts += DiscordEmoji.FromUnicode("🔁");
                if (g.musicInstance.shuffleMode == ShuffleMode.On) opts += DiscordEmoji.FromUnicode("🔀");
                if (opts != "")
                {
                    eb.AddField("Playback Options", opts);
                }
                if (img != null)
                {
                    eb.WithImageUrl($"attachment://{lastPlayedSongs[0].track.Uri.ToString().Split('/')[lastPlayedSongs[0].track.Uri.ToString().Split('/').Count() - 2]}.{MimeGuesser.GuessExtension(img)}");
                }
            }
            else
            {
                string time2 = "";
                if (lastPlayedSongs[0].track.Length.Hours < 1)
                {
                    time2 = lastPlayedSongs[0].track.Length.ToString(@"mm\:ss");
                }
                else
                {
                    time2 = lastPlayedSongs[0].track.Length.ToString(@"hh\:mm\:ss");
                }
                eb.AddField($"{lastPlayedSongs[0].track.Title} ({time2})", $"By {lastPlayedSongs[0].track.Author}\n[Link]({lastPlayedSongs[0].track.Uri})");
                var opts = "";
                if (g.musicInstance.repeatMode == RepeatMode.On) opts += DiscordEmoji.FromUnicode("🔂");
                if (g.musicInstance.repeatMode == RepeatMode.All) opts += DiscordEmoji.FromUnicode("🔁");
                if (g.musicInstance.shuffleMode == ShuffleMode.On) opts += DiscordEmoji.FromUnicode("🔀");
                if (opts != "")
                {
                    eb.AddField("Playback Options", opts);
                }
            }
            if (e == null)
            {
                await ctx.RespondAsync(embed: eb.Build());
            }
            else
            {
                await ctx.RespondWithFileAsync(embed: eb.Build(), fileData: img, fileName: $"{lastPlayedSongs[0].track.Uri.ToString().Split('/')[lastPlayedSongs[0].track.Uri.ToString().Split('/').Count() - 2]}.{MimeGuesser.GuessExtension(img)}");
                File.Delete($@"{lastPlayedSongs[0].track.Uri.ToString().Split('/')[lastPlayedSongs[0].track.Uri.ToString().Split('/').Count() - 2]}.{lastPlayedSongs[0].track.Uri.ToString().Split('/').Last()}");
            }
        }

        [Command("lastplayinglist"), Aliases("lpl", "lpq")]
        [Description("Show what song were played before")]
        public async Task LastPlaylingList(CommandContext ctx)
        {
            var lastPlayedSongs = await Database.GetLPL(ctx.Guild);
            try
            {
                var g = Bot.Guilds[ctx.Guild.Id];
                if (lastPlayedSongs.Count == 0)
                {
                    await ctx.RespondAsync("Queue empty");
                    return;
                }
                var inter = ctx.Client.GetInteractivity();
                int songsPerPage = 0;
                int currentPage = 1;
                int songAmount = 0;
                int totalP = lastPlayedSongs.Count / 10;
                if ((lastPlayedSongs.Count % 10) != 0) totalP++;
                var emb = new DiscordEmbedBuilder();
                List<Page> Pages = new List<Page>();
                foreach (var Track in lastPlayedSongs)
                {

                    string time = "";
                    if (Track.track.Length.Hours < 1) time = Track.track.Length.ToString(@"mm\:ss");
                    else time = Track.track.Length.ToString(@"hh\:mm\:ss");
                    emb.AddField($"{songAmount+1}.{Track.track.Title.Replace("*", "").Replace("|", "")}",$"by {Track.track.Author.Replace("*", "").Replace("|", "")} [{time}] [Link]({Track.track.Uri})");
                    songsPerPage++;
                    songAmount++;
                    if (songsPerPage == 10)
                    {
                        songsPerPage = 0;
                        emb.WithTitle("Last played songs in this server:\n");
                        emb.WithFooter($"Page {currentPage}/{totalP}");
                        Pages.Add(new Page(embed: emb));
                        emb.ClearFields();
                        emb.WithTitle("more™");
                        currentPage++;
                    }
                    if (songAmount == lastPlayedSongs.Count)
                    {
                        emb.WithTitle("Last played songs in this server:\n");
                        emb.WithFooter($"Page {currentPage}/{totalP}");
                        Pages.Add(new Page(embed: emb));
                        emb.ClearFields();
                    }
                }
                if (currentPage == 1)
                {
                    await ctx.RespondAsync(embed: Pages.First().Embed);
                    return;
                }
                else if (currentPage == 2 && songsPerPage == 0)
                {
                    await ctx.RespondAsync(embed: Pages.First().Embed);
                    return;
                }
                foreach (var eP in Pages.Where(x => x.Embed.Fields.Count == 0).ToList())
                {
                    Pages.Remove(eP);
                }
                await inter.SendPaginatedMessageAsync(ctx.Channel, ctx.User, Pages, timeoutoverride: TimeSpan.FromMinutes(5));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
