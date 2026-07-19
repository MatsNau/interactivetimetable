using System.Text.Json;
using Timetable.Domain.Time;
using Timetable.Infrastructure.Persistence;
using Xunit;

namespace Timetable.Infrastructure.Tests.Persistence;

public class FuzzyDateJsonConverterTests
{
    public static TheoryData<FuzzyDate, string> Beispiele => new()
    {
        { FuzzyDate.FromDay(new DateOnly(2027, 6, 22)), "\"2027-06-22\"" },
        { FuzzyDate.FromWeek(2026, 53), "\"2026-W53\"" },
        { FuzzyDate.FromMonth(2027, 3), "\"2027-03\"" },
    };

    [Theory]
    [MemberData(nameof(Beispiele))]
    public void Serialisierung_liefert_kompakten_String(FuzzyDate date, string expectedJson)
    {
        Assert.Equal(expectedJson, JsonSerializer.Serialize(date, PlanJson.Options));
    }

    [Theory]
    [MemberData(nameof(Beispiele))]
    public void Roundtrip_erhaelt_Wert_und_Praezision(FuzzyDate date, string expectedJson)
    {
        var restored = JsonSerializer.Deserialize<FuzzyDate>(expectedJson, PlanJson.Options);

        Assert.Equal(date, restored);
    }

    [Theory]
    [InlineData("\"22.06.2027\"")]  // deutsches Format ist nicht das Speicherformat
    [InlineData("\"2025-W53\"")]    // 2025 hat nur 52 Wochen
    [InlineData("\"2027-13\"")]     // ungültiger Monat
    [InlineData("\"quatsch\"")]
    [InlineData("null")]
    public void Ungueltige_Werte_werfen_JsonException(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<FuzzyDate>(json, PlanJson.Options));
    }
}
