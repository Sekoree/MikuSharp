namespace MikuSharp.Entities;

public class Feedback
{
	public long? Ufid { get; internal set; }
	public ulong UserId { get; private set; }
	public string Message { get; private set; }
	public short? Rating { get; private set; }
	public DateTimeOffset Created { get; private set; }

	public Feedback(long? ufid, ulong userId, string message, short? rating, DateTimeOffset created)
	{
		this.Ufid = ufid;
		this.UserId = userId;
		this.Message = message;
		this.Rating = rating;
		this.Created = created;
	}

	public override string ToString()
		=> $"**Feedback from {this.UserId}**\n\n- Message:\n```\n{this.Message}\n```\n\n- Rating: {this.Rating?.ToString() ?? "Not given"}\n- Send at: {Formatter.Timestamp(this.Created, TimestampFormat.LongDateTime)}\n- Unique ID: {this.Ufid ?? new Random().NextInt64()}";
}
