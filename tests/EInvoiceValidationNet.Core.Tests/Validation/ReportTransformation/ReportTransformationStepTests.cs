using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using EInvoiceValidationNet.Core.Configuration;
using EInvoiceValidationNet.Core.Validation;
using EInvoiceValidationNet.Core.Validation.ReportTransformation;
using EInvoiceValidationNet.Core.Validation.SchemaValidation;

namespace EInvoiceValidationNet.Core.Tests.Validation.ReportTransformation
{
    using Configuration = EInvoiceValidationNet.Core.Configuration.Configuration;

    public class ReportTransformationStepTests
    {
        private static readonly XNamespace ReportInputNamespace =
            "http://www.xoev.de/de/validator/framework/1/createreportinput";

        [Test]
        public void Execute_ScenarioTransformation_BuildsReportInputAndTransforms()
        {
            var xmlProcessing = new FakeXmlProcessing();
            var schemaValidator = new FakeSchemaValidator();
            var scenario = CreateScenario(new ReportTransformationConfiguration("report.xsl", "Report", null));
            var context = CreateContext(scenario);

            new ReportTransformationStep(xmlProcessing, schemaValidator).Execute(context);

            var transformCall = xmlProcessing.TransformCalls.Single();
            Assert.Multiple(() =>
            {
                Assert.That(transformCall.XsltPath, Is.EqualTo("report.xsl"));
                Assert.That(transformCall.Input.Root?.Name,
                    Is.EqualTo(ReportInputNamespace + "createReportInput"));
                Assert.That(transformCall.Parameters?["input-document"].Root?.Name.LocalName, Is.EqualTo("Invoice"));
                Assert.That(context.Report, Is.Not.Null);
                Assert.That(context.ReportInput.Root?.Name,
                    Is.EqualTo(ReportInputNamespace + "createReportInput"));
                Assert.That(context.ProcessingErrors, Is.Empty);
                Assert.That(schemaValidator.Validations.Single().xsdPath, Does.EndWith("createReportInput.xsd"));
            });
        }

        [Test]
        public void Execute_NoScenarioTransformation_UsesNoScenarioReport()
        {
            var xmlProcessing = new FakeXmlProcessing();
            var scenario = CreateScenario(null);
            var context = CreateContext(scenario,
                new ReportTransformationConfiguration("fallback.xsl", "Fallback", null));

            new ReportTransformationStep(xmlProcessing, new FakeSchemaValidator()).Execute(context);

            Assert.That(xmlProcessing.TransformCalls.Single().XsltPath, Is.EqualTo("fallback.xsl"));
        }

        [Test]
        public void Execute_NoReportTransformationConfigured_OnlyBuildsReportInput()
        {
            var xmlProcessing = new FakeXmlProcessing();
            var context = CreateContext(CreateScenario(null));

            new ReportTransformationStep(xmlProcessing, new FakeSchemaValidator()).Execute(context);

            Assert.Multiple(() =>
            {
                Assert.That(xmlProcessing.TransformCalls, Is.Empty);
                Assert.That(context.Report, Is.Null);
                Assert.That(context.ReportInput.Root?.Name,
                    Is.EqualTo(ReportInputNamespace + "createReportInput"));
                Assert.That(context.ProcessingErrors, Is.Empty);
            });
        }

        [Test]
        public void Execute_ParseFailure_UsesErrorMessageDocument()
        {
            var xmlProcessing = new FakeXmlProcessing();
            var context = CreateContext(null,
                new ReportTransformationConfiguration("fallback.xsl", "Fallback", null));
            context.InvoiceDocument = null;
            context.WellFormednessValidationResult =
                new WellFormednessValidationResult(new XmlException("Unclosed element"));

            new ReportTransformationStep(xmlProcessing, new FakeSchemaValidator()).Execute(context);

            var inputDocument = xmlProcessing.TransformCalls.Single().Parameters?["input-document"];
            Assert.Multiple(() =>
            {
                Assert.That(inputDocument?.Root?.Name, Is.EqualTo(ReportInputNamespace + "error-message"));
                Assert.That(inputDocument?.Root?.Value, Does.Contain("Unclosed element"));
            });
        }

        [Test]
        public void Execute_FrameworkSchemaValidationFailure_AddsProcessingErrorsAndRebuildsInput()
        {
            var xmlProcessing = new FakeXmlProcessing();
            var schemaValidator = new FakeSchemaValidator
            {
                ResultFactory = xsdPath => xsdPath.EndsWith("createReportInput.xsd")
                    ? new SchemaValidationResult(false,
                        [new XmlSyntaxError("invalid report input", XmlSyntaxErrorSeverity.Error, null, null)])
                    : new SchemaValidationResult(true, [])
            };
            var context = CreateContext(CreateScenario(new ReportTransformationConfiguration("report.xsl", "Report", null)));

            new ReportTransformationStep(xmlProcessing, schemaValidator).Execute(context);

            var transformCall = xmlProcessing.TransformCalls.Single();
            Assert.Multiple(() =>
            {
                Assert.That(context.ProcessingErrors.Single(),
                    Does.Contain("Report input has errors: invalid report input"));
                Assert.That(transformCall.Input.Root?.Element(ReportInputNamespace + "processingError"), Is.Not.Null);
                Assert.That(context.Report, Is.Not.Null);
            });
        }

        [Test]
        public void Execute_TransformationFailure_AddsProcessingErrorAndKeepsReportInput()
        {
            var xmlProcessing = new FakeXmlProcessing
            {
                TransformException = new InvalidOperationException("stylesheet is not valid")
            };
            var context = CreateContext(CreateScenario(new ReportTransformationConfiguration("report.xsl", "Report", null)));

            new ReportTransformationStep(xmlProcessing, new FakeSchemaValidator()).Execute(context);

            Assert.Multiple(() =>
            {
                Assert.That(context.Report, Is.Null);
                Assert.That(context.ProcessingErrors.Single(), Does.Contain("stylesheet is not valid"));
                Assert.That(context.ReportInput.Root?.Element(ReportInputNamespace + "processingError"), Is.Not.Null);
            });
        }

        [Test]
        public void Execute_WithOutputDirectory_WritesReportFile()
        {
            var xmlProcessing = new FakeXmlProcessing();
            var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var scenario = CreateScenario(
                new ReportTransformationConfiguration("report.xsl", "Report", outputDirectory));
            var context = CreateContext(scenario);

            try
            {
                new ReportTransformationStep(xmlProcessing, new FakeSchemaValidator()).Execute(context);

                var reportFile = Path.Combine(outputDirectory, "invoice-report.xml");
                Assert.Multiple(() =>
                {
                    Assert.That(File.Exists(reportFile), Is.True);
                    Assert.That(XDocument.Load(reportFile).Root?.Name, Is.EqualTo(context.Report?.Root?.Name));
                });
            }
            finally
            {
                if (Directory.Exists(outputDirectory))
                {
                    Directory.Delete(outputDirectory, true);
                }
            }
        }

        private static ValidationContext CreateContext(Scenario? scenario,
            ReportTransformationConfiguration? noScenarioReport = null)
        {
            var configuration = new Configuration([CreateScenario(null)], noScenarioReport);
            return new ValidationContext(configuration, "invoice.xml")
            {
                DocumentHash = [1, 2, 3],
                InvoiceDocument = new XDocument(new XElement("Invoice")),
                WellFormednessValidationResult = new WellFormednessValidationResult(),
                MatchingScenario = scenario
            };
        }

        private static Scenario CreateScenario(ReportTransformationConfiguration? reportTransformation)
        {
            return new Scenario(
                new XPathMatchExpression("true()"),
                new SchemaValidationConfiguration("schema.xsd", "Schema"),
                null,
                "Test scenario",
                reportTransformation);
        }

        private sealed class FakeXmlProcessing : IXmlProcessing
        {
            public List<TransformCall> TransformCalls { get; } = new();

            public XDocument TransformResult { get; set; } = new XDocument(new XElement("report"));

            public Exception? TransformException { get; set; }

            public bool EvaluateXPathBoolean(XDocument xmlDocument, string expression,
                IEnumerable<KeyValuePair<string, string>>? namespaces = null)
            {
                throw new NotSupportedException();
            }

            public XDocument Transform(XDocument inputDocument, string xsltPath,
                IReadOnlyDictionary<string, XDocument>? parameters = null)
            {
                if (TransformException != null)
                {
                    throw TransformException;
                }

                TransformCalls.Add(new TransformCall(inputDocument, xsltPath, parameters));
                return TransformResult;
            }
        }

        private sealed record TransformCall(XDocument Input, string XsltPath,
            IReadOnlyDictionary<string, XDocument>? Parameters);

        private sealed class FakeSchemaValidator : ISchemaValidator
        {
            public List<(XDocument document, string xsdPath)> Validations { get; } = new();

            public Func<string, SchemaValidationResult>? ResultFactory { get; set; }

            public SchemaValidationResult Validate(XDocument invoiceXml, string xsdFilePath)
            {
                Validations.Add((invoiceXml, xsdFilePath));
                return ResultFactory?.Invoke(xsdFilePath)
                    ?? new SchemaValidationResult(true, []);
            }
        }
    }
}
