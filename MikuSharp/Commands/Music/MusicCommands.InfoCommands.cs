using MikuSharp.Attributes;

namespace MikuSharp.Commands.Music;

public partial class MusicCommands
{
	/// <summary>
	///     The info commands.
	/// </summary>
	[SlashCommandGroup("info", "Music info commands"), RequireUserAndBotVoicechatConnection]
	public class InfoCommands : ApplicationCommandsModule
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
