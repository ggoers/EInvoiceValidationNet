namespace EInvoiceValidationNet.Validation
{
    /// <summary>
    /// Identifies this engine in the intermediate report input document and the validation reports created from it.
    /// </summary>
    internal static class EngineInformation
    {
        public const string Name = "EInvoiceValidationNet";

        public const string FrameworkVersion = "1.0.2";

        public static string EngineName => Name + " " + Version;

        public static string Version { get; } =
            typeof(EngineInformation).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    }
}
