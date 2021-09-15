using DisCatSharp.Entities;
using DisCatSharp.Lavalink;
using DisCatSharp.Lavalink.EventArgs;

using MikuSharp.Enums;
using MikuSharp.Utilities;

using System;
using System.Threading.Tasks;

namespace MikuSharp.Events
{
    public class Lavalink
    {
        public static async Task LavalinkTrackFinish(LavalinkGuildConnection lava, TrackFinishEventArgs e)
        {
            try
            {
                var g = Bot.Guilds[e.Player.Guild.Id];
                var lastPlayedSongs = await Database.GetLPL(e.Player.Guild);
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
                            await Database.RemoveFromQueue(g.musicInstance.currentSong.position, e.Player.Guild);
                            if (lastPlayedSongs.Count == 0)
                            {
                                await Database.AddToLPL(e.Player.Guild.Id, g.musicInstance.currentSong.track.TrackString);
                            }
                            else if (lastPlayedSongs[0].track.Uri != g.musicInstance.currentSong.track.Uri)
                            {
                                await Database.AddToLPL(e.Player.Guild.Id, g.musicInstance.currentSong.track.TrackString);
                            }
                            g.musicInstance.lastSong = g.musicInstance.currentSong;
                            g.musicInstance.currentSong = null;
                            var queue = await Database.GetQueue(e.Player.Guild);
                            if (queue.Count != 0) await g.musicInstance.PlaySong();
                            else g.musicInstance.playstate = Playstate.NotPlaying;
                            break;
                        }
                    case TrackEndReason.Finished:
                        {
                            g.musicInstance.guildConnection.PlaybackFinished -= LavalinkTrackFinish;
                            if (g.musicInstance.repeatMode != RepeatMode.On && g.musicInstance.repeatMode != RepeatMode.All) await Database.RemoveFromQueue(g.musicInstance.currentSong.position, e.Player.Guild);
                            if (lastPlayedSongs.Count == 0)
                            {
                                await Database.AddToLPL(e.Player.Guild.Id, g.musicInstance.currentSong.track.TrackString);
                            }
                            else if (lastPlayedSongs[0].track.Uri != g.musicInstance.currentSong.track.Uri)
                            {
                                await Database.AddToLPL(e.Player.Guild.Id, g.musicInstance.currentSong.track.TrackString);
                            }
                            g.musicInstance.lastSong = g.musicInstance.currentSong;
                            g.musicInstance.currentSong = null;
                            var queue = await Database.GetQueue(e.Player.Guild);
                            if (queue.Count != 0) await g.musicInstance.PlaySong();
                            else g.musicInstance.playstate = Playstate.NotPlaying;
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

    }
}
