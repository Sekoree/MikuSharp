using DisCatSharp.Entities;
using DisCatSharp.Lavalink;
using DisCatSharp.Lavalink.Enums;

namespace MikuSharp.Entities;

public sealed class MusicSession
{
	public DiscordChannel CurrentChannel { get; set; }

	public DiscordGuild CurrentGuild { get; set; }

	public LavalinkSession LavalinkSession { get; set; }

	public LavalinkGuildPlayer LavalinkGuildPlayer
		=> this.LavalinkSession.GetGuildPlayer(this.CurrentGuild);

	public RepeatMode RepeatMode { get; set; }

	public DiscordMember CurrentDj { get; internal set; }
}
