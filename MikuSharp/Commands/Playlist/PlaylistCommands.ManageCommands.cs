namespace MikuSharp.Commands.Playlist;

public partial class PlaylistCommands
{
	[SlashCommandGroup("manage", "Playlist management")]
	public class ManageCommands : ApplicationCommandsModule
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
