using System;
using System.Threading.Tasks;

using DisCatSharp.Entities;
using DisCatSharp.Lavalink;
using DisCatSharp.Lavalink.Enums;
using DisCatSharp.Lavalink.EventArgs;

using Microsoft.Extensions.Logging;

using MikuSharp.Enums;
using MikuSharp.Utilities;

namespace MikuSharp.Events;

public class Lavalink
{
    public static async Task LavalinkTrackFinish(LavalinkGuildPlayer lava, LavalinkTrackEndedEventArgs e)
    {
        try
        {
            var g = MikuBot.Guilds[e.GuildId];
            var lastPlayedSongs = await Database.GetLastPlayingListAsync(e.Guild);
            if (g.MusicInstance == null!)
                return;

            switch (e.Reason)
            {
                case LavalinkTrackEndReason.Stopped:
                {
                    g.MusicInstance.Playstate = Playstate.Stopped;
                    g.MusicInstance.GuildConnection.TrackEnded -= LavalinkTrackFinish;
                    g.MusicInstance.LastSong = g.MusicInstance.CurrentSong;
                    g.MusicInstance.CurrentSong = null;
                    break;
                }
                case LavalinkTrackEndReason.Replaced:
                {
                    break;
                }
                case LavalinkTrackEndReason.LoadFailed:
                {
                    await g.MusicInstance.UsedChannel.SendMessageAsync(new DiscordEmbedBuilder().WithTitle("Track failed to play")
                        .WithDescription($"**{g.MusicInstance.CurrentSong.Track.Info.Title}**\nby {g.MusicInstance.CurrentSong.Track.Info.Author}\n" + $"**Failed to load, Skipping to next Track**"));
                    g.MusicInstance.GuildConnection.TrackEnded -= LavalinkTrackFinish;
                    await Database.RemoveFromQueueAsync(g.MusicInstance.CurrentSong.Position, e.Guild);
                    if (lastPlayedSongs.Count == 0)
                        await Database.AddToLastPlayingListAsync(e.GuildId, g.MusicInstance.CurrentSong.Track.Encoded);
                    else if (lastPlayedSongs[0].Track.Info.Uri != g.MusicInstance.CurrentSong.Track.Info.Uri)
                        await Database.AddToLastPlayingListAsync(e.GuildId, g.MusicInstance.CurrentSong.Track.Encoded);
                    g.MusicInstance.LastSong = g.MusicInstance.CurrentSong;
                    g.MusicInstance.CurrentSong = null;
                    var queue = await Database.GetQueueAsync(e.Guild);
                    if (queue.Count != 0) await g.MusicInstance.PlaySong();
                    else g.MusicInstance.Playstate = Playstate.NotPlaying;
                    break;
                }
                case LavalinkTrackEndReason.Finished:
                {
                    g.MusicInstance.GuildConnection.TrackEnded -= LavalinkTrackFinish;
                    if (g.MusicInstance.RepeatMode != RepeatMode.On && g.MusicInstance.RepeatMode != RepeatMode.All)
                        await Database.RemoveFromQueueAsync(g.MusicInstance.CurrentSong.Position, e.Guild);
                    if (lastPlayedSongs.Count == 0)
                        await Database.AddToLastPlayingListAsync(e.GuildId, g.MusicInstance.CurrentSong.Track.Encoded);
                    else if (lastPlayedSongs[0].Track.Info.Uri != g.MusicInstance.CurrentSong.Track.Info.Uri)
                        await Database.AddToLastPlayingListAsync(e.GuildId, g.MusicInstance.CurrentSong.Track.Encoded);
                    g.MusicInstance.LastSong = g.MusicInstance.CurrentSong;
                    g.MusicInstance.CurrentSong = null;
                    var queue = await Database.GetQueueAsync(e.Guild);
                    if (queue.Count != 0) await g.MusicInstance.PlaySong();
                    else g.MusicInstance.Playstate = Playstate.NotPlaying;
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
