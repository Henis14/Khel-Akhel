namespace Khel_Akhel_Server.Models.LocationMaster
{
    public class CountryResponse
    {
        public long Id { get; set; }
        public string CountryName { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string PhoneCode { get; set; } = string.Empty;
    }

    public class StateResponse
    {
        public long Id { get; set; }
        public long CountryId { get; set; }
        public string StateName { get; set; } = string.Empty;
        public string StateCode { get; set; } = string.Empty;
    }
}
