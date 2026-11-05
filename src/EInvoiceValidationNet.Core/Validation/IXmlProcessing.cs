using System.Collections.Generic;
using System.Xml.Linq;

namespace EInvoiceValidationNet.Core.Validation
{

    /// <summary>
    /// Provides the XML processing capabilities required by the core: evaluating XPath expressions and running XSLT
    /// transformations.
    /// </summary>
    /// <remarks>
    /// Implementations must support at least XSLT 2.0 and XPath 2.0.
    /// This abstraction keeps the Core independent of specific XML processing engines and third-party libraries, so the
    /// engine (e.g. Saxon CS HE) can be replaced without touching the validation logic.
    /// Implementations are stateless and safe to reuse for multiple documents.
    /// </remarks>
    public interface IXmlProcessing
    {
        /// <summary>
        /// Evaluates an XPath expression (at least XPath 2.0 support is needed) and returns its effective boolean value:
        /// an empty result is <c>false</c>, a non-empty node result is <c>true</c> and atomic values are converted
        /// following the XPath 2.0 rules.
        /// </summary>
        /// <param name="xmlDocument">Required. The document to evaluate the expression on.</param>
        /// <param name="expression">Required. The XPath expression.</param>
        /// <param name="namespaces">Optional namespace declarations (prefix to URI) which can be referenced by prefixes
        /// within the expression; <c>null</c> means no prefixes are available.</param>
        /// <returns>The effective boolean value of the expression result.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="xmlDocument"/> or <paramref name="expression"/> is <c>null</c>.</exception>
        /// <exception cref="System.InvalidOperationException">Thrown when the expression is invalid or its effective boolean value is undefined.</exception>
        bool EvaluateXPathBoolean(XDocument xmlDocument, string expression, IEnumerable<KeyValuePair<string, string>> namespaces = null);

        /// <summary>
        /// Transforms the input document with the XSLT (at least XSLT 2.0 support is needed) located at the given path
        /// and returns the transformed XML. The path is also used as base URI, so relative references
        /// (e.g. <c>xsl:import</c>) resolve next to the XSLT file.
        /// </summary>
        /// <param name="inputDocument">Required. The document to transform.</param>
        /// <param name="xsltPath">Required. Path of the XSLT to execute; the file must exist.</param>
        /// <param name="parameters">Optional named stylesheet parameters with document values (e.g. <c>input-document</c>);
        /// <c>null</c> means no parameters are passed.</param>
        /// <returns>The transformation result.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="inputDocument"/> or <paramref name="xsltPath"/> is <c>null</c>.</exception>
        /// <exception cref="System.IO.IOException">Thrown when the XSLT file cannot be read.</exception>
        /// <exception cref="System.InvalidOperationException">Thrown when the stylesheet cannot be compiled or executed.</exception>
        XDocument Transform(XDocument inputDocument, string xsltPath, IReadOnlyDictionary<string, XDocument> parameters = null);
    }
}
