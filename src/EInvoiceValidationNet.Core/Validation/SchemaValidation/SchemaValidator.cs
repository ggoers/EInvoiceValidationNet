using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace EInvoiceValidationNet.Core.Validation.SchemaValidation
{
    /// <summary>
    /// Schema validation based on the BCL <see cref="System.Xml.Schema"/> namespace.
    /// Warning/Error from the validation events, FatalError from exceptions thrown during schema compilation or validation.
    /// </summary>
    internal class SchemaValidator : ISchemaValidator
    {
        private readonly ConcurrentDictionary<string, Lazy<XmlSchemaSet>> _compiledXmlSchemaSetCache = new ConcurrentDictionary<string, Lazy<XmlSchemaSet>>(StringComparer.OrdinalIgnoreCase);

        public SchemaValidationResult Validate(XDocument invoiceXml, string xsdFilePath)
        {

            if (invoiceXml is null)
            {
                throw new ArgumentNullException(nameof(invoiceXml));
            }

            if (xsdFilePath is null)
            {
                throw new ArgumentNullException(nameof(xsdFilePath));
            }

            if (!File.Exists(xsdFilePath))
            {
                throw new FileNotFoundException("The XSD file does not exist", xsdFilePath);
            }

            try
            {
                var key = Path.GetFullPath(xsdFilePath);
                var compiledSchemaSetLazy = _compiledXmlSchemaSetCache.GetOrAdd(key, (keyPath) =>
                {
                    return new Lazy<XmlSchemaSet>(() =>
                    {
                        // XmlSchemaSet.XmlResolver defaults to null in modern .NET (unlike .NET Framework),
                        // so relative xsd:import/xsd:include schema locations would not be resolved without an explicit resolver.
                        var schemaSet = new XmlSchemaSet
                        {
                            XmlResolver = new XmlUrlResolver()
                        };

                        // The schema document is read with our own reader: XmlSchemaSet reads resolver-supplied
                        // documents (transitive xsd:import/xsd:include) with an internal DtdProcessing.Prohibit
                        // reader, rejecting schema documents carrying a <!DOCTYPE> internal subset (e.g. the W3C
                        // "UBL-xmldsig-core-schema-2.1.xsd" shipped with the official XRechnung configuration) -
                        // via xsd:import the failure is even swallowed silently, surfacing later as a misleading
                        // "element is not declared" compile error. Reading the root schema with a DtdProcessing.Parse
                        // reader makes the set apply these reader settings to the imported/included schema documents
                        // as well
                        // XmlResolver = null keeps external DTD/entity access disabled for security reasons.
                        var readerSettings = new XmlReaderSettings
                        {
                            DtdProcessing = DtdProcessing.Parse,
                            XmlResolver = null
                        };
                        using (var schemaReader = XmlReader.Create(keyPath, readerSettings))
                        {
                            schemaSet.Add(null, schemaReader);
                        }

                        schemaSet.Compile();

                        return schemaSet;
                    });
                });

                XmlSchemaSet compiledSchemaSet;
                try
                {
                    compiledSchemaSet = compiledSchemaSetLazy.Value;
                }
                catch
                {
                    _compiledXmlSchemaSetCache.TryRemove(key, out _);
                    throw;
                }

                var errors = new List<XmlSyntaxError>();
                var settings = new XmlReaderSettings
                {
                    ValidationType = ValidationType.Schema,
                    Schemas = compiledSchemaSet
                };
                settings.ValidationEventHandler += (sender, args) => errors.Add(MapError(args));

                using (var documentReader = invoiceXml.CreateReader())
                using (var validatingReader = XmlReader.Create(documentReader, settings))
                {
                    while (validatingReader.Read())
                    {
                    }
                }

                var hasErrors = errors.Any(e => e.Severity != XmlSyntaxErrorSeverity.Warning);
                return new SchemaValidationResult(!hasErrors, errors);
            }
            catch (Exception e)
            {
                var fatalError = new XmlSyntaxError(e.Message, XmlSyntaxErrorSeverity.FatalError, null, null);
                return new SchemaValidationResult(false, new[] { fatalError });
            }
        }

        private static XmlSyntaxError MapError(ValidationEventArgs args)
        {
            var severity = args.Severity == XmlSeverityType.Warning
                ? XmlSyntaxErrorSeverity.Warning
                : XmlSyntaxErrorSeverity.Error;

            int? rowNumber = null;
            int? columnNumber = null;
            if (args.Exception is XmlSchemaException schemaException)
            {
                if (schemaException.LineNumber > 0)
                {
                    rowNumber = schemaException.LineNumber;
                }

                if (schemaException.LinePosition > 0)
                {
                    columnNumber = schemaException.LinePosition;
                }
            }

            return new XmlSyntaxError(args.Message, severity, rowNumber, columnNumber);
        }
    }
}
