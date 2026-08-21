namespace PainNet;

/// <summary>
/// Selects how the SEPA creditor scheme name (<c>SchmeNm</c>) is written inside
/// <c>CdtrSchmeId</c> on a direct debit. Both forms carry the fixed value <c>SEPA</c>;
/// some banks expect the proprietary form (<c>Prtry</c>) and others the code form
/// (<c>Cd</c>).
/// </summary>
public enum SchemeNameForm
{
    /// <summary>Write <c>SchmeNm/Prtry=SEPA</c>. This is the default, backward-compatible form.</summary>
    Proprietary = 0,

    /// <summary>Write <c>SchmeNm/Cd=SEPA</c>.</summary>
    Code = 1,
}
