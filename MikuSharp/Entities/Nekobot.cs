using System;
using System.Collections.Generic;
using System.Text;

namespace MikuSharp.Entities
{
    public class NekoBot : Img_Data
    {
        public string message { get; set; }
        public int status { get; set; }
        public bool success { get; set; }
    }
}
