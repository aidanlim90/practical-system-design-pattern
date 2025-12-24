namespace UrlShortener.AWS.Api;

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
        int maxLength = (int)Math.Ceiling(Math.Log(number + 1, Base));
        Span<char> buffer = stackalloc char[maxLength];

        int index = maxLength - 1;
        while (number > 0)
        {
            buffer[index--] = Alphabet[(int)(number % Base)];
            number /= Base;
        }

        // Convert Span<char> to string from the used portion
        return new string(buffer.Slice(index + 1));
    }
}
