using System;

namespace FootballManager.Domain.Entities;

/// <summary>
/// Explicit allow-list of fields for a division in a season. When any rows exist, only these fields are used for scheduling.
/// </summary>
public class DivisionSeasonField
{
    public Guid DivisionSeasonId { get; private set; }
    public virtual DivisionSeason DivisionSeason { get; private set; }

    public Guid FieldId { get; private set; }
    public virtual Field Field { get; private set; }

    protected DivisionSeasonField()
    {
    }

    public DivisionSeasonField(DivisionSeason divisionSeason, Field field)
    {
        DivisionSeason = divisionSeason ?? throw new ArgumentNullException(nameof(divisionSeason));
        DivisionSeasonId = divisionSeason.Id;
        Field = field ?? throw new ArgumentNullException(nameof(field));
        FieldId = field.Id;
    }
}
