using Serilog;

namespace MikuSharp;

public class Program
{
	public static async Task Main(string[] args = null)
	{
		using MikuBot bot = new();
		await bot.SetupAsync();
		await bot.RunAsync();
		bot.Dispose();
		Log.Logger.Information("Shutdown!");
	}
}
