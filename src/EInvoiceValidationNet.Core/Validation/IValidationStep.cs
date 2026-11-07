namespace EInvoiceValidationNet.Validation
{
    internal interface IValidationStep
    {
        void Execute(ValidationContext validationContext);
    }
}