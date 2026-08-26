using System.Globalization;
using System.Text;

namespace CathedrAll.Pessoas.Domain;

internal static class TextNormalization
{
    internal static string Normalize(string text)
    {
        string decomposed = text.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);

        StringBuilder builder = new(decomposed.Length);

        foreach (char character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
