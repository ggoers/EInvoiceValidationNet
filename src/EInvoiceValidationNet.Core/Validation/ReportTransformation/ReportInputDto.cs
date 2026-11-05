using System.ComponentModel;
using System.Xml.Linq;
using System.Xml.Serialization;
using EInvoiceValidationNet.Core.Validation.SchematronValidation.SVRLParsing;

// The serialization models are an implementation detail that System.Xml.Serialization requires to be public;
// they are hidden from IntelliSense via EditorBrowsable and deliberately not documented.
#pragma warning disable CS1591

namespace EInvoiceValidationNet.Core.Validation.ReportTransformation
{
    //this unit holds the objects which represent the intermediate createReportInput document (use with XmlSerializer)
    //they are only needed to serialize the report input and are mapped from the public validation result model
    //these models should be internal since it's an implementation detail and not important for users of this NuGet, but public is unfortunately required by XmlSerializer
    [EditorBrowsable(EditorBrowsableState.Never)]
    [XmlRoot("createReportInput", Namespace = FrameworkXml.ReportInputNamespace)]
    public class CreateReportInputDto
    {
        [XmlAttribute("frameworkVersion")]
        public string FrameworkVersion { get; set; }

        [XmlElement("engine")]
        public EngineDto Engine { get; set; }

        [XmlElement("timestamp")]
        public string Timestamp { get; set; }

        [XmlElement("documentIdentification")]
        public DocumentIdentificationDto DocumentIdentification { get; set; }

        [XmlAnyElement("scenario", Namespace = FrameworkXml.ScenariosNamespace)]
        public XElement Scenario { get; set; }

        [XmlElement("validationResultsWellformedness")]
        public ValidationResultsWellformednessDto ValidationResultsWellformedness { get; set; }

        [XmlElement("validationResultsXmlSchema")]
        public ValidationResultsXmlSchemaDto ValidationResultsXmlSchema { get; set; }

        [XmlElement("validationResultsSchematron")]
        public ValidationResultsSchematronDto[] ValidationResultsSchematron { get; set; }

        [XmlElement("processingError")]
        public ProcessingErrorDto ProcessingError { get; set; }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class EngineDto
    {
        [XmlElement("name")]
        public string Name { get; set; }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class DocumentIdentificationDto
    {
        [XmlElement("documentHash")]
        public DocumentHashDto DocumentHash { get; set; }

        [XmlElement("documentReference")]
        public string DocumentReference { get; set; }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class DocumentHashDto
    {
        [XmlElement("hashAlgorithm")]
        public string HashAlgorithm { get; set; }

        [XmlElement("hashValue")]
        public byte[] HashValue { get; set; }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ValidationResultsWellformednessDto
    {
        [XmlElement("xmlSyntaxError")]
        public XmlSyntaxErrorDto[] XmlSyntaxErrors { get; set; }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ValidationResultsXmlSchemaDto
    {
        [XmlAnyElement("resource", Namespace = FrameworkXml.ScenariosNamespace)]
        public XElement[] Resources { get; set; }

        [XmlElement("xmlSyntaxError")]
        public XmlSyntaxErrorDto[] XmlSyntaxErrors { get; set; }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ValidationResultsSchematronDto
    {
        [XmlAnyElement("resource", Namespace = FrameworkXml.ScenariosNamespace)]
        public XElement Resource { get; set; }

        [XmlElement("results")]
        public SchematronResultsDto Results { get; set; }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class SchematronResultsDto
    {
        [XmlAnyElement("schematron-output", Namespace = SvrlSchematronOutput.Namespace)]
        public XElement SchematronOutput { get; set; }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class XmlSyntaxErrorDto
    {
        [XmlElement("message")]
        public string Message { get; set; }

        [XmlElement("severityCode")]
        public ReportInputSeverityDto SeverityCode { get; set; }

        [XmlElement("rowNumber")]
        public int RowNumber { get; set; }

        [XmlIgnore]
        public bool RowNumberSpecified { get; set; }

        [XmlElement("columnNumber")]
        public int ColumnNumber { get; set; }

        [XmlIgnore]
        public bool ColumnNumberSpecified { get; set; }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public enum ReportInputSeverityDto
    {
        [XmlEnum("SEVERITY_WARNING")]
        Warning,

        [XmlEnum("SEVERITY_ERROR")]
        Error,

        [XmlEnum("SEVERITY_FATAL_ERROR")]
        FatalError
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ProcessingErrorDto
    {
        [XmlElement("error")]
        public string[] Errors { get; set; }
    }
}
