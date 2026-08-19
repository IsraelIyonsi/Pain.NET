namespace PainNet;

/// <summary>
/// A customer credit transfer initiation (ISO 20022 pain.001.001.09): one debtor
/// instructing one or more outgoing payments to creditors.
/// </summary>
/// <param name="MessageId">Unique identifier for the whole message (GrpHdr/MsgId). Must be non-empty.</param>
/// <param name="CreationDateTime">Caller-supplied creation timestamp, written as ISO 8601 so output is deterministic and testable.</param>
/// <param name="Debtor">The party sending the funds. Also used as the initiating party.</param>
/// <param name="DebtorIban">The debtor account IBAN. Must be non-empty. Passed through verbatim; not otherwise validated here.</param>
/// <param name="DebtorBic">The debtor agent BIC. Required, because pain.001.001.09 makes <c>DbtrAgt</c> mandatory. Passed through verbatim; not otherwise validated here.</param>
/// <param name="Transactions">The individual credit-transfer transactions. Must contain at least one.</param>
public sealed record CreditTransfer(
    string MessageId,
    DateTimeOffset CreationDateTime,
    Party Debtor,
    string DebtorIban,
    string DebtorBic,
    IReadOnlyList<CreditTransferTransaction> Transactions);

/// <summary>
/// A single credit-transfer transaction within a <see cref="CreditTransfer"/>.
/// </summary>
/// <param name="EndToEndId">Unique end-to-end reference for this transaction. Must be non-empty.</param>
/// <param name="Amount">The instructed amount. Must be strictly greater than zero.</param>
/// <param name="Currency">ISO 4217 currency code written as the <c>Ccy</c> attribute (for example <c>EUR</c>).</param>
/// <param name="Creditor">The party receiving the funds.</param>
/// <param name="CreditorIban">The creditor account IBAN. Must be non-empty. Passed through verbatim; not otherwise validated here.</param>
/// <param name="CreditorBic">The creditor agent BIC. Required, because pain.001.001.09 makes <c>CdtrAgt</c> mandatory. Passed through verbatim; not otherwise validated here.</param>
/// <param name="RemittanceInfo">Optional unstructured remittance information.</param>
public sealed record CreditTransferTransaction(
    string EndToEndId,
    decimal Amount,
    string Currency,
    Party Creditor,
    string CreditorIban,
    string CreditorBic,
    string? RemittanceInfo);
