using System.Xml.Linq;

namespace PainNet.Tests;

public class RoundTripAndValidationTests
{
    private static readonly XNamespace Ns = "urn:iso:std:iso:20022:tech:xsd:pain.001.001.09";

    [Fact]
    public void Round_trip_recovers_message_id_count_and_per_transaction_fields()
    {
        CreditTransfer original = Fixtures.TwoTransactionCreditTransfer();

        string xml = global::PainNet.Pain.WriteCreditTransfer(original);
        CreditTransfer parsed = global::PainNet.Pain.ReadCreditTransfer(xml);

        Assert.Equal(original.MessageId, parsed.MessageId);
        Assert.Equal(original.Transactions.Count, parsed.Transactions.Count);

        for (int i = 0; i < original.Transactions.Count; i++)
        {
            Assert.Equal(original.Transactions[i].EndToEndId, parsed.Transactions[i].EndToEndId);
            Assert.Equal(original.Transactions[i].Amount, parsed.Transactions[i].Amount);
            Assert.Equal(original.Transactions[i].CreditorIban, parsed.Transactions[i].CreditorIban);
        }
    }

    [Fact]
    public void Round_trip_recovers_debtor_and_optional_fields()
    {
        CreditTransfer original = Fixtures.TwoTransactionCreditTransfer();
        CreditTransfer parsed = global::PainNet.Pain.ReadCreditTransfer(global::PainNet.Pain.WriteCreditTransfer(original));

        Assert.Equal("Acme GmbH", parsed.Debtor.Name);
        Assert.Equal(original.DebtorIban, parsed.DebtorIban);
        Assert.Equal(original.DebtorBic, parsed.DebtorBic);
        Assert.Equal("Invoice 1", parsed.Transactions[0].RemittanceInfo);
        Assert.Null(parsed.Transactions[1].RemittanceInfo);
        Assert.Equal("BNPAFRPPXXX", parsed.Transactions[0].CreditorBic);
        Assert.Equal("ABNANL2AXXX", parsed.Transactions[1].CreditorBic);
    }

    [Fact]
    public void Round_trip_preserves_creation_date_time_offset()
    {
        CreditTransfer original = Fixtures.TwoTransactionCreditTransfer() with
        {
            CreationDateTime = new DateTimeOffset(2024, 3, 1, 9, 30, 0, TimeSpan.FromHours(2)),
        };

        CreditTransfer parsed = global::PainNet.Pain.ReadCreditTransfer(global::PainNet.Pain.WriteCreditTransfer(original));

        Assert.Equal(original.CreationDateTime, parsed.CreationDateTime);
        Assert.Equal(original.CreationDateTime.Offset, parsed.CreationDateTime.Offset);
    }

    [Fact]
    public void Round_trip_preserves_fractional_seconds_in_creation_date_time()
    {
        CreditTransfer original = Fixtures.TwoTransactionCreditTransfer() with
        {
            CreationDateTime = new DateTimeOffset(2024, 3, 1, 9, 30, 15, 250, TimeSpan.FromHours(2)),
        };

        string xml = global::PainNet.Pain.WriteCreditTransfer(original);
        Assert.Contains("2024-03-01T09:30:15.25", xml);

        CreditTransfer parsed = global::PainNet.Pain.ReadCreditTransfer(xml);
        Assert.Equal(original.CreationDateTime, parsed.CreationDateTime);
        Assert.Equal(original.CreationDateTime.Offset, parsed.CreationDateTime.Offset);
    }

    [Fact]
    public void Whole_second_creation_date_time_is_written_without_a_decimal_point()
    {
        string xml = global::PainNet.Pain.WriteCreditTransfer(Fixtures.TwoTransactionCreditTransfer());

        Assert.Contains("<CreDtTm>2024-03-01T09:30:00+00:00</CreDtTm>", xml);
    }

    [Fact]
    public void Missing_currency_attribute_throws_pain_validation_exception()
    {
        string xml = global::PainNet.Pain.WriteCreditTransfer(Fixtures.TwoTransactionCreditTransfer());
        XDocument doc = XDocument.Parse(xml);
        doc.Descendants(Ns + "InstdAmt").First().Attribute("Ccy")!.Remove();

        Assert.Throws<PainValidationException>(() => global::PainNet.Pain.ReadCreditTransfer(doc.ToString()));
    }

    [Fact]
    public void Tampered_payment_control_sum_throws_pain_validation_exception()
    {
        string xml = global::PainNet.Pain.WriteCreditTransfer(Fixtures.TwoTransactionCreditTransfer());
        XDocument doc = XDocument.Parse(xml);
        doc.Descendants(Ns + "PmtInf").Single().Element(Ns + "CtrlSum")!.Value = "0.01";

        PainValidationException ex = Assert.Throws<PainValidationException>(
            () => global::PainNet.Pain.ReadCreditTransfer(doc.ToString()));
        Assert.Contains("PmtInf", ex.Message);
    }

    [Fact]
    public void Tampered_payment_number_of_transactions_throws_pain_validation_exception()
    {
        string xml = global::PainNet.Pain.WriteCreditTransfer(Fixtures.TwoTransactionCreditTransfer());
        XDocument doc = XDocument.Parse(xml);
        doc.Descendants(Ns + "PmtInf").Single().Element(Ns + "NbOfTxs")!.Value = "7";

        PainValidationException ex = Assert.Throws<PainValidationException>(
            () => global::PainNet.Pain.ReadCreditTransfer(doc.ToString()));
        Assert.Contains("PmtInf", ex.Message);
    }

    [Fact]
    public void TryReadCreditTransfer_returns_false_on_malformed_date()
    {
        string xml = global::PainNet.Pain.WriteCreditTransfer(Fixtures.TwoTransactionCreditTransfer());
        XDocument doc = XDocument.Parse(xml);
        doc.Descendants(Ns + "GrpHdr").Single().Element(Ns + "CreDtTm")!.Value = "not-a-date";

        Assert.False(global::PainNet.Pain.TryReadCreditTransfer(doc.ToString(), out CreditTransfer? parsed));
        Assert.Null(parsed);
    }

    [Fact]
    public void Tampered_control_sum_throws_pain_validation_exception()
    {
        string xml = global::PainNet.Pain.WriteCreditTransfer(Fixtures.TwoTransactionCreditTransfer());
        XDocument doc = XDocument.Parse(xml);
        doc.Descendants(Ns + "GrpHdr").Single().Element(Ns + "CtrlSum")!.Value = "9999.99";

        PainValidationException ex = Assert.Throws<PainValidationException>(
            () => global::PainNet.Pain.ReadCreditTransfer(doc.ToString()));
        Assert.Contains("CtrlSum", ex.Message);
    }

    [Fact]
    public void Tampered_number_of_transactions_throws_pain_validation_exception()
    {
        string xml = global::PainNet.Pain.WriteCreditTransfer(Fixtures.TwoTransactionCreditTransfer());
        XDocument doc = XDocument.Parse(xml);
        doc.Descendants(Ns + "GrpHdr").Single().Element(Ns + "NbOfTxs")!.Value = "5";

        Assert.Throws<PainValidationException>(() => global::PainNet.Pain.ReadCreditTransfer(doc.ToString()));
    }

    [Fact]
    public void TryRead_returns_false_on_tampered_document()
    {
        string xml = global::PainNet.Pain.WriteCreditTransfer(Fixtures.TwoTransactionCreditTransfer());
        XDocument doc = XDocument.Parse(xml);
        doc.Descendants(Ns + "GrpHdr").Single().Element(Ns + "CtrlSum")!.Value = "0.01";

        Assert.False(global::PainNet.Pain.TryReadCreditTransfer(doc.ToString(), out CreditTransfer? parsed));
        Assert.Null(parsed);
    }

    [Fact]
    public void TryRead_returns_true_on_valid_document()
    {
        string xml = global::PainNet.Pain.WriteCreditTransfer(Fixtures.TwoTransactionCreditTransfer());

        Assert.True(global::PainNet.Pain.TryReadCreditTransfer(xml, out CreditTransfer? parsed));
        Assert.NotNull(parsed);
        Assert.Equal("MSG-CT-001", parsed!.MessageId);
    }

    [Fact]
    public void Read_rejects_malformed_xml()
    {
        Assert.Throws<PainValidationException>(() => global::PainNet.Pain.ReadCreditTransfer("<not-closed>"));
    }
}
