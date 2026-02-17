-- ============================================================
-- FOOTBALL MANAGEMENT SYSTEM - POSTGRESQL SCHEMA
-- ============================================================

-- UUID generation (modern & recommended)
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ============================================================
-- TABLE: leagues
-- ============================================================
CREATE TABLE leagues (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    country VARCHAR(100),
    logo_url TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

-- ============================================================
-- TABLE: seasons
-- ============================================================
CREATE TABLE seasons (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    league_id UUID NOT NULL REFERENCES leagues(id) ON DELETE RESTRICT,
    name VARCHAR(100) NOT NULL,
    start_date DATE NOT NULL,
    end_date DATE,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    deleted_at TIMESTAMPTZ,
    UNIQUE (league_id, name)
);

-- ============================================================
-- TABLE: divisions
-- ============================================================
CREATE TABLE divisions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    season_id UUID NOT NULL REFERENCES seasons(id) ON DELETE RESTRICT,
    name VARCHAR(50) NOT NULL,
    description TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    deleted_at TIMESTAMPTZ,
    UNIQUE (season_id, name)
);

-- ============================================================
-- TABLE: teams
-- ============================================================
CREATE TABLE teams (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    division_id UUID NOT NULL REFERENCES divisions(id) ON DELETE RESTRICT,
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
    UNIQUE (division_id, name)
);

-- ============================================================
-- TABLE: players
-- ============================================================
CREATE TABLE players (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    team_id UUID NOT NULL REFERENCES teams(id) ON DELETE RESTRICT,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    document VARCHAR(50),
    birth_date DATE NOT NULL,
    jersey_number INT,
    position VARCHAR(10)
        CHECK (position IN ('GK', 'DEF', 'MID', 'FWD')),
    phone VARCHAR(50),
    email VARCHAR(100),
    nationality VARCHAR(100),
    height_cm INT,
    weight_kg INT,
    photo_url TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    deleted_at TIMESTAMPTZ,
    UNIQUE (team_id, jersey_number),
    UNIQUE (document)
);

-- ============================================================
-- TABLE: fields
-- ============================================================
CREATE TABLE fields (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL UNIQUE,
    address TEXT,
    city VARCHAR(100),
    geo_lat DOUBLE PRECISION,
    geo_lng DOUBLE PRECISION,
    is_available BOOLEAN DEFAULT TRUE,
    description TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

-- ============================================================
-- TABLE: fixtures
-- ============================================================
CREATE TABLE fixtures (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    division_id UUID NOT NULL REFERENCES divisions(id) ON DELETE RESTRICT,
    home_team_id UUID NOT NULL REFERENCES teams(id) ON DELETE RESTRICT,
    away_team_id UUID NOT NULL REFERENCES teams(id) ON DELETE RESTRICT,
    field_id UUID REFERENCES fields(id) ON DELETE SET NULL,
    match_date DATE NOT NULL,
    match_time TIME NOT NULL,
    referee_name VARCHAR(100),
    round_number INT,
    attendance INT,
    status VARCHAR(20) DEFAULT 'SCHEDULED'
        CHECK (status IN ('SCHEDULED','PLAYED','POSTPONED','CANCELLED')),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    deleted_at TIMESTAMPTZ,
    CONSTRAINT different_teams CHECK (home_team_id <> away_team_id)
);

-- ============================================================
-- TABLE: results
-- ============================================================
CREATE TABLE results (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    fixture_id UUID NOT NULL UNIQUE REFERENCES fixtures(id) ON DELETE CASCADE,
    home_team_goals INT NOT NULL DEFAULT 0,
    away_team_goals INT NOT NULL DEFAULT 0,
    notes TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================================
-- TABLE: match_events
-- ============================================================
CREATE TABLE match_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    fixture_id UUID NOT NULL REFERENCES fixtures(id) ON DELETE CASCADE,
    player_id UUID REFERENCES players(id) ON DELETE SET NULL,
    event_type VARCHAR(20)
        CHECK (event_type IN ('GOAL','YELLOW_CARD','RED_CARD','SUBSTITUTION','OWN_GOAL')),
    minute INT CHECK (minute >= 0),
    extra_info TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================================
-- TABLE: users
-- ============================================================
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    full_name VARCHAR(150) NOT NULL,
    email VARCHAR(150) UNIQUE NOT NULL,
    password_hash TEXT,
    google_sub VARCHAR(255),
    avatar_url TEXT,
    role VARCHAR(30) DEFAULT 'ADMIN',
    is_active BOOLEAN DEFAULT TRUE,
    is_verified BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================================
-- TABLE: user_leagues
-- ============================================================
CREATE TABLE user_leagues (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    league_id UUID NOT NULL REFERENCES leagues(id) ON DELETE CASCADE,
    assigned_role VARCHAR(30) DEFAULT 'ADMIN',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE (user_id, league_id)
);

-- ============================================================
-- OPTIONAL: roles
-- ============================================================
CREATE TABLE roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(50) UNIQUE NOT NULL,
    description TEXT
);

-- ============================================================
-- VIEW: standings
-- ============================================================
CREATE OR REPLACE VIEW standings AS
SELECT
    t.id AS team_id,
    t.name AS team_name,
    t.logo_url,
    d.name AS division_name,
    s.name AS season_name,
    l.name AS league_name,
    COUNT(r.id) AS matches_played,
    SUM(
        CASE
            WHEN r.home_team_goals > r.away_team_goals AND f.home_team_id = t.id THEN 1
            WHEN r.away_team_goals > r.home_team_goals AND f.away_team_id = t.id THEN 1
            ELSE 0
        END
    ) AS wins,
    SUM(CASE WHEN r.home_team_goals = r.away_team_goals THEN 1 ELSE 0 END) AS draws,
    SUM(
        CASE
            WHEN (r.home_team_goals < r.away_team_goals AND f.home_team_id = t.id)
              OR (r.away_team_goals < r.home_team_goals AND f.away_team_id = t.id)
            THEN 1 ELSE 0
        END
    ) AS losses,
    SUM(
        CASE
            WHEN r.home_team_goals > r.away_team_goals AND f.home_team_id = t.id THEN 3
            WHEN r.away_team_goals > r.home_team_goals AND f.away_team_id = t.id THEN 3
            WHEN r.home_team_goals = r.away_team_goals THEN 1
            ELSE 0
        END
    ) AS points,
    SUM(
        CASE WHEN f.home_team_id = t.id THEN r.home_team_goals
             WHEN f.away_team_id = t.id THEN r.away_team_goals
             ELSE 0 END
    ) AS goals_for,
    SUM(
        CASE WHEN f.home_team_id = t.id THEN r.away_team_goals
             WHEN f.away_team_id = t.id THEN r.home_team_goals
             ELSE 0 END
    ) AS goals_against,
    SUM(
        CASE WHEN f.home_team_id = t.id THEN r.home_team_goals - r.away_team_goals
             WHEN f.away_team_id = t.id THEN r.away_team_goals - r.home_team_goals
             ELSE 0 END
    ) AS goal_difference
FROM teams t
JOIN divisions d ON d.id = t.division_id
JOIN seasons s ON s.id = d.season_id
JOIN leagues l ON l.id = s.league_id
LEFT JOIN fixtures f ON (f.home_team_id = t.id OR f.away_team_id = t.id)
LEFT JOIN results r ON r.fixture_id = f.id
GROUP BY t.id, t.name, t.logo_url, d.name, s.name, l.name
ORDER BY points DESC, goal_difference DESC, goals_for DESC;

-- ============================================================
-- INDEXES
-- ============================================================
CREATE INDEX idx_seasons_league_id ON seasons(league_id);
CREATE INDEX idx_divisions_season_id ON divisions(season_id);
CREATE INDEX idx_teams_division_id ON teams(division_id);
CREATE INDEX idx_players_team_id ON players(team_id);
CREATE INDEX idx_fixtures_division_id ON fixtures(division_id);
CREATE INDEX idx_fixtures_date ON fixtures(match_date);
CREATE INDEX idx_fixtures_home_team_id ON fixtures(home_team_id);
CREATE INDEX idx_fixtures_away_team_id ON fixtures(away_team_id);
CREATE INDEX idx_results_fixture_id ON results(fixture_id);
CREATE INDEX idx_user_leagues_user_id ON user_leagues(user_id);
CREATE INDEX idx_user_leagues_league_id ON user_leagues(league_id);
