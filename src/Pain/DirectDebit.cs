namespace PainNet;

/// <summary>
/// A customer direct debit initiation (ISO 20022 pain.008.001.08): one creditor
/// collecting one or more payments from debtors under SEPA mandates.
/// </summary>
/// <param name="MessageId">Unique identifier for the whole message (GrpHdr/MsgId). Must be non-empty.</param>
/// <param name="CreationDateTime">Caller-supplied creation timestamp, written as ISO 8601 so output is deterministic and testable.</param>
/// <param name="Creditor">The party collecting the funds. Also used as the initiating party.</param>
/// <param name="CreditorIban">The creditor account IBAN. Must be non-empty. Passed through verbatim; not otherwise validated here.</param>
/// <param name="CreditorBic">The creditor agent BIC. Required, because pain.008.001.08 makes <c>CdtrAgt</c> mandatory. Passed through verbatim; not otherwise validated here.</param>
/// <param name="CreditorSchemeId">The SEPA creditor identifier (<c>CdtrSchmeId</c>). Required for SEPA CORE direct debits. Passed through verbatim; not otherwise validated here.</param>
/// <param name="Transactions">The individual direct-debit transactions. Must contain at least one.</param>
public sealed record DirectDebit(
    string MessageId,
    DateTimeOffset CreationDateTime,
    Party Creditor,
    string CreditorIban,
    string CreditorBic,
    string CreditorSchemeId,
    IReadOnlyList<DirectDebitTransaction> Transactions);

/// <summary>
/// A single direct-debit transaction within a <see cref="DirectDebit"/>.
/// </summary>
/// <param name="EndToEndId">Unique end-to-end reference for this transaction. Must be non-empty.</param>
/// <param name="Amount">The instructed amount. Must be strictly greater than zero.</param>
/// <param name="Currency">ISO 4217 currency code written as the <c>Ccy</c> attribute (for example <c>EUR</c>).</param>
/// <param name="Debtor">The party the funds are collected from.</param>
/// <param name="DebtorIban">The debtor account IBAN. Must be non-empty. Passed through verbatim; not otherwise validated here.</param>
/// <param name="DebtorBic">The debtor agent BIC. Required, because pain.008.001.08 makes <c>DbtrAgt</c> mandatory. Passed through verbatim; not otherwise validated here.</param>
/// <param name="MandateId">The SEPA mandate identifier authorising this collection. Must be non-empty.</param>
/// <param name="MandateSignatureDate">The date the mandate was signed.</param>
/// <param name="RemittanceInfo">Optional unstructured remittance information.</param>
public sealed record DirectDebitTransaction(
    string EndToEndId,
    decimal Amount,
    string Currency,
    Party Debtor,
    string DebtorIban,
    string DebtorBic,
    string MandateId,
    DateOnly MandateSignatureDate,
    string? RemittanceInfo);
