namespace Castle.MonoRail.Extensions
{
	internal static class StringExtensions
	{
		public static string RemoveSufix(this string name, string sufix)
		{
			return name.Substring(0, name.Length - sufix.Length).ToLowerInvariant();
		}
	}
}
