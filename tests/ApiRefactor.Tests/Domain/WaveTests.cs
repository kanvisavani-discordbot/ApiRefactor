using ApiRefactor.Domain.Entities;
using FluentAssertions;

namespace ApiRefactor.Tests.Domain;

public sealed class WaveTests
{
    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        var wave1 = Wave.Create("Wave A");
        var wave2 = Wave.Create("Wave B");

        wave1.Id.Should().NotBe(wave2.Id);
    }

    [Fact]
    public void Create_ShouldSetNameAndDefaultUtcDate()
    {
        var before = DateTime.UtcNow;
        var wave = Wave.Create("TestWave");
        var after = DateTime.UtcNow;

        wave.Name.Should().Be("TestWave");
        wave.WaveDate.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void UpdateName_ShouldChangeName()
    {
        var wave = Wave.Create("Original");
        wave.UpdateName("Updated");
        wave.Name.Should().Be("Updated");
    }

    [Fact]
    public void UpdateWaveDate_ShouldChangeDate()
    {
        var wave = Wave.Create("Wave");
        var newDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        wave.UpdateWaveDate(newDate);

        wave.WaveDate.Should().Be(newDate);
    }

    [Fact]
    public void Reconstitute_ShouldPreserveAllProperties()
    {
        var id = Guid.NewGuid();
        var date = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var wave = Wave.Reconstitute(id, "Reconstituted", date);

        wave.Id.Should().Be(id);
        wave.Name.Should().Be("Reconstituted");
        wave.WaveDate.Should().Be(date);
    }
}
