# Pain.Net

Maintained, tested, zero-dependency ISO 20022 SEPA payment-initiation for .NET. Reads **and** writes both `pain.001.001.09` (customer credit transfer) and `pain.008.001.08` (customer direct debit), with `NbOfTxs` and `CtrlSum` computed for you on write and re-validated on read.

[![NuGet](https://img.shields.io/nuget/v/Pain.Net.svg)](https://www.nuget.org/packages/Pain.Net) &nbsp; MIT &nbsp; Zero dependencies &nbsp; Native AOT clean

## Why

The long-time incumbent for SEPA XML in .NET, `SepaWriter`, has been abandoned since 2021. It is write-only, .NET Framework era, and no longer maintained. If you need to emit a payment file today you either fork a dead package or hand-roll `System.Xml`, and if you need to read a file back you are on your own.

`Pain.Net` is the maintained replacement: a small, tested, zero-dependency library that both writes and reads the two SEPA initiation messages, using only the in-box `System.Xml.Linq`. No transitive dependencies, Native-AOT clean, `decimal` money throughout.

## Install

```
dotnet add package Pain.Net
```

## Write a credit transfer (pain.001.001.09)

```csharp
using PainNet;

var transfer = new CreditTransfer(
    MessageId: "MSG-001",
    CreationDateTime: new DateTimeOffset(2024, 3, 1, 9, 30, 0, TimeSpan.Zero),
    Debtor: new Party("Acme GmbH"),
    DebtorIban: "DE89370400440532013000",
    DebtorBic: "COBADEFFXXX",
    Transactions: new[]
    {
        new CreditTransferTransaction("E2E-1", 1234.56m, "EUR",
            new Party("Beta SARL"), "FR7630006000011234567890189", "BNPAFRPPXXX", "Invoice 1"),
        new CreditTransferTransaction("E2E-2", 78.90m, "EUR",
            new Party("Gamma BV"), "NL91ABNA0417164300", null, null),
    });

string xml = Pain.WriteCreditTransfer(transfer);
```

## Write a direct debit (pain.008.001.08)

```csharp
var debit = new DirectDebit(
    MessageId: "MSG-DD-001",
    CreationDateTime: new DateTimeOffset(2024, 3, 1, 9, 30, 0, TimeSpan.Zero),
    Creditor: new Party("Utility Co"),
    CreditorIban: "DE89370400440532013000",
    CreditorBic: "COBADEFFXXX",
    CreditorSchemeId: "DE98ZZZ09999999999",
    Transactions: new[]
    {
        new DirectDebitTransaction("DD-1", 42.00m, "EUR",
            new Party("Customer One"), "FR7630006000011234567890189", "BNPAFRPPXXX",
            MandateId: "MANDATE-1", MandateSignatureDate: new DateOnly(2023, 1, 10),
            RemittanceInfo: "Subscription"),
    });

string xml = Pain.WriteDirectDebit(debit);
```

## Read one back

Both messages read back into the typed model, re-validating `NbOfTxs` and `CtrlSum` on the way in.

```csharp
CreditTransfer parsedTransfer = Pain.ReadCreditTransfer(xml);
DirectDebit parsedDebit = Pain.ReadDirectDebit(xml);

// Or non-throwing:
if (Pain.TryReadCreditTransfer(xml, out CreditTransfer? maybeTransfer))
{
    // maybeTransfer is populated and validated
}

if (Pain.TryReadDirectDebit(xml, out DirectDebit? maybeDebit))
{
    // maybeDebit is populated and validated
}
```

## Creditor scheme-name form

Some banks expect the SEPA creditor scheme name as a proprietary value (`SchmeNm/Prtry`, the default) and others as a code (`SchmeNm/Cd`). Both carry `SEPA`. Select the code form when your bank requires it:

```csharp
string xml = Pain.WriteDirectDebit(debit, SchemeNameForm.Code);
```

## The `NbOfTxs` / `CtrlSum` guarantee

Two fields cause more rejected SEPA files than any other: the transaction count (`NbOfTxs`) and the control sum (`CtrlSum`). If they disagree with the transactions actually in the file, the bank rejects the whole batch, and the bug is silent until then.

`Pain.Net` closes that gap from both sides:

- **On write**, `NbOfTxs` and `CtrlSum` are always computed from the transactions themselves, in both the group header and each payment block. A caller-supplied total is never trusted, because there is no way to supply one.
- **On read**, the stated `NbOfTxs` and `CtrlSum` are recomputed from the parsed transactions and compared, for both messages. If either disagrees, the reader throws `PainValidationException` (and the `TryRead` variant returns `false`). That mismatch is exactly the bug this catches.

All money is `decimal`, formatted invariant with two decimals and a `Ccy` attribute. `CreationDateTime` is caller-supplied and written as ISO 8601, so output is deterministic and testable.

## Correctness

Every claim here is backed by the xUnit suite, which runs in seconds:

- Credit-transfer writer produces one `PmtInf`, the right number of `CdtTrfTxInf`, the correct IBANs and amounts, and `NbOfTxs` / `CtrlSum` equal to the exact decimal sum.
- Full round-trip: write then read recovers the message id, transaction count, and per-transaction end-to-end id, amount, and creditor IBAN.
- Tamper tests: corrupting `CtrlSum` or `NbOfTxs` in valid XML makes the reader throw `PainValidationException`.
- Direct-debit writer emits the right totals, mandate ids, signature dates, and SEPA `SEPA` / `CORE` codes, in either `SchmeNm/Prtry` or `SchmeNm/Cd` form.
- Direct-debit round-trip: write then read recovers the message id, creditor scheme id, and every per-transaction field, including mandate id and signature date; tampering with `CtrlSum` or `NbOfTxs` makes the reader throw.
- Guard rails: empty `MessageId`, empty `EndToEndId`, empty `MandateId`, or a non-positive amount throw `ArgumentException`.
- Structural: the root is `Document` in the `pain.001.001.09` namespace URN with a `CstmrCdtTrfInitn` child.

## Notes and limitations

- Supports `pain.001.001.09` and `pain.008.001.08` only, on the SEPA profile (`SvcLvl` `SEPA`, direct debits `CORE` / `RCUR`, charge bearer `SLEV`).
- The agents that SEPA makes mandatory are required and always emitted: the debtor and creditor BICs (`DbtrAgt` / `CdtrAgt`), and for direct debits the creditor identifier (`CreditorSchemeId`, written as `CdtrSchmeId`). Empty values are rejected with `ArgumentException`.
- IBAN, BIC, and creditor identifier are passed through as strings and are **not** format-validated here beyond the non-empty check. Validate their structure with a dedicated library before serializing.
- Amounts must have at most two decimal places; more are rejected with `ArgumentException`. Money is `decimal` throughout, formatted invariant with two decimals.
- `CreationDateTime` is written with its UTC offset and round-trips exactly. Sub-second precision is preserved when present (`yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz`); a whole-second timestamp is written without a decimal point. The reader also accepts the offset-less form.
- Readers are provided for both messages (`ReadCreditTransfer` / `TryReadCreditTransfer` and `ReadDirectDebit` / `TryReadDirectDebit`), and each validates both group-level and per-`PmtInf` `NbOfTxs` / `CtrlSum`. A missing `Ccy` attribute on an amount is rejected with `PainValidationException`.
- Other optional SEPA elements not modelled include ultimate parties and batch-booking flags. Add them if your bank requires them.
- XML is produced schema-shaped in the correct element order with an explicit UTF-8 declaration and no byte-order mark; it is not validated against the official XSD at runtime.

## License

MIT. Copyright Israel Iyonsi.
