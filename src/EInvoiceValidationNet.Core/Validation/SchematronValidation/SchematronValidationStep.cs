using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EInvoiceValidationNet.Configuration;
using EInvoiceValidationNet.Validation.ReportTransformation;

namespace EInvoiceValidationNet.Validation.SchematronValidation
{
    internal sealed class SchematronValidationStep : IValidationStep
    {
        private static readonly XNamespace SvrlNamespace = XNamespace.Get(FrameworkXml.SvrlNamespace);

        private readonly ISchematronValidator _schematronValidator;

        public SchematronValidationStep(ISchematronValidator schematronValidator)
        {
            _schematronValidator = schematronValidator ?? throw new ArgumentNullException(nameof(schematronValidator));
        }

        public void Execute(ValidationContext validationContext)
        {
            var scenario = validationContext.MatchingScenario;
            if (scenario is null || scenario.SchematronValidations.Count == 0
                || validationContext.ProcessingErrors.Count > 0
                || (validationContext.SchemaValidationResult != null && !validationContext.SchemaValidationResult.IsValid))
            {
                return;
            }

            foreach (var schematronValidation in scenario.SchematronValidations)
            {
                try
                {
                    var schematronValidationResult =
                        _schematronValidator.Validate(validationContext.InvoiceDocument, schematronValidation);
                    var customizedResult = ApplyRuleCustomizations(schematronValidationResult, scenario.RuleCustomizations);
                    validationContext.SchematronValidations.Add(
                        new SchematronValidationOutcome(customizedResult, schematronValidation));
                }
                catch (Exception e)
                {
                    validationContext.AddProcessingError(
                        $"Error processing schematron validation {schematronValidation.Name}: {e.Message}");
                    var errorResult = new SchematronValidationResult(schematronValidation.Name,
                        Enumerable.Empty<Assertion>(), CreateEmptySvrlDocument());
                    validationContext.SchematronValidations.Add(
                        new SchematronValidationOutcome(errorResult, schematronValidation));
                }
            }
        }

        private static SchematronValidationResult ApplyRuleCustomizations(
            SchematronValidationResult schematronValidationResult,
            IReadOnlyList<RuleCustomization> ruleCustomizations)
        {
            if (ruleCustomizations.Count == 0)
            {
                return schematronValidationResult;
            }

            var customizedAssertions = schematronValidationResult.Assertions
                .Select(assertion => ApplyRuleCustomization(assertion, ruleCustomizations))
                .ToList();

            return new SchematronValidationResult(schematronValidationResult.Name, customizedAssertions,
                schematronValidationResult.RawSvrl);
        }

        private static Assertion ApplyRuleCustomization(Assertion assertion,
            IReadOnlyList<RuleCustomization> ruleCustomizations)
        {
            var customization = ruleCustomizations.FirstOrDefault(custom => custom.RuleId == assertion.RuleId);

            return customization is null
                ? assertion
                : new Assertion(assertion.RuleId, assertion.Severity, assertion.Text, assertion.Location,
                    MapLevel(customization.LevelOverwrite));
        }

        private static AssertionSeverity MapLevel(Level level)
        {
            switch (level)
            {
                case Level.Information:
                    return AssertionSeverity.Information;
                case Level.Warning:
                    return AssertionSeverity.Warning;
                case Level.Error:
                    return AssertionSeverity.Error;
                case Level.Fatal:
                    return AssertionSeverity.Fatal;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown rule customization level");
            }
        }

        private static XDocument CreateEmptySvrlDocument()
        {
            return new XDocument(new XElement(SvrlNamespace + "schematron-output"));
        }
    }
}
