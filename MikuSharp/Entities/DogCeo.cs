namespace MikuSharp.Entities;

public class DogCeo
{
	[JsonProperty("status")]
	public string Status { get; set; }

	[JsonProperty("message")]
	public string Message { get; set; }
}
