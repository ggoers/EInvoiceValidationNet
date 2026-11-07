using EInvoiceValidationNet.Configuration;
using EInvoiceValidationNet.Validation.SchematronValidation.SVRLParsing;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace EInvoiceValidationNet.Validation.SchematronValidation
{
    internal class SchematronValidator : ISchematronValidator
    {
        private readonly IXmlProcessing _xmlProcessing;

        public SchematronValidator(IXmlProcessing xmlProcessing)
        {
            _xmlProcessing = xmlProcessing ?? throw new ArgumentNullException(nameof(xmlProcessing));
        }

        public SchematronValidationResult Validate(XDocument invoiceXml, SchematronValidationConfiguration configuration)
        {
            if (invoiceXml is null)
            {
                throw new ArgumentNullException(nameof(invoiceXml));
            }

            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var svrlDocument = _xmlProcessing.Transform(invoiceXml, configuration.XsltFilePath);
            var svrlOutput = SvrlParser.Parse(svrlDocument);

            return Map(configuration.Name, svrlDocument, svrlOutput);
        }

        private static SchematronValidationResult Map(string name, XDocument svrlDocument, SvrlSchematronOutput svrlOutput)
        {
            var failedAsserts = svrlOutput.FailedAsserts ?? Array.Empty<SvrlFailedAssert>();
            var assertions = new List<Assertion>();
            foreach (var failedAssert in failedAsserts)
            {
                assertions.Add(new Assertion(failedAssert.Id, MapSeverity(failedAssert.Flag), failedAssert.Text?.Value, failedAssert.Location));
            }

            return new SchematronValidationResult(name, assertions, svrlDocument);
        }

        private static AssertionSeverity MapSeverity(string flag)
        {
            switch (flag)
            {
                case "information":
                    return AssertionSeverity.Information;
                case "warning":
                    return AssertionSeverity.Warning;
                case "error":
                    return AssertionSeverity.Error;
                case "fatal":
                    return AssertionSeverity.Fatal;
                default:
                    throw new ArgumentOutOfRangeException(nameof(flag), flag,
                        "Unknown flag of a schematron failed assertion");
            }
        }
    }
}
