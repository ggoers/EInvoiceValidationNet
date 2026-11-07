using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EInvoiceValidationNet.Configuration;
using EInvoiceValidationNet.Validation;
using EInvoiceValidationNet.Validation.SchematronValidation;
using EInvoiceValidationNet.Validation.SchemaValidation;

namespace EInvoiceValidationNet.Core.Tests.Validation
{
    using Configuration = EInvoiceValidationNet.Configuration.Configuration;

    public class AcceptanceStepTests
    {
        [Test]
        public void Execute_ProcessingErrorAndValidWellFormedness_LeavesAcceptanceUndefined()
        {
            var context = CreateContext(CreateScenario());
            context.AddProcessingError("report transformation failed");

            new AcceptanceStep(new MatchExpressionEvaluator(new FakeXmlProcessing())).Execute(context);

            Assert.That(context.IsAccepted, Is.Null);
        }

        [Test]
        public void Execute_NoMatchingScenario_Rejects()
        {
            var context = CreateContext(CreateScenario());
            context.MatchingScenario = null;

            new AcceptanceStep(new MatchExpressionEvaluator(new FakeXmlProcessing())).Execute(context);

            Assert.That(context.IsAccepted, Is.False);
        }

        [Test]
        public void Execute_AcceptMatchExpression_EvaluatesAgainstReport()
        {
            var xmlProcessing = new FakeXmlProcessing { XPathResult = true };
            var scenario = CreateScenario(new XPathMatchExpression("/rep:report/rep:accept"));
            var context = CreateContext(scenario);
            context.Report = new XDocument(new XElement("report"));

            new AcceptanceStep(new MatchExpressionEvaluator(xmlProcessing)).Execute(context);

            Assert.Multiple(() =>
            {
                Assert.That(context.IsAccepted, Is.True);
                Assert.That(xmlProcessing.Evaluations.Single().expression, Is.EqualTo("/rep:report/rep:accept"));
                Assert.That(xmlProcessing.Evaluations.Single().document, Is.SameAs(context.Report));
            });
        }

        [Test]
        public void Execute_AcceptMatchEvaluationThrows_AddsProcessingErrorAndLeavesUndefined()
        {
            var xmlProcessing = new FakeXmlProcessing
            {
                XPathException = new InvalidOperationException("invalid accept expression")
            };
            var context = CreateContext(CreateScenario(new XPathMatchExpression("/rep:report/rep:accept")));
            context.Report = new XDocument(new XElement("report"));

            new AcceptanceStep(new MatchExpressionEvaluator(xmlProcessing)).Execute(context);

            Assert.Multiple(() =>
            {
                Assert.That(context.IsAccepted, Is.Null);
                Assert.That(context.ProcessingErrors.Single(),
                    Does.Contain("Error evaluating accept recommendation: invalid accept expression"));
            });
        }

        [Test]
        public void Execute_WithoutAcceptMatchAndNoFailedAssertions_Accepts()
        {
            var context = CreateContext(CreateScenario());

            new AcceptanceStep(new MatchExpressionEvaluator(new FakeXmlProcessing())).Execute(context);

            Assert.That(context.IsAccepted, Is.True);
        }

        [Test]
        public void Execute_WithoutAcceptMatchAndFailedAssertions_Rejects()
        {
            var context = CreateContext(CreateScenario());
            context.SchematronValidations.Add(new SchematronValidationOutcome(
                new SchematronValidationResult("Rules", [
                    new Assertion("BR-CO-16", AssertionSeverity.Error, "rule failed", "/Invoice[1]")
                ], new XDocument(new XElement("schematron-output"))),
                new SchematronValidationConfiguration("rules.xsl", "Rules")));

            new AcceptanceStep(new MatchExpressionEvaluator(new FakeXmlProcessing())).Execute(context);

            Assert.That(context.IsAccepted, Is.False);
        }

        [Test]
        public void Execute_InvalidSchemaWithAcceptMatch_DoesNotEvaluateAndRejects()
        {
            var xmlProcessing = new FakeXmlProcessing { XPathResult = true };
            var context = CreateContext(CreateScenario(new XPathMatchExpression("/rep:report/rep:accept")));
            context.Report = new XDocument(new XElement("report"));
            context.SchemaValidationResult = new SchemaValidationResult(false,
                [new XmlSyntaxError("invalid", XmlSyntaxErrorSeverity.Error, 1, 1)]);

            new AcceptanceStep(new MatchExpressionEvaluator(xmlProcessing)).Execute(context);

            Assert.Multiple(() =>
            {
                Assert.That(context.IsAccepted, Is.False);
                Assert.That(xmlProcessing.Evaluations, Is.Empty);
            });
        }

        private static ValidationContext CreateContext(Scenario scenario)
        {
            return new ValidationContext(new Configuration([CreateScenario()]), "invoice.xml")
            {
                WellFormednessValidationResult = new WellFormednessValidationResult(),
                MatchingScenario = scenario
            };
        }

        private static Scenario CreateScenario(MatchExpression? acceptMatchExpression = null)
        {
            return new Scenario(
                new XPathMatchExpression("true()"),
                new SchemaValidationConfiguration("schema.xsd", "Schema"),
                null,
                "Test scenario",
                acceptMatchExpression: acceptMatchExpression);
        }

        private sealed class FakeXmlProcessing : IXmlProcessing
        {
            public bool XPathResult { get; set; }

            public Exception? XPathException { get; set; }

            public List<(string expression, XDocument document)> Evaluations { get; } = new();

            public bool EvaluateXPathBoolean(XDocument xmlDocument, string expression,
                IEnumerable<KeyValuePair<string, string>>? namespaces = null)
            {
                Evaluations.Add((expression, xmlDocument));
                if (XPathException != null)
                {
                    throw XPathException;
                }

                return XPathResult;
            }

            public XDocument Transform(XDocument inputDocument, string xsltPath,
                IReadOnlyDictionary<string, XDocument>? parameters = null)
            {
                throw new NotSupportedException();
            }
        }
    }
}
