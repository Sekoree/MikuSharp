using System.Threading.Tasks;

using DisCatSharp.Lavalink;
using DisCatSharp.Lavalink.Entities;

using MikuSharp.Utilities;

namespace MikuSharp.Entities;

internal sealed class MusicQueueEntry : IQueueEntry
{
	/// <inheritdoc />
	public async Task<bool> BeforePlayingAsync(LavalinkGuildPlayer player)
	{
		var musicSession = MikuBot.MusicSessions[player.GuildId];
		await musicSession.UpdateStatusMessageAsync(musicSession.BuildMusicStatusEmbed($"Playing {this.Track.Info.Title} from {this.Track.Info.Author}"));
		return true;
	}

	/// <inheritdoc />
	public async Task AfterPlayingAsync(LavalinkGuildPlayer player)
	{
		var musicSession = MikuBot.MusicSessions[player.GuildId];
		await musicSession.UpdateStatusMessageAsync(musicSession.BuildMusicStatusEmbed());
	}

	/// <inheritdoc />
	public LavalinkTrack Track { get; set; }
}
