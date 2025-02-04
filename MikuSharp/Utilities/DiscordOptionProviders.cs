using NuGet.Packaging;

namespace MikuSharp.Utilities;

internal class FixedOptionProviders
{
	internal sealed class RepeatModeProvider : ChoiceProvider
	{
		public override Task<IEnumerable<DiscordApplicationCommandOptionChoice>> Provider()
		{
			var list = new List<DiscordApplicationCommandOptionChoice>(3)
			{
				new("None", $"{(int)RepeatMode.None}"),
				new("All", $"{(int)RepeatMode.All}"),
				new("Current", $"{(int)RepeatMode.Current}")
			};
			return Task.FromResult<IEnumerable<DiscordApplicationCommandOptionChoice>>(list);
		}
	}
}

internal class AutocompleteProviders
{
	internal sealed class BanProvider : IAutocompleteProvider
	{
		public async Task<IEnumerable<DiscordApplicationCommandAutocompleteChoice>> Provider(AutocompleteContext ctx)
		{
			var bans = await ctx.Guild.GetBansAsync();
			List<DiscordBan> bannedUsers = new(25);
			bannedUsers.AddRange(ctx.FocusedOption.Value is null
				? bans.Take(25)
				: bans.Where(x => x.User.Username.ToLower().Contains(Convert.ToString(ctx.FocusedOption.Value))).Take(25));

			return bannedUsers.Select(x => new DiscordApplicationCommandAutocompleteChoice(x.User.UsernameWithGlobalName, x.User.Id.ToString()));
		}
	}

	/*
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

	        switch (playlist)
	        {
	            case null:
	                return new List<DiscordApplicationCommandAutocompleteChoice>() { new("You have no playlist selected", "error") };
	            case "error":
	                return new List<DiscordApplicationCommandAutocompleteChoice>() { new("You have no valid playlist selected", "error") };
	        }

	        var pls = await PlaylistDB.GetPlaylist(ctx.Guild, ctx.Member.Id, playlist);
	        var tracks = await pls.GetEntries();
	        List<PlaylistEntry> songs = new(25);
	        if (ctx.FocusedOption.Value == null)
	            songs.AddRange(tracks.Take(25));
	        else if (int.TryParse(Convert.ToString(ctx.FocusedOption.Value), out var pos))
	            songs.AddRange(tracks.Where(x => x.Position.ToString().StartsWith(pos.ToString())).Take(25));
	        else
	            songs.AddRange(tracks.Where(x => x.Track.Info.Title.ToLower().Contains(Convert.ToString(ctx.FocusedOption.Value).ToLower())).Take(25));

	        return songs.Select(x => new DiscordApplicationCommandAutocompleteChoice($"{x.Position}: {x.Track.Info.Title}", x.Position.ToString()));
	    }
	}
	*/

	internal sealed class QueueProvider : IAutocompleteProvider
	{
		public async Task<IEnumerable<DiscordApplicationCommandAutocompleteChoice>> Provider(AutocompleteContext ctx)
		{
			return await ctx.ExecuteWithMusicSessionAsync(async (_, musicSession) =>
			{
				await Task.Delay(0);

				Dictionary<int, LavalinkTrack> queueEntries = [];
				Dictionary<int, LavalinkTrack> filteredQueueEntries = [];

				var queue = musicSession.LavalinkGuildPlayer?.Queue.ToList();
				if (queue is null)
					return [new("The queue is empty", -1)];

				var i = 1;
				foreach (var entry in queue)
				{
					queueEntries[i] = entry;
					i++;
				}

				var value = ctx.FocusedOption.Value as string;
				filteredQueueEntries.AddRange(string.IsNullOrEmpty(value)
					? queueEntries.Take(25)
					: queueEntries.Where(x => x.Value.Info.Title.ToLower().Contains(Convert.ToString(ctx.FocusedOption.Value)!.ToLower())).Take(25));

				return filteredQueueEntries.Select(x => new DiscordApplicationCommandAutocompleteChoice($"{x.Key}: {x.Value.Info.Title}", x.Key - 1));
			}, null, [new("The queue is empty", -1)]);
		}
	}
}
