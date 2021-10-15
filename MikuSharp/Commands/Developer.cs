using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Entities;

using System;
using System.IO;
using System.Threading.Tasks;

namespace MikuSharp.Commands
{
    /// <summary>
    /// The developer commands.
    /// </summary>
    public class Developer : ApplicationCommandsModule
    {
        /// <summary>
        /// Gets the debug log.
        /// </summary>
        /// <param name="ctx">The interaction context.</param>
        [SlashCommand("dbg", "Get the logs of today", false)]
        public static async Task GetDebugLogAsync(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Trying to get log").AsEphemeral(false));
            DateTime now = DateTime.Now;
            var target_file = $"miku_log{now.ToString("yyyy/MM/dd").Replace("/","")}.txt";
            if (!File.Exists(target_file))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Failed to get log"));
                return;
            }
            else
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Found log {Formatter.Bold(target_file)}"));
            }
            try
            {
                if (!File.Exists($"temp-{target_file}"))
                {
                    File.Copy(target_file, $"temp-{target_file}");
                }
                else
                {
                    File.Delete($"temp-{target_file}");
                    File.Copy(target_file, $"temp-{target_file}");
                }
                FileStream log = new FileStream(path: $"temp-{target_file}", FileMode.Open, FileAccess.Read);
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddFile(target_file, log, true).WithContent($"Log {Formatter.Bold(target_file)}").AsEphemeral(false));
                log.Close();
                log.Dispose();
                File.Delete($"temp-{target_file}");
            }
            catch (Exception ex)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(ex.Message).AsEphemeral(false));
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(ex.StackTrace).AsEphemeral(false));
            }
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done"));
        }
    }
}
