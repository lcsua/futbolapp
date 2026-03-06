-- Run this script in PostgreSQL if the table match_incidents does not exist.
-- Example: psql -U postgres -d YourDatabase -f create_match_incidents_manual.sql

CREATE TABLE IF NOT EXISTS match_incidents (
    id uuid PRIMARY KEY,
    match_id uuid NOT NULL,
    minute integer NOT NULL,
    team_id uuid,
    player_name character varying(200) NOT NULL,
    incident_type character varying(20) NOT NULL,
    notes character varying(500) NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    "DeletedAt" timestamp with time zone NULL,
    CONSTRAINT FK_match_incidents_fixtures_match_id
        FOREIGN KEY (match_id) REFERENCES fixtures(id) ON DELETE CASCADE,
    CONSTRAINT FK_match_incidents_teams_team_id
        FOREIGN KEY (team_id) REFERENCES teams(id) ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS IX_match_incidents_match_id ON match_incidents(match_id);
CREATE INDEX IF NOT EXISTS IX_match_incidents_team_id ON match_incidents(team_id);
