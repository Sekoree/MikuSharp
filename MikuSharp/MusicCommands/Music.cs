using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Lavalink;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using MikuSharp.Events.MusicCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MikuSharp.MusicCommands
{
    class Music : BaseCommandModule
    {
        [Command("join")]
        public async Task Join(CommandContext ctx)
        {
            var chn = ctx.Member.VoiceState?.Channel;
            if (chn == null || chn == Bot.Guilds[ctx.Guild.Id].GuildConnection?.Channel) return;
            Bot.Guilds[ctx.Guild.Id].UsedChannel = ctx.Channel.Id;
            Bot.Guilds[ctx.Guild.Id].ShardID = ctx.Client.ShardId;
            if (Bot.Guilds[ctx.Guild.Id].GuildConnection?.IsConnected == false || Bot.Guilds[ctx.Guild.Id].GuildConnection == null)
            {
                Bot.Guilds[ctx.Guild.Id].GuildConnection = await Bot.LLEU[ctx.Client.ShardId].ConnectAsync(chn);
                Bot.Guilds[ctx.Guild.Id].GuildConnection.PlaybackFinished += LavalinkEvents.TrackEnd;
            }
            else
            {
                var BotMember = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id);
                await chn.PlaceMemberAsync(BotMember);
            }
            await ctx.RespondAsync($"Heya! {ctx.Member.Mention}");
        }

        [Command("leave")]
        public async Task Leave(CommandContext ctx, string Options = null)
        {
            var chn = ctx.Member.VoiceState?.Channel;
            if (!Bot.Guilds[ctx.Guild.Id].GuildConnection.IsConnected
                || chn == null) return;
            Bot.Guilds[ctx.Guild.Id].UsedChannel = ctx.Channel.Id;
            Bot.Guilds[ctx.Guild.Id].ShardID = ctx.Client.ShardId;
            if (Options?.ToLower().StartsWith("k") == true)
            {
                Bot.Guilds[ctx.Guild.Id].GuildConnection.Disconnect();
                Bot.Guilds[ctx.Guild.Id].Repeat = false;
                Bot.Guilds[ctx.Guild.Id].RepeatAll = false;
                Bot.Guilds[ctx.Guild.Id].Shuffle = false;
                Bot.Guilds[ctx.Guild.Id].Queue.Clear();
                Bot.Guilds[ctx.Guild.Id].CurrentSong = new QueueBase();
            }
            else
            {
                Bot.Guilds[ctx.Guild.Id].GuildConnection.Disconnect();
                Bot.Guilds[ctx.Guild.Id].Repeat = false;
                Bot.Guilds[ctx.Guild.Id].RepeatAll = false;
                Bot.Guilds[ctx.Guild.Id].Shuffle = false;
                Bot.Guilds[ctx.Guild.Id].Queue.Clear();
                Bot.Guilds[ctx.Guild.Id].CurrentSong = new QueueBase();
            }
            Bot.Guilds[ctx.Guild.Id].Playing = false;
            Bot.Guilds[ctx.Guild.Id].GuildConnection.PlaybackFinished -= LavalinkEvents.TrackEnd;
            Bot.Guilds[ctx.Guild.Id].GuildConnection = null;
            await ctx.RespondAsync("cya!");
        }

        [Command("play"), Aliases("p")]
        public async Task Play(CommandContext ctx, [RemainingText]string song = null)
        {
            var chn = ctx.Member.VoiceState?.Channel;
            if (chn == null) return;
            List<LavalinkTrack> Tracks = null;
            if (song == null && ctx.Message.Attachments.First().Url != null)
            {
                song = ctx.Message.Attachments.First().Url;
            }
            if (Bot.Guilds[ctx.Guild.Id].GuildConnection == null || !Bot.Guilds[ctx.Guild.Id].GuildConnection.IsConnected)
            {
                if (Bot.Guilds[ctx.Guild.Id].GuildConnection == null)
                {
                    Bot.Guilds[ctx.Guild.Id].GuildConnection = await Bot.LLEU[ctx.Client.ShardId].ConnectAsync(chn);
                    Bot.Guilds[ctx.Guild.Id].GuildConnection.PlaybackFinished += LavalinkEvents.TrackEnd;
                }
                else
                {
                    Bot.Guilds[ctx.Guild.Id].GuildConnection = await Bot.LLEU[ctx.Client.ShardId].ConnectAsync(chn);
                }
            }
            Bot.Guilds[ctx.Guild.Id].UsedChannel = ctx.Channel.Id;
            Bot.Guilds[ctx.Guild.Id].ShardID = ctx.Client.ShardId;
            if (song != null)
            {
                //var Get = await Utilities.GetTrack(ctx, song);
                //Tracks = Get;
                if (Tracks == null)
                {
                    await ctx.RespondAsync("Cancelled or broken URL");
                    return;
                }
            }
            if (Bot.Guilds[ctx.Guild.Id].Queue.Count != 0
                && song == null
                && Bot.Guilds[ctx.Guild.Id].Playing
                && Bot.Guilds[ctx.Guild.Id].Paused)
            {
                Bot.Guilds[ctx.Guild.Id].Paused = false;
                Bot.Guilds[ctx.Guild.Id].GuildConnection.Resume();
                await ctx.RespondAsync("Playback was just paused, so it was unpaused!");
            }
            else if (Bot.Guilds[ctx.Guild.Id].CurrentSong.Requester == null
                && Bot.Guilds[ctx.Guild.Id].Queue.Count != 0
                && song == null
                && !Bot.Guilds[ctx.Guild.Id].Playing)
            {
                Bot.Guilds[ctx.Guild.Id].Playing = true;
                int nextSong = 0;
                Random rnd = new Random();
                if (Bot.Guilds[ctx.Guild.Id].RepeatAll)
                {
                    Bot.Guilds[ctx.Guild.Id].RepeatAllPosition++; nextSong = Bot.Guilds[ctx.Guild.Id].RepeatAllPosition;
                    if (Bot.Guilds[ctx.Guild.Id].RepeatAllPosition == Bot.Guilds[ctx.Guild.Id].Queue.Count) { Bot.Guilds[ctx.Guild.Id].RepeatAllPosition = 0; nextSong = 0; }
                }
                if (Bot.Guilds[ctx.Guild.Id].Shuffle)
                {
                    nextSong = rnd.Next(0, Bot.Guilds[ctx.Guild.Id].Queue.Count);
                    while ((Bot.Guilds[ctx.Guild.Id].Queue[nextSong] == Bot.Guilds[ctx.Guild.Id].CurrentSong)
                        && Bot.Guilds[ctx.Guild.Id].Queue.Count != 1)
                    {
                        nextSong = rnd.Next(0, Bot.Guilds[ctx.Guild.Id].Queue.Count);
                    }
                }
                Bot.Guilds[ctx.Guild.Id].CurrentSong = Bot.Guilds[ctx.Guild.Id].Queue[nextSong];
                var Next = Bot.Guilds[ctx.Guild.Id].Queue[nextSong].Track;
                if (Next.IsStream)
                {
                    //var e = await Utilities.GetTrackByURL(Bot.Guilds[ctx.Guild.Id].Queue[nextSong].Track.Uri.OriginalString, ctx.Client.ShardId);
                    //Next = e.First();
                }
                Bot.Guilds[ctx.Guild.Id].GuildConnection.Play(Next);
                string time = "";
                if (Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Length.Hours < 1) time = Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Length.ToString(@"mm\:ss");
                else time = Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Length.ToString(@"hh\:mm\:ss");
                await ctx.RespondAsync($"Playing: **{Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Title}** by **{Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Author}** [{time}]\n" +
                    $"Requested by {ctx.Member.Username}");
            }
            else if (Bot.Guilds[ctx.Guild.Id].Queue.Count == 0
                && song != null
                && !Bot.Guilds[ctx.Guild.Id].Playing)
            {
                Bot.Guilds[ctx.Guild.Id].Playing = true;
                foreach (var Track in Tracks)
                {
                    Bot.Guilds[ctx.Guild.Id].Queue.Add(new QueueBase
                    {
                        Requester = ctx.Member,
                        RequestTime = DateTime.Now,
                        Track = Track
                    });
                }
                int nextSong = 0;
                Random rnd = new Random();
                if (Bot.Guilds[ctx.Guild.Id].RepeatAll)
                {
                    Bot.Guilds[ctx.Guild.Id].RepeatAllPosition++; nextSong = Bot.Guilds[ctx.Guild.Id].RepeatAllPosition;
                    if (Bot.Guilds[ctx.Guild.Id].RepeatAllPosition == Bot.Guilds[ctx.Guild.Id].Queue.Count) { Bot.Guilds[ctx.Guild.Id].RepeatAllPosition = 0; nextSong = 0; }
                }
                if (Bot.Guilds[ctx.Guild.Id].Shuffle)
                {
                    nextSong = rnd.Next(0, Bot.Guilds[ctx.Guild.Id].Queue.Count);
                    while ((Bot.Guilds[ctx.Guild.Id].Queue[nextSong] == Bot.Guilds[ctx.Guild.Id].CurrentSong)
                        && Bot.Guilds[ctx.Guild.Id].Queue.Count != 1)
                    {
                        nextSong = rnd.Next(0, Bot.Guilds[ctx.Guild.Id].Queue.Count);
                    }
                }
                Bot.Guilds[ctx.Guild.Id].CurrentSong = Bot.Guilds[ctx.Guild.Id].Queue[nextSong];
                var Next = Bot.Guilds[ctx.Guild.Id].Queue[nextSong].Track;
                if (Next.IsStream)
                {
                    //var e = await Utilities.GetTrackByURL(Bot.Guilds[ctx.Guild.Id].Queue[nextSong].Track.Uri.OriginalString, ctx.Client.ShardId);
                    //Next = e.First();
                }
                Bot.Guilds[ctx.Guild.Id].GuildConnection.Play(Next);
                string time = "";
                if (Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Length.Hours < 1) time = Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Length.ToString(@"mm\:ss");
                else time = Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Length.ToString(@"hh\:mm\:ss");
                await ctx.RespondAsync($"Playing: **{Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Title}** by **{Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Author}** [{time}]\n" +
                    $"Requested by {ctx.Member.Username}");
            }
            else if (Bot.Guilds[ctx.Guild.Id].Queue.Count != 0
                && song != null
                && !Bot.Guilds[ctx.Guild.Id].Playing)
            {
                foreach (var Track in Tracks)
                {
                    Bot.Guilds[ctx.Guild.Id].Queue.Add(new QueueBase
                    {
                        Requester = ctx.Member,
                        RequestTime = DateTime.Now,
                        Track = Track
                    });
                }
                if (Tracks.Count == 1)
                {
                    string time2 = "";
                    if (Tracks.First().Length.Hours < 1) time2 = Tracks.First().Length.ToString(@"mm\:ss");
                    else time2 = Tracks.First().Length.ToString(@"hh\:mm\:ss");
                    await ctx.RespondAsync($"Added: **{Tracks.First().Title}** by **{Tracks.First().Author}** [{time2}]\n" +
                        $"Requested by {ctx.Member.Username}");
                }
                else
                {
                    await ctx.RespondAsync($"Added: {Tracks.Count} to queue!\n" +
                        $"Requested by {ctx.Member.Username}");
                }
                Bot.Guilds[ctx.Guild.Id].Playing = true;
                int nextSong = 0;
                Random rnd = new Random();
                if (Bot.Guilds[ctx.Guild.Id].RepeatAll)
                {
                    Bot.Guilds[ctx.Guild.Id].RepeatAllPosition++; nextSong = Bot.Guilds[ctx.Guild.Id].RepeatAllPosition;
                    if (Bot.Guilds[ctx.Guild.Id].RepeatAllPosition == Bot.Guilds[ctx.Guild.Id].Queue.Count) { Bot.Guilds[ctx.Guild.Id].RepeatAllPosition = 0; nextSong = 0; }
                }
                if (Bot.Guilds[ctx.Guild.Id].Shuffle)
                {
                    nextSong = rnd.Next(0, Bot.Guilds[ctx.Guild.Id].Queue.Count);
                    while ((Bot.Guilds[ctx.Guild.Id].Queue[nextSong] == Bot.Guilds[ctx.Guild.Id].CurrentSong)
                        && Bot.Guilds[ctx.Guild.Id].Queue.Count != 1)
                    {
                        nextSong = rnd.Next(0, Bot.Guilds[ctx.Guild.Id].Queue.Count);
                    }
                }
                Bot.Guilds[ctx.Guild.Id].CurrentSong = Bot.Guilds[ctx.Guild.Id].Queue[nextSong];
                var Next = Bot.Guilds[ctx.Guild.Id].Queue[nextSong].Track;
                if (Next.IsStream)
                {
                    //var e = await Utilities.GetTrackByURL(Bot.Guilds[ctx.Guild.Id].Queue[nextSong].Track.Uri.OriginalString, ctx.Client.ShardId);
                    //Next = e.First();
                }
                Bot.Guilds[ctx.Guild.Id].GuildConnection.Play(Next);
                string time = "";
                if (Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Length.Hours < 1) time = Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Length.ToString(@"mm\:ss");
                else time = Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Length.ToString(@"hh\:mm\:ss");
                await ctx.RespondAsync($"Playing: **{Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Title}** by **{Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Author}** [{time}]\n" +
                    $"Requested by {ctx.Member.Username}");
            }
            else //if (Bot.Guilds[ctx.Guild.Id].Queue.Count != 0
                 //&& song != null
                 //&& Bot.Guilds[ctx.Guild.Id].Playing)
            {
                foreach (var Track in Tracks)
                {
                    Bot.Guilds[ctx.Guild.Id].Queue.Add(new QueueBase
                    {
                        Requester = ctx.Member,
                        RequestTime = DateTime.Now,
                        Track = Track
                    });
                }
                if (Tracks.Count == 1)
                {
                    string time = "";
                    if (Tracks.First().Length.Hours < 1) time = Tracks.First().Length.ToString(@"mm\:ss");
                    else time = Tracks.First().Length.ToString(@"hh\:mm\:ss");
                    await ctx.RespondAsync($"Added: **{Tracks.First().Title}** by **{Tracks.First().Author}** [{time}]\n" +
                        $"Requested by {ctx.Member.Username}");
                }
                else
                {
                    await ctx.RespondAsync($"Added: {Tracks.Count} to queue!\n" +
                        $"Requested by {ctx.Member.Username}");
                }
                if (Bot.Guilds[ctx.Guild.Id].Paused)
                {
                    Bot.Guilds[ctx.Guild.Id].Paused = false;
                    Bot.Guilds[ctx.Guild.Id].GuildConnection.Resume();
                    await ctx.RespondAsync("Playback was just paused, so it was resumed!");
                }
            }
            if (Tracks == null)
            {
                await ctx.RespondAsync("playing...");
            }
        }

        [Command("skip")]
        public async Task Skip(CommandContext ctx)
        {
            var chn = ctx.Member.VoiceState?.Channel;
            if (Bot.Guilds[ctx.Guild.Id].GuildConnection?.IsConnected == false
                || chn == null
                || Bot.Guilds[ctx.Guild.Id].GuildConnection == null) return;
            Bot.Guilds[ctx.Guild.Id].UsedChannel = ctx.Channel.Id;
            Bot.Guilds[ctx.Guild.Id].ShardID = ctx.Client.ShardId;
            if (Bot.Guilds[ctx.Guild.Id].Queue.Count > 1)
            {
                if (!Bot.Guilds[ctx.Guild.Id].Repeat && !Bot.Guilds[ctx.Guild.Id].RepeatAll && Bot.Guilds[ctx.Guild.Id].GuildConnection != null)
                {
                    Bot.Guilds[ctx.Guild.Id].Queue.Remove(Bot.Guilds[ctx.Guild.Id].Queue.First(x => x.RequestTime == Bot.Guilds[ctx.Guild.Id].CurrentSong.RequestTime));
                }
                int nextSong = 0;
                Random rnd = new Random();
                if (Bot.Guilds[ctx.Guild.Id].RepeatAll)
                {
                    Bot.Guilds[ctx.Guild.Id].RepeatAllPosition++; nextSong = Bot.Guilds[ctx.Guild.Id].RepeatAllPosition;
                    if (Bot.Guilds[ctx.Guild.Id].RepeatAllPosition == Bot.Guilds[ctx.Guild.Id].Queue.Count) { Bot.Guilds[ctx.Guild.Id].RepeatAllPosition = 0; nextSong = 0; }
                }
                if (Bot.Guilds[ctx.Guild.Id].Shuffle)
                {
                    nextSong = rnd.Next(0, Bot.Guilds[ctx.Guild.Id].Queue.Count);
                    while ((Bot.Guilds[ctx.Guild.Id].Queue[nextSong] == Bot.Guilds[ctx.Guild.Id].CurrentSong)
                        && Bot.Guilds[ctx.Guild.Id].Queue.Count != 1)
                    {
                        nextSong = rnd.Next(0, Bot.Guilds[ctx.Guild.Id].Queue.Count);
                    }
                }
                Bot.Guilds[ctx.Guild.Id].CurrentSong = Bot.Guilds[ctx.Guild.Id].Queue[nextSong];
                var Next = Bot.Guilds[ctx.Guild.Id].Queue[nextSong].Track;
                if (Next.IsStream)
                {
                    //var e = await Utilities.GetTrackByURL(Bot.Guilds[ctx.Guild.Id].Queue[nextSong].Track.Uri.OriginalString, ctx.Client.ShardId);
                    //Next = e.First();
                }
                Bot.Guilds[ctx.Guild.Id].GuildConnection.Play(Next);
            }
            else
            {
                if (!Bot.Guilds[ctx.Guild.Id].Repeat && !Bot.Guilds[ctx.Guild.Id].RepeatAll && Bot.Guilds[ctx.Guild.Id].GuildConnection != null)
                {
                    Bot.Guilds[ctx.Guild.Id].Queue.Remove(Bot.Guilds[ctx.Guild.Id].Queue.First(x => x.RequestTime == Bot.Guilds[ctx.Guild.Id].CurrentSong.RequestTime));
                }
                Bot.Guilds[ctx.Guild.Id].GuildConnection.Stop();
            }
            await ctx.RespondAsync("skipped...");
        }

        [Command("stop")]
        public async Task Stop(CommandContext ctx)
        {
            var chn = ctx.Member.VoiceState?.Channel;
            if (Bot.Guilds[ctx.Guild.Id].GuildConnection?.IsConnected == false
                || chn == null
                || Bot.Guilds[ctx.Guild.Id].GuildConnection == null) return;
            Bot.Guilds[ctx.Guild.Id].UsedChannel = ctx.Channel.Id;
            Bot.Guilds[ctx.Guild.Id].ShardID = ctx.Client.ShardId;
            Bot.Guilds[ctx.Guild.Id].Stop = true;
            Bot.Guilds[ctx.Guild.Id].GuildConnection.Stop();
            await ctx.RespondAsync("stopped");
        }

        [Command("volume"), Aliases("vol")]
        public async Task Volume(CommandContext ctx, int vol = 100)
        {
            var chn = ctx.Member.VoiceState?.Channel;
            if (Bot.Guilds[ctx.Guild.Id].GuildConnection?.IsConnected == false
                || chn == null
                || Bot.Guilds[ctx.Guild.Id].GuildConnection == null) return;
            Bot.Guilds[ctx.Guild.Id].UsedChannel = ctx.Channel.Id;
            Bot.Guilds[ctx.Guild.Id].ShardID = ctx.Client.ShardId;
            if (vol > 150) vol = 150;
            Bot.Guilds[ctx.Guild.Id].GuildConnection.SetVolume(vol);
            await ctx.RespondAsync($"Volume changed to **{vol}** (150 is max)");
        }

        [Command("pause")]
        public async Task Pause(CommandContext ctx)
        {
            var chn = ctx.Member.VoiceState?.Channel;
            if (Bot.Guilds[ctx.Guild.Id].GuildConnection?.IsConnected == false
                || chn == null
                || Bot.Guilds[ctx.Guild.Id].GuildConnection == null) return;
            Bot.Guilds[ctx.Guild.Id].UsedChannel = ctx.Channel.Id;
            Bot.Guilds[ctx.Guild.Id].ShardID = ctx.Client.ShardId;
            if (Bot.Guilds[ctx.Guild.Id].Paused)
            {
                Bot.Guilds[ctx.Guild.Id].GuildConnection.Resume();
                Bot.Guilds[ctx.Guild.Id].Paused = false;
                await ctx.RespondAsync("Resumed...");
            }
            else
            {
                Bot.Guilds[ctx.Guild.Id].GuildConnection.Pause();
                Bot.Guilds[ctx.Guild.Id].Paused = true;
                await ctx.RespondAsync("Paused.");
            }
        }

        [Command("resume"), Aliases("unpause")]
        public async Task Resume(CommandContext ctx)
        {
            var chn = ctx.Member.VoiceState?.Channel;
            if (Bot.Guilds[ctx.Guild.Id].GuildConnection?.IsConnected == false
                || chn == null
                || Bot.Guilds[ctx.Guild.Id].GuildConnection == null) return;
            Bot.Guilds[ctx.Guild.Id].UsedChannel = ctx.Channel.Id;
            Bot.Guilds[ctx.Guild.Id].ShardID = ctx.Client.ShardId;
            Bot.Guilds[ctx.Guild.Id].GuildConnection.Resume();
            Bot.Guilds[ctx.Guild.Id].Paused = false;
            await ctx.RespondAsync($"Resumed...");
        }

        [Command("queuerclear"), Aliases("qc")]
        public async Task QueuecClear(CommandContext ctx)
        {
            var chn = ctx.Member.VoiceState?.Channel;
            if (Bot.Guilds[ctx.Guild.Id].GuildConnection?.IsConnected == false
                || chn == null
                || Bot.Guilds[ctx.Guild.Id].GuildConnection == null) return;
            Bot.Guilds[ctx.Guild.Id].UsedChannel = ctx.Channel.Id;
            Bot.Guilds[ctx.Guild.Id].ShardID = ctx.Client.ShardId;
            if (Bot.Guilds[ctx.Guild.Id].Playing)
            {
                Bot.Guilds[ctx.Guild.Id].Queue.Clear();
                Bot.Guilds[ctx.Guild.Id].Queue.Add(Bot.Guilds[ctx.Guild.Id].CurrentSong);
            }
            else
            {
                Bot.Guilds[ctx.Guild.Id].Queue.Clear();
            }
            await ctx.RespondAsync("Cleared Queue");

        }

        [Command("queueremove"), Aliases("qr")]
        public async Task QueueRemove(CommandContext ctx, int r)
        {
            var chn = ctx.Member.VoiceState?.Channel;
            if (Bot.Guilds[ctx.Guild.Id].GuildConnection?.IsConnected == false
                || chn == null
                || Bot.Guilds[ctx.Guild.Id].GuildConnection == null
                || r > Bot.Guilds[ctx.Guild.Id].Queue.Count - 1) return;
            Bot.Guilds[ctx.Guild.Id].UsedChannel = ctx.Channel.Id;
            Bot.Guilds[ctx.Guild.Id].ShardID = ctx.Client.ShardId;
            int pos2 = ctx.Member.Roles.ToList().FindIndex(x => x.CheckPermission(DSharpPlus.Permissions.ManageMessages) == DSharpPlus.PermissionLevel.Allowed);
            int pos3 = ctx.Member.Roles.ToList().FindIndex(x => x.CheckPermission(DSharpPlus.Permissions.Administrator) == DSharpPlus.PermissionLevel.Allowed);
            if (ctx.Member == Bot.Guilds[ctx.Guild.Id].Queue[r].Requester || pos2 != -1 || pos3 != -1)
            {
                await ctx.RespondAsync($"Removed: **{Bot.Guilds[ctx.Guild.Id].Queue[r].Track.Title}** by **{Bot.Guilds[ctx.Guild.Id].Queue[r].Track.Author}**");
                Bot.Guilds[ctx.Guild.Id].Queue.RemoveAt(r);
            }
            else
            {
                await ctx.RespondAsync("You need the ``Manage Messages`` permission to delete others tracks");
            }
            Console.WriteLine($"[{ctx.Guild.Id}] Song Removed");
            await Task.CompletedTask;
        }

        [Command("repeat"), Aliases("r")]
        public async Task Repeat(CommandContext ctx)
        {
            var chn = ctx.Member.VoiceState?.Channel;
            if (Bot.Guilds[ctx.Guild.Id].GuildConnection?.IsConnected == false
                || chn == null
                || Bot.Guilds[ctx.Guild.Id].GuildConnection == null) return;
            Bot.Guilds[ctx.Guild.Id].UsedChannel = ctx.Channel.Id;
            Bot.Guilds[ctx.Guild.Id].ShardID = ctx.Client.ShardId;
            Bot.Guilds[ctx.Guild.Id].Repeat = !Bot.Guilds[ctx.Guild.Id].Repeat;
            await ctx.RespondAsync($"Repeat set to {Bot.Guilds[ctx.Guild.Id].Repeat}");
        }

        [Command("repeatall"), Aliases("ra")]
        public async Task RepeatAll(CommandContext ctx)
        {
            var chn = ctx.Member.VoiceState?.Channel;
            Console.WriteLine("chn");
            if (Bot.Guilds[ctx.Guild.Id].GuildConnection?.IsConnected == false
                || chn == null
                || Bot.Guilds[ctx.Guild.Id].GuildConnection == null) return;
            Console.WriteLine("con");
            Bot.Guilds[ctx.Guild.Id].UsedChannel = ctx.Channel.Id;
            Console.WriteLine("useC");
            Bot.Guilds[ctx.Guild.Id].ShardID = ctx.Client.ShardId;
            Console.WriteLine("shard");
            if (Bot.Guilds[ctx.Guild.Id].RepeatAll)
            {
                if (Bot.Guilds[ctx.Guild.Id].RepeatAllPosition != 0)
                {
                    Console.WriteLine("resetra");
                    int i = 0;
                    while (Bot.Guilds[ctx.Guild.Id].RepeatAllPosition != i)
                    {
                        Bot.Guilds[ctx.Guild.Id].Queue.RemoveAt(i);
                        i++;
                    }
                }
                Console.WriteLine("?ra0");
                Bot.Guilds[ctx.Guild.Id].RepeatAllPosition = 0;
                Console.WriteLine("setra");
                Bot.Guilds[ctx.Guild.Id].RepeatAll = false;
                Console.WriteLine("maketrue");
            }
            else
            {
                Bot.Guilds[ctx.Guild.Id].RepeatAll = true;
            }
            await ctx.RespondAsync($"Repeat all set to {Bot.Guilds[ctx.Guild.Id].RepeatAll}");
        }

        [Command("shuffle"), Aliases("s")]
        public async Task Shuffle(CommandContext ctx)
        {
            var chn = ctx.Member.VoiceState?.Channel;
            if (Bot.Guilds[ctx.Guild.Id].GuildConnection?.IsConnected == false
                || chn == null
                || Bot.Guilds[ctx.Guild.Id].GuildConnection == null) return;
            Bot.Guilds[ctx.Guild.Id].UsedChannel = ctx.Channel.Id;
            Bot.Guilds[ctx.Guild.Id].ShardID = ctx.Client.ShardId;
            Bot.Guilds[ctx.Guild.Id].Shuffle = !Bot.Guilds[ctx.Guild.Id].Shuffle;
            await ctx.RespondAsync($"Shuffle set to {Bot.Guilds[ctx.Guild.Id].Shuffle}");
        }

        [Command("queue"), Aliases("q")]
        public async Task Queue(CommandContext ctx)
        {
            try
            {
                var chn = ctx.Member.VoiceState?.Channel;
                if (Bot.Guilds[ctx.Guild.Id].GuildConnection?.IsConnected == false
                    || chn == null
                    || Bot.Guilds[ctx.Guild.Id].GuildConnection == null) return;
                Bot.Guilds[ctx.Guild.Id].UsedChannel = ctx.Channel.Id;
                Bot.Guilds[ctx.Guild.Id].ShardID = ctx.Client.ShardId;
                if (Bot.Guilds[ctx.Guild.Id].Queue.Count == 0)
                {
                    await ctx.RespondAsync("Queue empty");
                    return;
                }
                var inter = ctx.Client.GetInteractivity();
                int songsPerPage = 0;
                int currentPage = 1;
                int songAmount = 0;
                int totalP = Bot.Guilds[ctx.Guild.Id].Queue.Count / 5;
                if ((Bot.Guilds[ctx.Guild.Id].Queue.Count % 5) != 0) totalP++;
                var emb = new DiscordEmbedBuilder();
                emb.WithColor(new DiscordColor("6785A9"));
                List<Page> Pages = new List<Page>();
                string stuff = "";
                if (Bot.Guilds[ctx.Guild.Id].RepeatAll)
                {
                    songAmount = Bot.Guilds[ctx.Guild.Id].RepeatAllPosition;
                    foreach (var Track in Bot.Guilds[ctx.Guild.Id].Queue)
                    {
                        if (songsPerPage == 0 && currentPage == 1)
                        {
                            emb.WithTitle("Current Song");
                            string time = "";
                            if (Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Length.Hours < 1) time = Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Length.ToString(@"mm\:ss");
                            else time = Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Length.ToString(@"hh\:mm\:ss");
                            string time2 = "";
                            if (Bot.Guilds[ctx.Guild.Id].GuildConnection.CurrentState.PlaybackPosition.Hours < 1) time2 = Bot.Guilds[ctx.Guild.Id].GuildConnection.CurrentState.PlaybackPosition.ToString(@"mm\:ss");
                            else time2 = Bot.Guilds[ctx.Guild.Id].GuildConnection.CurrentState.PlaybackPosition.ToString(@"hh\:mm\:ss");
                            stuff += $"**{songAmount}.{Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Title}** by {Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Author} [{time2}/{time}]\n" +
                                $"Requested by {Bot.Guilds[ctx.Guild.Id].CurrentSong.Requester.Username} [Link]({Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Uri.AbsoluteUri})\nˉˉˉˉˉ\n\n";
                        }
                        else
                        {
                            string time = "";
                            if (Bot.Guilds[ctx.Guild.Id].Queue.ElementAt(songAmount).Track.Length.Hours < 1) time = Bot.Guilds[ctx.Guild.Id].Queue.ElementAt(songAmount).Track.Length.ToString(@"mm\:ss");
                            else time = Bot.Guilds[ctx.Guild.Id].Queue.ElementAt(songAmount).Track.Length.ToString(@"hh\:mm\:ss");
                            stuff += $"**{songAmount}.{Bot.Guilds[ctx.Guild.Id].Queue.ElementAt(songAmount).Track.Title}** by {Bot.Guilds[ctx.Guild.Id].Queue.ElementAt(songAmount).Track.Author} [{time}]\n" +
                                $"Requested by {Bot.Guilds[ctx.Guild.Id].Queue.ElementAt(songAmount).Requester.Username} [Link]({Bot.Guilds[ctx.Guild.Id].Queue.ElementAt(songAmount).Track.Uri.AbsoluteUri})\n˘˘˘˘˘\n";
                        }
                        songsPerPage++;
                        songAmount++;
                        if (songAmount == Bot.Guilds[ctx.Guild.Id].Queue.Count)
                        {
                            songAmount = 0;
                        }
                        if (songsPerPage == 5)
                        {
                            songsPerPage = 0;
                            emb.WithDescription(stuff);
                            var opts = "";
                            if (Bot.Guilds[ctx.Guild.Id].Repeat) opts += DiscordEmoji.FromUnicode("🔂");
                            if (Bot.Guilds[ctx.Guild.Id].RepeatAll) opts += DiscordEmoji.FromUnicode("🔁");
                            if (Bot.Guilds[ctx.Guild.Id].Shuffle) opts += DiscordEmoji.FromUnicode("🔀");
                            if (opts != "")
                            {
                                emb.AddField("Playback Options", opts);
                            }
                            emb.WithFooter($"Page {currentPage}/{totalP}");
                            Pages.Add(new Page
                            {
                                Embed = emb.Build()
                            });
                            emb.ClearFields();
                            emb.WithTitle("more™");
                            stuff = "";
                            currentPage++;
                        }
                        if (songAmount == Bot.Guilds[ctx.Guild.Id].RepeatAllPosition)
                        {
                            emb.WithDescription(stuff);
                            var opts = "";
                            if (Bot.Guilds[ctx.Guild.Id].Repeat) opts += DiscordEmoji.FromUnicode("🔂");
                            if (Bot.Guilds[ctx.Guild.Id].RepeatAll) opts += DiscordEmoji.FromUnicode("🔁");
                            if (Bot.Guilds[ctx.Guild.Id].Shuffle) opts += DiscordEmoji.FromUnicode("🔀");
                            if (opts != "")
                            {
                                emb.AddField("Playback Options", opts);
                            }
                            emb.WithFooter($"Page {currentPage}/{totalP}");
                            Pages.Add(new Page
                            {
                                Embed = emb.Build()
                            });
                            emb.ClearFields();
                        }
                    }
                }
                else
                {
                    foreach (var Track in Bot.Guilds[ctx.Guild.Id].Queue)
                    {
                        if (songsPerPage == 0 && currentPage == 1)
                        {
                            emb.WithTitle("Current Song");
                            string time = "";
                            if (Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Length.Hours < 1) time = Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Length.ToString(@"mm\:ss");
                            else time = Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Length.ToString(@"hh\:mm\:ss");
                            string time2 = "";
                            if (Bot.Guilds[ctx.Guild.Id].GuildConnection.CurrentState.PlaybackPosition.Hours < 1) time2 = Bot.Guilds[ctx.Guild.Id].GuildConnection.CurrentState.PlaybackPosition.ToString(@"mm\:ss");
                            else time2 = Bot.Guilds[ctx.Guild.Id].GuildConnection.CurrentState.PlaybackPosition.ToString(@"hh\:mm\:ss");
                            stuff += $"**{Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Title}** by {Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Author} [{time2}/{time}]\n" +
                                $"Requested by {Bot.Guilds[ctx.Guild.Id].CurrentSong.Requester.Username} [Link]({Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Uri.AbsoluteUri})\nˉˉˉˉˉ\n\n";
                        }
                        else
                        {
                            string time = "";
                            if (Track.Track.Length.Hours < 1) time = Track.Track.Length.ToString(@"mm\:ss");
                            else time = Track.Track.Length.ToString(@"hh\:mm\:ss");
                            stuff += $"**{songAmount}.{Track.Track.Title}** by {Track.Track.Author} [{time}]\n" +
                                $"Requested by {Track.Requester.Username} [Link]({Track.Track.Uri.AbsoluteUri})\n˘˘˘˘˘\n";
                        }
                        songsPerPage++;
                        songAmount++;
                        if (songsPerPage == 5)
                        {
                            songsPerPage = 0;
                            emb.WithDescription(stuff);
                            emb.WithFooter($"Page {currentPage}/{totalP}");
                            var opts = "";
                            if (Bot.Guilds[ctx.Guild.Id].Repeat) opts += DiscordEmoji.FromUnicode("🔂");
                            if (Bot.Guilds[ctx.Guild.Id].RepeatAll) opts += DiscordEmoji.FromUnicode("🔁");
                            if (Bot.Guilds[ctx.Guild.Id].Shuffle) opts += DiscordEmoji.FromUnicode("🔀");
                            if (opts != "")
                            {
                                emb.AddField("Playback Options", opts);
                            }
                            Pages.Add(new Page
                            {
                                Embed = emb.Build()
                            });
                            emb.ClearFields();
                            emb.WithTitle("more™");
                            stuff = "";
                            currentPage++;
                        }
                        if (songAmount == Bot.Guilds[ctx.Guild.Id].Queue.Count)
                        {
                            emb.WithDescription(stuff);
                            emb.WithFooter($"Page {currentPage}/{totalP}");
                            var opts = "";
                            if (Bot.Guilds[ctx.Guild.Id].Repeat) opts += DiscordEmoji.FromUnicode("🔂");
                            if (Bot.Guilds[ctx.Guild.Id].RepeatAll) opts += DiscordEmoji.FromUnicode("🔁");
                            if (Bot.Guilds[ctx.Guild.Id].Shuffle) opts += DiscordEmoji.FromUnicode("🔀");
                            if (opts != "")
                            {
                                emb.AddField("Playback Options", opts);
                            }
                            Pages.Add(new Page
                            {
                                Embed = emb.Build()
                            });
                            emb.ClearFields();
                        }
                    }
                }
                if (currentPage == 1)
                {
                    emb.WithDescription(stuff);
                    var opts = "";
                    if (Bot.Guilds[ctx.Guild.Id].Repeat) opts += DiscordEmoji.FromUnicode("🔂");
                    if (Bot.Guilds[ctx.Guild.Id].RepeatAll) opts += DiscordEmoji.FromUnicode("🔁");
                    if (Bot.Guilds[ctx.Guild.Id].Shuffle) opts += DiscordEmoji.FromUnicode("🔀");
                    if (opts != "")
                    {
                        emb.AddField("Playback Options", opts);
                    }
                    await ctx.RespondAsync(embed: emb.Build());
                    return;
                }
                else if (currentPage == 2 && songsPerPage == 0)
                {
                    await ctx.RespondAsync(embed: Pages.First().Embed);
                    return;
                }
                foreach (var eP in Pages.Where(x => x.Embed.Description.Length == 0).ToList())
                {
                    Pages.Remove(eP);
                }
                await inter.SendPaginatedMessage(ctx.Channel, ctx.User, Pages, TimeSpan.FromMinutes(5), TimeoutBehaviour.DeleteReactions);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        [Command("nowplaying"), Aliases("np")]
        public async Task NowPlayling(CommandContext ctx)
        {
            var chn = ctx.Member.VoiceState?.Channel;
            var bot = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id);
            if (chn == null || bot.VoiceState?.Channel != chn || Bot.Guilds[ctx.Guild.Id].GuildConnection == null)
            {
                await Task.CompletedTask;
                return;
            }
            Bot.Guilds[ctx.Guild.Id].UsedChannel = ctx.Channel.Id;
            Bot.Guilds[ctx.Guild.Id].ShardID = ctx.Client.ShardId;
            var eb = new DiscordEmbedBuilder();
            eb.WithColor(new DiscordColor("6785A9"));
            eb.WithTitle("Now Playing");
            eb.WithDescription("**__Current Song:__**");
            if (Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Uri.ToString().Contains("youtu"))
            {
                try
                {
                    var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                    {
                        ApiKey = "AIzaSyDbj184qjOOS8fE6PlHcuyasA8VB_gr_f0",
                        ApplicationName = this.GetType().ToString()
                    });
                    var searchListRequest = youtubeService.Search.List("snippet");
                    searchListRequest.Q = Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Title + " " + Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Author;
                    searchListRequest.MaxResults = 1;
                    searchListRequest.Type = "video";
                    string time1 = "";
                    string time2 = "";
                    if (Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Length.Hours < 1)
                    {
                        time1 = Bot.Guilds[ctx.Guild.Id].GuildConnection.CurrentState.PlaybackPosition.ToString(@"mm\:ss");
                        time2 = Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Length.ToString(@"mm\:ss");
                    }
                    else
                    {
                        time1 = Bot.Guilds[ctx.Guild.Id].GuildConnection.CurrentState.PlaybackPosition.ToString(@"hh\:mm\:ss");
                        time2 = Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Length.ToString(@"hh\:mm\:ss");
                    }
                    var searchListResponse = await searchListRequest.ExecuteAsync();
                    eb.AddField($"{Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Title} ({time1}/{time2})", $"[Video Link]({Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Uri})\n" +
                        $"[{Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Author}](https://www.youtube.com/channel/" + searchListResponse.Items[0].Snippet.ChannelId + ")");
                    if (searchListResponse.Items[0].Snippet.Description.Length > 500) eb.AddField("Description", searchListResponse.Items[0].Snippet.Description.Substring(0, 500) + "...");
                    else eb.AddField("Description", searchListResponse.Items[0].Snippet.Description);
                    eb.WithImageUrl(searchListResponse.Items[0].Snippet.Thumbnails.High.Url);
                    var opts = "";
                    if (Bot.Guilds[ctx.Guild.Id].Repeat) opts += DiscordEmoji.FromUnicode("🔂");
                    if (Bot.Guilds[ctx.Guild.Id].RepeatAll) opts += DiscordEmoji.FromUnicode("🔁");
                    if (Bot.Guilds[ctx.Guild.Id].Shuffle) opts += DiscordEmoji.FromUnicode("🔀");
                    if (opts != "")
                    {
                        eb.AddField("Playback Options", opts);
                    }
                }
                catch
                {
                    if (eb.Fields.Count != 1)
                    {
                        eb.AddField($"{Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Title} ({Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Length})", $"By {Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Author}\n[Link]({Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Uri})\nRequested by {Bot.Guilds[ctx.Guild.Id].CurrentSong.Requester.Mention}");
                        var opts = "";
                        if (Bot.Guilds[ctx.Guild.Id].Repeat) opts += DiscordEmoji.FromUnicode("🔂");
                        if (Bot.Guilds[ctx.Guild.Id].RepeatAll) opts += DiscordEmoji.FromUnicode("🔁");
                        if (Bot.Guilds[ctx.Guild.Id].Shuffle) opts += DiscordEmoji.FromUnicode("🔀");
                        if (opts != "")
                        {
                            eb.AddField("Playback Options", opts);
                        }
                    }
                }
            }
            else
            {
                string time1 = "";
                string time2 = "";
                if (Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Length.Hours < 1)
                {
                    time1 = Bot.Guilds[ctx.Guild.Id].GuildConnection.CurrentState.PlaybackPosition.ToString(@"mm\:ss");
                    time2 = Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Length.ToString(@"mm\:ss");
                }
                else
                {
                    time1 = Bot.Guilds[ctx.Guild.Id].GuildConnection.CurrentState.PlaybackPosition.ToString(@"hh\:mm\:ss");
                    time2 = Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Length.ToString(@"hh\:mm\:ss");
                }
                eb.AddField($"{Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Title} ({time1}/{time2})", $"By {Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Author}\n[Link]({Bot.Guilds[ctx.Guild.Id].CurrentSong.Track.Uri})\nRequested by {Bot.Guilds[ctx.Guild.Id].CurrentSong.Requester.Mention}");
                var opts = "";
                if (Bot.Guilds[ctx.Guild.Id].Repeat) opts += DiscordEmoji.FromUnicode("🔂");
                if (Bot.Guilds[ctx.Guild.Id].RepeatAll) opts += DiscordEmoji.FromUnicode("🔁");
                if (Bot.Guilds[ctx.Guild.Id].Shuffle) opts += DiscordEmoji.FromUnicode("🔀");
                if (opts != "")
                {
                    eb.AddField("Playback Options", opts);
                }
            }
            await ctx.RespondAsync(embed: eb.Build());
        }
    }
}
