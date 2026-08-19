using System.Globalization;

namespace PainNet;

/// <summary>
/// Culture-independent formatting and parsing helpers shared by the reader and writer.
/// All money is handled as <see cref="decimal"/> and all conversions use
/// <see cref="CultureInfo.InvariantCulture"/> so output never depends on the host locale.
/// </summary>
internal static class Formats
{
    internal const string MoneyFormat = "F2";
    internal const string DateFormat = "yyyy-MM-dd";
    internal const string DateTimeFormat = "yyyy-MM-ddTHH:mm:sszzz";
    internal const string DateTimeFormatNoOffset = "yyyy-MM-ddTHH:mm:ss";

    internal static readonly string[] DateTimeReadFormats = { DateTimeFormat, DateTimeFormatNoOffset };

    internal static string Money(decimal amount) =>
        amount.ToString(MoneyFormat, CultureInfo.InvariantCulture);

    internal static decimal ParseMoney(string text) =>
        decimal.Parse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);

    internal static int ParseCount(string text) =>
        int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);

    internal static string DateTime(DateTimeOffset value) =>
        value.ToString(DateTimeFormat, CultureInfo.InvariantCulture);

    internal static string Date(DateTimeOffset value) =>
        value.ToString(DateFormat, CultureInfo.InvariantCulture);

    internal static string Date(DateOnly value) =>
        value.ToString(DateFormat, CultureInfo.InvariantCulture);
}
