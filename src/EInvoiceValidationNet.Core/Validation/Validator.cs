using EInvoiceValidationNet.Validation.ReportTransformation;
using EInvoiceValidationNet.Validation.SchematronValidation;
using EInvoiceValidationNet.Validation.SchemaValidation;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;

namespace EInvoiceValidationNet.Validation
{
    using Configuration = EInvoiceValidationNet.Configuration.Configuration;

    internal sealed class Validator : IValidator
    {
        private readonly IValidationStep[] _validationSteps;

        public Validator(IXmlProcessing xmlProcessing, ISchemaValidator schemaValidator, ISchematronValidator schematronValidator)
        {
            var matchExpressionEvaluator = new MatchExpressionEvaluator(xmlProcessing);

            _validationSteps = new IValidationStep[]
            {
                new ScenarioSelectionStep(matchExpressionEvaluator),
                new SchemaValidationStep(schemaValidator),
                new SchematronValidationStep(schematronValidator),
                new ReportTransformationStep(xmlProcessing, schemaValidator),
                new AcceptanceStep(matchExpressionEvaluator)
            };
        }

        public ValidationResult Validate(string pathToInvoiceXml, Configuration configuration, string documentName = null)
        {
            if (pathToInvoiceXml is null)
            {
                throw new ArgumentNullException(nameof(pathToInvoiceXml));
            }

            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            byte[] input;
            try
            {
                input = File.ReadAllBytes(pathToInvoiceXml);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                return ValidationResult.ParsingInputXmlFailed(e);
            }

            return Validate(input, documentName ?? pathToInvoiceXml, configuration);
        }

        public ValidationResult Validate(Stream invoiceXml, Configuration configuration, string documentName = null)
        {
            if (invoiceXml is null)
            {
                throw new ArgumentNullException(nameof(invoiceXml));
            }

            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            using (var inputCopy = new MemoryStream())
            {
                invoiceXml.CopyTo(inputCopy);

                return Validate(inputCopy.ToArray(), documentName ?? string.Empty, configuration);
            }
        }

        private ValidationResult Validate(byte[] input, string documentReference, Configuration configuration)
        {
            var documentHash = ComputeDocumentHash(input);
            var (invoiceDocument, wellFormednessResult) = ReadAndParseInvoice(input);

            var validationContext = new ValidationContext(configuration, documentReference)
            {
                DocumentHash = documentHash,
                InvoiceDocument = invoiceDocument,
                WellFormednessValidationResult = wellFormednessResult
            };

            if (!wellFormednessResult.IsValid)
            {
                validationContext.AddProcessingError(
                    $"Input XML could not be parsed: {wellFormednessResult.Error.Message}");
            }

            foreach (var validationStep in _validationSteps)
            {
                validationStep.Execute(validationContext);
            }

            return CreateResult(validationContext);
        }

        private static (XDocument InvoiceDocument, WellFormednessValidationResult WellFormednessResult) ReadAndParseInvoice(byte[] input)
        {
            try
            {
                using (var invoiceStream = new MemoryStream(input))
                {
                    var invoiceDocument = XDocument.Load(invoiceStream, LoadOptions.SetLineInfo);

                    return (invoiceDocument, new WellFormednessValidationResult());
                }
            }
            catch (Exception e) when (e is XmlException || e is IOException)
            {
                return (null, new WellFormednessValidationResult(e));
            }
        }

        private static ValidationResult CreateResult(ValidationContext validationContext)
        {
            var processingErrors = validationContext.ProcessingErrors;
            var status = DetermineStatus(validationContext);
            var isProcessingSuccessful = processingErrors.Count == 0;
            var schematronValidationResults = validationContext.SchematronValidations
                .Select(schematronValidation => schematronValidation.Result)
                .ToList();

            if (status == ValidationProcessStatus.ParsingInputXmlFailed)
            {
                return ValidationResult.ParsingInputXmlFailed(validationContext.WellFormednessValidationResult.Error,
                    validationContext.Report, validationContext.ReportInput, processingErrors, isProcessingSuccessful);
            }

            if (status == ValidationProcessStatus.NoScenarioMatched)
            {
                return ValidationResult.NoScenarioMatched(validationContext.Report, validationContext.ReportInput,
                    processingErrors, isProcessingSuccessful);
            }

            return new ValidationResult(status, validationContext.WellFormednessValidationResult,
                validationContext.SchemaValidationResult, schematronValidationResults, validationContext.IsAccepted,
                validationContext.Report, validationContext.ReportInput, processingErrors, isProcessingSuccessful);
        }

        private static ValidationProcessStatus DetermineStatus(ValidationContext validationContext)
        {
            if (!validationContext.WellFormednessValidationResult.IsValid)
            {
                return ValidationProcessStatus.ParsingInputXmlFailed;
            }

            if (validationContext.ProcessingErrors.Count > 0)
            {
                return ValidationProcessStatus.ProcessingFailed;
            }

            return validationContext.MatchingScenario is null
                ? ValidationProcessStatus.NoScenarioMatched
                : ValidationProcessStatus.Completed;
        }

        private static byte[] ComputeDocumentHash(byte[] input)
        {
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(input);
            }
        }
    }
}
