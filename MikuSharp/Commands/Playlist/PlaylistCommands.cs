using System.Threading.Tasks;

using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Enums;

using MikuSharp.Attributes;

namespace MikuSharp.Commands.Playlist;

/// <summary>
///     The playlist commands
/// </summary>
[SlashCommandGroup("playlist", "Playlist commands", allowedContexts: [InteractionContextType.Guild], integrationTypes: [ApplicationCommandIntegrationTypes.GuildInstall]), DeferResponseAsync(true), EnsureLavalinkSession]
public partial class PlaylistCommands : ApplicationCommandsModule
{
	/// <summary>
	///     A dummy command.
	/// </summary>
	/// <param name="ctx">The interaction context.</param>
	[SlashCommand("dummy", "Dummy command")]
	public async Task DummyCommand(InteractionContext ctx)
		=> await ctx.EditResponseAsync("This command is a placeholder and does nothing.");
}
