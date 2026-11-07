using EInvoiceValidationNet.Validation;
using EInvoiceValidationNet.Validation.SchematronValidation;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace EInvoiceValidationNet.IntegrationTests;

/// <summary>
/// validation integrations tests (complete validation, no mocked behavior)
/// the goal of this test is not to test every possible business rule from a configuration -> instead we want to perform
/// end-to-end validation using "real" invoices and a real configuration covering the main outcomes:
/// - successful validation
/// - schema validation failure
/// - schematron validation failure
/// - invalid xml in invoice (parsing error)
/// - no scenario matched
/// each outcome must also produce the expected validation report
/// </summary>
public class ValidationTests : IntegrationTestBase
{
    private static readonly XNamespace VarlNamespace = "http://www.xoev.de/de/validator/varl/1";

    private static readonly XNamespace ScenarioNamespace = "http://www.xoev.de/de/validator/framework/1/scenarios";

    private static readonly XNamespace ReportInputNamespace =
        "http://www.xoev.de/de/validator/framework/1/createreportinput";

    private IValidator _validator;

    [SetUp]
    public void Setup()
    {
        var validatorFactory = new ValidatorFactory();
        _validator = validatorFactory.CreateValidator();
    }

    [Test]
    public void Validate_UBL_Success()
    {
        var result = ValidateInvoice(_validator, "ubl_success.xml");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ValidationProcessStatus.Completed));
            Assert.That(result.SchemaValidationResult.IsValid, Is.True);
            Assert.That(result.SchemaValidationResult.Errors, Is.Empty);
            Assert.That(GetAssertions(result), Is.Empty);
            Assert.That(
                result.SchematronValidationResults.Select(schematronValidation => schematronValidation.Name),
                Is.EquivalentTo(["Schematron rules for EN16931 (UBL)", "Schematron rules for Invoice - CIUS XRechnung (UBL)"]));

            Assert.That(result.Report, Is.Not.Null);
            AssertReportIsValid(result, true);
            Assert.That(GetReportMessages(result), Is.Empty);
            Assert.That(GetAssessment(result)?.Element(VarlNamespace + "accept"), Is.Not.Null);
            AssertReportContainsScenario(result, "EN16931 XRechnung (UBL Invoice)");
            Assert.That(GetScenarioElement(result)?.Element(ScenarioNamespace + "match")?.Value,
                Does.Contain("CustomizationID"));
            Assert.That(GetScenarioElement(result)?.Element(ScenarioNamespace + "createReport")
                    ?.Elements(ScenarioNamespace + "customLevel")
                    .Single(customLevel => customLevel.Value == "BR-CL-23").Attribute("level")?.Value,
                Is.EqualTo("warning"));
            var documentData = result.Report.Root?.Element(VarlNamespace + "scenarioMatched")
                ?.Element(VarlNamespace + "documentData");
            Assert.That(documentData?.Element("seller")?.Value, Is.EqualTo("[Seller name]"));
            Assert.That(documentData?.Element("id")?.Value, Is.EqualTo("123456XX"));
            Assert.That(documentData?.Element("issueDate")?.Value, Is.EqualTo("2016-04-04"));

            Assert.That(result.IsProcessingSuccessful, Is.True);
            Assert.That(result.ProcessingErrors, Is.Empty);
            Assert.That(result.IsAccepted, Is.True);
            AssertReportInputIsValid(result, "ubl_success.xml");
        });
    }

    [Test]
    public void Validate_CII_Success()
    {
        var result = ValidateInvoice(_validator, "cii_success.xml");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ValidationProcessStatus.Completed));
            Assert.That(result.SchemaValidationResult.IsValid, Is.True);
            Assert.That(result.SchemaValidationResult.Errors, Is.Empty);
            Assert.That(GetAssertions(result), Is.Empty);
            Assert.That(
                result.SchematronValidationResults.Select(schematronValidation => schematronValidation.Name),
                Is.EquivalentTo(["Schematron rules for EN16931 (CII)", "Schematron rules for CIUS XRechnung (CII)"]));

            Assert.That(result.Report, Is.Not.Null);
            AssertReportIsValid(result, true);
            Assert.That(GetReportMessages(result), Is.Empty);
            Assert.That(GetAssessment(result)?.Element(VarlNamespace + "accept"), Is.Not.Null);
            AssertReportContainsScenario(result, "EN16931 XRechnung (CII)");
            Assert.That(result.IsProcessingSuccessful, Is.True);
            Assert.That(result.ProcessingErrors, Is.Empty);
            Assert.That(result.IsAccepted, Is.True);
        });
    }

    [Test]
    public void Validate_UBL_Schema_Validation_Failure()
    {
        var result = ValidateInvoice(_validator, "ubl_schemavalidation_failure.xml");

        Assert.Multiple(() =>
        {
            Assert.That(result.SchemaValidationResult.IsValid, Is.False);
            Assert.That(result.SchemaValidationResult.Errors, Is.Not.Empty);
            Assert.That(result.SchemaValidationResult.Errors.Any(error => error.Message.Contains("LineExtensionAmount")), Is.True);

            Assert.That(result.Report, Is.Not.Null);
            AssertReportIsValid(result, false);
            AssertReportContainsScenario(result, "EN16931 XRechnung (UBL Invoice)");
            var steps = GetValidationSteps(result);
            Assert.That(steps["val-xsd"].Attribute("valid")?.Value, Is.EqualTo("false"));
            Assert.That(steps["val-xsd"].Descendants(VarlNamespace + "message").Count(), Is.GreaterThan(0));
            Assert.That(steps.Keys, Does.Not.Contain("val-sch.1"));
            Assert.That(GetAssessment(result)?.Element(VarlNamespace + "reject"), Is.Not.Null);
            Assert.That(result.IsProcessingSuccessful, Is.True);
            Assert.That(result.IsAccepted, Is.False);
        });
    }

    [Test]
    public void Validate_CII_Schema_Validation_Failure()
    {
        var result = ValidateInvoice(_validator, "cii_schemavalidation_failure.xml");
        Assert.Multiple(() =>
        {
            Assert.That(result.SchemaValidationResult.IsValid, Is.False);
            Assert.That(result.SchemaValidationResult.Errors, Is.Not.Empty);
            Assert.That(
                result.SchemaValidationResult.Errors.Any(error => error.Message.Contains("LineTotalAmount")),
                Is.True);

            Assert.That(result.Report, Is.Not.Null);
            AssertReportIsValid(result, false);
            AssertReportContainsScenario(result, "EN16931 XRechnung (CII)");
            var steps = GetValidationSteps(result);
            Assert.That(steps["val-xsd"].Attribute("valid")?.Value, Is.EqualTo("false"));
            Assert.That(steps.Keys, Does.Not.Contain("val-sch.1"));
            Assert.That(GetAssessment(result)?.Element(VarlNamespace + "reject"), Is.Not.Null);
            Assert.That(result.IsAccepted, Is.False);
        });
    }

    [Test]
    public void Validate_UBL_Schematron_Validation_Failure()
    {
        var result = ValidateInvoice(_validator, "ubl_schematronvalidation_failure.xml");

        Assert.Multiple(() =>
        {
            Assert.That(result.SchemaValidationResult.IsValid, Is.True);

            var assertions = GetAssertions(result).ToArray();
            Assert.That(assertions, Has.One.Matches<Assertion>(assertion => assertion.Severity == AssertionSeverity.Information && assertion.RuleId == "BR-DE-TMP-32"));
            Assert.That(assertions, Has.One.Matches<Assertion>(assertion => assertion.Severity == AssertionSeverity.Warning && assertion.RuleId == "UBL-CR-002"));
            Assert.That(assertions, Has.One.Matches<Assertion>(assertion => assertion.Severity == AssertionSeverity.Fatal && assertion.RuleId == "BR-CO-16"));

            Assert.That(result.Report, Is.Not.Null);
            AssertReportIsValid(result, false);
            AssertReportContainsScenario(result, "EN16931 XRechnung (UBL Invoice)");
            var messages = GetReportMessages(result).ToList();
            Assert.That(messages, Has.One.Matches<(string id, string level, string code)>(message =>
                message.id == "val-sch.1.1" && message.level == "error" && message.code == "BR-CO-16"));
            Assert.That(messages, Has.One.Matches<(string id, string level, string code)>(message =>
                message.id == "val-sch.1.2" && message.level == "warning" && message.code == "UBL-CR-002"));
            Assert.That(messages, Has.One.Matches<(string id, string level, string code)>(message =>
                message.id == "val-sch.2.1" && message.level == "information" && message.code == "BR-DE-TMP-32"));
            Assert.That(GetAssessment(result)?.Element(VarlNamespace + "reject"), Is.Not.Null);
            Assert.That(result.IsProcessingSuccessful, Is.True);
            Assert.That(result.IsAccepted, Is.False);
        });
    }

    [Test]
    public void Validate_CII_Schematron_Validation_Failure()
    {
        var result = ValidateInvoice(_validator, "cii_schematronvalidation_failure.xml");

        Assert.Multiple(() =>
        {
            Assert.That(result.SchemaValidationResult.IsValid, Is.True);

            var assertions = GetAssertions(result).ToArray();
            Assert.That(assertions, Has.One.Matches<Assertion>(assertion => assertion.Severity == AssertionSeverity.Information && assertion.RuleId == "BR-DE-TMP-32"));
            Assert.That(assertions, Has.One.Matches<Assertion>(assertion => assertion.Severity == AssertionSeverity.Warning && assertion.RuleId == "BR-DE-19"));
            Assert.That(assertions, Has.One.Matches<Assertion>(assertion => assertion.Severity == AssertionSeverity.Fatal && assertion.RuleId == "BR-CO-16"));

            Assert.That(result.Report, Is.Not.Null);
            AssertReportIsValid(result, false);
            AssertReportContainsScenario(result, "EN16931 XRechnung (CII)");
            var messages = GetReportMessages(result).ToList();
            Assert.That(messages, Has.One.Matches<(string id, string level, string code)>(message =>
                message.id == "val-sch.1.1" && message.level == "error" && message.code == "BR-CO-16"));
            Assert.That(messages, Has.One.Matches<(string id, string level, string code)>(message =>
                message.id == "val-sch.2.1" && message.level == "information" && message.code == "BR-DE-TMP-32"));
            Assert.That(messages, Has.One.Matches<(string id, string level, string code)>(message =>
                message.id == "val-sch.2.2" && message.level == "warning" && message.code == "BR-DE-19"));
            Assert.That(GetAssessment(result)?.Element(VarlNamespace + "reject"), Is.Not.Null);
            Assert.That(result.IsProcessingSuccessful, Is.True);
            Assert.That(result.IsAccepted, Is.False);
        });
    }

    [Test]
    public void Validate_Malformed_ShoudResultInStatusParsingInputXmlFailed()
    {
        var result = ValidateInvoice(_validator, "malformed_invoice.xml");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ValidationProcessStatus.ParsingInputXmlFailed));
            Assert.That(result.WellFormednessResult.IsValid, Is.False);
            Assert.That(result.WellFormednessResult.Error, Is.Not.Null);
            Assert.That(result.SchemaValidationResult, Is.Null);
            Assert.That(result.SchematronValidationResults, Is.Empty);

            Assert.That(result.Report, Is.Not.Null);
            AssertReportIsValid(result, false);
            Assert.That(result.Report.Root?.Element(VarlNamespace + "noScenarioMatched"), Is.Not.Null);
            var steps = GetValidationSteps(result);
            Assert.That(steps["val-xml"].Attribute("valid")?.Value, Is.EqualTo("false"));
            Assert.That(steps["val-xml"].Descendants(VarlNamespace + "message").Count(), Is.EqualTo(1));
            Assert.That(GetAssessment(result)?.Element(VarlNamespace + "reject"), Is.Not.Null);
            Assert.That(result.IsProcessingSuccessful, Is.False);
            Assert.That(result.ProcessingErrors, Is.Not.Empty);
            Assert.That(result.IsAccepted, Is.False);
        });
    }

    [Test]
    public void Validate_NoScenarioMatched_ShouldCreateNoScenarioReport()
    {
        var result = ValidateInvoice(_validator, "ubl_no_scenario.xml");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ValidationProcessStatus.NoScenarioMatched));
            Assert.That(result.SchemaValidationResult, Is.Null);
            Assert.That(result.SchematronValidationResults, Is.Empty);

            Assert.That(result.Report, Is.Not.Null);
            AssertReportIsValid(result, false);
            Assert.That(result.Report.Root?.Element(VarlNamespace + "noScenarioMatched"), Is.Not.Null);
            Assert.That(result.Report.Root?.Element(VarlNamespace + "scenarioMatched"), Is.Null);
            var steps = GetValidationSteps(result);
            Assert.That(steps["val-xml"].Attribute("valid")?.Value, Is.EqualTo("true"));
            Assert.That(GetReportMessages(result), Is.Empty);
            Assert.That(GetAssessment(result)?.Element(VarlNamespace + "reject"), Is.Not.Null);
            Assert.That(result.IsProcessingSuccessful, Is.True);
            Assert.That(result.ProcessingErrors, Is.Empty);
            Assert.That(result.IsAccepted, Is.False);
        });
    }

    private static void AssertReportIsValid(ValidationResult result, bool isValid)
    {
        Assert.That(result.Report.Root?.Name, Is.EqualTo(VarlNamespace + "report"));
        Assert.That(result.Report.Root?.Attribute("valid")?.Value, Is.EqualTo(isValid ? "true" : "false"));
    }

    private static void AssertReportContainsScenario(ValidationResult result, string scenarioName)
    {
        Assert.That(GetScenarioElement(result)?.Element(ScenarioNamespace + "name")?.Value, Is.EqualTo(scenarioName));
    }

    private static void AssertReportInputIsValid(ValidationResult result, string invoiceFileName)
    {
        var invoicePath = Path.Combine(TestdataDirectory, invoiceFileName);
        var root = result.ReportInput.Root;
        Assert.That(root?.Name, Is.EqualTo(ReportInputNamespace + "createReportInput"));
        Assert.That(root?.Attribute("frameworkVersion")?.Value, Is.EqualTo("1.0.2"));
        Assert.That(root?.Element(ReportInputNamespace + "engine")?.Element(ReportInputNamespace + "name")?.Value,
            Is.Not.Empty);
        Assert.That(root?.Element(ReportInputNamespace + "documentIdentification")
            ?.Element(ReportInputNamespace + "documentHash")
            ?.Element(ReportInputNamespace + "hashAlgorithm")?.Value, Is.EqualTo("SHA-256"));
        Assert.That(root?.Element(ReportInputNamespace + "documentIdentification")
            ?.Element(ReportInputNamespace + "documentHash")
            ?.Element(ReportInputNamespace + "hashValue")?.Value,
            Is.EqualTo(Convert.ToBase64String(SHA256.HashData(File.ReadAllBytes(invoicePath)))));
        Assert.That(root?.Element(ReportInputNamespace + "documentIdentification")
            ?.Element(ReportInputNamespace + "documentReference")?.Value, Is.EqualTo(invoicePath));
        Assert.That(root?.Elements(ReportInputNamespace + "validationResultsSchematron").Count(), Is.EqualTo(2));
    }

    private static XElement? GetScenarioElement(ValidationResult result)
    {
        return result.Report.Root?.Element(VarlNamespace + "scenarioMatched")
            ?.Element(ScenarioNamespace + "scenario");
    }

    private static XElement? GetAssessment(ValidationResult result)
    {
        return result.Report.Root?.Element(VarlNamespace + "assessment");
    }

    private static Dictionary<string, XElement> GetValidationSteps(ValidationResult result)
    {
        return result.Report.Root?.Descendants(VarlNamespace + "validationStepResult")
            .ToDictionary(step => step.Attribute("id")?.Value ?? string.Empty) ?? [];
    }

    private static IEnumerable<(string id, string level, string code)> GetReportMessages(ValidationResult result)
    {
        return result.Report.Root?.Descendants(VarlNamespace + "message")
            .Select(message =>
                (message.Attribute("id")?.Value ?? string.Empty,
                    message.Attribute("level")?.Value ?? string.Empty,
                    message.Attribute("code")?.Value ?? string.Empty)) ?? [];
    }

    private static IEnumerable<Assertion> GetAssertions(ValidationResult result)
    {
        return result.SchematronValidationResults.SelectMany(schematronResult => schematronResult.Assertions);
    }
}
