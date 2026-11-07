using EInvoiceValidationNet.Configuration;
using EInvoiceValidationNet.Core.Utils;
using EInvoiceValidationNet.Validation;

namespace EInvoiceValidationNet.IntegrationTests
{
    public class IntegrationTestBase
    {
        protected static readonly string TestdataDirectory = Path.Combine(AppContext.BaseDirectory, "Testdata");

        private static readonly string ScenariosXmlPath = Path
            .Combine(TestdataDirectory, "xrechnung_validator_configuration/scenarios.xml")
            .NormalizePath();

        protected static readonly Configuration.Configuration Configuration = ConfigurationBuilder.FromScenariosXml(ScenariosXmlPath, Path.GetDirectoryName(ScenariosXmlPath), null);

        protected ValidationResult ValidateInvoice(Validation.IValidator validator, string fileName)
        {
            return validator.Validate(Path.Combine(TestdataDirectory, fileName), Configuration);
        }
    }
}
