using Khel_Akhel_Server.BL.Common;

namespace Khel_Akhel_Server.Models
{
    public class CommonResponse
    {
        #region COMMON API RESPONSE
        public class ApiResponse
        {
            public bool Success { get; set; }

            public int StatusCode { get; set; }

            public string Message { get; set; } = string.Empty;

            public object? Data { get; set; }

            public List<string>? Errors { get; set; }

            public string Timestamp { get; set; } = DateTimeFormat.Format(DateTime.Now);
        }
        #endregion
    }
}
