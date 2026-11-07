namespace EInvoiceValidationNet.Validation.ReportTransformation
{
    /// <summary>
    /// The XML namespaces and identifiers of the KoSIT validation framework's intermediate report input format,
    /// used to build the <c>createReportInput</c> document and to locate the framework schema resources.
    /// </summary>
    internal static class FrameworkXml
    {
        public const string ReportInputNamespace = "http://www.xoev.de/de/validator/framework/1/createreportinput";

        public const string ScenariosNamespace = "http://www.xoev.de/de/validator/framework/1/scenarios";

        public const string SvrlNamespace = "http://purl.oclc.org/dsdl/svrl";

        public const string HashAlgorithm = "SHA-256";
    }
}
