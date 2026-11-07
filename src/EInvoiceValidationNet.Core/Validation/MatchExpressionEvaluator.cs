using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EInvoiceValidationNet.Configuration;

namespace EInvoiceValidationNet.Validation
{
    internal sealed class MatchExpressionEvaluator
    {
        private readonly IXmlProcessing _xmlProcessing;

        public MatchExpressionEvaluator(IXmlProcessing xmlProcessing)
        {
            _xmlProcessing = xmlProcessing ?? throw new ArgumentNullException(nameof(xmlProcessing));
        }

        public bool Evaluate(MatchExpression matchExpression, XDocument document,
            IReadOnlyList<NamespaceDeclaration> namespaces)
        {
            switch (matchExpression)
            {
                case XPathMatchExpression xPathExpression:
                    return _xmlProcessing.EvaluateXPathBoolean(
                        document,
                        xPathExpression.XPath,
                        namespaces.Select(namespaceDeclaration =>
                            new KeyValuePair<string, string>(namespaceDeclaration.Prefix, namespaceDeclaration.Uri)));
                case PredicateMatchExpression predicateExpression:
                    return predicateExpression.Predicate(document);
                default:
                    throw new InvalidOperationException("Unknown match expression type");
            }
        }
    }
}
