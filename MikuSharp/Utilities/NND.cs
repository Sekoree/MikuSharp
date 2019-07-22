using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using DSharpPlus.Entities;
using HeyRed.Mime;
using MikuSharp.Entities;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace MikuSharp.Utilities
{
    public class NND
    {
        public static async Task<MemoryStream> GetNND(string s, DiscordMessage msg)
        {
            try
            {
                var cookies = new CookieContainer();
                var handler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    CookieContainer = cookies,
                    UseCookies = true,
                    UseDefaultCredentials = false
                };
                var client = new HttpClient(handler);
                string loginForm = $"mail={Bot.cfg.NndConfig.Mail}&password={Bot.cfg.NndConfig.Password}&site=nicometro";
                var body = new StringContent(loginForm, Encoding.UTF8, "application/x-www-form-urlencoded");
                string login = "https://secure.nicovideo.jp/secure/login?site=niconico";
                body.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                await msg.ModifyAsync("Logging in");
                var doLogin = await client.PostAsync(new Uri(login), body);
                await msg.ModifyAsync("Getting video page");
                var videoPage = await client.GetStringAsync(new Uri($"https://www.nicovideo.jp/watch/{s}"));
                var parser = new HtmlParser();
                var parsedDoc = await parser.ParseDocumentAsync(videoPage);
                IElement flashPart = null;
                var h5Part = parsedDoc.GetElementById("js-initial-watch-data");
                string jsonData = "";
                string songTitle = "";
                string songArtist = "";
                if (h5Part != null)
                    jsonData = h5Part.GetAttribute("data-api-data");
                else
                {
                    flashPart = parsedDoc.GetElementById("watchAPIDataContainer");
                    jsonData = flashPart.TextContent;
                }
                MemoryStream videoData = new MemoryStream();
                await msg.ModifyAsync("Downloading video (this may take up to 5 min)");
                if (flashPart == null)
                {
                    var dataObject = JsonConvert.DeserializeObject<NND_Watch>(jsonData);
                    videoData = new MemoryStream(await client.GetByteArrayAsync(dataObject.video.smileInfo.url));
                    songTitle = dataObject.video.originalTitle;
                    songArtist = dataObject.owner?.nickname == null ? "n/a" : dataObject.owner.nickname;
                }
                else
                {
                    var dataObject = JsonConvert.DeserializeObject<NND_Watch_Flash>(jsonData);
                    var directVideoUri = dataObject.flashvars.flvInfo.Replace("%253A%252F%252F", "://").Replace("%252F", "/").Replace("%253F", "?").Replace("%253D", "=").Split("%3D").First(x => x.StartsWith("http")).Split("%26")[0];
                    Console.WriteLine(directVideoUri);
                    songTitle = dataObject.videoDetail.title_original;
                    songArtist = dataObject.uploaderInfo?.nickname == null ? "n/a" : dataObject.uploaderInfo.nickname;
                    videoData = new MemoryStream(await client.GetByteArrayAsync(directVideoUri));
                }
                videoData.Position = 0;
                var videoFile = File.Create($@"{s}");
                videoData.CopyTo(videoFile);
                videoData.Position = 0;
                videoFile.Close();
                await msg.ModifyAsync("Converting");
                Process process = new Process();
                process.StartInfo.FileName = "ffmpeg";
                process.StartInfo.Arguments = $"-i {$@"{s}"} -metadata title=\"{songTitle}\" -metadata artist=\"{songArtist}\" {$@"{s}"}.mp3";
                process.OutputDataReceived += (d, f) =>
                {
                    Console.WriteLine(f.Data);
                };
                process.Start();
                process.WaitForExit();
                File.Delete($@"{s}");
                MemoryStream ms = new MemoryStream(await File.ReadAllBytesAsync($@"{s}.mp3"));
                File.Delete($@"{s}.mp3");
                ms.Position = 0;
                return ms;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }
    }
}
