using System;

namespace EInvoiceValidationNet.Configuration
{
    /// <summary>
    /// Signals that a configuration is incomplete or inconsistent, e.g. a scenario without a validation step,
    /// a missing match expression or an empty resource path.
    /// </summary>
    public class InvalidConfigurationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the exception.
        /// </summary>
        /// <param name="message">Description of the configuration problem.</param>
        public InvalidConfigurationException(string message) : base(message) { }
    }
}
