namespace PainNet;

/// <summary>
/// A party to a payment (debtor or creditor), identified by name.
/// </summary>
/// <param name="Name">The party's name as it should appear on the payment instruction.</param>
public sealed record Party(string Name);
