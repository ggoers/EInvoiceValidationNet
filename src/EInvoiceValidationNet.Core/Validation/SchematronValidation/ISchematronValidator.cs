using System.Xml.Linq;
using EInvoiceValidationNet.Core.Configuration;

namespace EInvoiceValidationNet.Core.Validation.SchematronValidation
{
    internal interface ISchematronValidator
    {
        SchematronValidationResult Validate(XDocument invoiceXml, SchematronValidationConfiguration configuration);
    }
}
