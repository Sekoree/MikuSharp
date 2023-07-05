using DisCatSharp.Lavalink.Enums;

using MikuSharp.Enums;
using MikuSharp.Events;
using MikuSharp.Utilities;

namespace MikuSharp.Entities;

public class MusicInstance
{
	public int ShardID { get; set; }

	public DiscordChannel CommandChannel { get; set; }

	public DiscordChannel VoiceChannel { get; set; }

	public PlayState Playstate { get; set; }

	public RepeatMode RepeatMode { get; set; }

	public ShuffleMode ShuffleMode { get; set; }

	public int RepeatAllPosition { get; set; }

	public DateTime AloneTime { get; set; }

	public CancellationTokenSource AloneCheckCancellationToken { get; set; }

	public LavalinkSession Session { get; set; }

	public LavalinkGuildPlayer GuildPlayer { get; set; }

	public QueueEntry CurrentSong { get; set; }

	public QueueEntry LastSong { get; set; }

	public MusicInstance(LavalinkSession session, int shardId)
	{
		this.ShardID = shardId;
		this.Session = session;
		this.CommandChannel = null;
		this.Playstate = PlayState.NotPlaying;
		this.RepeatMode = RepeatMode.Off;
		this.RepeatAllPosition = 0;
		this.ShuffleMode = ShuffleMode.Off;
	}

	public async Task<LavalinkGuildPlayer> ConnectToChannel(DiscordChannel channel)
	{
		switch (channel.Type)
		{
			case ChannelType.Voice:
			case ChannelType.Stage:
			{
				this.GuildPlayer = await this.Session.ConnectAsync(channel);
				this.VoiceChannel = channel;
				return this.GuildPlayer;
			}
			default:
				return null;
		}
	}
	public async Task<TrackResult> QueueSong(string url_or_name, InteractionContext ctx, int position = -1)
	{
		var queue = await Database.GetQueueAsync(ctx.Guild);
		var inter = ctx.Client.GetInteractivity();
		// NicoNicoNii
		if (url_or_name.IsNndUrl())
		{
			var msg = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Processing NND Video...").AsEphemeral());
			var split = url_or_name.Split("/".ToCharArray());
			var nico_nico_id = split.First(x => x.StartsWith("sm") || x.StartsWith("nm")).Split("?")[0];
			FtpClient client = new(MikuBot.Config.NndConfig.FtpConfig.Hostname, new NetworkCredential(MikuBot.Config.NndConfig.FtpConfig.User, MikuBot.Config.NndConfig.FtpConfig.Password));
			client.Connect();
			if (!client.FileExists($"{nico_nico_id}.mp3"))
			{
				await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Preparing download..."));
				var ex = await ctx.GetNNDAsync(url_or_name, nico_nico_id, msg.Id);
				if (ex == null)
				{
					await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Please try again or verify the link"));
					return null;
				}
				await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Uploading"));
				client.UploadStream(ex, $"{nico_nico_id}.mp3", FtpRemoteExists.Skip, true);
			}
			var Track = await this.NodeConnection.Rest.GetTracksAsync(new Uri($"https://nnd.meek.moe/new/{nico_nico_id}.mp3"));
			if (position == -1)
				await Database.AddToQueueAsync(ctx.Guild, ctx.Member.Id, Track.Tracks.First().TrackString);
			else
				await Database.InsertToQueueAsync(ctx.Guild, ctx.Member.Id, Track.Tracks.First().TrackString, position);
			if (this.GuildPlayer.IsConnected && (this.Playstate == PlayState.NotPlaying || this.Playstate == PlayState.Stopped))
				await this.PlaySong();
			return new TrackResult(Track.PlaylistInfo, Track.Tracks.First());
		}
		// Bilibili
		else if (url_or_name.ToLower().StartsWith("https://www.bilibili.com")
			|| url_or_name.ToLower().StartsWith("http://www.bilibili.com"))
		{
			var msg = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Processing Bilibili Video...").AsEphemeral());
			url_or_name = url_or_name.Replace("https://www.bilibili.com/", "");
			url_or_name = url_or_name.Replace("http://www.bilibili.com/", "");
			var split = url_or_name.Split("/".ToCharArray());
			if (!split.Contains("video"))
			{
				await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Failure"));
				return null;
			}
			var nndID = split[1].Split("?")[0];
			FtpClient client = new(MikuBot.Config.NndConfig.FtpConfig.Hostname, new NetworkCredential(MikuBot.Config.NndConfig.FtpConfig.User, MikuBot.Config.NndConfig.FtpConfig.Password));
			client.Connect();
			if (!client.FileExists($"{nndID}.mp3"))
			{
				await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Preparing download..."));
				var ex = await ctx.GetBilibiliAsync(nndID, msg.Id);
				if (ex == null)
				{
					await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Please try again or verify the link"));
					return null;
				}
				await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Uploading..."));
				client.UploadStream(ex, $"{nndID}.mp3", FtpRemoteExists.Skip, true);
			}
			var Track = await this.NodeConnection.Rest.GetTracksAsync(new Uri($"https://nnd.meek.moe/new/{nndID}.mp3"));
			if (position == -1)
				await Database.AddToQueueAsync(ctx.Guild, ctx.Member.Id, Track.Tracks.First().TrackString);
			else
				await Database.InsertToQueueAsync(ctx.Guild, ctx.Member.Id, Track.Tracks.First().TrackString, position);
			if (this.GuildPlayer.IsConnected && (this.Playstate == PlayState.NotPlaying || this.Playstate == PlayState.Stopped))
				await this.PlaySong();
			return new TrackResult(Track.PlaylistInfo, Track.Tracks.First());
		}
		// Http(s) stream/file
		else if (url_or_name.StartsWith("http://") | url_or_name.StartsWith("https://"))
		{
			try
			{
				var s = await this.NodeConnection.Rest.GetTracksAsync(new Uri(url_or_name));
				switch (s.LoadResultType)
				{
					case LavalinkLoadResultType.Error:
					{
						await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().AddEmbed(new DiscordEmbedBuilder().WithTitle("Failed to load").WithDescription("Loading this song/playlist failed, please try again, reasons could be:\name_or_url" +
							"> Playlist is set to private or unlisted\name_or_url" +
							"> The song is unavailable/deleted").Build()));
						return null;
					};
					case LavalinkLoadResultType.Empty:
					{
						await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().AddEmbed(new DiscordEmbedBuilder().WithTitle("Failed to load").WithDescription("No song/playlist was found with this URL, please try again/a different one").Build()));
						return null;
					};
					case LavalinkLoadResultType.Playlist:
					{
						// This is a playlist
						if (s.PlaylistInfo.SelectedTrack == -1)
						{
							List<DiscordButtonComponent> buttons = new(2)
							{
								new DiscordButtonComponent(ButtonStyle.Success, "yes", "Add entire playlist"),
								new DiscordButtonComponent(ButtonStyle.Primary, "no", "Don't add")
							};
							var msg = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().WithContent("Playlist link detected!").AddEmbed(new DiscordEmbedBuilder()
									.WithDescription("Choose how to handle the playlist link")
									.WithAuthor($"Requested by {ctx.Member.UsernameWithDiscriminator} || Timeout 30 seconds", iconUrl: ctx.Member.AvatarUrl)
									.Build()).AddComponents(buttons));
							var resp = await inter.WaitForButtonAsync(msg, ctx.User, TimeSpan.FromSeconds(30));
							if (resp.TimedOut)
							{
								buttons.ForEach(x => x.Disable());
								await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(buttons).WithContent("Timed out!"));
								return null;
							}
							else if (resp.Result.Id == "yes")
							{
								await resp.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
								buttons.ForEach(x => x.Disable());
								await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(buttons).WithContent("Adding entire playlist"));
								await Database.AddToQueueAsync(ctx.Guild, ctx.Member.Id, s.Tracks.ToList());
								if (this.GuildPlayer.IsConnected && (this.Playstate == PlayState.NotPlaying || this.Playstate == PlayState.Stopped))
									await this.PlaySong();
								return new TrackResult(s.PlaylistInfo, s.Tracks);
							}
							else
							{
								await resp.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
								buttons.ForEach(x => x.Disable());
								await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(buttons).WithContent("Canceled!"));
								return null;
							}
						}
						// We detected an attached playlist link
						else
						{
							List<DiscordButtonComponent> buttons = new(3)
							{
								new DiscordButtonComponent(ButtonStyle.Primary, "yes", "Add only referred song"),
								new DiscordButtonComponent(ButtonStyle.Success, "yes", "Add the entire playlist"),
								new DiscordButtonComponent(ButtonStyle.Danger, "no", "Cancel")
							};
							var msg = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().AddEmbed(new DiscordEmbedBuilder()
									.WithTitle("Link with Playlist detected!")
									.WithDescription("Please choose how to handle the playlist link")
									.WithAuthor($"Requested by {ctx.Member.UsernameWithDiscriminator} || Timeout 30 seconds", iconUrl: ctx.Member.AvatarUrl)
									.Build()).AddComponents(buttons));
							var resp = await inter.WaitForButtonAsync(msg, ctx.User, TimeSpan.FromSeconds(30));
							if (resp.TimedOut)
							{
								buttons.ForEach(x => x.Disable());
								await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(buttons).WithContent("Timed out!"));
								return null;
							}
							else if (resp.Result.Id == "yes")
							{
								await resp.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
								buttons.ForEach(x => x.Disable());
								await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(buttons).WithContent($"Adding single song: {s.Tracks.ElementAt(s.PlaylistInfo.SelectedTrack).Title}"));
								if (position == -1)
									await Database.AddToQueueAsync(ctx.Guild, ctx.Member.Id, s.Tracks.ElementAt(s.PlaylistInfo.SelectedTrack).TrackString);
								else
									await Database.InsertToQueueAsync(ctx.Guild, ctx.Member.Id, s.Tracks.ElementAt(s.PlaylistInfo.SelectedTrack).TrackString, position);
								if (this.GuildPlayer.IsConnected && (this.Playstate == PlayState.NotPlaying || this.Playstate == PlayState.Stopped))
									await this.PlaySong();
								return new TrackResult(s.PlaylistInfo, s.Tracks.ElementAt(s.PlaylistInfo.SelectedTrack));
							}
							else if (resp.Result.Id == "all")
							{
								await resp.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
								buttons.ForEach(x => x.Disable());
								await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(buttons).WithContent($"Adding entire playlist: {s.PlaylistInfo.Name}"));
								if (position == -1)
									await Database.AddToQueueAsync(ctx.Guild, ctx.Member.Id, s.Tracks);
								else
								{
									s.Tracks.Reverse();
									await Database.InsertToQueueAsync(ctx.Guild, ctx.Member.Id, s.Tracks, position);
								}
								if (this.GuildPlayer.IsConnected && (this.Playstate == PlayState.NotPlaying || this.Playstate == PlayState.Stopped))
									await this.PlaySong();
								return new TrackResult(s.PlaylistInfo, s.Tracks);
							}
							else
							{
								await resp.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
								buttons.ForEach(x => x.Disable());
								await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(buttons).WithContent("Canceled!"));
								return null;
							}
						}
					};
					// We play a single song
					default:
					{
						await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().WithContent($"Playing single song: {s.Tracks.First().Title}"));
						if (position == -1)
							await Database.AddToQueueAsync(ctx.Guild, ctx.Member.Id, s.Tracks.First().TrackString);
						else
							await Database.InsertToQueueAsync(ctx.Guild, ctx.Member.Id, s.Tracks.First().TrackString, position);
						if (this.GuildPlayer.IsConnected && (this.Playstate == PlayState.NotPlaying || this.Playstate == PlayState.Stopped))
							await this.PlaySong();
						return new TrackResult(s.PlaylistInfo, s.Tracks.First());
					};
				}
			}
			catch (Exception ex)
			{
				ctx.Client.Logger.LogError("{msg}", ex.Message);
				ctx.Client.Logger.LogError("{stack}", ex.StackTrace);
				return null;
			}
		}
		// A search is triggered
		else
		{
			var type = LavalinkSearchType.Youtube;
			if (url_or_name.StartsWith("ytsearch:"))
			{
				url_or_name = url_or_name.Replace("ytsearch:", "");
				type = LavalinkSearchType.Youtube;
			}
			else if (url_or_name.StartsWith("scsearch:"))
			{
				url_or_name = url_or_name.Replace("ytsearch:", "");
				type = LavalinkSearchType.SoundCloud;
			}
			else if (url_or_name.StartsWith("spsearch:"))
			{
				url_or_name = url_or_name.Replace("spsearch:", "");
				type = LavalinkSearchType.Spotify;
			}
			else if (url_or_name.StartsWith("amsearch:"))
			{
				url_or_name = url_or_name.Replace("amsearch:", "");
				type = LavalinkSearchType.AppleMusic;
			}

			var s = await this.NodeConnection.Rest.GetTracksAsync(url_or_name, type);
			switch (s.LoadResultType)
			{
				case LavalinkLoadResultType.Error:
				{
					ctx.Client.Logger.LogDebug("Load failed");
					await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().AddEmbed(new DiscordEmbedBuilder().WithTitle("Failed to load").WithDescription("Loading this song/playlist failed, please try again, reason could be:\name_or_url" +
						"> The song is unavailable/deleted").Build()));
					return null;
				};
				case LavalinkLoadResultType.Empty:
				{
					ctx.Client.Logger.LogDebug("No matches");
					await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().AddEmbed(new DiscordEmbedBuilder().WithTitle("Failed to load").WithDescription("No song was found, please try again").Build()));
					return null;
				};
				default:
				{
					ctx.Client.Logger.LogDebug("Found something");
					var leng = s.Tracks.Count;
					if (leng > 5)
						leng = 5;
					List<DiscordStringSelectComponentOption> selectOptions = new(leng);
					var em = new DiscordEmbedBuilder()
							.WithTitle("Results!")
							.WithDescription("Please select a track:\name_or_url")
							.WithAuthor($"Requested by {ctx.Member.UsernameWithDiscriminator} || Timeout 30 seconds", iconUrl: ctx.Member.AvatarUrl);
					for (var i = 0; i < leng; i++)
					{
						em.AddField(new DiscordEmbedField($"{i + 1}.{s.Tracks.ElementAt(i).Title} [{s.Tracks.ElementAt(i).Length}]", $"by {s.Tracks.ElementAt(i).Author} [Link]({s.Tracks.ElementAt(i).Uri})"));
						selectOptions.Add(new DiscordStringSelectComponentOption(s.Tracks.ElementAt(i).Title, i.ToString(), $"by {s.Tracks.ElementAt(i).Author}. Length: {s.Tracks.ElementAt(i).Length}"));
					}
					DiscordStringSelectComponent select = new("Select song to play", selectOptions, minOptions: 1, maxOptions: 1);
					var msg = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().AddEmbed(em.Build()).AddComponents(select));
					var resp = await inter.WaitForSelectAsync(msg, ctx.User, select.CustomId, ComponentType.StringSelect, TimeSpan.FromSeconds(30));
					if (resp.TimedOut)
					{
						select.Disable();
						await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(select).WithContent("Timed out!"));
						return null;
					}
					await resp.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
					var trackSelect = Convert.ToInt32(resp.Result.Values.First());
					var track = s.Tracks.ElementAt(trackSelect);
					select.Disable();
					await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(select).WithContent($"Chose {track.Title}"));
					if (position == -1)
						await Database.AddToQueueAsync(ctx.Guild, ctx.Member.Id, track.Encoded);
					else
						await Database.InsertToQueueAsync(ctx.Guild, ctx.Member.Id, track.Encoded, position);
					if (this.GuildPlayer.IsConnected && (this.Playstate == PlayState.NotPlaying || this.Playstate == PlayState.Stopped))
						await this.PlaySong();
					return new TrackResult(s.PlaylistInfo, track);
				};
			}
		}
	}

	public async Task<QueueEntry> PlaySong()
	{
		var queue = await Database.GetQueueAsync(this.VoiceChannel.Guild);
		var cur = this.LastSong;
		if (queue.Count != 1 && this.RepeatMode == RepeatMode.All)
			this.RepeatAllPosition++;
		if (this.RepeatAllPosition >= queue.Count)
			this.RepeatAllPosition = 0;
		this.CurrentSong = this.ShuffleMode == ShuffleMode.Off ? queue[0] : queue[new Random().Next(0, queue.Count)];
		if (this.RepeatMode == RepeatMode.All)
			this.CurrentSong = queue[this.RepeatAllPosition];
		if (this.RepeatMode == RepeatMode.On)
			this.CurrentSong = cur;
		MikuBot.ShardedClient.Logger.LogDebug("PlaySong(): {track}", this.CurrentSong?.Track.Info.Identifier);
		this.GuildPlayer.TrackEnded += Lavalink.LavalinkTrackFinished;
		this.Playstate = PlayState.Playing;
		await Task.Run(async () => await this.GuildPlayer.PlayAsync(this.CurrentSong.Track), MikuBot._canellationTokenSource.Token);
		return this.CurrentSong;
	}
}

//     B/S(｀・ω・´) ❤️ (´ω｀)U/C
