using System;

namespace EInvoiceValidationNet.Configuration
{
    /// <summary>
    /// Overrides the severity of a single business rule.
    /// (a <c>customLevel</c> entry of <c>scenarios.xml</c>).
    /// </summary>
    public sealed class RuleCustomization
    {
        /// <summary>
        /// Initializes a rule customization.
        /// </summary>
        /// <param name="ruleId">Required. Identifier of the business rule whose level is overridden, e.g. <c>BR-CL-23</c>. It is trimmed.</param>
        /// <param name="levelOverwrite">Required. The level reported for this rule instead of the level determined by the Schematron validation.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="ruleId"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="levelOverwrite"/> is not a defined <see cref="Level"/> value.</exception>
        public RuleCustomization(string ruleId, Level levelOverwrite)
        {
            if (ruleId is null)
            {
                throw new ArgumentNullException(nameof(ruleId));
            }

            if (!Enum.IsDefined(typeof(Level), levelOverwrite))
            {
                throw new ArgumentOutOfRangeException(nameof(levelOverwrite));
            }

            RuleId = ruleId.Trim();
            LevelOverwrite = levelOverwrite;

        }

        /// <summary>
        /// Identifier of the overridden business rule (trimmed).
        /// </summary>
        public string RuleId { get; }

        /// <summary>
        /// The level reported for the rule regardless of its original severity.
        /// </summary>
        public Level LevelOverwrite { get; }

        /// <summary>
        /// The overridden level as the lower-case string used in reports.
        /// </summary>
        public string LevelOverwriteAsString => LevelOverwrite.MapToString();
    }

    /// <summary>
    /// Severity of a validation finding, matching the levels of the KoSIT validation report.
    /// </summary>
    public enum Level
    {
        /// <summary>Informational finding that does not indicate a problem.</summary>
        Information,

        /// <summary>Finding that should be reviewed but does not necessarily prevent acceptance.</summary>
        Warning,

        /// <summary>A violated rule; normally leads to a rejection.</summary>
        Error,

        /// <summary>Severe problem that prevents reliable validation of the document.</summary>
        Fatal
    }

    /// <summary>
    /// Converts <see cref="Level"/> values to and from the lower-case strings used by <c>scenarios.xml</c> and the reports.
    /// </summary>
    public static class LevelExtensions
    {
        /// <summary>
        /// Returns the lower-case report representation of a level.
        /// </summary>
        /// <param name="level">The level to convert; must be a defined <see cref="Level"/> value.</param>
        /// <returns><c>information</c>, <c>warning</c>, <c>error</c> or <c>fatal</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="level"/> is not a defined <see cref="Level"/> value.</exception>
        public static string MapToString(this Level level)
        {
            switch (level)
            {
                case Level.Information:
                    return "information";
                case Level.Warning:
                    return "warning";
                case Level.Error:
                    return "error";
                case Level.Fatal:
                    return "fatal";
                default:
                    throw new ArgumentOutOfRangeException(nameof(level));
            }
        }

        /// <summary>
        /// Converts a textual level as used in <c>scenarios.xml</c> and the reports into a <see cref="Level"/>.
        /// </summary>
        /// <param name="level">Required. The textual level (<c>information</c>, <c>warning</c>, <c>error</c> or <c>fatal</c>);
        /// the comparison is case-sensitive, surrounding whitespace is ignored.</param>
        /// <returns>The matching <see cref="Level"/> value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="level"/> is unknown or <c>null</c>.</exception>
        public static Level GetLevelFromString(string level)
        {
            switch (level?.Trim())
            {
                case "information":
                    return Level.Information;
                case "warning":
                    return Level.Warning;
                case "error":
                    return Level.Error;
                case "fatal":
                    return Level.Fatal;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level));
            }

        }
    }
}
