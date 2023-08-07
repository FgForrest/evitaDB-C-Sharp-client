using System.Globalization;

namespace Client.Utils;

public static class ComparatorUtils
{
    public static int CompareLocale(CultureInfo? locale, CultureInfo? otherLocale, Func<int> tieBreakingResult)
    {
        int localeResult;
        if (locale == null && otherLocale == null)
        {
            localeResult = 0;
        }
        else if (locale != null && otherLocale == null)
        {
            localeResult = -1;
        }
        else if (locale == null)
        {
            localeResult = 1;
        }
        else
        {
            localeResult = locale.CompareInfo.Compare(locale.IetfLanguageTag, otherLocale?.IetfLanguageTag);
        }
        return localeResult == 0 ? tieBreakingResult.Invoke() : localeResult;
    }
}