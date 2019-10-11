using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using MikuSharp.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MikuSharp.Events
{
    public class VoiceChat
    {
        public static async Task LeftAlone(VoiceStateUpdateEventArgs e) 
        {
            try
            {
                if (!Bot.Guilds.Any(x => x.Key == e.Guild.Id)) return;
                var g = Bot.Guilds[e.Guild.Id];
                if (g.musicInstance == null
                    || g.musicInstance?.guildConnection?.IsConnected == false) return;
                if ((e.After?.Channel?.Users.Where(x => !x.IsBot).Count() == 0
                || e.Before?.Channel?.Users.Where(x => !x.IsBot).Count() == 0
                || e.Channel?.Users.Where(x => !x.IsBot).Count() == 0)
                && (e.After?.Channel?.Users.Contains(e.Guild.Members[e.Client.CurrentUser.Id]) == true
                || e.Before?.Channel?.Users.Contains(e.Guild.Members[e.Client.CurrentUser.Id]) == true
                || e.Channel?.Users.Contains(e.Guild.Members[e.Client.CurrentUser.Id]) == true)
                && g.musicInstance?.guildConnection?.Channel?.Users.Where(x => !x.IsBot).Count() == 0)
                {
                    if (g.musicInstance.playstate == Playstate.Playing)
                    {
                        g.musicInstance.guildConnection.Pause();
                        g.musicInstance.playstate = Playstate.Paused;
                        try
                        {
                            await g.musicInstance.usedChannel.SendMessageAsync(embed: new DiscordEmbedBuilder().WithDescription("**Paused** since everyone left the VC, connect back and use m%resume to continue playback otherwise I will disconnect in 5 min").Build());
                        }
                        catch { }
                    }
                    else
                    {
                        try
                        {
                            await g.musicInstance.usedChannel.SendMessageAsync(embed: new DiscordEmbedBuilder().WithDescription("Since everyone left the VC I will disconnect too in 5 min").Build());
                        }
                        catch { }
        }
                    g.musicInstance.aloneTime = DateTime.UtcNow;
                    g.musicInstance.aloneCTS = new CancellationTokenSource();
                    g.AloneCheckThread = Task.Run(g.CheckAlone);
                }
                else if (e.After?.Channel?.Users.Where(x => !x.IsBot).Count() != 0 && e.After?.Channel?.Users.Contains(e.Guild.Members[e.Client.CurrentUser.Id]) == true)
                {
                    if (g.musicInstance != null && g.musicInstance?.aloneCTS != null)
                    {
                        g.musicInstance.aloneCTS.Cancel();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
