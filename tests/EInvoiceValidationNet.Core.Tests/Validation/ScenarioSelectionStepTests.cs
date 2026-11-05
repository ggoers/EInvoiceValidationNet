using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EInvoiceValidationNet.Core.Configuration;
using EInvoiceValidationNet.Core.Validation;

namespace EInvoiceValidationNet.Core.Tests.Validation
{
    using Configuration = EInvoiceValidationNet.Core.Configuration.Configuration;

    public class ScenarioSelectionStepTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void Execute_SingleScenario_EvaluatesMatchExpression(bool matches)
        {
            var xmlProcessing = new FakeXmlProcessing { XPathBooleanEvaluator = (_, _) => matches };
            var scenario = CreateScenario("Only", new XPathMatchExpression("true()"));
            var context = CreateContext(CreateConfiguration(scenario));

            new ScenarioSelectionStep(new MatchExpressionEvaluator(xmlProcessing)).Execute(context);

            Assert.Multiple(() =>
            {
                if (matches)
                {
                    Assert.That(context.MatchingScenario, Is.SameAs(scenario));
                }
                else
                {
                    Assert.That(context.MatchingScenario, Is.Null);
                }

                string[] expectedEvaluations = ["true()"];
                Assert.That(xmlProcessing.Evaluations, Is.EqualTo(expectedEvaluations));
            });
        }

        [Test]
        public void Execute_MultipleScenarios_SelectsFirstMatchingScenario()
        {
            var xmlProcessing = new FakeXmlProcessing
            {
                XPathBooleanEvaluator = (expression, _) => expression == "second"
            };
            var first = CreateScenario("First", new XPathMatchExpression("first"));
            var second = CreateScenario("Second", new XPathMatchExpression("second"));
            var third = CreateScenario("Third", new XPathMatchExpression("third"));
            var context = CreateContext(CreateConfiguration(first, second, third));

            new ScenarioSelectionStep(new MatchExpressionEvaluator(xmlProcessing)).Execute(context);

            Assert.Multiple(() =>
            {
                Assert.That(context.MatchingScenario, Is.SameAs(second));
                string[] expectedEvaluations = ["first", "second"];
                Assert.That(xmlProcessing.Evaluations, Is.EqualTo(expectedEvaluations));
            });
        }

        [Test]
        public void Execute_MultipleScenariosNoMatch_LeavesScenarioNull()
        {
            var xmlProcessing = new FakeXmlProcessing { XPathBooleanEvaluator = (_, _) => false };
            var context = CreateContext(CreateConfiguration(
                CreateScenario("First", new XPathMatchExpression("first")),
                CreateScenario("Second", new XPathMatchExpression("second"))));

            new ScenarioSelectionStep(new MatchExpressionEvaluator(xmlProcessing)).Execute(context);

            Assert.Multiple(() =>
            {
                Assert.That(context.MatchingScenario, Is.Null);
                string[] expectedEvaluations = ["first", "second"];
                Assert.That(xmlProcessing.Evaluations, Is.EqualTo(expectedEvaluations));
            });
        }

        [Test]
        public void Execute_PredicateMatchExpression_SelectsMatchingScenario()
        {
            var matching = CreateScenario("Matching", new PredicateMatchExpression(_ => true));
            var context = CreateContext(CreateConfiguration(
                CreateScenario("Not matching", new PredicateMatchExpression(_ => false)),
                matching));

            new ScenarioSelectionStep(new MatchExpressionEvaluator(new FakeXmlProcessing())).Execute(context);

            Assert.That(context.MatchingScenario, Is.SameAs(matching));
        }

        [Test]
        public void Execute_InvoiceDocumentNull_DoesNotSelectScenario()
        {
            var xmlProcessing = new FakeXmlProcessing();
            var context = CreateContext(CreateConfiguration(CreateScenario("Only", new XPathMatchExpression("true()"))));
            context.InvoiceDocument = null;

            new ScenarioSelectionStep(new MatchExpressionEvaluator(xmlProcessing)).Execute(context);

            Assert.Multiple(() =>
            {
                Assert.That(context.MatchingScenario, Is.Null);
                Assert.That(xmlProcessing.Evaluations, Is.Empty);
            });
        }

        private static Configuration CreateConfiguration(params Scenario[] scenarios)
        {
            return new Configuration(scenarios);
        }

        private static ValidationContext CreateContext(Configuration configuration)
        {
            return new ValidationContext(configuration, "invoice.xml")
            {
                InvoiceDocument = new XDocument(new XElement("Invoice")),
                WellFormednessValidationResult = new WellFormednessValidationResult()
            };
        }

        private static Scenario CreateScenario(string name, MatchExpression matchExpression)
        {
            return new Scenario(
                matchExpression,
                new SchemaValidationConfiguration("schema.xsd", "Schema"),
                null,
                name);
        }

        private sealed class FakeXmlProcessing : IXmlProcessing
        {
            public Func<string, XDocument, bool>? XPathBooleanEvaluator { get; set; }

            public List<string> Evaluations { get; } = new();

            public bool EvaluateXPathBoolean(XDocument xmlDocument, string expression,
                IEnumerable<KeyValuePair<string, string>>? namespaces = null)
            {
                Evaluations.Add(expression);

                return XPathBooleanEvaluator?.Invoke(expression, xmlDocument) ?? false;
            }

            public XDocument Transform(XDocument inputDocument, string xsltPath,
                IReadOnlyDictionary<string, XDocument>? parameters = null)
            {
                throw new NotSupportedException();
            }
        }
    }
}
