using System;
using System.Collections.Generic;
using System.Linq;

namespace EInvoiceValidationNet.Validation.SchemaValidation
{
    /// <summary>
    /// Severity of a finding produced by the XML schema (XSD) validation.
    /// </summary>
    public enum XmlSyntaxErrorSeverity
    {
        /// <summary>A schema hint that does not make the document invalid (e.g. a part that could not be validated).</summary>
        Warning,

        /// <summary>A schema violation; the document is not valid against the XSD.</summary>
        Error,

        /// <summary>The schema could not be compiled or the validation aborted, e.g. because the XSD file is missing.</summary>
        FatalError
    }

    /// <summary>
    /// A single finding of the XML schema validation, or the error that prevented the validation from running.
    /// </summary>
    public sealed class XmlSyntaxError
    {
        /// <summary>
        /// Initializes a schema finding.
        /// </summary>
        /// <param name="message">Required. The error message; it is trimmed.</param>
        /// <param name="severity">Required. Must be a defined <see cref="XmlSyntaxErrorSeverity"/> value.</param>
        /// <param name="rowNumber">Optional. Line in the validated document, or <c>null</c> when unknown (e.g. for schema compile errors).</param>
        /// <param name="columnNumber">Optional. Column in the validated document, or <c>null</c> when unknown.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="message"/> is <c>null</c>, empty or whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="severity"/> is not a defined <see cref="XmlSyntaxErrorSeverity"/> value.</exception>
        public XmlSyntaxError(string message, XmlSyntaxErrorSeverity severity, int? rowNumber, int? columnNumber)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message cannot be null, empty or whitespace", nameof(message));
            }

            if (!Enum.IsDefined(typeof(XmlSyntaxErrorSeverity), severity))
            {
                throw new ArgumentOutOfRangeException(nameof(severity));
            }

            Message = message.Trim();
            Severity = severity;
            RowNumber = rowNumber;
            ColumnNumber = columnNumber;
        }

        /// <summary>
        /// The error message (trimmed).
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// The severity of the finding.
        /// </summary>
        public XmlSyntaxErrorSeverity Severity { get; }

        /// <summary>
        /// The line in the validated document the finding refers to, or <c>null</c> when unknown.
        /// </summary>
        public int? RowNumber { get; }

        /// <summary>
        /// The column in the validated document the finding refers to, or <c>null</c> when unknown.
        /// </summary>
        public int? ColumnNumber { get; }
    }

    /// <summary>
    /// Result of the XML schema (XSD) validation step of a scenario.
    /// </summary>
    public sealed class SchemaValidationResult
    {
        /// <summary>
        /// Initializes a schema validation result.
        /// </summary>
        /// <param name="isValid">Required. <c>true</c> when the document is valid (findings of severity
        /// <see cref="XmlSyntaxErrorSeverity.Warning"/> do not make it invalid).</param>
        /// <param name="errors">Required. All findings; may be empty.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is <c>null</c>.</exception>
        public SchemaValidationResult(bool isValid, IEnumerable<XmlSyntaxError> errors)
        {
            IsValid = isValid;
            Errors = errors?.ToList() ?? throw new ArgumentNullException(nameof(errors));
        }

        /// <summary>
        /// <c>true</c> when no finding of severity <see cref="XmlSyntaxErrorSeverity.Error"/> or
        /// <see cref="XmlSyntaxErrorSeverity.FatalError"/> occurred.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// All schema findings in the order the validator reported them; empty when the document is valid.
        /// </summary>
        public IReadOnlyList<XmlSyntaxError> Errors { get; }
    }
}
