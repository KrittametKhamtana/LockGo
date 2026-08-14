using System.Security.Cryptography;

namespace LockGo.Application.Common;

public static class BookingNumberGenerator
{
    // Excludes 0/O and 1/I to avoid ambiguity when read aloud at a locker.
    private const string Alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";

    public static string Generate()
    {
        Span<char> suffix = stackalloc char[6];
        for (var i = 0; i < suffix.Length; i++)
        {
            suffix[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return $"LG-{DateTimeOffset.UtcNow:yyyyMMdd}-{new string(suffix)}";
    }
}
