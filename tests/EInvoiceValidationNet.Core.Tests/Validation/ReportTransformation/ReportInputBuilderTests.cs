using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using EInvoiceValidationNet.Core.Configuration;
using EInvoiceValidationNet.Core.Validation;
using EInvoiceValidationNet.Core.Validation.ReportTransformation;
using EInvoiceValidationNet.Core.Validation.SchematronValidation;
using EInvoiceValidationNet.Core.Validation.SchemaValidation;

namespace EInvoiceValidationNet.Core.Tests.Validation.ReportTransformation
{
    using Configuration = EInvoiceValidationNet.Core.Configuration.Configuration;

    public class ReportInputBuilderTests
    {
        private static readonly XNamespace ReportInputNamespace = "http://www.xoev.de/de/validator/framework/1/createreportinput";

        private static readonly XNamespace ScenarioNamespace = "http://www.xoev.de/de/validator/framework/1/scenarios";

        private static readonly XNamespace SvrlNamespace = "http://purl.oclc.org/dsdl/svrl";

        private static readonly string FrameworkXsdPath = Path.Combine(
            AppContext.BaseDirectory, "Testdata", "ReportInputXsd", "createReportInput.xsd");

        [Test]
        public void Build_CompleteRun_CreatesXsdValidDocument()
        {
            var builder = new ReportInputBuilder(CreateCompleteContext());

            var document = builder.Build();

            var root = document.Root;
            Assert.Multiple(() =>
            {
                Assert.That(root?.Name, Is.EqualTo(ReportInputNamespace + "createReportInput"));
                Assert.That(root?.Attribute("frameworkVersion")?.Value, Is.EqualTo("1.0.2"));
                Assert.That(root?.Element(ReportInputNamespace + "engine")?.Element(ReportInputNamespace + "name")?.Value,
                    Is.Not.Empty);
                Assert.That(root?.Element(ReportInputNamespace + "timestamp")?.Value,
                    Does.Match(new Regex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$")));
                Assert.That(root?.Element(ReportInputNamespace + "documentIdentification")
                    ?.Element(ReportInputNamespace + "documentHash")
                    ?.Element(ReportInputNamespace + "hashAlgorithm")?.Value, Is.EqualTo("SHA-256"));
                Assert.That(root?.Element(ReportInputNamespace + "documentIdentification")
                    ?.Element(ReportInputNamespace + "documentHash")
                    ?.Element(ReportInputNamespace + "hashValue")?.Value,
                    Is.EqualTo(Convert.ToBase64String(CreateDocumentHash())));
                Assert.That(root?.Element(ReportInputNamespace + "documentIdentification")
                    ?.Element(ReportInputNamespace + "documentReference")?.Value, Is.EqualTo("invoice.xml"));
                Assert.That(root?.Element(ReportInputNamespace + "validationResultsWellformedness"), Is.Not.Null);
                Assert.That(root?.Element(ReportInputNamespace + "validationResultsXmlSchema"), Is.Not.Null);
                Assert.That(root?.Elements(ReportInputNamespace + "validationResultsSchematron").Count(), Is.EqualTo(2));
                Assert.That(root?.Element(ReportInputNamespace + "processingError"), Is.Null);
                Assert.That(ValidateAgainstFrameworkXsd(document).IsValid, Is.True);
            });
        }

        [Test]
        public void Build_CompleteRun_EmbedsVerbatimScenarioElementAndRawSvrl()
        {
            var scenarioElement = CreateVerbatimScenarioElement();
            var svrlDocument = CreateSvrlDocument();
            var builder = new ReportInputBuilder(CreateCompleteContext(scenarioElement, svrlDocument));

            var document = builder.Build();

            var root = document.Root;
            Assert.Multiple(() =>
            {
                Assert.That(XNode.DeepEquals(root?.Element(ScenarioNamespace + "scenario"), scenarioElement), Is.True);
                var firstSchematronResult = root?.Elements(ReportInputNamespace + "validationResultsSchematron").First();
                Assert.That(firstSchematronResult?.Element(ScenarioNamespace + "resource")
                    ?.Element(ScenarioNamespace + "name")?.Value, Is.EqualTo("Schematron rules for EN16931 (UBL)"));
                Assert.That(firstSchematronResult?.Element(ScenarioNamespace + "resource")
                    ?.Element(ScenarioNamespace + "location")?.Value, Is.EqualTo("EN16931-UBL-validation.xsl"));
                Assert.That(XNode.DeepEquals(
                    firstSchematronResult?.Element(ReportInputNamespace + "results")
                        ?.Element(SvrlNamespace + "schematron-output"), svrlDocument.Root), Is.True);
            });
        }

        [Test]
        public void Build_ParseFailure_CreatesXsdValidDocumentWithoutScenario()
        {
            var context = CreateContext();
            context.WellFormednessValidationResult = new WellFormednessValidationResult(new XmlException("Unclosed element"));

            var document = new ReportInputBuilder(context).Build();

            var wellformedness = document.Root?.Element(ReportInputNamespace + "validationResultsWellformedness");
            var syntaxError = wellformedness?.Element(ReportInputNamespace + "xmlSyntaxError");
            Assert.Multiple(() =>
            {
                Assert.That(document.Root?.Element(ScenarioNamespace + "scenario"), Is.Null);
                Assert.That(syntaxError?.Element(ReportInputNamespace + "message")?.Value, Does.Contain("Unclosed element"));
                Assert.That(syntaxError?.Element(ReportInputNamespace + "severityCode")?.Value,
                    Is.EqualTo("SEVERITY_FATAL_ERROR"));
                Assert.That(syntaxError?.Element(ReportInputNamespace + "rowNumber"), Is.Null);
                Assert.That(syntaxError?.Element(ReportInputNamespace + "columnNumber"), Is.Null);
                Assert.That(ValidateAgainstFrameworkXsd(document).IsValid, Is.True);
            });
        }

        [Test]
        public void Build_SchemaErrors_MapsSeverityRowAndColumnNumbers()
        {
            var context = CreateContext();
            context.MatchingScenario = CreateScenario();
            context.SchemaValidationResult = new SchemaValidationResult(false,
            [
                new XmlSyntaxError("cvc-datatype-valid.1.2.1: invalid value", XmlSyntaxErrorSeverity.Error, 42, 7),
                new XmlSyntaxError("schema hint", XmlSyntaxErrorSeverity.Warning, null, null)
            ]);

            var document = new ReportInputBuilder(context).Build();

            var schemaResults = document.Root?.Element(ReportInputNamespace + "validationResultsXmlSchema");
            var syntaxErrors = schemaResults?.Elements(ReportInputNamespace + "xmlSyntaxError").ToList();
            Assert.Multiple(() =>
            {
                Assert.That(syntaxErrors, Has.Count.EqualTo(2));
                Assert.That(syntaxErrors?[0].Element(ReportInputNamespace + "severityCode")?.Value, Is.EqualTo("SEVERITY_ERROR"));
                Assert.That(syntaxErrors?[0].Element(ReportInputNamespace + "rowNumber")?.Value, Is.EqualTo("42"));
                Assert.That(syntaxErrors?[0].Element(ReportInputNamespace + "columnNumber")?.Value, Is.EqualTo("7"));
                Assert.That(syntaxErrors?[1].Element(ReportInputNamespace + "severityCode")?.Value, Is.EqualTo("SEVERITY_WARNING"));
                Assert.That(syntaxErrors?[1].Element(ReportInputNamespace + "rowNumber"), Is.Null);
                Assert.That(syntaxErrors?[1].Element(ReportInputNamespace + "columnNumber"), Is.Null);
                Assert.That(ValidateAgainstFrameworkXsd(document).IsValid, Is.True);
            });
        }

        [Test]
        public void Build_ProcessingError_ContainsErrorMessages()
        {
            var context = CreateContext();
            context.AddProcessingError("Can not create final report: stylesheet not found");

            var document = new ReportInputBuilder(context).Build();

            var processingError = document.Root?.Element(ReportInputNamespace + "processingError");
            Assert.Multiple(() =>
            {
                string[] expectedErrors = ["Can not create final report: stylesheet not found"];
                Assert.That(processingError?.Elements(ReportInputNamespace + "error").Select(error => error.Value),
                    Is.EqualTo(expectedErrors));
                Assert.That(ValidateAgainstFrameworkXsd(document).IsValid, Is.True);
            });
        }

        [Test]
        public void Build_WithoutSchematronValidations_ContainsNoSchematronSection()
        {
            var document = new ReportInputBuilder(CreateContext()).Build();

            Assert.Multiple(() =>
            {
                Assert.That(document.Root?.Elements(ReportInputNamespace + "validationResultsSchematron"), Is.Empty);
                Assert.That(ValidateAgainstFrameworkXsd(document).IsValid, Is.True);
            });
        }

        [Test]
        public void Build_SchematronValidationWithMetadata_StripsNonFrameworkElements()
        {
            var svrlDocument = CreateSvrlDocumentWithMetadata();
            var context = CreateContext();
            context.MatchingScenario = CreateScenario();
            context.SchematronValidations.Add(new SchematronValidationOutcome(
                CreateSchematronValidationResult("Schematron rules for EN16931 (UBL)", "EN16931-UBL-validation.xsl",
                    svrlDocument),
                new SchematronValidationConfiguration("EN16931-UBL-validation.xsl",
                    "Schematron rules for EN16931 (UBL)")));

            var document = new ReportInputBuilder(context).Build();

            var embeddedOutput = document.Root
                ?.Elements(ReportInputNamespace + "validationResultsSchematron")
                .Single()
                .Element(ReportInputNamespace + "results")
                ?.Element(SvrlNamespace + "schematron-output");
            Assert.Multiple(() =>
            {
                Assert.That(embeddedOutput?.Elements(), Has.None.Matches<XElement>(element => element.Name.LocalName == "metadata"));
                Assert.That(embeddedOutput?.Elements(SvrlNamespace + "failed-assert"), Has.One.Items);
                Assert.That(XNode.DeepEquals(svrlDocument.Root, embeddedOutput), Is.False);
                Assert.That(ValidateAgainstFrameworkXsd(document).IsValid, Is.True);
            });
        }

        private static ValidationContext CreateContext(string documentReference = "invoice.xml")
        {
            return new ValidationContext(CreateConfiguration(), documentReference)
            {
                DocumentHash = CreateDocumentHash(),
                WellFormednessValidationResult = new WellFormednessValidationResult()
            };
        }

        private static Configuration CreateConfiguration()
        {
            return new Configuration([CreateScenario()]);
        }

        private static Scenario CreateScenario()
        {
            return new Scenario(
                new XPathMatchExpression("true()"),
                new SchemaValidationConfiguration("UBL-Invoice-2.1.xsd", "XML Schema for UBL 2.1 Invoice"),
                null,
                "Test scenario");
        }

        private static ValidationContext CreateCompleteContext(
            XElement? scenarioElement = null,
            XDocument? svrlDocument = null)
        {
            var context = CreateContext();
            var scenario = CreateScenario();
            if (scenarioElement != null)
            {
                scenario.RawScenarioElement = scenarioElement;
            }

            context.MatchingScenario = scenario;
            context.SchemaValidationResult =
                new SchemaValidationResult(true, []);
            context.SchematronValidations.Add(new SchematronValidationOutcome(
                CreateSchematronValidationResult("Schematron rules for EN16931 (UBL)", "EN16931-UBL-validation.xsl",
                    svrlDocument ?? CreateSvrlDocument()),
                new SchematronValidationConfiguration("EN16931-UBL-validation.xsl",
                    "Schematron rules for EN16931 (UBL)")));
            context.SchematronValidations.Add(new SchematronValidationOutcome(
                CreateSchematronValidationResult("Schematron rules for Invoice - CIUS XRechnung (UBL)",
                    "XRechnung-UBL-validation.xsl", CreateSvrlDocument()),
                new SchematronValidationConfiguration("XRechnung-UBL-validation.xsl",
                    "Schematron rules for Invoice - CIUS XRechnung (UBL)")));

            return context;
        }

        private static SchematronValidationResult CreateSchematronValidationResult(string name, string ruleIdPrefix, XDocument svrlDocument)
        {
            var assertions = svrlDocument.Root
                ?.Elements(SvrlNamespace + "failed-assert")
                .Select(failedAssert => new Assertion(
                    failedAssert.Attribute("id")?.Value,
                    AssertionSeverity.Fatal,
                    failedAssert.Element(SvrlNamespace + "text")?.Value,
                    failedAssert.Attribute("location")?.Value))
                .ToList();

            return new SchematronValidationResult(name, assertions, svrlDocument);
        }

        private static XElement CreateVerbatimScenarioElement()
        {
            return new XElement(ScenarioNamespace + "scenario",
                new XElement(ScenarioNamespace + "name", "EN16931 XRechnung (UBL Invoice)"),
                new XElement(ScenarioNamespace + "namespace",
                    new XAttribute("prefix", "invoice"),
                    "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"),
                new XElement(ScenarioNamespace + "match", "exists(/invoice:Invoice)"),
                new XElement(ScenarioNamespace + "validateWithXmlSchema",
                    new XElement(ScenarioNamespace + "resource",
                        new XElement(ScenarioNamespace + "name", "XML Schema for UBL 2.1 Invoice"),
                        new XElement(ScenarioNamespace + "location", "UBL-Invoice-2.1.xsd"))));
        }

        private static XDocument CreateSvrlDocument()
        {
            return new XDocument(
                new XElement(SvrlNamespace + "schematron-output",
                    new XAttribute("title", "EN16931 UBL"),
                    new XElement(SvrlNamespace + "ns-prefix-in-attribute-values",
                        new XAttribute("prefix", "cbc"),
                        new XAttribute("uri", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")),
                    new XElement(SvrlNamespace + "active-pattern", new XAttribute("id", "BR")),
                    new XElement(SvrlNamespace + "fired-rule",
                        new XAttribute("context", "ubl:Invoice")),
                    new XElement(SvrlNamespace + "failed-assert",
                        new XAttribute("id", "BR-CO-16"),
                        new XAttribute("flag", "fatal"),
                        new XAttribute("location", "/Q{urn:oasis:names:specification:ubl:schema:xsd:Invoice-2}Invoice[1]"),
                        new XAttribute("test", "cac:LegalMonetaryTotal/cbc:PayableAmount"),
                        new XElement(SvrlNamespace + "text",
                            "[BR-CO-16]-Amount due for payment (BT-115) ..."))));
        }

        private static XDocument CreateSvrlDocumentWithMetadata()
        {
            var svrlDocument = CreateSvrlDocument();
            svrlDocument.Root?.AddFirst(new XElement(SvrlNamespace + "metadata",
                new XElement(XNamespace.Get("http://purl.org/dc/terms/") + "creator", "EInvoiceValidationNet")));
            return svrlDocument;
        }

        private static byte[] CreateDocumentHash()
        {
            return SHA256.HashData([1, 2, 3]);
        }

        private static SchemaValidationResult ValidateAgainstFrameworkXsd(XDocument document)
        {
            return new SchemaValidator().Validate(document, FrameworkXsdPath);
        }
    }
}
