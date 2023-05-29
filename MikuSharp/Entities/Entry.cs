namespace MikuSharp.Entities;

public class Entry
{
	public LavalinkTrack Track { get; protected set; }

	public DateTimeOffset AdditionDate { get; protected set; }

	public Entry(LavalinkTrack track, DateTimeOffset additionDate)
	{
		this.Track = track;
		this.AdditionDate = additionDate;
	}
}
