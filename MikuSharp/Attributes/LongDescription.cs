using System;
using System.Collections.Generic;
using System.Text;

namespace MikuSharp.Attributes
{
    public class LongDescription : Attribute
    {
        public string value { get; set; }
        public LongDescription(string d)
        {
            value = d;
        }
    }
}
