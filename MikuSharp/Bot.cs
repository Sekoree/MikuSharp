using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.Entities;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Enums;
using DisCatSharp.Interactivity.Extensions;
using DisCatSharp.Lavalink;
using DisCatSharp.Net;

using Microsoft.Extensions.Logging;

using MikuSharp.Entities;
using MikuSharp.Enums;
using MikuSharp.Events;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Weeb.net;

namespace MikuSharp
{
    class Bot : IDisposable
    {
        public static BotConfig cfg { get; set; } 
        public static WeebClient _weeb = new WeebClient("Hatsune Miku Bot C# Rewrite", "0.0.2");
        public Task GameSetThread { get; set; }
        public Task StatusThread { get; set; }
        public static DiscordShardedClient bot { get; set; }
        static CancellationTokenSource _cts { get; set; }
        public static Dictionary<ulong, Guild> Guilds = new Dictionary<ulong, Guild>();
        public IReadOnlyDictionary<int, InteractivityExtension> interC { get; set; }
        public IReadOnlyDictionary<int, CommandsNextExtension> cmdC { get; set; }
        public IReadOnlyDictionary<int, LavalinkExtension> lavaC { get; set; }
        public static Dictionary<int, LavalinkNodeConnection> LLEU = new Dictionary<int, LavalinkNodeConnection>();
        public static Playstate ps = Playstate.Playing;
        public static Stopwatch psc = new Stopwatch();

        public Bot()
        {
            cfg = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText(@"config.json"));
            cfg.DbConnectString = $"Host={cfg.DbConfig.Hostname};Username={cfg.DbConfig.User};Password={cfg.DbConfig.Password};Database=MikuSharpDB";
            Console.WriteLine("first");
            _cts = new CancellationTokenSource();
            bot = new DiscordShardedClient(new DiscordConfiguration
            {
                Token = cfg.DiscordToken,
                TokenType = DisCatSharp.TokenType.Bot,
                MinimumLogLevel = LogLevel.Trace,
                AutoReconnect = true,
                Intents = DiscordIntents.AllUnprivileged,
                MessageCacheSize = 2048,
                ShardCount = 1,
                ShardId = 0
            });
            bot.GuildDownloadCompleted += (sender, args) =>
            {
                GameSetThread = Task.Run(setGame);
                return Task.CompletedTask;
            };
            bot.ClientErrored += (sender, args) =>
            {
                Console.WriteLine(args.Exception.Message);
                Console.WriteLine(args.Exception.StackTrace);
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
                await Task.Delay(TimeSpan.FromMinutes(5));
                DiscordActivity test2 = new DiscordActivity
                {
                    Name = "Check the new playlist commands!",
                    ActivityType = ActivityType.Playing
                };
                await bot.UpdateStatusAsync(activity: test2, userStatus: UserStatus.Online);
                await Task.Delay(TimeSpan.FromMinutes(5));
                DiscordActivity test3 = new DiscordActivity
                {
                    Name = "Full NND support!",
                    ActivityType = ActivityType.Playing
                };
                await bot.UpdateStatusAsync(activity: test3, userStatus: UserStatus.Online);
                await Task.Delay(TimeSpan.FromMinutes(5));
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
            await Task.Delay(1);
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
                cmdC[g.Key].RegisterCommands<Commands.Playlist>();
                cmdC[g.Key].RegisterCommands<Commands.Settings>();
                bot.ShardClients[g.Key].VoiceStateUpdated += VoiceChat.LeftAlone;
                Console.WriteLine("Caching Done " + g.Key);
                cmdC[g.Key].CommandExecuted += (sender, args) =>
                {
                    Console.WriteLine($"Command: {args.Command.Name} by {args.Context.User.Username}#{args.Context.User.Discriminator}({args.Context.User.Id}) on {args.Context.Guild.Name}({args.Context.Guild.Id})");
                    return Task.CompletedTask;
                };
                cmdC[g.Key].CommandErrored += (sender, args) =>
                {
                    Console.WriteLine(args.Exception);
                    return Task.CompletedTask;
                };
            }
        }

        public async Task RunBot()
        {
            int i = 0;
            await _weeb.Authenticate(cfg.WeebShToken, Weeb.net.TokenType.Wolke);
            var LL = await bot.UseLavalinkAsync();
            lavaC = LL;
            interC = await bot.UseInteractivityAsync(new InteractivityConfiguration
                {
                PaginationBehaviour = PaginationBehaviour.WrapAround,
                PaginationDeletion = PaginationDeletion.DeleteEmojis,
                Timeout = TimeSpan.FromMinutes(2),
                PollBehaviour = PollBehaviour.DeleteEmojis,
                AckPaginationButtons = true

            });
            cmdC = await bot.UseCommandsNextAsync(new CommandsNextConfiguration
            {
                EnableDefaultHelp = false,
                StringPrefixes = new[] {"m%"},
                CaseSensitive = true,
                EnableDms = true,
                DmHelp = true,
                EnableMentionPrefix = true
                //PrefixResolver = GetPrefix
            });
            await CacheRegister();
            await bot.StartAsync();
            foreach (var shard in lavaC)
            {
                var LCon = await shard.Value.ConnectAsync(new LavalinkConfiguration
                {
                    SocketEndpoint = new ConnectionEndpoint { Hostname = cfg.LavaConfig.Hostname, Port = cfg.LavaConfig.Port },
                    RestEndpoint = new ConnectionEndpoint { Hostname = cfg.LavaConfig.Hostname, Port = cfg.LavaConfig.Port },
                    Password = cfg.LavaConfig.Password
                });
                LLEU.Add(shard.Key, LCon);
            }
            foreach (var g in bot.ShardClients)
            {
                g.Value.GuildDownloadCompleted += (sender, args) =>
                {
                    i++;
                    return Task.CompletedTask;
                };
            }
            while(i != bot.ShardClients.Count - 1 && cmdC == null && interC == null)
            {
                await Task.Delay(1000);
            }
            //StatusThread = Task.Run(ShowConnections);
            while (!_cts.IsCancellationRequested)
            {
                await Task.Delay(25);
            }
            foreach (var shard in bot.ShardClients)
            {
                await shard.Value.DisconnectAsync();
            }
        }

        /*public async Task<int> GetPrefix(DiscordMessage msg)
        {
            try
            {
                if (msg.Author.IsBot) return -1;
                if (msg.Content.StartsWith(bot.CurrentUser.Mention))
                {
                    return msg.GetMentionPrefixLength(bot.CurrentUser);
                }
                if (msg.Channel.Type == ChannelType.Private)
                {
                    var prefixes = await PrefixDB.GetAllUserPrefixes(msg.Author.Id);
                    if (prefixes.Count == 0 && msg.Content.StartsWith("mm%"))
                    {
                        return msg.GetStringPrefixLength("mm%");
                    }
                    else if (prefixes.Any(x => x.Key == 0))
                    {
                        if (prefixes[0].Any(x => msg.Content.StartsWith(x)))
                        {
                            return msg.GetStringPrefixLength(prefixes[0].First(x => msg.Content.StartsWith(x)));
                        }
                        else if (msg.Content.StartsWith("mm%"))
                        {
                            return msg.GetStringPrefixLength("mm%");
                        }
                    }
                }
                else if (msg.Channel.Type == ChannelType.Text)
                {
                    var userprefixes = await PrefixDB.GetAllUserPrefixes(msg.Author.Id);
                    var guildprefixes = await PrefixDB.GetGuildPrefixes(msg.Channel.Guild.Id);
                    Console.WriteLine(guildprefixes.Count);
                    if (guildprefixes.Count == 0 && msg.Content.StartsWith("mm%"))
                    {
                        return msg.GetStringPrefixLength("mm%");
                    }
                    if (userprefixes.Any(x => x.Key == msg.Channel.Guild.Id))
                    {
                        Console.WriteLine("has one");
                        if (userprefixes[msg.Channel.Guild.Id].Any(x => msg.Content.StartsWith(x)))
                        {
                            Console.WriteLine("Yes here");
                            return msg.GetStringPrefixLength(userprefixes[msg.Channel.Guild.Id].First(x => msg.Content.StartsWith(x)));
                        }
                        else if (userprefixes[0].Any(x => msg.Content.StartsWith(x)))
                        {
                            return msg.GetStringPrefixLength(userprefixes[0].First(x => msg.Content.StartsWith(x)));
                        }
                    }
                    else if (guildprefixes.Any(x => msg.Content.StartsWith(x)))
                    {
                        return msg.GetStringPrefixLength(guildprefixes.First(x => msg.Content.StartsWith(x)));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return -1;
        }*/

        public void Dispose()
        {
            bot = null;
        }
    }
}
