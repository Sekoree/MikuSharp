using MikuSharp.Enums;
using MikuSharp.Utilities;

namespace MikuSharp.Events;

public class Lavalink
{
	public static async Task LavalinkTrackFinish(LavalinkGuildConnection lava, TrackFinishEventArgs e)
	{
		try
		{
			var g = MikuBot.Guilds[e.Player.Guild.Id];
			var lastPlayedSongs = await Database.GetLastPlayingListAsync(e.Player.Guild);
			if (g.MusicInstance == null)
				return;
			switch (e.Reason)
			{
				case TrackEndReason.Stopped:
				{
					g.MusicInstance.Playstate = PlayState.Stopped;
					g.MusicInstance.GuildConnection.PlaybackFinished -= LavalinkTrackFinish;
					g.MusicInstance.LastSong = g.MusicInstance.CurrentSong;
					g.MusicInstance.CurrentSong = null;
					break;
				}
				case TrackEndReason.Replaced:
				{
					break;
				}
				case TrackEndReason.LoadFailed:
				{
					await g.MusicInstance.CommandChannel.SendMessageAsync(embed: new DiscordEmbedBuilder().WithTitle("Track failed to play")
						.WithDescription($"**{g.MusicInstance.CurrentSong.Track.Title}**\nby {g.MusicInstance.CurrentSong.Track.Author}\n" +
						$"**Failed to load, Skipping to next track**"));
					g.MusicInstance.GuildConnection.PlaybackFinished -= LavalinkTrackFinish;
					await Database.RemoveFromQueueAsync(g.MusicInstance.CurrentSong.Position, e.Player.Guild);
					if (lastPlayedSongs.Count == 0)
					{
						await Database.AddToLastPlayingListAsync(e.Player.Guild.Id, g.MusicInstance.CurrentSong.Track.TrackString);
					}
					else if (lastPlayedSongs[0].Track.Uri != g.MusicInstance.CurrentSong.Track.Uri)
					{
						await Database.AddToLastPlayingListAsync(e.Player.Guild.Id, g.MusicInstance.CurrentSong.Track.TrackString);
					}
					g.MusicInstance.LastSong = g.MusicInstance.CurrentSong;
					g.MusicInstance.CurrentSong = null;
					var queue = await Database.GetQueueAsync(e.Player.Guild);
					if (queue.Count != 0)
						await g.MusicInstance.PlaySong();
					else
						g.MusicInstance.Playstate = PlayState.NotPlaying;
					break;
				}
				case TrackEndReason.Finished:
				{
					g.MusicInstance.GuildConnection.PlaybackFinished -= LavalinkTrackFinish;
					if (g.MusicInstance.RepeatMode != RepeatMode.On && g.MusicInstance.RepeatMode != RepeatMode.All)
						await Database.RemoveFromQueueAsync(g.MusicInstance.CurrentSong.Position, e.Player.Guild);
					if (lastPlayedSongs.Count == 0)
					{
						await Database.AddToLastPlayingListAsync(e.Player.Guild.Id, g.MusicInstance.CurrentSong.Track.TrackString);
					}
					else if (lastPlayedSongs[0].Track.Uri != g.MusicInstance.CurrentSong.Track.Uri)
					{
						await Database.AddToLastPlayingListAsync(e.Player.Guild.Id, g.MusicInstance.CurrentSong.Track.TrackString);
					}
					g.MusicInstance.LastSong = g.MusicInstance.CurrentSong;
					g.MusicInstance.CurrentSong = null;
					var queue = await Database.GetQueueAsync(e.Player.Guild);
					if (queue.Count != 0)
						await g.MusicInstance.PlaySong();
					else
						g.MusicInstance.Playstate = PlayState.NotPlaying;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			MikuBot.ShardedClient.Logger.LogError(ex.Message);
			MikuBot.ShardedClient.Logger.LogError(ex.StackTrace);
		}
	}

}
