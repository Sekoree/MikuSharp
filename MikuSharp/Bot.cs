using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.CommandsNext;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Enums;
using DisCatSharp.Interactivity.EventHandling;
using DisCatSharp.Interactivity.Extensions;
using DisCatSharp.Lavalink;
using DisCatSharp.Net;

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

namespace MikuSharp
{
    class Bot : IDisposable
    {
        public static BotConfig cfg { get; set; } 
        public static WeebClient _weeb = new WeebClient("Hatsune Miku Bot C# Rewrite", "2.0.0");
        public Task GameSetThread { get; set; }
        public Task StatusThread { get; set; }
        public static DiscordShardedClient bot { get; set; }
        static CancellationTokenSource _cts { get; set; }
        public static Dictionary<ulong, Guild> Guilds = new Dictionary<ulong, Guild>();
        public IReadOnlyDictionary<int, InteractivityExtension> interC { get; set; }
        public IReadOnlyDictionary<int, CommandsNextExtension> cmdC { get; set; }
        public IReadOnlyDictionary<int, ApplicationCommandsExtension> acC { get; set; }
        public static IReadOnlyDictionary<int, LavalinkExtension> lavaC { get; set; }
        public static Dictionary<int, LavalinkNodeConnection> LLEU = new Dictionary<int, LavalinkNodeConnection>();
        public static Playstate ps = Playstate.Playing;
        public static Stopwatch psc = new Stopwatch();

        public Bot()
        {
            cfg = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText(@"config.json"));
            cfg.DbConnectString = $"Host={cfg.DbConfig.Hostname};Username={cfg.DbConfig.User};Password={cfg.DbConfig.Password};Database=MikuSharpDB";
            _cts = new CancellationTokenSource();
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("miku_log.txt", fileSizeLimitBytes: null, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 2, shared: true)
                .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
            Log.Logger.Information("Starting up!");
            bot = new DiscordShardedClient(new DiscordConfiguration
            {
                Token = cfg.DiscordToken,
                TokenType = DisCatSharp.TokenType.Bot,
                MinimumLogLevel = LogLevel.Debug,
                AutoReconnect = true,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers,
                MessageCacheSize = 2048,
                LoggerFactory = new LoggerFactory().AddSerilog(Log.Logger)

            });
            bot.GuildDownloadCompleted += (sender, args) =>
            {
                GameSetThread = Task.Run(setGame);
                return Task.CompletedTask;
            };
            bot.ClientErrored += (sender, args) =>
            {
                sender.Logger.LogError(args.Exception.Message);
                sender.Logger.LogError(args.Exception.StackTrace);
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
                bot.Logger.LogInformation("Voice Connections: " + al.Where(x => x.Value.musicInstance.guildConnection?.IsConnected == true).Count());
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
                g.Value.VoiceStateUpdated += VoiceChat.LeftAlone;
                cmdC[g.Key].CommandExecuted += (sender, args) =>
                {
                    // TODO: Check if guild is null
                    sender.Client.Logger.LogInformation($"Command: {args.Command.Name} by {args.Context.User.Username}#{args.Context.User.Discriminator}({args.Context.User.Id}) on {args.Context.Guild.Name}({args.Context.Guild.Id})");
                    return Task.CompletedTask;
                };
                cmdC[g.Key].CommandErrored += (sender, args) =>
                {
                    sender.Client.Logger.LogError(args.Exception.Message);
                    sender.Client.Logger.LogError(args.Exception.StackTrace);
                    return Task.CompletedTask;
                };
                g.Value.MessageCreated += async (sender, args) =>
                {
                    if(args.Guild.Id == 483279257431441410 && args.Message.Content.ToLower() == "#smolarmy")
                    {
                        var guild = args.Guild;
                        var member = await guild.GetMemberAsync(args.Author.Id);
                        if (member.Roles.Any(x => x.Id == 607989212696018945))
                        {
                            await member.RevokeRoleAsync(guild.GetRole(607989212696018945));
                            await args.Message.RespondAsync(":(");
                            return;
                        }
                        await member.GrantRoleAsync(guild.GetRole(607989212696018945));
                        await args.Message.RespondAsync("Welcome to smolcar");
                    }
                    await Task.FromResult(true);
                };
                /*g.Value.GuildMemberAdded += async (sender, args) =>
                {
                    if (args.Guild.Id == 483279257431441410)
                        await Task.Run(async () => await MikuGuild.OnJoinAsync(sender, args));
                    else
                        await Task.FromResult(true);
                };*/
                acC[g.Key].RegisterCommands<Commands.Slash>(483279257431441410);
                acC[g.Key].RegisterCommands<Commands.Developer>(483279257431441410, perms => {
                    perms.AddUser(756968464090136636, true);
                    perms.AddRole(602923142586957853, true);
                });
                acC[g.Key].RegisterCommands<Commands.Developer>(858089281214087179, perms => {
                    perms.AddUser(756968464090136636, true);
                    perms.AddRole(887877553929469952, true);
                });
                g.Value.Logger.LogInformation("Caching Done for shard " + g.Key);
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
                ResponseMessage = "Something went wrong.",
                ResponseBehavior = InteractionResponseBehavior.Respond
            });
            acC = await bot.UseApplicationCommandsAsync(new ApplicationCommandsConfiguration
            {
                Services = null
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
                g.Value.GetApplicationCommands();
                g.Value.GuildMemberUpdated += async (sender, args) =>
                {
                    if (args.Guild.Id == 483279257431441410)
                    {
                        g.Value.Logger.LogDebug("Guild member update in MikuGuild");
                        await MikuGuild.OnUpdateAsync(sender, args);
                    }
                    else
                    {
                        await Task.FromResult(true);
                    }
                };
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
            cmdC = null;
            acC = null;
            interC = null;
            lavaC = null;
            cfg = null;
            bot = null;
        }
    }
}
