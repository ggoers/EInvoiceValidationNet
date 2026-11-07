using System.Xml.Linq;
using EInvoiceValidationNet.Configuration;

namespace EInvoiceValidationNet.Validation.SchematronValidation
{
    internal interface ISchematronValidator
    {
        SchematronValidationResult Validate(XDocument invoiceXml, SchematronValidationConfiguration configuration);
    }
}
