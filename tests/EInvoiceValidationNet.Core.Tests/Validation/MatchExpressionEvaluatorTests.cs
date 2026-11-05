using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EInvoiceValidationNet.Core.Configuration;
using EInvoiceValidationNet.Core.Validation;

namespace EInvoiceValidationNet.Core.Tests.Validation
{
    public class MatchExpressionEvaluatorTests
    {
        [Test]
        public void Evaluate_XPathMatchExpression_PassesExpressionAndNamespaces()
        {
            var xmlProcessing = new FakeXmlProcessing { XPathResult = true };
            var document = new XDocument(new XElement("Invoice"));
            IReadOnlyList<NamespaceDeclaration> namespaces =
            [
                new NamespaceDeclaration("invoice", "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2")
            ];

            var result = new MatchExpressionEvaluator(xmlProcessing)
                .Evaluate(new XPathMatchExpression("exists(/invoice:Invoice)"), document, namespaces);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(xmlProcessing.Evaluations.Single().expression, Is.EqualTo("exists(/invoice:Invoice)"));
                Assert.That(xmlProcessing.Evaluations.Single().document, Is.SameAs(document));
                Assert.That(xmlProcessing.Evaluations.Single().namespaces.Single().Key, Is.EqualTo("invoice"));
                Assert.That(xmlProcessing.Evaluations.Single().namespaces.Single().Value,
                    Is.EqualTo("urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"));
            });
        }

        [Test]
        public void Evaluate_PredicateMatchExpression_UsesDocument()
        {
            var document = new XDocument(new XElement("Invoice"));
            XDocument? receivedDocument = null;

            var result = new MatchExpressionEvaluator(new FakeXmlProcessing())
                .Evaluate(new PredicateMatchExpression(received =>
                {
                    receivedDocument = received;
                    return true;
                }), document, []);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(receivedDocument, Is.SameAs(document));
            });
        }

        [Test]
        public void Evaluate_UnknownMatchExpressionType_Throws()
        {
            var evaluator = new MatchExpressionEvaluator(new FakeXmlProcessing());

            Assert.Throws<InvalidOperationException>(() => evaluator.Evaluate(new UnknownMatchExpression(),
                new XDocument(), []));
        }

        private sealed class UnknownMatchExpression : MatchExpression
        {
        }

        private sealed class FakeXmlProcessing : IXmlProcessing
        {
            public bool XPathResult { get; set; }

            public List<(string expression, XDocument document,
                IReadOnlyList<KeyValuePair<string, string>> namespaces)> Evaluations { get; } = new();

            public bool EvaluateXPathBoolean(XDocument xmlDocument, string expression,
                IEnumerable<KeyValuePair<string, string>>? namespaces = null)
            {
                Evaluations.Add((expression, xmlDocument, namespaces?.ToList() ?? []));
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
