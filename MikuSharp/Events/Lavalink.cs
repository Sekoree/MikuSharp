using DisCatSharp.Lavalink.Enums;

using MikuSharp.Enums;
using MikuSharp.Utilities;

namespace MikuSharp.Events;

public class Lavalink
{
	public static async Task LavalinkTrackFinished(LavalinkGuildPlayer guildConnection, LavalinkTrackEndedEventArgs eventArgs)
	{
		try
		{
			var g = MikuBot.Guilds[eventArgs.Guild.Id];
			var lastPlayedSongs = await Database.GetLastPlayingListAsync(eventArgs.Guild);
			if (g.MusicInstance == null)
				return;
			switch (eventArgs.Reason)
			{
				case LavalinkTrackEndReason.Stopped:
				{
					g.MusicInstance.PlayState = PlayState.Stopped;
					g.MusicInstance.GuildPlayer.TrackEnded -= LavalinkTrackFinished;
					g.MusicInstance.LastSong = g.MusicInstance.CurrentSong;
					g.MusicInstance.CurrentSong = null;
					break;
				}
				case LavalinkTrackEndReason.Replaced:
					break;
				case LavalinkTrackEndReason.LoadFailed:
				{
					await g.MusicInstance.CommandChannel.SendMessageAsync(embed: new DiscordEmbedBuilder().WithTitle("Track failed to play")
						.WithDescription($"**{g.MusicInstance.CurrentSong.Track.Info.Title}**\nby {g.MusicInstance.CurrentSong.Track.Info.Author}\n" +
						$"**Failed to load, Skipping to next track**"));
					g.MusicInstance.GuildPlayer.TrackEnded -= LavalinkTrackFinished;
					await Database.RemoveFromQueueAsync(g.MusicInstance.CurrentSong.Position, eventArgs.Guild);
					if (lastPlayedSongs.Count == 0)
						await Database.AddToLastPlayingListAsync(eventArgs.Guild.Id, g.MusicInstance.CurrentSong.Track.Encoded);
					else if (lastPlayedSongs[0].Track.Info.Uri != g.MusicInstance.CurrentSong.Track.Info.Uri)
						await Database.AddToLastPlayingListAsync(eventArgs.Guild.Id, g.MusicInstance.CurrentSong.Track.Encoded);
					g.MusicInstance.LastSong = g.MusicInstance.CurrentSong;
					g.MusicInstance.CurrentSong = null;
					var queue = await Database.GetQueueAsync(eventArgs.Guild);
					if (queue.Count != 0)
						await g.MusicInstance.PlaySong();
					else
						g.MusicInstance.PlayState = PlayState.NotPlaying;
					break;
				}
				case LavalinkTrackEndReason.Finished:
				{
					g.MusicInstance.GuildPlayer.TrackEnded -= LavalinkTrackFinished;
					if (g.MusicInstance.Config.RepeatMode != RepeatMode.On && g.MusicInstance.Config.RepeatMode != RepeatMode.All)
						await Database.RemoveFromQueueAsync(g.MusicInstance.CurrentSong.Position, eventArgs.Guild);
					if (lastPlayedSongs.Count == 0)
						await Database.AddToLastPlayingListAsync(eventArgs.Guild.Id, g.MusicInstance.CurrentSong.Track.Encoded);
					else if (lastPlayedSongs[0].Track.Info.Uri != g.MusicInstance.CurrentSong.Track.Info.Uri)
						await Database.AddToLastPlayingListAsync(eventArgs.Guild.Id, g.MusicInstance.CurrentSong.Track.Encoded);
					g.MusicInstance.LastSong = g.MusicInstance.CurrentSong;
					g.MusicInstance.CurrentSong = null;
					var queue = await Database.GetQueueAsync(eventArgs.Guild);
					if (queue.Count != 0)
						await g.MusicInstance.PlaySong();
					else
						g.MusicInstance.PlayState = PlayState.NotPlaying;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			MikuBot.ShardedClient.Logger.LogError("{msg}", ex.Message);
			MikuBot.ShardedClient.Logger.LogError("{stack}", ex.StackTrace);
		}
	}

}
