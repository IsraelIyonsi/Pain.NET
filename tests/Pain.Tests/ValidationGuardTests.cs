namespace PainNet.Tests;

public class ValidationGuardTests
{
    [Fact]
    public void Empty_message_id_throws_argument_exception()
    {
        CreditTransfer transfer = Fixtures.TwoTransactionCreditTransfer() with { MessageId = "" };
        Assert.Throws<ArgumentException>(() => global::PainNet.Pain.WriteCreditTransfer(transfer));
    }

    [Fact]
    public void Empty_end_to_end_id_throws_argument_exception()
    {
        CreditTransfer transfer = Fixtures.TwoTransactionCreditTransfer();
        CreditTransferTransaction bad = transfer.Transactions[0] with { EndToEndId = "" };
        CreditTransfer withBad = transfer with { Transactions = new[] { bad, transfer.Transactions[1] } };

        Assert.Throws<ArgumentException>(() => global::PainNet.Pain.WriteCreditTransfer(withBad));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void Non_positive_amount_throws_argument_exception(decimal amount)
    {
        CreditTransfer transfer = Fixtures.TwoTransactionCreditTransfer();
        CreditTransferTransaction bad = transfer.Transactions[0] with { Amount = amount };
        CreditTransfer withBad = transfer with { Transactions = new[] { bad } };

        Assert.Throws<ArgumentException>(() => global::PainNet.Pain.WriteCreditTransfer(withBad));
    }

    [Theory]
    [InlineData(1.234)]
    [InlineData(0.001)]
    [InlineData(100.005)]
    [InlineData(9.999)]
    public void Amount_with_more_than_two_decimal_places_throws_argument_exception(decimal amount)
    {
        CreditTransfer transfer = Fixtures.TwoTransactionCreditTransfer();
        CreditTransferTransaction bad = transfer.Transactions[0] with { Amount = amount };
        CreditTransfer withBad = transfer with { Transactions = new[] { bad } };

        Assert.Throws<ArgumentException>(() => global::PainNet.Pain.WriteCreditTransfer(withBad));
    }

    [Fact]
    public void Empty_debtor_bic_throws_argument_exception()
    {
        CreditTransfer transfer = Fixtures.TwoTransactionCreditTransfer() with { DebtorBic = "" };
        Assert.Throws<ArgumentException>(() => global::PainNet.Pain.WriteCreditTransfer(transfer));
    }

    [Fact]
    public void Empty_creditor_bic_throws_argument_exception()
    {
        CreditTransfer transfer = Fixtures.TwoTransactionCreditTransfer();
        CreditTransferTransaction bad = transfer.Transactions[0] with { CreditorBic = "" };
        CreditTransfer withBad = transfer with { Transactions = new[] { bad } };

        Assert.Throws<ArgumentException>(() => global::PainNet.Pain.WriteCreditTransfer(withBad));
    }

    [Fact]
    public void Empty_debtor_iban_throws_argument_exception()
    {
        CreditTransfer transfer = Fixtures.TwoTransactionCreditTransfer() with { DebtorIban = "" };
        Assert.Throws<ArgumentException>(() => global::PainNet.Pain.WriteCreditTransfer(transfer));
    }

    [Fact]
    public void Empty_creditor_scheme_id_throws_argument_exception()
    {
        DirectDebit debit = Fixtures.TwoTransactionDirectDebit() with { CreditorSchemeId = "" };
        Assert.Throws<ArgumentException>(() => global::PainNet.Pain.WriteDirectDebit(debit));
    }

    [Fact]
    public void Empty_mandate_id_throws_argument_exception()
    {
        DirectDebit debit = Fixtures.TwoTransactionDirectDebit();
        DirectDebitTransaction bad = debit.Transactions[0] with { MandateId = "" };
        DirectDebit withBad = debit with { Transactions = new[] { bad } };

        Assert.Throws<ArgumentException>(() => global::PainNet.Pain.WriteDirectDebit(withBad));
    }

    [Fact]
    public void Direct_debit_non_positive_amount_throws_argument_exception()
    {
        DirectDebit debit = Fixtures.TwoTransactionDirectDebit();
        DirectDebitTransaction bad = debit.Transactions[0] with { Amount = 0m };
        DirectDebit withBad = debit with { Transactions = new[] { bad } };

        Assert.Throws<ArgumentException>(() => global::PainNet.Pain.WriteDirectDebit(withBad));
    }
}
