using MikuSharp.Enums;

namespace MikuSharp.Events;

public class VoiceChat
{
	public static async Task LeftAlone(DiscordClient client, VoiceStateUpdateEventArgs e)
	{
		try
		{
			if (!MikuBot.Guilds.Any(x => x.Key == e.Guild.Id))
				return;
			var g = MikuBot.Guilds[e.Guild.Id];
			if (g.MusicInstance == null
				|| g.MusicInstance?.GuildConnection?.IsConnected == false)
				return;
			if ((e.After?.Channel?.Users.Where(x => !x.IsBot).Count() == 0
			|| e.Before?.Channel?.Users.Where(x => !x.IsBot).Count() == 0
			|| e.Channel?.Users.Where(x => !x.IsBot).Count() == 0)
			&& (e.After?.Channel?.Users.Contains(e.Guild.Members[client.CurrentUser.Id]) == true
			|| e.Before?.Channel?.Users.Contains(e.Guild.Members[client.CurrentUser.Id]) == true
			|| e.Channel?.Users.Contains(e.Guild.Members[client.CurrentUser.Id]) == true)
			&& g.MusicInstance?.GuildConnection?.Channel?.Users.Where(x => !x.IsBot).Count() == 0)
			{
				if (g.MusicInstance.Playstate == PlayState.Playing)
				{
					await g.MusicInstance.GuildConnection.PauseAsync();
					g.MusicInstance.Playstate = PlayState.Paused;
					try
					{
						await g.MusicInstance.CommandChannel.SendMessageAsync(embed: new DiscordEmbedBuilder().WithDescription("**Paused** since everyone left the VC, connect back and use m%resume to continue playback otherwise I will disconnect in 5 min").Build());
					}
					catch { }
				}
				else
				{
					try
					{
						await g.MusicInstance.CommandChannel.SendMessageAsync(embed: new DiscordEmbedBuilder().WithDescription("Since everyone left the VC I will disconnect too in 5 min").Build());
					}
					catch { }
				}
				g.MusicInstance.AloneTime = DateTime.UtcNow;
				g.MusicInstance.AloneCheckCancellationToken = new();
				g.AloneCheckThread = Task.Run(g.CheckAlone, MikuBot._cts.Token);
			}
			else if (e.After?.Channel?.Users.Where(x => !x.IsBot).Count() != 0 && e.After?.Channel?.Users.Contains(e.Guild.Members[client.CurrentUser.Id]) == true)
			{
				if (g.MusicInstance != null && g.MusicInstance?.AloneCheckCancellationToken != null)
				{
					g.MusicInstance.AloneCheckCancellationToken.Cancel();
				}
			}
		}
		catch (Exception ex)
		{
			client.Logger.LogError("{msg}", ex.Message);
			client.Logger.LogError("{stack}", ex.StackTrace);
		}
	}
}
