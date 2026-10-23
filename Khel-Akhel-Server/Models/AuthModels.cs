namespace Khel_Akhel_Server.Models
{
    public class PortalLoginRequest
    {
        public string UserName { get; set; } = string.Empty; // Email or MobileNo
        public string Password { get; set; } = string.Empty;
        public string? DeviceInfo { get; set; }
        public string? CaptchaId { get; set; }
        public string? Captcha { get; set; }
    }

    public class CaptchaResponseData
    {
        public string CaptchaId { get; set; } = string.Empty;
        public string CaptchaImage { get; set; } = string.Empty;
    }

    // Alias for backward compatibility
    public class LoginPasswordRequest : PortalLoginRequest { }

    public class SendOtpRequest
    {
        public string Value { get; set; } = string.Empty;
        public string? DeviceInfo { get; set; }
    }

    public class VerifyOtpRequest
    {
        public string Value { get; set; } = string.Empty;
        public string OTP { get; set; } = string.Empty;
        public string OTPType { get; set; } = string.Empty;
        public string? DeviceInfo { get; set; }
    }

    public class ResendOtpRequest
    {
        public string Value { get; set; } = string.Empty;
        public string OTPType { get; set; } = string.Empty;
    }

    public class ForgetPasswordRequest
    {
        public string UserName { get; set; } = string.Empty; // Email or MobileNo
        public string? CaptchaId { get; set; }
        public string? Captcha { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class PasswordResetRequestAdminDto
    {
        public string RequestId { get; set; } = string.Empty; // Encrypted
        public string CustomerId { get; set; } = string.Empty; // Encrypted
        public string AccountNo { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string MobileNo { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string RequestedDate { get; set; } = string.Empty;
        public string? RequestedIp { get; set; }
        public string? IssuedByAdminName { get; set; }
        public string? IssuedDate { get; set; }
        public string? TokenExpiresDate { get; set; }
        public string? AdminNotes { get; set; }
    }

    public class IssueResetLinkRequestDto
    {
        public string AdminNotes { get; set; } = string.Empty;
    }

    public class RejectResetRequestDto
    {
        public string AdminNotes { get; set; } = string.Empty;
    }

    public class IssueResetLinkResponseDto
    {
        public string ResetUrl { get; set; } = string.Empty;
        public string ExpiresUtc { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class AuthResponseData
    {
        public string Token { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
    }
}
