using System.Security.Cryptography;
using System.Text;

namespace SocialAutomation.Infrastructure.Instagram;

public static class InstagramWebhookValidator
{
    public static bool ValidateSignature(
        string payload,
        string signatureHeader,
        string appSecret)
    {
        if (string.IsNullOrWhiteSpace(payload) ||
            string.IsNullOrWhiteSpace(signatureHeader) ||
            string.IsNullOrWhiteSpace(appSecret))
            return false;

        const string prefix = "sha256=";

        signatureHeader = signatureHeader.Trim();

        if (!signatureHeader.StartsWith(
                prefix,
                StringComparison.OrdinalIgnoreCase))
            return false;

        var signatureHex = signatureHeader[prefix.Length..];

        if (signatureHex.Length != 64)
            return false;

        byte[] receivedHash;

        try
        {
            receivedHash = Convert.FromHexString(signatureHex);
        }
        catch (FormatException)
        {
            return false;
        }

        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var secretBytes = Encoding.UTF8.GetBytes(appSecret);

        var calculatedHash =
            HMACSHA256.HashData(secretBytes, payloadBytes);

        return CryptographicOperations.FixedTimeEquals(
            calculatedHash,
            receivedHash);
    }
}