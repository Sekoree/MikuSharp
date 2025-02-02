using System.Threading.Tasks;

using DisCatSharp.Lavalink;
using DisCatSharp.Lavalink.Entities;

namespace MikuSharp.Entities;

internal sealed class MusicQueueEntry : IQueueEntry
{
	public ulong UserId { get; internal set; }

	public ulong GuildId { get; internal set; }

	/// <inheritdoc />
	public Task<bool> BeforePlayingAsync(LavalinkGuildPlayer player)
		=> Task.FromResult(true);

	/// <inheritdoc />
	public Task AfterPlayingAsync(LavalinkGuildPlayer player)
		=> Task.FromResult(true);

	/// <inheritdoc />
	public LavalinkTrack Track { get; set; }

	/// <inheritdoc />
	public IQueueEntry AddTrack(LavalinkTrack track)
	{
		this.Track = track;
		dynamic? userData = track.UserData;
		if (userData is null)
			return this;

		this.UserId = userData.UserId;
		this.GuildId = userData.GuildId;
		return this;
	}
}
