using DisCatSharp.Interactivity.EventHandling;

using MikuSharp.Attributes;
using MikuSharp.Commands;
using MikuSharp.Entities;
using MikuSharp.Events;

using Serilog.Sinks.SystemConsole.Themes;

namespace MikuSharp
{
	/// <summary>
	/// Represents the <see cref="MikuBot"/>.
	/// </summary>
	internal class MikuBot : IDisposable
	{
		/// <summary>
		/// Gets the cancellation token source.
		/// </summary>
		internal static CancellationTokenSource CanellationTokenSource { get; set; }

		/// <summary>
		/// Gets the global cancellation token source.
		/// </summary>
		internal static CancellationTokenSource GlobalCancellationTokenSource { get; set; }

		/// <summary>
		/// Gets the bot config.
		/// </summary>
		internal static BotConfig Config { get; set; }

		/// <summary>
		/// Gets the lavalink configuration for every voice connection.
		/// </summary>
		internal static LavalinkConfiguration LavalinkConfig { get; set; }

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
		internal AuthDiscordBotListApi DiscordBotListApi { get; set; }

		/// <summary>
		/// Gets the discord sharded client.
		/// </summary>
		internal static DiscordShardedClient ShardedClient { get; set; }

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
		internal static Dictionary<int, LavalinkSession> LavalinkSessions { get; } = new();

		/// <summary>
		/// Gets the custom guild entities.
		/// </summary>
		internal static Dictionary<ulong, Guild> Guilds { get; } = new();

		/// <summary>
		/// Constructs a new instance of <see cref="MikuBot"/>.
		/// </summary>
		/// <exception cref="ArgumentNullException">Thrown when the config.json was not found or null.</exception>
		internal MikuBot(CancellationTokenSource globalCts)
		{
			GlobalCancellationTokenSource = globalCts;
			var fileData = File.ReadAllText(@"config.json") ?? throw new("config.json is null or missing");

			Config = JsonConvert.DeserializeObject<BotConfig>(fileData) ?? throw new("config.json is null");
			Config.DbConnectString = $"Host={Config.DbConfig.Hostname};Username={Config.DbConfig.User};Password={Config.DbConfig.Password};Database={Config.DbConfig.Database}";

			CanellationTokenSource = new();

			var level = LogEventLevel.Information;
#if DEBUG
			level = LogEventLevel.Debug;
#endif
			var loggingTemplate = "[{Timestamp:HH:mm:ss}] [{Level:u4}] {SourceContext}:{NewLine}{Message:lj}{NewLine}{Exception}";
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Debug()
				.WriteTo.File("miku_log.txt", outputTemplate: loggingTemplate, restrictedToMinimumLevel: LogEventLevel.Debug, fileSizeLimitBytes: null, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 2, shared: true)
				.WriteTo.Console(level, loggingTemplate, theme: new AnsiConsoleTheme(new Dictionary<ConsoleThemeStyle, string>
				{
					[ConsoleThemeStyle.Text] = "\x1b[0m",
					[ConsoleThemeStyle.SecondaryText] = "\x1b[90m",
					[ConsoleThemeStyle.TertiaryText] = "\x1b[90m",
					[ConsoleThemeStyle.Invalid] = "\x1b[31m",
					[ConsoleThemeStyle.Null] = "\x1b[95m",
					[ConsoleThemeStyle.Name] = "\x1b[93m",
					[ConsoleThemeStyle.String] = "\x1b[96m",
					[ConsoleThemeStyle.Number] = "\x1b[95m",
					[ConsoleThemeStyle.Boolean] = "\x1b[95m",
					[ConsoleThemeStyle.Scalar] = "\x1b[95m",
					[ConsoleThemeStyle.LevelVerbose] = "\x1b[34m",
					[ConsoleThemeStyle.LevelDebug] = "\x1b[90m",
					[ConsoleThemeStyle.LevelInformation] = "\x1b[36m",
					[ConsoleThemeStyle.LevelWarning] = "\x1b[33m",
					[ConsoleThemeStyle.LevelError] = "\x1b[31m",
					[ConsoleThemeStyle.LevelFatal] = "\x1b[97;91m"
				}))
				.CreateLogger();
			Log.Logger.Information("Starting up!");

			var token = Config.DiscordToken;
			/*
	#if DEBUG
			token = Config.DiscordTokenDev;
	#endif*/

			ShardedClient = new(new()
			{
				Token = token,
				TokenType = DisCatSharp.Enums.TokenType.Bot,
				MinimumLogLevel = LogLevel.Debug,
				AutoReconnect = true,
				ApiChannel = ApiChannel.Canary,
				HttpTimeout = TimeSpan.FromMinutes(2),
				Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers,
				MessageCacheSize = 2048,
				LoggerFactory = new LoggerFactory().AddSerilog(Log.Logger),
				Timezone = "Europe/Berlin",
				AutoFetchSkuIds = true,
				EnableSentry = true,
				ReportMissingFields = true,
				EnableLibraryDeveloperMode = true,
				DeveloperUserId = 856780995629154305,
				FeedbackEmail = "aiko@aitsys.dev",
				AttachUserInfo = true
				//DisableExceptionFilter = true,
				//CustomSentryDsn = Config.SentryDsn
			});
		}

		/// <summary>
		/// Prepares the bot to run.
		/// </summary>
		internal async Task SetupAsync()
		{
			_ = await ShardedClient.UseInteractivityAsync(new()
			{
				Timeout = TimeSpan.FromMinutes(2),
				PaginationBehaviour = PaginationBehaviour.WrapAround,
				PaginationDeletion = PaginationDeletion.DeleteEmojis,
				PollBehaviour = PollBehaviour.DeleteEmojis,
				AckPaginationButtons = true,
				ButtonBehavior = ButtonPaginationBehavior.Disable,
				PaginationButtons = new()
				{
					SkipLeft = new(ButtonStyle.Primary, PaginationButtons.SKIP_LEFT_CUSTOM_ID, "First", false, new("⏮️")),
					Left = new(ButtonStyle.Primary, PaginationButtons.LEFT_CUSTOM_ID, "Previous", false, new("◀️")),
					Stop = new(ButtonStyle.Danger, PaginationButtons.STOP_CUSTOM_ID, "Cancel", false, new("⏹️")),
					Right = new(ButtonStyle.Primary, PaginationButtons.RIGHT_CUSTOM_ID, "Next", false, new("▶️")),
					SkipRight = new(ButtonStyle.Primary, PaginationButtons.SKIP_RIGHT_CUSTOM_ID, "Last", false, new("⏭️"))
				},
				ResponseBehavior = InteractionResponseBehavior.Ignore
			});

			this.ApplicationCommandsModules = await ShardedClient.UseApplicationCommandsAsync(new()
			{
				DebugStartup = true, ManualOverride = true
			});

			/*this.CommandsNextModules = await ShardedClient.UseCommandsNextAsync(new()
			{
				CaseSensitive = true,
				EnableMentionPrefix = true,
				DmHelp = false,
				EnableDefaultHelp = true,
				IgnoreExtraArguments = true,
				StringPrefixes = new(),
				UseDefaultCommandHandler = true,
				DefaultHelpChecks = new(1) { new NotStaffAttribute() }
			});*/

			/*LavalinkConfig = new()
			{
				SocketEndpoint = new()
				{
					Hostname = Config.LavaConfig.Hostname, Port = Config.LavaConfig.Port
				},
				RestEndpoint = new()
				{
					Hostname = Config.LavaConfig.Hostname, Port = Config.LavaConfig.Port
				},
				Password = Config.LavaConfig.Password,
				DefaultVolume = 40,
				EnableBuiltInQueueSystem = true
			};

			this.LavalinkModules = await ShardedClient.UseLavalinkAsync();*/

			this.RegisterEvents();
			this.RegisterCommands();
		}

		/// <summary>
		/// Registers all events.
		/// </summary>
		internal void RegisterEvents()
		{
			ShardedClient.ClientErrored += (sender, args) =>
			{
				sender.Logger.LogError("{msg}", args.Exception.Message);
				sender.Logger.LogError("{stack}", args.Exception.StackTrace);
				return Task.CompletedTask;
			};

			foreach (var discordClientKvp in ShardedClient.ShardClients)
			{
				/*discordClientKvp.Value.VoiceStateUpdated += (sender, args) =>
				{
					_ = Task.Run(async () => await VoiceChat.LeftAlone(sender, args));
					return Task.CompletedTask;
				};*/

				discordClientKvp.Value.GetApplicationCommands().ApplicationCommandsModuleStartupFinished += (sender, args) =>
				{
					sender.Client.Logger.LogInformation("Shard {shard} finished application command startup", sender.Client.ShardId);
					return Task.CompletedTask;
				};

				//discordClientKvp.Value.PayloadReceived += this.ValueOnPayloadReceived;

				discordClientKvp.Value.GuildMemberAdded += async (sender, args) =>
				{
					_ = Task.Run(async () =>
					{
						if (sender.CurrentApplication.Team.Members.Any(x => x.User.Id == args.Member.Id))
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
					}, CanellationTokenSource.Token);
					await Task.FromResult(true);
				};
				discordClientKvp.Value.GuildMemberUpdated += async (sender, args) =>
				{
					if (args.Guild.Id == 483279257431441410)
						_ = Task.Run(async () => await MikuGuildEvents.OnGuildMemberUpdateAsync(sender, args), CanellationTokenSource.Token);
					else
						await Task.FromResult(true);
				};

				discordClientKvp.Value.Logger.LogInformation("Registered events for shard {shard}", discordClientKvp.Value.ShardId);
				discordClientKvp.Value.EntitlementCreated += this.ValueOnEntitlementCreated;
				discordClientKvp.Value.EntitlementUpdated += this.ValueOnEntitlementUpdated;
				discordClientKvp.Value.EntitlementDeleted += this.ValueOnEntitlementDeleted;
			}

			ShardedClient.EntitlementCreated += this.ShardedClientOnEntitlementCreated;
		}

		private async Task ShardedClientOnEntitlementCreated(DiscordClient sender, EntitlementCreateEventArgs e)
			=> sender.Logger.LogDebug("sc: {json}", JsonConvert.SerializeObject(e.Entitlement, Formatting.Indented));

		private async Task ValueOnEntitlementDeleted(DiscordClient sender, EntitlementDeleteEventArgs e)
			=> sender.Logger.LogDebug("d: {json}", JsonConvert.SerializeObject(e.Entitlement, Formatting.Indented));

		private async Task ValueOnEntitlementUpdated(DiscordClient sender, EntitlementUpdateEventArgs e)
			=> sender.Logger.LogDebug("u: {json}", JsonConvert.SerializeObject(e.Entitlement, Formatting.Indented));

		private async Task ValueOnEntitlementCreated(DiscordClient sender, EntitlementCreateEventArgs e)
			=> sender.Logger.LogDebug("c: {json}", JsonConvert.SerializeObject(e.Entitlement, Formatting.Indented));

		internal DiscordChannel LogChannel { get; set; }

		private async Task ValueOnPayloadReceived(DiscordClient sender, PayloadReceivedEventArgs e)
		{
			return;

			if (e.EventName.ToLowerInvariant() == "interaction_create" || e.EventName.ToLowerInvariant().Contains("entitlement"))
				await this.LogChannel.SendMessageAsync(JsonConvert.SerializeObject(e.Json, Formatting.Indented).BlockCode("json"));
		}

		/// <summary>
		/// Shows all voice node connections every 15 minutes.
		/// </summary>
		internal async Task ShowConnections()
		{
			while (!CanellationTokenSource.IsCancellationRequested)
			{
				var al = Guilds.Where(x => x.Value?.MusicInstance != null);
				ShardedClient.Logger.LogInformation("Voice Connections: {count}", al.Count(x => x.Value.MusicInstance.GuildPlayer?.IsConnected == true));
				await Task.Delay(TimeSpan.FromMinutes(15), CanellationTokenSource.Token);
			}
		}

		/// <summary>
		/// Updates the bot list stats every 15 minutes.
		/// </summary>
		internal async Task UpdateBotList()
		{
			await Task.Delay(TimeSpan.FromMinutes(15), CanellationTokenSource.Token);
			while (!CanellationTokenSource.IsCancellationRequested)
			{
				var me = await this.DiscordBotListApi.GetMeAsync();
				var manCount = 0;
				var count = Array.Empty<int>();
				var clients = ShardedClient.ShardClients.Values;
				foreach (var client in clients)
				{
					count = count.Append(client.Guilds.Count).ToArray();
					manCount += client.Guilds.Count;
				}

				await me.UpdateStatsAsync(0, ShardedClient.ShardClients.Count, count);
				try
				{
					var rest = ShardedClient.ShardClients.First().Value.RestClient;
					HttpRequestMessage msg = new(HttpMethod.Post, $"https://motiondevelopment.top/api/v1.2/bots/{ShardedClient.CurrentApplication.Id}/stats");
					msg.Headers.TryAddWithoutValidation("Content-Type", "application/json");
					msg.Headers.TryAddWithoutValidation("key", Config.MotionBotListToken);
					msg.Content = new StringContent(JsonConvert.SerializeObject(new MotionGuildCount(manCount), Formatting.Indented), Encoding.UTF8);
					await rest.SendAsync(msg, CanellationTokenSource.Token);
				}
				catch (Exception)
				{ }

				await Task.Delay(TimeSpan.FromMinutes(15), CanellationTokenSource.Token);
			}
		}

		/// <summary>
		/// <para>This functions updates the bots activity every 20 minutes.</para>
		/// <para>We have 3 activities it's switching through.</para>
		/// </summary>
		internal async Task SetActivity()
		{
			while (!CanellationTokenSource.IsCancellationRequested)
			{
				DiscordActivity test = new()
				{
					Name = "Mikuuuuuuuuuuu!", ActivityType = ActivityType.Playing
				};
				await ShardedClient.UpdateStatusAsync(test, UserStatus.Online);
				await Task.Delay(TimeSpan.FromMinutes(20), CanellationTokenSource.Token);
				/*DiscordActivity test2 = new()
				{
					Name = "Mention me with help for nsfw commands!",
					ActivityType = ActivityType.Watching
				};
				await ShardedClient.UpdateStatusAsync(activity: test2, userStatus: UserStatus.Online);
				await Task.Delay(TimeSpan.FromMinutes(20), _canellationTokenSource.Token);*/
				DiscordActivity test3 = new()
				{
					Name = "Full NND support!", ActivityType = ActivityType.ListeningTo
				};
				await ShardedClient.UpdateStatusAsync(test3, UserStatus.Online);
				await Task.Delay(TimeSpan.FromMinutes(20), CanellationTokenSource.Token);
			}
		}

		/// <summary>
		/// Registers all text and slash commands
		/// </summary>
		internal void RegisterCommands()
			// Nsfw stuff needs to be hidden, that's why we use commands next
			//this.CommandsNextModules.RegisterCommands<Nsfw>();
			//this.ApplicationCommandsModules.RegisterGlobalCommands<ActionCommands>();
		{
			this.ApplicationCommandsModules.RegisterGuildCommands<Developer>(483279257431441410);
			this.ApplicationCommandsModules.RegisterGuildCommands<Developer>(858089274087309313);
		}

		//this.ApplicationCommandsModules.RegisterGuildCommands<Fun>(858089281214087179);
		//this.ApplicationCommandsModules.RegisterGlobalCommands<About>();
		//this.ApplicationCommandsModules.RegisterGlobalCommands<Moderation>();
		//this.ApplicationCommandsModules.RegisterGlobalCommands<Music>();
		//this.ApplicationCommandsModules.RegisterGuildCommands<MusicV2>(858089281214087179);
		//this.ApplicationCommandsModules.RegisterGlobalCommands<Playlist>();
		//this.ApplicationCommandsModules.RegisterGlobalCommands<Utility>();
		//this.ApplicationCommandsModules.RegisterGlobalCommands<WeebCommands>();
		// Smolcar command, only guild command
		//this.ApplicationCommandsModules.RegisterGuildCommands<MikuGuild>(483279257431441410);
		/// <summary>
		/// Runs the actual bot.
		/// </summary>
		internal async Task RunAsync()
		{
			await WeebClient.Authenticate(Config.WeebShToken, Weeb.net.TokenType.Wolke);
			await ShardedClient.StartAsync();
			await Task.Delay(5000);
			await ShardedClient.UpdateStatusAsync(null, UserStatus.Invisible, null);
			this.LogChannel = await ShardedClient.GetShard(0).GetChannelAsync(987522384489762836, true);
			/*foreach (var lavalinkShard in this.LavalinkModules)
				LavalinkSessions.Add(lavalinkShard.Key, await lavalinkShard.Value.ConnectAsync(LavalinkConfig));*/
			//this.SetActivityThread = Task.Run(this.SetActivity, CanellationTokenSource.Token);
			//this.ConnectionThread = Task.Run(this.ShowConnections, CanellationTokenSource.Token);
#if !DEBUG
		this.DiscordBotListApi = new AuthDiscordBotListApi(ShardedClient.CurrentApplication.Id, Config.DiscordBotListToken);
		this.BotlistThread = Task.Run(this.UpdateBotList, _canellationTokenSource.Token);
#endif
			while (!CanellationTokenSource.IsCancellationRequested && !GlobalCancellationTokenSource.IsCancellationRequested)
				await Task.Delay(1000);
			/*foreach (var lavalinkShard in this.LavalinkModules.Values)
				foreach (var session in lavalinkShard.ConnectedSessions.Values)
					await session.DestroyAsync();
			LavalinkSessions.Clear();*/
			foreach (var appModule in await ShardedClient.GetApplicationCommandsAsync())
				appModule.Value.CleanModule();
			if (GlobalCancellationTokenSource.IsCancellationRequested)
				CanellationTokenSource.Cancel();
			await ShardedClient.UpdateStatusAsync(userStatus: UserStatus.Offline);
			await Task.Delay(4000);
			await ShardedClient.StopAsync();
		}

		~MikuBot()
		{
			this.Dispose();
		}

		/// <summary>
		/// Disposes the bot.
		/// </summary>
		public void Dispose()
		{
			this.DiscordBotListApi = null;
			this.ApplicationCommandsModules = null;
			//this.CommandsNextModules = null;
			this.LavalinkModules = null;
			Guilds.Clear();
			ShardedClient = null;
			GC.SuppressFinalize(this);
		}
	}
}
