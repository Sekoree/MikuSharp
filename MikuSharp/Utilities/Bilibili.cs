using AngleSharp.Html.Parser;
using DSharpPlus.Entities;
using MikuSharp.Entities;
using Newtonsoft.Json;
using NYoutubeDL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MikuSharp.Utilities
{
    public class Bilibili
    {
        public static async Task<MemoryStream> GetBilibili(string s, DiscordMessage msg)
        {
            try
            {
                await msg.ModifyAsync("Downloading video(this may take up to 5 min)");
                var youtubeDl = new YoutubeDL(@"youtube-dl");
                youtubeDl.Options.FilesystemOptions.Output = $@"{s}.mp4";
                youtubeDl.Options.PostProcessingOptions.ExtractAudio = true;
                youtubeDl.Options.PostProcessingOptions.FfmpegLocation = @"ffmpeg";
                youtubeDl.Options.PostProcessingOptions.AudioFormat = NYoutubeDL.Helpers.Enums.AudioFormat.mp3;
                youtubeDl.Options.PostProcessingOptions.AddMetadata = true;
                youtubeDl.Options.PostProcessingOptions.KeepVideo = false;
                youtubeDl.StandardOutputEvent += (e,f) =>
                {
                    Console.WriteLine(f);
                };
                youtubeDl.StandardErrorEvent += (e, f) =>
                {
                    Console.WriteLine(f);
                };
                youtubeDl.VideoUrl = "https://www.bilibili.com/video/" + s;
                await youtubeDl.DownloadAsync();
                var ms = new MemoryStream();
                if (File.Exists($@"{s}.mp3"))
                {
                    var song = File.Open($@"{s}.mp3", FileMode.Open);
                    await song.CopyToAsync(ms);
                    ms.Position = 0;
                    song.Close();
                    File.Delete($@"{s}.mp3");
                }
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
