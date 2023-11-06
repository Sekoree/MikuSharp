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
		if (args.PendingBefore is true)
		{
			if (args.PendingAfter is false)
			{
				ulong memberRoleId = 483280207927574528;
				var memberRole = args.Guild.GetRole(memberRoleId);
				await args.Member.GrantRoleAsync(memberRole);
			}
		}
		await Task.FromResult(true);
	}
}
