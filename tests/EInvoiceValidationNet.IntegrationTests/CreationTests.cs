using EInvoiceValidationNet.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace EInvoiceValidationNet.IntegrationTests
{
    public class CreationTests : IntegrationTestBase
    {
        [Test]
        public void CreateWithDependencyInjection_Singleton_Validates()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddEInvoiceValidator();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var validator = serviceProvider.GetRequiredService<IValidator>();

            var result = ValidateInvoice(validator, "cii_success.xml");

            Assert.That(result.Status, Is.EqualTo(ValidationProcessStatus.Completed));
            Assert.That(result.IsAccepted, Is.True);
        }

        [Test]
        public void CreateWithDependencyInjection_Transient_Validates()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddEInvoiceValidatorTransient();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var validator = serviceProvider.GetRequiredService<IValidator>();

            var result = ValidateInvoice(validator, "cii_success.xml");

            Assert.That(result.Status, Is.EqualTo(ValidationProcessStatus.Completed));
            Assert.That(result.IsAccepted, Is.True);
        }

        [Test]
        public void CreateWithFactory_Validates()
        {
            var factory = new ValidatorFactory();
            var validator = factory.CreateValidator();

            var result = ValidateInvoice(validator, "cii_success.xml");

            Assert.That(result.Status, Is.EqualTo(ValidationProcessStatus.Completed));
            Assert.That(result.IsAccepted, Is.True);
        }
    }
}
