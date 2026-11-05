using System;

namespace EInvoiceValidationNet.Core.Configuration
{
    /// <summary>
    /// A namespace prefix declaration (prefix to URI) that is available to the XPath expressions of a scenario
    /// (the <c>namespace</c> elements of <c>scenarios.xml</c>).
    /// </summary>
    public sealed class NamespaceDeclaration
    {
        /// <summary>
        /// Initializes a namespace declaration.
        /// </summary>
        /// <param name="prefix">Required. The prefix used in XPath expressions. It is trimmed.</param>
        /// <param name="uri">Required. The namespace URI the prefix stands for. It is trimmed.</param>
        /// <exception cref="InvalidConfigurationException">Thrown when <paramref name="prefix"/> or <paramref name="uri"/> is <c>null</c>, empty or whitespace.</exception>
        public NamespaceDeclaration(string prefix, string uri)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                throw new InvalidConfigurationException("Namespace prefix cannot be null, empty or whitespace");
            }

            if (string.IsNullOrWhiteSpace(uri))
            {
                throw new InvalidConfigurationException($"Namespace uri for prefix '{prefix}' cannot be null, empty or whitespace");
            }

            Prefix = prefix.Trim();
            Uri = uri.Trim();
        }

        /// <summary>
        /// The prefix as declared in the XPath expressions (trimmed).
        /// </summary>
        public string Prefix { get; }

        /// <summary>
        /// The namespace URI the prefix stands for (trimmed).
        /// </summary>
        public string Uri { get; }
    }
}
