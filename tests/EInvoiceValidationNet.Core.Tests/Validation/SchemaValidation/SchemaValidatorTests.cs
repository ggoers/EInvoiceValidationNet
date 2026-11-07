using System.Xml.Linq;
using EInvoiceValidationNet.Validation.SchemaValidation;

namespace EInvoiceValidationNet.Core.Tests.Validation.SchemaValidation
{
    public class SchemaValidatorTests
    {
        private SchemaValidator _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new SchemaValidator();
        }

        [Test]
        public void Validate_ValidDocument_ShouldHaveNoErrors()
        {
            var invoice = LoadInvoice("valid-invoice.xml");

            var result = _validator.Validate(invoice, XsdPath("invoice.xsd"));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.True);
                Assert.That(result.Errors, Is.Empty);
            });
        }

        [Test]
        public void Validate_DocumentWithSingleError_ShouldHaveOneError()
        {
            var invoice = LoadInvoice("invalid-single-error.xml");

            var result = _validator.Validate(invoice, XsdPath("invoice.xsd"));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Count.EqualTo(1));
                Assert.That(result.Errors[0].Severity, Is.EqualTo(XmlSyntaxErrorSeverity.Error));
                Assert.That(result.Errors[0].Message, Does.Contain("Currency"));
                Assert.That(result.Errors[0].RowNumber, Is.GreaterThan(0));
                Assert.That(result.Errors[0].ColumnNumber, Is.GreaterThan(0));
            });
        }

        [Test]
        public void Validate_DocumentWithMultipleErrors_ShouldHaveAllErrors()
        {
            var invoice = LoadInvoice("invalid-multiple-errors.xml");

            var result = _validator.Validate(invoice, XsdPath("invoice.xsd"));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Count.EqualTo(3));
                Assert.That(result.Errors.All(e => e.Severity == XmlSyntaxErrorSeverity.Error), Is.True);
                Assert.That(result.Errors.Any(e => e.Message.Contains("TotalAmount")), Is.True);
                Assert.That(result.Errors.Any(e => e.Message.Contains("Street")), Is.True);
                Assert.That(result.Errors.Any(e => e.Message.Contains("Currency")), Is.True);
            });
        }

        // The import chain mimics the official UBL 2.1 configuration: the main schema imports a
        // schema document (bare relative location) that carries a <!DOCTYPE> internal subset.
        // The BCL XmlSchemaSet parses resolver-provided schemas with DtdProcessing.Prohibit,
        // which must not break the import (Java/Xerces processes the internal subset).
        [Test]
        public void Validate_SchemaImportingDoctypeSchema_ValidDocument_ShouldHaveNoErrors()
        {
            var invoice = LoadInvoice("dtd-invoice-valid.xml");

            var result = _validator.Validate(invoice, XsdPath("dtd-invoice.xsd"));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.True);
                Assert.That(result.Errors, Is.Empty);
            });
        }

        [Test]
        public void Validate_DoctypeSchemaAsRootSchema_ValidDocument_ShouldHaveNoErrors()
        {
            var money = LoadInvoice("dtd-money-valid.xml");

            var result = _validator.Validate(money, XsdPath("dtd-common.xsd"));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.True);
                Assert.That(result.Errors, Is.Empty);
            });
        }

        // The DOCTYPE entity "currencyCode" is used inside the imported schema. It proves that
        // the DOCTYPE handling preserves semantics (entity expansion) instead of blindly
        // stripping the declaration: "USD" must violate the entity-defined enumeration.
        [Test]
        public void Validate_SchemaImportingDoctypeSchema_EntityValueInvalid_ShouldHaveEnumerationError()
        {
            var invoice = LoadInvoice("dtd-invoice-invalid.xml");

            var result = _validator.Validate(invoice, XsdPath("dtd-invoice.xsd"));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Count.EqualTo(1));
                Assert.That(result.Errors[0].Severity, Is.EqualTo(XmlSyntaxErrorSeverity.Error));
                Assert.That(result.Errors[0].Message, Does.Contain("USD"));
            });
        }

        [Test]
        public void Validate_NotCompilableSchema_ShouldMapExceptionToFatalError()
        {
            var invoice = LoadInvoice("valid-invoice.xml");

            var result = _validator.Validate(invoice, XsdPath("broken.xsd"));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Count.EqualTo(1));
                Assert.That(result.Errors[0].Severity, Is.EqualTo(XmlSyntaxErrorSeverity.FatalError));
                Assert.That(result.Errors[0].Message, Is.Not.Empty);
            });
        }

        [Test]
        public void Validate_NullArguments_ShouldThrow()
        {
            var invoice = LoadInvoice("valid-invoice.xml");

            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentNullException>(() => _validator.Validate(null, XsdPath("invoice.xsd")));
                Assert.Throws<ArgumentNullException>(() => _validator.Validate(invoice, null));
            });
        }

        [Test]
        public void Validate_NotExistingXsdFile_ShouldThrow()
        {
            var invoice = LoadInvoice("valid-invoice.xml");

            Assert.Throws<FileNotFoundException>(() => _validator.Validate(invoice, XsdPath("not-existing.xsd")));
        }

        private static XDocument LoadInvoice(string fileName)
        {
            return XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Testdata", "SchemaValidation", fileName),
                LoadOptions.SetLineInfo);
        }

        private static string XsdPath(string fileName)
        {
            return Path.Combine(AppContext.BaseDirectory, "Testdata", "SchemaValidation", fileName);
        }
    }
}
