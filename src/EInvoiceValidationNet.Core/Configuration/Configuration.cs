using System.Collections.Generic;
using System.Linq;

namespace EInvoiceValidationNet.Core.Configuration
{
    /// <summary>
    /// Immutable validation configuration: the scenarios used to validate e-invoices plus the optional report
    /// transformation that is applied when no scenario matches.
    /// </summary>
    /// <remarks>
    /// Create instances with <see cref="ConfigurationBuilder"/> (fluent API) or
    /// <see cref="ConfigurationBuilder.FromScenariosXml(System.IO.Stream, string, string)"/> (<c>scenarios.xml</c>).
    /// </remarks>
    public sealed class Configuration
    {
        /// <summary>
        /// Initializes a new configuration.
        /// </summary>
        /// <param name="scenarios">Required. The scenarios in the order they are evaluated when selecting a scenario for an invoice. Must contain at least one scenario.</param>
        /// <param name="noScenarioReport">Optional. The report transformation used when no scenario matches an invoice. <c>null</c> (the default) means no report is created in that case.</param>
        /// <exception cref="InvalidConfigurationException">Thrown when <paramref name="scenarios"/> contains no scenario.</exception>
        public Configuration(
            IEnumerable<Scenario> scenarios,
            ReportTransformationConfiguration noScenarioReport = null)
        {
            Scenarios = scenarios.ToList();
            NoScenarioReport = noScenarioReport;
            if (Scenarios.Count == 0)
            {
                throw new InvalidConfigurationException("The configuration needs at least one scenario.");
            }
        }

        /// <summary>
        /// The configured scenarios, in the order they are evaluated when selecting a scenario for an invoice.
        /// Always contains at least one scenario.
        /// </summary>
        public IReadOnlyList<Scenario> Scenarios { get; }

        /// <summary>
        /// The report transformation used when no scenario matches an invoice, or <c>null</c> when no report
        /// should be created in that case.
        /// </summary>
        public ReportTransformationConfiguration NoScenarioReport { get; }
    }
}
