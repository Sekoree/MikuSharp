namespace MikuSharp.Entities;

public class NekoBot : ImgData
{
	[JsonProperty("status")]
	public string Message { get; set; }

	[JsonProperty("status")]
	public int Status { get; set; }

	[JsonProperty("success")]
	public bool IsSuccess { get; set; }
}
