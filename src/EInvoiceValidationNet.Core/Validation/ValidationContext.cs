using System;
using System.Collections.Generic;
using System.Xml.Linq;
using EInvoiceValidationNet.Core.Configuration;
using EInvoiceValidationNet.Core.Validation.SchematronValidation;
using EInvoiceValidationNet.Core.Validation.SchemaValidation;
using ValidationConfiguration = EInvoiceValidationNet.Core.Configuration.Configuration;

namespace EInvoiceValidationNet.Core.Validation
{
    internal class ValidationContext
    {
        private readonly List<string> _processingErrors = new List<string>();

        public ValidationContext(ValidationConfiguration configuration, string documentReference)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            DocumentReference = documentReference ?? string.Empty;
        }

        public ValidationConfiguration Configuration { get; }

        public string DocumentReference { get; }

        public IReadOnlyList<string> ProcessingErrors => _processingErrors;

        public Scenario MatchingScenario { get; set; }

        public XDocument InvoiceDocument { get; set; }

        public byte[] DocumentHash { get; set; }

        public WellFormednessValidationResult WellFormednessValidationResult { get; set; }

        public SchemaValidationResult SchemaValidationResult { get; set; }

        public List<SchematronValidationOutcome> SchematronValidations { get; } =
            new List<SchematronValidationOutcome>();

        public XDocument ReportInput { get; set; }

        public XDocument Report { get; set; }

        public bool? IsAccepted { get; set; }

        public void AddProcessingError(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
            {
                throw new ArgumentException("Processing error cannot be null, empty or whitespace", nameof(error));
            }

            _processingErrors.Add(error);
        }
    }
}
