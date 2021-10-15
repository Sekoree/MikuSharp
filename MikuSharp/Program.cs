
using Serilog;

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
            Log.Logger.Information("Shutdown!");
        }
    }
}
