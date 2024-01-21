using System.Collections.Generic;

namespace MikuSharp.Entities;

public class BiliPlayinfo
{
	public int Code { get; set; }
	public string Message { get; set; }
	public int Ttl { get; set; }
	public Data Data { get; set; }
	public string Session { get; set; }
	public VideoFrame VideoFrame { get; set; }
}

public class Durl
{
	public int Order { get; set; }
	public int Length { get; set; }
	public int Size { get; set; }
	public string Ahead { get; set; }
	public string Vhead { get; set; }
	public string Url { get; set; }
	public object BackupUrl { get; set; }
}

public class Data
{
	public string From { get; set; }
	public string Result { get; set; }
	public string Message { get; set; }
	public int Quality { get; set; }
	public string Format { get; set; }
	public int Timelength { get; set; }
	public string AcceptFormat { get; set; }
	public List<string> AcceptDescription { get; set; }
	public List<int> AcceptQuality { get; set; }
	public int VideoCodecid { get; set; }
	public string SeekParam { get; set; }
	public string SeekType { get; set; }
	public List<Durl> Durl { get; set; }
}

public class VideoFrame
{ }