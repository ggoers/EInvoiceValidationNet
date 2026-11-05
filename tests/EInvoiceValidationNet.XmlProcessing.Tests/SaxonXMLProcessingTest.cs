using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace EInvoiceValidationNet.XmlProcessing.Tests;

public class Tests
{
    [NotNull]
    private SaxonXmlProcessing? _xmlProcessing;

    [SetUp]
    public void Setup()
    {
        _xmlProcessing = new SaxonXmlProcessing();
    }

    [Test]
    public void EvaluateXPathBoolean_MatchSuccessful_ShouldReturnTrue()
    {
        const string xml = """
                  <?xml version="1.0" encoding="utf-8"?>
                  <test1>
                    <id>1</id>
                  </test1>
                  """;
        var xDocument = XDocument.Parse(xml);

        //exists is an XPath 2.0 feature
        var result = _xmlProcessing.EvaluateXPathBoolean(xDocument, "exists(/test1/id)");

        Assert.That(result, Is.True);
    }

    [TestCase("1", true)]
    [TestCase("true", true)]
    [TestCase("test", true)]
    [TestCase("", false)]
    public void EvaluateXPathBoolean_MatchReturnsText_ShouldUseEffectiveBooleanValue(string value, bool expectedResult)
    {
        var xml = $"""
                   <?xml version="1.0" encoding="utf-8"?>
                   <test1>
                     <id>{value}</id>
                   </test1>
                   """;
        var xDocument = XDocument.Parse(xml);

        var result = _xmlProcessing.EvaluateXPathBoolean(xDocument, "string(/test1/id)");

        Assert.That(result, Is.EqualTo(expectedResult));
    }

    [TestCase("/test1/id", true)]
    [TestCase("/test1/missing", false)]
    public void EvaluateXPathBoolean_MatchReturnsNodes_ShouldUseEffectiveBooleanValue(string expression, bool expectedResult)
    {
        const string xml = """
                           <?xml version="1.0" encoding="utf-8"?>
                           <test1>
                             <id>test</id>
                           </test1>
                           """;
        var xDocument = XDocument.Parse(xml);

        var result = _xmlProcessing.EvaluateXPathBoolean(xDocument, expression);

        Assert.That(result, Is.EqualTo(expectedResult));
    }

    [Test]
    public void EvaluateXPathBoolean_WithNamespaces_ShouldResolvePrefixes()
    {
        const string xml = """
                           <?xml version="1.0" encoding="utf-8"?>
                           <a:root xmlns:a="urn:test:a">
                             <b:child xmlns:b="urn:test:b" />
                           </a:root>
                           """;
        var xDocument = XDocument.Parse(xml);
        List<KeyValuePair<string, string>> namespaces =
        [
            new("a", "urn:test:a"),
            new("b", "urn:test:b")
        ];

        var result = _xmlProcessing.EvaluateXPathBoolean(xDocument, "exists(/a:root/b:child)", namespaces);

        Assert.That(result, Is.True);
    }

    [Test]
    public void EvaluateXPathBoolean_WithWrongNamespaceUri_ShouldReturnFalse()
    {
        const string xml = """
                           <?xml version="1.0" encoding="utf-8"?>
                           <a:root xmlns:a="urn:test:a">
                             <b:child xmlns:b="urn:test:b" />
                           </a:root>
                           """;
        var xDocument = XDocument.Parse(xml);
        List<KeyValuePair<string, string>> namespaces =
        [
            new("a", "urn:test:something-else"),
            new("b", "urn:test:b")
        ];

        var result = _xmlProcessing.EvaluateXPathBoolean(xDocument, "exists(/a:root/b:child)", namespaces);

        Assert.That(result, Is.False);
    }

    [Test]
    public void Transform_WithoutParameters_ShouldTransformInputDocument()
    {
        const string xml = """
                           <?xml version="1.0" encoding="utf-8"?>
                           <test1>
                             <id>1</id>
                           </test1>
                           """;
        var xDocument = XDocument.Parse(xml);
        var xsltPath = Path.Combine(AppContext.BaseDirectory, "Testdata", "transform-copy.xsl");

        var result = _xmlProcessing.Transform(xDocument, xsltPath);

        Assert.That(result.ToString(), Is.EqualTo("<result>1</result>"));
    }

    [Test]
    public void Transform_WithXslImport_ShouldResolveImportRelativeToXsltPath()
    {
        const string xml = """
                           <?xml version="1.0" encoding="utf-8"?>
                           <test1>
                             <id>1</id>
                           </test1>
                           """;
        var xDocument = XDocument.Parse(xml);
        var xsltPath = Path.Combine(AppContext.BaseDirectory, "Testdata", "transform-import-main.xsl");

        var result = _xmlProcessing.Transform(xDocument, xsltPath);

        Assert.That(result.ToString(), Is.EqualTo("<result>1</result>"));
    }

    [Test]
    public void Transform_WithParameters_ShouldPassParameterDocuments()
    {
        const string reportInputXml = """
                                       <?xml version="1.0" encoding="utf-8"?>
                                       <reportInput />
                                       """;
        const string invoiceXml = """
                                  <?xml version="1.0" encoding="utf-8"?>
                                  <test1>
                                    <id>42</id>
                                  </test1>
                                  """;
        var reportInput = XDocument.Parse(reportInputXml);
        var invoice = XDocument.Parse(invoiceXml);
        var xsltPath = Path.Combine(AppContext.BaseDirectory, "Testdata", "transform-parameter.xsl");
        var parameters = new Dictionary<string, XDocument>
        {
            ["input-document"] = invoice
        };

        var result = _xmlProcessing.Transform(reportInput, xsltPath, parameters);

        Assert.That(result.ToString(), Is.EqualTo("<result>42</result>"));
    }
}
