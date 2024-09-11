using System.IO;

using DisCatSharp.Entities;

namespace MikuSharp.Entities;

public sealed class WeebSh
{
    public MemoryStream ImgData { get; set; }
    public string Extension { get; set; }
    public DiscordEmbedBuilder Embed { get; set; }
}
