using MikuSharp.Attributes;

namespace MikuSharp.Commands.Music;

public partial class MusicCommands
{
	/// <summary>
	///     The queue commands.
	/// </summary>
	[SlashCommandGroup("queue", "Music queue commands"), RequireUserAndBotVoicechatConnection]
	public class QueueCommands : ApplicationCommandsModule
	{
		/// <summary>
		///     A dummy command.
		/// </summary>
		/// <param name="ctx">The interaction context.</param>
		[SlashCommand("dummy", "Dummy command")]
		public async Task DummyCommand(InteractionContext ctx)
			=> await ctx.EditResponseAsync("This command is a placeholder and does nothing.");
	}
}
