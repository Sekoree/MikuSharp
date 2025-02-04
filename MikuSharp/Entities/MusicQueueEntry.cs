using MikuSharp.Enums;
using MikuSharp.Utilities;

namespace MikuSharp.Entities;

internal sealed class MusicQueueEntry : IQueueEntry
{
	/// <inheritdoc />
	public async Task<bool> BeforePlayingAsync(LavalinkGuildPlayer player)
	{
		return await player.GuildId.ExecuteWithMusicSessionAsync(async (_, musicSession) =>
		{
			musicSession.UpdatePlaybackState(PlaybackState.Playing);
			await musicSession.UpdateStatusMessageAsync(musicSession.BuildMusicStatusEmbed($"Playing {this.Track.Info.Title.Bold()} from {this.Track.Info.Author.Italic()}"));
			return true;
		}, defaultValue: false);
	}

	/// <inheritdoc />
	public async Task AfterPlayingAsync(LavalinkGuildPlayer player)
	{
		await player.GuildId.ExecuteWithMusicSessionAsync(async (_, musicSession) =>
		{
			musicSession.UpdatePlaybackState(PlaybackState.Stopped);
			await musicSession.UpdateStatusMessageAsync(musicSession.BuildMusicStatusEmbed("Nothing playing"));
		});
	}

	/// <inheritdoc />
	public LavalinkTrack Track { get; set; }
}
