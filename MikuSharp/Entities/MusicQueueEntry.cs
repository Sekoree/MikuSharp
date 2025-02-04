using System.Threading.Tasks;

using DisCatSharp;
using DisCatSharp.Lavalink;
using DisCatSharp.Lavalink.Entities;

using MikuSharp.Enums;
using MikuSharp.Utilities;

namespace MikuSharp.Entities;

internal sealed class MusicQueueEntry : IQueueEntry
{
	/// <inheritdoc />
	public async Task<bool> BeforePlayingAsync(LavalinkGuildPlayer player)
	{
		var guildId = player.GuildId;
		var asyncLock = MikuBot.MusicSessionLocks.GetOrAdd(guildId, _ => new());
		using (await asyncLock.LockAsync(MikuBot.Cts.Token))
		{
			if (!MikuBot.MusicSessions.TryGetValue(guildId, out var musicSession))
				return false;

			musicSession.UpdatePlaybackState(PlaybackState.Playing);
			await musicSession.UpdateStatusMessageAsync(musicSession.BuildMusicStatusEmbed($"Playing {this.Track.Info.Title.Bold()} from {this.Track.Info.Author.Italic()}"));
			return true;
		}
	}

	/// <inheritdoc />
	public async Task AfterPlayingAsync(LavalinkGuildPlayer player)
	{
		var guildId = player.GuildId;
		var asyncLock = MikuBot.MusicSessionLocks.GetOrAdd(guildId, _ => new());
		using (await asyncLock.LockAsync(MikuBot.Cts.Token))
		{
			if (MikuBot.MusicSessions.TryGetValue(guildId, out var musicSession))
			{
				musicSession.UpdatePlaybackState(PlaybackState.Stopped);
				await musicSession.UpdateStatusMessageAsync(musicSession.BuildMusicStatusEmbed());
			}
		}
	}

	/// <inheritdoc />
	public LavalinkTrack Track { get; set; }
}
