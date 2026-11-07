using EInvoiceValidationNet.Validation;
using EInvoiceValidationNet.Validation.SchematronValidation;
using EInvoiceValidationNet.Validation.SchemaValidation;
using EInvoiceValidationNet.XmlProcessing;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers the e-invoice validator in an <see cref="IServiceCollection"/>.
/// </summary>
/// <remarks>
/// A validator owns the caches of its compiled XSLT, XPath and XSD artifacts. Its lifetime therefore is the
/// lifetime of these caches.
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IValidator"/> as singleton: one validator and one cache for the whole application.
    /// This is the recommended registration. The validator is thread-safe.
    /// </summary>
    public static IServiceCollection AddEInvoiceValidator(this IServiceCollection services)
        => AddEInvoiceValidator(services, ServiceLifetime.Singleton);

    /// <summary>
    /// Registers <see cref="IValidator"/> as transient: every resolved validator has its own, initially empty cache.
    /// </summary>
    /// <remarks>
    /// Only use this when the validator is resolved once and then kept, e.g. injected into your own singleton
    /// service. Useful if you don´t want to make your main project dependent of this library. Register as transient and resolve
    /// a new instance for every use is not recommended, since the caching is not used then.
    /// </remarks>
    public static IServiceCollection AddEInvoiceValidatorTransient(this IServiceCollection services)
        => AddEInvoiceValidator(services, ServiceLifetime.Transient);

    private static IServiceCollection AddEInvoiceValidator(IServiceCollection services, ServiceLifetime validatorLifetime)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTransient<IXmlProcessing, SaxonXmlProcessing>();
        services.Add(ServiceDescriptor.Describe(
            typeof(IValidator),
            serviceProvider =>
            {
                var xmlProcessing = serviceProvider.GetRequiredService<IXmlProcessing>();
                return new Validator(xmlProcessing, new SchemaValidator(), new SchematronValidator(xmlProcessing));
            },
            validatorLifetime));

        return services;
    }
}