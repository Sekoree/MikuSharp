using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Enums;
using DisCatSharp.Interactivity.Extensions;
using DisCatSharp.Lavalink;

using MikuSharp.Attributes;
using MikuSharp.Entities;
using MikuSharp.Enums;
using MikuSharp.Utilities;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MikuSharp.Commands
{
    [Group("playlist")]
    [Aliases("pl")]
    public class Playlist : BaseCommandModule
    {
        [Command("copyqueue")]
        [Aliases("cq")]
        [Description("Copy the current queue to a playlist!")]
        [Usage("(playlistname) |-> Creates a playlist with that name and the current queue")]
        public async Task CopyQueue(CommandContext ctx, [RemainingText] string name)
        {
            var q = await Database.GetQueue(ctx.Guild);
            if (q.Count == 0)
            {
                await ctx.RespondAsync("Nothing in queue");
                return;
            }
            var pls = await PlaylistDB.GetPlaylistsSimple(ctx.Member.Id);
            if (pls.Any(x => x == name))
            {
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Copy Queue").WithDescription("**Error** You already have a playlist with that name!").Build());
                return;
            }
            await PlaylistDB.AddPlaylist(name, ctx.Member.Id);
            foreach(var e in q)
            {
                await PlaylistDB.AddEntry(name, ctx.Member.Id, e.track.TrackString);
            }
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Queue Copy").WithDescription("Queue was saved to new playlist -> " + name).Build());
        }

        [Command("create")]
        [Aliases("c")]
        [Description("Create a playlist")]
        [Usage("(playlistname) |-> Creates a playlist with that name")]
        public async Task Create(CommandContext ctx, [RemainingText] string name)
        {
            var pls = await PlaylistDB.GetPlaylistsSimple(ctx.Member.Id);
            if (pls.Any(x => x == name))
            {
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Create Playlist").WithDescription("**Error** You already have a playlist with that name!").Build());
                return;
            }
            await PlaylistDB.AddPlaylist(name, ctx.Member.Id);
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Create Playlist").WithDescription("New Playlist was created -> " + name).Build());
        }

        [Command("createfixed")]
        [Aliases("cf")]
        [Description("Create a fixed playlist (linked to a Youtube or Soundcloud playlist)")]
        [Usage("(playlistname) |-> Creates a playlist with that name and links it to a YT or SC playlist (You will be asked for the link seperately)")]
        public async Task CreateFixed(CommandContext ctx, [RemainingText] string name)
        {
            var inter = ctx.Client.GetInteractivity();
            var pls = await PlaylistDB.GetPlaylistsSimple(ctx.Member.Id);
            if (pls.Any(x => x == name))
            {
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Create Fixed Playlist").WithDescription("**Error** You already have a playlist with that name!").Build());
                return;
            }
            var msg = await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Create Fixed Playlist").WithDescription("Please send the playlist URL now!\n" +
                "> Just the link, no bot prefix needed!").Build());
            var gets = await inter.WaitForMessageAsync(x => x.Author == ctx.Member, TimeSpan.FromSeconds(30));
            await msg.DeleteAsync();
            LavalinkLoadResult s = null;
            try
            {
                s = await Bot.LLEU[ctx.Client.ShardId].Rest.GetTracksAsync(new Uri(gets.Result.Content));
            }
            catch
            {
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Create Fixed Playlist").WithDescription("**Error** Reasons could be:\n" +
                    "> The provided link was not a playlist\n" +
                    "> The playlist is unavailable (for example set to private)").Build());
                return;
            }
            if (s.LoadResultType != LavalinkLoadResultType.PlaylistLoaded)
            {
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Create Fixed Playlist").WithDescription("**Error** Reasons could be:\n" +
                    "> The provided link was not a playlist\n" +
                    "> The playlist is unavailable (for example set to private)").Build());
                return;
            }
            if (gets.Result.Content.Contains("youtu") && !gets.Result.Content.Contains("soundcloud"))
            {
                await PlaylistDB.AddPlaylist(name, ctx.Member.Id, ExtService.Youtube, gets.Result.Content);
            }
            else
            {
                await PlaylistDB.AddPlaylist(name, ctx.Member.Id, ExtService.Soundcloud, gets.Result.Content);
            }
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Create Fixed Playlist").WithDescription($"Fixed playlist created with name -> {name} and {s.Tracks.Count()} Songs!").Build());
        }

        [Command("list")]
        [Aliases("l")]
        [Description("List all your playlists")]
        [Usage("|-> Shows all your playlists")]
        public async Task List(CommandContext ctx)
        {
            var pls = await PlaylistDB.GetPlaylists(ctx.Guild, ctx.Member.Id);
            if (pls.Count == 0)
            {
                await ctx.RespondAsync("You dont have any playlists");
                return;
            }
            Console.WriteLine(pls.Count);
            var inter = ctx.Client.GetInteractivity();
            int songsPerPage = 0;
            int currentPage = 1;
            int songAmount = 0;
            int totalP = pls.Count / 5;
            if ((pls.Count % 5) != 0) totalP++;
            var emb = new DiscordEmbedBuilder();
            List<Page> Pages = new List<Page>();
            Console.WriteLine(pls == null);
            foreach (var Track in pls)
            {
                //Console.WriteLine(Track.Value == null);
                //Console.WriteLine(Track.Key);
                int songam = 0;
                var ent = await Track.Value.GetEntries();
                songam = ent.Count;
                string sub = "";
                if (Track.Value.ExternalService == ExtService.None)
                {
                    sub = $"Created on: {Track.Value.Creation}\n" +
                        $"Last modified on: {Track.Value.Modify}";
                }
                else
                {
                    sub = $"Created on: {Track.Value.Creation}\n" +
                        $"{Track.Value.ExternalService} [Link]({Track.Value.Url})";
                }
                emb.AddField($"**{songAmount + 1}.{Track.Key}** ({songam} Songs)",
                    sub);
                songsPerPage++;
                songAmount++;
                emb.WithTitle($"List Playlists");
                if (songsPerPage == 5)
                {
                    songsPerPage = 0;
                    emb.WithFooter($"Page {currentPage}/{totalP}");
                    Pages.Add(new Page(embed: emb));
                    emb.ClearFields();
                    currentPage++;
                }
                if (songAmount == pls.Count)
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
            await inter.SendPaginatedMessageAsync(ctx.Channel, ctx.User, Pages, PaginationBehaviour.WrapAround, ButtonPaginationBehavior.Disable);
        }

        [Command("show")]
        [Aliases("s")]
        [Description("Show the contents of a playlist")]
        [Usage("(playlistname) |-> Shows all entries of that playlist",
            "|-> You will be asked to select a playlist and the entries of the selected will be shown")]
        public async Task Show(CommandContext ctx, [RemainingText] string name = null)
        {
            if (name == null)
            {
                name = await SelectPL(ctx);
                if (name == null) return;
            }
            var pls = await PlaylistDB.GetPlaylistsSimple(ctx.Member.Id);
            if (!pls.Any(x => x == name))
            {
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Show Playlist").WithDescription("**Error** You dont have a playlist with that name!").Build());
                return;
            }
            var q = await PlaylistDB.GetPlaylist(ctx.Guild, ctx.Member.Id, name);
            var queue = await q.GetEntries();
            if (queue.Count == 0)
            {
                await ctx.RespondAsync("Playlist empty!");
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
            foreach (var Track in queue)
            {
                string time = "";
                if (Track.track.Length.Hours < 1) time = Track.track.Length.ToString(@"mm\:ss");
                else time = Track.track.Length.ToString(@"hh\:mm\:ss");
                emb.AddField($"**{songAmount+1}.{Track.track.Title.Replace("*", "").Replace("|", "")}** by {Track.track.Author.Replace("*", "").Replace("|", "")} [{time}]",
                    $"Added on {Track.additionDate} [Link]({Track.track.Uri.AbsoluteUri})");
                songsPerPage++;
                songAmount++;
                emb.WithTitle($"Songs in {name}");
                if (songsPerPage == 5)
                {
                    songsPerPage = 0;
                    emb.WithFooter($"Page {currentPage}/{totalP}");
                    Pages.Add(new Page(embed: emb));
                    emb.ClearFields();
                    currentPage++;
                }
                if (songAmount == queue.Count)
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
            await inter.SendPaginatedMessageAsync(ctx.Channel, ctx.User, Pages, PaginationBehaviour.WrapAround, ButtonPaginationBehavior.Disable);
        }

        [Command("delete")]
        [Aliases("d")]
        [Description("Delete a playlist")]
        [Usage("(playlistname) |-> Deletes a playlist with that name",
            "|-> You will be asked to select a playlist and the selected will be deleted")]
        public async Task Delete(CommandContext ctx, [RemainingText] string name= null)
        {
            if (name == null)
            {
                name = await SelectPL(ctx);
                if (name == null) return;
            }
            var pls = await PlaylistDB.GetPlaylistsSimple(ctx.Member.Id);
            if (!pls.Any(x => x == name))
            {
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Delete Playlist").WithDescription("**Error** You dont have a playlist with that name!").Build());
                return;
            }
            await PlaylistDB.RemovePlaylist(name, ctx.Member.Id);
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Delete Playlist").WithDescription("Deleted playlist -> " + name).Build());
        }

        [Command("rename")]
        [Aliases("rn")]
        [Description("Rename a playlist")]
        [Usage("(playlistname) |-> Renames the playlist, you will be asked for the new name seperately!",
            "|-> You will be asked to select a playlist that you want to rename (you will be asked for the new name seperately)")]
        public async Task Rename(CommandContext ctx, [RemainingText] string name = null)
        {
            if (name == null)
            {
                name = await SelectPL(ctx);
                if (name == null) return;
            }
            var inter = ctx.Client.GetInteractivity();
            var p = await PlaylistDB.GetPlaylistsSimple(ctx.Member.Id);
            if (!p.Any(x => x == name))
            {
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Rename Playlist").WithDescription("**Error** You dont have a playlist with that name!").Build());
                return;
            }
            var pls = await PlaylistDB.GetPlaylist(ctx.Guild, ctx.Member.Id, name);
            await pls.GetEntries();
            var msg = await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Rename Playlist").WithDescription($"Please enter the new playlistname now!\n" +
                $"No prefix needed!").Build());
            var hm = await inter.WaitForMessageAsync(x => x.Author == ctx.Member, TimeSpan.FromSeconds(30));
            await msg.DeleteAsync();
            await PlaylistDB.RenameList(name, ctx.Member.Id, hm.Result.Content);
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Rename Playlist").WithDescription($"Renamed Playlist to\n" +
                $"{name} -> {hm.Result.Content}!").Build());
        }

        [Command("add")]
        [Aliases("a")]
        [Description("Add a song to a playlist")]
        [Usage("(playlistname) |-> Adds a song(s) to the playlist (You will be asked for the song link/search a song seperately)",
            "|-> You will be asked to select a playlist and a song will be added to the selected one (You will be asked for the song link/search a song seperately) ")]
        public async Task Add(CommandContext ctx, [RemainingText] string name = null)
        {
            if (name == null)
            {
                name = await SelectPL(ctx);
                if (name == null) return;
            }
            var inter = ctx.Client.GetInteractivity();
            var p = await PlaylistDB.GetPlaylistsSimple(ctx.Member.Id);
            if (!p.Any(x => x == name))
            {
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Add Song").WithDescription("**Error** You dont have a playlist with that name!").Build());
                return;
            }
            var pls = await PlaylistDB.GetPlaylist(ctx.Guild, ctx.Member.Id, name);
            if (pls.ExternalService != ExtService.None)
            {
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Add Song").WithDescription("**Error** This playlist is a fixed one, you cant add songs to this!").Build());
                return;
            }
            var msg = await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Add Song").WithDescription($"Please enter the song searchterm or URL now\n" +
                $"No prefix needed!").Build());
            var hm = await inter.WaitForMessageAsync(x => x.Author == ctx.Member, TimeSpan.FromSeconds(30));
            await msg.DeleteAsync();
            var got = await PlaylistDB.GetSong(hm.Result.Content, ctx);
            if (got == null) return;
            await PlaylistDB.AddEntry(name, ctx.Member.Id, got.Tracks);
            var ent = await pls.GetEntries();
            if (got.Tracks.Count > 1)
            {
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Add Song").WithDescription($"Added entry -> {got.Tracks[0].Title}!\n" +
                    $"And {got.Tracks.Count - 2} more!").Build());
            }
            else
            {
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Add Song").WithDescription($"Added entry -> {got.Tracks[0].Title}!").Build());
            }
        }

        [Command("insert")]
        [Aliases("i")]
        [Description("Insert a song into a playlist (refer to show command)")]
        [Usage("(position) (playlist) |-> Inserts a songs(s) to a playlist at the entered position (You will be asked for the song link/search a song seperately)",
            "(position) |-> You will be asked to select a playlist and in selected one a song(s) will inserted at the entered position (Link/Serach a song will be seperately))")]
        public async Task Insert(CommandContext ctx, int pos, [RemainingText] string name = null)
        {
            if (name == null)
            {
                name = await SelectPL(ctx);
                if (name == null) return;
            }
            pos--;
            var inter = ctx.Client.GetInteractivity();
            var p = await PlaylistDB.GetPlaylistsSimple(ctx.Member.Id);
            if (!p.Any(x => x == name))
            {
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Insert Song").WithDescription("**Error** You dont have a playlist with that name!").Build());
                return;
            }
            var pls = await PlaylistDB.GetPlaylist(ctx.Guild, ctx.Member.Id, name);
            if (pls.ExternalService != ExtService.None)
            {
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Insert Song").WithDescription("**Error** This playlist is a fixed one, you cant add songs to this!").Build());
                return;
            }
            var msg = await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Insert Song").WithDescription($"Please enter the song searchterm or URL now\n" +
                $"No prefix needed!").Build());
            var hm2 = await inter.WaitForMessageAsync(x => x.Author == ctx.Member, TimeSpan.FromSeconds(30));
            await msg.DeleteAsync();
            var got = await PlaylistDB.GetSong(hm2.Result.Content, ctx);
            if (got == null) return;
            got.Tracks.Reverse();
            await PlaylistDB.InsertEntry(ctx.Guild, name, ctx.Member.Id,got.Tracks, pos);
            var ent = await pls.GetEntries();
            if (got.Tracks.Count > 1)
            {
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Insert Song").WithDescription($"Inserted entry -> {got.Tracks[0].Title} at {pos + 1}!\n" +
                    $"And {got.Tracks.Count - 2} more!").Build());
            }
            else
            {
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Insert Song").WithDescription($"Inserted entry -> {got.Tracks[0].Title} at {pos + 1}!").Build());
            }
        }

        [Command("move")]
        [Aliases("m")]
        [Description("Move a song in your playlist (refer to show command)")]
        [Usage("(currentPosition) (newPosition) (playlistname) |-> Moves a song from the current to the new position",
            "(currentPosition (newPosition) |-> You will be asked to select a playlist and in selected that song will be moved")]
        public async Task Move(CommandContext ctx, int oldpos, int newpos, [RemainingText] string pl = null)
        {
            if (pl == null)
            {
                pl = await SelectPL(ctx);
                if (pl == null) return;
            }
            oldpos--;
            newpos--;
            var p = await PlaylistDB.GetPlaylistsSimple(ctx.Member.Id);
            if (!p.Any(x => x == pl))
            {
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Insert Song").WithDescription("**Error** You dont have a playlist with that name!").Build());
                return;
            }
            var pls = await PlaylistDB.GetPlaylist(ctx.Guild, ctx.Member.Id, pl);
            if (pls.ExternalService != ExtService.None)
            {
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Insert Song").WithDescription("**Error** This playlist is a fixed one, you cant move songs!").Build());
                return;
            }
            var e = await pls.GetEntries();
            if (e[newpos] == null | e[oldpos] == null) return;
            await PlaylistDB.MoveListItems(ctx.Guild, pl, ctx.Member.Id, oldpos, newpos);
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Move Song").WithDescription($"Moved entry -> {e[oldpos].track.Title} to position {newpos+1}!").Build());
        }

        [Command("clear")]
        [Aliases("cc")]
        [Description("Clear all entries from a playlist")]
        [Usage("(playlistname) |-> Clear all entries from that playlist",
           "|-> You will be asked to select a playlist and all entries of selected one will be deleted")]
        public async Task Clear(CommandContext ctx, [RemainingText] string pl = null)
        {
            if (pl == null)
            {
                pl = await SelectPL(ctx);
                if (pl == null) return;
            }
            var p = await PlaylistDB.GetPlaylistsSimple(ctx.Member.Id);
            if (!p.Any(x => x == pl))
            {
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Clear Playlist").WithDescription("**Error** You dont have a playlist with that name!").Build());
                return;
            }
            await PlaylistDB.ClearList(pl, ctx.Member.Id);
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Clear Playlist").WithDescription($"Cleared all songs from playlist -> {pl}!").Build());
        }

        [Command("remove")]
        [Aliases("r", "rm")]
        [Description("Remove a song from a playlist (refer to show command)")]
        [Usage("(position) (playlistname)|-> Removes the song at that position from the selected playlist",
            "(position) |-> You will be asked to select a playlist and the song att he entered position will be removed")]
        public async Task Remove(CommandContext ctx, int pos, [RemainingText] string pl = null)
        {
            if (pl == null)
            {
                pl = await SelectPL(ctx);
                if (pl == null) return;
            }
            pos--;
            var p = await PlaylistDB.GetPlaylistsSimple(ctx.Member.Id);
            if (!p.Any(x => x == pl))
            {
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Remove Song").WithDescription("**Error** You dont have a playlist with that name!").Build());
                return;
            }
            var ents = await PlaylistDB.GetPlaylist(ctx.Guild, ctx.Member.Id, pl);
            var en = await ents.GetEntries();
            await PlaylistDB.RemoveFromList(ctx.Guild, pos, pl, ctx.Member.Id);
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Remove Song").WithDescription($"Entry removed! -> {en[pos].track.Title}").Build());
        }

        [Command("play")]
        [Aliases("p")]
        [Description("Play a playlist/Add the songs to the queue")]
        [Usage("(playlistname) |-> Play/Add the songs to queue of that playlist",
            "|-> You will be asked to select a playlist and it will be played/added to queue")]
        [RequireUserVoicechatConnection]
        public async Task Play(CommandContext ctx,[RemainingText] string pl = null)
        {
            if (pl == null)
            {
                pl = await SelectPL(ctx);
                if (pl == null) return;
            }
            var ps = await PlaylistDB.GetPlaylists(ctx.Guild, ctx.Member.Id);
            if (!ps.Any(x => x.Key == pl))
            {
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Play Playlist").WithDescription("**Error** You dont have a playlist with that name!").Build());
                return;
            }
            var pls = await PlaylistDB.GetPlaylist(ctx.Guild, ctx.Member.Id, pl);
            var p = await pls.GetEntries();
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
            await Database.AddToQueue(ctx.Guild, ctx.Member.Id, p);
            if (g.musicInstance.guildConnection.IsConnected && (g.musicInstance.playstate == Playstate.NotPlaying || g.musicInstance.playstate == Playstate.Stopped)) await g.musicInstance.PlaySong();
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Play Playlist").WithDescription($"Playing playlist/Added to queue!").Build());
        }

        public async Task<string> SelectPL(CommandContext ctx)
        {
            var inter = ctx.Client.GetInteractivity();
            await ctx.TriggerTypingAsync();
            var plls = await PlaylistDB.GetPlaylistsSimple(ctx.Member.Id);
            if (plls.Count == 0)
            {
                await ctx.RespondAsync("You dont have any playlists");
                return null;
            }
            var pls = await PlaylistDB.GetPlaylists(ctx.Guild, ctx.Member.Id);
            string plss = "";
            int i = 1;
            foreach(var pl in pls)
            {
                plss += $"**{i}.{pl.Key} ({pl.Value.SongCount}Songs)**\n" +
                    $"Created at {pl.Value.Creation}\n" +
                    $"Last modified at {pl.Value.Modify}\n";
                i++;
            }
            var msg = await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Select Playlist || Timeout 25 seconds").WithDescription("Please select a playlist either by **Number** or **Name**\n\n" +
                $"{plss}"));
            var res = await inter.WaitForMessageAsync(x => x.Author == ctx.User, TimeSpan.FromSeconds(25));
            int got = -1;
            try
            {
                got = Convert.ToInt32(res.Result.Content);
            }
            catch { }
            await msg.DeleteAsync();
            await res.Result.DeleteAsync();
            if (got == -1)
            {
                if (pls.Any(x => x.Key == res.Result.Content)) return res.Result.Content;
                else return null;
            }
            else if (got != -1 && pls.ElementAtOrDefault(got-1).Value != null)
            {
                return pls.ElementAtOrDefault(got-1).Key;
            }
            else return null;
        }
    }
}
