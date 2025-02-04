using DisCatSharp.ApplicationCommands.Exceptions;

using DiscordBotsList.Api;

using MikuSharp.Attributes;
using MikuSharp.Commands;
using MikuSharp.Commands.Music;
using MikuSharp.Entities;

using Serilog.Events;

using Weeb.net;

using Action = MikuSharp.Commands.Action;
using MikuGuild = MikuSharp.Events.MikuGuild;
using PlaylistCommands = MikuSharp.Commands.Playlist.PlaylistCommands;
using TokenType = DisCatSharp.Enums.TokenType;

namespace MikuSharp;

internal sealed class MikuBot : IDisposable
{
	internal static readonly WeebClient WeebClient = new("Hatsune Miku Bot", "5.0.0");

	//internal static Playstate Ps = Playstate.Playing;
	//internal static Stopwatch Psc = new();

	/// <summary>
	///     Gets the music sessions.
	/// </summary>
	internal static readonly ConcurrentDictionary<ulong, MusicSession> MusicSessions = [];

	/// <summary>
	///    Gets the music session locks.
	/// </summary>
	internal static readonly ConcurrentDictionary<ulong, AsyncLock> MusicSessionLocks = [];

	internal MikuBot()
	{
		var fileData = File.ReadAllText("config.json") ?? throw new ArgumentNullException(null, "config.json is null or missing");

		var config = JsonConvert.DeserializeObject<BotConfig>(fileData);
		ArgumentNullException.ThrowIfNull(config);
		Config = config;

		Config.DbConnectString = $"Host={Config.DbConfig.Hostname};Username={Config.DbConfig.User};Password={Config.DbConfig.Password};Database={Config.DbConfig.Database}";
		Cts = new();

		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Debug()
			.WriteTo.File("miku_log.txt", fileSizeLimitBytes: null, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 2, shared: true)
			.WriteTo.Console(LogEventLevel.Debug)
			.CreateLogger();
		Log.Logger.Information("Starting up!");

		ShardedClient = new(new()
		{
			Token = Config.DiscordToken,
			TokenType = TokenType.Bot,
			MinimumLogLevel = LogLevel.Debug,
			AutoReconnect = true,
			ApiChannel = ApiChannel.Canary,
			HttpTimeout = TimeSpan.FromMinutes(1),
			Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers,
			MessageCacheSize = 2048,
			LoggerFactory = new LoggerFactory().AddSerilog(Log.Logger),
			ShowReleaseNotesInUpdateCheck = false,
			IncludePrereleaseInUpdateCheck = true,
			DisableUpdateCheck = true,
			EnableSentry = true,
			FeedbackEmail = "aiko@aitsys.dev",
			DeveloperUserId = 856780995629154305,
			AttachUserInfo = true,
			ReconnectIndefinitely = true
		});

		this.InteractivityModules = ShardedClient.UseInteractivityAsync(new()
		{
			Timeout = TimeSpan.FromMinutes(2),
			PaginationBehaviour = PaginationBehaviour.WrapAround,
			PaginationDeletion = PaginationDeletion.DeleteEmojis,
			PollBehaviour = PollBehaviour.DeleteEmojis,
			AckPaginationButtons = true,
			ButtonBehavior = ButtonPaginationBehavior.Disable,
			PaginationButtons = new()
			{
				SkipLeft = new(ButtonStyle.Primary, "pgb-skip-left", "First", false, new("⏮️")),
				Left = new(ButtonStyle.Primary, "pgb-left", "Previous", false, new("◀️")),
				Stop = new(ButtonStyle.Danger, "pgb-stop", "Cancel", false, new("⏹️")),
				Right = new(ButtonStyle.Primary, "pgb-right", "Next", false, new("▶️")),
				SkipRight = new(ButtonStyle.Primary, "pgb-skip-right", "Last", false, new("⏭️"))
			},
			ResponseMessage = "Something went wrong.",
			ResponseBehavior = InteractionResponseBehavior.Ignore
		}).Result;

		this.ApplicationCommandsModules = ShardedClient.UseApplicationCommandsAsync(new()
		{
			EnableDefaultHelp = true,
			DebugStartup = true,
			EnableLocalization = false,
			GenerateTranslationFilesOnly = false
		}).Result;

		this.CommandsNextModules = ShardedClient.UseCommandsNextAsync(new()
		{
			CaseSensitive = false,
			EnableMentionPrefix = true,
			DmHelp = false,
			EnableDefaultHelp = true,
			IgnoreExtraArguments = true,
			StringPrefixes = [],
			UseDefaultCommandHandler = true,
			DefaultHelpChecks = [new NotDiscordStaffAttribute()]
		}).Result;

		this.LavalinkConfig = new()
		{
			SocketEndpoint = new()
			{
				Hostname = Config.LavaConfig.Hostname,
				Port = Config.LavaConfig.Port
			},
			RestEndpoint = new()
			{
				Hostname = Config.LavaConfig.Hostname,
				Port = Config.LavaConfig.Port
			},
			Password = Config.LavaConfig.Password,
			EnableBuiltInQueueSystem = true,
			QueueEntryFactory = () => new MusicQueueEntry()
		};

		this.LavalinkModules = ShardedClient.UseLavalinkAsync().Result;
	}

	internal static CancellationTokenSource Cts { get; set; }

	internal static BotConfig Config { get; set; }

	internal LavalinkConfiguration LavalinkConfig { get; set; }

	internal Task GameSetThread { get; set; }

	internal Task StatusThread { get; set; }

	internal Task BotListThread { get; set; }

	internal static AuthDiscordBotListApi DiscordBotListApi { get; set; }

	internal static DiscordShardedClient ShardedClient { get; set; }

	internal IReadOnlyDictionary<int, InteractivityExtension> InteractivityModules { get; set; }

	internal IReadOnlyDictionary<int, ApplicationCommandsExtension> ApplicationCommandsModules { get; set; }

	internal IReadOnlyDictionary<int, CommandsNextExtension> CommandsNextModules { get; set; }

	internal IReadOnlyDictionary<int, LavalinkExtension> LavalinkModules { get; set; }

	public void Dispose()
	{
#pragma warning disable IDE0022 // Use expression body for method
		GC.SuppressFinalize(this);
#pragma warning restore IDE0022 // Use expression body for method
	}

	internal static async Task RegisterEvents()
	{
		ShardedClient.ClientErrored += (sender, args) =>
		{
			sender.Logger.LogError(args.Exception.Message);
			sender.Logger.LogError(args.Exception.StackTrace);
			return Task.CompletedTask;
		};

		await Task.Delay(1);

		foreach (var discordClientKvp in ShardedClient.ShardClients)
		{
			//discordClientKvp.Value.VoiceStateUpdated += VoiceChat.LeftAlone;

			discordClientKvp.Value.GetApplicationCommands().ApplicationCommandsModuleStartupFinished += (sender, args) =>
			{
				sender.Client.Logger.LogInformation("Shard {shard} finished application command startup.", args.ShardId);
				args.Handled = true;
				return Task.CompletedTask;
			};

			discordClientKvp.Value.GetApplicationCommands().ApplicationCommandsModuleReady += (sender, args) =>
			{
				sender.Client.Logger.LogInformation("Application commands module is ready");
				args.Handled = true;
				return Task.CompletedTask;
			};

			discordClientKvp.Value.GetApplicationCommands().SlashCommandErrored += async (sender, args) =>
			{
				if (args.Exception is SlashExecutionChecksFailedException ex)
					if (ex.FailedChecks.Any(x => x is ApplicationCommandRequireTeamMemberAttribute))
					{
						await args.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral().WithContent("This command is limit to developers"));
						return;
					}

				await args.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral().WithContent("An error occurred while executing this command."));
			};

			discordClientKvp.Value.GetApplicationCommands().ContextMenuErrored += async (sender, args) =>
			{
				if (args.Exception is SlashExecutionChecksFailedException ex)
					if (ex.FailedChecks.Any(x => x is ApplicationCommandRequireTeamMemberAttribute))
					{
						await args.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral().WithContent("This command is limit to developers"));
						return;
					}

				await args.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral().WithContent("An error occurred while executing this command."));
			};

			discordClientKvp.Value.GuildMemberUpdated += async (sender, args) =>
			{
				if (args.Guild.Id == 483279257431441410)
					await MikuGuild.OnUpdateAsync(sender, args);
				else
					await Task.FromResult(true);
			};

			discordClientKvp.Value.Logger.LogInformation("Registered events for shard {shard}", discordClientKvp.Value.ShardId);
		}
	}

	/*internal async Task ShowConnections()
	{
	    while (true)
	    {
	        var al = Guilds.Where(x => x.Value?.MusicInstance != null);
	        ShardedClient.Logger.LogInformation("Voice Connections: " + al.Count(x => x.Value.MusicInstance.GuildConnection?.IsConnected == true));
	        await Task.Delay(15000);
	    }
	}*/

	internal static async Task UpdateBotList()
	{
		await Task.Delay(15000);

		while (true)
		{
			var me = await DiscordBotListApi.GetMeAsync();
			var count = Array.Empty<int>();
			var clients = ShardedClient.ShardClients.Values;
			count = clients.Aggregate(count, (current, client) => [.. current, client.Guilds.Count]);
			await me.UpdateStatsAsync(0, ShardedClient.ShardClients.Count, count);
			await Task.Delay(TimeSpan.FromMinutes(15));
		}
	}

	internal static async Task SetActivity()
	{
		while (true)
		{
			DiscordActivity test = new()
			{
				Name = "New music system coming up soon!",
				ActivityType = ActivityType.Playing
			};
			await ShardedClient.UpdateStatusAsync(test, UserStatus.Online);
			await Task.Delay(TimeSpan.FromMinutes(20));
			DiscordActivity test2 = new()
			{
				Name = "Mention me with help for other commands!",
				ActivityType = ActivityType.Playing
			};
			await ShardedClient.UpdateStatusAsync(test2, UserStatus.Online);
			await Task.Delay(TimeSpan.FromMinutes(20));
			DiscordActivity test3 = new()
			{
				Name = "Full NND support!",
				ActivityType = ActivityType.Playing
			};
			await ShardedClient.UpdateStatusAsync(test3, UserStatus.Online);
			await Task.Delay(TimeSpan.FromMinutes(20));
		}
	}

	internal void RegisterCommands()
	{
		// Nsfw stuff needs to be hidden, that's why we use commands next
		this.CommandsNextModules.RegisterCommands<Nsfw>();

		this.ApplicationCommandsModules.RegisterGlobalCommands<Action>();
		this.ApplicationCommandsModules.RegisterGlobalCommands<Developer>();
		this.ApplicationCommandsModules.RegisterGlobalCommands<Fun>();
		this.ApplicationCommandsModules.RegisterGlobalCommands<About>();
		this.ApplicationCommandsModules.RegisterGlobalCommands<Moderation>();
		this.ApplicationCommandsModules.RegisterGlobalCommands<MusicCommands>();
		this.ApplicationCommandsModules.RegisterGlobalCommands<PlaylistCommands>();
		this.ApplicationCommandsModules.RegisterGlobalCommands<Utility>();
		this.ApplicationCommandsModules.RegisterGlobalCommands<Commands.Weeb>();

		// Smolcar command, miku discord guild command
		this.ApplicationCommandsModules.RegisterGuildCommands<Commands.MikuGuild>(483279257431441410);
	}

	internal async Task RunAsync()
	{
		await WeebClient.Authenticate(Config.WeebShToken, Weeb.net.TokenType.Wolke);
		await ShardedClient.StartAsync();
		await Task.Delay(5000);

		foreach (var lavalinkShard in this.LavalinkModules)
			await lavalinkShard.Value.ConnectAsync(this.LavalinkConfig);

		this.GameSetThread = Task.Run(SetActivity);
		//StatusThread = Task.Run(ShowConnections);
		//DiscordBotListApi = new AuthDiscordBotListApi(ShardedClient.CurrentApplication.Id, Config.DiscordBotListToken);
		//BotListThread = Task.Run(UpdateBotList);
		while (!Cts.IsCancellationRequested)
			await Task.Delay(25);
		_ = this.LavalinkModules.Select(lavalinkShard => lavalinkShard.Value.ConnectedSessions.Select(async connectedSession => await connectedSession.Value.DestroyAsync()));
		await ShardedClient.StopAsync();
	}

	~MikuBot()
	{
		this.Dispose();
	}
}
