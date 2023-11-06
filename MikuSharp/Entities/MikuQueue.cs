using DisCatSharp.Lavalink.Entities;

namespace MikuSharp.Entities
{
	internal class MikuQueue : IQueueEntry
	{
		public async Task<bool> BeforePlayingAsync(LavalinkGuildPlayer player)
		{
			await player.Channel.SendMessageAsync($"Playing {this.Track.Info.Title}");
			return true;
		}

		public async Task AfterPlayingAsync(LavalinkGuildPlayer player)
			=> await player.Channel.SendMessageAsync($"Finished playing {this.Track.Info.Title}");

		public LavalinkTrack Track { get; set; }
	}
}
