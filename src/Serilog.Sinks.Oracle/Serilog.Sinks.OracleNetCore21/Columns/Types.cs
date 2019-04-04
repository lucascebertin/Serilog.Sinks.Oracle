using System;

namespace Serilog.Sinks.Oracle.Columns
{
    public class Types
    {
        public static bool TryChangeType(object obj, Type type, out object conversion)
        {
            conversion = null;
            try
            {
                conversion = Convert.ChangeType(obj, type);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
