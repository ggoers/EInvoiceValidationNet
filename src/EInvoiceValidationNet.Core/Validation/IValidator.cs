using System.IO;
using System.Xml.Linq;

namespace EInvoiceValidationNet.Validation
{
    /// <summary>
    /// Validates an e-invoice against a <see cref="Configuration.Configuration"/>.
    /// </summary>
    /// <remarks>
    /// A malformed or unreadable invoice does not throw; it is reported through the returned result
    /// (<see cref="ValidationProcessStatus.ParsingInputXmlFailed"/> and <see cref="ValidationResult.WellFormednessResult"/>).
    /// Implementations are stateless, so one instance can be reused for many invoices.
    /// </remarks>
    public interface IValidator
    {
        /// <summary>
        /// Validates the invoice contained in a stream.
        /// </summary>
        /// <param name="invoiceXml">Required. The XML document to validate; it is read from the current position and is not disposed.</param>
        /// <param name="configuration">Required. The configuration that provides the scenarios.</param>
        /// <param name="documentName">Optional. A reference of the validated document (e.g. its file name) that appears in
        /// the report; <c>null</c> means the report carries an empty reference.</param>
        /// <returns>The result of the validation; never <c>null</c>.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="invoiceXml"/> or <paramref name="configuration"/> is <c>null</c>.</exception>
        ValidationResult Validate(Stream invoiceXml, Configuration.Configuration configuration, string documentName = null);

        /// <summary>
        /// Validates the invoice stored in a file.
        /// </summary>
        /// <param name="pathToInvoiceXml">Required. Path of the XML document to validate.</param>
        /// <param name="configuration">Required. The configuration that provides the scenarios.</param>
        /// <param name="documentName">Optional. A reference of the validated document that appears in the report;
        /// <c>null</c> means the given path is used as the reference.</param>
        /// <returns>The result of the validation; never <c>null</c>. IO problems while opening the file are reported as
        /// <see cref="ValidationProcessStatus.ParsingInputXmlFailed"/> instead of being thrown.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="pathToInvoiceXml"/> or <paramref name="configuration"/> is <c>null</c>.</exception>
        ValidationResult Validate(string pathToInvoiceXml, Configuration.Configuration configuration, string documentName = null);
    }
}
