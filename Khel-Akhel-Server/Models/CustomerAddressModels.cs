namespace Khel_Akhel_Server.Models
{
    public class CustomerAddressCreateRequest
    {
        public string? AddressTitle { get; set; }
        public string? AddressType { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string MobileNo { get; set; } = string.Empty;
        public string AddressLine1 { get; set; } = string.Empty;
        public string? AddressLine2 { get; set; }
        public string? Landmark { get; set; }
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Pincode { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }

    public class CustomerAddressUpdateRequest
    {
        public string EncryptedAddressId { get; set; } = string.Empty;
        public string? AddressTitle { get; set; }
        public string? AddressType { get; set; }
        public string? FullName { get; set; }
        public string? MobileNo { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? Landmark { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? Pincode { get; set; }
        public bool? IsDefault { get; set; }
    }

    public class CustomerAddressResponse
    {
        public string EncryptedAddressId { get; set; } = string.Empty;
        public string AddressTitle { get; set; } = string.Empty;
        public string AddressType { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string MobileNo { get; set; } = string.Empty;
        public string AddressLine1 { get; set; } = string.Empty;
        public string AddressLine2 { get; set; } = string.Empty;
        public string Landmark { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Pincode { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }
        public string CreatedDate { get; set; } = string.Empty;
    }
}
