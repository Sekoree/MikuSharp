using System;
using System.Linq;
using System.Threading.Tasks;

using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;

using Microsoft.Extensions.Logging;

using MikuSharp.Enums;

namespace MikuSharp.Events;

public class VoiceChat
{
    public static async Task LeftAlone(DiscordClient client, VoiceStateUpdateEventArgs e)
    {
        try
        {
            if (MikuBot.Guilds.All(x => x.Key != e.Guild.Id))
                return;

            var g = MikuBot.Guilds[e.Guild.Id];
            if (g.MusicInstance == null || g.MusicInstance?.GuildConnection?.IsConnected == false) return;

            if ((e.After?.Channel?.Users?.Count(x => !x.IsBot) == 0 || e.Before?.Channel?.Users?.Count(x => !x.IsBot) == 0 || e.Channel?.Users?.Count(x => !x.IsBot) == 0) &&
                (e.After?.Channel?.Users.Contains(e.Guild.Members[client.CurrentUser.Id]) == true ||
                 e.Before?.Channel?.Users.Contains(e.Guild.Members[client.CurrentUser.Id]) == true ||
                 e.Channel?.Users.Contains(e.Guild.Members[client.CurrentUser.Id]) == true) &&
                g.MusicInstance?.GuildConnection?.Channel?.Users?.Count(x => !x.IsBot) == 0)
            {
                if (g.MusicInstance.Playstate == Playstate.Playing)
                {
                    await g.MusicInstance.GuildConnection.PauseAsync();
                    g.MusicInstance.Playstate = Playstate.Paused;

                    try
                    {
                        await g.MusicInstance.UsedChannel.SendMessageAsync(new DiscordEmbedBuilder()
                            .WithDescription("**Paused** since everyone left the VC, connect back and use m%resume to continue playback otherwise I will disconnect in 5 min").Build());
                    }
                    catch
                    { }
                }
                else
                    try
                    {
                        await g.MusicInstance.UsedChannel.SendMessageAsync(new DiscordEmbedBuilder().WithDescription("Since everyone left the VC I will disconnect too in 5 min").Build());
                    }
                    catch
                    { }

                g.MusicInstance.AloneTime = DateTime.UtcNow;
                g.MusicInstance.AloneCts = new();
                g.AloneCheckThread = Task.Run(g.CheckAlone);
            }
            else if (e.After?.Channel?.Users?.Count(x => !x.IsBot) != 0 && e.After?.Channel?.Users.Contains(e.Guild.Members[client.CurrentUser.Id]) == true)
                if (g.MusicInstance is { AloneCts: not null })
                    g.MusicInstance.AloneCts.Cancel();
        }
        catch (Exception ex)
        {
            client.Logger.LogError(ex.Message);
            client.Logger.LogError(ex.StackTrace);
        }
    }
}
