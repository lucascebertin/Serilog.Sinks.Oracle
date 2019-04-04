using Serilog.Events;
using Serilog.Formatting.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Serilog.Sinks.Oracle.Columns
{
    public class Properties
    {
        private readonly ColumnOptions _columnOptions;
        private readonly HashSet<string> _additionalDataColumnNames;
        private readonly JsonFormatter _jsonFormatter;

        public Properties(ColumnOptions columnOptions, HashSet<string> additionalDataColumnNames, IFormatProvider formatProvider)
        {
            _columnOptions = columnOptions;
            _additionalDataColumnNames = additionalDataColumnNames;

            if (_columnOptions.Store.Contains(StandardColumn.LogEvent))
                _jsonFormatter = new JsonFormatter(formatProvider: formatProvider);
        }

        public string ConvertPropertiesToXmlStructure(IEnumerable<KeyValuePair<string, LogEventPropertyValue>> properties)
        {
            var options = _columnOptions.Properties;

            if (options.ExcludeAdditionalProperties)
                properties = properties.Where(p => !_additionalDataColumnNames.Contains(p.Key));

            var sb = new StringBuilder();

            sb.AppendFormat("<{0}>", options.RootElementName);

            foreach (var property in properties)
            {
                var value = XmlPropertyFormatter.Simplify(property.Value, options);

                if (options.OmitElementIfEmpty && string.IsNullOrEmpty(value))
                    continue;

                if (options.UsePropertyKeyAsElementName)
                    sb.AppendFormat("<{0}>{1}</{0}>", XmlPropertyFormatter.GetValidElementName(property.Key), value);
                else
                    sb.AppendFormat("<{0} key='{1}'>{2}</{0}>", options.PropertyElementName, property.Key, value);
            }

            sb.AppendFormat("</{0}>", options.RootElementName);

            return sb.ToString();
        }

        public void ConvertPropertiesToColumn(DataRow row, IReadOnlyDictionary<string, LogEventPropertyValue> properties)
        {
            foreach (var property in properties)
            {

                if (!row.Table.Columns.Contains(property.Key))
                    continue;

                var columnName = property.Key;
                var columnType = row.Table.Columns[columnName].DataType;

                var scalarValue = property.Value as ScalarValue;
                if (scalarValue == null)
                {
                    row[columnName] = property.Value.ToString();
                    continue;
                }

                if (scalarValue.Value == null && row.Table.Columns[columnName].AllowDBNull)
                {
                    row[columnName] = DBNull.Value;
                    continue;
                }

                if (Types.TryChangeType(scalarValue.Value, columnType, out object conversion))
                    row[columnName] = conversion;
                else
                    row[columnName] = property.Value.ToString();
            }
        }


        public string LogEventToJson(LogEvent logEvent)
        {
            if (_columnOptions.LogEvent.ExcludeAdditionalProperties)
            {
                var filteredProperties = logEvent.Properties.Where(p => !_additionalDataColumnNames.Contains(p.Key));
                logEvent = new LogEvent(logEvent.Timestamp, logEvent.Level, logEvent.Exception, logEvent.MessageTemplate, filteredProperties.Select(x => new LogEventProperty(x.Key, x.Value)));
            }

            var sb = new StringBuilder();
            using (var writer = new System.IO.StringWriter(sb))
                _jsonFormatter.Format(logEvent, writer);

            return sb.ToString();
        }
    }
}
