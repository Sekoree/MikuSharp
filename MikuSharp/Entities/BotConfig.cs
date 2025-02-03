using Newtonsoft.Json;

namespace MikuSharp.Entities;

public sealed class BotConfig
{
#if DEBUG
	[JsonProperty("discordTokenDev")]
#else
	[JsonProperty("discordToken")]
#endif
	public string DiscordToken { get; set; }

	[JsonProperty("discordBotListToken")]
	public string DiscordBotListToken { get; set; }

	[JsonProperty("weebShToken")]
	public string WeebShToken { get; set; }

	[JsonProperty("youtubeApiToken")]
	public string YoutubeApiToken { get; set; }

	[JsonProperty("ksoftSiToken")]
	public string KsoftSiToken { get; set; }

	[JsonIgnore]
	public string DbConnectString { get; set; }

	[JsonProperty("dbConfig")]
	public DatabaseConfig DbConfig { get; set; }

	[JsonProperty("lavaConfig")]
	public LavalinkConfig LavaConfig { get; set; }

	[JsonProperty("nndConfig")]
	public NndConfig NndConfig { get; set; }
}

public sealed class DatabaseConfig
{
	[JsonProperty("hostname")]
	public string Hostname { get; set; }

	[JsonProperty("user")]
	public string User { get; set; }

	[JsonProperty("password")]
	public string Password { get; set; }

	[JsonProperty("database")]
	public string Database { get; set; }
}

public sealed class LavalinkConfig
{
	[JsonProperty("hostname")]
	public string Hostname { get; set; }

	[JsonProperty("password")]
	public string Password { get; set; }

	[JsonProperty("port")]
	public int Port { get; set; }
}

public sealed class NndConfig
{
	[JsonProperty("mail")]
	public string Mail { get; set; }

	[JsonProperty("password")]
	public string Password { get; set; }

	[JsonProperty("ftpConfig")]
	public DatabaseConfig FtpConfig { get; set; }
}
