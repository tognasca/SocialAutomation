
using System.Security.Cryptography;
using System.Text;

namespace SocialAutomation.Infrastructure.Instagram;

public static class InstagramWebhookValidator
{
    public static bool ValidateSignature(string payload, string signatureHeader, string appSecret)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader) || !signatureHeader.StartsWith("sha256="))
            return false;

        var expectedSignature = signatureHeader["sha256=".Length..];
        var keyBytes = Encoding.UTF8.GetBytes(appSecret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(payloadBytes);
        var actualSignature = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(actualSignature),
            Encoding.UTF8.GetBytes(expectedSignature));
    }
}