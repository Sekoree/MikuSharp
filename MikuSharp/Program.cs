namespace MikuSharp;

public class Program
{
	internal static CancellationTokenSource GlobalCancellationTokenSource { get; set; } = new();

	public static async Task Main(string[] args = null)
	{
		while(!GlobalCancellationTokenSource.IsCancellationRequested)
		{
			Log.Logger.Information("Starting up Miku");
			using MikuBot bot = new(GlobalCancellationTokenSource);
			await bot.SetupAsync();
			await bot.RunAsync();
			bot.Dispose();
			Log.Logger.Information("Shutdown!");
			if (!GlobalCancellationTokenSource.IsCancellationRequested)
				Log.Logger.Information("Restarting soon..");
			await Task.Delay(5000);
		}
	}
}
