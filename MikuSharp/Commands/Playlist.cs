using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Extensions;
using DisCatSharp.Lavalink;

using Microsoft.Extensions.Logging;

using MikuSharp.Attributes;
using MikuSharp.Entities;
using MikuSharp.Enums;
using MikuSharp.Utilities;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MikuSharp.Commands;

[SlashCommandGroup("playlists", "Manage your playlists", dmPermission: false)]
public class Playlists : ApplicationCommandsModule
{
	[SlashCommandGroup("new", "Playlist creation")]
	public class PlaylistCreation : ApplicationCommandsModule
	{
		[SlashCommand("copy_queue", "Copy the current queue to a playlist!")]
		[RequireUserAndBotVoicechatConnection]
		public static async Task CopyQueueToNewPlaylistAsync(InteractionContext ctx, 
			[Option("name", "Name of new playlist")] string name
		)
		{
			await ctx.DeferAsync(true);
			var q = await Database.GetQueueAsync(ctx.Guild);
			if (q.Count == 0)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Nothing in queue"));
				return;
			}
			var pls = await PlaylistDB.GetPlaylistsSimple(ctx.Member.Id);
			if (pls.Any(x => x == name))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Copy Queue").WithDescription("**Error** You already have a playlist with that playlist!").Build()));
				return;
			}
			await PlaylistDB.AddPlaylist(name, ctx.Member.Id);
			foreach (var e in q)
			{
				await PlaylistDB.AddEntry(name, ctx.Member.Id, e.track.TrackString);
			}
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Queue Copy").WithDescription("Queue was saved to new playlist -> " + name).Build()));
		}

		[SlashCommand("create", "Create a playlist")]
		public static async Task CreatePlaylistAsync(InteractionContext ctx, 
			[Option("name", "Name of new playlist")] string name
		)
		{
			await ctx.DeferAsync(true);
			var pls = await PlaylistDB.GetPlaylistsSimple(ctx.Member.Id);
			if (pls.Any(x => x == name))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Create Playlist").WithDescription("**Error** You already have a playlist with that playlist!").Build()));
				return;
			}
			await PlaylistDB.AddPlaylist(name, ctx.Member.Id);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Create Playlist").WithDescription("New Playlist was created -> " + name).Build()));
		}

		[SlashCommand("create_fixed", "Create a fixed playlist (linked to a Youtube or Soundcloud playlist)")]
		public static async Task CreateFixedPlaylistAsync(InteractionContext ctx, 
			[Option("name", "Name of new playlist")] string name,
			[Option("link", "Link to playlist")] string link
		)
		{
			await ctx.DeferAsync(true);
			var pls = await PlaylistDB.GetPlaylistsSimple(ctx.Member.Id);
			if (pls.Any(x => x == name))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Create Fixed Playlist").WithDescription("**Error** You already have a playlist with that playlist!").Build()));
				return;
			}
			LavalinkLoadResult s = null;
			try
			{
				s = await MikuBot.LavalinkNodeConnections[ctx.Client.ShardId].Rest.GetTracksAsync(new Uri(link));
			}
			catch
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Create Fixed Playlist").WithDescription("**Error** Reasons could be:\n> The provided link was not a playlist\n> The playlist is unavailable (for example set to private)").Build()));
				return;
			}
			if (s.LoadResultType != LavalinkLoadResultType.PlaylistLoaded)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Create Fixed Playlist").WithDescription("**Error** Reasons could be:\n> The provided link was not a playlist\n> The playlist is unavailable (for example set to private)").Build()));
				return;
			}
			if (link.Contains("youtu") && !link.Contains("soundcloud"))
			{
				await PlaylistDB.AddPlaylist(name, ctx.Member.Id, ExtService.Youtube, link);
			}
			else
			{
				await PlaylistDB.AddPlaylist(name, ctx.Member.Id, ExtService.Soundcloud, link);
			}
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Create Fixed Playlist").WithDescription($"Fixed playlist created with playlist -> {name} and {s.Tracks.Count} Songs!").Build()));
		}
	}

	[SlashCommandGroup("manage", "Playlist management")]
	public class PlaylistManagement : ApplicationCommandsModule
	{
		[SlashCommand("list", "List all your playlists")]
		public static async Task ListPlaylistsAsync(InteractionContext ctx)
		{
			await ctx.DeferAsync(true);
			var pls = await PlaylistDB.GetPlaylists(ctx.Guild, ctx.Member.Id);
			if (pls.Count == 0)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You dont have any playlists"));
				return;
			}
			//ctx.Client.Logger.LogDebug(pls.Count.ToString());
			var inter = ctx.Client.GetInteractivity();
			int songsPerPage = 0;
			int currentPage = 1;
			int songAmount = 0;
			int totalP = pls.Count / 5;
			if ((pls.Count % 5) != 0) totalP++;
			var emb = new DiscordEmbedBuilder();
			List<Page> Pages = new();
			foreach (var Track in pls)
			{
				//ctx.Client.Logger.LogDebug(Track.Value == null);
				//ctx.Client.Logger.LogDebug(Track.Key);
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
				emb.AddField(new DiscordEmbedField($"**{songAmount + 1}.{Track.Key}** ({songam} Songs)", sub));
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
					emb.ClearFields();
				}
			}
			if (currentPage == 1)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(Pages.First().Embed));
				return;
			}
			else if (currentPage == 2 && songsPerPage == 0)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(Pages.First().Embed));
				return;
			}
			foreach (var eP in Pages.Where(x => x.Embed.Fields.Count == 0).ToList())
			{
				Pages.Remove(eP);
			}
			await inter.SendPaginatedResponseAsync(ctx.Interaction, true, false, ctx.User, Pages);
		}

		[SlashCommand("show", "Show the contents of a playlist")]
		public static async Task ShowPlaylistAsync(InteractionContext ctx, 
			[Option("playlist", "Name of playlist to show", true), Autocomplete(typeof(AutocompleteProviders.PlaylistProvider))] string playlist
		)
		{
			await ctx.DeferAsync(true);
			if (CheckError(playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Detected error, do you have a playlist?"));
				return;
			}
			var pls = await PlaylistDB.GetPlaylistsSimple(ctx.Member.Id);
			if (!pls.Any(x => x == playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Show Playlist").WithDescription("**Error** You dont have a playlist with that playlist!").Build()));
				return;
			}
			var q = await PlaylistDB.GetPlaylist(ctx.Guild, ctx.Member.Id, playlist);
			var queue = await q.GetEntries();
			if (queue.Count == 0)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Playlist empty!"));
				return;
			}
			var inter = ctx.Client.GetInteractivity();
			int songsPerPage = 0;
			int currentPage = 1;
			int songAmount = 0;
			int totalP = queue.Count / 5;
			if ((queue.Count % 5) != 0) totalP++;
			var emb = new DiscordEmbedBuilder();
			List<Page> Pages = new();
			foreach (var Track in queue)
			{
				string time = "";
				if (Track.track.Length.Hours < 1) time = Track.track.Length.ToString(@"mm\:ss");
				else time = Track.track.Length.ToString(@"hh\:mm\:ss");
				emb.AddField(new DiscordEmbedField($"**{songAmount + 1}.{Track.track.Title.Replace("*", "").Replace("|", "")}** by {Track.track.Author.Replace("*", "").Replace("|", "")} [{time}]",
					$"Added on {Track.additionDate} [Link]({Track.track.Uri.AbsoluteUri})"));
				songsPerPage++;
				songAmount++;
				emb.WithTitle($"Songs in {playlist}");
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
					emb.ClearFields();
				}
			}
			if (currentPage == 1)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(Pages.First().Embed));
				return;
			}
			else if (currentPage == 2 && songsPerPage == 0)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(Pages.First().Embed));
				return;
			}
			foreach (var eP in Pages.Where(x => x.Embed.Fields.Count == 0).ToList())
			{
				Pages.Remove(eP);
			}
			await inter.SendPaginatedResponseAsync(ctx.Interaction, true, false, ctx.User, Pages);
		}

		[SlashCommand("delete", "Delete a playlist")]
		public static async Task DeletePlaylistAsync(InteractionContext ctx, 
			[Option("playlist", "Name of playlist to delete", true), Autocomplete(typeof(AutocompleteProviders.PlaylistProvider))] string playlist
		)
		{
			await ctx.DeferAsync(true);
			if (CheckError(playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Detected error, do you have a playlist?"));
				return;
			}
			var pls = await PlaylistDB.GetPlaylistsSimple(ctx.Member.Id);
			if (!pls.Any(x => x == playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Delete Playlist").WithDescription("**Error** You dont have a playlist with that playlist!").Build()));
				return;
			}
			await PlaylistDB.RemovePlaylist(playlist, ctx.Member.Id);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Delete Playlist").WithDescription("Deleted playlist -> " + playlist).Build()));
		}

		[SlashCommand("rename", "Rename a playlist")]
		public static async Task RenamePlaylistAsync(InteractionContext ctx,
			[Option("playlist", "Name of playlist to rename", true), Autocomplete(typeof(AutocompleteProviders.PlaylistProvider))] string playlist, 
			[Option("name", "New name for playlist")] string name
		)
		{
			await ctx.DeferAsync(true);
			if (CheckError(playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Detected error, do you have a playlist?"));
				return;
			}
			var p = await PlaylistDB.GetPlaylistsSimple(ctx.Member.Id);
			if (!p.Any(x => x == playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Rename Playlist").WithDescription("**Error** You dont have a playlist with that name!").Build()));
				return;
			}
			var pls = await PlaylistDB.GetPlaylist(ctx.Guild, ctx.Member.Id, playlist);
			await pls.GetEntries();
			
			await PlaylistDB.RenameList(playlist, ctx.Member.Id, name);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Rename Playlist").WithDescription($"Renamed Playlist to {playlist} -> {name}!").Build()));
		}

		[SlashCommand("clear", "Clear all entries from a playlist")]
		public static async Task ClearPlaylistAsync(InteractionContext ctx, 
			[Option("playlist", "Name of playlist to clear", true), Autocomplete(typeof(AutocompleteProviders.PlaylistProvider))] string playlist
		)
		{
			await ctx.DeferAsync(true);
			if (CheckError(playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Detected error, do you have a playlist?"));
				return;
			}
			var p = await PlaylistDB.GetPlaylistsSimple(ctx.Member.Id);
			if (!p.Any(x => x == playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Clear Playlist").WithDescription("**Error** You dont have a playlist with that playlist!").Build()));
				return;
			}
			await PlaylistDB.ClearList(playlist, ctx.Member.Id);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Clear Playlist").WithDescription($"Cleared all songs from playlist -> {playlist}!").Build()));
		}

		[SlashCommand("play", "Play a playlist/Add the songs to the queue")]
		[RequireUserVoicechatConnection]
		public static async Task PlayPlaylistAsync(InteractionContext ctx,
			[Option("playlist", "Name of playlist to play", true), Autocomplete(typeof(AutocompleteProviders.PlaylistProvider))] string playlist
		)
		{
			await ctx.DeferAsync(true);
			var ps = await PlaylistDB.GetPlaylists(ctx.Guild, ctx.Member.Id);
			if (!ps.Any(x => x.Key == playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Play Playlist").WithDescription("**Error** You dont have a playlist with that playlist!").Build()));
				return;
			}
			var pls = await PlaylistDB.GetPlaylist(ctx.Guild, ctx.Member.Id, playlist);
			var p = await pls.GetEntries();
			if (!MikuBot.Guilds.Any(x => x.Key == ctx.Guild.Id))
			{
				MikuBot.Guilds.TryAdd(ctx.Guild.Id, new Guild(ctx.Client.ShardId));
			}
			var g = MikuBot.Guilds[ctx.Guild.Id];
			g.musicInstance ??= new MusicInstance(MikuBot.LavalinkNodeConnections[ctx.Client.ShardId], ctx.Client.ShardId);
			await g.ConditionalConnect(ctx);
			g.musicInstance.usedChannel = ctx.Channel;
			await Database.AddToQueue(ctx.Guild, ctx.Member.Id, p);
			if (g.musicInstance.guildConnection.IsConnected && (g.musicInstance.playstate == Playstate.NotPlaying || g.musicInstance.playstate == Playstate.Stopped))
				await g.musicInstance.PlaySong();
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Play Playlist").WithDescription($"Playing playlist/Added to queue!").Build()));
		}
	}

	[SlashCommandGroup("song", "Song management")]
	public class SongOperations : ApplicationCommandsModule
	{
		[SlashCommand("add", "Add a song to a playlist")]
		public static async Task AddSongAsync(InteractionContext ctx, 
			[Option("playlist", "Name of playlist to add song to", true), Autocomplete(typeof(AutocompleteProviders.PlaylistProvider))] string playlist,
			[Option("url_or_search", "Url or name of song to add")] string song
		)
		{
			await ctx.DeferAsync(true);
			if (CheckError(playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Detected error, do you have a playlist?"));
				return;
			}
			var p = await PlaylistDB.GetPlaylistsSimple(ctx.Member.Id);
			if (!p.Any(x => x == playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Add Song").WithDescription("**Error** You dont have a playlist with that playlist!").Build()));
				return;
			}
			var pls = await PlaylistDB.GetPlaylist(ctx.Guild, ctx.Member.Id, playlist);
			if (pls.ExternalService != ExtService.None)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Add Song").WithDescription("**Error** This playlist is a fixed one, you cant add songs to this!").Build()));
				return;
			}
			TrackResult got = await PlaylistDB.GetSong(song, ctx);
			if (got == null)
				return;
			await PlaylistDB.AddEntry(playlist, ctx.Member.Id, got.Tracks);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Add Song").WithDescription($"Added entry -> {got.Tracks[0].Title}!").Build()));
		}

		[SlashCommand("insert_at", "Insert a song into a playlist at a choosen position")]
		public static async Task InsertAtAsync(InteractionContext ctx, 
			[Option("playlist", "Name of playlist to add song to", true), Autocomplete(typeof(AutocompleteProviders.PlaylistProvider))] string playlist, 
			[Option("position", "Position to insert song at" ,true), Autocomplete(typeof(AutocompleteProviders.SongProvider))] string posi,
			[Option("url_or_search", "Url or name of song to add")] string song
		)
		{
			await ctx.DeferAsync(true);
			if (CheckError(playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Detected error, do you have a playlist?"));
				return;
			}
			var pos = Convert.ToInt32(posi);
			var p = await PlaylistDB.GetPlaylistsSimple(ctx.Member.Id);
			if (!p.Any(x => x == playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Insert Song").WithDescription("**Error** You dont have a playlist with that playlist!").Build()));
				return;
			}
			var pls = await PlaylistDB.GetPlaylist(ctx.Guild, ctx.Member.Id, playlist);
			if (pls.ExternalService != ExtService.None)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Insert Song").WithDescription("**Error** This playlist is a fixed one, you cant add songs to this!").Build()));
				return;
			}
			TrackResult got = await PlaylistDB.GetSong(song, ctx);
			if (got == null)
				return;
			got.Tracks.Reverse();
			await PlaylistDB.InsertEntry(ctx.Guild, playlist, ctx.Member.Id, got.Tracks, pos);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Insert Song").WithDescription($"Inserted entry -> {got.Tracks[0].Title} at {pos}!").Build()));
		}

		[SlashCommand("move", "Move a song to a specific position in your playlist")]
		public static async Task MoveSongAsync(InteractionContext ctx,
			[Option("playlist", "Name of playlist to move the song within", true), Autocomplete(typeof(AutocompleteProviders.PlaylistProvider))] string playlist, 
			[Option("old_position", "Position to move the song from", true), Autocomplete(typeof(AutocompleteProviders.SongProvider))] string oldposi, 
			[Option("new_position", "Position to move song to", true), Autocomplete(typeof(AutocompleteProviders.SongProvider))] string newposi
		)
		{
			await ctx.DeferAsync(true);
			if (CheckError(playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Detected error, do you have a playlist?"));
				return;
			}
			var oldpos = Convert.ToInt32(oldposi);
			var newpos = Convert.ToInt32(newposi);
			var p = await PlaylistDB.GetPlaylistsSimple(ctx.Member.Id);
			if (!p.Any(x => x == playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Insert Song").WithDescription("**Error** You dont have a playlist with that playlist!").Build()));
				return;
			}
			var pls = await PlaylistDB.GetPlaylist(ctx.Guild, ctx.Member.Id, playlist);
			if (pls.ExternalService != ExtService.None)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Insert Song").WithDescription("**Error** This playlist is a fixed one, you cant move songs!").Build()));
				return;
			}
			var e = await pls.GetEntries();
			if (e[newpos] == null | e[oldpos] == null)
				return;
			await PlaylistDB.MoveListItems(ctx.Guild, playlist, ctx.Member.Id, oldpos, newpos);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Move Song").WithDescription($"Moved entry -> {e[oldpos].track.Title} to position {newpos}!").Build()));
		}

		[SlashCommand("remove", "Remove a song from a playlist")]
		public static async Task RemoveSongAsync(InteractionContext ctx, 
			[Option("playlist", "Name of playlist to remove the song from", true), Autocomplete(typeof(AutocompleteProviders.PlaylistProvider))] string playlist, 
			[Option("song", "Song to remove", true), Autocomplete(typeof(AutocompleteProviders.SongProvider))] string posi
		)
		{
			await ctx.DeferAsync(true);
			if (CheckError(playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Detected error, do you have a playlist?"));
				return;
			}
			var pos = Convert.ToInt32(posi);
			var p = await PlaylistDB.GetPlaylistsSimple(ctx.Member.Id);
			if (!p.Any(x => x == playlist))
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Remove Song").WithDescription("**Error** You dont have a playlist with that playlist!").Build()));
				return;
			}
			var ents = await PlaylistDB.GetPlaylist(ctx.Guild, ctx.Member.Id, playlist);
			var en = await ents.GetEntries();
			await PlaylistDB.RemoveFromList(ctx.Guild, pos, playlist, ctx.Member.Id);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithTitle("Remove Song").WithDescription($"Entry removed! -> {en[pos].track.Title}").Build()));
		}
	}

	public static bool CheckError(string playlist)
	{
		if (playlist == "error")
			return true;
		else
			return false;
	}
}
