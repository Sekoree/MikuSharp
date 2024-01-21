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
//using DisCatSharp.Lavalink;
using DisCatSharp.Net;

using DiscordBotsList.Api;

using Microsoft.Extensions.Logging;

using MikuSharp.Entities;
using MikuSharp.Enums;
using MikuSharp.Events;

using Newtonsoft.Json;

using Serilog;
using Serilog.Events;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Weeb.net;

namespace MikuSharp;

internal class MikuBot : IDisposable
{
	internal static CancellationTokenSource _cts { get; set; }

	internal static BotConfig Config { get; set; }
	//internal LavalinkConfiguration LavalinkConfig { get; set; }

	internal Task GameSetThread { get; set; }
	internal Task StatusThread { get; set; }
	internal Task BotListThread { get; set; }

	internal static WeebClient _weebClient = new("Hatsune Miku Bot", "4.0.0");
	internal static AuthDiscordBotListApi DiscordBotListApi { get; set; }
	internal static DiscordShardedClient ShardedClient { get; set; }

	internal IReadOnlyDictionary<int, InteractivityExtension> InteractivityModules { get; set; }
	internal IReadOnlyDictionary<int, ApplicationCommandsExtension> ApplicationCommandsModules { get; set; }
	internal IReadOnlyDictionary<int, CommandsNextExtension> CommandsNextModules { get; set; }
	//internal IReadOnlyDictionary<int, LavalinkExtension> LavalinkModules { get; set; }

	//internal static Dictionary<int, LavalinkNodeConnection> LavalinkNodeConnections = new();
	//internal static Dictionary<ulong, Guild> Guilds = new();

	internal static Playstate ps = Playstate.Playing;
	internal static Stopwatch psc = new();

	internal MikuBot()
	{
		var fileData = File.ReadAllText(@"config.json") ?? throw new ArgumentNullException("config.json is null or missing");

		Config = JsonConvert.DeserializeObject<BotConfig>(fileData);
		if (Config == null)
			throw new ArgumentNullException("config.json is null");

		Config.DbConnectString = $"Host={Config.DbConfig.Hostname};Username={Config.DbConfig.User};Password={Config.DbConfig.Password};Database={Config.DbConfig.Database}";
		_cts = new();

		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Debug()
			.WriteTo.File("miku_log.txt", fileSizeLimitBytes: null, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 2, shared: true)
			.WriteTo.Console(LogEventLevel.Debug, "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
			.CreateLogger();
		Log.Logger.Information("Starting up!");

		ShardedClient = new(new()
		{
			Token = Config.DiscordToken,
			TokenType = DisCatSharp.Enums.TokenType.Bot,
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
			StringPrefixes = new(),
			UseDefaultCommandHandler = true,
			DefaultHelpChecks = new(1) { new Attributes.NotStaffAttribute() }
		}).Result;

		/*LavalinkConfig = new()
		{
			SocketEndpoint = new ConnectionEndpoint { Hostname = Config.LavaConfig.Hostname, Port = Config.LavaConfig.Port },
			RestEndpoint = new ConnectionEndpoint { Hostname = Config.LavaConfig.Hostname, Port = Config.LavaConfig.Port },
			Password = Config.LavaConfig.Password
		};

		LavalinkModules = ShardedClient.UseLavalinkAsync().Result;*/
	}

	internal async static Task RegisterEvents()
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

			discordClientKvp.Value.GuildMemberAdded += async (sender, args) =>
			{
				if (sender.CurrentApplication.Team.Members.Where(x => x.User.Id == args.Member.Id).Any())
				{
					var text = $"Heywo <:MikuWave:655783221942026271>!"
					           + $"\n\nOne of my developers joined your server!"
					           + $"\nAs you're the owner of the server ({args.Guild.Name}) I want to inform you about that. But don't worry, they won't disturb anyone!"
					           + $"\nThey're here to debug me on different servers to transition to slash commands because discord forces us bots to use it (Read more here: https://support-dev.discord.com/hc/en-us/articles/4404772028055)."
					           + $"\nThe problem is the _message content intent_ which means I can't listen to my `m%` prefix anymore :(."
					           + $"\n\nIf you have a problem please contact my developer {args.Member.UsernameWithDiscriminator}!"
					           + $"\n\n\nI wish you a happy day <:mikuthumbsup:623933340520546306>";
					var message = await args.Guild.Owner.SendMessageAsync(text);
					sender.Logger.LogInformation("I wrote {owner} a message", args.Guild.Owner.UsernameWithDiscriminator);
					sender.Logger.LogInformation("Message content: {content}", message.Content);
				}
				else
					await Task.FromResult(true);
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

/*
	internal async Task ShowConnections()
	{
		while (true)
		{
			var al = Guilds.Where(x => x.Value?.musicInstance != null);
			ShardedClient.Logger.LogInformation("Voice Connections: " + al.Where(x => x.Value.musicInstance.guildConnection?.IsConnected == true).Count());
			await Task.Delay(15000);
		}
	}
*/
	internal async static Task UpdateBotList()
	{
		await Task.Delay(15000);
		while (true)
		{
			var me = await DiscordBotListApi.GetMeAsync();
			var count = Array.Empty<int>();
			var clients = ShardedClient.ShardClients.Values;
			foreach (var client in clients)
				count = count.Append(client.Guilds.Count).ToArray();
			await me.UpdateStatsAsync(0, ShardedClient.ShardClients.Count, count);
			await Task.Delay(TimeSpan.FromMinutes(15));
		}
	}

	internal async Task SetActivity()
	{
		while (true)
		{
			DiscordActivity test = new()
			{
				Name = "I'm using slash commands now!",
				ActivityType = ActivityType.Playing
			};
			await ShardedClient.UpdateStatusAsync(test, UserStatus.Online);
			await Task.Delay(TimeSpan.FromMinutes(20));
			DiscordActivity test2 = new()
			{
				Name = "Mention me with help for nsfw commands!",
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
		this.CommandsNextModules.RegisterCommands<Commands.NSFW>();

		this.ApplicationCommandsModules.RegisterGlobalCommands<Commands.Action>();
		this.ApplicationCommandsModules.RegisterGlobalCommands<Commands.Developer>();
		this.ApplicationCommandsModules.RegisterGlobalCommands<Commands.Fun>();
		this.ApplicationCommandsModules.RegisterGlobalCommands<Commands.About>();
		this.ApplicationCommandsModules.RegisterGlobalCommands<Commands.Moderation>();
		//ApplicationCommandsModules.RegisterGlobalCommands<Commands.Music>();
		//ApplicationCommandsModules.RegisterGlobalCommands<Commands.Playlists>();
		this.ApplicationCommandsModules.RegisterGlobalCommands<Commands.Utility>();
		this.ApplicationCommandsModules.RegisterGlobalCommands<Commands.Weeb>();

		// Smolcar command, only guild command
		this.ApplicationCommandsModules.RegisterGuildCommands<Commands.MikuGuild>(483279257431441410);
	}

	internal async Task RunAsync()
	{
		await _weebClient.Authenticate(Config.WeebShToken, Weeb.net.TokenType.Wolke);
		await ShardedClient.StartAsync();
		await Task.Delay(5000);
		/*foreach (var lavalinkShard in LavalinkModules)
		{
			var LCon = await lavalinkShard.Value.ConnectAsync(LavalinkConfig);
			LavalinkNodeConnections.Add(lavalinkShard.Key, LCon);
		}*/
		this.GameSetThread = Task.Run(this.SetActivity);
		//StatusThread = Task.Run(ShowConnections);
		//DiscordBotListApi = new AuthDiscordBotListApi(ShardedClient.CurrentApplication.Id, Config.DiscordBotListToken);
		//BotListThread = Task.Run(UpdateBotList);
		while (!_cts.IsCancellationRequested)
			await Task.Delay(25);
		await ShardedClient.StopAsync();
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
	}
}