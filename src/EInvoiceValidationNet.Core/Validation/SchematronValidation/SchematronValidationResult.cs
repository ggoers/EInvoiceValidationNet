using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace EInvoiceValidationNet.Validation.SchematronValidation
{
    /// <summary>
    /// Severity of a Schematron assertion, taken from the <c>flag</c> attribute of the SVRL <c>failed-assert</c>.
    /// </summary>
    public enum AssertionSeverity
    {
        /// <summary>The assertion carries information only.</summary>
        Information,

        /// <summary>The assertion is a warning.</summary>
        Warning,

        /// <summary>The assertion is a business rule violation.</summary>
        Error,

        /// <summary>The assertion represents a severe problem.</summary>
        Fatal
    }

    /// <summary>
    /// A single failed Schematron assertion, i.e. one <c>failed-assert</c> element of the SVRL output.
    /// </summary>
    public sealed class Assertion
    {
        /// <summary>
        /// Initializes an assertion.
        /// </summary>
        /// <param name="ruleId">Required. The business rule identifier (SVRL <c>id</c>), e.g. <c>BR-CO-10</c>; it is trimmed.</param>
        /// <param name="severity">Required. Must be a defined <see cref="AssertionSeverity"/> value.</param>
        /// <param name="text">Required. The human readable message of the assertion (SVRL <c>text</c>); it is trimmed.</param>
        /// <param name="location">Required. The path of the node(s) the assertion applies to (SVRL <c>location</c>); it is trimmed.</param>
        /// <param name="effectiveSeverity">Optional. The severity that applies after the scenario's rule customizations;
        /// <c>null</c> means no customization matched, so <see cref="EffectiveSeverity"/> equals <paramref name="severity"/>.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="ruleId"/>, <paramref name="text"/> or
        /// <paramref name="location"/> is <c>null</c>, empty or whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="severity"/> or
        /// <paramref name="effectiveSeverity"/> is not a defined <see cref="AssertionSeverity"/> value.</exception>
        public Assertion(string ruleId, AssertionSeverity severity, string text, string location,
            AssertionSeverity? effectiveSeverity = null)
        {
            if (string.IsNullOrWhiteSpace(ruleId))
            {
                throw new ArgumentException("RuleId cannot be null, empty or whitespace", nameof(ruleId));
            }

            if (!Enum.IsDefined(typeof(AssertionSeverity), severity))
            {
                throw new ArgumentOutOfRangeException(nameof(severity));
            }

            if (effectiveSeverity.HasValue && !Enum.IsDefined(typeof(AssertionSeverity), effectiveSeverity.Value))
            {
                throw new ArgumentOutOfRangeException(nameof(effectiveSeverity));
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Text cannot be null, empty or whitespace", nameof(text));
            }

            if (string.IsNullOrWhiteSpace(location))
            {
                throw new ArgumentException("Location cannot be null, empty or whitespace", nameof(location));
            }

            RuleId = ruleId.Trim();
            Severity = severity;
            EffectiveSeverity = effectiveSeverity ?? severity;
            Text = text.Trim();
            Location = location.Trim();
        }

        /// <summary>
        /// The identifier of the violated business rule (trimmed).
        /// </summary>
        public string RuleId { get; }

        /// <summary>
        /// The severity the rule reported (SVRL <c>flag</c>).
        /// </summary>
        public AssertionSeverity Severity { get; }

        /// <summary>
        /// The severity that applies for this validation: the level of the scenario's rule customization when one
        /// matches the <see cref="RuleId"/>, otherwise <see cref="Severity"/>. The raw SVRL document keeps the original
        /// severity.
        /// </summary>
        public AssertionSeverity EffectiveSeverity { get; }

        /// <summary>
        /// The human readable message of the failed assertion (trimmed).
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Path of the node(s) in the validated document the assertion applies to (SVRL <c>location</c>, always present).
        /// </summary>
        public string Location { get; }
    }

    /// <summary>
    /// Result of one Schematron validation step of a scenario.
    /// </summary>
    public sealed class SchematronValidationResult
    {
        /// <summary>
        /// Initializes a Schematron validation result.
        /// </summary>
        /// <param name="name">Optional. The name of the validation step as configured; it is trimmed.</param>
        /// <param name="assertions">Required. All failed assertions; may be empty.</param>
        /// <param name="rawSvrl">Required. The SVRL document produced by the Schematron transformation.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="assertions"/> or <paramref name="rawSvrl"/> is <c>null</c>.</exception>
        public SchematronValidationResult(string name, IEnumerable<Assertion> assertions, XDocument rawSvrl)
        {
            Name = name?.Trim();
            Assertions = assertions?.ToList() ?? throw new ArgumentNullException(nameof(assertions));
            RawSvrl = rawSvrl ?? throw new ArgumentNullException(nameof(rawSvrl));
        }

        /// <summary>
        /// Name of the schematron validation as configured, or <c>null</c> if the configuration did not specify one.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// All failed assertions of the validation; empty when the invoice satisfied every rule.
        /// </summary>
        public IReadOnlyList<Assertion> Assertions { get; }

        /// <summary>
        /// The raw SVRL document produced by the schematron transformation.
        /// </summary>
        public XDocument RawSvrl { get; }
    }
}
