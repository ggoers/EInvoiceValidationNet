using System;

namespace EInvoiceValidationNet.Configuration
{
    /// <summary>
    /// Configures one Schematron validation step of a scenario (a <c>validateWithSchematron</c> element of <c>scenarios.xml</c>).
    /// The configured XSLT must transform the invoice into an SVRL document.
    /// </summary>
    public sealed class SchematronValidationConfiguration
    {
        /// <summary>
        /// Initializes a Schematron validation step.
        /// </summary>
        /// <param name="xsltFilePath">Required. Path of the compiled Schematron XSLT 2.0 stylesheet (it is trimmed). Relative references
        /// inside the stylesheet resolve next to this file.</param>
        /// <param name="name">Optional. Human readable name of the validation step, e.g. "Schematron rules for EN16931 (UBL)"; it is trimmed.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="xsltFilePath"/> is <c>null</c>.</exception>
        public SchematronValidationConfiguration(string xsltFilePath, string name)
        {
            if (xsltFilePath is null)
            {
                throw new ArgumentNullException(nameof(xsltFilePath));
            }

            XsltFilePath = xsltFilePath.Trim();
            Name = name?.Trim();
            OriginalLocation = XsltFilePath;
        }

        /// <summary>
        /// Path of the Schematron XSLT (trimmed).
        /// </summary>
        public string XsltFilePath { get; }

        /// <summary>
        /// The optional name of the validation step, or <c>null</c> when the configuration did not specify one.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The resource location as it was configured (the absolute <see cref="XsltFilePath"/> for code-defined
        /// configurations, the original relative location for <c>scenarios.xml</c> configurations). It is embedded in the
        /// report input.
        /// </summary>
        internal string OriginalLocation { get; set; }
    }
}
