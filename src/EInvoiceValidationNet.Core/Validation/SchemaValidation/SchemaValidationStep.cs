using System;

namespace EInvoiceValidationNet.Core.Validation.SchemaValidation
{
    internal class SchemaValidationStep : IValidationStep
    {
        private readonly ISchemaValidator _schemaValidator;

        public SchemaValidationStep(ISchemaValidator schemaValidator)
        {
            _schemaValidator = schemaValidator;
        }
        
        public void Execute(ValidationContext validationContext)
        {
            var scenario = validationContext.MatchingScenario;
            if (scenario?.SchemaValidation is null)
            {
                return;
            }

            try
            {
                validationContext.SchemaValidationResult = _schemaValidator.Validate(validationContext.InvoiceDocument, scenario.SchemaValidation.XsdFilePath);
            }
            catch (Exception e)
            {
                validationContext.AddProcessingError(
                    $"Error processing schema validation for scenario {scenario.Name}: {e.Message}");
            }
        }
    }
}