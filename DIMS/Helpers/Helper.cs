using DIMS.Models;
using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIMS.Helpers
{
    internal static class Helper
    {
        public static readonly int PRODUCT_CODE_LENGTH = 18;
        public static readonly int PRODUCT_MODEL_INDEX = 6;
        public static readonly int PRODUCT_MODEL_LENGTH = 4;

        public static string? TrimProductCode(string? code)
        {
            if (code == null)
                return null;

            string result = code.Trim();
            if (string.IsNullOrEmpty(result))
                return null;

            if(result.Length >= PRODUCT_CODE_LENGTH)
                result = result.Substring(0, PRODUCT_CODE_LENGTH);

            return result;
        }

        public static string? GetProductPosId(string? id)
        {
            if (id == null || id.Length != PRODUCT_CODE_LENGTH)
                return null;

            return string.Concat("69248981", id.AsSpan(PRODUCT_MODEL_INDEX, PRODUCT_MODEL_LENGTH));
        }

        public static string? GetProductModel(string? code)
        {
            string? pcode = GetProductPosId(TrimProductCode(code));
            if (string.IsNullOrEmpty(pcode))
                return null;

            var items = MysqlDbHelper.Default.ProductQuery(pcode, null);
            if (items == null || items.Count() == 0)
                return null;

            return items.ElementAt(0).Model;
        }

        public static ProductionDataModel? GetLastProductionRecord(string? code)
        {
            if (string.IsNullOrEmpty(code))
                return null;

            var result = MysqlDbHelper.Default.HistoryQuery(DateTime.Now - TimeSpan.FromDays(3), DateTime.Now, null, code);
            if (result == null || result.Count() == 0)
                return null;

            return result.FirstOrDefault();
        }


    }
}
