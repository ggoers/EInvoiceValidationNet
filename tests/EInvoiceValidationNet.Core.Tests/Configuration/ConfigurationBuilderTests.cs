using System.Diagnostics.CodeAnalysis;
using EInvoiceValidationNet.Core.Configuration;
using EInvoiceValidationNet.Core.Utils;

namespace EInvoiceValidationNet.Core.Tests.Configuration
{
    public class ConfigurationBuilderTests
    {
        [Test]
        public void FromScenariosXml_ShouldMapFieldsCorrectly()
        {
            var testFilePath = Path.Combine(AppContext.BaseDirectory, "Testdata\\scenarios.xml").NormalizePath();

            var configuration = ConfigurationBuilder.FromScenariosXml(testFilePath, "C:\\MyConfigurationRoot");

            Assert.Multiple(() =>
            {
                Assert.That(configuration.Scenarios, Has.Count.EqualTo(11));
                Assert.That(configuration.Scenarios[0].Name, Is.EqualTo("EN16931 XRechnung (UBL Invoice)"));
                Assert.That(configuration.Scenarios[0].MatchExpression, Is.TypeOf<XPathMatchExpression>());
                Assert.That((configuration.Scenarios[0].MatchExpression as XPathMatchExpression)?.XPath, Is.EqualTo("exists(/invoice:Invoice/cbc:CustomizationID[ . = 'urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0'])"));
                Assert.That(configuration.Scenarios[0].AcceptMatchExpression, Is.TypeOf<XPathMatchExpression>());
                Assert.That((configuration.Scenarios[0].AcceptMatchExpression as XPathMatchExpression)?.XPath, Is.EqualTo("/rep:report/rep:assessment[1]/rep:accept[1]"));
                Assert.That(configuration.Scenarios[0].Namespaces, Has.Count.EqualTo(3));
                Assert.That(configuration.Scenarios[0].Namespaces[0].Prefix, Is.EqualTo("cbc"));
                Assert.That(configuration.Scenarios[0].Namespaces[0].Uri, Is.EqualTo("urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2"));
                Assert.That(configuration.Scenarios[0].Namespaces[1].Prefix, Is.EqualTo("invoice"));
                Assert.That(configuration.Scenarios[0].Namespaces[1].Uri, Is.EqualTo("urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"));
                Assert.That(configuration.Scenarios[0].Namespaces[2].Prefix, Is.EqualTo("rep"));
                Assert.That(configuration.Scenarios[0].Namespaces[2].Uri, Is.EqualTo("http://www.xoev.de/de/validator/varl/1"));
                Assert.That(configuration.Scenarios[0].SchemaValidation.XsdFilePath, Is.EqualTo("C:\\MyConfigurationRoot\\resources\\ubl\\2.1\\xsd\\maindoc\\UBL-Invoice-2.1.xsd".NormalizePath()));
                Assert.That(configuration.Scenarios[0].SchemaValidation.Name, Is.EqualTo("XML Schema for UBL 2.1 Invoice"));
                Assert.That(configuration.Scenarios[0].SchematronValidations, Has.Count.EqualTo(2));
                Assert.That(configuration.Scenarios[0].SchematronValidations[0].XsltFilePath, Is.EqualTo("C:\\MyConfigurationRoot\\resources\\ubl\\2.1\\xsl\\EN16931-UBL-validation.xsl".NormalizePath()));
                Assert.That(configuration.Scenarios[0].SchematronValidations[0].Name, Is.EqualTo("Schematron rules for EN16931 (UBL)"));
                Assert.That(configuration.Scenarios[0].ReportTransformation.XsltFilePath, Is.EqualTo("C:\\MyConfigurationRoot\\resources\\xrechnung-report.xsl".NormalizePath()));
                Assert.That(configuration.Scenarios[0].ReportTransformation.Name, Is.EqualTo("Validation report for XRechnung"));
                Assert.That(configuration.Scenarios[0].RuleCustomizations, Has.Count.EqualTo(3));
                Assert.That(configuration.Scenarios[0].RuleCustomizations[0].RuleId, Is.EqualTo("BR-CL-23"));
                Assert.That(configuration.Scenarios[0].RuleCustomizations[0].LevelOverwrite, Is.EqualTo(Level.Warning));
                Assert.That(configuration.Scenarios[0].RuleCustomizations[1].RuleId, Is.EqualTo("BR-CL-21"));
                Assert.That(configuration.Scenarios[0].RuleCustomizations[1].LevelOverwrite, Is.EqualTo(Level.Warning));
                Assert.That(configuration.Scenarios[0].RuleCustomizations[2].RuleId, Is.EqualTo("UBL-CR-646"));
                Assert.That(configuration.Scenarios[0].RuleCustomizations[2].LevelOverwrite, Is.EqualTo(Level.Error));
                Assert.That(configuration.NoScenarioReport, Is.Not.Null);
                Assert.That(configuration.NoScenarioReport.Name, Is.EqualTo("Default Report"));
                Assert.That(configuration.NoScenarioReport.XsltFilePath, Is.EqualTo("C:\\MyConfigurationRoot\\resources\\default-report.xsl".NormalizePath()));
            });
        }
    }
}
