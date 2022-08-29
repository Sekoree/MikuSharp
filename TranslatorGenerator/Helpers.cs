namespace DisCatSharp.TranslationGenerator;

internal static class Helpers
{
	internal static string RemoveLtfBs(this string str)
		=> str.Replace("translation_generator_export-", "").Replace('-', ' ').Replace('_', ' ').Replace(".json", "");
}
