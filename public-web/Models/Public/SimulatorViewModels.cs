namespace PublicWeb.Models.Public;

public class SimulatorPageViewModel
{
    public string SeasonName { get; set; } = string.Empty;
    public string SeasonSlug { get; set; } = string.Empty;
    public bool Supported { get; set; }
    public string? Limitation { get; set; }
    public string? Warning { get; set; }
    public SimulatorRulesViewModel Rules { get; set; } = new();
    public List<SimulatorDivisionViewModel> Divisions { get; set; } = new();
}

public class SimulatorRulesViewModel
{
    public int WinPoints { get; set; } = 3;
    public int DrawPoints { get; set; } = 1;
    public int LossPoints { get; set; }
    public List<string> TieBreakers { get; set; } = new();
    public List<SimulatorAdjustmentViewModel> Adjustments { get; set; } = new();
}

public class SimulatorAdjustmentViewModel
{
    public Guid TeamId { get; set; }
    public int Points { get; set; }
}

public class SimulatorDivisionViewModel
{
    public string DivisionName { get; set; } = string.Empty;
    public string DivisionSlug { get; set; } = string.Empty;
    public int PendingCount { get; set; }
    public List<SimulatorOfficialRowViewModel> Official { get; set; } = new();
    public List<SimulatorRoundViewModel> Rounds { get; set; } = new();

    public bool HasPending => PendingCount > 0;
}

public class SimulatorOfficialRowViewModel
{
    public int Position { get; set; }
    public TeamViewModel Team { get; set; } = new();
    public int Played { get; set; }
    public int Won { get; set; }
    public int Drawn { get; set; }
    public int Lost { get; set; }
    public int GoalsFor { get; set; }
    public int GoalsAgainst { get; set; }
    public int Points { get; set; }
    public int GoalDifference => GoalsFor - GoalsAgainst;
}

public class SimulatorRoundViewModel
{
    public int Round { get; set; }
    public List<SimulatorMatchViewModel> Matches { get; set; } = new();
    public List<SimulatorByeViewModel> Byes { get; set; } = new();
}

public class SimulatorMatchViewModel
{
    public Guid Id { get; set; }
    public int Round { get; set; }
    public DateTime? Kickoff { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public TeamViewModel Home { get; set; } = new();
    public TeamViewModel Away { get; set; } = new();
    public bool HomeCounts { get; set; }
    public bool AwayCounts { get; set; }
}

public class SimulatorByeViewModel
{
    public TeamViewModel Team { get; set; } = new();
}
