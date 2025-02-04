using MikuSharp.Attributes;
using MikuSharp.Utilities;

namespace MikuSharp.Commands.Music;

public partial class MusicCommands
{
	/// <summary>
	///     The options commands.
	/// </summary>
	[SlashCommandGroup("options", "Music options commands"), RequireUserAndBotVoicechatConnection]
	public class OptionsCommands : ApplicationCommandsModule
	{
		[SlashCommand("repeat", "Repeat the current song or the entire queue")]
		public static async Task RepeatAsync(
			InteractionContext ctx,
			[Option("mode", "New repeat mode"), ChoiceProvider(typeof(FixedOptionProviders.RepeatModeProvider))]
			RepeatMode mode
		)
		{
			ArgumentNullException.ThrowIfNull(ctx.GuildId);

			var guildId = ctx.GuildId.Value;
			var asyncLock = MikuBot.MusicSessionLocks.GetOrAdd(guildId, _ => new());
			using (await asyncLock.LockAsync(MikuBot.Cts.Token))
			{
				if (MikuBot.MusicSessions.TryGetValue(guildId, out var musicSession))
				{
					musicSession.UpdateRepeatMode(mode);
					await musicSession.UpdateStatusMessageAsync(musicSession.BuildMusicStatusEmbed());
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Set repeat mode to: **{mode}**"));
				}
			}
		}

		[SlashCommand("shuffle", "Shuffle the queue")]
		public static async Task ShuffleAsync(InteractionContext ctx)
		{
			ArgumentNullException.ThrowIfNull(ctx.GuildId);
			var guildId = ctx.GuildId.Value;
			var asyncLock = MikuBot.MusicSessionLocks.GetOrAdd(guildId, _ => new());
			using (await asyncLock.LockAsync(MikuBot.Cts.Token))
			{
				if (MikuBot.MusicSessions.TryGetValue(guildId, out var musicSession))
				{
					musicSession.LavalinkGuildPlayer.ShuffleQueue();
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Shuffled the queue!"));
				}
			}
		}
	}
}
