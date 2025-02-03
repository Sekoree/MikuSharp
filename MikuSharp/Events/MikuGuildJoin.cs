using System.Threading.Tasks;

using DisCatSharp;
using DisCatSharp.EventArgs;

namespace MikuSharp.Events;

/// <summary>
///     Event handler for the miku guild.
/// </summary>
public class MikuGuild
{
	/// <summary>
	///     Fired when a new guild member joins.
	/// </summary>
	/// <param name="sender">The client.</param>
	/// <param name="args">The event args.</param>
	public static async Task OnJoinAsync(DiscordClient sender, GuildMemberAddEventArgs args)
	{
		await Task.FromResult(true);
	}

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
			var memberRole = args.Guild.GetRole(memberRoleId);
			await args.Member.GrantRoleAsync(memberRole);
		}

		await Task.FromResult(true);
	}
}
