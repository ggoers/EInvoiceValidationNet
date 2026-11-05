using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;
using EInvoiceValidationNet.Core.Validation.SchemaValidation;
using EInvoiceValidationNet.Core.Validation.SchematronValidation.SVRLParsing;

namespace EInvoiceValidationNet.Core.Validation.ReportTransformation
{
    /// <summary>
    /// Builds the intermediate <c>createReportInput</c> document that the report transformations consume from the
    /// current state of a <see cref="ValidationContext"/>.
    /// </summary>
    internal sealed class ReportInputBuilder
    {
        private const string TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(CreateReportInputDto));

        private static readonly XmlSerializerNamespaces SerializerNamespaces = CreateSerializerNamespaces();

        private readonly ValidationContext _validationContext;

        /// <summary>
        /// Initializes the builder for the current validation run.
        /// </summary>
        /// <param name="validationContext">Required. Provides the document identification, the validation results and
        /// the processing errors embedded in the report input.</param>
        public ReportInputBuilder(ValidationContext validationContext)
        {
            _validationContext = validationContext ?? throw new ArgumentNullException(nameof(validationContext));
        }

        /// <summary>
        /// Builds the intermediate report input document from the current state of the validation context.
        /// </summary>
        /// <returns>The serialized <c>createReportInput</c> document; never <c>null</c>.</returns>
        public XDocument Build()
        {
            if (_validationContext.DocumentHash is null)
            {
                throw new InvalidOperationException("The document hash must be set in the validation context");
            }

            var reportInput = new CreateReportInputDto
            {
                FrameworkVersion = EngineInformation.FrameworkVersion,
                Engine = new EngineDto { Name = EngineInformation.EngineName },
                Timestamp = DateTime.UtcNow.ToString(TimestampFormat, CultureInfo.InvariantCulture),
                DocumentIdentification = new DocumentIdentificationDto
                {
                    DocumentHash = new DocumentHashDto
                    {
                        HashAlgorithm = FrameworkXml.HashAlgorithm,
                        HashValue = _validationContext.DocumentHash
                    },
                    DocumentReference = _validationContext.DocumentReference
                },
                Scenario = CreateScenarioElement(),
                ValidationResultsWellformedness = new ValidationResultsWellformednessDto
                {
                    XmlSyntaxErrors = MapSyntaxErrors(CreateWellFormednessErrors())
                },
                ValidationResultsXmlSchema = CreateSchemaValidationSection(),
                ValidationResultsSchematron = _validationContext.SchematronValidations
                    .Select(schematronValidation => new ValidationResultsSchematronDto
                    {
                        Resource = CreateResourceElement(schematronValidation.Configuration.Name,
                            schematronValidation.Configuration.OriginalLocation),
                        Results = new SchematronResultsDto
                        {
                            SchematronOutput =
                                CreateFrameworkConformingSchematronOutput(schematronValidation.Result.RawSvrl)
                        }
                    })
                    .ToArray(),
                ProcessingError = _validationContext.ProcessingErrors.Count > 0
                    ? new ProcessingErrorDto { Errors = _validationContext.ProcessingErrors.ToArray() }
                    : null
            };

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, reportInput, SerializerNamespaces);

                stream.Position = 0;
                return XDocument.Load(stream);
            }
        }

        private XElement CreateScenarioElement()
        {
            var scenario = _validationContext.MatchingScenario;
            if (scenario is null)
            {
                return null;
            }

            return scenario.RawScenarioElement ?? ScenarioXmlGenerator.CreateScenarioElement(scenario);
        }

        private IEnumerable<XmlSyntaxError> CreateWellFormednessErrors()
        {
            var wellFormednessResult = _validationContext.WellFormednessValidationResult;
            if (wellFormednessResult is null || wellFormednessResult.IsValid)
            {
                return Enumerable.Empty<XmlSyntaxError>();
            }

            return new[]
            {
                new XmlSyntaxError(wellFormednessResult.Error.Message, XmlSyntaxErrorSeverity.FatalError, null, null)
            };
        }

        private ValidationResultsXmlSchemaDto CreateSchemaValidationSection()
        {
            var schemaValidation = _validationContext.MatchingScenario?.SchemaValidation;
            if (schemaValidation is null)
            {
                return null;
            }

            var schemaValidationResult = _validationContext.SchemaValidationResult;
            var errors = schemaValidationResult != null && !schemaValidationResult.IsValid
                ? schemaValidationResult.Errors
                : Enumerable.Empty<XmlSyntaxError>();

            return new ValidationResultsXmlSchemaDto
            {
                Resources = new[] { CreateResourceElement(schemaValidation.Name, schemaValidation.OriginalLocation) },
                XmlSyntaxErrors = MapSyntaxErrors(errors)
            };
        }

        private static XmlSyntaxErrorDto[] MapSyntaxErrors(IEnumerable<XmlSyntaxError> errors)
        {
            return errors.Select(error => new XmlSyntaxErrorDto
            {
                Message = error.Message,
                SeverityCode = MapSeverity(error.Severity),
                RowNumber = error.RowNumber ?? 0,
                RowNumberSpecified = error.RowNumber.HasValue,
                ColumnNumber = error.ColumnNumber ?? 0,
                ColumnNumberSpecified = error.ColumnNumber.HasValue
            }).ToArray();
        }

        private static ReportInputSeverityDto MapSeverity(XmlSyntaxErrorSeverity severity)
        {
            switch (severity)
            {
                case XmlSyntaxErrorSeverity.Warning:
                    return ReportInputSeverityDto.Warning;
                case XmlSyntaxErrorSeverity.Error:
                    return ReportInputSeverityDto.Error;
                case XmlSyntaxErrorSeverity.FatalError:
                    return ReportInputSeverityDto.FatalError;
                default:
                    throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown xml syntax error severity");
            }
        }

        internal static XElement CreateResourceElement(string name, string location)
        {
            var scenarioNamespace = XNamespace.Get(FrameworkXml.ScenariosNamespace);
            return new XElement(scenarioNamespace + "resource",
                new XElement(scenarioNamespace + "name", name ?? string.Empty),
                new XElement(scenarioNamespace + "location", location ?? string.Empty));
        }

        private static XElement CreateFrameworkConformingSchematronOutput(XDocument rawSvrl)
        {
            var svrlNamespace = XNamespace.Get(SvrlSchematronOutput.Namespace);
            if (rawSvrl.Root is null)
            {
                return new XElement(svrlNamespace + "schematron-output");
            }

            var conformingOutput = new XElement(rawSvrl.Root);
            var nonFrameworkElements = conformingOutput.Elements()
                .Where(element => !FrameworkSchematronOutputChildren.Contains(element.Name))
                .ToList();
            foreach (var nonFrameworkElement in nonFrameworkElements)
            {
                nonFrameworkElement.Remove();
            }

            return conformingOutput;
        }

        private static readonly HashSet<XName> FrameworkSchematronOutputChildren = new HashSet<XName>
        {
            XNamespace.Get(SvrlSchematronOutput.Namespace) + "text",
            XNamespace.Get(SvrlSchematronOutput.Namespace) + "ns-prefix-in-attribute-values",
            XNamespace.Get(SvrlSchematronOutput.Namespace) + "active-pattern",
            XNamespace.Get(SvrlSchematronOutput.Namespace) + "fired-rule",
            XNamespace.Get(SvrlSchematronOutput.Namespace) + "failed-assert",
            XNamespace.Get(SvrlSchematronOutput.Namespace) + "successful-report"
        };

        private static XmlSerializerNamespaces CreateSerializerNamespaces()
        {
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("in", FrameworkXml.ReportInputNamespace);
            namespaces.Add("s", FrameworkXml.ScenariosNamespace);
            namespaces.Add("svrl", SvrlSchematronOutput.Namespace);

            return namespaces;
        }
    }
}
