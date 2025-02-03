using System.IO;

using DisCatSharp.Entities;

namespace MikuSharp.Entities;

public class ImgData
{
	public Stream Data { get; set; }
	public string Filetype { get; set; }

	public DiscordEmbed Embed { get; set; }
}
