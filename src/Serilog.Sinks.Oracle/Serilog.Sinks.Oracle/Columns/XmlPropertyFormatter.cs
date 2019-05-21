using Serilog.Events;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Serilog.Sinks.Oracle.Columns
{
    public static class XmlPropertyFormatter
    {
        /// <summary>
        ///     Simplify the object so as to make handling the serialized
        ///     representation easier.
        /// </summary>
        /// <param name="value">The value to simplify (possibly null).</param>
        /// <param name="options">Options to use during formatting</param>
        /// <returns>A simplified representation.</returns>
        public static string Simplify(LogEventPropertyValue value, ColumnOptions.PropertiesColumnOptions options)
        {
            var scalar = value as ScalarValue;
            if (scalar != null)
                return SimplifyScalar(scalar.Value);

            var dict = value as DictionaryValue;
            if (dict != null)
            {
                var sb = new StringBuilder();

                bool isEmpty = true;

                foreach (var element in dict.Elements)
                {
                    var itemValue = Simplify(element.Value, options);
                    if (options.OmitElementIfEmpty && string.IsNullOrEmpty(itemValue))
                        continue;

                    if (isEmpty)
                    {
                        isEmpty = false;

                        if (!options.OmitDictionaryContainerElement)
                            sb.AppendFormat("<{0}>", options.DictionaryElementName);
                    }

                    var key = SimplifyScalar(element.Key.Value);
                    if (options.UsePropertyKeyAsElementName)
                        sb.AppendFormat("<{0}>{1}</{0}>", GetValidElementName(key), itemValue);
                    else
                        sb.AppendFormat("<{0} key='{1}'>{2}</{0}>", options.ItemElementName, key, itemValue);
                }

                if (!isEmpty && !options.OmitDictionaryContainerElement)
                    sb.AppendFormat("</{0}>", options.DictionaryElementName);

                return sb.ToString();
            }

            var seq = value as SequenceValue;
            if (seq != null)
            {
                var sb = new StringBuilder();

                bool isEmpty = true;

                foreach (var element in seq.Elements)
                {
                    var itemValue = Simplify(element, options);
                    if (options.OmitElementIfEmpty && string.IsNullOrEmpty(itemValue))
                        continue;

                    if (isEmpty)
                    {
                        isEmpty = false;
                        if (!options.OmitSequenceContainerElement)
                            sb.AppendFormat("<{0}>", options.SequenceElementName);
                    }

                    sb.AppendFormat("<{0}>{1}</{0}>", options.ItemElementName, itemValue);
                }

                if (!isEmpty && !options.OmitSequenceContainerElement)
                    sb.AppendFormat("</{0}>", options.SequenceElementName);

                return sb.ToString();
            }

            var str = value as StructureValue;
            if (str != null)
            {
                var props = str.Properties.ToDictionary(p => p.Name, p => Simplify(p.Value, options));

                var sb = new StringBuilder();

                bool isEmpty = true;

                foreach (var element in props)
                {
                    var itemValue = element.Value;
                    if (options.OmitElementIfEmpty && string.IsNullOrEmpty(itemValue))
                        continue;

                    if (isEmpty)
                    {
                        isEmpty = false;

                        if (!options.OmitStructureContainerElement)
                            if (options.UsePropertyKeyAsElementName)
                                sb.AppendFormat("<{0}>", GetValidElementName(str.TypeTag));
                            else
                                sb.AppendFormat("<{0} type='{1}'>", options.StructureElementName, str.TypeTag);
                    }

                    if (options.UsePropertyKeyAsElementName)
                        sb.AppendFormat("<{0}>{1}</{0}>", GetValidElementName(element.Key), itemValue);
                    else
                        sb.AppendFormat("<{0} key='{1}'>{2}</{0}>", options.PropertyElementName,
                            element.Key, itemValue);
                }

                if (!isEmpty && !options.OmitStructureContainerElement)
                {
                    if (options.UsePropertyKeyAsElementName)
                        sb.AppendFormat("</{0}>", GetValidElementName(str.TypeTag));
                    else
                        sb.AppendFormat("</{0}>", options.StructureElementName);
                }

                return sb.ToString();
            }

            return null;
        }

        internal static string GetValidElementName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "x";

            var validName = name.Trim();
            if (!char.IsLetter(validName[0]) || validName.StartsWith("xml", StringComparison.CurrentCultureIgnoreCase))
                validName = "x" + name;

            validName = Regex.Replace(validName, @"\s", "_");

            return validName;
        }

        static string SimplifyScalar(object value) => 
            value == null ? null : new XText(value.ToString()).ToString();
    }
}
