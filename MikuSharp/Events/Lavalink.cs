/*using DisCatSharp.Entities;
using DisCatSharp.Lavalink;
using DisCatSharp.Lavalink.EventArgs;

using Microsoft.Extensions.Logging;

using MikuSharp.Enums;
using MikuSharp.Utilities;

using System;
using System.Threading.Tasks;

namespace MikuSharp.Events;

public class Lavalink
{
	public static async Task LavalinkTrackFinish(LavalinkGuildConnection lava, TrackFinishEventArgs e)
	{
		try
		{
			var g = MikuBot.Guilds[e.Player.Guild.Id];
			var lastPlayedSongs = await Database.GetLastPlayingListAsync(e.Player.Guild);
			if (g.musicInstance == null)
				return;
			switch (e.Reason)
			{
				case TrackEndReason.Stopped:
					{
						g.musicInstance.playstate = Playstate.Stopped;
						g.musicInstance.guildConnection.PlaybackFinished -= LavalinkTrackFinish;
						g.musicInstance.lastSong = g.musicInstance.currentSong;
						g.musicInstance.currentSong = null;
						break;
					}
				case TrackEndReason.Replaced:
					{
						break;
					}
				case TrackEndReason.LoadFailed:
					{
						await g.musicInstance.usedChannel.SendMessageAsync(embed: new DiscordEmbedBuilder().WithTitle("Track failed to play")
							.WithDescription($"**{g.musicInstance.currentSong.track.Title}**\nby {g.musicInstance.currentSong.track.Author}\n" +
							$"**Failed to load, Skipping to next track**"));
						g.musicInstance.guildConnection.PlaybackFinished -= LavalinkTrackFinish;
						await Database.RemoveFromQueueAsync(g.musicInstance.currentSong.position, e.Player.Guild);
						if (lastPlayedSongs.Count == 0)
						{
							await Database.AddToLastPlayingListAsync(e.Player.Guild.Id, g.musicInstance.currentSong.track.TrackString);
						}
						else if (lastPlayedSongs[0].track.Uri != g.musicInstance.currentSong.track.Uri)
						{
							await Database.AddToLastPlayingListAsync(e.Player.Guild.Id, g.musicInstance.currentSong.track.TrackString);
						}
						g.musicInstance.lastSong = g.musicInstance.currentSong;
						g.musicInstance.currentSong = null;
						var queue = await Database.GetQueueAsync(e.Player.Guild);
						if (queue.Count != 0) await g.musicInstance.PlaySong();
						else g.musicInstance.playstate = Playstate.NotPlaying;
						break;
					}
				case TrackEndReason.Finished:
					{
						g.musicInstance.guildConnection.PlaybackFinished -= LavalinkTrackFinish;
						if (g.musicInstance.repeatMode != RepeatMode.On && g.musicInstance.repeatMode != RepeatMode.All) await Database.RemoveFromQueueAsync(g.musicInstance.currentSong.position, e.Player.Guild);
						if (lastPlayedSongs.Count == 0)
						{
							await Database.AddToLastPlayingListAsync(e.Player.Guild.Id, g.musicInstance.currentSong.track.TrackString);
						}
						else if (lastPlayedSongs[0].track.Uri != g.musicInstance.currentSong.track.Uri)
						{
							await Database.AddToLastPlayingListAsync(e.Player.Guild.Id, g.musicInstance.currentSong.track.TrackString);
						}
						g.musicInstance.lastSong = g.musicInstance.currentSong;
						g.musicInstance.currentSong = null;
						var queue = await Database.GetQueueAsync(e.Player.Guild);
						if (queue.Count != 0) await g.musicInstance.PlaySong();
						else g.musicInstance.playstate = Playstate.NotPlaying;
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
*/