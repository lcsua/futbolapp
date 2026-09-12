import { useMemo, useRef, useState } from 'react'
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import ContentPasteGoIcon from '@mui/icons-material/ContentPasteGo'
import UploadFileIcon from '@mui/icons-material/UploadFile'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { divisionsService } from '../api/divisions'
import { seasonsService } from '../api/seasons'
import { teamsService } from '../api/teams'
import type { Club, Division, Team, TeamFormData } from '../api/types'
import {
  findDuplicateCategoryNames,
  findIntraCategoryDuplicates,
  findMatchingDivision,
  importedTeamName,
  normalizeKey,
  parseCategoryRosters,
  publicTeamName,
  type ParsedRosterTeam,
} from '../utils/parseCategoryRosters'

export type ImportCategoryRostersDialogProps = {
  open: boolean
  onClose: () => void
  leagueId: string
  seasonId: string
  onImported?: (summary: { created: number; reused: number; divisionsCreated: number }) => void
}

function takeReusableTeam(pool: Team[], parsed: ParsedRosterTeam, listName: string): Team | undefined {
  const wantClub = normalizeKey(parsed.name)
  const wantSuffix = normalizeKey(parsed.suffix ?? '')
  const wantRaw = normalizeKey(parsed.raw)
  const wantList = normalizeKey(listName)

  const idx = pool.findIndex((team) => {
    const display = normalizeKey(team.displayName ?? team.name)
    const name = normalizeKey(team.name)
    const suffix = normalizeKey(team.suffix ?? '')
    if (display === wantRaw || name === wantRaw) return true
    if ((display === wantList || name === wantList) && suffix === wantSuffix) return true
    return (name === wantClub || name === wantList) && suffix === wantSuffix
  })
  if (idx < 0) return undefined
  return pool.splice(idx, 1)[0]
}

function toUpdatePayload(team: Team, name: string, suffix: string | null, clubId: string): TeamFormData {
  return {
    name,
    suffix: suffix ?? '',
    clubId,
    shortName: team.shortName ?? '',
    primaryColor: '',
    secondaryColor: '',
    foundedYear: team.foundedYear ?? undefined,
    delegateName: team.delegateName ?? '',
    delegateContact: team.delegateContact ?? '',
    email: team.email ?? '',
    logoUrl: team.logoUrl ?? '',
    photoUrl: team.photoUrl ?? '',
  }
}

export function ImportCategoryRostersDialog({
  open,
  onClose,
  leagueId,
  seasonId,
  onImported,
}: ImportCategoryRostersDialogProps) {
  const queryClient = useQueryClient()
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [text, setText] = useState('')
  const [fileName, setFileName] = useState<string | null>(null)
  const [localError, setLocalError] = useState<string | null>(null)
  const [progress, setProgress] = useState<string | null>(null)

  const parsed = useMemo(() => parseCategoryRosters(text), [text])
  const totalTeams = parsed.categories.reduce((sum, c) => sum + c.teams.length, 0)
  const blockingIssues = useMemo(
    () => [
      ...findDuplicateCategoryNames(parsed.categories),
      ...findIntraCategoryDuplicates(parsed.categories),
    ],
    [parsed.categories]
  )
  const uniqueClubNames = useMemo(() => {
    const names = new Set<string>()
    for (const category of parsed.categories) {
      for (const team of category.teams) names.add(normalizeKey(team.name))
    }
    return names.size
  }, [parsed.categories])

  const { data: divisions = [] } = useQuery({
    queryKey: ['leagues', leagueId, 'divisions'],
    queryFn: ({ signal }) => divisionsService.getByLeagueId(leagueId, signal),
    enabled: open && !!leagueId,
  })
  const { data: clubs = [] } = useQuery({
    queryKey: ['leagues', leagueId, 'clubs'],
    queryFn: ({ signal }) => teamsService.getClubsByLeague(leagueId, signal),
    enabled: open && !!leagueId,
  })
  const { data: teams = [] } = useQuery({
    queryKey: ['leagues', leagueId, 'teams'],
    queryFn: ({ signal }) => teamsService.getByLeagueId(leagueId, signal),
    enabled: open && !!leagueId,
  })
  const { data: assignedData } = useQuery({
    queryKey: ['leagues', leagueId, 'seasons', seasonId, 'assigned-team-ids'],
    queryFn: ({ signal }) => seasonsService.getAssignedTeamIds(leagueId, seasonId, signal),
    enabled: open && !!leagueId && !!seasonId,
  })
  const { data: setupData } = useQuery({
    queryKey: ['leagues', leagueId, 'seasons', seasonId, 'setup'],
    queryFn: ({ signal }) => seasonsService.getSetup(leagueId, seasonId, signal),
    enabled: open && !!leagueId && !!seasonId,
  })

  const assignedSet = useMemo(() => new Set(assignedData?.teamIds ?? []), [assignedData])
  const lockedDivisionIds = useMemo(
    () => new Set((setupData?.divisions ?? []).filter((d) => d.fixturesLocked).map((d) => d.divisionId)),
    [setupData]
  )

  const previewRows = useMemo(() => {
    const pool = teams.filter((t) => !assignedSet.has(t.id))
    const seenClubKeys = new Set<string>()
    return parsed.categories.flatMap((category) => {
      const existingDivision = findMatchingDivision(category.divisionName, divisions)
      return category.teams.map((team) => {
        const listName = importedTeamName(team.name, category.shortYear)
        const reused = takeReusableTeam(pool, team, listName)
        const clubKey = normalizeKey(team.name)
        const existingClub = clubs.find((c) => normalizeKey(c.name) === clubKey)
        const clubAction = existingClub
          ? `Club ${existingClub.name}`
          : seenClubKeys.has(clubKey)
            ? 'Mismo club'
            : 'Crear club'
        seenClubKeys.add(clubKey)
        return {
          category: category.divisionName,
          raw: team.raw,
          name: team.name,
          listName,
          publicName: publicTeamName(team.name, team.suffix),
          suffix: team.suffix,
          divisionAction: existingDivision ? `Usar ${existingDivision.name}` : 'Crear división',
          teamAction: reused ? `Reutilizar ${reused.displayName ?? reused.name}` : 'Crear equipo',
          clubAction,
        }
      })
    })
  }, [parsed.categories, divisions, clubs, teams, assignedSet])

  const reset = () => {
    setText('')
    setFileName(null)
    setLocalError(null)
    setProgress(null)
    if (fileInputRef.current) fileInputRef.current.value = ''
  }

  const handleClose = () => {
    if (importMutation.isPending) return
    reset()
    onClose()
  }

  const importMutation = useMutation({
    mutationFn: async () => {
      const duplicateIssues = [
        ...findDuplicateCategoryNames(parsed.categories),
        ...findIntraCategoryDuplicates(parsed.categories),
      ]
      if (duplicateIssues.length > 0) {
        throw new Error(duplicateIssues.join('\n'))
      }

      const errors: string[] = []
      let created = 0
      let reused = 0
      let divisionsCreated = 0

      const divisionCache = new Map<string, Division>()
      for (const division of divisions) {
        divisionCache.set(normalizeKey(division.name), division)
      }

      const clubCache = new Map<string, Club>()
      for (const club of clubs) {
        clubCache.set(normalizeKey(club.name), club)
      }

      const ensureClub = async (name: string) => {
        const key = normalizeKey(name)
        const existing = clubCache.get(key)
        if (existing) return existing.id
        try {
          const createdClub = await teamsService.createClub(leagueId, { name })
          const club: Club = { id: createdClub.id, name, logoUrl: '' }
          clubCache.set(key, club)
          return club.id
        } catch (e) {
          const refreshed = await teamsService.getClubsByLeague(leagueId)
          for (const club of refreshed) clubCache.set(normalizeKey(club.name), club)
          const after = clubCache.get(key)
          if (after) return after.id
          throw e
        }
      }

      const ensureDivision = async (divisionName: string) => {
        const key = normalizeKey(divisionName)
        const existing = divisionCache.get(key)
        if (existing) return existing
        const createdDivision = await divisionsService.create(leagueId, {
          name: divisionName,
          description: '',
          kickoffRestrictionEnabled: false,
          kickoffRestrictionStart: null,
          kickoffRestrictionEnd: null,
        })
        const division: Division = {
          id: createdDivision.id,
          leagueId,
          name: divisionName,
          description: null,
        }
        divisionCache.set(key, division)
        divisionsCreated += 1
        return division
      }

      const reusablePool = teams.filter((t) => !assignedSet.has(t.id))
      const steps = parsed.categories.reduce((sum, c) => sum + c.teams.length, 0)
      let done = 0

      for (const category of parsed.categories) {
        const division = await ensureDivision(category.divisionName)
        if (lockedDivisionIds.has(division.id)) {
          errors.push(`${category.divisionName}: la división ya tiene fixtures y está bloqueada.`)
          done += category.teams.length
          setProgress(`Importando… ${Math.min(done, steps)}/${steps}`)
          continue
        }

        await teamsService.assignDivisionToSeason(leagueId, seasonId, division.id)

        for (const team of category.teams) {
          done += 1
          setProgress(`Importando… ${done}/${steps}`)
          try {
            const clubId = await ensureClub(team.name)
            const listName = importedTeamName(team.name, category.shortYear)
            const existingTeam = takeReusableTeam(reusablePool, team, listName)
            if (existingTeam) {
              const needsClub = existingTeam.clubId !== clubId
              const needsName =
                existingTeam.name !== listName || (existingTeam.suffix ?? '') !== (team.suffix ?? '')
              if (needsClub || needsName) {
                await teamsService.update(
                  leagueId,
                  existingTeam.id,
                  toUpdatePayload(existingTeam, listName, team.suffix, clubId)
                )
              }
              await teamsService.assignTeamToDivisionSeason(leagueId, seasonId, division.id, existingTeam.id)
              reused += 1
            } else {
              const createdTeam = await teamsService.create(leagueId, {
                name: listName,
                suffix: team.suffix ?? undefined,
                clubId,
                seasonId,
                divisionId: division.id,
              })
              await teamsService.assignTeamToDivisionSeason(leagueId, seasonId, division.id, createdTeam.id)
              created += 1
            }
          } catch (e) {
            errors.push(`${category.divisionName} / ${team.raw}: ${e instanceof Error ? e.message : 'Error'}`)
          }
        }
      }

      if (errors.length > 0) {
        throw new Error(
          `Importados ${created + reused} (creados ${created}, reutilizados ${reused}). Errores:\n${errors.join('\n')}`
        )
      }
      return { created, reused, divisionsCreated }
    },
    onSuccess: (summary) => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId] })
      onImported?.(summary)
      reset()
      onClose()
    },
    onError: (err) => {
      setLocalError(err instanceof Error ? err.message : 'No se pudo importar')
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId] })
    },
  })

  const handleFile = async (file: File) => {
    setFileName(file.name)
    setLocalError(null)
    setText(await file.text())
  }

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="md" fullWidth>
      <DialogTitle>Pegar categorías y equipos</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Pegá todas las categorías y zonas con sus equipos. El recuento entre paréntesis es opcional.
          Se crean las divisiones y los clubes que falten. En la lista de equipos el nombre lleva el año
          (Academia 14/15); en tablas y fixtures se muestra el club (Academia), salvo variantes como Negro/Rojo o A/B.
        </Typography>
        <TextField
          value={text}
          onChange={(e) => setText(e.target.value)}
          placeholder={'Categoría 2018/19 — Zona A (6 equipos)\nAcademia\nBorussia\n...'}
          multiline
          minRows={8}
          fullWidth
          disabled={importMutation.isPending}
        />
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mt: 1.5 }}>
          <input
            ref={fileInputRef}
            type="file"
            accept=".txt,.csv,text/plain"
            hidden
            onChange={(e) => {
              const file = e.target.files?.[0]
              if (file) void handleFile(file)
            }}
          />
          <Button
            variant="outlined"
            size="small"
            startIcon={<UploadFileIcon />}
            onClick={() => fileInputRef.current?.click()}
            disabled={importMutation.isPending}
          >
            Subir archivo
          </Button>
          {fileName && (
            <Typography variant="caption" color="text.secondary">
              {fileName}
            </Typography>
          )}
        </Box>

        {blockingIssues.map((issue) => (
          <Alert key={issue} severity="error" sx={{ mt: 1.5 }}>
            {issue}
          </Alert>
        ))}

        {parsed.warnings.map((warning) => (
          <Alert key={warning} severity="warning" sx={{ mt: 1.5 }}>
            {warning}
          </Alert>
        ))}

        {parsed.categories.length > 0 && (
          <Alert severity={blockingIssues.length > 0 ? 'warning' : 'info'} sx={{ mt: 2 }}>
            Se van a importar <strong>{parsed.categories.length}</strong> división(es) y{' '}
            <strong>{totalTeams}</strong> equipo(s), agrupados en <strong>{uniqueClubNames}</strong> club(es).
            El mismo club en otra categoría no es un duplicado: se crea otro equipo y se asocia al mismo club.
          </Alert>
        )}

        {previewRows.length > 0 && (
          <Box sx={{ mt: 1, maxHeight: 320, overflow: 'auto', border: 1, borderColor: 'divider', borderRadius: 1 }}>
            <Table size="small" stickyHeader>
              <TableHead>
                <TableRow>
                  <TableCell>División</TableCell>
                  <TableCell>Lista</TableCell>
                  <TableCell>Público</TableCell>
                  <TableCell>Club</TableCell>
                  <TableCell>Acción</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {previewRows.map((row, index) => (
                  <TableRow key={`${row.category}-${row.raw}-${index}`}>
                    <TableCell>{row.category}</TableCell>
                    <TableCell>{row.listName}{row.suffix ? ` ${row.suffix}` : ''}</TableCell>
                    <TableCell>{row.publicName}</TableCell>
                    <TableCell>{row.clubAction}</TableCell>
                    <TableCell>{row.teamAction}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Box>
        )}

        {importMutation.isPending && (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mt: 2 }}>
            <CircularProgress size={20} />
            <Typography variant="body2">{progress ?? 'Importando…'}</Typography>
          </Box>
        )}

        {localError && (
          <Alert severity="error" sx={{ mt: 2, whiteSpace: 'pre-wrap' }} onClose={() => setLocalError(null)}>
            {localError}
          </Alert>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose} disabled={importMutation.isPending}>
          Cancelar
        </Button>
        <Button
          variant="contained"
          startIcon={<ContentPasteGoIcon />}
          onClick={() => {
            setLocalError(null)
            importMutation.mutate()
          }}
          disabled={importMutation.isPending || totalTeams === 0 || !seasonId || blockingIssues.length > 0}
        >
          {importMutation.isPending ? <CircularProgress size={22} color="inherit" /> : 'Importar'}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
