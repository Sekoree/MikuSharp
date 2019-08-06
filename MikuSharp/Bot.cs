using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Lavalink;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using MikuSharp.Entities;
using Weeb.net;
using Newtonsoft.Json;
using DSharpPlus.Interactivity.Enums;
using MikuSharp.Utilities;
using MikuSharp.Events;
using System.IO;
using MikuSharp.Enums;

namespace MikuSharp
{
    class Bot : IDisposable
    {
        public static BotConfig cfg { get; set; } 
        public static WeebClient _weeb = new WeebClient("Hatsune Miku Bot C# Rewrite", "0.0.1");
        public Task GameSetThread { get; set; }
        public Task StatusThread { get; set; }
        public static DiscordShardedClient bot { get; set; }
        static CancellationTokenSource _cts { get; set; }
        public static Dictionary<ulong, Guild> Guilds = new Dictionary<ulong, Guild>();
        public IReadOnlyDictionary<int, InteractivityExtension> interC { get; set; }
        public IReadOnlyDictionary<int, CommandsNextExtension> cmdC { get; set; }
        public IReadOnlyDictionary<int, LavalinkExtension> lavaC { get; set; }
        public static Dictionary<int, LavalinkNodeConnection> LLEU = new Dictionary<int, LavalinkNodeConnection>();

        public Bot()
        {
            cfg = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText(@"config.json"));
            cfg.DbConnectString = $"Host={cfg.DbConfig.Hostname};Username={cfg.DbConfig.User};Password={cfg.DbConfig.Password};Database=MikuSharpDB";
            Console.WriteLine("first");
            _cts = new CancellationTokenSource();
            bot = new DiscordShardedClient(new DiscordConfiguration
            {
                Token = cfg.DiscordToken,
                TokenType = DSharpPlus.TokenType.Bot,
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true,
                ReconnectIndefinitely = true
            });
            bot.GuildDownloadCompleted += e =>
            {
                GameSetThread = Task.Run(setGame);
                return Task.CompletedTask;
            };
            bot.ClientErrored += e =>
            {
                Console.WriteLine(e.Exception.Message);
                Console.WriteLine(e.Exception.StackTrace);
                return Task.CompletedTask;
            };
        }

        public async Task setGame()
        {
            await Task.CompletedTask;
            while (true)
            {
                DiscordActivity test = new DiscordActivity
                {
                    Name = "m%help for commands!",
                    ActivityType = ActivityType.Playing
                };
                await bot.UpdateStatusAsync(activity: test, userStatus: UserStatus.Online);
                await Task.Delay(TimeSpan.FromMinutes(30));
            }
        }

        public async Task ShowConnections()
        {
            while (true)
            {
                var al = Guilds.Where(x => x.Value.musicInstance != null);
                Console.WriteLine("Voice Connections: " + al.Where(x => x.Value.musicInstance.guildConnection?.IsConnected == true).Count());
                await Task.Delay(15000);
            }
        }

        public async Task CacheRegister()
        {
            int i = 0;
            foreach (var g in bot.ShardClients)
            {
                foreach (var gg in g.Value.Guilds.ToList())
                {
                    Guilds.TryAdd(gg.Key, new Guild(g.Key));
                    await Database.CacheLPL(gg.Key);
                }
                i++;
            }
            foreach(var g in bot.ShardClients)
            {
                cmdC[g.Key].RegisterCommands<Commands.Action>();
                cmdC[g.Key].RegisterCommands<Commands.Fun>();
                cmdC[g.Key].RegisterCommands<Commands.General>();
                cmdC[g.Key].RegisterCommands<Commands.Moderation>();
                cmdC[g.Key].RegisterCommands<Commands.Music>();
                cmdC[g.Key].RegisterCommands<Commands.NSFW>();
                cmdC[g.Key].RegisterCommands<Commands.Utility>();
                cmdC[g.Key].RegisterCommands<Commands.Weeb>();
                cmdC[g.Key].RegisterCommands<Commands.MikuGuild>();
                bot.ShardClients[g.Key].VoiceStateUpdated += VoiceChat.LeftAlone;
                Console.WriteLine("Caching Done " + g.Key);
                cmdC[g.Key].CommandErrored += e =>
                {
                    Console.WriteLine(e.Exception);
                    return Task.CompletedTask;
                };
            }
        }

        public async Task RunBot()
        {
            int i = 0;
            await _weeb.Authenticate(cfg.WeebShToken, Weeb.net.TokenType.Wolke);
            await bot.StartAsync();
            var LL = await bot.UseLavalinkAsync();
            lavaC = LL;
            foreach (var shard in lavaC)
            {
                var LCon = await shard.Value.ConnectAsync(new LavalinkConfiguration
                {
                    SocketEndpoint = new DSharpPlus.Net.ConnectionEndpoint { Hostname = cfg.LavaConfig.Hostname, Port = cfg.LavaConfig.Port },
                    RestEndpoint = new DSharpPlus.Net.ConnectionEndpoint { Hostname = cfg.LavaConfig.Hostname, Port = cfg.LavaConfig.Port },
                    Password = cfg.LavaConfig.Password
                });
                LLEU.Add(shard.Key, LCon);
            }
            
            interC = await bot.UseInteractivityAsync(new InteractivityConfiguration
                {
                PaginationBehaviour = PaginationBehaviour.WrapAround,
                PaginationDeletion = PaginationDeletion.DeleteEmojis,
                Timeout = TimeSpan.FromMinutes(2)
            });
            cmdC = await bot.UseCommandsNextAsync(new CommandsNextConfiguration
            {
                EnableDefaultHelp = false,
                StringPrefixes = new[] { "m%" }
            });
            foreach (var g in bot.ShardClients)
            {
                g.Value.GuildDownloadCompleted += e =>
                {
                    i++;
                    return Task.CompletedTask;
                };
            }
            while(i != bot.ShardClients.Count - 1 && cmdC == null && interC == null)
            {
                await Task.Delay(1000);
            }
            await Task.Run(CacheRegister);
            StatusThread = Task.Run(ShowConnections);
            while (!_cts.IsCancellationRequested)
            {
                await Task.Delay(25);
            }
            foreach (var shard in bot.ShardClients)
            {
                await shard.Value.DisconnectAsync();
            }
        }

        public void Dispose()
        {
            bot = null;
        }
    }
}
