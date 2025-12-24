namespace UrlShortener.Services.Write.Application.Common.Utils;

public static class Base62Converter
{
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const int Base = 62; // Length of Alphabet

    public static string Encode(long number)
    {
        if (number == 0)
        {
            return "0"; // Single character case
        }

        // Calculate the maximum length needed (log_base(number) + 1)
        var logValue = Math.Log(number + 1, Base);
        int maxLength = (int)Math.Ceiling(logValue);
        Span<char> buffer = stackalloc char[maxLength];

        int index = maxLength - 1;
        while (number > 0)
        {
            buffer[index] = Alphabet[(int)(number % Base)];
            index--;
            number /= Base;
        }

        // Convert Span<char> to string from the used portion
        return new string(buffer[(index + 1)..]);
    }
}
