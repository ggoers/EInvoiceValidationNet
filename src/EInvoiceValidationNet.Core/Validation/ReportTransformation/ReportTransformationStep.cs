using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using EInvoiceValidationNet.Configuration;
using EInvoiceValidationNet.Validation.SchemaValidation;

namespace EInvoiceValidationNet.Validation.ReportTransformation
{
    internal sealed class ReportTransformationStep : IValidationStep
    {
        private const string InputDocumentParameterName = "input-document";

        private static readonly XNamespace ReportInputNamespace = XNamespace.Get(FrameworkXml.ReportInputNamespace);

        private readonly IXmlProcessing _xmlProcessing;

        private readonly ISchemaValidator _schemaValidator;

        public ReportTransformationStep(IXmlProcessing xmlProcessing, ISchemaValidator schemaValidator)
        {
            _xmlProcessing = xmlProcessing ?? throw new ArgumentNullException(nameof(xmlProcessing));
            _schemaValidator = schemaValidator ?? throw new ArgumentNullException(nameof(schemaValidator));
        }

        public void Execute(ValidationContext validationContext)
        {
            var reportTransformation = validationContext.MatchingScenario?.ReportTransformation
                ?? validationContext.Configuration.NoScenarioReport;

            validationContext.ReportInput = BuildReportInput(validationContext);

            if (reportTransformation is null)
            {
                return;
            }

            if (CanBeValidatedAgainstFrameworkSchema(validationContext.MatchingScenario))
            {
                var frameworkCheck = _schemaValidator.Validate(validationContext.ReportInput, ReportInputSchema.XsdFilePath);
                if (!frameworkCheck.IsValid)
                {
                    foreach (var error in frameworkCheck.Errors)
                    {
                        validationContext.AddProcessingError($"Report input has errors: {error.Message}");
                    }

                    validationContext.ReportInput = BuildReportInput(validationContext);
                }
            }

            var inputDocument = validationContext.WellFormednessValidationResult.IsValid
                ? validationContext.InvoiceDocument
                : CreateErrorMessageDocument(validationContext.WellFormednessValidationResult);
            var parameters = new Dictionary<string, XDocument> { { InputDocumentParameterName, inputDocument } };

            try
            {
                validationContext.Report = _xmlProcessing.Transform(
                    validationContext.ReportInput, reportTransformation.XsltFilePath, parameters);
            }
            catch (Exception e)
            {
                validationContext.AddProcessingError($"Can not create final report: {e.Message}");
                validationContext.ReportInput = BuildReportInput(validationContext);

                return;
            }

            WriteReportFile(validationContext.Report, reportTransformation, validationContext.DocumentReference);
        }

        private static XDocument BuildReportInput(ValidationContext validationContext)
        {
            return new ReportInputBuilder(validationContext).Build();
        }

        private static bool CanBeValidatedAgainstFrameworkSchema(Scenario scenario)
        {
            if (scenario is null)
            {
                return false;
            }

            return scenario.RawScenarioElement != null
                || (scenario.MatchExpression is XPathMatchExpression && scenario.SchemaValidation != null);
        }

        private static XDocument CreateErrorMessageDocument(WellFormednessValidationResult wellFormednessResult)
        {
            return new XDocument(new XElement(ReportInputNamespace + "error-message",
                wellFormednessResult.Error.Message));
        }

        private static void WriteReportFile(XDocument report, ReportTransformationConfiguration reportTransformation,
            string documentReference)
        {
            if (reportTransformation.OutputDirectory is null)
            {
                return;
            }

            Directory.CreateDirectory(reportTransformation.OutputDirectory);
            var documentName = Path.GetFileNameWithoutExtension(documentReference);
            var reportFileName = string.IsNullOrEmpty(documentName) ? "report.xml" : documentName + "-report.xml";
            report.Save(Path.Combine(reportTransformation.OutputDirectory, reportFileName));
        }
    }
}
