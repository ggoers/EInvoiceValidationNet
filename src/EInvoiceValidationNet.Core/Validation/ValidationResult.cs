using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EInvoiceValidationNet.Core.Validation.SchematronValidation;
using EInvoiceValidationNet.Core.Validation.SchemaValidation;

namespace EInvoiceValidationNet.Core.Validation
{
    /// <summary>
    /// The outcome of validating one invoice: whether the input could be parsed, which validation steps ran, what
    /// they found, and which report they produced.
    /// </summary>
    public sealed class ValidationResult
    {
        /// <summary>
        /// Initializes a validation result.
        /// </summary>
        /// <param name="status">Required. How far the validation pipeline ran.</param>
        /// <param name="wellFormednessResult">Required. The result of parsing the invoice XML.</param>
        /// <param name="schemaValidationResult">Optional. The XSD validation result, or <c>null</c> when no schema validation ran.</param>
        /// <param name="schematronValidationResults">Optional. The Schematron results in configuration order; <c>null</c> means none ran.</param>
        /// <param name="isAccepted">Optional. The acceptance decision, or <c>null</c> when no decision was made.</param>
        /// <param name="report">Optional. The generated validation report, or <c>null</c> when no report transformation is
        /// configured or creating the report failed.</param>
        /// <param name="reportInput">Optional. The intermediate report input document the report was created from; it is
        /// available even when the report transformation itself failed.</param>
        /// <param name="processingErrors">Optional. Errors that stopped the validation before it completed; <c>null</c> means none occurred.</param>
        /// <param name="isProcessingSuccessful">Whether the pipeline ran completely. <c>false</c> when a processing error
        /// stopped it, including a failed parse of the invoice.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="wellFormednessResult"/> is <c>null</c>.</exception>
        public ValidationResult(
            ValidationProcessStatus status,
            WellFormednessValidationResult wellFormednessResult,
            SchemaValidationResult schemaValidationResult,
            IEnumerable<SchematronValidationResult> schematronValidationResults,
            bool? isAccepted,
            XDocument report = null,
            XDocument reportInput = null,
            IEnumerable<string> processingErrors = null,
            bool isProcessingSuccessful = true)
        {
            Status = status;
            WellFormednessResult = wellFormednessResult ?? throw new ArgumentNullException(nameof(wellFormednessResult));
            SchemaValidationResult = schemaValidationResult;
            SchematronValidationResults = schematronValidationResults?.ToList() ?? new List<SchematronValidationResult>();
            IsAccepted = isAccepted;
            Report = report;
            ReportInput = reportInput;
            ProcessingErrors = processingErrors?.ToList() ?? new List<string>();
            IsProcessingSuccessful = isProcessingSuccessful;
        }

        /// <summary>
        /// How far the validation pipeline ran. <see cref="ValidationProcessStatus.Completed"/> does not mean the invoice
        /// is valid — check <see cref="SchemaValidationResult"/> and <see cref="SchematronValidationResults"/>.
        /// </summary>
        public ValidationProcessStatus Status { get; }

        /// <summary>
        /// The result of parsing the invoice XML. <see cref="WellFormednessValidationResult.IsValid"/> is <c>false</c>
        /// when the document could not be read or is not well-formed.
        /// </summary>
        public WellFormednessValidationResult WellFormednessResult { get; }

        /// <summary>
        /// The XML schema validation result, or <c>null</c> when the scenario does not validate against a schema or the
        /// pipeline stopped before that step.
        /// </summary>
        public SchemaValidationResult SchemaValidationResult { get; }

        /// <summary>
        /// One result per configured Schematron validation, in configuration order. Empty when no Schematron validation is
        /// configured or when it was skipped because the schema validation failed.
        /// </summary>
        public IReadOnlyList<SchematronValidationResult> SchematronValidationResults { get; }

        /// <summary>
        /// The acceptance decision that results from the scenario's <c>acceptMatch</c> expression, or <c>null</c> when no
        /// decision is available (e.g. the scenario does not define one or the processing stopped before it was evaluated).
        /// </summary>
        public bool? IsAccepted { get; }

        /// <summary>
        /// The generated validation report, or <c>null</c> when no report transformation is configured for the outcome
        /// or creating the report failed (see <see cref="ProcessingErrors"/>).
        /// </summary>
        public XDocument Report { get; }

        /// <summary>
        /// The intermediate report input document the report was created from, or <c>null</c> when the pipeline stopped
        /// before it was built. It is available even when the report transformation itself failed.
        /// </summary>
        public XDocument ReportInput { get; }

        /// <summary>
        /// Errors that stopped the validation before it completed (e.g. a failing report transformation); empty when the
        /// pipeline ran completely.
        /// </summary>
        public IReadOnlyList<string> ProcessingErrors { get; }

        /// <summary>
        /// Whether the validation pipeline ran completely. <c>false</c> when a processing error stopped it, including a
        /// failed parse of the invoice (see <see cref="Status"/> and <see cref="ProcessingErrors"/>).
        /// </summary>
        public bool IsProcessingSuccessful { get; }

        internal static ValidationResult NoScenarioMatched(XDocument report = null, XDocument reportInput = null,
            IEnumerable<string> processingErrors = null, bool isProcessingSuccessful = true)
        {
            return new ValidationResult(ValidationProcessStatus.NoScenarioMatched, new WellFormednessValidationResult(),
                null, null, false, report, reportInput, processingErrors, isProcessingSuccessful);
        }

        internal static ValidationResult ParsingInputXmlFailed(Exception error, XDocument report = null,
            XDocument reportInput = null, IEnumerable<string> processingErrors = null, bool isProcessingSuccessful = false)
        {
            return new ValidationResult(ValidationProcessStatus.ParsingInputXmlFailed,
                new WellFormednessValidationResult(error), null, null, false, report, reportInput,
                processingErrors, isProcessingSuccessful);
        }
    }

    /// <summary>
    /// Describes how far the validation pipeline ran. It does not tell whether the invoice is valid.
    /// </summary>
    public enum ValidationProcessStatus
    {
        /// <summary>The invoice was parsed, a scenario matched and the configured validation steps were executed.</summary>
        Completed,

        /// <summary>No scenario matched the invoice.</summary>
        NoScenarioMatched,

        /// <summary>The invoice could not be read or is not well-formed, so no validation step ran;
        /// see <see cref="ValidationResult.WellFormednessResult"/>.</summary>
        ParsingInputXmlFailed,

        /// <summary>A processing error stopped the validation before it completed; see
        /// <see cref="ValidationResult.ProcessingErrors"/>. A report may still have been created.</summary>
        ProcessingFailed
    }
}
