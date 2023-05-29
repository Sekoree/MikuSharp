namespace MikuSharp;

public class Program
{
	internal static CancellationTokenSource _globalCancellationTokenSource { get; set; } = new();

	public static async Task Main(string[] args = null)
	{
		while(!_globalCancellationTokenSource.IsCancellationRequested)
		{
			Log.Logger.Information("Starting up Miku");
			using MikuBot bot = new(_globalCancellationTokenSource);
			await bot.SetupAsync();
			await bot.RunAsync();
			bot.Dispose();
			Log.Logger.Information("Shutdown!");
			if (!_globalCancellationTokenSource.IsCancellationRequested)
				Log.Logger.Information("Restarting soon..");
			await Task.Delay(5000);
		}
	}
}
