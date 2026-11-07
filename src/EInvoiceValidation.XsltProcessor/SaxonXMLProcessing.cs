using EInvoiceValidationNet.Validation;
using Saxon.Api;
using System.Collections.Concurrent;
using System.Xml.Linq;

namespace EInvoiceValidationNet.XmlProcessing
{
    internal class SaxonXmlProcessing : IXmlProcessing
    {
        private readonly Processor _processor = new();

        private readonly ConcurrentDictionary<string, Lazy<XsltExecutable>> _compiledXsltCache = new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, Lazy<XPathExecutable>> _compiledXPathCache = new(StringComparer.Ordinal);

        public bool EvaluateXPathBoolean(XDocument xmlDocument, string expression, IEnumerable<KeyValuePair<string, string>>? namespaces = null)
        {
            ArgumentNullException.ThrowIfNull(xmlDocument);
            ArgumentNullException.ThrowIfNull(expression);

            var namespacesList = namespaces?.ToList() ?? [];
            var key = CreateXPathCacheKey(expression, namespacesList);
            var compiledXPathLazy = _compiledXPathCache.GetOrAdd(key, (key) =>
            {
                return new Lazy<XPathExecutable>(() =>
                {
                    var xPathCompiler = _processor.NewXPathCompiler();
                    foreach (var namespaceDeclaration in namespacesList)
                    {
                        xPathCompiler.DeclareNamespace(namespaceDeclaration.Key, namespaceDeclaration.Value);
                    }

                    return xPathCompiler.Compile(expression);
                });
            });

            var selector = compiledXPathLazy.Value.Load();
            using var reader = xmlDocument.CreateReader();
            selector.ContextItem = _processor.NewDocumentBuilder().Build(reader);

            return selector.EffectiveBooleanValue();
        }

        public XDocument Transform(XDocument inputDocument, string xsltPath, IReadOnlyDictionary<string, XDocument>? parameters = null)
        {
            ArgumentNullException.ThrowIfNull(inputDocument);
            ArgumentNullException.ThrowIfNull(xsltPath);


            var key = Path.GetFullPath(xsltPath);
            var compiledXsltLazy = _compiledXsltCache.GetOrAdd(key, (key) =>
            {
                //the Lazy has LazyThreadSafetyMode.ExecutionAndPublication as default -> if two threads try to add a key -> factory is only executed once
                return new Lazy<XsltExecutable>(() =>
                {
                    var xsltCompiler = _processor.NewXsltCompiler();
                    xsltCompiler.BaseUri = new Uri(key);

                    using (var xsltStream = File.OpenRead(key))
                    {
                        return xsltCompiler.Compile(xsltStream);
                    }

                });
            });

            XsltExecutable compiledXslt;
            try
            {
                compiledXslt = compiledXsltLazy.Value;
            }
            catch (Exception)
            {
                _compiledXsltCache.TryRemove(new KeyValuePair<string, Lazy<XsltExecutable>>(key, compiledXsltLazy));
                throw;
            }

            var transformer = compiledXslt.Load30();
            using var reader = inputDocument.CreateReader();
            var input = _processor.NewDocumentBuilder().Build(reader);
            transformer.GlobalContextItem = input;

            if (parameters != null)
            {
                var stylesheetParameters = new Dictionary<QName, XdmValue>();
                foreach (var parameter in parameters)
                {
                    using var parameterReader = parameter.Value.CreateReader();
                    stylesheetParameters[new QName(parameter.Key)] = _processor.NewDocumentBuilder().Build(parameterReader);
                }

                transformer.SetStylesheetParameters(stylesheetParameters);
            }

            var dest = new XdmDestination();
            transformer.ApplyTemplates(input, dest);

            return XDocument.Parse(dest.XdmNode.ToString());
        }

        private static string CreateXPathCacheKey(string expression, List<KeyValuePair<string, string>> namespaces)
        {
            var namespacesKey = String.Join('\0', namespaces
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => $"{x.Key}={x.Value}"));

            return $"{expression}\0{namespacesKey}";
        }
    }
}
