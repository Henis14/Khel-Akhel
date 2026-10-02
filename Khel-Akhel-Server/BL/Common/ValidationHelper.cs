using System.Text.RegularExpressions;

namespace Khel_Akhel_Server.BL.Common
{
    public static class ValidationHelper
    {
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);

        // ReDoS-Safe Regex Patterns matching prompt specifications
        private static readonly Regex NameRegex = new(
            @"^[A-Za-z]{2,50}$",
            RegexOptions.Compiled,
            RegexTimeout
        );

        private static readonly Regex EmailRegex = new(
            @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled,
            RegexTimeout
        );

        private static readonly Regex MobileRegex = new(
            @"^[6-9][0-9]{9}$",
            RegexOptions.Compiled,
            RegexTimeout
        );

        private static readonly Regex PasswordRegex = new(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
            RegexOptions.Compiled,
            RegexTimeout
        );

        private static readonly Regex SkuRegex = new(
            @"^[A-Za-z0-9_-]{3,50}$",
            RegexOptions.Compiled,
            RegexTimeout
        );

        private static readonly Regex PincodeRegex = new(
            @"^[1-9][0-9]{5}$",
            RegexOptions.Compiled,
            RegexTimeout
        );

        private static readonly Regex AlphaSpaceRegex = new(
            @"^[A-Za-z ]{2,100}$",
            RegexOptions.Compiled,
            RegexTimeout
        );

        private static readonly Regex AccountNoRegex = new(
            @"^DRC[0-9]{6}$",
            RegexOptions.Compiled,
            RegexTimeout
        );

        private static readonly Regex ProductCodeRegex = new(
            @"^DRP[0-9]{6}$",
            RegexOptions.Compiled,
            RegexTimeout
        );

        private static readonly Regex CityRegex = new(
            @"^[A-Za-z][A-Za-z .'-]{1,99}$",
            RegexOptions.Compiled,
            RegexTimeout
        );

        public static bool IsValidName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;
            try
            {
                return NameRegex.IsMatch(name.Trim());
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email) || email.Trim().Length > 255)
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

        public static bool IsValidPassword(string? password)
        {
            // Do NOT trim passwords - validate as provided!
            if (string.IsNullOrEmpty(password))
                return false;
            try
            {
                return PasswordRegex.IsMatch(password);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static bool IsValidSku(string? sku)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return false;
            try
            {
                return SkuRegex.IsMatch(sku.Trim());
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static bool IsValidProductName(string? productName)
        {
            if (string.IsNullOrWhiteSpace(productName))
                return false;
            string trimmed = productName.Trim();
            return trimmed.Length >= 2 && trimmed.Length <= 200;
        }

        public static bool IsValidCategoryName(string? categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                return false;
            string trimmed = categoryName.Trim();
            return trimmed.Length >= 2 && trimmed.Length <= 100;
        }

        public static bool IsValidCategoryDescription(string? desc)
        {
            if (string.IsNullOrEmpty(desc))
                return true;
            return desc.Trim().Length <= 500;
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

        public static bool IsValidAlphaSpaceString(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;
            try
            {
                return AlphaSpaceRegex.IsMatch(text.Trim());
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static bool IsValidAddressFullName(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return false;
            string trimmed = fullName.Trim();
            return trimmed.Length >= 2 && trimmed.Length <= 200;
        }

        public static bool IsValidAddressLine1(string? line1)
        {
            if (string.IsNullOrWhiteSpace(line1))
                return false;
            return line1.Trim().Length <= 255;
        }

        public static bool IsValidAddressLine2(string? line2)
        {
            if (string.IsNullOrEmpty(line2))
                return true;
            return line2.Trim().Length <= 255;
        }

        public static bool IsValidLandmark(string? landmark)
        {
            if (string.IsNullOrEmpty(landmark))
                return true;
            return landmark.Trim().Length <= 150;
        }

        public static bool IsValidAccountNo(string? accountNo)
        {
            if (string.IsNullOrWhiteSpace(accountNo))
                return false;
            try
            {
                return AccountNoRegex.IsMatch(accountNo.Trim());
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static bool IsValidProductCode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;
            try
            {
                return ProductCodeRegex.IsMatch(code.Trim());
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static bool IsValidCity(string? city)
        {
            if (string.IsNullOrWhiteSpace(city))
                return false;
            try
            {
                return CityRegex.IsMatch(city.Trim());
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }
    }
}
