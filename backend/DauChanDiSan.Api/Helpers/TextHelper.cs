namespace DauChanDiSan.Api.Helpers;

public static class TextHelper
{
    public static string Limit(string value, int max)
    {
        if (value.Length <= max) return value;
        return value[..(char.IsHighSurrogate(value[max - 1]) ? max - 1 : max)];
    }
}
