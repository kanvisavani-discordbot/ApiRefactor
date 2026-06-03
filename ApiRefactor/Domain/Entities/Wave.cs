namespace ApiRefactor.Domain.Entities;

public sealed class Wave
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public DateTime WaveDate { get; private set; }

    // Parameterless constructor for Dapper
    private Wave() { }

    private Wave(Guid id, string name, DateTime waveDate)
    {
        Id = id;
        Name = name;
        WaveDate = waveDate;
    }

    /// <summary>Creates a new wave with a generated ID and current timestamp.</summary>
    public static Wave Create(string name) =>
        new(Guid.NewGuid(), name, DateTime.UtcNow);

    /// <summary>Reconstitutes a wave from persistence.</summary>
    public static Wave Reconstitute(Guid id, string name, DateTime waveDate) =>
        new(id, name, waveDate);

    public void UpdateName(string name) => Name = name;
    public void UpdateWaveDate(DateTime waveDate) => WaveDate = waveDate;
}
