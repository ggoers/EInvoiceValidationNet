using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EInvoiceValidationNet.Configuration;

namespace EInvoiceValidationNet.Validation
{
    internal sealed class ScenarioSelectionStep : IValidationStep
    {
        private readonly MatchExpressionEvaluator _matchExpressionEvaluator;

        public ScenarioSelectionStep(MatchExpressionEvaluator matchExpressionEvaluator)
        {
            _matchExpressionEvaluator =
                matchExpressionEvaluator ?? throw new ArgumentNullException(nameof(matchExpressionEvaluator));
        }

        public void Execute(ValidationContext validationContext)
        {
            if (validationContext.InvoiceDocument is null)
            {
                return;
            }
            
            validationContext.MatchingScenario = validationContext.Configuration.Scenarios.FirstOrDefault(
                    scenario => _matchExpressionEvaluator.Evaluate(scenario.MatchExpression, validationContext.InvoiceDocument, scenario.Namespaces));
        }
    }
}
