namespace MikuSharp.Entities;

public sealed class KsoftSiRanImg : Img_Data
{
	public string url { get; set; }
	public string snowflake { get; set; }
	public bool nsfw { get; set; }
	public string tag { get; set; }
}