namespace MikuSharp.Entities;

public sealed class NekoBot : ImgData
{
	public string Message { get; set; }
	public int Status { get; set; }
	public bool Success { get; set; }
}