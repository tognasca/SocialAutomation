namespace SocialAutomation.Domain.Models.Instagram;

public static class PublicationMedia
{
    public static bool IsValid(string? type, IReadOnlyCollection<string>? urls)
    {
        if (urls is null || type is not (null or "Post" or "Story" or "Reel" or "Carousel"))
            return false;

        var validCount = type == "Carousel" ? urls.Count is >= 2 and <= 10 : urls.Count == 1;
        return validCount && urls.All(value =>
            Uri.TryCreate(value, UriKind.Absolute, out var url) &&
            url.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(url.UserInfo));
    }
}
