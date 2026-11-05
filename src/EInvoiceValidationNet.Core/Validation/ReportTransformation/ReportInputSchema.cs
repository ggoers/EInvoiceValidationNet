using System;
using System.IO;
using System.Reflection;

namespace EInvoiceValidationNet.Core.Validation.ReportTransformation
{
    /// <summary>
    /// Provides the file path of the framework's <c>createReportInput</c> schema, which the validator uses to
    /// self-check the intermediate report input before the report transformation runs.
    /// </summary>
    /// <remarks>
    /// The schema and its imports are embedded in this assembly and materialized into a temporary directory on first
    /// use, so relative <c>xsd:import</c> locations resolve like for a file-based schema.
    /// </remarks>
    internal static class ReportInputSchema
    {
        private static readonly Lazy<string> XsdLazyPath = new Lazy<string>(Materialize);

        public static string XsdFilePath => XsdLazyPath.Value;

        private static string Materialize()
        {
            var targetDirectory = Path.Combine(Path.GetTempPath(), "EInvoiceValidationNet", "framework-schema");
            Directory.CreateDirectory(targetDirectory);

            var assembly = typeof(ReportInputSchema).GetTypeInfo().Assembly;
            foreach (var resourceName in new[] { "createReportInput.xsd", "scenarios.xsd", "svrl-kosit.xsd", "xml.xsd" })
            {
                var targetPath = Path.Combine(targetDirectory, resourceName);
                if (!File.Exists(targetPath))
                {
                    using (var resourceStream = assembly.GetManifestResourceStream(resourceName))
                    using (var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
                    {
                        if (resourceStream is null)
                        {
                            throw new InvalidOperationException($"Embedded framework schema {resourceName} was not found");
                        }

                        resourceStream.CopyTo(fileStream);
                    }
                }
            }

            return Path.Combine(targetDirectory, "createReportInput.xsd");
        }
    }
}
