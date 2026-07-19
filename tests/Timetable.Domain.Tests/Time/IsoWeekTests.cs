using Timetable.Domain.Time;
using Xunit;

namespace Timetable.Domain.Tests.Time;

public class IsoWeekTests
{
    [Theory]
    [InlineData(2026, 1, 1, 2026, 1)]    // Donnerstag → KW 1 des eigenen Jahres
    [InlineData(2026, 12, 31, 2026, 53)] // 2026 hat 53 Wochen
    [InlineData(2027, 1, 1, 2026, 53)]   // Freitag → gehört noch zur KW 53 des Vorjahres
    [InlineData(2027, 1, 4, 2027, 1)]    // erster Montag → KW 1
    [InlineData(2027, 6, 22, 2027, 25)]
    public void FromDate_liefert_korrekte_ISO_Woche(int y, int m, int d, int expectedYear, int expectedWeek)
    {
        var week = IsoWeek.FromDate(new DateOnly(y, m, d));

        Assert.Equal(new IsoWeek(expectedYear, expectedWeek), week);
    }

    [Theory]
    [InlineData(2025, 52)]
    [InlineData(2026, 53)]
    [InlineData(2027, 52)]
    public void WeeksInYear_kennt_52_und_53_Wochen_Jahre(int year, int expected)
    {
        Assert.Equal(expected, IsoWeek.WeeksInYear(year));
    }

    [Fact]
    public void Ungueltige_Wochennummer_wird_abgelehnt()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new IsoWeek(2025, 53));
        Assert.Throws<ArgumentOutOfRangeException>(() => new IsoWeek(2026, 0));
    }

    [Fact]
    public void Monday_und_Sunday_begrenzen_die_Woche()
    {
        var week = new IsoWeek(2026, 53);

        Assert.Equal(new DateOnly(2026, 12, 28), week.Monday);
        Assert.Equal(new DateOnly(2027, 1, 3), week.Sunday);
        Assert.True(week.Contains(new DateOnly(2027, 1, 1)));
        Assert.False(week.Contains(new DateOnly(2027, 1, 4)));
    }

    [Fact]
    public void Next_ueberspringt_den_Jahreswechsel_korrekt()
    {
        Assert.Equal(new IsoWeek(2027, 1), new IsoWeek(2026, 53).Next());
        Assert.Equal(new IsoWeek(2026, 53), new IsoWeek(2027, 1).Previous());
    }

    [Fact]
    public void Range_ueber_den_Jahreswechsel_liefert_alle_Wochen()
    {
        var weeks = IsoWeek.Range(new IsoWeek(2026, 52), new IsoWeek(2027, 2)).ToList();

        Assert.Equal(
        [
            new IsoWeek(2026, 52),
            new IsoWeek(2026, 53),
            new IsoWeek(2027, 1),
            new IsoWeek(2027, 2),
        ], weeks);
    }

    [Fact]
    public void Vergleich_ordnet_erst_nach_Jahr_dann_nach_Woche()
    {
        Assert.True(new IsoWeek(2026, 53) < new IsoWeek(2027, 1));
        Assert.True(new IsoWeek(2027, 10) > new IsoWeek(2027, 2));
    }
}
