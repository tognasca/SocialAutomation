using System.Text.Json.Serialization;

namespace SocialAutomation.Domain.Models.Instagram;

public class MetaOAuthTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; set; }
}

public class MetaAccountsResponse
{
    [JsonPropertyName("data")]
    public List<MetaPageData> Data { get; set; } = new();
}

public class MetaPageData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("instagram_business_account")]
    public MetaInstagramBusinessAccount? InstagramBusinessAccount { get; set; }
}

public class MetaInstagramBusinessAccount
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("profile_picture_url")]
    public string? ProfilePictureUrl { get; set; }
}

public class MetaSendMessageResponse
{
    [JsonPropertyName("recipient_id")]
    public string RecipientId { get; set; } = string.Empty;

    [JsonPropertyName("message_id")]
    public string MessageId { get; set; } = string.Empty;
}

public class MetaUserProfileResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("profile_pic")]
    public string? ProfilePic { get; set; }
}