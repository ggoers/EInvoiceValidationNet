using EInvoiceValidationNet.Core.Validation;
using Saxon.Api;
using Saxon.Hej.value;
using System.Xml.Linq;

namespace EInvoiceValidationNet.XmlProcessing
{
    internal class SaxonXmlProcessing : IXmlProcessing
    {
        public bool EvaluateXPathBoolean(XDocument xmlDocument, string expression, IEnumerable<KeyValuePair<string, string>>? namespaces = null)
        {
            ArgumentNullException.ThrowIfNull(xmlDocument);
            ArgumentNullException.ThrowIfNull(expression);

            var processor = new Processor();
            var xPathCompiler = processor.NewXPathCompiler();
            if (namespaces != null)
            {
                foreach (var namespaceDeclaration in namespaces)
                {
                    xPathCompiler.DeclareNamespace(namespaceDeclaration.Key, namespaceDeclaration.Value);
                }
            }

            using var reader = xmlDocument.CreateReader();
            var result = xPathCompiler.Evaluate($"boolean(({expression}))",
                processor.NewDocumentBuilder().Build(reader));

            return result is not XdmAtomicValue atomicValue
                ? throw new InvalidOperationException("The XPath expression must evaluate to a boolean value.")
                : atomicValue.AsBoolean();
        }

        public XDocument Transform(XDocument inputDocument, string xsltPath, IReadOnlyDictionary<string, XDocument>? parameters = null)
        {
            ArgumentNullException.ThrowIfNull(inputDocument);
            ArgumentNullException.ThrowIfNull(xsltPath);

            var processor = new Processor();
            var xsltCompiler = processor.NewXsltCompiler();
            xsltCompiler.BaseUri = new Uri(Path.GetFullPath(xsltPath));

            //TODO cache compiled XSLT (key: xsltPath)
            XsltExecutable compiledXslt;
            using (var xsltStream = File.OpenRead(xsltPath))
            {
                compiledXslt = xsltCompiler.Compile(xsltStream);
            }

            var transformer = compiledXslt.Load30();
            using var reader = inputDocument.CreateReader();
            var input = processor.NewDocumentBuilder().Build(reader);
            transformer.GlobalContextItem = input;

            if (parameters != null)
            {
                var stylesheetParameters = new Dictionary<QName, XdmValue>();
                foreach (var parameter in parameters)
                {
                    using var parameterReader = parameter.Value.CreateReader();
                    stylesheetParameters[new QName(parameter.Key)] = processor.NewDocumentBuilder().Build(parameterReader);
                }

                transformer.SetStylesheetParameters(stylesheetParameters);
            }

            var dest = new XdmDestination();
            transformer.ApplyTemplates(input, dest);

            return XDocument.Parse(dest.XdmNode.ToString());
        }
    }
}
