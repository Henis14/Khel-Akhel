using System.Data;

namespace Khel_Akhel_Server.BL.Common
{
    public class DataTableMapper
    {
        public static List<Dictionary<string, object?>> ToList(DataTable dt)
        {
            var list = new List<Dictionary<string, object?>>();

            foreach (DataRow row in dt.Rows)
            {
                var item = new Dictionary<string, object?>();

                foreach (DataColumn col in dt.Columns)
                {
                    object? value = row[col];

                    if (value == DBNull.Value)
                    {
                        item[col.ColumnName] = null;
                    }
                    else if (value is DateTime dateValue)
                    {
                        item[col.ColumnName] = DateTimeFormat.Format(dateValue);
                    }
                    else
                    {
                        item[col.ColumnName] = value;
                    }
                }

                list.Add(item);
            }

            return list;
        }
    }
}
