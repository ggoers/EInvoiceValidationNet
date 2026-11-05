using System.Xml.Linq;

namespace EInvoiceValidationNet.IntegrationTests.Report
{
    internal static class ReportReader
    {
        private static readonly XNamespace Rep = "http://www.xoev.de/de/validator/varl/1";

        public static Report Read(XDocument document)
        {
            var root = document.Root ?? throw new InvalidOperationException("The KoSIT report has no root element.");

            if (root.Name != Rep + "report")
            {
                throw new InvalidOperationException($"Unexpected report root element: {root.Name}");
            }

            var scenarioMatched = root.Element(Rep + "scenarioMatched")
                ?? throw new InvalidOperationException("no scenarioMatched element available");

            return new Report
            {
                IsValid = ReadRequiredBooleanAttribute(root, "valid"),

                ValidationStepResults = [.. scenarioMatched
                    .Elements(Rep + "validationStepResult")
                    .Select(ReadValidationStepResult)]
            };
        }

        private static ValidationStepResult ReadValidationStepResult(
            XElement element)
        {
            return new ValidationStepResult
            {
                Id = ReadRequiredAttribute(element, "id"),
                IsValid = ReadRequiredBooleanAttribute(element, "valid"),

                Messages = [.. element
                    .Elements(Rep + "message")
                    .Select(ReadMessage)]
            };
        }

        private static Message ReadMessage(XElement element)
        {
            return new Message
            {
                Id = ReadRequiredAttribute(element, "id"),
                Level = ReadRequiredAttribute(element, "level"),
                XPathLocation = element.Attribute("xpathLocation")?.Value,
                Code = element.Attribute("code")?.Value,
                Text = element.Value.Trim()
            };
        }

        private static string ReadRequiredAttribute(XElement element, string attributeName)
        {
            return element.Attribute(attributeName)?.Value
                ?? throw new InvalidOperationException($"missing required attribute '{attributeName}' on element '{element.Name}'");
        }

        private static bool ReadRequiredBooleanAttribute(XElement element, string attributeName)
        {
            var value = ReadRequiredAttribute(element, attributeName);

            return bool.TryParse(value, out var result)
                ? result
                : throw new InvalidOperationException($"invalid Boolean value '{value}' for attribute '{attributeName}' on element '{element.Name}'");
        }
    }
}
