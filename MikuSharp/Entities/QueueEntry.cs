using DisCatSharp.Lavalink.Entities;

namespace MikuSharp.Entities;

public class QueueEntry : Entry
{
	public int Position { get; set; }

	public ulong AddedBy { set; get; }

	public QueueEntry(LavalinkTrack track, ulong memberId, DateTimeOffset additionDate, int position)
		: base(track, additionDate)
	{
		this.Position = position;
		this.AddedBy = memberId;
	}
}
