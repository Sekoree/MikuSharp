using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using MikuSharp.Entities;
using MikuSharp.Enums;
using MikuSharp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MikuSharp.Commands
{
    [Group("pl")]
    public class Playlist : BaseCommandModule
    {
        [Command("copyqueue")]
        public async Task CopyQueue(CommandContext ctx, [RemainingText] string name)
        {
            var q = await Database.GetQueue(ctx.Guild);
            if (q.Count == 0) return;
            var pls = await PlaylistDB.GetPlaylists(ctx.Member.Id);
            if (pls.Any(x => x.Key == name)) return;
            await PlaylistDB.AddPlaylist(name, ctx.Member.Id);
            foreach(var e in q)
            {
                await PlaylistDB.AddEntry(name, ctx.Member.Id, e.track.TrackString);
            }
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Queue copy").WithDescription("Queue was saved to new playlist -> " + name).Build());
        }

        [Command("create")]
        public async Task Create(CommandContext ctx, [RemainingText] string name)
        {
            var pls = await PlaylistDB.GetPlaylists(ctx.Member.Id);
            if (pls.Any(x => x.Key == name)) return;
            await PlaylistDB.AddPlaylist(name, ctx.Member.Id);
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Create Playlist").WithDescription("New Playlist was created -> " + name).Build());
        }

        [Command("createfixed")]
        public async Task CreateFixed(CommandContext ctx, [RemainingText] string name)
        {
            var inter = ctx.Client.GetInteractivity();
            var pls = await PlaylistDB.GetPlaylists(ctx.Member.Id);
            if (pls.Any(x => x.Key == name)) return;
            var msg = await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Create fixed Playlist").WithDescription("Please send the playlist URL now!\n" +
                "Just the link, no bot prefix needed!").Build());
            var gets = await inter.WaitForMessageAsync(x => x.Author == ctx.Member, TimeSpan.FromSeconds(30));
            await msg.DeleteAsync();
            var s = await Bot.LLEU[ctx.Client.ShardId].GetTracksAsync(new Uri(gets.Result.Content));
            if (s.LoadResultType != DSharpPlus.Lavalink.LavalinkLoadResultType.PlaylistLoaded) return;
            if (gets.Result.Content.Contains("youtu") && !gets.Result.Content.Contains("soundcloud"))
            {
                await PlaylistDB.AddPlaylist(name, ctx.Member.Id, ExtService.Youtube, gets.Result.Content);
            }
            else
            {
                await PlaylistDB.AddPlaylist(name, ctx.Member.Id, ExtService.Soundcloud, gets.Result.Content);
            }
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Create fixed Playlist").WithDescription($"Fixed playlist created with name -> {name} and {s.Tracks.Count()} Songs!").Build());
        }

        [Command("list")]
        public async Task List(CommandContext ctx)
        {
            var pls = await PlaylistDB.GetPlaylists(ctx.Member.Id);
            string pl = "";
            if (pls.Count == 0)
            {
                await ctx.RespondAsync("You dont have any playlists");
                return;
            }
            foreach (var ex in pls)
            {
                pl += ex.Key + Environment.NewLine;
            }
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Playlists").WithDescription("Your playlists:\n" + pl).Build());
        }

        [Command("show")]
        public async Task Show(CommandContext ctx, [RemainingText] string name)
        {
            var queue = await PlaylistDB.GetPlaylist(ctx.Member.Id, name);
            await queue.GetEntries();
            if (queue.Entries.Count == 0)
            {
                await ctx.RespondAsync("Playlist empty!");
                return;
            }
            var inter = ctx.Client.GetInteractivity();
            int songsPerPage = 0;
            int currentPage = 1;
            int songAmount = 0;
            int totalP = queue.Entries.Count / 5;
            if ((queue.Entries.Count % 5) != 0) totalP++;
            var emb = new DiscordEmbedBuilder();
            List<Page> Pages = new List<Page>();
            foreach (var Track in queue.Entries)
            {
                string time = "";
                if (Track.track.Length.Hours < 1) time = Track.track.Length.ToString(@"mm\:ss");
                else time = Track.track.Length.ToString(@"hh\:mm\:ss");
                emb.AddField($"**{songAmount+1}.{Track.track.Title.Replace("*", "").Replace("|", "")}** by {Track.track.Author.Replace("*", "").Replace("|", "")} [{time}]",
                    $"Added on {Track.additionDate} [Link]({Track.track.Uri.AbsoluteUri})");
                songsPerPage++;
                songAmount++;
                if (songsPerPage == 5)
                {
                    songsPerPage = 0;
                    emb.WithFooter($"Page {currentPage}/{totalP}");
                    Pages.Add(new Page(embed: emb));
                    emb.ClearFields();
                    emb.WithTitle($"Songs in {name}");
                    currentPage++;
                }
                if (songAmount == queue.Entries.Count)
                {
                    emb.WithFooter($"Page {currentPage}/{totalP}");
                    Pages.Add(new Page(embed: emb));
                    //Console.WriteLine(emb.Fields.Count);
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
            foreach (var eP in Pages.Where(x => x.Embed.Fields.Count() == 0).ToList())
            {
                Pages.Remove(eP);
            }
            await inter.SendPaginatedMessageAsync(ctx.Channel, ctx.User, Pages, timeoutoverride: TimeSpan.FromMinutes(5));
        }

        [Command("delete")]
        public async Task Delete(CommandContext ctx, [RemainingText] string name)
        {
            var pls = await PlaylistDB.GetPlaylists(ctx.Member.Id);
            if (!pls.Any(x => x.Key == name)) return;
            await PlaylistDB.RemovePlaylist(name, ctx.Member.Id);
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Playlist delete").WithDescription("Deleted playlist -> " + name).Build());
        }

        [Command("rename")]
        public async Task Rename(CommandContext ctx, [RemainingText] string name)
        {
            var inter = ctx.Client.GetInteractivity();
            var pls = await PlaylistDB.GetPlaylist(ctx.Member.Id, name);
            if (pls.ExternalService != ExtService.None) return;
            await pls.GetEntries();
            var msg = await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Rename Playlist").WithDescription($"Please enter the new playlistname now!\n" +
                $"No prefix needed!").Build());
            var hm = await inter.WaitForMessageAsync(x => x.Author == ctx.Member, TimeSpan.FromSeconds(30));
            await msg.DeleteAsync();
            await pls.RenameList(hm.Result.Content);
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("rename Playlist").WithDescription($"Renamed Playlist to -> {hm.Result.Content}!").Build());
        }

        [Command("add")]
        public async Task Add(CommandContext ctx, [RemainingText] string name)
        {
            var inter = ctx.Client.GetInteractivity();
            var pls = await PlaylistDB.GetPlaylist(ctx.Member.Id, name);
            if (pls.ExternalService != ExtService.None) return;
            await pls.GetEntries();
            var msg = await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Add Song").WithDescription($"Please enter the song searchterm or URL now\n" +
                $"No prefix needed!").Build());
            var hm = await inter.WaitForMessageAsync(x => x.Author == ctx.Member, TimeSpan.FromSeconds(30));
            await msg.DeleteAsync();
            await pls.AddSong(hm.Result.Content, ctx);
            await pls.GetEntries();
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Add Song").WithDescription($"Added entry -> {pls.Entries.Last().track.Title}!").Build());
        }

        [Command("insert")]
        public async Task Insert(CommandContext ctx, int pos, [RemainingText] string name)
        {
            pos = pos - 1;
            var inter = ctx.Client.GetInteractivity();
            var pls = await PlaylistDB.GetPlaylist(ctx.Member.Id, name);
            if (pls.ExternalService != ExtService.None) return;
            await pls.GetEntries();
            var msg = await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Insert Song").WithDescription($"Please enter the song searchterm or URL now\n" +
                $"No prefix needed!").Build());
            var hm2 = await inter.WaitForMessageAsync(x => x.Author == ctx.Member, TimeSpan.FromSeconds(30));
            await msg.DeleteAsync();
            await pls.AddSong(hm2.Result.Content, ctx, pos);
            await pls.GetEntries();
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Insert Song").WithDescription($"Inserted entry -> {pls.Entries[pos].track.Title} at {pos+1}!").Build());
        }

        [Command("move")]
        public async Task Move(CommandContext ctx, int oldpos, int newpos, [RemainingText] string pl)
        {
            oldpos = oldpos - 1;
            newpos = newpos - 1;
            var pls = await PlaylistDB.GetPlaylist(ctx.Member.Id, pl);
            await pls.MoveListItems(oldpos, newpos);
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Move Song").WithDescription($"Moved entry -> {pls.Entries[newpos].track.Title}!").Build());
        }

        [Command("clear")]
        public async Task Clear(CommandContext ctx, [RemainingText] string pl)
        {
            var pls = await PlaylistDB.GetPlaylist(ctx.Member.Id, pl);
            await pls.ClearList();
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Clear Playlist").WithDescription($"Cleared all songs from playlist -> {pl}!").Build());
        }

        [Command("remove")]
        public async Task Remove(CommandContext ctx, int pos, [RemainingText] string pl)
        {
            var pls = await PlaylistDB.GetPlaylist(ctx.Member.Id, pl);
            if (pls.ExternalService != ExtService.None) return;
            await pls.GetEntries();
            await pls.RemoveFromList(pos);
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Remove Entry").WithDescription($"Entry removed!").Build());
        }

        [Command("play")]
        public async Task Play(CommandContext ctx,[RemainingText] string pl)
        {
            var pls = await PlaylistDB.GetPlaylist(ctx.Member.Id, pl);
            await pls.GetEntries();
            Console.WriteLine("Done");
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
            g.musicInstance.usedChannel = ctx.Channel;
            foreach (var e in pls.Entries)
            {
                await Database.AddToQueue(ctx.Guild, ctx.Member.Id, e.track.TrackString);
            }
            if (g.musicInstance.guildConnection.IsConnected && (g.musicInstance.playstate == Playstate.NotPlaying || g.musicInstance.playstate == Playstate.Stopped)) await g.musicInstance.PlaySong();
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Play Playlist").WithDescription($"Playing playlist/Added to queue!").Build());
        }
    }
}
