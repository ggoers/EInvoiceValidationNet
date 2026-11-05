using System;
using System.Linq;

namespace EInvoiceValidationNet.Core.Validation
{
    internal sealed class AcceptanceStep : IValidationStep
    {
        private readonly MatchExpressionEvaluator _matchExpressionEvaluator;

        public AcceptanceStep(MatchExpressionEvaluator matchExpressionEvaluator)
        {
            _matchExpressionEvaluator =
                matchExpressionEvaluator ?? throw new ArgumentNullException(nameof(matchExpressionEvaluator));
        }

        public void Execute(ValidationContext validationContext)
        {
            if (validationContext.ProcessingErrors.Count > 0
                && validationContext.WellFormednessValidationResult.IsValid)
            {
                validationContext.IsAccepted = null;

                return;
            }

            var scenario = validationContext.MatchingScenario;
            if (scenario is null)
            {
                validationContext.IsAccepted = false;

                return;
            }

            var isSchemaValid = validationContext.SchemaValidationResult is null
                || validationContext.SchemaValidationResult.IsValid;
            if (validationContext.Report != null && isSchemaValid && scenario.AcceptMatchExpression != null)
            {
                try
                {
                    validationContext.IsAccepted = _matchExpressionEvaluator.Evaluate(
                        scenario.AcceptMatchExpression, validationContext.Report, scenario.Namespaces);
                }
                catch (Exception e)
                {
                    validationContext.AddProcessingError($"Error evaluating accept recommendation: {e.Message}");
                    validationContext.IsAccepted = null;
                }

                return;
            }

            var hasFailedAssertions = validationContext.SchematronValidations
                .SelectMany(schematronValidation => schematronValidation.Result.Assertions)
                .Any();

            validationContext.IsAccepted = isSchemaValid && !hasFailedAssertions;
        }
    }
}
