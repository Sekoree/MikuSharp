using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;

using MikuSharp.Entities;
using MikuSharp.Enums;

namespace MikuSharp.Utilities;

internal class FixedOptionProviders
{
	internal class RepeatModeProvider : ChoiceProvider
	{
		public override Task<IEnumerable<DiscordApplicationCommandOptionChoice>> Provider()
		{
			var list = new List<DiscordApplicationCommandOptionChoice>(3)
			{
				new DiscordApplicationCommandOptionChoice("Off", $"{(int)RepeatMode.Off}"),
				new DiscordApplicationCommandOptionChoice("On", $"{(int)RepeatMode.On}"),
				new DiscordApplicationCommandOptionChoice("All", $"{(int)RepeatMode.All}"),
			};
			return Task.FromResult<IEnumerable<DiscordApplicationCommandOptionChoice>>(list);
		}
	}
}

internal class AutocompleteProviders
{
	internal class BanProvider : IAutocompleteProvider
	{
		public async Task<IEnumerable<DiscordApplicationCommandAutocompleteChoice>> Provider(AutocompleteContext ctx)
		{
			var bans = await ctx.Guild.GetBansAsync();
			List<DiscordBan> bannedUsers = new(25);
			if (ctx.FocusedOption.Value == null)
				bannedUsers.AddRange(bans.Take(25));
			else
				bannedUsers.AddRange(bans.Where(x => x.User.Username.ToLower().Contains(Convert.ToString(ctx.FocusedOption.Value))).Take(25));

			return bannedUsers.Select(x => new DiscordApplicationCommandAutocompleteChoice(x.User.UsernameWithDiscriminator, x.User.Id.ToString()));
		}
	}

	internal class PlaylistProvider : IAutocompleteProvider
	{
		public async Task<IEnumerable<DiscordApplicationCommandAutocompleteChoice>> Provider(AutocompleteContext ctx)
		{
			var plls = await PlaylistDB.GetPlaylistsSimple(ctx.Member.Id);
			if (plls.Count == 0)
				return new List<DiscordApplicationCommandAutocompleteChoice>() { new("You have no songs", "error") };
			var DbPlaylists = await PlaylistDB.GetPlaylists(ctx.Guild, ctx.Member.Id);

			List<KeyValuePair<string, Playlist>> playlists = new(25);
			if (ctx.FocusedOption.Value == null)
				playlists.AddRange(DbPlaylists.Take(25));
			else
				playlists.AddRange(DbPlaylists.Where(x => x.Value.Name.Contains(Convert.ToString(ctx.FocusedOption.Value).ToLower())).Take(25));

			return playlists.Select(x => new DiscordApplicationCommandAutocompleteChoice(x.Value.Name, x.Key));
		}
	}

	internal class SongProvider : IAutocompleteProvider
	{
		public async Task<IEnumerable<DiscordApplicationCommandAutocompleteChoice>> Provider(AutocompleteContext ctx)
		{
			var playlist = Convert.ToString(ctx.Options.First(x => x.Name == "playlist").Value);
			if (playlist == null)
				return new List<DiscordApplicationCommandAutocompleteChoice>() { new("You have no playlist selected", "error") };
			if (playlist == "error")
				return new List<DiscordApplicationCommandAutocompleteChoice>() { new("You have no valid playlist selected", "error") };

			var pls = await PlaylistDB.GetPlaylist(ctx.Guild, ctx.Member.Id, playlist);
			var tracks = await pls.GetEntries();
			List<PlaylistEntry> songs = new(25);
			if (ctx.FocusedOption.Value == null)
				songs.AddRange(tracks.Take(25));
			else if (int.TryParse(Convert.ToString(ctx.FocusedOption.Value), out var pos))
				songs.AddRange(tracks.Where(x => x.Position.ToString().StartsWith(pos.ToString())).Take(25));
			else
				songs.AddRange(tracks.Where(x => x.track.Title.ToLower().Contains(Convert.ToString(ctx.FocusedOption.Value).ToLower())).Take(25));

			return songs.Select(x => new DiscordApplicationCommandAutocompleteChoice($"{x.Position}: {x.track.Title}", x.Position.ToString()));
		}
	}

	internal class QueueProvider : IAutocompleteProvider
	{
		public async Task<IEnumerable<DiscordApplicationCommandAutocompleteChoice>> Provider(AutocompleteContext ctx)
		{
			var queue = await Database.GetQueueAsync(ctx.Guild);
			List<QueueEntry> songs = new(25);
			if (ctx.FocusedOption.Value == null)
				songs.AddRange(queue.Take(25));
			else if (int.TryParse(Convert.ToString(ctx.FocusedOption.Value), out var pos))
				songs.AddRange(queue.Where(x => x.position.ToString().StartsWith(pos.ToString())).Take(25));
			else
				songs.AddRange(queue.Where(x => x.track.Title.ToLower().Contains(Convert.ToString(ctx.FocusedOption.Value).ToLower())).Take(25));

			return songs.Select(x => new DiscordApplicationCommandAutocompleteChoice($"{x.position}: {x.track.Title}", x.position.ToString()));
		}
	}
}
