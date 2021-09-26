using System;

namespace MikuSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var b = new Bot())
            {
                b.RunBot().Wait();
            }
            Console.WriteLine("Shutdown!");
        }
    }
}
