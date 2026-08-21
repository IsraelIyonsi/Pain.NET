using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace PainNet;

/// <summary>
/// Reads and writes ISO 20022 SEPA payment-initiation XML for both pain.001.001.09
/// (customer credit transfer) and pain.008.001.08 (customer direct debit).
/// The writer always computes <c>NbOfTxs</c> and <c>CtrlSum</c> from the transactions
/// themselves, and the reader re-validates them, so a caller-supplied or corrupted total
/// can never pass silently.
/// </summary>
public static class Pain
{
    /// <summary>
    /// Serializes a <see cref="CreditTransfer"/> to schema-shaped pain.001.001.09 XML.
    /// <c>NbOfTxs</c> and <c>CtrlSum</c> are computed from the transactions and never
    /// taken from the caller.
    /// </summary>
    /// <param name="transfer">The credit transfer to serialize.</param>
    /// <returns>The pain.001.001.09 document as an XML string.</returns>
    /// <exception cref="ArgumentNullException">The transfer is null.</exception>
    /// <exception cref="ArgumentException">A required field is empty, there are no transactions, or a transaction has a non-positive amount or more than two decimal places.</exception>
    public static string WriteCreditTransfer(CreditTransfer transfer)
    {
        ArgumentNullException.ThrowIfNull(transfer);
        Require(!string.IsNullOrWhiteSpace(transfer.MessageId), "MessageId must not be empty.");
        Require(!string.IsNullOrWhiteSpace(transfer.DebtorIban), "DebtorIban must not be empty.");
        Require(!string.IsNullOrWhiteSpace(transfer.DebtorBic), "DebtorBic must not be empty.");
        Require(transfer.Transactions is { Count: > 0 }, "A credit transfer must contain at least one transaction.");
        foreach (CreditTransferTransaction tx in transfer.Transactions)
        {
            ValidateTransaction(tx.EndToEndId, tx.Amount, tx.Currency, tx.CreditorIban, tx.CreditorBic);
        }

        XNamespace ns = Names.CreditTransferNs;
        int count = transfer.Transactions.Count;
        decimal controlSum = SumOf(transfer.Transactions.Select(t => t.Amount));

        XElement transactions = new(ns + Names.PaymentInformation,
            new XElement(ns + Names.PaymentInformationId, transfer.MessageId),
            new XElement(ns + Names.PaymentMethod, Names.PaymentMethodCreditTransfer),
            new XElement(ns + Names.NumberOfTransactions, count),
            new XElement(ns + Names.ControlSum, Formats.Money(controlSum)),
            new XElement(ns + Names.PaymentTypeInformation,
                new XElement(ns + Names.ServiceLevel,
                    new XElement(ns + Names.Code, Names.ServiceLevelSepa))),
            new XElement(ns + Names.RequestedExecutionDate,
                new XElement(ns + Names.Date, Formats.Date(transfer.CreationDateTime))),
            new XElement(ns + Names.Debtor, new XElement(ns + Names.Name, transfer.Debtor.Name)),
            Account(ns, Names.DebtorAccount, transfer.DebtorIban),
            Agent(ns, Names.DebtorAgent, transfer.DebtorBic),
            new XElement(ns + Names.ChargeBearer, Names.ChargeBearerSlev));

        foreach (CreditTransferTransaction tx in transfer.Transactions)
        {
            transactions.Add(new XElement(ns + Names.CreditTransferTransactionInfo,
                new XElement(ns + Names.PaymentId,
                    new XElement(ns + Names.EndToEndId, tx.EndToEndId)),
                new XElement(ns + Names.Amount,
                    new XElement(ns + Names.InstructedAmount,
                        new XAttribute(Names.Currency, tx.Currency),
                        Formats.Money(tx.Amount))),
                Agent(ns, Names.CreditorAgent, tx.CreditorBic),
                new XElement(ns + Names.Creditor, new XElement(ns + Names.Name, tx.Creditor.Name)),
                Account(ns, Names.CreditorAccount, tx.CreditorIban),
                Remittance(ns, tx.RemittanceInfo)));
        }

        XElement root = new(ns + Names.Document,
            new XElement(ns + Names.CustomerCreditTransferInitiation,
                GroupHeader(ns, transfer.MessageId, transfer.CreationDateTime, count, controlSum, transfer.Debtor.Name),
                transactions));

        return Render(root);
    }

    /// <summary>
    /// Serializes a <see cref="DirectDebit"/> to schema-shaped pain.008.001.08 XML.
    /// <c>NbOfTxs</c> and <c>CtrlSum</c> are computed from the transactions and never
    /// taken from the caller.
    /// </summary>
    /// <param name="debit">The direct debit to serialize.</param>
    /// <param name="schemeNameForm">Whether the creditor scheme name is written as <c>SchmeNm/Prtry</c> (default) or <c>SchmeNm/Cd</c>.</param>
    /// <returns>The pain.008.001.08 document as an XML string.</returns>
    /// <exception cref="ArgumentNullException">The debit is null.</exception>
    /// <exception cref="ArgumentException">A required field is empty, there are no transactions, or a transaction has a non-positive amount or more than two decimal places.</exception>
    public static string WriteDirectDebit(DirectDebit debit, SchemeNameForm schemeNameForm = SchemeNameForm.Proprietary)
    {
        ArgumentNullException.ThrowIfNull(debit);
        Require(!string.IsNullOrWhiteSpace(debit.MessageId), "MessageId must not be empty.");
        Require(!string.IsNullOrWhiteSpace(debit.CreditorIban), "CreditorIban must not be empty.");
        Require(!string.IsNullOrWhiteSpace(debit.CreditorBic), "CreditorBic must not be empty.");
        Require(!string.IsNullOrWhiteSpace(debit.CreditorSchemeId), "CreditorSchemeId must not be empty.");
        Require(debit.Transactions is { Count: > 0 }, "A direct debit must contain at least one transaction.");
        foreach (DirectDebitTransaction tx in debit.Transactions)
        {
            ValidateTransaction(tx.EndToEndId, tx.Amount, tx.Currency, tx.DebtorIban, tx.DebtorBic);
            Require(!string.IsNullOrWhiteSpace(tx.MandateId), "MandateId must not be empty.");
        }

        XNamespace ns = Names.DirectDebitNs;
        int count = debit.Transactions.Count;
        decimal controlSum = SumOf(debit.Transactions.Select(t => t.Amount));

        XElement transactions = new(ns + Names.PaymentInformation,
            new XElement(ns + Names.PaymentInformationId, debit.MessageId),
            new XElement(ns + Names.PaymentMethod, Names.PaymentMethodDirectDebit),
            new XElement(ns + Names.NumberOfTransactions, count),
            new XElement(ns + Names.ControlSum, Formats.Money(controlSum)),
            new XElement(ns + Names.PaymentTypeInformation,
                new XElement(ns + Names.ServiceLevel,
                    new XElement(ns + Names.Code, Names.ServiceLevelSepa)),
                new XElement(ns + Names.LocalInstrument,
                    new XElement(ns + Names.Code, Names.LocalInstrumentCore)),
                new XElement(ns + Names.SequenceType, Names.SequenceTypeRecurring)),
            new XElement(ns + Names.RequestedCollectionDate, Formats.Date(debit.CreationDateTime)),
            new XElement(ns + Names.Creditor, new XElement(ns + Names.Name, debit.Creditor.Name)),
            Account(ns, Names.CreditorAccount, debit.CreditorIban),
            Agent(ns, Names.CreditorAgent, debit.CreditorBic),
            new XElement(ns + Names.ChargeBearer, Names.ChargeBearerSlev),
            CreditorSchemeId(ns, debit.CreditorSchemeId, schemeNameForm));

        foreach (DirectDebitTransaction tx in debit.Transactions)
        {
            transactions.Add(new XElement(ns + Names.DirectDebitTransactionInfo,
                new XElement(ns + Names.PaymentId,
                    new XElement(ns + Names.EndToEndId, tx.EndToEndId)),
                new XElement(ns + Names.InstructedAmount,
                    new XAttribute(Names.Currency, tx.Currency),
                    Formats.Money(tx.Amount)),
                new XElement(ns + Names.DirectDebitTransaction,
                    new XElement(ns + Names.MandateRelatedInfo,
                        new XElement(ns + Names.MandateId, tx.MandateId),
                        new XElement(ns + Names.DateOfSignature, Formats.Date(tx.MandateSignatureDate)))),
                Agent(ns, Names.DebtorAgent, tx.DebtorBic),
                new XElement(ns + Names.Debtor, new XElement(ns + Names.Name, tx.Debtor.Name)),
                Account(ns, Names.DebtorAccount, tx.DebtorIban),
                Remittance(ns, tx.RemittanceInfo)));
        }

        XElement root = new(ns + Names.Document,
            new XElement(ns + Names.CustomerDirectDebitInitiation,
                GroupHeader(ns, debit.MessageId, debit.CreationDateTime, count, controlSum, debit.Creditor.Name),
                transactions));

        return Render(root);
    }

    /// <summary>
    /// Parses pain.001.001.09 XML back into a <see cref="CreditTransfer"/>, re-validating
    /// that the document's stated <c>NbOfTxs</c> and <c>CtrlSum</c> match the transactions
    /// actually present.
    /// </summary>
    /// <param name="xml">The pain.001.001.09 document.</param>
    /// <returns>The parsed credit transfer.</returns>
    /// <exception cref="ArgumentNullException">The xml is null.</exception>
    /// <exception cref="PainValidationException">The document is malformed, uses the wrong namespace, or its stated totals disagree with its transactions.</exception>
    public static CreditTransfer ReadCreditTransfer(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        XDocument doc = ParseDocument(xml);
        XNamespace ns = Names.CreditTransferNs;
        XElement root = doc.Root ?? throw new PainValidationException("Document has no root element.");
        RequireValid(root.Name == ns + Names.Document,
            $"Root element must be {Names.Document} in namespace {Names.CreditTransferNamespaceUri}.");

        XElement initiation = Child(root, ns + Names.CustomerCreditTransferInitiation);
        XElement header = Child(initiation, ns + Names.GroupHeader);

        string messageId = Value(header, ns + Names.MessageId);
        DateTimeOffset creationDateTime = DateTimeOffset.ParseExact(
            Value(header, ns + Names.CreationDateTime),
            Formats.DateTimeReadFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal);

        List<CreditTransferTransaction> transactions = new();
        Party? debtor = null;
        string debtorIban = string.Empty;
        string debtorBic = string.Empty;

        foreach (XElement payment in initiation.Elements(ns + Names.PaymentInformation))
        {
            debtor ??= new Party(Value(Child(payment, ns + Names.Debtor), ns + Names.Name));
            if (debtorIban.Length == 0)
            {
                debtorIban = Iban(payment, ns, Names.DebtorAccount);
                debtorBic = RequiredBic(payment, ns, Names.DebtorAgent);
            }

            List<CreditTransferTransaction> paymentTransactions = new();
            foreach (XElement info in payment.Elements(ns + Names.CreditTransferTransactionInfo))
            {
                XElement amount = Child(Child(info, ns + Names.Amount), ns + Names.InstructedAmount);
                paymentTransactions.Add(new CreditTransferTransaction(
                    Value(Child(info, ns + Names.PaymentId), ns + Names.EndToEndId),
                    Formats.ParseMoney(amount.Value),
                    RequiredCurrency(amount),
                    new Party(Value(Child(info, ns + Names.Creditor), ns + Names.Name)),
                    Iban(info, ns, Names.CreditorAccount),
                    RequiredBic(info, ns, Names.CreditorAgent),
                    OptionalRemittance(info, ns)));
            }

            ValidateTotals(payment, ns, paymentTransactions.Count, SumOf(paymentTransactions.Select(t => t.Amount)), Names.PaymentInformation);
            transactions.AddRange(paymentTransactions);
        }

        RequireValid(debtor is not null, "Document contains no PmtInf block.");

        ValidateTotals(header, ns, transactions.Count, SumOf(transactions.Select(t => t.Amount)), Names.GroupHeader);

        return new CreditTransfer(messageId, creationDateTime, debtor!, debtorIban, debtorBic, transactions);
    }

    /// <summary>
    /// Attempts to parse pain.001.001.09 XML into a <see cref="CreditTransfer"/>, returning
    /// <see langword="false"/> instead of throwing on malformed input or a totals mismatch.
    /// </summary>
    /// <param name="xml">The pain.001.001.09 document.</param>
    /// <param name="transfer">The parsed credit transfer when parsing succeeds; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if parsing and validation succeeded; otherwise <see langword="false"/>.</returns>
    public static bool TryReadCreditTransfer(string xml, out CreditTransfer? transfer)
    {
        try
        {
            transfer = ReadCreditTransfer(xml);
            return true;
        }
        catch (Exception e) when (e is PainValidationException or ArgumentException or FormatException or OverflowException)
        {
            transfer = null;
            return false;
        }
    }

    /// <summary>
    /// Parses pain.008.001.08 XML back into a <see cref="DirectDebit"/>, re-validating
    /// that the document's stated <c>NbOfTxs</c> and <c>CtrlSum</c> match the transactions
    /// actually present, at both group-header and per-<c>PmtInf</c> level.
    /// </summary>
    /// <param name="xml">The pain.008.001.08 document.</param>
    /// <returns>The parsed direct debit.</returns>
    /// <exception cref="ArgumentNullException">The xml is null.</exception>
    /// <exception cref="PainValidationException">The document is malformed, uses the wrong namespace, omits a required element or currency, or its stated totals disagree with its transactions.</exception>
    public static DirectDebit ReadDirectDebit(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        XDocument doc = ParseDocument(xml);
        XNamespace ns = Names.DirectDebitNs;
        XElement root = doc.Root ?? throw new PainValidationException("Document has no root element.");
        RequireValid(root.Name == ns + Names.Document,
            $"Root element must be {Names.Document} in namespace {Names.DirectDebitNamespaceUri}.");

        XElement initiation = Child(root, ns + Names.CustomerDirectDebitInitiation);
        XElement header = Child(initiation, ns + Names.GroupHeader);

        string messageId = Value(header, ns + Names.MessageId);
        DateTimeOffset creationDateTime = DateTimeOffset.ParseExact(
            Value(header, ns + Names.CreationDateTime),
            Formats.DateTimeReadFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal);

        List<DirectDebitTransaction> transactions = new();
        Party? creditor = null;
        string creditorIban = string.Empty;
        string creditorBic = string.Empty;
        string creditorSchemeId = string.Empty;

        foreach (XElement payment in initiation.Elements(ns + Names.PaymentInformation))
        {
            creditor ??= new Party(Value(Child(payment, ns + Names.Creditor), ns + Names.Name));
            if (creditorIban.Length == 0)
            {
                creditorIban = Iban(payment, ns, Names.CreditorAccount);
                creditorBic = RequiredBic(payment, ns, Names.CreditorAgent);
                creditorSchemeId = SchemeId(payment, ns);
            }

            List<DirectDebitTransaction> paymentTransactions = new();
            foreach (XElement info in payment.Elements(ns + Names.DirectDebitTransactionInfo))
            {
                XElement amount = Child(info, ns + Names.InstructedAmount);
                XElement mandate = Child(Child(info, ns + Names.DirectDebitTransaction), ns + Names.MandateRelatedInfo);
                paymentTransactions.Add(new DirectDebitTransaction(
                    Value(Child(info, ns + Names.PaymentId), ns + Names.EndToEndId),
                    Formats.ParseMoney(amount.Value),
                    RequiredCurrency(amount),
                    new Party(Value(Child(info, ns + Names.Debtor), ns + Names.Name)),
                    Iban(info, ns, Names.DebtorAccount),
                    RequiredBic(info, ns, Names.DebtorAgent),
                    Value(mandate, ns + Names.MandateId),
                    Formats.ParseDate(Value(mandate, ns + Names.DateOfSignature)),
                    OptionalRemittance(info, ns)));
            }

            ValidateTotals(payment, ns, paymentTransactions.Count, SumOf(paymentTransactions.Select(t => t.Amount)), Names.PaymentInformation);
            transactions.AddRange(paymentTransactions);
        }

        RequireValid(creditor is not null, "Document contains no PmtInf block.");

        ValidateTotals(header, ns, transactions.Count, SumOf(transactions.Select(t => t.Amount)), Names.GroupHeader);

        return new DirectDebit(messageId, creationDateTime, creditor!, creditorIban, creditorBic, creditorSchemeId, transactions);
    }

    /// <summary>
    /// Attempts to parse pain.008.001.08 XML into a <see cref="DirectDebit"/>, returning
    /// <see langword="false"/> instead of throwing on malformed input or a totals mismatch.
    /// </summary>
    /// <param name="xml">The pain.008.001.08 document.</param>
    /// <param name="debit">The parsed direct debit when parsing succeeds; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if parsing and validation succeeded; otherwise <see langword="false"/>.</returns>
    public static bool TryReadDirectDebit(string xml, out DirectDebit? debit)
    {
        try
        {
            debit = ReadDirectDebit(xml);
            return true;
        }
        catch (Exception e) when (e is PainValidationException or ArgumentException or FormatException or OverflowException)
        {
            debit = null;
            return false;
        }
    }

    private static XElement GroupHeader(XNamespace ns, string messageId, DateTimeOffset creationDateTime, int count, decimal controlSum, string initiatingPartyName) =>
        new(ns + Names.GroupHeader,
            new XElement(ns + Names.MessageId, messageId),
            new XElement(ns + Names.CreationDateTime, Formats.DateTime(creationDateTime)),
            new XElement(ns + Names.NumberOfTransactions, count),
            new XElement(ns + Names.ControlSum, Formats.Money(controlSum)),
            new XElement(ns + Names.InitiatingParty, new XElement(ns + Names.Name, initiatingPartyName)));

    private static XElement Account(XNamespace ns, string accountElement, string iban) =>
        new(ns + accountElement,
            new XElement(ns + Names.Identification,
                new XElement(ns + Names.Iban, iban)));

    private static XElement Agent(XNamespace ns, string agentElement, string bic) =>
        new(ns + agentElement,
            new XElement(ns + Names.FinancialInstitutionId,
                new XElement(ns + Names.BicFi, bic)));

    private static XElement CreditorSchemeId(XNamespace ns, string schemeId, SchemeNameForm schemeNameForm) =>
        new(ns + Names.CreditorSchemeId,
            new XElement(ns + Names.Identification,
                new XElement(ns + Names.PrivateId,
                    new XElement(ns + Names.Other,
                        new XElement(ns + Names.Identification, schemeId),
                        new XElement(ns + Names.SchemeName,
                            new XElement(ns + SchemeNameElement(schemeNameForm), Names.ServiceLevelSepa))))));

    private static string SchemeNameElement(SchemeNameForm schemeNameForm) =>
        schemeNameForm == SchemeNameForm.Code ? Names.Code : Names.Proprietary;

    private static XElement? Remittance(XNamespace ns, string? remittanceInfo) =>
        string.IsNullOrWhiteSpace(remittanceInfo)
            ? null
            : new XElement(ns + Names.RemittanceInfo,
                new XElement(ns + Names.Unstructured, remittanceInfo));

    private static void ValidateTotals(XElement scope, XNamespace ns, int actualCount, decimal actualSum, string scopeLabel)
    {
        int statedCount = Formats.ParseCount(Value(scope, ns + Names.NumberOfTransactions));
        decimal statedSum = Formats.ParseMoney(Value(scope, ns + Names.ControlSum));

        RequireValid(statedCount == actualCount,
            $"{scopeLabel} NbOfTxs mismatch: document states {statedCount} but contains {actualCount} transactions.");
        RequireValid(statedSum == actualSum,
            $"{scopeLabel} CtrlSum mismatch: document states {Formats.Money(statedSum)} but transactions total {Formats.Money(actualSum)}.");
    }

    private static XDocument ParseDocument(string xml)
    {
        try
        {
            return XDocument.Parse(xml);
        }
        catch (System.Xml.XmlException e)
        {
            throw new PainValidationException($"Input is not well-formed XML: {e.Message}");
        }
    }

    private static string Iban(XElement parent, XNamespace ns, string accountElement) =>
        Value(Child(Child(parent, ns + accountElement), ns + Names.Identification), ns + Names.Iban);

    private static string RequiredBic(XElement parent, XNamespace ns, string agentElement) =>
        Value(Child(Child(parent, ns + agentElement), ns + Names.FinancialInstitutionId), ns + Names.BicFi);

    private static string SchemeId(XElement parent, XNamespace ns) =>
        Value(
            Child(Child(Child(Child(parent, ns + Names.CreditorSchemeId), ns + Names.Identification), ns + Names.PrivateId), ns + Names.Other),
            ns + Names.Identification);

    private static string RequiredCurrency(XElement amount)
    {
        XAttribute? currency = amount.Attribute(Names.Currency);
        RequireValid(currency is not null,
            $"Missing required {Names.Currency} attribute on {amount.Name.LocalName}.");
        return currency!.Value;
    }

    private static string? OptionalRemittance(XElement parent, XNamespace ns) =>
        parent.Element(ns + Names.RemittanceInfo)?.Element(ns + Names.Unstructured)?.Value;

    private static XElement Child(XElement parent, XName name) =>
        parent.Element(name)
        ?? throw new PainValidationException($"Missing required element {name.LocalName}.");

    private static string Value(XElement parent, XName name) => Child(parent, name).Value;

    private static decimal SumOf(IEnumerable<decimal> amounts)
    {
        decimal total = 0m;
        foreach (decimal amount in amounts)
        {
            total += amount;
        }

        return total;
    }

    private static void ValidateTransaction(string endToEndId, decimal amount, string currency, string iban, string bic)
    {
        Require(!string.IsNullOrWhiteSpace(endToEndId), "EndToEndId must not be empty.");
        Require(amount > 0m, "Amount must be strictly greater than zero.");
        Require(decimal.Round(amount, 2) == amount, "Amount must not have more than two decimal places.");
        Require(!string.IsNullOrWhiteSpace(currency), "Currency must not be empty.");
        Require(!string.IsNullOrWhiteSpace(iban), "IBAN must not be empty.");
        Require(!string.IsNullOrWhiteSpace(bic), "BIC must not be empty.");
    }

    private static string Render(XElement root)
    {
        UTF8Encoding encoding = new(encoderShouldEmitUTF8Identifier: false);
        XmlWriterSettings settings = new()
        {
            OmitXmlDeclaration = false,
            Encoding = encoding,
            Indent = true,
            NewLineChars = "\n",
        };

        XDocument document = new(new XDeclaration("1.0", encoding.WebName, null), root);
        using Utf8StringWriter stringWriter = new(encoding);
        using (XmlWriter writer = XmlWriter.Create(stringWriter, settings))
        {
            document.Save(writer);
        }

        return stringWriter.ToString();
    }

    private sealed class Utf8StringWriter : StringWriter
    {
        private readonly Encoding encoding;

        internal Utf8StringWriter(Encoding encoding) => this.encoding = encoding;

        public override Encoding Encoding => encoding;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new ArgumentException(message);
        }
    }

    private static void RequireValid(bool condition, string message)
    {
        if (!condition)
        {
            throw new PainValidationException(message);
        }
    }
}
