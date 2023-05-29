using MikuSharp.Enums;
using MikuSharp.Events;
using MikuSharp.Utilities;

namespace MikuSharp.Entities;

public class MusicInstance
{
	public int shardID { get; set; }
	public DiscordChannel usedChannel { get; set; }
	public DiscordChannel voiceChannel { get; set; }
	public Playstate playstate { get; set; }
	public RepeatMode repeatMode { get; set; }
	public int repeatAllPos { get; set; }
	public ShuffleMode shuffleMode { get; set; }
	public DateTime aloneTime { get; set; }
	public CancellationTokenSource aloneCTS { get; set; }
	public LavalinkNodeConnection nodeConnection { get; set; }
	public LavalinkGuildConnection guildConnection { get; set; }
	public QueueEntry currentSong { get; set; }
	public QueueEntry lastSong { get; set; }

	public MusicInstance(LavalinkNodeConnection node, int shard)
	{
		shardID = shard;
		nodeConnection = node;
		usedChannel = null;
		playstate = Playstate.NotPlaying;
		repeatMode = RepeatMode.Off;
		repeatAllPos = 0;
		shuffleMode = ShuffleMode.Off;
	}

	public async Task<LavalinkGuildConnection> ConnectToChannel(DiscordChannel channel)
	{
		switch (channel.Type)
		{
			case ChannelType.Voice:
				{
					guildConnection = await nodeConnection.ConnectAsync(channel);
					voiceChannel = channel;
					return guildConnection;
				}
			default:
				return null;
		}
	}
	public async Task<TrackResult> QueueSong(string n, InteractionContext ctx, int pos = -1)
	{
		var queue = await Database.GetQueueAsync(ctx.Guild);
		var inter = ctx.Client.GetInteractivity();
		// NicoNicoNii
		if (n.ToLower().StartsWith("http://nicovideo.jp")
			|| n.ToLower().StartsWith("http://sp.nicovideo.jp")
			|| n.ToLower().StartsWith("https://nicovideo.jp")
			|| n.ToLower().StartsWith("https://sp.nicovideo.jp")
			|| n.ToLower().StartsWith("http://www.nicovideo.jp")
			|| n.ToLower().StartsWith("https://www.nicovideo.jp"))
		{
			var msg = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Processing NND Video...").AsEphemeral());
			var split = n.Split("/".ToCharArray());
			var nndID = split.First(x => x.StartsWith("sm") || x.StartsWith("nm")).Split("?")[0];
			FtpClient client = new(MikuBot.Config.NndConfig.FtpConfig.Hostname, new NetworkCredential(MikuBot.Config.NndConfig.FtpConfig.User, MikuBot.Config.NndConfig.FtpConfig.Password));
			client.Connect();
			if (!client.FileExists($"{nndID}.mp3"))
			{
				await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Preparing download..."));
				var ex = await ctx.GetNNDAsync(n, nndID, msg.Id);
				if (ex == null)
				{
					await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Please try again or verify the link"));
					return null;
				}
				await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Uploading"));
				client.UploadStream(ex, $"{nndID}.mp3", FtpRemoteExists.Skip, true);
			}
			var Track = await nodeConnection.Rest.GetTracksAsync(new Uri($"https://nnd.meek.moe/new/{nndID}.mp3"));
			if (pos == -1)
				await Database.AddToQueueAsync(ctx.Guild, ctx.Member.Id, Track.Tracks.First().TrackString);
			else
				await Database.InsertToQueueAsync(ctx.Guild, ctx.Member.Id, Track.Tracks.First().TrackString, pos);
			if (guildConnection.IsConnected && (playstate == Playstate.NotPlaying || playstate == Playstate.Stopped))
				await PlaySong();
			return new TrackResult(Track.PlaylistInfo, Track.Tracks.First());
		}
		// Bilibili
		else if (n.ToLower().StartsWith("https://www.bilibili.com")
			|| n.ToLower().StartsWith("http://www.bilibili.com"))
		{
			var msg = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Processing Bilibili Video...").AsEphemeral());
			n = n.Replace("https://www.bilibili.com/", "");
			n = n.Replace("http://www.bilibili.com/", "");
			var split = n.Split("/".ToCharArray());
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
			var Track = await nodeConnection.Rest.GetTracksAsync(new Uri($"https://nnd.meek.moe/new/{nndID}.mp3"));
			if (pos == -1)
				await Database.AddToQueueAsync(ctx.Guild, ctx.Member.Id, Track.Tracks.First().TrackString);
			else
				await Database.InsertToQueueAsync(ctx.Guild, ctx.Member.Id, Track.Tracks.First().TrackString, pos);
			if (guildConnection.IsConnected && (playstate == Playstate.NotPlaying || playstate == Playstate.Stopped))
				await PlaySong();
			return new TrackResult(Track.PlaylistInfo, Track.Tracks.First());
		}
		// Http(s) stream/file
		else if (n.StartsWith("http://") | n.StartsWith("https://"))
		{
			try
			{
				var s = await nodeConnection.Rest.GetTracksAsync(new Uri(n));
				switch (s.LoadResultType)
				{
					case LavalinkLoadResultType.LoadFailed:
						{
							await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().AddEmbed(new DiscordEmbedBuilder().WithTitle("Failed to load").WithDescription("Loading this song/playlist failed, please try again, reasons could be:\n" +
								"> Playlist is set to private or unlisted\n" +
								"> The song is unavailable/deleted").Build()));
							return null;
						};
					case LavalinkLoadResultType.NoMatches:
						{
							await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().AddEmbed(new DiscordEmbedBuilder().WithTitle("Failed to load").WithDescription("No song/playlist was found with this URL, please try again/a different one").Build()));
							return null;
						};
					case LavalinkLoadResultType.PlaylistLoaded:
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
									if (guildConnection.IsConnected && (playstate == Playstate.NotPlaying || playstate == Playstate.Stopped))
										await PlaySong();
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
									if (pos == -1)
										await Database.AddToQueueAsync(ctx.Guild, ctx.Member.Id, s.Tracks.ElementAt(s.PlaylistInfo.SelectedTrack).TrackString);
									else
										await Database.InsertToQueueAsync(ctx.Guild, ctx.Member.Id, s.Tracks.ElementAt(s.PlaylistInfo.SelectedTrack).TrackString, pos);
									if (guildConnection.IsConnected && (playstate == Playstate.NotPlaying || playstate == Playstate.Stopped))
										await PlaySong();
									return new TrackResult(s.PlaylistInfo, s.Tracks.ElementAt(s.PlaylistInfo.SelectedTrack));
								}
								else if (resp.Result.Id == "all")
								{
									await resp.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
									buttons.ForEach(x => x.Disable());
									await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(buttons).WithContent($"Adding entire playlist: {s.PlaylistInfo.Name}"));
									if (pos == -1)
										await Database.AddToQueueAsync(ctx.Guild, ctx.Member.Id, s.Tracks);
									else
									{
										s.Tracks.Reverse();
										await Database.InsertToQueueAsync(ctx.Guild, ctx.Member.Id, s.Tracks, pos);
									}
									if (guildConnection.IsConnected && (playstate == Playstate.NotPlaying || playstate == Playstate.Stopped))
										await PlaySong();
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
							if (pos == -1)
								await Database.AddToQueueAsync(ctx.Guild, ctx.Member.Id, s.Tracks.First().TrackString);
							else
								await Database.InsertToQueueAsync(ctx.Guild, ctx.Member.Id, s.Tracks.First().TrackString, pos);
							if (guildConnection.IsConnected && (playstate == Playstate.NotPlaying || playstate == Playstate.Stopped))
								await PlaySong();
							return new TrackResult(s.PlaylistInfo, s.Tracks.First());
						};
				}
			}
			catch (Exception ex)
			{
				ctx.Client.Logger.LogError("{ex}", ex.Message);
				ctx.Client.Logger.LogError("{ex}", ex.StackTrace);
				return null;
			}
		}
		// A search is triggered
		else
		{
			var type = LavalinkSearchType.Youtube;
			if (n.StartsWith("ytsearch:"))
			{
				n = n.Replace("ytsearch:", "");
				type = LavalinkSearchType.Youtube;
			}
			else if (n.StartsWith("scsearch:"))
			{
				n = n.Replace("ytsearch:", "");
				type = LavalinkSearchType.SoundCloud;
			}
			else if (n.StartsWith("spsearch:"))
			{
				n = n.Replace("spsearch:", "");
				type = LavalinkSearchType.Spotify;
			}
			else if (n.StartsWith("amsearch:"))
			{
				n = n.Replace("amsearch:", "");
				type = LavalinkSearchType.AppleMusic;
			}

			var s = await nodeConnection.Rest.GetTracksAsync(n, type);
			switch (s.LoadResultType)
			{
				case LavalinkLoadResultType.LoadFailed:
					{
						ctx.Client.Logger.LogDebug("Load failed");
						await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().AddEmbed(new DiscordEmbedBuilder().WithTitle("Failed to load").WithDescription("Loading this song/playlist failed, please try again, reason could be:\n" +
							"> The song is unavailable/deleted").Build()));
						return null;
					};
				case LavalinkLoadResultType.NoMatches:
					{
						ctx.Client.Logger.LogDebug("No matches");
						await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().AddEmbed(new DiscordEmbedBuilder().WithTitle("Failed to load").WithDescription("No song was found, please try again").Build()));
						return null;
					};
				default:
					{
						ctx.Client.Logger.LogDebug("Found something");
						int leng = s.Tracks.Count;
						if (leng > 5)
							leng = 5;
						List<DiscordStringSelectComponentOption> selectOptions = new(leng);
						var em = new DiscordEmbedBuilder()
							.WithTitle("Results!")
							.WithDescription("Please select a track:\n")
							.WithAuthor($"Requested by {ctx.Member.UsernameWithDiscriminator} || Timeout 30 seconds", iconUrl: ctx.Member.AvatarUrl);
						for (int i = 0; i < leng; i++)
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
						if (pos == -1)
							await Database.AddToQueueAsync(ctx.Guild, ctx.Member.Id, track.TrackString);
						else
							await Database.InsertToQueueAsync(ctx.Guild, ctx.Member.Id, track.TrackString, pos);
						if (guildConnection.IsConnected && (playstate == Playstate.NotPlaying || playstate == Playstate.Stopped))
							await PlaySong();
						return new TrackResult(s.PlaylistInfo, track);
					};
			}
		}
	}

	public async Task<QueueEntry> PlaySong()
	{
		var queue = await Database.GetQueueAsync(voiceChannel.Guild);
		var cur = lastSong;
		if (queue.Count != 1 && repeatMode == RepeatMode.All)
			repeatAllPos++;
		if (repeatAllPos >= queue.Count)
			repeatAllPos = 0;
		currentSong = shuffleMode == ShuffleMode.Off ? queue[0] : queue[new Random().Next(0, queue.Count)];
		if (repeatMode == RepeatMode.All)
			currentSong = queue[repeatAllPos];
		if (repeatMode == RepeatMode.On)
			currentSong = cur;
		MikuBot.ShardedClient.Logger.LogDebug(currentSong?.track.TrackString);
		guildConnection.PlaybackFinished += Lavalink.LavalinkTrackFinish;
		playstate = Playstate.Playing;
		await Task.Run(async () => await guildConnection.PlayAsync(currentSong.track), MikuBot._cts.Token);
		return currentSong;
	}
}

//     B/S(｀・ω・´) ❤️ (´ω｀)U/C
