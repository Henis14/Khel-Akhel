using System.ComponentModel.DataAnnotations;

namespace Khel_Akhel_Server.Models
{
    #region Contact Inquiry Requests
    public class ContactInquiryCreateRequest
    {
        private string _fullName = string.Empty;
        private string _mobileNumber = string.Empty;
        private string _email = string.Empty;
        private string? _companyName;
        private string _serviceDescription = string.Empty;

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 150 characters.")]
        public string FullName
        {
            get => _fullName;
            set => _fullName = value?.Trim() ?? string.Empty;
        }

        [Required(ErrorMessage = "Mobile number is required.")]
        [StringLength(20, MinimumLength = 7, ErrorMessage = "Mobile number must be between 7 and 20 characters.")]
        public string MobileNumber
        {
            get => _mobileNumber;
            set => _mobileNumber = value?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Optional alias for MobileNumber to accommodate frontend variations.
        /// </summary>
        public string? MobileNo
        {
            get => _mobileNumber;
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(_mobileNumber))
                {
                    _mobileNumber = value.Trim();
                }
            }
        }

        [Required(ErrorMessage = "Email address is required.")]
        [StringLength(254, ErrorMessage = "Email address cannot exceed 254 characters.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string Email
        {
            get => _email;
            set => _email = value?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Optional alias for Email to accommodate frontend variations.
        /// </summary>
        public string? EmailAddress
        {
            get => _email;
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(_email))
                {
                    _email = value.Trim();
                }
            }
        }

        [StringLength(200, ErrorMessage = "Company name cannot exceed 200 characters.")]
        public string? CompanyName
        {
            get => _companyName;
            set => _companyName = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        [Required(ErrorMessage = "Service description is required.")]
        [StringLength(300, MinimumLength = 5, ErrorMessage = "Service description must be between 5 and 300 characters.")]
        public string ServiceDescription
        {
            get => _serviceDescription;
            set => _serviceDescription = value?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Honeypot field for spam prevention. Must remain empty for valid user submissions.
        /// </summary>
        public string? Website { get; set; }
    }
    #endregion

    #region Contact Inquiry Responses
    public class ContactInquirySubmitResult
    {
        public string EncryptedInquiryId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ConfirmationMessage { get; set; } = string.Empty;
    }

    public class ContactInquiryResponse
    {
        public string EncryptedInquiryId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string ServiceDescription { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
    #endregion
}
