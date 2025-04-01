using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace XmlParser
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Example usage
            Console.WriteLine("XML Parser - Struct <-> XML Converter");
            Console.WriteLine("------------------------------------");

            // Example 1: Convert struct to XML
            string structDef = @"
struct data{
    int a;
    string b;
}";

            Console.WriteLine("Original struct:");
            Console.WriteLine(structDef);

            string xml = StructParser.StructToXml(structDef);
            Console.WriteLine("\nConverted to XML:");
            Console.WriteLine(xml);

            // Example 2: Convert XML back to struct
            string reconstructedStruct = StructParser.XmlToStruct(xml);
            Console.WriteLine("\nConverted back to struct:");
            Console.WriteLine(reconstructedStruct);

            // Wait for user input before closing
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }

    /// <summary>
    /// Parser for converting between C-style struct definitions and XML
    /// </summary>
    public static class StructParser
    {
        /// <summary>
        /// Converts a C-style struct definition to XML
        /// </summary>
        /// <param name="structDefinition">The C struct definition</param>
        /// <returns>XML representation of the struct</returns>
        public static string StructToXml(string structDefinition)
        {
            try
            {
                // Parse the struct definition
                StructInfo structInfo = ParseStructDefinition(structDefinition);

                // Create XML from the parsed struct
                XDocument doc = CreateXmlFromStruct(structInfo);

                // Convert to formatted string
                return FormatXml(doc);
            }
            catch (Exception ex)
            {
                return $"Error converting struct to XML: {ex.Message}";
            }
        }

        /// <summary>
        /// Converts an XML representation back to a C-style struct definition
        /// </summary>
        /// <param name="xml">The XML representation of the struct</param>
        /// <returns>C struct definition</returns>
        public static string XmlToStruct(string xml)
        {
            try
            {
                // Parse the XML back to a struct
                StructInfo structInfo = ParseXmlToStruct(xml);

                // Create struct definition
                return CreateStructDefinition(structInfo);
            }
            catch (Exception ex)
            {
                return $"Error converting XML to struct: {ex.Message}";
            }
        }

        #region Struct to XML conversion

        /// <summary>
        /// Parses a C-style struct definition
        /// </summary>
        private static StructInfo ParseStructDefinition(string structDefinition)
        {
            // Extract struct name using regex
            string structNamePattern = @"struct\s+(\w+)\s*\{";
            Match nameMatch = Regex.Match(structDefinition, structNamePattern);

            if (!nameMatch.Success)
                throw new FormatException("Could not find valid struct definition");

            string structName = nameMatch.Groups[1].Value;

            // Extract struct fields
            string fieldsPattern = @"struct\s+\w+\s*\{(.*?)\}";
            Match fieldsMatch = Regex.Match(structDefinition, fieldsPattern, RegexOptions.Singleline);

            if (!fieldsMatch.Success)
                throw new FormatException("Could not parse struct body");

            string fieldsText = fieldsMatch.Groups[1].Value;

            // Parse individual fields
            List<StructField> fields = new List<StructField>();
            string fieldPattern = @"\s*(\w+)\s+(\w+)\s*;";
            MatchCollection fieldMatches = Regex.Matches(fieldsText, fieldPattern);

            foreach (Match fieldMatch in fieldMatches)
            {
                string fieldType = fieldMatch.Groups[1].Value;
                string fieldName = fieldMatch.Groups[2].Value;

                fields.Add(new StructField { Name = fieldName, Type = fieldType });
            }

            return new StructInfo { Name = structName, Fields = fields };
        }

        /// <summary>
        /// Creates an XML document from a parsed struct
        /// </summary>
        private static XDocument CreateXmlFromStruct(StructInfo structInfo)
        {
            XDocument doc = new XDocument();
            XElement rootElement = new XElement(structInfo.Name, new XAttribute("type", "struct"));

            foreach (var field in structInfo.Fields)
            {
                XElement fieldElement = new XElement(field.Name,
                    new XAttribute("value", ""),
                    new XAttribute("type", field.Type));

                rootElement.Add(fieldElement);
            }

            doc.Add(rootElement);
            return doc;
        }

        /// <summary>
        /// Formats an XML document as a string with proper indentation
        /// </summary>
        private static string FormatXml(XDocument doc)
        {
            var sb = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "\t",
                NewLineChars = Environment.NewLine,
                NewLineHandling = NewLineHandling.Replace
            };

            using (var writer = XmlWriter.Create(sb, settings))
            {
                doc.Save(writer);
            }

            return sb.ToString();
        }

        #endregion

        #region XML to Struct conversion

        /// <summary>
        /// Parses an XML representation back to struct information
        /// </summary>
        private static StructInfo ParseXmlToStruct(string xml)
        {
            XDocument doc = XDocument.Parse(xml);
            XElement rootElement = doc.Root;

            // Verify this is a struct
            string structType = rootElement.Attribute("type")?.Value;
            if (structType != "struct")
                throw new FormatException("XML does not represent a struct (missing or incorrect type attribute)");

            string structName = rootElement.Name.LocalName;
            List<StructField> fields = new List<StructField>();

            // Process each child element as a field
            foreach (XElement fieldElement in rootElement.Elements())
            {
                string fieldName = fieldElement.Name.LocalName;
                string fieldType = fieldElement.Attribute("type")?.Value;

                if (string.IsNullOrEmpty(fieldType))
                    throw new FormatException($"Field '{fieldName}' is missing a type attribute");

                fields.Add(new StructField { Name = fieldName, Type = fieldType });
            }

            return new StructInfo { Name = structName, Fields = fields };
        }

        /// <summary>
        /// Creates a C-style struct definition from struct information
        /// </summary>
        private static string CreateStructDefinition(StructInfo structInfo)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"struct {structInfo.Name}{{");

            foreach (var field in structInfo.Fields)
            {
                sb.AppendLine($"\t{field.Type} {field.Name};");
            }

            sb.AppendLine("}");

            return sb.ToString();
        }

        #endregion
    }

    #region Helper Classes

    /// <summary>
    /// Represents information about a struct
    /// </summary>
    public class StructInfo
    {
        /// <summary>
        /// Name of the struct
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Fields in the struct
        /// </summary>
        public List<StructField> Fields { get; set; } = new List<StructField>();
    }

    /// <summary>
    /// Represents a field in a struct
    /// </summary>
    public class StructField
    {
        /// <summary>
        /// Name of the field
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Data type of the field
        /// </summary>
        public string Type { get; set; }
    }

    #endregion
}
