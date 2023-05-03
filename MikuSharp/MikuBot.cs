using System.Diagnostics;

using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Enums;
using DisCatSharp.Interactivity.EventHandling;
using DisCatSharp.Interactivity.Extensions;
using DisCatSharp.Lavalink;
using DisCatSharp.Net;

using DiscordBotsList.Api;

using Microsoft.Extensions.Logging;

using MikuSharp.Entities;
using MikuSharp.Enums;
using MikuSharp.Events;

using Newtonsoft.Json;

using Serilog;
using Serilog.Events;

using Weeb.net;

namespace MikuSharp;

/// <summary>
/// Represents the <see cref="MikuBot"/>.
/// </summary>
internal class MikuBot : IDisposable
{
	/// <summary>
	/// Gets the global canellation token source.
	/// </summary>
	internal static CancellationTokenSource _cts { get; set; }

	/// <summary>
	/// Gets the bot config.
	/// </summary>
	internal static BotConfig Config { get; set; }
	/// <summary>
	/// Gets the lavalink configuration for every voice connection.
	/// </summary>
	internal LavalinkConfiguration LavalinkConfig { get; set; }

	/// <summary>
	/// Runs the activity update.
	/// </summary>
	internal Task SetActivityThread { get; set; }
	/// <summary>
	/// Runs the connection update.
	/// </summary>
	internal Task ConnectionThread { get; set; }
	/// <summary>
	/// Runs the bot list stats update.
	/// </summary>
	internal Task BotlistThread { get; set; }

	/// <summary>
	/// Gets the weeb client for images and gifs.
	/// </summary>
	internal static WeebClient WeebClient { get; } = new("Hatsune Miku Bot", "4.0.0");
	/// <summary>
	/// Gets the discord bot list api client.
	/// </summary>
	internal static AuthDiscordBotListApi DiscordBotListApi { get; set; }
	/// <summary>
	/// Gets the discord sharded client.
	/// </summary>
	internal static DiscordShardedClient ShardedClient { get; set; }

	/// <summary>
	/// Gets the interactivity extensions for every shard.
	/// </summary>
	internal IReadOnlyDictionary<int, InteractivityExtension> InteractivityModules { get; set; }
	/// <summary>
	/// Gets the application commands extensions for every shard.
	/// </summary>
	internal IReadOnlyDictionary<int, ApplicationCommandsExtension> ApplicationCommandsModules { get; set; }
	/// <summary>
	/// Gets the commands next extensions for every shard.
	/// </summary>
	internal IReadOnlyDictionary<int, CommandsNextExtension> CommandsNextModules { get; set; }
	/// <summary>
	/// Gets the lavalink extensions for every shard.
	/// </summary>
	internal IReadOnlyDictionary<int, LavalinkExtension> LavalinkModules { get; set; }

	/// <summary>
	/// Gets the lavalink node connections for every shard.
	/// </summary>
	internal static Dictionary<int, LavalinkNodeConnection> LavalinkNodeConnections = new();
	/// <summary>
	/// Gets the custom guild entities.
	/// </summary>
	internal static Dictionary<ulong, Guild> Guilds { get; } = new();

	/// <summary>
	/// Constructs a new instance of <see cref="MikuBot"/>.
	/// </summary>
	/// <exception cref="ArgumentNullException">Thrown when the config.json was not found or null.</exception>
	internal MikuBot()
	{
		var fileData = File.ReadAllText(@"config.json") ?? throw new ArgumentNullException("config.json is null or missing");

		Config = JsonConvert.DeserializeObject<BotConfig>(fileData) ?? throw new ArgumentNullException("config.json is null");
		Config.DbConnectString = $"Host={Config.DbConfig.Hostname};Username={Config.DbConfig.User};Password={Config.DbConfig.Password};Database={Config.DbConfig.Database}";

		_cts = new CancellationTokenSource();

		LogEventLevel level = LogEventLevel.Information;
#if DEBUG
		level = LogEventLevel.Debug;
#endif

		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Debug()
			.WriteTo.File("miku_log.txt", restrictedToMinimumLevel: LogEventLevel.Debug, fileSizeLimitBytes: null, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 2, shared: true)
			.WriteTo.Console(restrictedToMinimumLevel: level, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
			.CreateLogger();
		Log.Logger.Information("Starting up!");

		string token = Config.DiscordToken;
#if DEBUG
		token = Config.DiscordTokenDev;
#endif

		ShardedClient = new DiscordShardedClient(new()
		{
			Token = token,
			TokenType = DisCatSharp.Enums.TokenType.Bot,
			MinimumLogLevel = LogLevel.Debug,
			AutoReconnect = true,
			UseCanary = true,
			HttpTimeout = TimeSpan.FromMinutes(2),
			Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers,
			MessageCacheSize = 2048,
			LoggerFactory = new LoggerFactory().AddSerilog(Log.Logger)
		});
	}

	/// <summary>
	/// Prepares the bot to run.
	/// </summary>
	internal async Task SetupAsync()
	{
		InteractivityModules = await ShardedClient.UseInteractivityAsync(new()
		{
			Timeout = TimeSpan.FromMinutes(2),
			PaginationBehaviour = PaginationBehaviour.WrapAround,
			PaginationDeletion = PaginationDeletion.DeleteEmojis,
			PollBehaviour = PollBehaviour.DeleteEmojis,
			AckPaginationButtons = true,
			ButtonBehavior = ButtonPaginationBehavior.Disable,
			PaginationButtons = new PaginationButtons()
			{
				SkipLeft = new DiscordButtonComponent(ButtonStyle.Primary, "pgb-skip-left", "First", false, new DiscordComponentEmoji("⏮️")),
				Left = new DiscordButtonComponent(ButtonStyle.Primary, "pgb-left", "Previous", false, new DiscordComponentEmoji("◀️")),
				Stop = new DiscordButtonComponent(ButtonStyle.Danger, "pgb-stop", "Cancel", false, new DiscordComponentEmoji("⏹️")),
				Right = new DiscordButtonComponent(ButtonStyle.Primary, "pgb-right", "Next", false, new DiscordComponentEmoji("▶️")),
				SkipRight = new DiscordButtonComponent(ButtonStyle.Primary, "pgb-skip-right", "Last", false, new DiscordComponentEmoji("⏭️"))
			},
			ResponseBehavior = InteractionResponseBehavior.Ignore
		});

		ApplicationCommandsModules = await ShardedClient.UseApplicationCommandsAsync(new()
		{
			DebugStartup = true,
			ManualOverride = true
		});

		CommandsNextModules = await ShardedClient.UseCommandsNextAsync(new()
		{
			CaseSensitive = true,
			EnableMentionPrefix = true,
			DmHelp = false,
			EnableDefaultHelp = true,
			IgnoreExtraArguments = true,
			StringPrefixes = new List<string>(),
			UseDefaultCommandHandler = true,
			DefaultHelpChecks = new List<CheckBaseAttribute>(1) { new Attributes.NotStaffAttribute() }
		});

		LavalinkConfig = new()
		{
			SocketEndpoint = new ConnectionEndpoint { Hostname = Config.LavaConfig.Hostname, Port = Config.LavaConfig.Port },
			RestEndpoint = new ConnectionEndpoint { Hostname = Config.LavaConfig.Hostname, Port = Config.LavaConfig.Port },
			Password = Config.LavaConfig.Password
		};

		LavalinkModules = await ShardedClient.UseLavalinkAsync();

		RegisterEvents();
		RegisterCommands();
	}

	/// <summary>
	/// Registers all events.
	/// </summary>
	internal static void RegisterEvents()
	{
		ShardedClient.ClientErrored += (sender, args) =>
		{
			sender.Logger.LogError("{msg}", args.Exception.Message);
			sender.Logger.LogError("{stack}", args.Exception.StackTrace);
			return Task.CompletedTask;
		};

		foreach (var discordClientKvp in ShardedClient.ShardClients)
		{
			discordClientKvp.Value.VoiceStateUpdated += VoiceChat.LeftAlone;

			discordClientKvp.Value.GetApplicationCommands().ApplicationCommandsModuleStartupFinished += (sender, args) =>
			{
				sender.Client.Logger.LogInformation("Shard {shard} finished application command startup.", sender.Client.ShardId);
				return Task.CompletedTask;
			};

			discordClientKvp.Value.GuildMemberAdded += async (sender, args) =>
			{
				_ = Task.Run(async () =>
				{
					if (sender.CurrentApplication.Team.Members.Where(x => x.User.Id == args.Member.Id).Any())
					{
						var text = $"Heywo <:MikuWave:655783221942026271>!" +
						$"\n\nOne of my developers joined your server!" +
						$"\nAs you're the owner of the server ({args.Guild.Name}) I want to inform you about that. But don't worry, they won't disturb anyone!" +
						$"\nThey're here to debug me on different servers." +
						$"\n\nIf you have a problem please contact my developer {args.Member.UsernameWithDiscriminator}!" +
						$"\n\n\nI wish you a happy day <:mikuthumbsup:623933340520546306>";
						try
						{
							var message = await args.Guild.Owner.SendMessageAsync(text);
							sender.Logger.LogInformation("I wrote {owner} a message", args.Guild.Owner.UsernameWithDiscriminator);
							await Task.FromResult(true);
						}
						catch (Exception)
						{
							sender.Logger.LogWarning("Could not inform {owner} of dev presence", args.Guild.Owner.UsernameWithDiscriminator);
							await Task.FromResult(false);
						}
					}
					else
						await Task.FromResult(true);
				});
				await Task.FromResult(true);
			};
			discordClientKvp.Value.GuildMemberUpdated += async (sender, args) =>
			{
				if (args.Guild.Id == 483279257431441410)
					_ = Task.Run(async () => await MikuGuild.OnUpdateAsync(sender, args));
				else
					await Task.FromResult(true);
			};

			discordClientKvp.Value.Logger.LogInformation("Registered events for shard {shard}", discordClientKvp.Value.ShardId);
		}
	}

	/// <summary>
	/// Shows all voice node connections every 15 minutes.
	/// </summary>
	internal async Task ShowConnections()
	{
		while (!_cts.IsCancellationRequested)
		{
			var al = Guilds.Where(x => x.Value?.musicInstance != null);
			ShardedClient.Logger.LogInformation("Voice Connections: " + al.Where(x => x.Value.musicInstance.guildConnection?.IsConnected == true).Count());
			await Task.Delay(15000);
		}
	}

	/// <summary>
	/// Updates the bot list stats.
	/// </summary>
	internal static async Task UpdateBotList()
	{
		await Task.Delay(15000);
		while (!_cts.IsCancellationRequested)
		{
			var me = await DiscordBotListApi.GetMeAsync();
			int[] count = Array.Empty<int>();
			var clients = ShardedClient.ShardClients.Values;
			foreach (var client in clients)
				count = count.Append(client.Guilds.Count).ToArray();
			await me.UpdateStatsAsync(0, ShardedClient.ShardClients.Count, count);
			await Task.Delay(TimeSpan.FromMinutes(15));
		}
	}

	/// <summary>
	/// <para>This functions updates the bots activity every 20 minutes.</para>
	/// <para>We have 3 activities it's switching through.</para>
	/// </summary>
	internal async Task SetActivity()
	{
		while (!_cts.IsCancellationRequested)
		{
			DiscordActivity test = new()
			{
				Name = "Mikuuuuuuuuuuu!",
				ActivityType = ActivityType.Playing
			};
			await ShardedClient.UpdateStatusAsync(activity: test, userStatus: UserStatus.Online);
			await Task.Delay(TimeSpan.FromMinutes(20));
			DiscordActivity test2 = new()
			{
				Name = "Mention me with help for nsfw commands!",
				ActivityType = ActivityType.Playing
			};
			await ShardedClient.UpdateStatusAsync(activity: test2, userStatus: UserStatus.Online);
			await Task.Delay(TimeSpan.FromMinutes(20));
			DiscordActivity test3 = new()
			{
				Name = "Full NND support!",
				ActivityType = ActivityType.Playing
			};
			await ShardedClient.UpdateStatusAsync(activity: test3, userStatus: UserStatus.Online);
			await Task.Delay(TimeSpan.FromMinutes(20));
		}
	}

	/// <summary>
	/// Registers all text and slash commands
	/// </summary>
	internal void RegisterCommands()
	{
		// Nsfw stuff needs to be hidden, that's why we use commands next
		CommandsNextModules.RegisterCommands<Commands.NSFW>();

		ApplicationCommandsModules.RegisterGlobalCommands<Commands.Action>();
		ApplicationCommandsModules.RegisterGlobalCommands<Commands.Developer>();
		ApplicationCommandsModules.RegisterGlobalCommands<Commands.Fun>();
		ApplicationCommandsModules.RegisterGlobalCommands<Commands.About>();
		ApplicationCommandsModules.RegisterGlobalCommands<Commands.Moderation>();
		ApplicationCommandsModules.RegisterGlobalCommands<Commands.Music>();
		ApplicationCommandsModules.RegisterGlobalCommands<Commands.Playlists>();
		ApplicationCommandsModules.RegisterGlobalCommands<Commands.Utility>();
		ApplicationCommandsModules.RegisterGlobalCommands<Commands.Weeb>();

		// Smolcar command, only guild command
		ApplicationCommandsModules.RegisterGuildCommands<Commands.MikuGuild>(483279257431441410);
	}

	/// <summary>
	/// Runs the actual bot.
	/// </summary>
	internal async Task RunAsync()
	{
		await WeebClient.Authenticate(Config.WeebShToken, Weeb.net.TokenType.Wolke);
		await ShardedClient.StartAsync();
		await Task.Delay(5000);
		foreach (var lavalinkShard in LavalinkModules)
		{
			var LCon = await lavalinkShard.Value.ConnectAsync(LavalinkConfig);
			LavalinkNodeConnections.Add(lavalinkShard.Key, LCon);
		}
		SetActivityThread = Task.Run(SetActivity);
		ConnectionThread = Task.Run(ShowConnections);
#if !DEBUG
		DiscordBotListApi = new AuthDiscordBotListApi(ShardedClient.CurrentApplication.Id, Config.DiscordBotListToken);
		BotListThread = Task.Run(UpdateBotList);
#endif
		while (!_cts.IsCancellationRequested)
			await Task.Delay(1000);
		await ShardedClient.UpdateStatusAsync(userStatus: UserStatus.Offline);
		await ShardedClient.StopAsync();
	}

	/// <summary>
	/// Disposes the bot.
	/// </summary>
	public void Dispose()
	{
		GC.SuppressFinalize(this);
	}
}
