
using Serilog;

namespace MikuSharp;

class Program
{
    static void Main(string[] args)
    {
		using (var bot = new MikuBot())
		{
            bot.RegisterEvents().Wait();
            bot.RegisterCommands();
            bot.RunAsync().Wait();
		}
		Log.Logger.Information("Shutdown!");
	}
}
