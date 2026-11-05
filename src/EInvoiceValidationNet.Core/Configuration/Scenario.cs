using System.Collections.Generic;
using System.Linq;

namespace EInvoiceValidationNet.Core.Configuration
{
    /// <summary>
    /// One validation pipeline: how a scenario is matched to an invoice, which schema and Schematron validations run,
    /// how the report is generated and which rule levels are overridden.
    /// </summary>
    /// <remarks>
    /// The steps run in this order: XML schema validation, Schematron validations (skipped when the schema validation
    /// failed), report transformation. A scenario needs at least one validation step (schema or Schematron).
    /// </remarks>
    public sealed class Scenario
    {
        /// <summary>
        /// Initializes a scenario.
        /// </summary>
        /// <param name="matchExpression">Required. Decides whether this scenario applies to an invoice. It is evaluated for
        /// every scenario in configuration order; the first matching scenario is selected. When none matches, the invoice
        /// is validated with <see cref="Configuration.NoScenarioReport"/> only (if configured).</param>
        /// <param name="schemaValidation">Optional. The XML schema validation step; <c>null</c> means no schema validation.</param>
        /// <param name="schematronValidations">Optional. The Schematron validation steps, executed in the given order.
        /// <c>null</c> means no Schematron validation. Together with <paramref name="schemaValidation"/> at least one
        /// validation step must be present. If <paramref name="schemaValidation"/> is configured, the first the schema validation is done. If it has erros,
        /// no schematron validation is done even if configured.</param>
        /// <param name="name">Optional and only informative. A human readable name of the scenario, e.g. the invoice format it applies to; it is trimmed.</param>
        /// <param name="reportTransformation">Optional. The report transformation; <c>null</c> means no report is created.</param>
        /// <param name="ruleCustomizations">Optional. Level overrides for single business rules; <c>null</c> means no overrides.</param>
        /// <param name="acceptMatchExpression">Optional. Decides, based on the generated report, whether the invoice is accepted;
        /// <c>null</c> means no acceptance decision is computed.</param>
        /// <param name="namespaces">Optional. Namespace declarations for the XPath expressions; prefixes must be unique.</param>
        /// <exception cref="InvalidConfigurationException">Thrown when <paramref name="matchExpression"/> is <c>null</c>,
        /// when no validation step is configured or when a namespace prefix is declared twice.</exception>
        public Scenario(
            MatchExpression matchExpression,
            SchemaValidationConfiguration schemaValidation,
            IEnumerable<SchematronValidationConfiguration> schematronValidations,
            string name = null,
            ReportTransformationConfiguration reportTransformation = null,
            IEnumerable<RuleCustomization> ruleCustomizations = null,
            MatchExpression acceptMatchExpression = null,
            IEnumerable<NamespaceDeclaration> namespaces = null)
        {
            MatchExpression = matchExpression ?? throw new InvalidConfigurationException("A scenario needs a matchExpression");
            SchemaValidation = schemaValidation;
            SchematronValidations = schematronValidations?.ToList() ?? new List<SchematronValidationConfiguration>();
            Name = name?.Trim();
            ReportTransformation = reportTransformation;
            RuleCustomizations = ruleCustomizations?.ToList() ?? new List<RuleCustomization>();
            AcceptMatchExpression = acceptMatchExpression;
            Namespaces = namespaces?.ToList() ?? new List<NamespaceDeclaration>();

            if (Namespaces.GroupBy(n => n.Prefix).Any(g => g.Count() > 1))
            {
                throw new InvalidConfigurationException("Namespace prefixes must be unique within a scenario");
            }

            if (SchemaValidation is null && SchematronValidations.Count == 0)
            {
                throw new InvalidConfigurationException("A scenario needs at least one ValidationConfiguration (Schema or Schematron).");
            }
        }

        /// <summary>
        /// The optional name of the scenario, or <c>null</c> when the configuration did not specify one.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The expression that decides whether the invoice is validated with this scenario (scenario <c>match</c>).
        /// Always set; it is evaluated when selecting a scenario for an invoice.
        /// </summary>
        public MatchExpression MatchExpression { get; }

        /// <summary>
        /// The optional expression that decides whether the invoice is accepted, evaluated against the generated report
        /// (scenario <c>acceptMatch</c>); <c>null</c> when no acceptance decision is computed.
        /// </summary>
        public MatchExpression AcceptMatchExpression { get; }

        /// <summary>
        /// The namespace declarations available to the XPath expressions of this scenario; empty when none were configured.
        /// Prefixes are unique within a scenario.
        /// </summary>
        public IReadOnlyList<NamespaceDeclaration> Namespaces { get; }

        /// <summary>
        /// The XML schema validation step, or <c>null</c> when the scenario does not validate against an XSD.
        /// </summary>
        public SchemaValidationConfiguration SchemaValidation { get; }

        /// <summary>
        /// The Schematron validation steps in execution order; empty when the scenario does not use Schematron.
        /// </summary>
        public IReadOnlyList<SchematronValidationConfiguration> SchematronValidations { get; }

        /// <summary>
        /// The report transformation, or <c>null</c> when the scenario does not create a report.
        /// </summary>
        public ReportTransformationConfiguration ReportTransformation { get; }

        /// <summary>
        /// The level overrides applied when the report is created; empty when no rule level is overridden.
        /// </summary>
        public IReadOnlyList<RuleCustomization> RuleCustomizations { get; }

        /// <summary>
        /// The verbatim <c>s:scenario</c> element this scenario was read from, or <c>null</c> when the scenario was
        /// configured in code (a matching element is generated for the report input then).
        /// </summary>
        internal System.Xml.Linq.XElement RawScenarioElement { get; set; }
    }
}
