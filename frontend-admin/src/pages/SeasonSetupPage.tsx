import { useMemo, useState } from 'react'
import type { SelectChangeEvent } from '@mui/material'
import {
  Alert,
  Box,
  Button,
  FormControl,
  InputLabel,
  ListItemText,
  MenuItem,
  Select,
  Typography,
  CircularProgress,
  Chip,
  OutlinedInput,
  Divider,
} from '@mui/material'
import CheckIcon from '@mui/icons-material/Check'
import UploadFileIcon from '@mui/icons-material/UploadFile'
import PersonRemoveIcon from '@mui/icons-material/PersonRemove'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Link as RouterLink } from 'react-router-dom'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { seasonsService } from '../api/seasons'
import { divisionsService } from '../api/divisions'
import { teamsService } from '../api/teams'
import { useLeagueId } from '../contexts/LeagueContext'
import { ImportTeamsCsvDialog } from '../components/ImportTeamsCsvDialog'

function getTeamDisplayName(team: { name: string; displayName?: string | null }) {
  return team.displayName ?? team.name
}

export function SeasonSetupPage() {
  const leagueId = useLeagueId()
  const queryClient = useQueryClient()
  const [seasonId, setSeasonId] = useState<string>('')
  const [divisionId, setDivisionId] = useState<string>('')
  const [teamIds, setTeamIds] = useState<string[]>([])
  const [unassignTeamIds, setUnassignTeamIds] = useState<string[]>([])
  const [assignError, setAssignError] = useState<string | null>(null)
  const [assignSuccess, setAssignSuccess] = useState<string | null>(null)
  const [importOpen, setImportOpen] = useState(false)

  const { data: seasons = [], isLoading: seasonsLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'seasons'],
    queryFn: ({ signal }) => seasonsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })
  const { data: divisions = [], isLoading: divisionsLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'divisions'],
    queryFn: ({ signal }) => divisionsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })
  const { data: teams = [], isLoading: teamsLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'teams'],
    queryFn: ({ signal }) => teamsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })
  const seasonClosed = !!seasons.find((s) => s.id === seasonId && s.isActive === false)
  const { data: setupData } = useQuery({
    queryKey: ['leagues', leagueId, 'seasons', seasonId, 'setup'],
    queryFn: ({ signal }) => seasonsService.getSetup(leagueId!, seasonId, signal),
    enabled: !!leagueId && !!seasonId,
  })
  const selectedDivisionSetup = setupData?.divisions.find((d) => d.divisionId === divisionId)
  const divisionFixturesLocked = !!selectedDivisionSetup?.fixturesLocked
  const teamsInDivision = useMemo(() => {
    const list = selectedDivisionSetup?.teams ?? []
    return [...list].sort((a, b) => {
      const aName = getTeamDisplayName(a)
      const bName = getTeamDisplayName(b)
      const nameCmp = aName.localeCompare(bName, undefined, { sensitivity: 'base' })
      if (nameCmp !== 0) return nameCmp
      return (a.suffix ?? '').localeCompare(b.suffix ?? '', undefined, { sensitivity: 'base' })
    })
  }, [selectedDivisionSetup])

  const { data: assignedData } = useQuery({
    queryKey: ['leagues', leagueId, 'seasons', seasonId, 'assigned-team-ids'],
    queryFn: ({ signal }) => seasonsService.getAssignedTeamIds(leagueId!, seasonId, signal),
    enabled: !!leagueId && !!seasonId,
  })
  const assignedTeamIds = assignedData?.teamIds ?? []

  const invalidateAssignmentQueries = () => {
    void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'seasons'] })
    void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'seasons', seasonId, 'assigned-team-ids'] })
    void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'seasons', seasonId, 'setup'] })
    void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'teams'] })
  }

  const assignMutation = useMutation({
    mutationFn: async () => {
      setAssignError(null)
      setAssignSuccess(null)
      const errors: string[] = []
      let successCount = 0
      for (const teamId of teamIds) {
        try {
          await teamsService.assignTeamToDivisionSeason(leagueId!, seasonId, divisionId, teamId)
          successCount += 1
        } catch (e) {
          const msg = e instanceof Error ? e.message : 'Failed to assign team'
          const team = teams.find((t) => t.id === teamId)
          errors.push(team ? `${getTeamDisplayName(team)}: ${msg}` : msg)
        }
      }
      if (errors.length > 0) throw new Error(errors.join('\n'))
      return successCount
    },
    onSuccess: (count) => {
      setAssignSuccess(`${count} team(s) assigned.`)
      setTeamIds([])
      invalidateAssignmentQueries()
    },
    onError: (err) => {
      setAssignError(err instanceof Error ? err.message : 'Assignment failed')
    },
  })

  const unassignMutation = useMutation({
    mutationFn: async () => {
      setAssignError(null)
      setAssignSuccess(null)
      const errors: string[] = []
      let successCount = 0
      for (const teamId of unassignTeamIds) {
        try {
          await teamsService.unassignTeamFromDivisionSeason(leagueId!, seasonId, divisionId, teamId)
          successCount += 1
        } catch (e) {
          const msg = e instanceof Error ? e.message : 'Failed to unassign team'
          const team = teamsInDivision.find((t) => t.id === teamId)
          errors.push(team ? `${getTeamDisplayName(team)}: ${msg}` : msg)
        }
      }
      if (errors.length > 0) throw new Error(errors.join('\n'))
      return successCount
    },
    onSuccess: (count) => {
      setAssignSuccess(`${count} team(s) unassigned.`)
      setUnassignTeamIds([])
      invalidateAssignmentQueries()
    },
    onError: (err) => {
      setAssignError(err instanceof Error ? err.message : 'Unassign failed')
    },
  })

  const handleSeasonChange = (e: SelectChangeEvent<string>) => {
    setSeasonId(e.target.value)
    setTeamIds([])
    setUnassignTeamIds([])
  }
  const handleDivisionChange = (e: SelectChangeEvent<string>) => {
    setDivisionId(e.target.value)
    setTeamIds([])
    setUnassignTeamIds([])
  }
  const handleTeamsChange = (e: SelectChangeEvent<string[]>) => {
    const value = e.target.value
    const next = typeof value === 'string' ? value.split(',') : value
    setTeamIds(next.filter((id) => !assignedTeamIds.includes(id)))
  }
  const handleUnassignTeamsChange = (e: SelectChangeEvent<string[]>) => {
    const value = e.target.value
    setUnassignTeamIds(typeof value === 'string' ? value.split(',') : value)
  }

  const canSave =
    !!leagueId &&
    !!seasonId &&
    !!divisionId &&
    teamIds.length > 0 &&
    !assignMutation.isPending &&
    !unassignMutation.isPending &&
    !seasonClosed &&
    !divisionFixturesLocked

  const canUnassign =
    !!leagueId &&
    !!seasonId &&
    !!divisionId &&
    unassignTeamIds.length > 0 &&
    !assignMutation.isPending &&
    !unassignMutation.isPending &&
    !seasonClosed &&
    !divisionFixturesLocked

  const sortedTeams = [...teams].sort((a, b) => {
    const nameCmp = a.name.localeCompare(b.name, undefined, { sensitivity: 'base' })
    if (nameCmp !== 0) return nameCmp
    return (a.suffix ?? '').localeCompare(b.suffix ?? '', undefined, { sensitivity: 'base' })
  })

  if (!leagueId) {
    return (
      <Alert severity="error" action={<Button component={RouterLink} to="/">Go to Leagues</Button>}>
        No league selected. Choose a league from the selector.
      </Alert>
    )
  }

  return (
    <Box>
      <Button component={RouterLink} to="/seasons" startIcon={<ArrowBackIcon />} size="small" sx={{ mb: 2 }}>
        Back to seasons
      </Button>
      <Typography variant="h5" component="h1" sx={{ mb: 3, fontWeight: 600 }}>
        Season setup — assign teams to division
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Assign or unassign teams for a division in a season. Each team can be in only one division per season.
        Divisions with committed fixtures stay locked.
      </Typography>
      {seasonClosed && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          This season is closed. Team assignments are locked.
        </Alert>
      )}
      {!seasonClosed && divisionFixturesLocked && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          This division already has committed fixtures. Team assignments for it are locked; pick another division to edit.
        </Alert>
      )}
      {seasonId ? (
        <Alert severity="info" sx={{ mb: 2 }}>
          Para configurar <strong>duración de partidos, horarios y campos por categoría</strong>, usa{' '}
          <Button component={RouterLink} to={`/seasons/${seasonId}/division-scheduling`} size="small" variant="contained" color="secondary">
            Reglas por división (esta temporada)
          </Button>
        </Alert>
      ) : null}

      {assignError && (
        <Alert severity="error" sx={{ mb: 2, whiteSpace: 'pre-wrap' }} onClose={() => setAssignError(null)}>
          {assignError}
        </Alert>
      )}
      {assignSuccess && (
        <Alert severity="success" sx={{ mb: 2 }} onClose={() => setAssignSuccess(null)}>
          {assignSuccess}
        </Alert>
      )}

      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, maxWidth: 480 }}>
        <FormControl fullWidth disabled={seasonsLoading}>
          <InputLabel id="season-label">Season</InputLabel>
          <Select
            labelId="season-label"
            label="Season"
            value={seasonId}
            onChange={handleSeasonChange}
          >
            <MenuItem value="">
              <em>Select season</em>
            </MenuItem>
            {seasons.map((s) => (
              <MenuItem key={s.id} value={s.id}>
                {s.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <FormControl fullWidth disabled={divisionsLoading}>
          <InputLabel id="division-label">Division</InputLabel>
          <Select
            labelId="division-label"
            label="Division"
            value={divisionId}
            onChange={handleDivisionChange}
          >
            <MenuItem value="">
              <em>Select division</em>
            </MenuItem>
            {divisions.map((d) => (
              <MenuItem key={d.id} value={d.id}>
                {d.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>

        <Typography variant="subtitle2" sx={{ mt: 1 }}>
          Asignar equipos
        </Typography>
        <Typography variant="body2" sx={{ mb: 0.5 }}>
          Selected {teamIds.length} teams
        </Typography>
        <FormControl fullWidth disabled={teamsLoading || !seasonId || !divisionId}>
          <InputLabel id="teams-label">Teams</InputLabel>
          <Select
            labelId="teams-label"
            label="Teams"
            multiple
            value={teamIds}
            onChange={handleTeamsChange}
            input={<OutlinedInput label="Teams" />}
            renderValue={(selected) => (
              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                {selected.map((id) => {
                  const t = teams.find((x) => x.id === id)
                  return <Chip key={id} label={t ? getTeamDisplayName(t) : id} size="small" />
                })}
              </Box>
            )}
          >
            {sortedTeams.map((t) => {
              const alreadyAssigned = assignedTeamIds.includes(t.id)
              return (
                <MenuItem key={t.id} value={t.id} disabled={alreadyAssigned}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', width: '100%' }}>
                    <ListItemText
                      primary={alreadyAssigned ? `${getTeamDisplayName(t)} (Already assigned)` : getTeamDisplayName(t)}
                      secondary={alreadyAssigned ? undefined : (t.clubName ? `Club: ${t.clubName}` : t.shortName || undefined)}
                      primaryTypographyProps={alreadyAssigned ? { color: 'text.secondary' } : undefined}
                    />
                    {teamIds.includes(t.id) && <CheckIcon fontSize="small" color="primary" />}
                  </Box>
                </MenuItem>
              )
            })}
          </Select>
        </FormControl>
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, alignItems: 'center' }}>
          <Button
            variant="contained"
            disabled={!canSave}
            onClick={() => assignMutation.mutate()}
          >
            {assignMutation.isPending ? <CircularProgress size={24} color="inherit" /> : 'Save assignment'}
          </Button>
          <Button
            variant="outlined"
            startIcon={<UploadFileIcon />}
            disabled={!seasonId || !divisionId || seasonClosed || divisionFixturesLocked}
            onClick={() => setImportOpen(true)}
          >
            Importar CSV
          </Button>
        </Box>

        <Divider sx={{ my: 1 }} />

        <Typography variant="subtitle2">Desasignar equipos de esta división</Typography>
        <Typography variant="body2" color="text.secondary">
          {divisionId
            ? `${teamsInDivision.length} equipo(s) actualmente en la división.`
            : 'Elegí una división para ver los equipos asignados.'}
        </Typography>
        <FormControl fullWidth disabled={!seasonId || !divisionId || teamsInDivision.length === 0}>
          <InputLabel id="unassign-teams-label">Equipos en la división</InputLabel>
          <Select
            labelId="unassign-teams-label"
            label="Equipos en la división"
            multiple
            value={unassignTeamIds}
            onChange={handleUnassignTeamsChange}
            input={<OutlinedInput label="Equipos en la división" />}
            renderValue={(selected) => (
              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                {selected.map((id) => {
                  const t = teamsInDivision.find((x) => x.id === id)
                  return <Chip key={id} label={t ? getTeamDisplayName(t) : id} size="small" />
                })}
              </Box>
            )}
          >
            {teamsInDivision.map((t) => (
              <MenuItem key={t.id} value={t.id}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', width: '100%' }}>
                  <ListItemText primary={getTeamDisplayName(t)} />
                  {unassignTeamIds.includes(t.id) && <CheckIcon fontSize="small" color="primary" />}
                </Box>
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
          <Button
            variant="outlined"
            color="warning"
            startIcon={
              unassignMutation.isPending ? <CircularProgress size={18} color="inherit" /> : <PersonRemoveIcon />
            }
            disabled={!canUnassign}
            onClick={() => unassignMutation.mutate()}
          >
            Desasignar seleccionados
          </Button>
          <Button
            variant="text"
            color="warning"
            disabled={
              !seasonId ||
              !divisionId ||
              teamsInDivision.length === 0 ||
              seasonClosed ||
              divisionFixturesLocked ||
              unassignMutation.isPending ||
              assignMutation.isPending
            }
            onClick={() => setUnassignTeamIds(teamsInDivision.map((t) => t.id))}
          >
            Seleccionar todos
          </Button>
        </Box>

        {seasonId && divisionId && (
          <Button
            component={RouterLink}
            to={`/seasons/${seasonId}/divisions/${divisionId}/division-scheduling-rules`}
            variant="contained"
            color="secondary"
            size="small"
            sx={{ alignSelf: 'flex-start' }}
          >
            Abrir reglas de esta división (partidos y campos)
          </Button>
        )}
      </Box>

      {leagueId && seasonId && divisionId && (
        <ImportTeamsCsvDialog
          open={importOpen}
          onClose={() => setImportOpen(false)}
          leagueId={leagueId}
          seasonId={seasonId}
          divisionId={divisionId}
          divisionName={divisions.find((d) => d.id === divisionId)?.name ?? 'División'}
          teams={teams}
          assignedTeamIds={assignedTeamIds}
          onImported={({ matched, created, skipped }) => {
            const parts = [
              matched ? `${matched} asignado(s)` : null,
              created ? `${created} creado(s)` : null,
              skipped ? `${skipped} omitido(s)` : null,
            ].filter(Boolean)
            setAssignSuccess(parts.length ? `Import CSV: ${parts.join(', ')}.` : 'Import CSV completed.')
            setAssignError(null)
            invalidateAssignmentQueries()
          }}
        />
      )}
    </Box>
  )
}
