using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using MikuSharp.Attributes;

namespace MikuSharp.Commands
{
    class General : BaseCommandModule
    {
        [Command("donate")]
        [Description("Financial support information")]
        [LongDescription("Some info on how to support the bot's development")]
        [Usage("<Prefix>donate")]
        public async Task Donate(CommandContext ctx)
        {
            var emb = new DiscordEmbedBuilder();
            emb.WithThumbnailUrl(ctx.Client.CurrentUser.AvatarUrl).
                WithTitle("Donate Page!").
                WithAuthor("Miku Bot uwu").
                WithUrl("https://meek.moe/").
                WithColor(new DiscordColor("#348573")).
                WithDescription("Thank you for your interest in supporting the Bot's development!\n" +
                "Here are some links that may interest you").
                AddField("Patreon", "[Link](https://patreon.com/speyd3r)", true).
                AddField("PayPal", "[Link](https://paypal.me/speyd3r)", true);
            await ctx.RespondAsync(embed: emb.Build());
        }

        [Command("feedback")]
        [Description("Send feedback!")]
        public async Task Feedback(CommandContext ctx, [RemainingText] string text)
        {
            var guild = await ctx.Client.GetGuildAsync(483279257431441410);
            var emb = new DiscordEmbedBuilder();
            emb.WithAuthor(ctx.Member.Username).
                WithTitle("Feedback").
                WithDescription(text);
            await guild.GetChannel(484698873411928075).SendMessageAsync(embed: emb.Build());
        }

        [Command("help")]
        [Description("List of all commands")]
        public async Task Help(CommandContext ctx, string command = null)
        {
            try
            {
                if (command != null)
                {
                    if (!ctx.CommandsNext.RegisteredCommands.Any(x => x.Value.Module.ModuleType.Name.ToLower() == command.ToLower()))
                    {
                        LongDescription Desc = new LongDescription("not available currently");
                        string usg = "";
                        try { Desc = ctx.CommandsNext.RegisteredCommands[command].CustomAttributes.OfType<LongDescription>().First(); } catch { }
                        Usage Usage = new Usage("not available currently");
                        try { Usage = ctx.CommandsNext.RegisteredCommands[command].CustomAttributes.OfType<Usage>().First(); } catch { }
                        var emb = new DiscordEmbedBuilder();
                        Console.WriteLine("embs");
                        emb.WithTitle(ctx.CommandsNext.RegisteredCommands[command].Module.ModuleType.Name);
                        if (ctx.CommandsNext.RegisteredCommands[command].Aliases.Count != 0)
                        {
                            emb.AddField("Aliases", $"``{string.Join("``, ``", ctx.CommandsNext.RegisteredCommands[command].Aliases)}``");
                        }
                        emb.AddField("Description", Desc.value);
                        foreach (var usages in Usage.value)
                        {
                            usg += $"m%{command} {usages}\n";
                        }
                        emb.AddField("Usage", usg);
                        await ctx.RespondAsync(embed: emb.Build());
                        return;
                    }
                    else if (ctx.CommandsNext.RegisteredCommands.Any(x => x.Value.Module.ModuleType.Name.ToLower() == command.ToLower()))
                    {
                        var disemb = new DiscordEmbedBuilder();
                        string list = $"List of {ctx.CommandsNext.RegisteredCommands.First(x => x.Value.Module.ModuleType.Name.ToLower() == command.ToLower()).Value.Module.ModuleType.Name}\n\n";
                        foreach (var Command in ctx.CommandsNext.RegisteredCommands.Where(x => x.Value.Module.ModuleType.Name.ToLower() == command.ToLower()))
                        {
                            if (ctx.Prefix.Contains(ctx.Client.CurrentUser.Id.ToString())) list += $"\n**m%{Command.Key}** *|-|* {Command.Value.Description}";
                            else list += $"\n**{ctx.Prefix}{Command.Key}** *|-|* {Command.Value.Description}";
                        }
                        disemb.WithDescription(list);
                        disemb.AddField("General Info","" +
                            "Developer of the original bot: ohlookitsderpy#3939\n" +
                            "Current developer: Speyd3r#3939\n" +
                            "Avatar by: Chillow#1945 [Twitter](https://twitter.com/SaikoSamurai)\n" +
                            "Support server: [Invite](https://discord.gg/YPPA2Pu)\n" +
                            "Bot invite: [Invite Link](https://meek.moe/miku)\n" +
                            "Support: [PayPal](https://paypal.me/speyd3r)|[Patreon](https://patreon.com/speyd3r)");
                        await ctx.RespondAsync(embed: disemb.Build());
                        return;
                    }
                }
                var inter = ctx.Client.GetInteractivity();
                Dictionary<string, List<DiscordEmbedBuilder>> Helps = new Dictionary<string, List<DiscordEmbedBuilder>>();
                foreach (var Command in ctx.CommandsNext.RegisteredCommands)
                {
                    if (ctx.CommandsNext.RegisteredCommands.Any(x => x.Value.Aliases.Any(y => y == Command.Key))) continue;
                    var mod = Command.Value.Module.ModuleType.Name;
                    if (!Helps.Any(x => x.Key == mod))
                    {
                        Helps.Add(mod, new List<DiscordEmbedBuilder>());
                    }
                    if (Helps[mod].Count == 0 || Helps[mod]?.Last().Fields.Count == 15)
                    {
                        Helps[mod].Add(new DiscordEmbedBuilder());
                        Helps[mod].Last().WithTitle(mod);
                    }
                    if (ctx.Prefix.Contains(ctx.Client.CurrentUser.Id.ToString())) Helps[mod].Last().AddField($"m%{Command.Key}", $"{Command.Value.Description}", true);
                    else Helps[mod].Last().AddField($"{ctx.Prefix}{Command.Key}", $"{Command.Value.Description}.", true);
                }
                List<Page> All = new List<Page>();
                string cats = "**";
                foreach (var cat in Helps)
                {
                    if (cats.Length != 0)
                    {
                        cats += ", " + cat.Key;
                    }
                    else
                    {
                        cats += cat.Key;
                    }
                    foreach (var com in cat.Value)
                    {
                        All.Add(new Page(embed: com));
                    }
                }
                cats += "**";
                All.Insert(0, new Page(embed: new DiscordEmbedBuilder()
                    .WithTitle("Hatsune Miku Discord Bot")
                    .WithDescription("Heyo! This is the new and updated version of the Hatsune Miku Discord Bot, currently still in development, so things like\n" +
                    "-Playlists\n" +
                    "-Some Text Commands and Descriptions\n" +
                    "are still missing but will be re-added over the next couple of days/weeks\n\n" +
                    $"Current Category List consists of {cats}, all commands are displayed on the following pages (use the reactions to switch pages)\n" +
                    $"For a more indepth view if certain commands you can use ``{ctx.Prefix}help (commandname)`` to see A more detailed description and usage\n")
                    .AddField("General Info", "" +
                            "Developer of the original bot: ohlookitsderpy#3939\n" +
                            "Current Developer: Speyd3r#3939\n" +
                            "Avatar by: Chillow#1945 ❤ [Twitter](https://twitter.com/SaikoSamurai)\n" +
                            "Support server: [Invite](https://discord.gg/YPPA2Pu)\n" +
                            "Bot invite: [Invite Link](https://meek.moe/miku)\n" +
                            "Support: [PayPal](https://paypal.me/speyd3r)|[Patreon](https://patreon.com/speyd3r)")));
                //.WithFooter("Next update at").WithTimestamp(Bot.Guilds[ctx.Guild.Id].UpdateTime)
                await inter.SendPaginatedMessageAsync(ctx.Channel, ctx.User, All, timeoutoverride: TimeSpan.FromMinutes(5));
            }
            catch (Exception ex)
            {
                await ctx.RespondAsync("Something went wrong :( either I'm missing the permissions to add reactions, to use embeds, to manage messages or all of those. If you are sure that I have all those permissions, join discord.gg/YPPA2Pu and slap ``@Speyd3r#3939``");
                Console.WriteLine(ex);
            }
        }

        [Command("invite")]
        [Description("Bot invitation link")]
        public async Task Invite(CommandContext ctx)
        {
            await ctx.RespondAsync("Thanks for your interest in the Hatsune Miku Bot!\n" +
                "https://meek.moe/miku");
        }

        [Command("ping")]
        [Description("Current ping to Discord's services")]
        public async Task Ping(CommandContext ctx)
        {
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().WithTitle("Current Ping").WithDescription($"Ping: {ctx.Client.Ping}").Build());
        }

        [Command("stats")]
        [Description("Some stats of the Bot!")]
        public async Task Stats(CommandContext ctx)
        {
            int GuildCount = 0;
            int UserCount = 0;
            int NoBotCount = 0;
            int ChannelCount = 0;
            foreach (var client in Bot.bot.ShardClients)
            {
                GuildCount = GuildCount + client.Value.Guilds.Count;
                foreach (var guild in client.Value.Guilds)
                {
                    UserCount = UserCount + guild.Value.MemberCount;
                    NoBotCount = NoBotCount + guild.Value.Members.Where(x => !x.Value.IsBot).Count();
                    ChannelCount = ChannelCount + guild.Value.Channels.Count;
                }
            }
            var emb = new DiscordEmbedBuilder().
                WithTitle("Stats").
                AddField("Guilds", GuildCount.ToString(), true).
                AddField("Users(Without Bots)", $"{UserCount}({NoBotCount})", true).
                AddField("Channels", ChannelCount.ToString(), true).
                AddField("Ping", ctx.Client.Ping.ToString(), true).
                WithThumbnailUrl(ctx.Client.CurrentUser.AvatarUrl);
            await ctx.RespondAsync(embed: emb.Build());
        }

        [Command("support")]
        [Description("A link to the support server, if you need help of find bugs")]
        public async Task Support(CommandContext ctx)
        {
            var emb = new DiscordEmbedBuilder().
                WithTitle("Support Server").
                WithDescription("Need help or is something broken?\n" +
                "[Join the support server](https://discord.gg/YPPA2Pu)").
                WithThumbnailUrl(ctx.Client.CurrentUser.AvatarUrl);
            await ctx.RespondAsync(embed: emb.Build());
        }
    }
}
