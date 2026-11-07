using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EInvoiceValidationNet.Configuration;
using EInvoiceValidationNet.Validation;
using EInvoiceValidationNet.Validation.SchematronValidation;
using EInvoiceValidationNet.Validation.SchemaValidation;

namespace EInvoiceValidationNet.Core.Tests.Validation.SchematronValidation
{
    using Configuration = EInvoiceValidationNet.Configuration.Configuration;

    public class SchematronValidationStepTests
    {
        [Test]
        public void Execute_NoMatchingScenario_DoesNothing()
        {
            var validator = new FakeSchematronValidator();
            var context = CreateContext(CreateScenario("Rules"));
            context.MatchingScenario = null;

            new SchematronValidationStep(validator).Execute(context);

            Assert.Multiple(() =>
            {
                Assert.That(validator.ValidatedConfigurations, Is.Empty);
                Assert.That(context.SchematronValidations, Is.Empty);
            });
        }

        [Test]
        public void Execute_NoSchematronValidations_DoesNothing()
        {
            var validator = new FakeSchematronValidator();
            var context = CreateContext(CreateScenario());

            new SchematronValidationStep(validator).Execute(context);

            Assert.Multiple(() =>
            {
                Assert.That(validator.ValidatedConfigurations, Is.Empty);
                Assert.That(context.SchematronValidations, Is.Empty);
            });
        }

        [Test]
        public void Execute_InvalidSchemaResult_SkipsValidations()
        {
            var validator = new FakeSchematronValidator();
            var context = CreateContext(CreateScenario("Rules"));
            context.SchemaValidationResult = new SchemaValidationResult(false,
                [new XmlSyntaxError("invalid", XmlSyntaxErrorSeverity.Error, 1, 1)]);

            new SchematronValidationStep(validator).Execute(context);

            Assert.Multiple(() =>
            {
                Assert.That(validator.ValidatedConfigurations, Is.Empty);
                Assert.That(context.SchematronValidations, Is.Empty);
            });
        }

        [Test]
        public void Execute_ProcessingErrorsPresent_SkipsValidations()
        {
            var validator = new FakeSchematronValidator();
            var context = CreateContext(CreateScenario("Rules"));
            context.AddProcessingError("previous step failed");

            new SchematronValidationStep(validator).Execute(context);

            Assert.Multiple(() =>
            {
                Assert.That(validator.ValidatedConfigurations, Is.Empty);
                Assert.That(context.SchematronValidations, Is.Empty);
            });
        }

        [Test]
        public void Execute_MultipleValidations_AddsOutcomesInConfigurationOrder()
        {
            var validator = new FakeSchematronValidator();
            var scenario = CreateScenario("First", "Second");
            var context = CreateContext(scenario);

            new SchematronValidationStep(validator).Execute(context);

            Assert.Multiple(() =>
            {
                string[] expectedValidatedConfigurations = ["First", "Second"];
                Assert.That(validator.ValidatedConfigurations.Select(configuration => configuration.Name),
                    Is.EqualTo(expectedValidatedConfigurations));
                Assert.That(context.SchematronValidations.Select(outcome => outcome.Result.Name),
                    Is.EqualTo(expectedValidatedConfigurations));
                Assert.That(context.SchematronValidations.Select(outcome => outcome.Configuration.Name),
                    Is.EqualTo(expectedValidatedConfigurations));
            });
        }

        [Test]
        public void Execute_RuleCustomization_OverridesEffectiveSeverity()
        {
            var validator = new FakeSchematronValidator
            {
                ResultFactory = configuration => new SchematronValidationResult(configuration.Name, [
                    new Assertion("BR-CL-23", AssertionSeverity.Error, "customized rule failed", "/Invoice[1]"),
                    new Assertion("BR-CO-16", AssertionSeverity.Fatal, "other rule failed", "/Invoice[1]")
                ], new XDocument(new XElement("schematron-output")))
            };
            var scenario = CreateScenario([new RuleCustomization("BR-CL-23", Level.Warning)], "Rules");
            var context = CreateContext(scenario);

            new SchematronValidationStep(validator).Execute(context);

            var assertions = context.SchematronValidations.Single().Result.Assertions;
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
        public void Execute_ValidatorThrows_AddsProcessingErrorAndEmptyResultAndContinues()
        {
            var validator = new FakeSchematronValidator
            {
                ResultFactory = configuration => configuration.Name == "First"
                    ? throw new InvalidOperationException("stylesheet is broken")
                    : CreateResult(configuration)
            };
            var context = CreateContext(CreateScenario("First", "Second"));

            new SchematronValidationStep(validator).Execute(context);

            Assert.Multiple(() =>
            {
                string[] expectedNames = ["First", "Second"];
                Assert.That(validator.ValidatedConfigurations.Select(configuration => configuration.Name),
                    Is.EqualTo(expectedNames));
                Assert.That(context.ProcessingErrors.Single(), Does.Contain("First"));
                Assert.That(context.ProcessingErrors.Single(), Does.Contain("stylesheet is broken"));
                Assert.That(context.SchematronValidations, Has.Count.EqualTo(2));
                Assert.That(context.SchematronValidations[0].Result.Assertions, Is.Empty);
                Assert.That(context.SchematronValidations[0].Result.RawSvrl.Root?.Name.LocalName,
                    Is.EqualTo("schematron-output"));
                Assert.That(context.SchematronValidations[1].Result.Name, Is.EqualTo("Second"));
            });
        }

        private static ValidationContext CreateContext(Scenario scenario)
        {
            return new ValidationContext(new Configuration([scenario]), "invoice.xml")
            {
                InvoiceDocument = new XDocument(new XElement("Invoice")),
                WellFormednessValidationResult = new WellFormednessValidationResult(),
                MatchingScenario = scenario
            };
        }

        private static Scenario CreateScenario(params string[] schematronNames)
        {
            return CreateScenario(null, schematronNames);
        }

        private static Scenario CreateScenario(IEnumerable<RuleCustomization>? ruleCustomizations,
            params string[] schematronNames)
        {
            return new Scenario(
                new XPathMatchExpression("true()"),
                new SchemaValidationConfiguration("schema.xsd", "Schema"),
                schematronNames.Select(name => new SchematronValidationConfiguration(name + ".xsl", name)),
                "Test scenario",
                ruleCustomizations: ruleCustomizations);
        }

        private static SchematronValidationResult CreateResult(SchematronValidationConfiguration configuration)
        {
            return new SchematronValidationResult(configuration.Name, [],
                new XDocument(new XElement("schematron-output")));
        }

        private sealed class FakeSchematronValidator : ISchematronValidator
        {
            public List<SchematronValidationConfiguration> ValidatedConfigurations { get; } = new();

            public Func<SchematronValidationConfiguration, SchematronValidationResult>? ResultFactory { get; set; }

            public SchematronValidationResult Validate(XDocument invoiceXml,
                SchematronValidationConfiguration configuration)
            {
                ValidatedConfigurations.Add(configuration);
                return ResultFactory?.Invoke(configuration) ?? CreateResult(configuration);
            }
        }
    }
}
