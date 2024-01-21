using Serilog;

namespace MikuSharp;

internal class Program
{
	private static void Main(string[] args)
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