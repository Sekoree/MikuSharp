namespace MikuSharp.Entities;

public class KsoftSiRanImg : ImgData
{
	[JsonProperty("url")]
	public string Url { get; set; }

	[JsonProperty("snowflake")]
	public string Snowflake { get; set; }

	[JsonProperty("nsfw")]
	public bool IsNsfw { get; set; }

	[JsonProperty("tag")]
	public string Tag { get; set; }
}
