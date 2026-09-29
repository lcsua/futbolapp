import { useState, useMemo, useEffect, useRef, useLayoutEffect } from 'react'
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
  Paper,
  Select,
  Typography,
  CircularProgress,
  Snackbar,
  Chip,
  Tooltip,
  FormControlLabel,
  Checkbox,
} from '@mui/material'
import ContentCopyIcon from '@mui/icons-material/ContentCopy'
import ContentPasteGoIcon from '@mui/icons-material/ContentPasteGo'
import SaveIcon from '@mui/icons-material/Save'
import LockIcon from '@mui/icons-material/Lock'
import LockOpenIcon from '@mui/icons-material/LockOpen'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Link as RouterLink, useNavigate } from 'react-router-dom'
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
import { seasonsService, type TeamInSetup } from '../api/seasons'
import { useLeagueId } from '../contexts/LeagueContext'
import { QuickCreateTeamForDivisionDialog } from '../components/QuickCreateTeamForDivisionDialog'
import { ImportCategoryRostersDialog } from '../components/ImportCategoryRostersDialog'
import { CrestImg } from '../components/CrestImg'
import { effectiveTeamLogoUrl } from '../utils/teamLogo'

const UNASSIGNED_ID = 'unassigned'

type BoardDivision = {
  divisionId: string
  divisionName: string
  teams: TeamInSetup[]
  fixturesLocked?: boolean
  teamIdsWithFixtures: string[]
  savedTeamIds: string[]
}

type LockedDivisionChange = {
  divisionId: string
  divisionName: string
  added: TeamInSetup[]
  removed: TeamInSetup[]
}

function getTeamDisplayName(team: TeamInSetup): string {
  return team.displayName ?? team.name
}

function sortTeamsByNameAndSuffix(teams: TeamInSetup[]): TeamInSetup[] {
  return [...teams].sort((a, b) => {
    const nameCmp = a.name.localeCompare(b.name, undefined, { sensitivity: 'base' })
    if (nameCmp !== 0) return nameCmp
    return (a.suffix ?? '').localeCompare(b.suffix ?? '', undefined, { sensitivity: 'base' })
  })
}

function TeamCardContent({ team, divisionName }: { team: TeamInSetup; divisionName: string }) {
  const tooltipLines = [
    `Display name: ${getTeamDisplayName(team)}`,
    team.clubName ? `Club: ${team.clubName}` : null,
    `Division: ${divisionName}`,
  ].filter(Boolean) as string[]
  const logo = effectiveTeamLogoUrl(team)

  return (
    <Tooltip title={tooltipLines.join(' | ')} arrow>
      <CardContent sx={{ py: 1, px: 1.5, '&:last-child': { pb: 1 } }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          {logo ? (
            <CrestImg src={logo} alt="" size={28} />
          ) : null}
          <Box sx={{ minWidth: 0 }}>
            <Typography variant="body2" fontWeight={500}>
              {getTeamDisplayName(team)}
            </Typography>
            {team.clubName ? (
              <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                Club: {team.clubName}
              </Typography>
            ) : null}
            {team.shortName && (
              <Typography variant="caption" color="text.secondary">
                {team.shortName}
              </Typography>
            )}
          </Box>
        </Box>
      </CardContent>
    </Tooltip>
  )
}

function TeamCard({
  team,
  divisionName,
  disabled = false,
}: {
  team: TeamInSetup
  divisionName: string
  disabled?: boolean
}) {
  const { attributes, listeners, setNodeRef, transform, isDragging } = useDraggable({
    id: team.id,
    data: { team },
    disabled,
  })
  const style = transform
    ? { transform: CSS.Translate.toString(transform), opacity: disabled ? 0.85 : isDragging ? 0.5 : 1 }
    : { opacity: disabled ? 0.85 : isDragging ? 0.5 : 1 }

  return (
    <Card
      ref={setNodeRef}
      {...(disabled ? {} : listeners)}
      {...(disabled ? {} : attributes)}
      elevation={1}
      sx={{
        mb: 1,
        cursor: disabled ? 'default' : isDragging ? 'grabbing' : 'grab',
        flexShrink: 0,
        ...style,
      }}
      variant="outlined"
    >
      <TeamCardContent team={team} divisionName={divisionName} />
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
  isSticky = false,
  groupByClub = false,
  locked = false,
  fixturesLocked = false,
  headerAction,
  onHeaderDoubleClick,
}: {
  id: string
  title: string
  teams: TeamInSetup[]
  teamIds: string[]
  onRenderCard: (team: TeamInSetup) => React.ReactNode
  colorHint?: 'default' | 'unassigned'
  isSticky?: boolean
  groupByClub?: boolean
  locked?: boolean
  fixturesLocked?: boolean
  headerAction?: React.ReactNode
  onHeaderDoubleClick?: () => void
}) {
  const { isOver, setNodeRef } = useDroppable({ id, disabled: locked })
  const renderedTeams = sortTeamsByNameAndSuffix(teams)
  const groupedTeams = groupByClub
    ? renderedTeams.reduce<Record<string, TeamInSetup[]>>((acc, team) => {
        const key = team.clubName || 'No club'
        if (!acc[key]) acc[key] = []
        acc[key].push(team)
        return acc
      }, {})
    : null

  return (
    <Paper
      ref={setNodeRef}
      elevation={2}
      sx={{
        minWidth: { xs: 260, sm: 280 },
        flexShrink: 0,
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        border: isOver && !locked ? 2 : 0,
        borderColor: 'primary.main',
        bgcolor: locked
          ? 'action.selected'
          : colorHint === 'unassigned'
            ? 'action.hover'
            : 'background.paper',
        ...(isSticky && {
          position: 'sticky',
          left: 0,
          zIndex: 2,
          bgcolor: 'background.default',
          boxShadow: '2px 0 8px rgba(0,0,0,0.08)',
        }),
      }}
    >
      <CardContent sx={{ pb: 0, flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            gap: 1,
            mb: 1,
            flexWrap: 'wrap',
            ...(!locked && onHeaderDoubleClick && { cursor: 'pointer', userSelect: 'none' }),
          }}
          onDoubleClick={locked ? undefined : onHeaderDoubleClick}
          title={
            locked
              ? 'Fixtures committed — teams locked for this division'
              : onHeaderDoubleClick
                ? 'Double-click to add a team to this division'
                : undefined
          }
        >
          <Typography variant="subtitle1" fontWeight={600}>
            {title}
          </Typography>
          <Chip label={`${teamIds.length} teams`} size="small" />
          {fixturesLocked && (
            <Chip
              label={locked ? 'Fixtures locked' : 'Agregando equipos'}
              size="small"
              color="warning"
              variant={locked ? 'outlined' : 'filled'}
            />
          )}
          {headerAction}
        </Box>
        <Box sx={{ flex: 1, overflowY: 'auto', minHeight: 120 }}>
          {teams.length === 0 ? (
            <Typography variant="body2" color="text.secondary" sx={{ fontStyle: 'italic' }}>
              No teams assigned
            </Typography>
          ) : groupByClub && groupedTeams ? (
            Object.entries(groupedTeams).map(([clubName, clubTeams]) => (
              <Box key={clubName} sx={{ mb: 1.25 }}>
                <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5 }}>
                  {clubName}
                </Typography>
                {clubTeams.map((t) => (
                  <Box key={t.id}>{onRenderCard(t)}</Box>
                ))}
              </Box>
            ))
          ) : (
            renderedTeams.map((t) => (
              <Box key={t.id}>{onRenderCard(t)}</Box>
            ))
          )}
        </Box>
      </CardContent>
    </Paper>
  )
}

export function AdvancedSeasonSetupPage() {
  const leagueId = useLeagueId()
  const queryClient = useQueryClient()
  const [seasonId, setSeasonId] = useState<string>('')
  const [board, setBoard] = useState<{ unassignedTeams: TeamInSetup[]; divisions: BoardDivision[] } | null>(null)
  const [activeTeam, setActiveTeam] = useState<TeamInSetup | null>(null)
  const [copyDialogOpen, setCopyDialogOpen] = useState(false)
  const [rosterImportOpen, setRosterImportOpen] = useState(false)
  const [sourceSeasonId, setSourceSeasonId] = useState<string>('')
  const [groupByClub, setGroupByClub] = useState(false)
  const [snackbar, setSnackbar] = useState<{ message: string; severity: 'success' | 'error' } | null>(null)
  const [quickCreateDivision, setQuickCreateDivision] = useState<{ divisionId: string; divisionName: string } | null>(null)
  const [unlockedDivisionIds, setUnlockedDivisionIds] = useState<Set<string>>(new Set())
  const [pendingLockedChanges, setPendingLockedChanges] = useState<LockedDivisionChange[] | null>(null)
  const [replanPrompt, setReplanPrompt] = useState<LockedDivisionChange[] | null>(null)
  const navigate = useNavigate()

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 8 } })
  )

  const { data: seasons = [], isLoading: seasonsLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'seasons'],
    queryFn: ({ signal }) => seasonsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })
  const seasonClosed = !!seasons.find((s) => s.id === seasonId && s.isActive === false)

  const { data: setupData, isLoading: setupLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'seasons', seasonId, 'setup'],
    queryFn: ({ signal }) => seasonsService.getSetup(leagueId!, seasonId, signal),
    enabled: !!leagueId && !!seasonId,
  })

  const saveMutation = useMutation({
    mutationFn: async (lockedChanges: LockedDivisionChange[]) => {
      if (!leagueId || !seasonId || !board) return
      await seasonsService.saveSetup(
        leagueId,
        seasonId,
        {
          divisions: board.divisions.map((d) => ({
            divisionId: d.divisionId,
            teamIds: d.teams.map((t) => t.id),
          })),
          allowChangesToLockedDivisions: lockedChanges.length > 0,
        }
      )
    },
    onSuccess: (_, lockedChanges) => {
      setSnackbar({ message: 'Changes saved.', severity: 'success' })
      setPendingLockedChanges(null)
      setUnlockedDivisionIds(new Set())
      const withAdditions = lockedChanges.filter((c) => c.added.length > 0)
      if (withAdditions.length > 0) setReplanPrompt(withAdditions)
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
          fixturesLocked: !!d.fixturesLocked,
          teamIdsWithFixtures: d.teamIdsWithFixtures ?? [],
          savedTeamIds: d.teams.map((t) => t.id),
        })),
      })
      setUnlockedDivisionIds(new Set())
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
    if (seasonClosed) return
    const { active, over } = event
    if (!over || !board) return
    const teamId = active.id as string
    const targetId = over.id as string
    const team = findTeamById(teamId)
    if (!team) return

    const source = getTeamLocation(teamId)
    if (!source) return
    if (source.droppableId === targetId) return

    const sourceDivision = board.divisions.find((d) => d.divisionId === source.droppableId)
    const targetDivision = board.divisions.find((d) => d.divisionId === targetId)
    if (sourceDivision?.teamIdsWithFixtures.includes(teamId)) {
      setSnackbar({
        message: `${getTeamDisplayName(team)} ya tiene partidos en el fixture de ${sourceDivision.divisionName}; no se puede sacar.`,
        severity: 'error',
      })
      return
    }
    if (
      (sourceDivision && isDivisionLocked(sourceDivision)) ||
      (targetDivision && isDivisionLocked(targetDivision))
    ) {
      setSnackbar({
        message: 'Esa división tiene fixture guardado. Usá "Agregar equipos" en su columna para habilitar cambios.',
        severity: 'error',
      })
      return
    }

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

  function isDivisionLocked(division: BoardDivision): boolean {
    return seasonClosed || (!!division.fixturesLocked && !unlockedDivisionIds.has(division.divisionId))
  }

  function lockedDivisionChanges(): LockedDivisionChange[] {
    if (!board) return []
    const allTeams = [...board.unassignedTeams, ...board.divisions.flatMap((d) => d.teams)]
    return board.divisions
      .filter((d) => d.fixturesLocked)
      .map((d) => {
        const current = new Set(d.teams.map((t) => t.id))
        const saved = new Set(d.savedTeamIds)
        return {
          divisionId: d.divisionId,
          divisionName: d.divisionName,
          added: d.teams.filter((t) => !saved.has(t.id)),
          removed: allTeams.filter((t) => saved.has(t.id) && !current.has(t.id)),
        }
      })
      .filter((c) => c.added.length > 0 || c.removed.length > 0)
  }

  const toggleUnlocked = (divisionId: string) => {
    setUnlockedDivisionIds((prev) => {
      const next = new Set(prev)
      if (next.has(divisionId)) next.delete(divisionId)
      else next.add(divisionId)
      return next
    })
  }

  const handleSave = () => {
    const changes = lockedDivisionChanges()
    if (changes.length > 0) {
      setPendingLockedChanges(changes)
      return
    }
    saveMutation.mutate([])
  }

  const goToReplan = (changes: LockedDivisionChange[]) => {
    const params = new URLSearchParams({ seasonId, divisionId: changes[0].divisionId, replan: '1' })
    navigate(`/fixtures?${params.toString()}`)
  }

  const otherSeasons = useMemo(
    () => seasons.filter((s) => s.id !== seasonId),
    [seasons, seasonId]
  )

  const boardScrollRef = useRef<HTMLDivElement>(null)
  const topScrollRef = useRef<HTMLDivElement>(null)
  const [spacerWidth, setSpacerWidth] = useState(0)

  const handleBoardScroll = () => {
    if (topScrollRef.current && boardScrollRef.current) {
      topScrollRef.current.scrollLeft = boardScrollRef.current.scrollLeft
    }
  }

  const handleTopScroll = () => {
    if (boardScrollRef.current && topScrollRef.current) {
      boardScrollRef.current.scrollLeft = topScrollRef.current.scrollLeft
    }
  }

  useLayoutEffect(() => {
    if (!boardScrollRef.current || !board) return
    const updateWidth = () => {
      if (boardScrollRef.current) {
        setSpacerWidth(boardScrollRef.current.scrollWidth)
      }
    }
    updateWidth()
    const ro = new ResizeObserver(updateWidth)
    ro.observe(boardScrollRef.current)
    return () => ro.disconnect()
  }, [board])

  if (!leagueId) {
    return (
      <Alert severity="error" action={<Button component={RouterLink} to="/">Go to Leagues</Button>}>
        No league selected. Choose a league from the selector.
      </Alert>
    )
  }

  return (
    <Box sx={{ width: '100%', px: { xs: 0, sm: 1 } }}>
      <Button component={RouterLink} to="/season-setup" startIcon={<ArrowBackIcon />} size="small" sx={{ mb: 2 }}>
        Back to season setup
      </Button>
      <Typography variant="h5" component="h1" sx={{ mb: 2, fontWeight: 600 }}>
        Advanced season setup
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Drag teams between Unassigned and divisions. Save when done. Double-click a division title to create a team
        and assign it to that division. Divisions with committed fixtures stay locked; other divisions can still be edited.
      </Typography>
      {seasonClosed && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          This season is closed. Setup changes are locked.
        </Alert>
      )}
      {!seasonClosed && board?.divisions.some((d) => d.fixturesLocked) && (
        <Alert severity="info" sx={{ mb: 2 }}>
          Some divisions have committed fixtures and their team list is locked. You can still edit the other divisions.
          Para sumar equipos nuevos a una de ellas usá "Agregar equipos" en su columna; al guardar se pide confirmación.
        </Alert>
      )}

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
          startIcon={<ContentPasteGoIcon />}
          onClick={() => setRosterImportOpen(true)}
          disabled={seasonClosed || !seasonId}
        >
          Pegar categorías
        </Button>
        <Button
          variant="outlined"
          startIcon={<ContentCopyIcon />}
          onClick={() => setCopyDialogOpen(true)}
          disabled={seasonClosed || !seasonId || seasons.length < 2}
        >
          Copy from another season
        </Button>
        <Button
          variant="contained"
          startIcon={saveMutation.isPending ? <CircularProgress size={18} color="inherit" /> : <SaveIcon />}
          onClick={handleSave}
          disabled={seasonClosed || !board || saveMutation.isPending}
        >
          Save changes
        </Button>
        <FormControlLabel
          control={
            <Checkbox
              checked={groupByClub}
              onChange={(e) => setGroupByClub(e.target.checked)}
            />
          }
          label="Group by club"
        />
      </Box>

      {leagueId && seasonId && (
        <ImportCategoryRostersDialog
          open={rosterImportOpen}
          onClose={() => setRosterImportOpen(false)}
          leagueId={leagueId}
          seasonId={seasonId}
          onImported={({ created, reused, divisionsCreated }) => {
            const parts = [
              created ? `${created} creado(s)` : null,
              reused ? `${reused} reutilizado(s)` : null,
              divisionsCreated ? `${divisionsCreated} división(es) nueva(s)` : null,
            ].filter(Boolean)
            setSnackbar({
              message: parts.length ? `Categorías: ${parts.join(', ')}.` : 'Importación de categorías completa.',
              severity: 'success',
            })
            void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId] })
          }}
        />
      )}
      {leagueId && seasonId && quickCreateDivision && (
        <QuickCreateTeamForDivisionDialog
          open
          onClose={() => setQuickCreateDivision(null)}
          onCreated={() => setSnackbar({ message: 'Team created and assigned to the division.', severity: 'success' })}
          leagueId={leagueId}
          seasonId={seasonId}
          divisionId={quickCreateDivision.divisionId}
          divisionName={quickCreateDivision.divisionName}
        />
      )}

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

      <Dialog
        open={!!pendingLockedChanges}
        onClose={saveMutation.isPending ? undefined : () => setPendingLockedChanges(null)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Cambiar equipos de divisiones con fixture</DialogTitle>
        <DialogContent>
          {pendingLockedChanges?.map((c) => (
            <Box key={c.divisionId} sx={{ mb: 1.5 }}>
              <Typography variant="subtitle2">{c.divisionName}</Typography>
              {c.added.length > 0 && (
                <Typography variant="body2">Se agregan: {c.added.map(getTeamDisplayName).join(', ')}</Typography>
              )}
              {c.removed.length > 0 && (
                <Typography variant="body2">Se quitan: {c.removed.map(getTeamDisplayName).join(', ')}</Typography>
              )}
            </Box>
          ))}
          <Alert severity="warning" sx={{ mt: 1 }}>
            El fixture guardado no cambia: los equipos nuevos quedan sin partidos hasta que replanifiques esas zonas
            desde Fixture. Lo jugado y los partidos del resto no se tocan.
          </Alert>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setPendingLockedChanges(null)} disabled={saveMutation.isPending}>
            Cancelar
          </Button>
          <Button
            variant="contained"
            color="warning"
            onClick={() => pendingLockedChanges && saveMutation.mutate(pendingLockedChanges)}
            disabled={saveMutation.isPending}
          >
            {saveMutation.isPending ? <CircularProgress size={20} color="inherit" /> : 'Confirmar y guardar'}
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={!!replanPrompt} onClose={() => setReplanPrompt(null)} maxWidth="sm" fullWidth>
        <DialogTitle>Replanificar el fixture</DialogTitle>
        <DialogContent>
          <Typography variant="body2" sx={{ mb: 1 }}>
            Se sumaron equipos a divisiones que ya tienen fixture:
          </Typography>
          {replanPrompt?.map((c) => (
            <Typography key={c.divisionId} variant="body2">
              <strong>{c.divisionName}:</strong> {c.added.map(getTeamDisplayName).join(', ')}
            </Typography>
          ))}
          <Typography variant="body2" sx={{ mt: 1.5 }}>
            Todavía no tienen partidos. Replanificá desde la próxima fecha para que recuperen los partidos perdidos
            y, si hace falta, se completen las fechas con interzonales.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setReplanPrompt(null)}>Más tarde</Button>
          <Button variant="contained" onClick={() => replanPrompt && goToReplan(replanPrompt)}>
            Replanificar ahora
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
          <Box sx={{ width: '100%' }}>
            <Box
              ref={topScrollRef}
              onScroll={handleTopScroll}
              sx={{
                overflowX: 'auto',
                overflowY: 'hidden',
                pb: 1,
                mb: 0,
                '&::-webkit-scrollbar': { height: 8 },
              }}
            >
              <Box sx={{ width: spacerWidth || '100%', minWidth: '100%', height: 1 }} />
            </Box>
            <Box
              ref={boardScrollRef}
              onScroll={handleBoardScroll}
              sx={{
                display: 'flex',
                overflowX: 'auto',
                overflowY: 'hidden',
                gap: 3,
                pb: 2,
                minHeight: 380,
                alignItems: 'stretch',
                '&::-webkit-scrollbar': { height: 8 },
              }}
            >
              <DroppableColumn
                id={UNASSIGNED_ID}
                title="Unassigned Teams"
                teams={board.unassignedTeams}
                teamIds={board.unassignedTeams.map((t) => t.id)}
                colorHint="unassigned"
                isSticky
                groupByClub={groupByClub}
                locked={seasonClosed}
                onRenderCard={(team) => (
                  <TeamCard
                    key={team.id}
                    team={team}
                    divisionName="Unassigned"
                    disabled={seasonClosed}
                  />
                )}
              />
              {board.divisions.map((div) => (
                <DroppableColumn
                  key={div.divisionId}
                  id={div.divisionId}
                  title={div.divisionName}
                  teams={div.teams}
                  teamIds={div.teams.map((t) => t.id)}
                  groupByClub={groupByClub}
                  locked={isDivisionLocked(div)}
                  fixturesLocked={!!div.fixturesLocked}
                  headerAction={
                    div.fixturesLocked && !seasonClosed ? (
                      <Tooltip
                        title={
                          unlockedDivisionIds.has(div.divisionId)
                            ? 'Volver a bloquear la columna. Lo que ya moviste se confirma igual al guardar.'
                            : 'Habilita sumar equipos a esta división aunque tenga fixture. Los equipos con partidos no se pueden sacar.'
                        }
                      >
                        <Button
                          size="small"
                          variant="text"
                          startIcon={unlockedDivisionIds.has(div.divisionId) ? <LockIcon /> : <LockOpenIcon />}
                          onClick={() => toggleUnlocked(div.divisionId)}
                        >
                          {unlockedDivisionIds.has(div.divisionId) ? 'Bloquear' : 'Agregar equipos'}
                        </Button>
                      </Tooltip>
                    ) : undefined
                  }
                  onHeaderDoubleClick={
                    div.fixturesLocked || seasonClosed
                      ? undefined
                      : () =>
                          setQuickCreateDivision({
                            divisionId: div.divisionId,
                            divisionName: div.divisionName,
                          })
                  }
                  onRenderCard={(team) => (
                    <TeamCard
                      key={team.id}
                      team={team}
                      divisionName={div.divisionName}
                      disabled={isDivisionLocked(div) || div.teamIdsWithFixtures.includes(team.id)}
                    />
                  )}
                />
              ))}
            </Box>
          </Box>

          <DragOverlay>
            {activeTeam ? (
              <Card elevation={1} sx={{ cursor: 'grabbing' }} variant="outlined">
                <TeamCardContent team={activeTeam} divisionName="Dragging" />
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
