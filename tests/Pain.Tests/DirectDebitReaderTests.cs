using System.Xml.Linq;

namespace PainNet.Tests;

public class DirectDebitReaderTests
{
    private static readonly XNamespace Ns = "urn:iso:std:iso:20022:tech:xsd:pain.008.001.08";

    [Fact]
    public void Round_trip_recovers_message_id_count_and_per_transaction_fields()
    {
        DirectDebit original = Fixtures.TwoTransactionDirectDebit();

        string xml = global::PainNet.Pain.WriteDirectDebit(original);
        DirectDebit parsed = global::PainNet.Pain.ReadDirectDebit(xml);

        Assert.Equal(original.MessageId, parsed.MessageId);
        Assert.Equal(original.Transactions.Count, parsed.Transactions.Count);

        for (int i = 0; i < original.Transactions.Count; i++)
        {
            DirectDebitTransaction expected = original.Transactions[i];
            DirectDebitTransaction actual = parsed.Transactions[i];

            Assert.Equal(expected.EndToEndId, actual.EndToEndId);
            Assert.Equal(expected.Amount, actual.Amount);
            Assert.Equal(expected.Currency, actual.Currency);
            Assert.Equal(expected.Debtor.Name, actual.Debtor.Name);
            Assert.Equal(expected.DebtorIban, actual.DebtorIban);
            Assert.Equal(expected.DebtorBic, actual.DebtorBic);
            Assert.Equal(expected.MandateId, actual.MandateId);
            Assert.Equal(expected.MandateSignatureDate, actual.MandateSignatureDate);
            Assert.Equal(expected.RemittanceInfo, actual.RemittanceInfo);
        }
    }

    [Fact]
    public void Round_trip_recovers_creditor_scheme_and_optional_fields()
    {
        DirectDebit original = Fixtures.TwoTransactionDirectDebit();
        DirectDebit parsed = global::PainNet.Pain.ReadDirectDebit(global::PainNet.Pain.WriteDirectDebit(original));

        Assert.Equal("Utility Co", parsed.Creditor.Name);
        Assert.Equal(original.CreditorIban, parsed.CreditorIban);
        Assert.Equal(original.CreditorBic, parsed.CreditorBic);
        Assert.Equal(original.CreditorSchemeId, parsed.CreditorSchemeId);
        Assert.Equal("Subscription", parsed.Transactions[0].RemittanceInfo);
        Assert.Null(parsed.Transactions[1].RemittanceInfo);
    }

    [Fact]
    public void Round_trip_reads_document_written_in_code_scheme_name_form()
    {
        string xml = global::PainNet.Pain.WriteDirectDebit(Fixtures.TwoTransactionDirectDebit(), SchemeNameForm.Code);
        DirectDebit parsed = global::PainNet.Pain.ReadDirectDebit(xml);

        Assert.Equal("DE98ZZZ09999999999", parsed.CreditorSchemeId);
    }

    [Fact]
    public void Tampered_payment_control_sum_throws_pain_validation_exception()
    {
        string xml = global::PainNet.Pain.WriteDirectDebit(Fixtures.TwoTransactionDirectDebit());
        XDocument doc = XDocument.Parse(xml);
        doc.Descendants(Ns + "PmtInf").Single().Element(Ns + "CtrlSum")!.Value = "0.01";

        PainValidationException ex = Assert.Throws<PainValidationException>(
            () => global::PainNet.Pain.ReadDirectDebit(doc.ToString()));
        Assert.Contains("PmtInf", ex.Message);
    }

    [Fact]
    public void Tampered_payment_number_of_transactions_throws_pain_validation_exception()
    {
        string xml = global::PainNet.Pain.WriteDirectDebit(Fixtures.TwoTransactionDirectDebit());
        XDocument doc = XDocument.Parse(xml);
        doc.Descendants(Ns + "PmtInf").Single().Element(Ns + "NbOfTxs")!.Value = "9";

        PainValidationException ex = Assert.Throws<PainValidationException>(
            () => global::PainNet.Pain.ReadDirectDebit(doc.ToString()));
        Assert.Contains("PmtInf", ex.Message);
    }

    [Fact]
    public void Tampered_group_header_control_sum_throws_pain_validation_exception()
    {
        string xml = global::PainNet.Pain.WriteDirectDebit(Fixtures.TwoTransactionDirectDebit());
        XDocument doc = XDocument.Parse(xml);
        doc.Descendants(Ns + "GrpHdr").Single().Element(Ns + "CtrlSum")!.Value = "9999.99";

        PainValidationException ex = Assert.Throws<PainValidationException>(
            () => global::PainNet.Pain.ReadDirectDebit(doc.ToString()));
        Assert.Contains("CtrlSum", ex.Message);
    }

    [Fact]
    public void Tampered_group_header_number_of_transactions_throws_pain_validation_exception()
    {
        string xml = global::PainNet.Pain.WriteDirectDebit(Fixtures.TwoTransactionDirectDebit());
        XDocument doc = XDocument.Parse(xml);
        doc.Descendants(Ns + "GrpHdr").Single().Element(Ns + "NbOfTxs")!.Value = "5";

        Assert.Throws<PainValidationException>(() => global::PainNet.Pain.ReadDirectDebit(doc.ToString()));
    }

    [Fact]
    public void Missing_currency_attribute_throws_pain_validation_exception()
    {
        string xml = global::PainNet.Pain.WriteDirectDebit(Fixtures.TwoTransactionDirectDebit());
        XDocument doc = XDocument.Parse(xml);
        doc.Descendants(Ns + "InstdAmt").First().Attribute("Ccy")!.Remove();

        Assert.Throws<PainValidationException>(() => global::PainNet.Pain.ReadDirectDebit(doc.ToString()));
    }

    [Fact]
    public void TryReadDirectDebit_returns_true_on_valid_document()
    {
        string xml = global::PainNet.Pain.WriteDirectDebit(Fixtures.TwoTransactionDirectDebit());

        Assert.True(global::PainNet.Pain.TryReadDirectDebit(xml, out DirectDebit? parsed));
        Assert.NotNull(parsed);
        Assert.Equal("MSG-DD-001", parsed!.MessageId);
    }

    [Fact]
    public void TryReadDirectDebit_returns_false_on_tampered_document()
    {
        string xml = global::PainNet.Pain.WriteDirectDebit(Fixtures.TwoTransactionDirectDebit());
        XDocument doc = XDocument.Parse(xml);
        doc.Descendants(Ns + "GrpHdr").Single().Element(Ns + "CtrlSum")!.Value = "0.01";

        Assert.False(global::PainNet.Pain.TryReadDirectDebit(doc.ToString(), out DirectDebit? parsed));
        Assert.Null(parsed);
    }

    [Fact]
    public void TryReadDirectDebit_returns_false_on_malformed_date()
    {
        string xml = global::PainNet.Pain.WriteDirectDebit(Fixtures.TwoTransactionDirectDebit());
        XDocument doc = XDocument.Parse(xml);
        doc.Descendants(Ns + "GrpHdr").Single().Element(Ns + "CreDtTm")!.Value = "not-a-date";

        Assert.False(global::PainNet.Pain.TryReadDirectDebit(doc.ToString(), out DirectDebit? parsed));
        Assert.Null(parsed);
    }

    [Fact]
    public void Read_rejects_wrong_namespace()
    {
        string creditTransferXml = global::PainNet.Pain.WriteCreditTransfer(Fixtures.TwoTransactionCreditTransfer());

        Assert.Throws<PainValidationException>(() => global::PainNet.Pain.ReadDirectDebit(creditTransferXml));
    }
}
