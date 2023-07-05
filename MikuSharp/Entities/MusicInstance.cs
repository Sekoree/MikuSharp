using DisCatSharp.Lavalink.Entities;
using DisCatSharp.Lavalink.Enums;

using MikuSharp.Enums;
using MikuSharp.Events;
using MikuSharp.Utilities;

namespace MikuSharp.Entities;

public class MusicInstance
{
	public int ShardId { get; set; }

	public DiscordChannel CommandChannel { get; set; }

	public DiscordChannel VoiceChannel { get; set; }

	public PlayState PlayState { get; set; }

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
		this.ShardId = shardId;
		this.Session = session;
		this.CommandChannel = null;
		this.PlayState = PlayState.NotPlaying;
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
	public async Task<TrackResult> QueueSong(string urlOrName, InteractionContext ctx, int position = -1)
	{
		var queue = await Database.GetQueueAsync(ctx.Guild);
		var inter = ctx.Client.GetInteractivity();
		// NicoNicoNii
		if (urlOrName.IsNndUrl())
		{
			var msg = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Processing NND Video...").AsEphemeral());
			var split = urlOrName.Split("/".ToCharArray());
			var nicoNicoId = split.First(x => x.StartsWith("sm") || x.StartsWith("nm")).Split("?")[0];
			FtpClient client = new(MikuBot.Config.NndConfig.FtpConfig.Hostname, new NetworkCredential(MikuBot.Config.NndConfig.FtpConfig.User, MikuBot.Config.NndConfig.FtpConfig.Password));
			client.Connect();
			if (!client.FileExists($"{nicoNicoId}.mp3"))
			{
				await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Preparing download..."));
				var ex = await ctx.GetNndAsync(urlOrName, nicoNicoId, msg.Id);
				if (ex == null)
				{
					await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Please try again or verify the link"));
					return null;
				}
				await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Uploading"));
				client.UploadStream(ex, $"{nicoNicoId}.mp3", FtpRemoteExists.Skip, true);
			}

			var trackResult = await this.Session.LoadTracksAsync($"https://nnd.meek.moe/new/{nicoNicoId}.mp3");
			if (trackResult is { LoadType: LavalinkLoadResultType.Track, Result: LavalinkTrack track })
			{
				if (position == -1)
					await Database.AddToQueueAsync(ctx.Guild, ctx.Member.Id, track.Encoded);
				else
					await Database.InsertToQueueAsync(ctx.Guild, ctx.Member.Id, track.Encoded, position);
				if (this.GuildPlayer.IsConnected && this.PlayState is PlayState.NotPlaying or PlayState.Stopped)
					await this.PlaySong();
				return new TrackResult(track.Info.Title, track);
			}

			return null!;
		}
		// Bilibili
		else if (urlOrName.ToLower().StartsWith("https://www.bilibili.com")
			|| urlOrName.ToLower().StartsWith("http://www.bilibili.com"))
		{
			var msg = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Processing Bilibili Video...").AsEphemeral());
			urlOrName = urlOrName.Replace("https://www.bilibili.com/", "");
			urlOrName = urlOrName.Replace("http://www.bilibili.com/", "");
			var split = urlOrName.Split("/".ToCharArray());
			if (!split.Contains("video"))
			{
				await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Failure"));
				return null;
			}
			var nndId = split[1].Split("?")[0];
			FtpClient client = new(MikuBot.Config.NndConfig.FtpConfig.Hostname, new NetworkCredential(MikuBot.Config.NndConfig.FtpConfig.User, MikuBot.Config.NndConfig.FtpConfig.Password));
			client.Connect();
			if (!client.FileExists($"{nndId}.mp3"))
			{
				await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Preparing download..."));
				var ex = await ctx.GetBilibiliAsync(nndId, msg.Id);
				if (ex == null)
				{
					await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Please try again or verify the link"));
					return null;
				}
				await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().WithContent("Uploading..."));
				client.UploadStream(ex, $"{nndId}.mp3", FtpRemoteExists.Skip, true);
			}
			var trackResult = await this.Session.LoadTracksAsync($"https://nnd.meek.moe/new/{nndId}.mp3");
			if (trackResult is { LoadType: LavalinkLoadResultType.Track, Result: LavalinkTrack track })
			{
				if (position == -1)
					await Database.AddToQueueAsync(ctx.Guild, ctx.Member.Id, track.Encoded);
				else
					await Database.InsertToQueueAsync(ctx.Guild, ctx.Member.Id, track.Encoded,
						position);
				if (this.GuildPlayer.IsConnected && this.PlayState is PlayState.NotPlaying or PlayState.Stopped)
					await this.PlaySong();
				return new TrackResult(track.Info.Title, track);
			}

			return null!;
		}
		// Http(s) stream/file
		else if (urlOrName.StartsWith("http://") | urlOrName.StartsWith("https://"))
		{
			try
			{
				var s = await this.Session.LoadTracksAsync(urlOrName);
				switch (s.LoadType)
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
						var trackPlaylist = (LavalinkPlaylist)s.Result;
						// This is a playlist
						if (trackPlaylist.Info.SelectedTrack == -1)
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
								await Database.AddToQueueAsync(ctx.Guild, ctx.Member.Id, trackPlaylist.Tracks.ToList());
								if (this.GuildPlayer.IsConnected && this.PlayState is PlayState.NotPlaying or PlayState.Stopped)
									await this.PlaySong();
								return new TrackResult(trackPlaylist.Info.Name, trackPlaylist.Tracks);
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
								await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(buttons).WithContent($"Adding single song: {trackPlaylist.Tracks.ElementAt(trackPlaylist.Info.SelectedTrack).Info.Title}"));
								if (position == -1)
									await Database.AddToQueueAsync(ctx.Guild, ctx.Member.Id, trackPlaylist.Tracks.ElementAt(trackPlaylist.Info.SelectedTrack).Encoded);
								else
									await Database.InsertToQueueAsync(ctx.Guild, ctx.Member.Id, trackPlaylist.Tracks.ElementAt(trackPlaylist.Info.SelectedTrack).Encoded, position);
								if (this.GuildPlayer.IsConnected && this.PlayState is PlayState.NotPlaying or PlayState.Stopped)
									await this.PlaySong();
								return new TrackResult(trackPlaylist.Info.Name, trackPlaylist.Tracks.ElementAt(trackPlaylist.Info.SelectedTrack));
							}
							else if (resp.Result.Id == "all")
							{
								await resp.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
								buttons.ForEach(x => x.Disable());
								await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(buttons).WithContent($"Adding entire playlist: {trackPlaylist.Info.Name}"));
								if (position == -1)
									await Database.AddToQueueAsync(ctx.Guild, ctx.Member.Id, trackPlaylist.Tracks);
								else
								{
									trackPlaylist.Tracks.Reverse();
									await Database.InsertToQueueAsync(ctx.Guild, ctx.Member.Id, trackPlaylist.Tracks, position);
								}
								if (this.GuildPlayer.IsConnected && this.PlayState is PlayState.NotPlaying or PlayState.Stopped)
									await this.PlaySong();
								return new TrackResult(trackPlaylist.Info.Name, trackPlaylist.Tracks);
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
						var track = (LavalinkTrack)s.Result;
						await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral().WithContent($"Playing single song: {track.Info.Title}"));
						if (position == -1)
							await Database.AddToQueueAsync(ctx.Guild, ctx.Member.Id, track.Encoded);
						else
							await Database.InsertToQueueAsync(ctx.Guild, ctx.Member.Id, track.Encoded, position);
						if (this.GuildPlayer.IsConnected && this.PlayState is PlayState.NotPlaying or PlayState.Stopped)
							await this.PlaySong();
						return new TrackResult(track.Info.Title, track);
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
			if (urlOrName.StartsWith("ytsearch:"))
			{
				urlOrName = urlOrName.Replace("ytsearch:", "");
				type = LavalinkSearchType.Youtube;
			}
			else if (urlOrName.StartsWith("scsearch:"))
			{
				urlOrName = urlOrName.Replace("ytsearch:", "");
				type = LavalinkSearchType.SoundCloud;
			}
			else if (urlOrName.StartsWith("spsearch:"))
			{
				urlOrName = urlOrName.Replace("spsearch:", "");
				type = LavalinkSearchType.Spotify;
			}
			else if (urlOrName.StartsWith("amsearch:"))
			{
				urlOrName = urlOrName.Replace("amsearch:", "");
				type = LavalinkSearchType.AppleMusic;
			}

			var s = await this.GuildPlayer.LoadTracksAsync(type, urlOrName);
			switch (s.LoadType)
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
					var searchResult = (List<LavalinkTrack>)s.Result;
					ctx.Client.Logger.LogDebug("Found something");
					var leng = searchResult.Count;
					if (leng > 5)
						leng = 5;
					List<DiscordStringSelectComponentOption> selectOptions = new(leng);
					var em = new DiscordEmbedBuilder()
							.WithTitle("Results!")
							.WithDescription("Please select a track:\name_or_url")
							.WithAuthor($"Requested by {ctx.Member.UsernameWithDiscriminator} || Timeout 30 seconds", iconUrl: ctx.Member.AvatarUrl);
					for (var i = 0; i < leng; i++)
					{
						em.AddField(new DiscordEmbedField($"{i + 1}.{searchResult.ElementAt(i).Info.Title} [{searchResult.ElementAt(i).Info.Length}]", $"by {searchResult.ElementAt(i).Info.Author} [Link]({searchResult.ElementAt(i).Info.Uri})"));
						selectOptions.Add(new DiscordStringSelectComponentOption(searchResult.ElementAt(i).Info.Title, i.ToString(), $"by {searchResult.ElementAt(i).Info.Author}. Length: {searchResult.ElementAt(i).Info.Length}"));
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
					var track = searchResult.ElementAt(trackSelect);
					select.Disable();
					await ctx.EditFollowupAsync(msg.Id, new DiscordWebhookBuilder().AddComponents(select).WithContent($"Chose {track.Info.Title}"));
					if (position == -1)
						await Database.AddToQueueAsync(ctx.Guild, ctx.Member.Id, track.Encoded);
					else
						await Database.InsertToQueueAsync(ctx.Guild, ctx.Member.Id, track.Encoded, position);
					if (this.GuildPlayer.IsConnected && this.PlayState is PlayState.NotPlaying or PlayState.Stopped)
						await this.PlaySong();
					return new TrackResult(track.Info.Title, track);
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
		this.PlayState = PlayState.Playing;
		await Task.Run(async () => await this.GuildPlayer.PlayAsync(this.CurrentSong.Track), MikuBot.CanellationTokenSource.Token);
		return this.CurrentSong;
	}
}

//     B/S(｀・ω・´) ❤️ (´ω｀)U/C
