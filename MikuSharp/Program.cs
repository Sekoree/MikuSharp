
using Serilog;

namespace MikuSharp;

class Program
{
	static void Main(string[] args)
	{
		using (var bot = new MikuBot())
		{
			MikuBot.RegisterEvents().Wait();
			bot.RegisterCommands();
			bot.RunAsync().Wait();
			bot.Dispose();
		}
		Log.Logger.Information("Shutdown!");
	}
}
