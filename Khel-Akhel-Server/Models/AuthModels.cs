namespace Khel_Akhel_Server.Models
{
    public class PortalLoginRequest
    {
        public string UserName { get; set; } = string.Empty; // Email or MobileNo
        public string Password { get; set; } = string.Empty;
        public string? DeviceInfo { get; set; }
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
