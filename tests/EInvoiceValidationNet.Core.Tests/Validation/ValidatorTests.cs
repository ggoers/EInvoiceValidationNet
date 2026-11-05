using System.Text;
using System.Xml;
using System.Xml.Linq;
using EInvoiceValidationNet.Core.Configuration;
using EInvoiceValidationNet.Core.Validation;
using EInvoiceValidationNet.Core.Validation.SchematronValidation;
using EInvoiceValidationNet.Core.Validation.SchemaValidation;

namespace EInvoiceValidationNet.Core.Tests.Validation
{
    using Configuration = EInvoiceValidationNet.Core.Configuration.Configuration;

    public class ValidatorTests
    {
        private static readonly XNamespace ReportInputNamespace = "http://www.xoev.de/de/validator/framework/1/createreportinput";

        [Test]
        public void Validate_InvalidXml_ShouldReturnParsingInputXmlFailed()
        {
            var validator = CreateValidator(out var xmlProcessing, out _, out _);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("<invoice><unclosed></invoice>"));

            var result = validator.Validate(stream, CreateConfiguration());

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(ValidationProcessStatus.ParsingInputXmlFailed));
                Assert.That(result.WellFormednessResult.IsValid, Is.False);
                Assert.That(result.WellFormednessResult.Error, Is.InstanceOf<XmlException>());
                Assert.That(result.SchemaValidationResult, Is.Null);
                Assert.That(result.SchematronValidationResults, Is.Empty);
                Assert.That(xmlProcessing.TransformCalls, Is.Empty);
            });
        }

        [Test]
        public void Validate_MissingFile_ShouldReturnParsingInputXmlFailed()
        {
            var validator = CreateValidator(out _, out _, out _);
            var missingFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".xml");

            var result = validator.Validate(missingFile, CreateConfiguration());

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(ValidationProcessStatus.ParsingInputXmlFailed));
                Assert.That(result.WellFormednessResult.IsValid, Is.False);
                Assert.That(result.WellFormednessResult.Error, Is.InstanceOf<IOException>());
                Assert.That(result.SchemaValidationResult, Is.Null);
                Assert.That(result.SchematronValidationResults, Is.Empty);
            });
        }

        [Test]
        public void Validate_MatchingScenario_CreatesReportAndPassesInvoiceAsInputDocument()
        {
            var validator = CreateValidator(out var xmlProcessing, out var schemaValidator, out _);
            var configuration = CreateConfiguration();
            schemaValidator.ResultFactory = _ => CreateValidSchemaResult();

            var result = validator.Validate(CreateInvoiceStream(), configuration, "invoice.xml");

            var transformCall = xmlProcessing.TransformCalls.Single();
            Assert.Multiple(() =>
            {
                Assert.That(transformCall.XsltPath, Is.EqualTo("report.xsl"));
                Assert.That(transformCall.Input.Root?.Name,
                    Is.EqualTo(ReportInputNamespace + "createReportInput"));
                Assert.That(transformCall.Parameters?["input-document"].Root?.Name.LocalName, Is.EqualTo("Invoice"));
                Assert.That(result.Report, Is.Not.Null);
                Assert.That(result.ReportInput.Root?.Name, Is.EqualTo(ReportInputNamespace + "createReportInput"));
                Assert.That(result.Status, Is.EqualTo(ValidationProcessStatus.Completed));
                Assert.That(result.IsProcessingSuccessful, Is.True);
                Assert.That(result.ProcessingErrors, Is.Empty);
                Assert.That(result.IsAccepted, Is.True);
            });
        }

        [TestCase(true, true)]
        [TestCase(false, false)]
        public void Validate_AcceptMatchEvaluatedAgainstReport_DeterminesAcceptance(bool acceptMatchResult, bool expectedAcceptance)
        {
            var validator = CreateValidator(out var xmlProcessing, out var schemaValidator, out _);
            var configuration = CreateConfiguration(acceptMatchExpression: new XPathMatchExpression("/rep:report/rep:accept"));
            schemaValidator.ResultFactory = _ => CreateValidSchemaResult();
            xmlProcessing.XPathBooleanEvaluator = (expression, _) =>
                expression == "true()" || acceptMatchResult;

            var result = validator.Validate(CreateInvoiceStream(), configuration, "invoice.xml");

            var evaluation = xmlProcessing.XPathEvaluations
                .Single(xPathEvaluation => xPathEvaluation.expression == "/rep:report/rep:accept");
            Assert.Multiple(() =>
            {
                Assert.That(evaluation.expression, Is.EqualTo("/rep:report/rep:accept"));
                Assert.That(ReferenceEquals(evaluation.document, result.Report), Is.True);
                Assert.That(result.IsAccepted, Is.EqualTo(expectedAcceptance));
            });
        }

        [Test]
        public void Validate_SchemaValidationFailure_SkipsSchematronAndRejectsAcceptance()
        {
            var validator = CreateValidator(out _, out var schemaValidator, out var schematronValidator);
            var configuration = CreateConfiguration(
                schemaValidation: new SchemaValidationConfiguration("schema.xsd", "Schema"),
                schematronValidation: new SchematronValidationConfiguration("rules.xsl", "Rules"));
            schemaValidator.ResultFactory = xsdPath => xsdPath.EndsWith("createReportInput.xsd")
                ? CreateValidSchemaResult()
                : new SchemaValidationResult(false,
                    [new XmlSyntaxError("invalid", XmlSyntaxErrorSeverity.Error, 1, 1)]);

            var result = validator.Validate(CreateInvoiceStream(), configuration, "invoice.xml");

            Assert.Multiple(() =>
            {
                Assert.That(schematronValidator.ValidatedConfigurations, Is.Empty);
                Assert.That(result.SchematronValidationResults, Is.Empty);
                Assert.That(result.IsAccepted, Is.False);
                Assert.That(result.Report, Is.Not.Null);
                Assert.That(result.Status, Is.EqualTo(ValidationProcessStatus.Completed));
            });
        }

        [Test]
        public void Validate_ReportTransformationFailure_ShouldResultInProcessingFailed()
        {
            var validator = CreateValidator(out var xmlProcessing, out var schemaValidator, out _);
            var configuration = CreateConfiguration();
            schemaValidator.ResultFactory = _ => CreateValidSchemaResult();
            xmlProcessing.TransformException = new InvalidOperationException("stylesheet is not valid");

            var result = validator.Validate(CreateInvoiceStream(), configuration, "invoice.xml");

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(ValidationProcessStatus.ProcessingFailed));
                Assert.That(result.IsProcessingSuccessful, Is.False);
                Assert.That(result.ProcessingErrors, Is.Not.Empty);
                Assert.That(result.ProcessingErrors.Single(), Does.Contain("stylesheet is not valid"));
                Assert.That(result.Report, Is.Null);
                Assert.That(result.IsAccepted, Is.Null);
            });
        }

        [Test]
        public void Validate_RuleCustomization_OverridesEffectiveSeverityOfAssertions()
        {
            var validator = CreateValidator(out _, out var schemaValidator, out var schematronValidator);
            var configuration = CreateConfiguration(
                schematronValidation: new SchematronValidationConfiguration("rules.xsl", "Rules"),
                ruleCustomizations: [new RuleCustomization("BR-CL-23", Level.Warning)]);
            schemaValidator.ResultFactory = _ => CreateValidSchemaResult();
            schematronValidator.ResultFactory = _ => CreateSchematronResult(
                new Assertion("BR-CL-23", AssertionSeverity.Error, "customized rule failed", "/Invoice[1]"),
                new Assertion("BR-CO-16", AssertionSeverity.Fatal, "other rule failed", "/Invoice[1]"));

            var result = validator.Validate(CreateInvoiceStream(), configuration, "invoice.xml");

            var assertions = result.SchematronValidationResults.SelectMany(schematronResult => schematronResult.Assertions).ToList();
            Assert.Multiple(() =>
            {
                Assert.That(assertions.Single(assertion => assertion.RuleId == "BR-CL-23").Severity,
                    Is.EqualTo(AssertionSeverity.Error));
                Assert.That(assertions.Single(assertion => assertion.RuleId == "BR-CL-23").EffectiveSeverity,
                    Is.EqualTo(AssertionSeverity.Warning));
                Assert.That(assertions.Single(assertion => assertion.RuleId == "BR-CO-16").EffectiveSeverity,
                    Is.EqualTo(AssertionSeverity.Fatal));
            });
        }

        [Test]
        public void Validate_MalformedInput_UsesNoScenarioReportWithErrorMessageDocument()
        {
            var validator = CreateValidator(out var xmlProcessing, out _, out _);
            var configuration = CreateConfiguration(noScenarioReport: new ReportTransformationConfiguration("fallback.xsl", "Fallback", null));
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("<invoice><unclosed></invoice>"));

            var result = validator.Validate(stream, configuration, "invoice.xml");

            var transformCall = xmlProcessing.TransformCalls.Single();
            var errorMessage = transformCall.Parameters?["input-document"];
            Assert.Multiple(() =>
            {
                Assert.That(transformCall.XsltPath, Is.EqualTo("fallback.xsl"));
                Assert.That(errorMessage?.Root?.Name, Is.EqualTo(ReportInputNamespace + "error-message"));
                Assert.That(errorMessage?.Root?.Value, Is.Not.Empty);
                Assert.That(result.Status, Is.EqualTo(ValidationProcessStatus.ParsingInputXmlFailed));
                Assert.That(result.IsProcessingSuccessful, Is.False);
                Assert.That(result.ProcessingErrors, Is.Not.Empty);
                Assert.That(result.IsAccepted, Is.False);
                Assert.That(result.Report, Is.Not.Null);
            });
        }

        [Test]
        public void Validate_NoScenarioMatched_UsesNoScenarioReportWithInvoiceAsInputDocument()
        {
            var validator = CreateValidator(out var xmlProcessing, out _, out _);
            var configuration = new Configuration(
                [
                    CreateScenario(matchExpression: new XPathMatchExpression("false()")),
                    CreateScenario(matchExpression: new XPathMatchExpression("false()"))
                ],
                new ReportTransformationConfiguration("fallback.xsl", "Fallback", null));

            var result = validator.Validate(CreateInvoiceStream(), configuration, "invoice.xml");

            var transformCall = xmlProcessing.TransformCalls.Single();
            Assert.Multiple(() =>
            {
                Assert.That(transformCall.XsltPath, Is.EqualTo("fallback.xsl"));
                Assert.That(transformCall.Parameters?["input-document"].Root?.Name.LocalName, Is.EqualTo("Invoice"));
                Assert.That(result.Status, Is.EqualTo(ValidationProcessStatus.NoScenarioMatched));
                Assert.That(result.IsProcessingSuccessful, Is.True);
                Assert.That(result.ProcessingErrors, Is.Empty);
                Assert.That(result.IsAccepted, Is.False);
                Assert.That(result.Report, Is.Not.Null);
            });
        }

        [Test]
        public void Validate_SingleScenarioWithNonMatchingExpression_ReturnsNoScenarioMatched()
        {
            var validator = CreateValidator(out var xmlProcessing, out var schemaValidator, out _);
            var configuration = CreateConfiguration(matchExpression: new XPathMatchExpression("false()"));

            var result = validator.Validate(CreateInvoiceStream(), configuration, "invoice.xml");

            Assert.Multiple(() =>
            {
                Assert.That(xmlProcessing.XPathEvaluations.Single().expression, Is.EqualTo("false()"));
                Assert.That(schemaValidator.Validations, Is.Empty);
                Assert.That(result.Status, Is.EqualTo(ValidationProcessStatus.NoScenarioMatched));
                Assert.That(result.Report, Is.Null);
                Assert.That(result.ReportInput, Is.Not.Null);
                Assert.That(result.IsAccepted, Is.False);
            });
        }

        [Test]
        public void Validate_ComputesDocumentHashAndReference()
        {
            var validator = CreateValidator(out _, out var schemaValidator, out _);
            schemaValidator.ResultFactory = _ => CreateValidSchemaResult();
            var invoiceBytes = Encoding.UTF8.GetBytes("<Invoice/>");
            var expectedHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(invoiceBytes));

            using (var stream = new MemoryStream(invoiceBytes))
            {
                var result = validator.Validate(stream, CreateConfiguration());

                var hashElement = result.ReportInput.Root
                    ?.Element(ReportInputNamespace + "documentIdentification")
                    ?.Element(ReportInputNamespace + "documentHash");
                Assert.Multiple(() =>
                {
                    Assert.That(hashElement?.Element(ReportInputNamespace + "hashAlgorithm")?.Value, Is.EqualTo("SHA-256"));
                    Assert.That(hashElement?.Element(ReportInputNamespace + "hashValue")?.Value, Is.EqualTo(expectedHash));
                    Assert.That(result.ReportInput.Root
                        ?.Element(ReportInputNamespace + "documentIdentification")
                        ?.Element(ReportInputNamespace + "documentReference")?.Value, Is.Empty);
                });
            }

            using (var stream = new MemoryStream(invoiceBytes))
            {
                var result = validator.Validate(stream, CreateConfiguration(), "custom-reference.xml");

                Assert.That(result.ReportInput.Root
                    ?.Element(ReportInputNamespace + "documentIdentification")
                    ?.Element(ReportInputNamespace + "documentReference")?.Value, Is.EqualTo("custom-reference.xml"));
            }
        }

        [Test]
        public void Validate_WithOutputDirectory_WritesReportFile()
        {
            var validator = CreateValidator(out _, out var schemaValidator, out _);
            schemaValidator.ResultFactory = _ => CreateValidSchemaResult();
            var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var configuration = CreateConfiguration(
                reportTransformation: new ReportTransformationConfiguration("report.xsl", "Report", outputDirectory));

            try
            {
                var result = validator.Validate(CreateInvoiceStream(), configuration, "invoice.xml");

                var reportFile = Path.Combine(outputDirectory, "invoice-report.xml");
                Assert.Multiple(() =>
                {
                    Assert.That(File.Exists(reportFile), Is.True);
                    Assert.That(XDocument.Load(reportFile).Root?.Name, Is.EqualTo(result.Report?.Root?.Name));
                });
            }
            finally
            {
                Directory.Delete(outputDirectory, true);
            }
        }

        [Test]
        public void Validate_WithXPathAndSchemaScenario_ValidatesIntermediateAgainstFrameworkSchema()
        {
            var validator = CreateValidator(out _, out var schemaValidator, out _);
            schemaValidator.ResultFactory = _ => CreateValidSchemaResult();

            validator.Validate(CreateInvoiceStream(), CreateConfiguration(), "invoice.xml");

            Assert.That(schemaValidator.Validations,
                Has.One.Matches<(XDocument document, string xsdPath)>(validation =>
                    validation.xsdPath.EndsWith("createReportInput.xsd")));
        }

        [Test]
        public void Validate_WithPredicateScenarioWithoutSchemaValidation_SkipsIntermediateSchemaValidation()
        {
            var validator = CreateValidator(out _, out var schemaValidator, out var schematronValidator);
            var configuration = CreateConfiguration(
                matchExpression: new PredicateMatchExpression(_ => true),
                schemaValidation: null,
                schematronValidation: new SchematronValidationConfiguration("rules.xsl", "Rules"));
            schematronValidator.ResultFactory = _ => CreateSchematronResult();

            var result = validator.Validate(CreateInvoiceStream(), configuration, "invoice.xml");

            Assert.Multiple(() =>
            {
                Assert.That(schemaValidator.Validations, Is.Empty);
                Assert.That(result.SchematronValidationResults, Has.One.Items);
                Assert.That(result.Status, Is.EqualTo(ValidationProcessStatus.Completed));
                Assert.That(result.Report, Is.Not.Null);
            });
        }

        [Test]
        public void Validate_IntermediateSchemaValidationFailure_ShouldResultInProcessingFailedWithReport()
        {
            var validator = CreateValidator(out var xmlProcessing, out var schemaValidator, out _);
            schemaValidator.ResultFactory = xsdPath => xsdPath.EndsWith("createReportInput.xsd")
                ? new SchemaValidationResult(false, [new XmlSyntaxError("invalid report input", XmlSyntaxErrorSeverity.Error, null, null)])
                : CreateValidSchemaResult();

            var result = validator.Validate(CreateInvoiceStream(), CreateConfiguration(), "invoice.xml");

            var transformCall = xmlProcessing.TransformCalls.Single();
            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(ValidationProcessStatus.ProcessingFailed));
                Assert.That(result.IsProcessingSuccessful, Is.False);
                Assert.That(result.ProcessingErrors, Is.Not.Empty);
                Assert.That(result.IsAccepted, Is.Null);
                Assert.That(result.Report, Is.Not.Null);
                Assert.That(transformCall.Input.Root?.Element(ReportInputNamespace + "processingError"), Is.Not.Null);
            });
        }

        private static MemoryStream CreateInvoiceStream()
        {
            return new MemoryStream(Encoding.UTF8.GetBytes("<Invoice/>"));
        }

        private static Configuration CreateConfiguration(
            MatchExpression? matchExpression = null,
            SchemaValidationConfiguration? schemaValidation = null,
            SchematronValidationConfiguration? schematronValidation = null,
            ReportTransformationConfiguration? reportTransformation = null,
            ReportTransformationConfiguration? noScenarioReport = null,
            MatchExpression? acceptMatchExpression = null,
            IEnumerable<RuleCustomization>? ruleCustomizations = null)
        {
            return new Configuration(
                [CreateScenario(matchExpression, schemaValidation, schematronValidation, reportTransformation, acceptMatchExpression, ruleCustomizations)],
                noScenarioReport);
        }

        private static Scenario CreateScenario(
            MatchExpression? matchExpression = null,
            SchemaValidationConfiguration? schemaValidation = null,
            SchematronValidationConfiguration? schematronValidation = null,
            ReportTransformationConfiguration? reportTransformation = null,
            MatchExpression? acceptMatchExpression = null,
            IEnumerable<RuleCustomization>? ruleCustomizations = null)
        {
            if (schemaValidation is null && schematronValidation is null)
            {
                schemaValidation = new SchemaValidationConfiguration("schema.xsd", "Schema");
            }

            IEnumerable<SchematronValidationConfiguration> schematronValidations = schematronValidation is null
                ? []
                : [schematronValidation];

            return new Scenario(
                matchExpression ?? new XPathMatchExpression("true()"),
                schemaValidation,
                schematronValidations,
                "Test scenario",
                reportTransformation ?? new ReportTransformationConfiguration("report.xsl", "Report", null),
                ruleCustomizations,
                acceptMatchExpression,
                null);
        }

        private static SchemaValidationResult CreateValidSchemaResult()
        {
            return new SchemaValidationResult(true, []);
        }

        private static SchematronValidationResult CreateSchematronResult(params Assertion[] assertions)
        {
            var svrlDocument = new XDocument(new XElement("schematron-output"));
            return new SchematronValidationResult("Rules", assertions, svrlDocument);
        }

        private static Validator CreateValidator(
            out FakeXmlProcessing xmlProcessing,
            out FakeSchemaValidator schemaValidator,
            out FakeSchematronValidator schematronValidator)
        {
            xmlProcessing = new FakeXmlProcessing();
            schemaValidator = new FakeSchemaValidator();
            schematronValidator = new FakeSchematronValidator();

            return new Validator(xmlProcessing, schemaValidator, schematronValidator);
        }

        private sealed class FakeXmlProcessing : IXmlProcessing
        {
            public List<TransformCall> TransformCalls { get; } = new();

            public List<(string expression, XDocument document)> XPathEvaluations { get; } = new();

            public XDocument TransformResult { get; set; } = new XDocument(new XElement("report"));

            public Exception? TransformException { get; set; }

            public Func<string, XDocument, bool>? XPathBooleanEvaluator { get; set; }

            public bool EvaluateXPathBoolean(XDocument xmlDocument, string expression,
                IEnumerable<KeyValuePair<string, string>>? namespaces = null)
            {
                XPathEvaluations.Add((expression, xmlDocument));
                return XPathBooleanEvaluator?.Invoke(expression, xmlDocument) ?? expression == "true()";
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

        private sealed class FakeSchematronValidator : ISchematronValidator
        {
            public List<SchematronValidationConfiguration> ValidatedConfigurations { get; } = new();

            public Func<SchematronValidationConfiguration, SchematronValidationResult>? ResultFactory { get; set; }

            public SchematronValidationResult Validate(XDocument invoiceXml, SchematronValidationConfiguration configuration)
            {
                ValidatedConfigurations.Add(configuration);
                return ResultFactory?.Invoke(configuration)
                    ?? new SchematronValidationResult(configuration.Name, [],
                        new XDocument(new XElement("schematron-output")));
            }
        }
    }
}
