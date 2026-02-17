-- ============================================================
-- DIVISIONS, SEASONS, DIVISION_SEASONS, TEAMS, TEAM_DIVISION_SEASONS
-- Source of truth for reusable divisions and teams across seasons
-- ============================================================

-- divisions: belong to league; unique (league_id, name)
CREATE TABLE divisions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    league_id UUID NOT NULL REFERENCES leagues(id) ON DELETE RESTRICT,
    name VARCHAR(50) NOT NULL,
    description TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    deleted_at TIMESTAMPTZ,
    UNIQUE (league_id, name)
);
CREATE INDEX idx_divisions_league_id ON divisions(league_id);

-- seasons: unchanged (belong to league)
-- (already exists)

-- division_seasons: assign divisions to seasons
CREATE TABLE division_seasons (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    season_id UUID NOT NULL REFERENCES seasons(id) ON DELETE RESTRICT,
    division_id UUID NOT NULL REFERENCES divisions(id) ON DELETE RESTRICT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    deleted_at TIMESTAMPTZ,
    UNIQUE (season_id, division_id)
);
CREATE INDEX idx_division_seasons_season_id ON division_seasons(season_id);
CREATE INDEX idx_division_seasons_division_id ON division_seasons(division_id);

-- teams: belong to league
CREATE TABLE teams (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    league_id UUID NOT NULL REFERENCES leagues(id) ON DELETE RESTRICT,
    name VARCHAR(100) NOT NULL,
    short_name VARCHAR(20),
    primary_color VARCHAR(50),
    secondary_color VARCHAR(50),
    founded_year INT,
    delegate_name VARCHAR(100),
    delegate_contact VARCHAR(100),
    logo_url TEXT,
    photo_url TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    deleted_at TIMESTAMPTZ,
    UNIQUE (league_id, name)
);
CREATE INDEX idx_teams_league_id ON teams(league_id);

-- team_division_seasons: assign teams to a division in a season
CREATE TABLE team_division_seasons (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    team_id UUID NOT NULL REFERENCES teams(id) ON DELETE RESTRICT,
    division_season_id UUID NOT NULL REFERENCES division_seasons(id) ON DELETE RESTRICT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    deleted_at TIMESTAMPTZ,
    UNIQUE (team_id, division_season_id)
);
CREATE INDEX idx_team_division_seasons_team_id ON team_division_seasons(team_id);
CREATE INDEX idx_team_division_seasons_division_season_id ON team_division_seasons(division_season_id);

-- fixtures: reference division_season (division in a season)
-- ALTER: add division_season_id, backfill, drop division_id
-- (handled in EF migration or separate migration script)
