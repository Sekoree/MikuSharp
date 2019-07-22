using System;
using System.Collections.Generic;
using System.Text;

namespace MikuSharp.Entities
{
    public class KsoftSiRanImg : Img_Data
    {
        public string url { get; set; }
        public string snowflake { get; set; }
        public bool nsfw { get; set; }
        public string tag { get; set; }
    }
}
