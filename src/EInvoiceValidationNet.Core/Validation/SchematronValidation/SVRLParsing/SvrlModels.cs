using System.ComponentModel;
using System.Xml;
using System.Xml.Serialization;

// The deserialization models are an implementation detail that System.Xml.Serialization requires to be public;
// they are hidden from IntelliSense via EditorBrowsable and deliberately not documented.
#pragma warning disable CS1591

namespace EInvoiceValidationNet.Validation.SchematronValidation.SVRLParsing
{
    //this unit holds the objects which represent the SVRL document produced by a schematron transformation (use with XmlSerializer)
    //it is only internal for reading the XML and mapping it to the official public SchematronValidationResult
    //these models should be internal since it's an implementation detail and not important for users of this NuGet, but public is unfortunately required by XmlSerializer
    [EditorBrowsable(EditorBrowsableState.Never)]
    [XmlRoot("schematron-output", Namespace = Namespace)]
    public class SvrlSchematronOutput
    {
        public const string Namespace = "http://purl.oclc.org/dsdl/svrl";

        [XmlElement("failed-assert", Namespace = Namespace)]
        public SvrlFailedAssert[] FailedAsserts { get; set; }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class SvrlFailedAssert
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("flag")]
        public string Flag { get; set; }

        [XmlAttribute("location")]
        public string Location { get; set; }

        [XmlAttribute("test")]
        public string Test { get; set; }

        [XmlElement("text", Namespace = SvrlSchematronOutput.Namespace)]
        public SvrlText Text { get; set; }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class SvrlText
    {
        [XmlAnyElement]
        public XmlElement[] Elements { get; set; }

        [XmlText]
        public string Value { get; set; }
    }
}
