using SharedKernel;

namespace Domain.PasswordResetTokens;

public sealed class PasswordResetToken : Entity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Email { get; set; } = default!;

    public string Token { get; set; } = default!;

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
