using System.Xml.Linq;

namespace EInvoiceValidationNet.Validation.SchemaValidation
{
    /// <summary>
    /// Validates an already parsed (well-formed) e-invoice against the XML schema of a scenario.
    /// </summary>
    internal interface ISchemaValidator
    {
        /// <summary>
        /// Validates the given invoice against the XML schema (including relative schema imports) located at the given path.
        /// </summary>
        /// <param name="invoiceXml">the parsed invoice document (should be loaded with line info to get row/column numbers in errors)</param>
        /// <param name="xsdFilePath">path of the main XSD file</param>
        SchemaValidationResult Validate(XDocument invoiceXml, string xsdFilePath);
    }
}
