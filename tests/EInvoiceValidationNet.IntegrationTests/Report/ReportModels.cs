namespace EInvoiceValidationNet.IntegrationTests.Report
{
    //this unit contains the data structures which represent the final report
    //not all fields are included -> it's like a limited view from the integration test perspective
    internal sealed record Report
    {
        public Boolean IsValid { get; init; }

        public ValidationStepResult[] ValidationStepResults { get; init; } = [];

        public bool IsEqualTo(Report other)
        {
            return IsValid == other.IsValid
                && ValidationStepResults.Length == other.ValidationStepResults.Length
                && ValidationStepResults
                    .Zip(other.ValidationStepResults)
                    .All(pair => pair.First.IsEqualTo(pair.Second));
        }
    }

    internal sealed record ValidationStepResult
    {
        public required string Id { get; init; }

        public Boolean IsValid { get; init; }

        public Message[] Messages { get; init; } = [];

        public bool IsEqualTo(ValidationStepResult other)
        {
            return Id == other.Id
                && IsValid && other.IsValid
                && Messages.SequenceEqual(other.Messages);
        }
    }

    internal sealed record Message
    {
        public required string Id { get; init; }

        public required string Level { get; init; }

        public string? XPathLocation { get; init; }

        public string? Code { get; init; }

        public required string Text { get; init; }
    }
}
