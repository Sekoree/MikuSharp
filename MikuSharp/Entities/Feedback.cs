namespace MikuSharp.Entities;

/// <summary>
/// Represents an <see cref="MikuBot"/> <see cref="Feedback"/> object.
/// </summary>
public class Feedback
{
	/// <summary>
	/// Gets or sets the unique feedback id.
	/// </summary>
	public long? Ufid { get; internal set; }

	/// <summary>
	/// Gets the user id who submitted the feedback.
	/// </summary>
	public ulong UserId { get; private set; }

	/// <summary>
	/// Gets the feedback message.
	/// </summary>
	public string Message { get; private set; }

	/// <summary>
	/// Gets the rating, if any.
	/// </summary>
	public short? Rating { get; private set; }

	/// <summary>
	/// Gets when the feedback was send.
	/// </summary>
	public DateTimeOffset SendAt { get; private set; }

	/// <summary>
	/// Gets the feedback metadata.
	/// </summary>
	public FeedbackMetadata? Metadata { get; internal set; } = null;

	/// <summary>
	/// Constructs a new <see cref="Feedback"/> object.
	/// </summary>
	/// <param name="ufid">The unique feedback id or null.</param>
	/// <param name="userId">The user id.</param>
	/// <param name="message">The feedback message.</param>
	/// <param name="rating">The feedback rating.</param>
	/// <param name="sendAt">Dto when the feedback was send.</param>
	public Feedback(long? ufid, ulong userId, string message, short? rating, DateTimeOffset sendAt)
	{
		this.Ufid = ufid;
		this.UserId = userId;
		this.Message = message;
		this.Rating = rating;
		this.SendAt = sendAt;
	}

	public Feedback AttachMetadata(FeedbackMetadata metadata)
	{
		this.Metadata = metadata;
		return this;
	}

	/// <summary>
	/// Returns a string that represents the current object.
	/// </summary>
	/// <returns>A string that represents the current object.</returns>
	public override string ToString()
		=> $"**Feedback from {this.UserId}**\n\n- Message:\n```\n{this.Message}\n```\n\n- Rating: {this.Rating?.ToString() ?? "Not given"}\n- Send at: {Formatter.Timestamp(this.SendAt, TimestampFormat.LongDateTime)}\n- Unique ID: {this.Ufid ?? new Random().NextInt64()}\n\n\n{this.Metadata}";
}

/// <summary>
/// Represents an <see cref="FeedbackMetadata"/> object.
/// </summary>
public class FeedbackMetadata
{
	/// <summary>
	/// Gets the unique feedback id.
	/// </summary>
	public long? Ufid { get; internal set; }

	/// <summary>
	/// Gets the type of location the feedback came from.
	/// </summary>
	public int Type { get; private set; }

	/// <summary>
	/// Gets the guild id the feedback came from, if applicable.
	/// </summary>
	public ulong? GuildId { get; private set; }

	/// <summary>
	/// Gets the dm id the feedback came from, if applicable.
	/// </summary>
	public ulong? DmId { get; internal set; }

	/// <summary>
	/// Gets the thread id the feedback was send to.
	/// </summary>
	public ulong? ThreadId { get; internal set; }

	/// <summary>
	/// Gets whether a response was already send.
	/// </summary>
	public bool ResponseSend { get; internal set; }

	/// <summary>
	/// Constructs a new <see cref="FeedbackMetadata"/> object.
	/// </summary>
	/// <param name="ufid">The unique feedback id, if any. Defaults to <see langword="null"/>.</param>
	/// <param name="type">The type of location the feedback came from. Defaults to <c>0</c>.</param>
	/// <param name="guildId">The guild id the feedback came from, if any. Defaults to <see langword="null"/>.</param>
	/// <param name="dmId">The dm id the feedback came from, if any. Defaults to <see langword="null"/>.</param>
	/// <param name="threadId">The thread id the feedback was send to, if any. Defaults to <see langword="null"/>.</param>
	/// <param name="responseSend">Whether a response was already send. Defaults to <see langword="false"/>.</param>
	public FeedbackMetadata(long? ufid = null, int type = 0, ulong? guildId = null, ulong? dmId = null, ulong? threadId = null, bool responseSend = false)
	{
		this.Ufid = ufid;
		this.Type = type;
		this.GuildId = guildId;
		this.DmId = dmId;
		this.ThreadId = threadId;
		this.ResponseSend = responseSend;
	}

	/// <summary>
	/// Returns a string that represents the current object.
	/// </summary>
	/// <returns>A string that represents the current object.</returns>
	public override string ToString()
		=> $"**Metadata**\n\n- Type: {this.Type}\n\n- Guild Id: {this.GuildId?.ToString() ?? "Not applicable"}\n\n- Dm Id: {this.DmId?.ToString() ?? "Not yet applicable"}\n\n- Thread Id: {this.ThreadId?.ToString() ?? "Not yet applicable"}\n\n- Response Send: {this.ResponseSend}";

}
