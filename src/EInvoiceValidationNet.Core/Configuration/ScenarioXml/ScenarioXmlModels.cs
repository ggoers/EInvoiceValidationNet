using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

// The deserialization models are an implementation detail that System.Xml.Serialization requires to be public;
// they are hidden from IntelliSense via EditorBrowsable and deliberately not documented.
#pragma warning disable CS1591

namespace EInvoiceValidationNet.Core.Configuration.ScenarioXml
{
    //this unit holds the objects which represent the scenarios.xml from the validation configurations (use with XmlSerializer)
    //it is only internal for reading the XML and mapping it to the official public Configuration object
    //these models should be internal since it's an implementation detail and not important for users of this NuGet, but public is unfortunately required by XmlSerializer
    [EditorBrowsable(EditorBrowsableState.Never)]
    [XmlRoot("scenarios", Namespace = Namespace)]
    public class ScenariosConfiguration
    {
        public const string Namespace = "http://www.xoev.de/de/validator/framework/1/scenarios";

        [XmlAttribute("frameworkVersion")]
        public string FrameworkVersion { get; set; }

        [XmlElement("name")]
        public string Name { get; set; }

        [XmlElement("author")]
        public string Author { get; set; }

        [XmlElement("date")]
        public string Date { get; set; }

        [XmlElement("description")]
        public Description Description { get; set; }

        [XmlElement("scenario")]
        public List<Scenario> Scenarios { get; set; }

        [XmlElement("noScenarioReport")]
        public NoScenarioReport NoScenarioReport { get; set; }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class Scenario
    {
        [XmlElement("name")]
        public string Name { get; set; }

        [XmlElement("description")]
        public Description Description { get; set; }

        [XmlElement("namespace")]
        public List<NamespaceMapping> Namespaces { get; set; }

        [XmlElement("match")]
        public string Match { get; set; }

        [XmlElement("validateWithXmlSchema")]
        public ValidationStep XmlSchemaValidation { get; set; }

        [XmlElement("validateWithSchematron")]
        public List<ValidationStep> SchematronValidations { get; set; }

        [XmlElement("createReport")]
        public CreateReport CreateReport { get; set; }

        [XmlElement("acceptMatch")]
        public string AcceptMatch { get; set; }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class Description
    {
        [XmlElement("p")]
        public List<string> Paragraphs { get; set; }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class NamespaceMapping
    {
        [XmlAttribute("prefix")]
        public string Prefix { get; set; }

        [XmlText]
        public string Uri { get; set; }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ValidationStep
    {
        [XmlElement("resource")]
        public ResourceReference Resource { get; set; }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ResourceReference
    {
        [XmlElement("name")]
        public string Name { get; set; }

        [XmlElement("location")]
        public string Location { get; set; }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class CreateReport
    {
        [XmlElement("resource")]
        public ResourceReference Resource { get; set; }

        [XmlElement("customLevel")]
        public List<CustomLevel> CustomLevels { get; set; }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class CustomLevel
    {
        [XmlAttribute("level")]
        public string Level { get; set; }

        [XmlText]
        public string RuleId { get; set; }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class NoScenarioReport
    {
        [XmlElement("resource")]
        public ResourceReference Resource { get; set; }
    }
}
