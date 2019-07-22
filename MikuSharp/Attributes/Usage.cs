using System;
using System.Collections.Generic;
using System.Text;

namespace MikuSharp.Attributes
{
    public class Usage : Attribute
    {
        public List<string> value { get; set; }
        public Usage(params string[] u)
        {
            value = new List<string>(u);
        }
    }
}
