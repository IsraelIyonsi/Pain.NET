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

    // The "F" (uppercase) fractional-second specifiers preserve sub-second precision when
    // present and drop the decimal separator entirely when the time is on a whole second,
    // so a plain timestamp still round-trips to the exact same string it was read from.
    internal const string DateTimeWriteFormat = "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz";

    private const string DateTimeOffsetFraction = "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz";
    private const string DateTimeOffsetSecond = "yyyy-MM-ddTHH:mm:sszzz";
    private const string DateTimeNoOffsetFraction = "yyyy-MM-ddTHH:mm:ss.FFFFFFF";
    private const string DateTimeNoOffsetSecond = "yyyy-MM-ddTHH:mm:ss";

    internal static readonly string[] DateTimeReadFormats =
    {
        DateTimeOffsetFraction,
        DateTimeOffsetSecond,
        DateTimeNoOffsetFraction,
        DateTimeNoOffsetSecond,
    };

    internal static string Money(decimal amount) =>
        amount.ToString(MoneyFormat, CultureInfo.InvariantCulture);

    internal static decimal ParseMoney(string text) =>
        decimal.Parse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);

    internal static int ParseCount(string text) =>
        int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);

    internal static string DateTime(DateTimeOffset value) =>
        value.ToString(DateTimeWriteFormat, CultureInfo.InvariantCulture);

    internal static string Date(DateTimeOffset value) =>
        value.ToString(DateFormat, CultureInfo.InvariantCulture);

    internal static string Date(DateOnly value) =>
        value.ToString(DateFormat, CultureInfo.InvariantCulture);

    internal static DateOnly ParseDate(string text) =>
        DateOnly.ParseExact(text, DateFormat, CultureInfo.InvariantCulture);
}
