namespace PainNet.Tests;

internal static class Fixtures
{
    internal const string Currency = "EUR";
    internal static readonly DateTimeOffset CreationDateTime = new(2024, 3, 1, 9, 30, 0, TimeSpan.Zero);

    internal static CreditTransfer TwoTransactionCreditTransfer() => new(
        MessageId: "MSG-CT-001",
        CreationDateTime: CreationDateTime,
        Debtor: new Party("Acme GmbH"),
        DebtorIban: "DE89370400440532013000",
        DebtorBic: "COBADEFFXXX",
        Transactions: new[]
        {
            new CreditTransferTransaction("E2E-1", 1234.56m, Currency, new Party("Beta SARL"), "FR7630006000011234567890189", "BNPAFRPPXXX", "Invoice 1"),
            new CreditTransferTransaction("E2E-2", 78.90m, Currency, new Party("Gamma BV"), "NL91ABNA0417164300", "ABNANL2AXXX", null),
        });

    internal static DirectDebit TwoTransactionDirectDebit() => new(
        MessageId: "MSG-DD-001",
        CreationDateTime: CreationDateTime,
        Creditor: new Party("Utility Co"),
        CreditorIban: "DE89370400440532013000",
        CreditorBic: "COBADEFFXXX",
        CreditorSchemeId: "DE98ZZZ09999999999",
        Transactions: new[]
        {
            new DirectDebitTransaction("DD-E2E-1", 42.00m, Currency, new Party("Customer One"), "FR7630006000011234567890189", "BNPAFRPPXXX", "MANDATE-1", new DateOnly(2023, 1, 10), "Subscription"),
            new DirectDebitTransaction("DD-E2E-2", 99.99m, Currency, new Party("Customer Two"), "NL91ABNA0417164300", "ABNANL2AXXX", "MANDATE-2", new DateOnly(2023, 6, 20), null),
        });
}
