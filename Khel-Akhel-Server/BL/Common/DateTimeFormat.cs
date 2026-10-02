namespace Khel_Akhel_Server.BL.Common
{
    public class DateTimeFormat
    {
        public static string Format(DateTime? date)
        {
            if (date == null)
            {
                return string.Empty;
            }
            return date.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
        }
    }
}
