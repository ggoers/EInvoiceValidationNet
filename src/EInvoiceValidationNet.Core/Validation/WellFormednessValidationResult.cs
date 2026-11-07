using System;

namespace EInvoiceValidationNet.Validation
{
    /// <summary>
    /// Result of the well-formedness check, i.e. whether the invoice XML could be read and parsed.
    /// </summary>
    public sealed class WellFormednessValidationResult
    {
        /// <summary>
        /// Initializes a successful result, meaning the invoice was parsed without errors.
        /// </summary>
        public WellFormednessValidationResult()
        {
            IsValid = true;
        }

        /// <summary>
        /// Initializes a failed result.
        /// </summary>
        /// <param name="error">Required. The error raised while reading or parsing the invoice.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is <c>null</c>.</exception>
        public WellFormednessValidationResult(Exception error)
        {
            IsValid = false;
            Error = error ?? throw new ArgumentNullException(nameof(error));
        }

        /// <summary>
        /// <c>true</c> when the invoice was read and parsed successfully.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// The error that prevented parsing, or <c>null</c> when <see cref="IsValid"/> is <c>true</c>.
        /// </summary>
        public Exception Error { get; }
    }
}
