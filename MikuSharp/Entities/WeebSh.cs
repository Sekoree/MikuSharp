using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MikuSharp.Entities
{
    public class WeebSh
    {
        public MemoryStream ImgData { get; set; }
        public string Extension { get; set; }
        public DiscordEmbedBuilder Embed { get; set; }
    }
}
