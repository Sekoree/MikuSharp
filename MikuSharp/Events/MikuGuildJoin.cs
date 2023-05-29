namespace MikuSharp.Events;

/// <summary>
/// Event handler for the miku guild.
/// </summary>
public class MikuGuildEvents
{
	/// <summary>
	/// Fired when a guild member is updated.
	/// </summary>
	/// <param name="sender">The discord client.</param>
	/// <param name="args">The event args.</param>
	public static async Task OnGuildMemberUpdateAsync(DiscordClient sender, GuildMemberUpdateEventArgs args)
	{
		if (args.PendingBefore.HasValue && args.PendingBefore == true)
		{
			if (args.PendingAfter.HasValue && args.PendingAfter == false)
			{
				ulong member_role_id = 483280207927574528;
				var member_role = args.Guild.GetRole(member_role_id);
				await args.Member.GrantRoleAsync(member_role);
			}
		}
		await Task.FromResult(true);
	}
}
