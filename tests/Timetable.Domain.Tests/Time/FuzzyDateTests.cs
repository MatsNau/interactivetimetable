using Timetable.Domain.Time;
using Xunit;

namespace Timetable.Domain.Tests.Time;

public class FuzzyDateTests
{
    [Fact]
    public void Tagesgenauer_Termin_zeigt_Datum_und_belegt_eine_Woche()
    {
        var date = FuzzyDate.FromDay(new DateOnly(2027, 6, 22));

        Assert.Equal("22.06.2027", date.ToDisplayString());
        Assert.Equal(new DateOnly(2027, 6, 22), date.ExactDay);
        Assert.Equal([new IsoWeek(2027, 25)], date.CoveredWeeks());
    }

    [Fact]
    public void Wochengenauer_Termin_zeigt_KW_und_hat_kein_exaktes_Datum()
    {
        var date = FuzzyDate.FromWeek(2026, 53);

        Assert.Equal("KW 53/2026", date.ToDisplayString());
        Assert.Null(date.ExactDay);
        Assert.Equal(new DateOnly(2026, 12, 28), date.Anchor);
        Assert.Equal([new IsoWeek(2026, 53)], date.CoveredWeeks());
    }

    [Fact]
    public void Monatsgenauer_Termin_zeigt_xx_Schreibweise_wie_im_Excel()
    {
        var date = FuzzyDate.FromMonth(2027, 3);

        Assert.Equal("xx.03.2027", date.ToDisplayString());
        Assert.Null(date.ExactDay);
    }

    [Fact]
    public void Monatsgenauer_Termin_belegt_alle_beruehrten_Wochen()
    {
        var weeks = FuzzyDate.FromMonth(2027, 3).CoveredWeeks();

        Assert.Equal(IsoWeek.Range(new IsoWeek(2027, 9), new IsoWeek(2027, 13)).ToList(), weeks);
    }

    [Fact]
    public void Monat_am_Jahresanfang_beginnt_in_der_KW_des_Vorjahres()
    {
        var weeks = FuzzyDate.FromMonth(2027, 1).CoveredWeeks();

        Assert.Equal(new IsoWeek(2026, 53), weeks.First());
        Assert.Equal(new IsoWeek(2027, 4), weeks.Last());
        Assert.Equal(5, weeks.Count);
    }

    [Fact]
    public void Sortierung_mischt_alle_Praezisionsstufen_chronologisch()
    {
        var march = FuzzyDate.FromMonth(2027, 3);
        var midMarch = FuzzyDate.FromDay(new DateOnly(2027, 3, 15));
        var february = FuzzyDate.FromDay(new DateOnly(2027, 2, 27));
        var week10 = FuzzyDate.FromWeek(2027, 10); // Montag 08.03.2027

        var sorted = new[] { midMarch, march, week10, february }.OrderBy(d => d).ToList();

        Assert.Equal([february, march, week10, midMarch], sorted);
    }
}
