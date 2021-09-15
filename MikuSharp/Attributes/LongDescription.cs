using System;

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
