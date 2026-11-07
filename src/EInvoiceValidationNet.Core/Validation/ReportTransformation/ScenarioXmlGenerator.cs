using System.Linq;
using System.Xml.Linq;
using EInvoiceValidationNet.Configuration;

namespace EInvoiceValidationNet.Validation.ReportTransformation
{
    /// <summary>
    /// Generates the <c>s:scenario</c> element that is embedded in the report input for scenarios that were configured
    /// in code (scenarios read from <c>scenarios.xml</c> embed their original element verbatim instead).
    /// </summary>
    /// <remarks>
    /// The generated element contains everything the scenario configuration defines, with two limitations imposed by
    /// the framework schema: predicate match expressions and scenarios without schema validation cannot be represented,
    /// so their elements are omitted and the report input is not validated against the framework schema.
    /// </remarks>
    internal static class ScenarioXmlGenerator
    {
        public static XElement CreateScenarioElement(Scenario scenario)
        {
            var scenarioNamespace = XNamespace.Get(FrameworkXml.ScenariosNamespace);
            var element = new XElement(scenarioNamespace + "scenario",
                new XElement(scenarioNamespace + "name", scenario.Name ?? string.Empty));

            element.Add(scenario.Namespaces.Select(namespaceDeclaration => new XElement(
                scenarioNamespace + "namespace",
                new XAttribute("prefix", namespaceDeclaration.Prefix),
                namespaceDeclaration.Uri)));

            if (scenario.MatchExpression is XPathMatchExpression matchExpression)
            {
                element.Add(new XElement(scenarioNamespace + "match", matchExpression.XPath));
            }

            if (scenario.SchemaValidation != null)
            {
                element.Add(new XElement(scenarioNamespace + "validateWithXmlSchema",
                    ReportInputBuilder.CreateResourceElement(scenario.SchemaValidation.Name,
                        scenario.SchemaValidation.OriginalLocation)));
            }

            element.Add(scenario.SchematronValidations.Select(schematronValidation => new XElement(
                scenarioNamespace + "validateWithSchematron",
                ReportInputBuilder.CreateResourceElement(schematronValidation.Name,
                    schematronValidation.OriginalLocation))));

            if (scenario.ReportTransformation != null)
            {
                var createReport = new XElement(scenarioNamespace + "createReport",
                    ReportInputBuilder.CreateResourceElement(scenario.ReportTransformation.Name,
                        scenario.ReportTransformation.OriginalLocation));
                createReport.Add(scenario.RuleCustomizations.Select(customization => new XElement(
                    scenarioNamespace + "customLevel",
                    new XAttribute("level", customization.LevelOverwriteAsString),
                    customization.RuleId)));
                element.Add(createReport);
            }

            if (scenario.AcceptMatchExpression is XPathMatchExpression acceptMatchExpression)
            {
                element.Add(new XElement(scenarioNamespace + "acceptMatch", acceptMatchExpression.XPath));
            }

            return element;
        }
    }
}
