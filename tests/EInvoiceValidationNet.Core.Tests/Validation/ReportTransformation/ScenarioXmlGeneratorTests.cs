using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EInvoiceValidationNet.Core.Configuration;
using EInvoiceValidationNet.Core.Validation;
using EInvoiceValidationNet.Core.Validation.ReportTransformation;
using EInvoiceValidationNet.Core.Validation.SchemaValidation;

namespace EInvoiceValidationNet.Core.Tests.Validation.ReportTransformation
{
    using Configuration = EInvoiceValidationNet.Core.Configuration.Configuration;

    public class ScenarioXmlGeneratorTests
    {
        private static readonly XNamespace ScenarioNamespace = "http://www.xoev.de/de/validator/framework/1/scenarios";

        private static readonly string FrameworkXsdPath = Path.Combine(
            AppContext.BaseDirectory, "Testdata", "ReportInputXsd", "createReportInput.xsd");

        [Test]
        public void CreateScenarioElement_FluentScenario_ContainsAllConfiguredParts()
        {
            var scenario = CreateFluentScenario();

            var element = ScenarioXmlGenerator.CreateScenarioElement(scenario);

            Assert.Multiple(() =>
            {
                Assert.That(element.Name, Is.EqualTo(ScenarioNamespace + "scenario"));
                Assert.That(element.Element(ScenarioNamespace + "name")?.Value, Is.EqualTo("UBL Invoice scenario"));
                Assert.That(element.Elements(ScenarioNamespace + "namespace").Single().Attribute("prefix")?.Value,
                    Is.EqualTo("invoice"));
                Assert.That(element.Elements(ScenarioNamespace + "namespace").Single().Value,
                    Is.EqualTo("urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"));
                Assert.That(element.Element(ScenarioNamespace + "match")?.Value, Is.EqualTo("exists(/invoice:Invoice)"));
                Assert.That(element.Element(ScenarioNamespace + "validateWithXmlSchema")
                    ?.Element(ScenarioNamespace + "resource")?.Element(ScenarioNamespace + "name")?.Value,
                    Is.EqualTo("XML Schema for UBL 2.1 Invoice"));
                Assert.That(element.Element(ScenarioNamespace + "validateWithXmlSchema")
                    ?.Element(ScenarioNamespace + "resource")?.Element(ScenarioNamespace + "location")?.Value,
                    Is.EqualTo("UBL-Invoice-2.1.xsd"));
                string[] expectedSchematronNames =
                    ["Schematron rules for EN16931 (UBL)", "XRechnung rules (UBL)"];
                Assert.That(element.Elements(ScenarioNamespace + "validateWithSchematron").Select(validation =>
                    validation.Element(ScenarioNamespace + "resource")?.Element(ScenarioNamespace + "name")?.Value),
                    Is.EqualTo(expectedSchematronNames));
                var createReport = element.Element(ScenarioNamespace + "createReport");
                Assert.That(createReport?.Element(ScenarioNamespace + "resource")
                    ?.Element(ScenarioNamespace + "name")?.Value, Is.EqualTo("Validation report"));
                var customLevel = createReport?.Elements(ScenarioNamespace + "customLevel").Single();
                Assert.That(customLevel?.Attribute("level")?.Value, Is.EqualTo("warning"));
                Assert.That(customLevel?.Value, Is.EqualTo("BR-CL-23"));
                Assert.That(element.Element(ScenarioNamespace + "acceptMatch")?.Value,
                    Is.EqualTo("/rep:report/rep:assessment/rep:accept"));
            });
        }

        [Test]
        public void CreateScenarioElement_PredicateMatchWithoutSchemaValidation_OmitsMatchAndSchemaElements()
        {
            var scenario = new Scenario(
                new PredicateMatchExpression(_ => true),
                null,
                [new SchematronValidationConfiguration("rules.xsl", "Rules")],
                "Code only scenario");

            var element = ScenarioXmlGenerator.CreateScenarioElement(scenario);

            Assert.Multiple(() =>
            {
                Assert.That(element.Element(ScenarioNamespace + "match"), Is.Null);
                Assert.That(element.Element(ScenarioNamespace + "validateWithXmlSchema"), Is.Null);
                Assert.That(element.Element(ScenarioNamespace + "name")?.Value, Is.EqualTo("Code only scenario"));
                Assert.That(element.Elements(ScenarioNamespace + "validateWithSchematron"), Has.One.Items);
            });
        }

        [Test]
        public void CreateScenarioElement_FluentScenarioWithoutOptionalParts_ContainsOnlyRequiredParts()
        {
            var scenario = new Scenario(
                new XPathMatchExpression("exists(/invoice:Invoice)"),
                new SchemaValidationConfiguration("UBL-Invoice-2.1.xsd", null),
                null,
                null);

            var element = ScenarioXmlGenerator.CreateScenarioElement(scenario);

            Assert.Multiple(() =>
            {
                Assert.That(element.Element(ScenarioNamespace + "name")?.Value, Is.Empty);
                Assert.That(element.Elements(ScenarioNamespace + "namespace"), Is.Empty);
                Assert.That(element.Elements(ScenarioNamespace + "validateWithSchematron"), Is.Empty);
                Assert.That(element.Element(ScenarioNamespace + "createReport"), Is.Null);
                Assert.That(element.Element(ScenarioNamespace + "acceptMatch"), Is.Null);
            });
        }

        [Test]
        public void CreateScenarioElement_FluentScenarioWithXPathAndSchema_CreatesXsdValidScenarioElement()
        {
            var scenario = CreateFluentScenario();
            var context = new ValidationContext(new Configuration([scenario]), "invoice.xml")
            {
                DocumentHash = [1, 2, 3],
                WellFormednessValidationResult = new WellFormednessValidationResult(),
                MatchingScenario = scenario
            };

            var document = new ReportInputBuilder(context).Build();

            Assert.That(new SchemaValidator().Validate(document, FrameworkXsdPath).IsValid, Is.True);
        }

        private static Scenario CreateFluentScenario()
        {
            return new Scenario(
                new XPathMatchExpression("exists(/invoice:Invoice)"),
                new SchemaValidationConfiguration("UBL-Invoice-2.1.xsd", "XML Schema for UBL 2.1 Invoice"),
                [
                    new SchematronValidationConfiguration("EN16931-UBL-validation.xsl", "Schematron rules for EN16931 (UBL)"),
                    new SchematronValidationConfiguration("XRechnung-UBL-validation.xsl", "XRechnung rules (UBL)")
                ],
                "UBL Invoice scenario",
                new ReportTransformationConfiguration("report.xsl", "Validation report", null),
                [new RuleCustomization("BR-CL-23", Level.Warning)],
                new XPathMatchExpression("/rep:report/rep:assessment/rep:accept"),
                [new NamespaceDeclaration("invoice", "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2")]);
        }
    }
}
