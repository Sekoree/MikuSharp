using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MikuSharp.Entities
{
    public class Img_Data
    {
        public Stream Data { get; set; }
        public string Filetype { get; set; }

        public DiscordEmbed Embed { get; set; }
    }
}
