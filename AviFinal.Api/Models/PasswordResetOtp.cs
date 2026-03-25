using System;

namespace AviFinal.Api.Models;

public partial class PasswordResetOtp
{
    public int Id { get; set; }

    public string UserName { get; set; } = null!;

    public string OtpCode { get; set; } = null!;

    public string Email { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; }

    public bool IsVerified { get; set; }
}