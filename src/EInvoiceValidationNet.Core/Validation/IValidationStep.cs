namespace EInvoiceValidationNet.Core.Validation
{
    internal interface IValidationStep
    {
        void Execute(ValidationContext validationContext);
    }
}