using System.Xml.Linq;

namespace PainNet.Tests;

public class DirectDebitWriterTests
{
    private static readonly XNamespace Ns = "urn:iso:std:iso:20022:tech:xsd:pain.008.001.08";

    [Fact]
    public void Root_is_document_with_pain008_namespace_and_initiation_child()
    {
        XDocument doc = XDocument.Parse(global::PainNet.Pain.WriteDirectDebit(Fixtures.TwoTransactionDirectDebit()));

        Assert.Equal(Ns + "Document", doc.Root!.Name);
        Assert.Single(doc.Root!.Elements(Ns + "CstmrDrctDbtInitn"));
    }

    [Fact]
    public void Writes_computed_count_and_control_sum_with_two_transactions()
    {
        XDocument doc = XDocument.Parse(global::PainNet.Pain.WriteDirectDebit(Fixtures.TwoTransactionDirectDebit()));
        XElement header = doc.Descendants(Ns + "GrpHdr").Single();

        Assert.Equal("2", header.Element(Ns + "NbOfTxs")!.Value);
        Assert.Equal("141.99", header.Element(Ns + "CtrlSum")!.Value);
        Assert.Equal(2, doc.Descendants(Ns + "DrctDbtTxInf").Count());
    }

    [Fact]
    public void Writes_mandate_ids_and_signature_dates()
    {
        XDocument doc = XDocument.Parse(global::PainNet.Pain.WriteDirectDebit(Fixtures.TwoTransactionDirectDebit()));

        string[] mandateIds = doc.Descendants(Ns + "MndtId").Select(e => e.Value).ToArray();
        Assert.Equal(new[] { "MANDATE-1", "MANDATE-2" }, mandateIds);

        Assert.Equal("2023-01-10", doc.Descendants(Ns + "DtOfSgntr").First().Value);
    }

    [Fact]
    public void Emits_creditor_scheme_id_and_mandatory_debtor_agents()
    {
        XDocument doc = XDocument.Parse(global::PainNet.Pain.WriteDirectDebit(Fixtures.TwoTransactionDirectDebit()));

        XElement scheme = doc.Descendants(Ns + "CdtrSchmeId").Single();
        Assert.Equal("DE98ZZZ09999999999",
            scheme.Descendants(Ns + "Othr").Single().Element(Ns + "Id")!.Value);
        Assert.Equal("SEPA",
            scheme.Descendants(Ns + "SchmeNm").Single().Element(Ns + "Prtry")!.Value);

        Assert.Equal(2, doc.Descendants(Ns + "DrctDbtTxInf").Sum(t => t.Elements(Ns + "DbtrAgt").Count()));
    }

    [Fact]
    public void Writes_sepa_service_level_and_core_local_instrument()
    {
        XDocument doc = XDocument.Parse(global::PainNet.Pain.WriteDirectDebit(Fixtures.TwoTransactionDirectDebit()));

        Assert.Equal("SEPA", doc.Descendants(Ns + "SvcLvl").Single().Element(Ns + "Cd")!.Value);
        Assert.Equal("CORE", doc.Descendants(Ns + "LclInstrm").Single().Element(Ns + "Cd")!.Value);
    }
}
