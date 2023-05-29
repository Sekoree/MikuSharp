namespace MikuSharp.Entities;

internal class MotionGuildCount
{
	[JsonProperty("guilds")]
	internal int Guilds { get; set; }

	internal MotionGuildCount(int count)
	{
		this.Guilds = count;
	}
}
