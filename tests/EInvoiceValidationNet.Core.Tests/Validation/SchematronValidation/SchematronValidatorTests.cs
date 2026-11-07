using System.Xml.Linq;
using EInvoiceValidationNet.Configuration;
using EInvoiceValidationNet.Validation;
using EInvoiceValidationNet.Validation.SchematronValidation;

namespace EInvoiceValidationNet.Core.Tests.Validation.SchematronValidation
{
    public class SchematronValidatorTests
    {
        private const string SvrlWithFailedAsserts = """
                                                     <svrl:schematron-output xmlns:svrl="http://purl.oclc.org/dsdl/svrl" title="test" schemaVersion="1.0">
                                                         <svrl:ns-prefix-in-attribute-values prefix="cbc" uri="urn:example"/>
                                                         <svrl:active-pattern id="pattern-1"/>
                                                         <svrl:fired-rule context="/Invoice"/>
                                                         <svrl:failed-assert id="BR-CO-10" flag="fatal" location="/Invoice[1]" test="false()">
                                                             <svrl:text>[BR-CO-10]-Sum of Invoice line net amount (BT-106) = Σ Invoice line net amount (BT-131).</svrl:text>
                                                         </svrl:failed-assert>
                                                         <svrl:failed-assert id="BR-CL-23" flag="warning" location="/Invoice[1]" test="false()">
                                                             <svrl:text>[BR-CL-23]-A warning.</svrl:text>
                                                         </svrl:failed-assert>
                                                         <svrl:failed-assert id="BR-DE-TMP-32" flag="information" location="/Invoice[1]" test="false()">
                                                             <svrl:text>[BR-DE-TMP-32]-Some information.</svrl:text>
                                                         </svrl:failed-assert>
                                                     </svrl:schematron-output>
                                                     """;

        private const string SvrlWithoutFailedAsserts = """
                                                        <svrl:schematron-output xmlns:svrl="http://purl.oclc.org/dsdl/svrl" title="test" schemaVersion="1.0">
                                                            <svrl:active-pattern id="pattern-1"/>
                                                            <svrl:fired-rule context="/Invoice"/>
                                                        </svrl:schematron-output>
                                                        """;

        private const string SvrlWithUnknownFlag = """
                                                   <svrl:schematron-output xmlns:svrl="http://purl.oclc.org/dsdl/svrl" title="test" schemaVersion="1.0">
                                                       <svrl:failed-assert id="BR-CO-10" flag="unknown" location="/Invoice[1]" test="false()">
                                                           <svrl:text>[BR-CO-10]-Unknown flag.</svrl:text>
                                                       </svrl:failed-assert>
                                                   </svrl:schematron-output>
                                                   """;

        private const string SvrlWithMissingLocation = """
                                                       <svrl:schematron-output xmlns:svrl="http://purl.oclc.org/dsdl/svrl" title="test" schemaVersion="1.0">
                                                           <svrl:failed-assert id="BR-CO-10" flag="fatal" test="false()">
                                                               <svrl:text>[BR-CO-10]-Missing location.</svrl:text>
                                                           </svrl:failed-assert>
                                                       </svrl:schematron-output>
                                                       """;


        [Test]
        public void Validate_FailedAsserts_ShouldMapNameRuleIdSeverityTextAndRawSvrl()
        {
            var processing = new SvrlReturningXmlProcessing(SvrlWithFailedAsserts);
            var validator = new SchematronValidator(processing);

            var result = validator.Validate(new XDocument(), new SchematronValidationConfiguration("schema.xsl", "Schematron name"));

            Assert.Multiple(() =>
            {
                Assert.That(result.Name, Is.EqualTo("Schematron name"));
                Assert.That(result.RawSvrl, Is.SameAs(processing.Svrl));
                Assert.That(result.Assertions, Has.Count.EqualTo(3));
                Assert.That(result.Assertions[0].RuleId, Is.EqualTo("BR-CO-10"));
                Assert.That(result.Assertions[0].Severity, Is.EqualTo(AssertionSeverity.Fatal));
                Assert.That(result.Assertions[0].Text, Is.EqualTo("[BR-CO-10]-Sum of Invoice line net amount (BT-106) = Σ Invoice line net amount (BT-131)."));
                Assert.That(result.Assertions[0].Location, Is.EqualTo("/Invoice[1]"));
                Assert.That(result.Assertions[1].RuleId, Is.EqualTo("BR-CL-23"));
                Assert.That(result.Assertions[1].Severity, Is.EqualTo(AssertionSeverity.Warning));
                Assert.That(result.Assertions[2].RuleId, Is.EqualTo("BR-DE-TMP-32"));
                Assert.That(result.Assertions[2].Severity, Is.EqualTo(AssertionSeverity.Information));
            });
        }

        [Test]
        public void Validate_NoFailedAsserts_ShouldHaveNoAssertions()
        {
            var validator = new SchematronValidator(new SvrlReturningXmlProcessing(SvrlWithoutFailedAsserts));

            var result = validator.Validate(new XDocument(), new SchematronValidationConfiguration("schema.xsl", null));

            Assert.Multiple(() =>
            {
                Assert.That(result.Name, Is.Null);
                Assert.That(result.Assertions, Is.Empty);
            });
        }

        [Test]
        public void Validate_UnknownFlag_ShouldThrow()
        {
            var validator = new SchematronValidator(new SvrlReturningXmlProcessing(SvrlWithUnknownFlag));

            Assert.Throws<ArgumentOutOfRangeException>(() => validator.Validate(new XDocument(), new SchematronValidationConfiguration("schema.xsl", null)));
        }

        [Test]
        public void Validate_MissingLocation_ShouldThrow()
        {
            var validator = new SchematronValidator(new SvrlReturningXmlProcessing(SvrlWithMissingLocation));

            Assert.Throws<ArgumentException>(() => validator.Validate(new XDocument(), new SchematronValidationConfiguration("schema.xsl", null)));
        }

        [Test]
        public void Validate_NullArguments_ShouldThrow()
        {
            var validator = new SchematronValidator(new SvrlReturningXmlProcessing(SvrlWithoutFailedAsserts));

            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentNullException>(() => validator.Validate(null!, new SchematronValidationConfiguration("schema.xsl", null)));
                Assert.Throws<ArgumentNullException>(() => validator.Validate(new XDocument(), null!));
            });
        }

        private sealed class SvrlReturningXmlProcessing(string svrl) : IXmlProcessing
        {
            public XDocument Svrl { get; } = XDocument.Parse(svrl);

            public bool EvaluateXPathBoolean(XDocument xmlDocument, string expression,
                IEnumerable<KeyValuePair<string, string>>? namespaces = null)
            {
                throw new NotSupportedException();
            }

            public XDocument Transform(XDocument inputDocument, string xsltPath,
                IReadOnlyDictionary<string, XDocument>? parameters = null)
            {
                return Svrl;
            }
        }
    }
}
