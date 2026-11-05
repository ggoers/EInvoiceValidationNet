using EInvoiceValidationNet.Core.Configuration.ScenarioXml;
using EInvoiceValidationNet.Core.Validation.ReportTransformation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;
using EInvoiceValidationNet.Core.Utils;

namespace EInvoiceValidationNet.Core.Configuration
{
    /// <summary>
    /// Builds an immutable <see cref="Configuration"/>, either fluently in code or from a configuration file (<c>scenarios.xml</c>).
    /// </summary>
    /// <remarks>
    /// The builder can be changed after <see cref="Build"/> and built again.
    /// Every call to <see cref="Build"/> creates a new, independent configuration.
    /// </remarks>
    public class ConfigurationBuilder
    {
        private readonly List<Scenario> _scenarios = new List<Scenario>();

        private ReportTransformationConfiguration _noScenarioReportConfiguration;

        /// <summary>
        /// Adds a scenario that is configured with a builder callback.
        /// </summary>
        /// <param name="scenarioBuilder">Required. Configures the new scenario. It must add a match expression and at least
        /// one validation step (XML schema or Schematron).</param>
        /// <returns>This builder, to allow chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="scenarioBuilder"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidConfigurationException">Thrown when the configured scenario is incomplete.</exception>
        public ConfigurationBuilder AddScenario(Action<ScenarioBuilder> scenarioBuilder)
        {
            if (scenarioBuilder is null)
            {
                throw new ArgumentNullException(nameof(scenarioBuilder));
            }

            var builder = new ScenarioBuilder();
            scenarioBuilder(builder);
            _scenarios.Add(builder.Build());

            return this;
        }

        /// <summary>
        /// Adds an already created scenario. Scenarios are evaluated in the order they are added.
        /// </summary>
        /// <param name="scenario">Required. The scenario to add.</param>
        /// <returns>This builder, to allow chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="scenario"/> is <c>null</c>.</exception>
        public ConfigurationBuilder AddScenario(Scenario scenario)
        {
            if (scenario is null)
            {
                throw new ArgumentNullException(nameof(scenario));
            }

            _scenarios.Add(scenario);

            return this;
        }

        /// <summary>
        /// Sets the report transformation that is used when no scenario matches an invoice.
        /// </summary>
        /// <param name="xsltFilePath">Required. Path of the XSLT 2.0 stylesheet that creates the report.</param>
        /// <param name="name">Optional. Human readable name of the report transformation.</param>
        /// <param name="outputDirectory">Optional. Directory the report is written to.
        /// <c>null</c> means the report is created but not saved as a file. Instead you can access it in memory via the ValidationResult.</param>
        /// <returns>This builder, to allow chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="xsltFilePath"/> is <c>null</c>.</exception>
        public ConfigurationBuilder SetNoScenarioReport(string xsltFilePath, string name = null, string outputDirectory = null)
        {
            _noScenarioReportConfiguration = new ReportTransformationConfiguration(xsltFilePath, name, outputDirectory);
            return this;
        }

        /// <summary>
        /// Creates the immutable configuration from the scenarios added to this builder.
        /// </summary>
        /// <returns>A configuration that is independent of this builder.</returns>
        /// <exception cref="InvalidConfigurationException">Thrown when no scenario was added.</exception>
        public Configuration Build()
        {
            return new Configuration(_scenarios, _noScenarioReportConfiguration);
        }

        /// <summary>
        /// Reads a KoSIT <c>scenarios.xml</c> configuration from a stream and maps it to an equivalent <see cref="Configuration"/>.
        /// </summary>
        /// <param name="scenariosXmlStream">Required. The stream containing the <c>scenarios.xml</c> document. It is read from
        /// its current position and is not disposed.</param>
        /// <param name="validationConfigurationRoot">Required. Directory of the validation configuration (normally the directory
        /// containing the <c>scenarios.xml</c>). The relative resource locations in the file are resolved against it.</param>
        /// <param name="outputDirectory">Optional. Directory the reports are written to. <c>null</c> means the reports are not saved as a file.
        /// Instead you can access it in memory via the ValidationResult.</param>
        /// <returns>The mapped configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="scenariosXmlStream"/> is <c>null</c>.</exception>
        /// <exception cref="System.InvalidOperationException">Thrown when the stream does not contain a valid <c>scenarios.xml</c> document.</exception>
        public static Configuration FromScenariosXml(
            Stream scenariosXmlStream,
            string validationConfigurationRoot,
            string outputDirectory = null)
        {
            string BuildAbsoluteLocationPath(string location)
            {
                //normalize to current operating system separator 
                var path = Path
                    .Combine(validationConfigurationRoot.Trim(), location.Trim())
                    .NormalizePath();

                return path;
            }

            Scenario MapScenario(ScenarioXml.Scenario scenario)
            {
                SchemaValidationConfiguration schemaValidation = null;
                if (!(scenario.XmlSchemaValidation is null))
                {
                    schemaValidation = new SchemaValidationConfiguration(
                        BuildAbsoluteLocationPath(scenario.XmlSchemaValidation.Resource.Location),
                        scenario.XmlSchemaValidation.Resource?.Name)
                    {
                        OriginalLocation = scenario.XmlSchemaValidation.Resource.Location
                    };
                }

                var schematronValidations = new List<SchematronValidationConfiguration>();
                if (!(scenario.SchematronValidations is null))
                {
                    schematronValidations = scenario.SchematronValidations.Select(schematronValidation =>
                        new SchematronValidationConfiguration(
                            BuildAbsoluteLocationPath(schematronValidation.Resource.Location),
                            schematronValidation.Resource?.Name)
                        {
                            OriginalLocation = schematronValidation.Resource.Location
                        }).ToList();
                }

                ReportTransformationConfiguration reportTransformation = null;
                if (!(scenario.CreateReport is null))
                {
                    reportTransformation = new ReportTransformationConfiguration(
                        BuildAbsoluteLocationPath(scenario.CreateReport.Resource.Location),
                        scenario.CreateReport.Resource?.Name, outputDirectory)
                    {
                        OriginalLocation = scenario.CreateReport.Resource.Location
                    };
                }

                var ruleCustomizations = Enumerable.Empty<RuleCustomization>();
                if (!(scenario.CreateReport?.CustomLevels is null))
                {
                    ruleCustomizations = scenario.CreateReport.CustomLevels.Select(customLevel => new RuleCustomization(customLevel.RuleId, LevelExtensions.GetLevelFromString(customLevel.Level)));
                }

                var namespaces = Enumerable.Empty<NamespaceDeclaration>();
                if (!(scenario.Namespaces is null))
                {
                    namespaces = scenario.Namespaces.Select(namespaceMapping => new NamespaceDeclaration(namespaceMapping.Prefix, namespaceMapping.Uri));
                }

                MatchExpression acceptMatchExpression = null;
                if (!string.IsNullOrWhiteSpace(scenario.AcceptMatch))
                {
                    acceptMatchExpression = new XPathMatchExpression(scenario.AcceptMatch);
                }

                return new Scenario(
                    new XPathMatchExpression(scenario.Match),
                    schemaValidation,
                    schematronValidations,
                    scenario.Name,
                    reportTransformation,
                    ruleCustomizations,
                    acceptMatchExpression,
                    namespaces);
            }

            using (var scenariosXmlCopy = new MemoryStream())
            {
                scenariosXmlStream.CopyTo(scenariosXmlCopy);
                scenariosXmlCopy.Position = 0;

                var serializer = new XmlSerializer(typeof(ScenariosConfiguration));
                if (!(serializer.Deserialize(scenariosXmlCopy) is ScenariosConfiguration scenarios))
                {
                    throw new InvalidOperationException("Deserialization of scenarios.xml was not possible");
                }

                ReportTransformationConfiguration noScenarioReport = null;
                if (!(scenarios.NoScenarioReport is null))
                {
                    noScenarioReport = new ReportTransformationConfiguration(
                        BuildAbsoluteLocationPath(scenarios.NoScenarioReport.Resource.Location),
                        scenarios.NoScenarioReport.Resource?.Name,
                        outputDirectory)
                    {
                        OriginalLocation = scenarios.NoScenarioReport.Resource.Location
                    };
                }

                var mappedScenarios = scenarios.Scenarios.Select(MapScenario).ToList();
                EmbedRawScenarioElements(scenariosXmlCopy, mappedScenarios);

                return new Configuration(mappedScenarios, noScenarioReport);
            }
        }

        private static void EmbedRawScenarioElements(MemoryStream scenariosXmlCopy, List<Scenario> mappedScenarios)
        {
            scenariosXmlCopy.Position = 0;
            var document = XDocument.Load(scenariosXmlCopy);
            var scenarioNamespace = XNamespace.Get(FrameworkXml.ScenariosNamespace);
            var rawScenarioElements = document.Root?.Elements(scenarioNamespace + "scenario").ToList();

            if (rawScenarioElements is null || rawScenarioElements.Count != mappedScenarios.Count)
            {
                throw new InvalidOperationException(
                    "The number of deserialized scenarios does not match the number of scenario elements in scenarios.xml");
            }

            for (var i = 0; i < mappedScenarios.Count; i++)
            {
                mappedScenarios[i].RawScenarioElement = rawScenarioElements[i];
            }
        }

        /// <summary>
        /// Reads a KoSIT <c>scenarios.xml</c> file and maps it to an equivalent <see cref="Configuration"/>.
        /// </summary>
        /// <param name="scenariosXmlPath">Required. Path of the <c>scenarios.xml</c> file.</param>
        /// <param name="validationConfigurationRoot">Required. Directory of the validation configuration (normally the directory
        /// containing the <c>scenarios.xml</c>); the relative resource locations in the file are resolved against it.</param>
        /// <param name="outputDirectory">Optional. Directory the reports are written to. <c>null</c> means the reports are not saved as a file.
        /// Instead you can access it in memory via the ValidationResult.</param>
        /// <returns>The mapped configuration.</returns>
        /// <exception cref="System.IO.FileNotFoundException">Thrown when <paramref name="scenariosXmlPath"/> does not exist.</exception>
        /// <exception cref="System.IO.IOException">Thrown when the file cannot be read.</exception>
        /// <exception cref="System.InvalidOperationException">Thrown when the file does not contain a valid <c>scenarios.xml</c> document.</exception>
        public static Configuration FromScenariosXml(
            string scenariosXmlPath,
            string validationConfigurationRoot,
            string outputDirectory = null)
        {
            using (var fileStream = File.OpenRead(scenariosXmlPath))
            {
                return FromScenariosXml(fileStream, validationConfigurationRoot, outputDirectory);
            }
        }
    }

    /// <summary>
    /// Fluent builder for a single <see cref="Scenario"/>, used by <see cref="ConfigurationBuilder.AddScenario(Action{ScenarioBuilder})"/>.
    /// </summary>
    /// <remarks>
    /// All methods return the builder, so calls can be chained.
    /// </remarks>
    public class ScenarioBuilder
    {
        private string _name;

        private MatchExpression _matchExpression;

        private MatchExpression _acceptMatchExpression;

        private readonly List<NamespaceDeclaration> _namespaceDeclarations = new List<NamespaceDeclaration>();

        private SchemaValidationConfiguration _schemaValidationConfiguration;

        private readonly List<SchematronValidationConfiguration> _schematronValidationConfigurations = new List<SchematronValidationConfiguration>();

        private ReportTransformationConfiguration _reportTransformationConfiguration;

        private readonly List<RuleCustomization> _ruleCustomizations = new List<RuleCustomization>();

        /// <summary>
        /// Sets the descriptive name of the scenario.
        /// </summary>
        /// <param name="name">Optional and only informative, e.g. the invoice format the scenario applies to. It is trimmed.</param>
        /// <returns>This builder, to allow chaining.</returns>
        public ScenarioBuilder SetName(string name)
        {
            _name = name;
            return this;
        }

        /// <summary>
        /// Sets the XPath expression that decides whether the scenario applies to an invoice (scenario <c>match</c>).
        /// </summary>
        /// <param name="xPath">Required. An XPath 2.0 expression that returns a boolean. The declared namespaces are available as prefixes.</param>
        /// <returns>This builder, to allow chaining.</returns>
        /// <exception cref="InvalidConfigurationException">Thrown when <paramref name="xPath"/> is <c>null</c>, empty or whitespace.</exception>
        public ScenarioBuilder AddXPathMatchExpression(string xPath)
        {
            _matchExpression = new XPathMatchExpression(xPath);
            return this;
        }

        /// <summary>
        /// Sets a .NET predicate that decides whether the scenario applies to an invoice (code-defined alternative to the
        /// XPath <c>match</c> expression).
        /// </summary>
        /// <param name="predicate">Required. Called with the parsed invoice document; it should return <c>false</c> for
        /// documents that do not match instead of throwing.</param>
        /// <returns>This builder, to allow chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is <c>null</c>.</exception>
        public ScenarioBuilder AddPredicateMatchExpression(Func<XDocument, bool> predicate)
        {
            _matchExpression = new PredicateMatchExpression(predicate);
            return this;
        }

        /// <summary>
        /// Sets the XPath expression that decides whether an invoice is accepted, evaluated on the generated report
        /// (scenario <c>acceptMatch</c>).
        /// </summary>
        /// <param name="xPath">Required. An XPath 2.0 expression that returns a boolean. The declared namespaces are available as prefixes.</param>
        /// <returns>This builder, to allow chaining.</returns>
        /// <exception cref="InvalidConfigurationException">Thrown when <paramref name="xPath"/> is <c>null</c>, empty or whitespace.</exception>
        public ScenarioBuilder AddXPathAcceptMatchExpression(string xPath)
        {
            _acceptMatchExpression = new XPathMatchExpression(xPath);
            return this;
        }

        /// <summary>
        /// Sets a .NET predicate that decides whether an invoice is accepted (code-defined alternative to the XPath
        /// <c>acceptMatch</c> expression).
        /// </summary>
        /// <param name="predicate">Required. Called with the generated report document. It should return <c>false</c> for
        /// reports that do not represent an acceptable invoice instead of throwing.</param>
        /// <returns>This builder, to allow chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is <c>null</c>.</exception>
        public ScenarioBuilder AddPredicateAcceptMatchExpression(Func<XDocument, bool> predicate)
        {
            _acceptMatchExpression = new PredicateMatchExpression(predicate);
            return this;
        }

        /// <summary>
        /// Declares a namespace prefix that is available to the XPath expressions of the scenario
        /// (scenario <c>namespace</c> element).
        /// </summary>
        /// <param name="prefix">Required. The prefix. it must be unique within the scenario and is trimmed.</param>
        /// <param name="uri">Required. The namespace URI the prefix stands for. it is trimmed.</param>
        /// <returns>This builder, to allow chaining.</returns>
        /// <exception cref="InvalidConfigurationException">Thrown when <paramref name="prefix"/> or <paramref name="uri"/> is
        /// <c>null</c>, empty or whitespace, or when the prefix is declared twice.</exception>
        public ScenarioBuilder DeclareNamespace(string prefix, string uri)
        {
            _namespaceDeclarations.Add(new NamespaceDeclaration(prefix, uri));
            return this;
        }

        /// <summary>
        /// Sets the XML schema (XSD) validation step of the scenario (scenario <c>validateWithXmlSchema</c>).
        /// </summary>
        /// <param name="xsdFilePath">Required. Path of the main XSD file. Relative imports used in the main file are resolved automatically.</param>
        /// <param name="name">Optional. Human readable name of the validation step.</param>
        /// <returns>This builder, to allow chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="xsdFilePath"/> is <c>null</c>.</exception>
        public ScenarioBuilder SetSchemaValidation(string xsdFilePath, string name = null)
        {
            _schemaValidationConfiguration = new SchemaValidationConfiguration(xsdFilePath, name);
            return this;
        }

        /// <summary>
        /// Adds a Schematron validation step. Steps are executed in the order they are added.
        /// </summary>
        /// <param name="xsltFilePath">Required. Path of the compiled Schematron stylesheet.</param>
        /// <param name="name">Optional. Human readable name of the validation step.</param>
        /// <returns>This builder, to allow chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="xsltFilePath"/> is <c>null</c>.</exception>
        public ScenarioBuilder AddSchematronValidation(string xsltFilePath, string name = null)
        {
            _schematronValidationConfigurations.Add(new SchematronValidationConfiguration(xsltFilePath, name));
            return this;
        }

        /// <summary>
        /// Sets the report transformation of the scenario (scenario <c>createReport</c>).
        /// </summary>
        /// <param name="xsltFilePath">Required. Path of the XSLT stylesheet that creates the report.</param>
        /// <param name="name">Optional. Human readable name of the report transformation.</param>
        /// <param name="outputDirectory">Optional. Directory the report is written to. <c>null</c> means the report is not saved as a file.
        /// Instead you can access it in memory via the ValidationResult.</param>
        /// <returns>This builder, to allow chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="xsltFilePath"/> is <c>null</c>.</exception>
        public ScenarioBuilder SetReportTransformation(string xsltFilePath, string name = null, string outputDirectory = null)
        {
            _reportTransformationConfiguration = new ReportTransformationConfiguration(xsltFilePath, name, outputDirectory);
            return this;
        }

        /// <summary>
        /// Adds a level override for a single business rule (scenario <c>customLevel</c>). Overrides can be added
        /// once per rule.
        /// </summary>
        /// <param name="ruleName">Required. Identifier of the business rule, e.g. <c>BR-CL-23</c>.</param>
        /// <param name="levelOverwrite">Required. The level reported for the rule instead of its original severity.</param>
        /// <returns>This builder, to allow chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="ruleName"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="levelOverwrite"/> is not a defined <see cref="Level"/> value.</exception>
        public ScenarioBuilder AddRuleCustomization(string ruleName, Level levelOverwrite)
        {
            _ruleCustomizations.Add(new RuleCustomization(ruleName, levelOverwrite));
            return this;
        }

        /// <summary>
        /// Creates the scenario from the values set on this builder.
        /// </summary>
        /// <returns>A new, immutable scenario.</returns>
        /// <exception cref="InvalidConfigurationException">Thrown when no match expression or no validation step
        /// (XML schema or Schematron) was configured, or when a namespace prefix was declared twice.</exception>
        public Scenario Build()
        {
            return new Scenario(
                _matchExpression,
                _schemaValidationConfiguration,
                _schematronValidationConfigurations,
                _name,
                _reportTransformationConfiguration,
                _ruleCustomizations,
                _acceptMatchExpression,
                _namespaceDeclarations);
        }
    }
}
