using System.Xml.Linq;

namespace PainNet;

/// <summary>
/// Central, single-source definition of every ISO 20022 element name, attribute name,
/// namespace URN and fixed code word used by the reader and writer. Keeping these out of
/// the logic removes magic strings and makes the supported schema versions explicit.
/// </summary>
internal static class Names
{
    internal const string CreditTransferNamespaceUri = "urn:iso:std:iso:20022:tech:xsd:pain.001.001.09";
    internal const string DirectDebitNamespaceUri = "urn:iso:std:iso:20022:tech:xsd:pain.008.001.08";

    internal static readonly XNamespace CreditTransferNs = CreditTransferNamespaceUri;
    internal static readonly XNamespace DirectDebitNs = DirectDebitNamespaceUri;

    // Fixed SEPA code words.
    internal const string ServiceLevelSepa = "SEPA";
    internal const string LocalInstrumentCore = "CORE";
    internal const string SequenceTypeRecurring = "RCUR";
    internal const string PaymentMethodCreditTransfer = "TRF";
    internal const string PaymentMethodDirectDebit = "DD";
    internal const string ChargeBearerSlev = "SLEV";

    // Structural elements.
    internal const string Document = "Document";
    internal const string CustomerCreditTransferInitiation = "CstmrCdtTrfInitn";
    internal const string CustomerDirectDebitInitiation = "CstmrDrctDbtInitn";
    internal const string GroupHeader = "GrpHdr";
    internal const string PaymentInformation = "PmtInf";

    // Group header / payment information fields.
    internal const string MessageId = "MsgId";
    internal const string CreationDateTime = "CreDtTm";
    internal const string NumberOfTransactions = "NbOfTxs";
    internal const string ControlSum = "CtrlSum";
    internal const string InitiatingParty = "InitgPty";
    internal const string PaymentInformationId = "PmtInfId";
    internal const string PaymentMethod = "PmtMtd";
    internal const string PaymentTypeInformation = "PmtTpInf";
    internal const string ServiceLevel = "SvcLvl";
    internal const string LocalInstrument = "LclInstrm";
    internal const string SequenceType = "SeqTp";
    internal const string Code = "Cd";
    internal const string RequestedExecutionDate = "ReqdExctnDt";
    internal const string RequestedCollectionDate = "ReqdColltnDt";
    internal const string ChargeBearer = "ChrgBr";

    // Parties and accounts.
    internal const string Name = "Nm";
    internal const string Debtor = "Dbtr";
    internal const string DebtorAccount = "DbtrAcct";
    internal const string DebtorAgent = "DbtrAgt";
    internal const string Creditor = "Cdtr";
    internal const string CreditorAccount = "CdtrAcct";
    internal const string CreditorAgent = "CdtrAgt";
    internal const string Identification = "Id";
    internal const string Iban = "IBAN";
    internal const string FinancialInstitutionId = "FinInstnId";
    internal const string BicFi = "BICFI";

    // Transaction elements.
    internal const string CreditTransferTransactionInfo = "CdtTrfTxInf";
    internal const string DirectDebitTransactionInfo = "DrctDbtTxInf";
    internal const string PaymentId = "PmtId";
    internal const string EndToEndId = "EndToEndId";
    internal const string Amount = "Amt";
    internal const string InstructedAmount = "InstdAmt";
    internal const string Currency = "Ccy";
    internal const string Date = "Dt";
    internal const string DirectDebitTransaction = "DrctDbtTx";
    internal const string MandateRelatedInfo = "MndtRltdInf";
    internal const string MandateId = "MndtId";
    internal const string DateOfSignature = "DtOfSgntr";
    internal const string RemittanceInfo = "RmtInf";
    internal const string Unstructured = "Ustrd";

    // Creditor scheme identifier (SEPA CORE direct debit).
    internal const string CreditorSchemeId = "CdtrSchmeId";
    internal const string PrivateId = "PrvtId";
    internal const string Other = "Othr";
    internal const string SchemeName = "SchmeNm";
    internal const string Proprietary = "Prtry";
}
