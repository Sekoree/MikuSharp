using MikuSharp.Attributes;
using MikuSharp.Utilities;

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

		/// <summary>
		///     Skips to the next song.
		/// </summary>
		/// <param name="ctx">The interaction context.</param>
		[SlashCommand("skip", "Skips to the next song")]
		public async Task SkipAsync(InteractionContext ctx)
		{
			await ctx.ExecuteWithMusicSessionAsync(async (discard, musicSession) =>
			{
				if (musicSession.LavalinkGuildPlayer.TryPeekQueue(out _))
				{
					await musicSession.LavalinkGuildPlayer.SkipAsync();
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Successfully skipped the song!"));
				}
				else
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Cannot skip as there are no more songs in the queue."));
			});
		}
	}
}
