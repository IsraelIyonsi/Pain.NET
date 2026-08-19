using System.Globalization;
using System.Xml.Linq;

namespace PainNet.Tests;

public class CreditTransferWriterTests
{
    private static readonly XNamespace Ns = "urn:iso:std:iso:20022:tech:xsd:pain.001.001.09";

    [Fact]
    public void Output_begins_with_utf8_xml_declaration()
    {
        string xml = global::PainNet.Pain.WriteCreditTransfer(Fixtures.TwoTransactionCreditTransfer());

        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>", xml);
        Assert.Equal('<', xml[0]);
        Assert.DoesNotContain('﻿', xml);
    }

    [Fact]
    public void Always_emits_mandatory_debtor_and_creditor_agents()
    {
        XDocument doc = XDocument.Parse(global::PainNet.Pain.WriteCreditTransfer(Fixtures.TwoTransactionCreditTransfer()));

        Assert.Single(doc.Descendants(Ns + "DbtrAgt"));
        Assert.Equal(2, doc.Descendants(Ns + "CdtrAgt").Count());
        Assert.All(doc.Descendants(Ns + "CdtrAgt"),
            agent => Assert.False(string.IsNullOrEmpty(agent.Descendants(Ns + "BICFI").Single().Value)));
    }

    [Fact]
    public void Root_is_document_with_pain001_namespace_and_initiation_child()
    {
        XDocument doc = XDocument.Parse(global::PainNet.Pain.WriteCreditTransfer(Fixtures.TwoTransactionCreditTransfer()));

        Assert.Equal(Ns + "Document", doc.Root!.Name);
        Assert.Single(doc.Root!.Elements(Ns + "CstmrCdtTrfInitn"));
    }

    [Fact]
    public void Writes_computed_count_and_control_sum_and_one_payment_with_two_transactions()
    {
        XDocument doc = XDocument.Parse(global::PainNet.Pain.WriteCreditTransfer(Fixtures.TwoTransactionCreditTransfer()));
        XElement header = doc.Descendants(Ns + "GrpHdr").Single();

        Assert.Equal("2", header.Element(Ns + "NbOfTxs")!.Value);
        Assert.Equal("1313.46", header.Element(Ns + "CtrlSum")!.Value);

        Assert.Single(doc.Descendants(Ns + "PmtInf"));
        Assert.Equal(2, doc.Descendants(Ns + "CdtTrfTxInf").Count());
    }

    [Fact]
    public void Payment_level_totals_also_computed()
    {
        XDocument doc = XDocument.Parse(global::PainNet.Pain.WriteCreditTransfer(Fixtures.TwoTransactionCreditTransfer()));
        XElement payment = doc.Descendants(Ns + "PmtInf").Single();

        Assert.Equal("2", payment.Element(Ns + "NbOfTxs")!.Value);
        Assert.Equal("1313.46", payment.Element(Ns + "CtrlSum")!.Value);
    }

    [Fact]
    public void Writes_correct_ibans_and_amounts_with_currency_attribute()
    {
        XDocument doc = XDocument.Parse(global::PainNet.Pain.WriteCreditTransfer(Fixtures.TwoTransactionCreditTransfer()));

        string[] creditorIbans = doc.Descendants(Ns + "CdtrAcct")
            .Select(a => a.Descendants(Ns + "IBAN").Single().Value)
            .ToArray();
        Assert.Equal(new[] { "FR7630006000011234567890189", "NL91ABNA0417164300" }, creditorIbans);

        XElement firstAmount = doc.Descendants(Ns + "InstdAmt").First();
        Assert.Equal("1234.56", firstAmount.Value);
        Assert.Equal("EUR", firstAmount.Attribute("Ccy")!.Value);

        Assert.Equal("DE89370400440532013000",
            doc.Descendants(Ns + "DbtrAcct").Single().Descendants(Ns + "IBAN").Single().Value);
    }

    [Fact]
    public void Control_sum_is_the_exact_decimal_sum_of_amounts()
    {
        CreditTransfer transfer = Fixtures.TwoTransactionCreditTransfer();
        decimal expected = transfer.Transactions.Sum(t => t.Amount);

        XDocument doc = XDocument.Parse(global::PainNet.Pain.WriteCreditTransfer(transfer));
        decimal written = decimal.Parse(
            doc.Descendants(Ns + "GrpHdr").Single().Element(Ns + "CtrlSum")!.Value,
            CultureInfo.InvariantCulture);

        Assert.Equal(expected, written);
    }
}
