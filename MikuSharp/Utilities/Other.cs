using System;
using System.Collections.Generic;
using System.Text;

namespace MikuSharp.Utilities
{
    public class Other
    {
        public static string resizeLink(string url)
        {
            return $"https://api.meek.moe/im/?image={url}&resize=500";
        }
    }
}
