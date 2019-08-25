using MikuSharp.Enums;
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

        public static ExtService getExtService(string e)
        {
            if (e == "Youtube")
                return ExtService.Youtube;
            else if (e == "Soundcloud")
                return ExtService.Soundcloud;
            else
                return ExtService.None;
        }
    }
}
