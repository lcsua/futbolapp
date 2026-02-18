import { useState, useMemo, useEffect } from 'react'
import type { SelectChangeEvent } from '@mui/material'
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Typography,
  CircularProgress,
  Snackbar,
  Chip,
} from '@mui/material'
import ContentCopyIcon from '@mui/icons-material/ContentCopy'
import SaveIcon from '@mui/icons-material/Save'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Link as RouterLink } from 'react-router-dom'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import {
  DndContext,
  DragOverlay,
  useDraggable,
  useDroppable,
  type DragEndEvent,
  type DragStartEvent,
  PointerSensor,
  useSensor,
  useSensors,
} from '@dnd-kit/core'
import { CSS } from '@dnd-kit/utilities'
import { seasonsService, type SeasonSetupResponse, type TeamInSetup } from '../api/seasons'
import { useLeagueId } from '../contexts/LeagueContext'

const UNASSIGNED_ID = 'unassigned'

type BoardDivision = { divisionId: string; divisionName: string; teams: TeamInSetup[] }

function TeamCardContent({ team }: { team: TeamInSetup }) {
  return (
    <CardContent sx={{ py: 1, px: 1.5, '&:last-child': { pb: 1 } }}>
      <Typography variant="body2" fontWeight={500}>
        {team.name}
      </Typography>
      {team.shortName && (
        <Typography variant="caption" color="text.secondary">
          {team.shortName}
        </Typography>
      )}
    </CardContent>
  )
}

function TeamCard({ team }: { team: TeamInSetup }) {
  const { attributes, listeners, setNodeRef, transform, isDragging } = useDraggable({
    id: team.id,
    data: { team },
  })
  const style = transform
    ? { transform: CSS.Translate.toString(transform), opacity: isDragging ? 0.5 : 1 }
    : { opacity: isDragging ? 0.5 : 1 }

  return (
    <Card
      ref={setNodeRef}
      {...listeners}
      {...attributes}
      sx={{ mb: 1, cursor: 'grab', boxShadow: 1, ...style }}
      variant="outlined"
    >
      <TeamCardContent team={team} />
    </Card>
  )
}

function DroppableColumn({
  id,
  title,
  teams,
  teamIds,
  onRenderCard,
  colorHint = 'default',
}: {
  id: string
  title: string
  teams: TeamInSetup[]
  teamIds: string[]
  onRenderCard: (team: TeamInSetup) => React.ReactNode
  colorHint?: 'default' | 'unassigned'
}) {
  const { isOver, setNodeRef } = useDroppable({ id })
  return (
    <Card
      ref={setNodeRef}
      sx={{
        minWidth: 220,
        maxWidth: 260,
        flexShrink: 0,
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        boxShadow: 2,
        border: isOver ? 2 : 0,
        borderColor: 'primary.main',
        bgcolor: colorHint === 'unassigned' ? 'action.hover' : 'background.paper',
      }}
    >
      <CardContent sx={{ pb: 0, flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
          <Typography variant="subtitle1" fontWeight={600}>
            {title}
          </Typography>
          <Chip label={`${teamIds.length} teams`} size="small" />
        </Box>
        <Box sx={{ flex: 1, overflowY: 'auto', minHeight: 120 }}>
          {teams.length === 0 ? (
            <Typography variant="body2" color="text.secondary" sx={{ fontStyle: 'italic' }}>
              No teams assigned
            </Typography>
          ) : (
            teams.map((t) => (
              <Box key={t.id}>{onRenderCard(t)}</Box>
            ))
          )}
        </Box>
      </CardContent>
    </Card>
  )
}

export function AdvancedSeasonSetupPage() {
  const leagueId = useLeagueId()
  const queryClient = useQueryClient()
  const [seasonId, setSeasonId] = useState<string>('')
  const [board, setBoard] = useState<{ unassignedTeams: TeamInSetup[]; divisions: BoardDivision[] } | null>(null)
  const [activeTeam, setActiveTeam] = useState<TeamInSetup | null>(null)
  const [copyDialogOpen, setCopyDialogOpen] = useState(false)
  const [sourceSeasonId, setSourceSeasonId] = useState<string>('')
  const [snackbar, setSnackbar] = useState<{ message: string; severity: 'success' | 'error' } | null>(null)

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 8 } })
  )

  const { data: seasons = [], isLoading: seasonsLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'seasons'],
    queryFn: ({ signal }) => seasonsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })

  const { data: setupData, isLoading: setupLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'seasons', seasonId, 'setup'],
    queryFn: ({ signal }) => seasonsService.getSetup(leagueId!, seasonId, signal),
    enabled: !!leagueId && !!seasonId,
  })

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (!leagueId || !seasonId || !board) return
      await seasonsService.saveSetup(
        leagueId,
        seasonId,
        {
          divisions: board.divisions.map((d) => ({
            divisionId: d.divisionId,
            teamIds: d.teams.map((t) => t.id),
          })),
        }
      )
    },
    onSuccess: () => {
      setSnackbar({ message: 'Changes saved.', severity: 'success' })
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'seasons', seasonId, 'setup'] })
    },
    onError: (err) => {
      setSnackbar({ message: err instanceof Error ? err.message : 'Save failed', severity: 'error' })
    },
  })

  const copyMutation = useMutation({
    mutationFn: async () => {
      if (!leagueId || !seasonId || !sourceSeasonId) return
      await seasonsService.copyFrom(leagueId, seasonId, sourceSeasonId)
    },
    onSuccess: () => {
      setCopyDialogOpen(false)
      setSourceSeasonId('')
      setSnackbar({ message: 'Season setup copied.', severity: 'success' })
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'seasons', seasonId, 'setup'] })
    },
    onError: (err) => {
      setSnackbar({ message: err instanceof Error ? err.message : 'Copy failed', severity: 'error' })
    },
  })

  useEffect(() => {
    if (seasonId && setupData) {
      setBoard({
        unassignedTeams: [...setupData.unassignedTeams],
        divisions: setupData.divisions.map((d) => ({
          divisionId: d.divisionId,
          divisionName: d.divisionName,
          teams: [...d.teams],
        })),
      })
    } else if (seasonId && !setupLoading) {
      setBoard(null)
    }
  }, [seasonId, setupData, setupLoading])

  const handleSeasonChange = (e: SelectChangeEvent<string>) => {
    setSeasonId(e.target.value)
    setBoard(null)
  }

  const handleDragStart = (event: DragStartEvent) => {
    const team = findTeamById(event.active.id as string)
    if (team) setActiveTeam(team)
  }

  const handleDragEnd = (event: DragEndEvent) => {
    setActiveTeam(null)
    const { active, over } = event
    if (!over || !board) return
    const teamId = active.id as string
    const targetId = over.id as string
    const team = findTeamById(teamId)
    if (!team) return

    const source = getTeamLocation(teamId)
    if (!source) return
    if (source.droppableId === targetId) return

    setBoard((prev) => {
      if (!prev) return prev
      let unassigned = [...prev.unassignedTeams]
      const divisions = prev.divisions.map((d) => ({ ...d, teams: [...d.teams] }))

      if (source.droppableId === UNASSIGNED_ID) {
        unassigned = unassigned.filter((t) => t.id !== teamId)
      } else {
        const div = divisions.find((d) => d.divisionId === source.droppableId)
        if (div) div.teams = div.teams.filter((t) => t.id !== teamId)
      }

      if (targetId === UNASSIGNED_ID) {
        unassigned = [...unassigned, team]
      } else {
        const div = divisions.find((d) => d.divisionId === targetId)
        if (div) div.teams = [...div.teams, team]
      }

      return { unassignedTeams: unassigned, divisions }
    })
  }

  function findTeamById(id: string): TeamInSetup | null {
    if (!board) return null
    const inUnassigned = board.unassignedTeams.find((t) => t.id === id)
    if (inUnassigned) return inUnassigned
    for (const d of board.divisions) {
      const t = d.teams.find((x) => x.id === id)
      if (t) return t
    }
    return null
  }

  function getTeamLocation(teamId: string): { droppableId: string } | null {
    if (!board) return null
    if (board.unassignedTeams.some((t) => t.id === teamId)) return { droppableId: UNASSIGNED_ID }
    for (const d of board.divisions) {
      if (d.teams.some((t) => t.id === teamId)) return { droppableId: d.divisionId }
    }
    return null
  }

  const handleSave = () => {
    saveMutation.mutate()
  }

  const otherSeasons = useMemo(
    () => seasons.filter((s) => s.id !== seasonId),
    [seasons, seasonId]
  )

  if (!leagueId) {
    return (
      <Alert severity="error" action={<Button component={RouterLink} to="/">Go to Leagues</Button>}>
        No league selected. Choose a league from the selector.
      </Alert>
    )
  }

  return (
    <Box>
      <Button component={RouterLink} to="/season-setup" startIcon={<ArrowBackIcon />} size="small" sx={{ mb: 2 }}>
        Back to season setup
      </Button>
      <Typography variant="h5" component="h1" sx={{ mb: 2, fontWeight: 600 }}>
        Advanced season setup
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Drag teams between Unassigned and divisions. Save when done.
      </Typography>

      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, alignItems: 'center', mb: 3 }}>
        <FormControl size="small" sx={{ minWidth: 200 }} disabled={seasonsLoading}>
          <InputLabel id="adv-season-label">Season</InputLabel>
          <Select
            labelId="adv-season-label"
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
        <Button
          variant="outlined"
          startIcon={<ContentCopyIcon />}
          onClick={() => setCopyDialogOpen(true)}
          disabled={!seasonId || seasons.length < 2}
        >
          Copy from another season
        </Button>
        <Button
          variant="contained"
          startIcon={saveMutation.isPending ? <CircularProgress size={18} color="inherit" /> : <SaveIcon />}
          onClick={handleSave}
          disabled={!board || saveMutation.isPending}
        >
          Save changes
        </Button>
      </Box>

      <Dialog open={copyDialogOpen} onClose={() => { setCopyDialogOpen(false); setSourceSeasonId('') }} maxWidth="xs" fullWidth>
        <DialogTitle>Copy from another season</DialogTitle>
        <DialogContent>
          <FormControl fullWidth size="small" sx={{ mt: 1 }}>
            <InputLabel>Source season</InputLabel>
            <Select
              label="Source season"
              value={sourceSeasonId}
              onChange={(e) => setSourceSeasonId(e.target.value)}
            >
              <MenuItem value="">
                <em>Select season</em>
              </MenuItem>
              {otherSeasons.map((s) => (
                <MenuItem key={s.id} value={s.id}>
                  {s.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => { setCopyDialogOpen(false); setSourceSeasonId('') }}>Cancel</Button>
          <Button
            variant="contained"
            onClick={() => copyMutation.mutate()}
            disabled={!sourceSeasonId || copyMutation.isPending}
          >
            {copyMutation.isPending ? <CircularProgress size={20} /> : 'Confirm'}
          </Button>
        </DialogActions>
      </Dialog>

      {snackbar && (
        <Snackbar
          open={!!snackbar}
          autoHideDuration={5000}
          onClose={() => setSnackbar(null)}
          message={snackbar.message}
          ContentProps={{ sx: { bgcolor: snackbar.severity === 'error' ? 'error.main' : 'success.main', color: 'white' } }}
        />
      )}

      {setupLoading && seasonId && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress />
        </Box>
      )}

      {!setupLoading && seasonId && board && (
        <DndContext sensors={sensors} onDragStart={handleDragStart} onDragEnd={handleDragEnd}>
          <Box
            sx={{
              display: 'flex',
              gap: 2,
              overflowX: 'auto',
              pb: 2,
              minHeight: 380,
              alignItems: 'stretch',
            }}
          >
            <DroppableColumn
              id={UNASSIGNED_ID}
              title="Unassigned Teams"
              teams={board.unassignedTeams}
              teamIds={board.unassignedTeams.map((t) => t.id)}
              colorHint="unassigned"
              onRenderCard={(team) => <TeamCard key={team.id} team={team} />}
            />
            {board.divisions.map((div) => (
              <DroppableColumn
                key={div.divisionId}
                id={div.divisionId}
                title={div.divisionName}
                teams={div.teams}
                teamIds={div.teams.map((t) => t.id)}
                onRenderCard={(team) => <TeamCard key={team.id} team={team} />}
              />
            ))}
          </Box>

          <DragOverlay>
            {activeTeam ? (
              <Card sx={{ boxShadow: 3, cursor: 'grabbing' }} variant="outlined">
                <TeamCardContent team={activeTeam} />
              </Card>
            ) : null}
          </DragOverlay>
        </DndContext>
      )}

      {!setupLoading && seasonId && !board && (
        <Typography color="text.secondary">No setup data.</Typography>
      )}
    </Box>
  )
}
