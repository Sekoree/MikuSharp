namespace MikuSharp.Entities;

public sealed class KsoftSiRanImg : ImgData
{
	public string Url { get; set; }
	public string Snowflake { get; set; }
	public bool Nsfw { get; set; }
	public string Tag { get; set; }
}