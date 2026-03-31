namespace Epecps.Domain.Entities;

public class RefreshToken
{
    public int RefreshTokenId { get; set; }
    public int UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? ReasonRevoked { get; set; }

    public User User { get; set; } = null!;
}
