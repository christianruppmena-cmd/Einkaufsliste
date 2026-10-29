using System.Globalization;

namespace Einkaufsliste;

public static class TextHelfer
{
    public static string Normalisieren(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        TextInfo textInfo = CultureInfo.GetCultureInfo("de-DE").TextInfo;

        text = text.Trim().ToLower(new CultureInfo("de-DE"));

        return textInfo.ToTitleCase(text);
    }
}