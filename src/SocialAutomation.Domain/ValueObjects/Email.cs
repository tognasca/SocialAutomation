// src/SocialAutomation.Domain/ValueObjects/Email.cs

namespace SocialAutomation.Domain.ValueObjects;

public sealed class Email
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value.Trim().ToLowerInvariant();
    }

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty.", nameof(value));

        var trimmed = value.Trim().ToLowerInvariant();

        if (trimmed.Length > 255)
            throw new ArgumentException("Email cannot exceed 255 characters.", nameof(value));

        if (!trimmed.Contains('@') || !trimmed.Contains('.'))
            throw new ArgumentException("Email format is invalid.", nameof(value));

        return new Email(trimmed);
    }

    public override string ToString() => Value;
    public override bool Equals(object? obj) => obj is Email other && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public static implicit operator string(Email email) => email.Value;
}