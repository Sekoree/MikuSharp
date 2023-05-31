using MikuSharp.Enums;

namespace MikuSharp.Events;

public class VoiceChat
{
	public static async Task LeftAlone(DiscordClient client, VoiceStateUpdateEventArgs e)
	{
		try
		{
			if (!MikuBot.Guilds.ContainsKey(e.Guild.Id))
				return;
			var guild = MikuBot.Guilds[e.Guild.Id];
			var musicInstance = guild.MusicInstance;

			if (musicInstance == null || musicInstance.GuildConnection?.IsConnected == false)
				return;

			var currentUser = e.Guild.Members[client.CurrentUser.Id];

			var afterChannelUserCount = e.After?.Channel?.Users.Count(x => !x.IsBot) ?? 0;
			var currentChannelUserCount = e.Channel?.Users.Count(x => !x.IsBot) ?? 0;
			var guildConnectionUserCount = musicInstance.GuildConnection?.Channel?.Users.Count(x => !x.IsBot) ?? 0;

			var isCurrentUserInChannel = currentUser?.VoiceState?.ChannelId == e.Channel?.Id;

			if ((afterChannelUserCount == 0 || currentChannelUserCount == 0)
				&& !isCurrentUserInChannel && guildConnectionUserCount == 0)
			{
				if (musicInstance.Playstate == PlayState.Playing)
				{
					await musicInstance.GuildConnection.PauseAsync();
					musicInstance.Playstate = PlayState.Paused;

					try
					{
						await musicInstance.CommandChannel.SendMessageAsync(new DiscordEmbedBuilder()
							.WithDescription("**Paused** since everyone left the VC, connect back and use m%resume to continue playback otherwise I will disconnect in 5 min")
							.Build());
					}
					catch { }
				}
				else
				{
					try
					{
						await musicInstance.CommandChannel.SendMessageAsync(new DiscordEmbedBuilder()
							.WithDescription("Since everyone left the VC I will disconnect too in 5 min")
							.Build());
					}
					catch { }
				}

				musicInstance.AloneTime = DateTime.UtcNow;
				musicInstance.AloneCheckCancellationToken = new();
				guild.AloneCheckThread = Task.Run(guild.CheckAlone, MikuBot._canellationTokenSource.Token);
			}
			else if (afterChannelUserCount != 0 && isCurrentUserInChannel)
			{
				if (musicInstance != null && musicInstance.AloneCheckCancellationToken != null)
				{
					musicInstance.AloneCheckCancellationToken.Cancel(); try
					{
						await musicInstance.CommandChannel.SendMessageAsync(new DiscordEmbedBuilder()
							.WithDescription("Aborted the 5 minuten alone check :3")
							.Build());
					}
					catch { }
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
