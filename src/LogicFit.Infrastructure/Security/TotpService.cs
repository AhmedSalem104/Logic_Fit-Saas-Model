using System.Security.Cryptography;
using System.Text;
using LogicFit.Application;
using LogicFit.Shared;
using Microsoft.Extensions.Options;

namespace LogicFit.Infrastructure.Security;

public sealed class TotpService(IOptions<LogicFitRuntimeOptions> runtimeOptions) : ITotpService
{
    private const int SecretBytes = 20;
    private const int StepSeconds = 30;
    private const int Digits = 6;

    public TotpProvisioning CreateProvisioning(string accountName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountName);
        var secret = Base32.Encode(RandomNumberGenerator.GetBytes(SecretBytes));
        var issuer = Uri.EscapeDataString(runtimeOptions.Value.MfaIssuer);
        var account = Uri.EscapeDataString(accountName);
        var uri = $"otpauth://totp/{issuer}:{account}?secret={secret}&issuer={issuer}&algorithm=SHA1&digits={Digits}&period={StepSeconds}";
        return new TotpProvisioning(secret, uri);
    }

    public bool Verify(string secret, string code, DateTimeOffset? timestamp = null)
    {
        if (string.IsNullOrWhiteSpace(secret) || code is null || code.Length != Digits || !code.All(char.IsDigit))
        {
            return false;
        }

        byte[] secretBytes;
        try
        {
            secretBytes = Base32.Decode(secret);
        }
        catch (FormatException)
        {
            return false;
        }

        var currentStep = (timestamp ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds() / StepSeconds;
        for (var offset = -1; offset <= 1; offset++)
        {
            var expected = GenerateCode(secretBytes, currentStep + offset);
            if (CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(code)))
            {
                return true;
            }
        }

        return false;
    }

    private static string GenerateCode(byte[] secret, long counter)
    {
        Span<byte> counterBytes = stackalloc byte[8];
        for (var i = 7; i >= 0; i--)
        {
            counterBytes[i] = (byte)(counter & 0xff);
            counter >>= 8;
        }

        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(counterBytes.ToArray());
        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24)
            | ((hash[offset + 1] & 0xff) << 16)
            | ((hash[offset + 2] & 0xff) << 8)
            | (hash[offset + 3] & 0xff);
        return (binary % 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static class Base32
    {
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        public static string Encode(ReadOnlySpan<byte> data)
        {
            var output = new StringBuilder((data.Length * 8 + 4) / 5);
            var buffer = 0;
            var bits = 0;
            foreach (var value in data)
            {
                buffer = (buffer << 8) | value;
                bits += 8;
                while (bits >= 5)
                {
                    output.Append(Alphabet[(buffer >> (bits - 5)) & 31]);
                    bits -= 5;
                }
            }

            if (bits > 0)
            {
                output.Append(Alphabet[(buffer << (5 - bits)) & 31]);
            }

            return output.ToString();
        }

        public static byte[] Decode(string value)
        {
            var normalized = value.Trim().TrimEnd('=').ToUpperInvariant();
            var output = new List<byte>();
            var buffer = 0;
            var bits = 0;
            foreach (var character in normalized)
            {
                var index = Alphabet.IndexOf(character);
                if (index < 0)
                {
                    throw new FormatException("Invalid Base32 value.");
                }

                buffer = (buffer << 5) | index;
                bits += 5;
                if (bits >= 8)
                {
                    output.Add((byte)((buffer >> (bits - 8)) & 0xff));
                    bits -= 8;
                }
            }

            return output.ToArray();
        }
    }
}
