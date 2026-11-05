using System;
using EInvoiceValidationNet.Core.Configuration;

namespace EInvoiceValidationNet.Core.Validation.SchematronValidation
{
    internal sealed class SchematronValidationOutcome
    {
        public SchematronValidationOutcome(SchematronValidationResult result,
            SchematronValidationConfiguration configuration)
        {
            Result = result ?? throw new ArgumentNullException(nameof(result));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public SchematronValidationResult Result { get; }

        public SchematronValidationConfiguration Configuration { get; }
    }
}
