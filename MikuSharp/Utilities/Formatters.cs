namespace MikuSharp.Utilities;

/// <summary>
///     Represents a collection of formatters.
/// </summary>
internal static class Formatters
{
	/// <summary>
	///     Resizes an image link.
	/// </summary>
	/// <param name="url">The url of the image to resize.</param>
	/// <returns>The resized image.</returns>
	public static string ResizeLink(this string url)
		=> $"https://api.meek.moe/im/?image={url}&resize=500";

	/// <summary>
	///     Formats a <see cref="TimeSpan" /> into a human-readable string.
	/// </summary>
	/// <param name="timeSpan">The time span to format.</param>
	/// <returns>The formatted time span.</returns>
	public static string FormatTimeSpan(this TimeSpan timeSpan)
		=> timeSpan.TotalHours >= 1
			? $"{(int)timeSpan.TotalHours:D2}h:{timeSpan.Minutes:D2}m:{timeSpan.Seconds:D2}s"
			: timeSpan.TotalMinutes >= 1
				? $"{(int)timeSpan.TotalMinutes:D2}m:{timeSpan.Seconds:D2}s"
				: $"{(int)timeSpan.TotalSeconds:D2}s";
}
