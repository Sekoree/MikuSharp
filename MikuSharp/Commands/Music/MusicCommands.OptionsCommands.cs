using System;
using System.Linq;
using System.Threading.Tasks;

using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Lavalink.Enums;

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
			var musicSession = MikuBot.MusicSessions[ctx.GuildId.Value];
			musicSession.UpdateRepeatMode(mode);
			await musicSession.UpdateStatusMessageAsync(ctx.BuildMusicStatusEmbed(musicSession));
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Set repeat mode to: **{mode}**"));
		}

		[SlashCommand("shuffle", "Shuffle the queue")]
		public static async Task ShuffleAsync(InteractionContext ctx)
		{
			ArgumentNullException.ThrowIfNull(ctx.GuildId);
			var musicSession = MikuBot.MusicSessions[ctx.GuildId.Value];
			musicSession.LavalinkGuildPlayer.ShuffleQueue();
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Shuffled the queue!"));
		}
	}
}
