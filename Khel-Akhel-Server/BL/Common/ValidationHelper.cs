using System.Text.RegularExpressions;

namespace Khel_Akhel_Server.BL.Common
{
    public static class ValidationHelper
    {
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);

        // ReDoS-Safe Regex Patterns
        private static readonly Regex EmailRegex = new(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled,
            RegexTimeout
        );
        private static readonly Regex MobileRegex = new(
            @"^[6-9]\d{9}$",
            RegexOptions.Compiled,
            RegexTimeout
        );
        private static readonly Regex PincodeRegex = new(
            @"^[1-9][0-9]{5}$",
            RegexOptions.Compiled,
            RegexTimeout
        );
        private static readonly Regex CodeRegex = new(
            @"^[A-Za-z0-9_\-]{3,50}$",
            RegexOptions.Compiled,
            RegexTimeout
        );
        private static readonly Regex CouponCodeRegex = new(
            @"^[A-Za-z0-9_\-]{3,30}$",
            RegexOptions.Compiled,
            RegexTimeout
        );
        private static readonly Regex UrlRegex = new(
            @"^(https?:\/\/)?([\w\d\-]+\.)+[\w\d\-]+(\/[^\s]*)?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled,
            RegexTimeout
        );
        private static readonly Regex GstRegex = new(
            @"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled,
            RegexTimeout
        );

        public static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;
            try
            {
                return EmailRegex.IsMatch(email.Trim());
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static bool IsValidMobile(string? mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile))
                return false;
            try
            {
                return MobileRegex.IsMatch(mobile.Trim());
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static bool IsValidPincode(string? pincode)
        {
            if (string.IsNullOrWhiteSpace(pincode))
                return false;
            try
            {
                return PincodeRegex.IsMatch(pincode.Trim());
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static bool IsValidCode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;
            try
            {
                return CodeRegex.IsMatch(code.Trim());
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static bool IsValidCouponCode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;
            try
            {
                return CouponCodeRegex.IsMatch(code.Trim());
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static bool IsValidUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return true; // Optional URLs pass
            try
            {
                return UrlRegex.IsMatch(url.Trim());
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static bool IsValidGstNumber(string? gstNumber)
        {
            if (string.IsNullOrWhiteSpace(gstNumber))
                return true; // Optional field
            try
            {
                return GstRegex.IsMatch(gstNumber.Trim());
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }
    }
}
