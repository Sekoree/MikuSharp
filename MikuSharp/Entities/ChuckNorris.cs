using System.Collections.Generic;

namespace MikuSharp.Entities
{
    public class ChuckNorrisObject
    {
        public string Type { get; set; }
        public ChuckNorrisObjectValue Value { get; set; }
    }
    public class ChuckNorrisObjectValue
    {
        public int Id { get; set; }
        public string Joke { get; set; }
        public List<string> Categories { get; set; }
    }

}
