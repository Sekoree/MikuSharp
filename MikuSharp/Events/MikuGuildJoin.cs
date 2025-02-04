using DisCatSharp.EventArgs;

namespace MikuSharp.Events;

/// <summary>
///     Event handler for the miku guild.
/// </summary>
public class MikuGuild
{
	/// <summary>
	///     Fired when a guild member is updated.
	/// </summary>
	/// <param name="sender">The discord client.</param>
	/// <param name="args">The event args.</param>
	public static async Task OnUpdateAsync(DiscordClient sender, GuildMemberUpdateEventArgs args)
	{
		if (args is { PendingBefore: true, PendingAfter: false })
		{
			ulong memberRoleId = 483280207927574528;
			var memberRole = await args.Guild.GetRoleAsync(memberRoleId);
			await args.Member.GrantRoleAsync(memberRole);
		}

		await Task.FromResult(true);
	}
}
