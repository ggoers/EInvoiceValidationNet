using System;
using System.Xml.Linq;

namespace EInvoiceValidationNet.Core.Configuration
{
    /// <summary>
    /// An Expression which returns a Boolean (used to find the matching scenario or to evaluate if an invoice is accepted after validation).
    /// </summary>
    /// <remarks>
    /// Use <see cref="XPathMatchExpression"/> for XPath expressions (also supported by <c>scenarios.xml</c>) or
    /// <see cref="PredicateMatchExpression"/> for a .NET predicate (code-defined configurations only).
    /// </remarks>
    public abstract class MatchExpression
    {
    }

    /// <summary>
    /// Matches with an XPath expression that is evaluated on the invoice document.
    /// </summary>
    public sealed class XPathMatchExpression : MatchExpression
    {
        /// <summary>
        /// Initializes the expression from an XPath.
        /// </summary>
        /// <param name="xPath">Required. The XPath expression. Leading and trailing whitespace is removed.
        /// The namespace prefixes declared for the scenario are available inside the expression.</param>
        /// <exception cref="InvalidConfigurationException">Thrown when <paramref name="xPath"/> is <c>null</c>, empty or whitespace.</exception>
        public XPathMatchExpression(string xPath)
        {
            if (string.IsNullOrWhiteSpace(xPath))
            {
                throw new InvalidConfigurationException("XPath match expression cannot be empty or whitespace in a scenario");
            }

            XPath = xPath.Trim();
        }

        /// <summary>
        /// The configured XPath expression (trimmed).
        /// </summary>
        public string XPath { get; }
    }

    /// <summary>
    /// Matches with a .NET predicate that is evaluated on the parsed invoice document.
    /// Useful if you want to create a configuration in Code only and have your own Match logic.
    /// </summary>
    public sealed class PredicateMatchExpression : MatchExpression
    {
        /// <summary>
        /// Initializes the expression from a predicate.
        /// </summary>
        /// <param name="predicate">Required. Called with the parsed invoice document. It should return <c>false</c> for
        /// documents that do not match instead of throwing.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is <c>null</c>.</exception>
        public PredicateMatchExpression(Func<XDocument, bool> predicate)
        {
            Predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        /// <summary>
        /// The predicate that decides whether the scenario matches an invoice.
        /// </summary>
        public Func<XDocument, bool> Predicate { get; }
    }
}
