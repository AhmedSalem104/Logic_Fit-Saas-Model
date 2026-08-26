using System.Security.Cryptography;
using LogicFit.Application;
using LogicFit.Shared;
using Microsoft.Extensions.Options;

namespace LogicFit.Infrastructure.Security;

/// <summary>
/// Protects TOTP material before it is persisted. The key is supplied by the
/// deployment environment and is never generated from application data.
/// </summary>
public sealed class AesGcmSecretProtector(IOptions<LogicFitRuntimeOptions> options) : ISecretProtector
{
    private const byte Version = 1;
    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private byte[] Key => ReadKey(options.Value.MfaProtectionKeyBase64);

    public string Protect(string plaintext)
    {
        ArgumentException.ThrowIfNullOrEmpty(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plaintextBytes = System.Text.Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(Key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var payload = new byte[1 + nonce.Length + tag.Length + ciphertext.Length];
        payload[0] = Version;
        nonce.CopyTo(payload, 1);
        tag.CopyTo(payload, 1 + nonce.Length);
        ciphertext.CopyTo(payload, 1 + nonce.Length + tag.Length);
        CryptographicOperations.ZeroMemory(plaintextBytes);
        return Convert.ToBase64String(payload);
    }

    public string Unprotect(string protectedValue)
    {
        if (string.IsNullOrWhiteSpace(protectedValue))
        {
            throw new CryptographicException("Protected secret is empty.");
        }

        byte[] payload;
        try
        {
            payload = Convert.FromBase64String(protectedValue);
        }
        catch (FormatException exception)
        {
            throw new CryptographicException("Protected secret is invalid.", exception);
        }

        if (payload.Length < 1 + NonceSize + TagSize || payload[0] != Version)
        {
            throw new CryptographicException("Protected secret version is invalid.");
        }

        var nonce = payload.AsSpan(1, NonceSize).ToArray();
        var tag = payload.AsSpan(1 + NonceSize, TagSize).ToArray();
        var ciphertext = payload.AsSpan(1 + NonceSize + TagSize).ToArray();
        var plaintext = new byte[ciphertext.Length];

        try
        {
            using var aes = new AesGcm(Key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
            return System.Text.Encoding.UTF8.GetString(plaintext);
        }
        catch (CryptographicException)
        {
            throw new CryptographicException("Protected secret could not be decrypted.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
            CryptographicOperations.ZeroMemory(ciphertext);
            CryptographicOperations.ZeroMemory(tag);
            CryptographicOperations.ZeroMemory(nonce);
        }
    }

    private static byte[] ReadKey(string? encodedKey)
    {
        if (string.IsNullOrWhiteSpace(encodedKey))
        {
            throw new InvalidOperationException("LogicFit:Runtime:MfaProtectionKeyBase64 must be configured before MFA secrets can be stored.");
        }

        try
        {
            var key = Convert.FromBase64String(encodedKey);
            if (key.Length != KeySize)
            {
                throw new InvalidOperationException("MFA protection key must be exactly 32 bytes.");
            }

            return key;
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("MFA protection key must be valid Base64.", exception);
        }
    }
}
