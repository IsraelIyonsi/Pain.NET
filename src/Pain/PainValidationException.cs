namespace PainNet;

/// <summary>
/// Thrown when a parsed ISO 20022 document is internally inconsistent, in particular
/// when its stated <c>NbOfTxs</c> or <c>CtrlSum</c> does not match the transactions it
/// actually contains. This mismatch is exactly the class of silent totals bug this
/// library is designed to catch.
/// </summary>
public sealed class PainValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PainValidationException"/> class
    /// with a descriptive message.
    /// </summary>
    /// <param name="message">A message describing the inconsistency.</param>
    public PainValidationException(string message) : base(message)
    {
    }
}
