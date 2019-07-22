using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MikuSharp.Entities
{
   public partial class BotConfig
    {
        [JsonProperty("discordToken")]
        public string DiscordToken { get; set; }

        [JsonProperty("weebShToken")]
        public string WeebShToken { get; set; }

        [JsonProperty("youtubeApiToken")]
        public string YoutubeApiToken { get; set; }

        [JsonProperty("ksoftSiToken")]
        public string KsoftSiToken { get; set; }

        public string DbConnectString { get; set; }

        [JsonProperty("dbConfig")]
        public Config DbConfig { get; set; }

        [JsonProperty("lavaConfig")]
        public LavaConfig LavaConfig { get; set; }

        [JsonProperty("nndConfig")]
        public NndConfig NndConfig { get; set; }
    }

    public partial class Config
    {
        [JsonProperty("hostname")]
        public string Hostname { get; set; }

        [JsonProperty("user")]
        public string User { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }
    }

    public partial class LavaConfig
    {
        [JsonProperty("hostname")]
        public string Hostname { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("port")]
        public int Port { get; set; }
    }

    public partial class NndConfig
    {
        [JsonProperty("mail")]
        public string Mail { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("ftpConfig")]
        public Config FtpConfig { get; set; }
    }
}
