using System;

namespace EInvoiceValidationNet.Configuration
{
    /// <summary>
    /// Configures the XML schema (XSD) validation step of a scenario (the <c>validateWithXmlSchema</c> element of <c>scenarios.xml</c>).
    /// </summary>
    public sealed class SchemaValidationConfiguration
    {
        /// <summary>
        /// Initializes the schema validation step.
        /// </summary>
        /// <param name="xsdFilePath">Required. Path of the main XSD file (it is trimmed); relative paths are interpreted by the
        /// schema validator, typically relative to the working directory.</param>
        /// <param name="name">Optional. Human readable name of the validation step, e.g. "XML Schema for UBL 2.1 Invoice"; it is trimmed.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="xsdFilePath"/> is <c>null</c>.</exception>
        public SchemaValidationConfiguration(string xsdFilePath, string name)
        {
            if (xsdFilePath is null)
            {
                throw new ArgumentNullException(nameof(xsdFilePath));
            }

            XsdFilePath = xsdFilePath.Trim();
            Name = name?.Trim();
            OriginalLocation = XsdFilePath;
        }

        /// <summary>
        /// Path of the main XSD file (trimmed). Imports and includes inside the schema are resolved relative to it.
        /// </summary>
        public string XsdFilePath { get; }

        /// <summary>
        /// The optional name of the validation step, or <c>null</c> when the configuration did not specify one.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The resource location as it was configured (the absolute <see cref="XsdFilePath"/> for code-defined
        /// configurations, the original relative location for <c>scenarios.xml</c> configurations). It is embedded in the
        /// report input.
        /// </summary>
        internal string OriginalLocation { get; set; }
    }
}
