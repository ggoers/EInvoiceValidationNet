using EInvoiceValidationNet.Validation;
using EInvoiceValidationNet.Validation.SchematronValidation;
using EInvoiceValidationNet.Validation.SchemaValidation;
using EInvoiceValidationNet.XmlProcessing;

namespace EInvoiceValidationNet;

/// <summary>
/// Creates validators that are wired with the XML processing bundled in this package (Saxon-based, XSLT 2.0 / XPath 2.0).
/// </summary>
/// <remarks>
/// Use this factory when no dependency injection container is used; when DI is available, register
/// <see cref="IXmlProcessing"/> with the Saxon adapter instead and let the container create the validator.
/// </remarks>
public class ValidatorFactory
{
    /// <summary>
    /// Creates a new validator.
    /// </summary>
    /// <returns>A validator that can be reused for any number of invoices; every call returns an independent instance.</returns>
    public IValidator CreateValidator()
    {
        var xmlProcessing = new SaxonXmlProcessing();
        var schemaValidator = new SchemaValidator();
        var schematronValidator = new SchematronValidator(xmlProcessing);

        return new Validator(xmlProcessing, schemaValidator, schematronValidator);
    }
}
