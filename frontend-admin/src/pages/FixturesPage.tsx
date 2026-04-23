import { Fragment, useMemo, useState } from 'react'
import type { SelectChangeEvent } from '@mui/material'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Snackbar,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import AutoFixHighIcon from '@mui/icons-material/AutoFixHigh'
import RefreshIcon from '@mui/icons-material/Refresh'
import SaveIcon from '@mui/icons-material/Save'
import UploadFileIcon from '@mui/icons-material/UploadFile'
import DownloadIcon from '@mui/icons-material/Download'
import PrintIcon from '@mui/icons-material/Print'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Link as RouterLink } from 'react-router-dom'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { fixturesService } from '../api/fixtures'
import { seasonsService } from '../api/seasons'
import { divisionsService } from '../api/divisions'
import { competitionRulesService } from '../api/competitionRules'
import { leaguesService } from '../api/leagues'
import { useActiveLeague, useLeagueId } from '../contexts/LeagueContext'
import { ImportFixtureModal } from '../components/ImportFixtureModal'
import { useTranslation } from 'react-i18next'

function formatTime(timeStr: string): string {
  try {
    const parts = timeStr.split(':')
    return `${parts[0]}:${parts[1] ?? '00'}`
  } catch {
    return timeStr
  }
}

function formatDate(dateStr: string, locale: string): string {
  try {
    const m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(dateStr)
    if (!m) return dateStr
    const year = parseInt(m[1], 10)
    const month = parseInt(m[2], 10) - 1
    const day = parseInt(m[3], 10)
    // Parse as local date to avoid timezone shifts (YYYY-MM-DD is UTC in JS Date parser).
    return new Date(year, month, day).toLocaleDateString(locale, {
      weekday: 'short',
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    })
  } catch {
    return dateStr
  }
}

function escapeCsvCell(value: string): string {
  if (value.includes('"') || value.includes(',') || value.includes('\n')) {
    return `"${value.replace(/"/g, '""')}"`
  }
  return value
}

function escapeHtml(value: string): string {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;')
}

export function FixturesPage() {
  const { t, i18n } = useTranslation()
  const leagueId = useLeagueId()
  const activeLeague = useActiveLeague()
  const queryClient = useQueryClient()
  const [seasonId, setSeasonId] = useState<string>('')
  const [divisionId, setDivisionId] = useState<string>('')
  const [teamFilter, setTeamFilter] = useState<string>('')
  const [importModalOpen, setImportModalOpen] = useState(false)
  const [snackbar, setSnackbar] = useState<{ message: string; severity: 'success' | 'error' } | null>(null)

  const { data: seasons = [], isLoading: seasonsLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'seasons'],
    queryFn: ({ signal }) => seasonsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })

  const { data: divisions = [] } = useQuery({
    queryKey: ['leagues', leagueId, 'divisions'],
    queryFn: ({ signal }) => divisionsService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })

  const { data: fixturesData, isLoading: fixturesLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'seasons', seasonId, 'fixtures'],
    queryFn: ({ signal }) => fixturesService.get(leagueId!, seasonId, signal),
    enabled: !!leagueId && !!seasonId,
    retry: false,
  })

  const { data: globalCompetitionRule } = useQuery({
    queryKey: ['leagues', leagueId, 'competition-rules', 'global'],
    queryFn: async ({ signal }) => {
      try {
        return await competitionRulesService.get(leagueId!, undefined, signal)
      } catch (e) {
        if (e instanceof Error && (e.message.includes('Not Found') || e.message.includes('404')))
          return null
        throw e
      }
    },
    enabled: !!leagueId,
    retry: false,
  })

  const { data: leagueById } = useQuery({
    queryKey: ['leagues', leagueId, 'details'],
    queryFn: ({ signal }) => leaguesService.getById(leagueId!, signal),
    enabled: !!leagueId && (!activeLeague || activeLeague.id !== leagueId),
  })

  const generateMutation = useMutation({
    mutationFn: () => fixturesService.generate(leagueId!, seasonId, divisionId || undefined),
    onSuccess: () => {
      setSnackbar({ message: t('fixtures.draftGenerated'), severity: 'success' })
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'seasons', seasonId, 'fixtures'] })
    },
    onError: (err) => {
      setSnackbar({ message: err instanceof Error ? err.message : t('fixtures.generateFailed'), severity: 'error' })
    },
  })

  const commitMutation = useMutation({
    mutationFn: () => fixturesService.commit(leagueId!, seasonId),
    onSuccess: () => {
      setSnackbar({ message: t('fixtures.saved'), severity: 'success' })
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'seasons', seasonId, 'fixtures'] })
    },
    onError: (err) => {
      setSnackbar({ message: err instanceof Error ? err.message : t('fixtures.saveFailed'), severity: 'error' })
    },
  })

  const handleSeasonChange = (e: SelectChangeEvent<string>) => {
    setSeasonId(e.target.value)
    setDivisionId('')
    setTeamFilter('')
  }

  const handleGenerate = () => generateMutation.mutate()
  const handleRegenerate = () => generateMutation.mutate()
  const handleSave = () => commitMutation.mutate()

  const fixtures = fixturesData?.fixtures
  const isDraft = fixturesData?.isDraft ?? false
  const hasFixtures = fixtures && fixtures.rounds.length > 0
  const selectedDivisionName = divisionId ? (divisions.find((d) => d.id === divisionId)?.name ?? null) : null
  const dayNames = [
    t('fixtures.days.sunday'),
    t('fixtures.days.monday'),
    t('fixtures.days.tuesday'),
    t('fixtures.days.wednesday'),
    t('fixtures.days.thursday'),
    t('fixtures.days.friday'),
    t('fixtures.days.saturday'),
  ]
  const configuredMatchDays = (globalCompetitionRule?.matchDays ?? [])
    .filter((d) => d >= 0 && d <= 6)
    .sort((a, b) => a - b)
  const configuredMatchDaysText = configuredMatchDays
    .map((d) => `${dayNames[d]} (${d})`)
    .join(', ')
  const dateLocale = i18n.language?.toLowerCase().startsWith('es') ? 'es-AR' : 'en-US'
  const leagueName = activeLeague?.id === leagueId ? activeLeague.name : (leagueById?.name ?? leagueId ?? '')

  const divisionFilteredRounds = useMemo(() => {
    if (!fixtures) return []
    return fixtures.rounds
      .map((round) => {
        const matches = round.matches.filter((m) => !selectedDivisionName || m.divisionName === selectedDivisionName)
        const byeTeams = (round.byeTeams ?? []).filter(
          (b) => !selectedDivisionName || b.divisionName === selectedDivisionName,
        )
        return { ...round, matches, byeTeams }
      })
      .filter((round) => round.matches.length > 0 || round.byeTeams.length > 0)
  }, [fixtures, selectedDivisionName])

  const teamOptions = useMemo(() => {
    const names = new Set<string>()
    for (const round of divisionFilteredRounds) {
      for (const match of round.matches) {
        names.add(match.homeTeamName)
        names.add(match.awayTeamName)
      }
    }
    return Array.from(names).sort((a, b) => a.localeCompare(b))
  }, [divisionFilteredRounds])

  const visibleRounds = useMemo(() => {
    return divisionFilteredRounds
      .map((round) => {
        const matches = round.matches
          .filter((m) => !teamFilter || m.homeTeamName === teamFilter || m.awayTeamName === teamFilter)
          .slice()
          .sort((a, b) => {
            const timeCmp = (a.kickoffTime ?? '').localeCompare(b.kickoffTime ?? '')
            if (timeCmp !== 0) return timeCmp
            return a.fieldName.localeCompare(b.fieldName)
          })

        const byeTeams = (round.byeTeams ?? [])
          .filter((b) => !teamFilter || b.teamName === teamFilter)
          .slice()
          .sort((a, b) => a.divisionName.localeCompare(b.divisionName) || a.teamName.localeCompare(b.teamName))

        return { ...round, matches, byeTeams }
      })
      .filter((round) => round.matches.length > 0 || round.byeTeams.length > 0)
  }, [divisionFilteredRounds, teamFilter])
  const hasVisibleFixtures = visibleRounds.length > 0

  const handleDownload = () => {
    if (!hasFixtures) return

    try {
      const headers = [
        t('fixtures.cols.round'),
        t('fixtures.cols.date'),
        t('fixtures.cols.division'),
        t('fixtures.cols.field'),
        t('fixtures.cols.time'),
        t('fixtures.cols.home'),
        t('fixtures.cols.away'),
      ]

      const rows: string[] = [headers.map(escapeCsvCell).join(',')]

      for (const round of visibleRounds) {
        for (const match of round.matches) {
          rows.push(
            [
              String(round.roundNumber),
              round.matchDate ? formatDate(round.matchDate, dateLocale) : '',
              match.divisionName,
              match.fieldName,
              formatTime(match.kickoffTime),
              match.homeTeamName,
              match.awayTeamName,
            ]
              .map(escapeCsvCell)
              .join(','),
          )
        }

        for (const bye of round.byeTeams) {
          rows.push(
            [
              String(round.roundNumber),
              round.matchDate ? formatDate(round.matchDate, dateLocale) : '',
              bye.divisionName,
              '-',
              '-',
              bye.teamName,
              t('fixtures.bye'),
            ]
              .map(escapeCsvCell)
              .join(','),
          )
        }
      }

      const csv = '\uFEFF' + rows.join('\n')
      const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' })
      const url = window.URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = `fixtures-${seasonId}.csv`
      document.body.appendChild(link)
      link.click()
      document.body.removeChild(link)
      window.URL.revokeObjectURL(url)
    } catch {
      setSnackbar({ message: t('fixtures.downloadFailed'), severity: 'error' })
    }
  }

  const handlePrint = () => {
    if (!hasFixtures) return

    const title = `${t('fixtures.title')} - ${leagueName}`
    const tableRows = visibleRounds
      .flatMap((round) => [
        ...round.matches.map((match) => [
          round.roundNumber,
          round.matchDate ? formatDate(round.matchDate, dateLocale) : '',
          match.divisionName,
          match.fieldName,
          formatTime(match.kickoffTime),
          match.homeTeamName,
          match.awayTeamName,
        ]),
        ...round.byeTeams.map((bye) => [
          round.roundNumber,
          round.matchDate ? formatDate(round.matchDate, dateLocale) : '',
          bye.divisionName,
          '-',
          '-',
          bye.teamName,
          t('fixtures.bye'),
        ]),
      ])
      .map(
        (cells) =>
          `<tr>${cells.map((cell) => `<td>${escapeHtml(String(cell))}</td>`).join('')}</tr>`,
      )
      .join('')

    const html = `
      <!doctype html>
      <html>
        <head>
          <meta charset="utf-8" />
          <title>${escapeHtml(title)}</title>
          <style>
            body { font-family: Arial, sans-serif; padding: 24px; }
            h1 { font-size: 20px; margin-bottom: 16px; }
            table { width: 100%; border-collapse: collapse; font-size: 12px; }
            th, td { border: 1px solid #ccc; padding: 6px 8px; text-align: left; }
            th { background: #f5f5f5; }
          </style>
        </head>
        <body>
          <h1>${escapeHtml(title)}</h1>
          <table>
            <thead>
              <tr>
                <th>${escapeHtml(t('fixtures.cols.round'))}</th>
                <th>${escapeHtml(t('fixtures.cols.date'))}</th>
                <th>${escapeHtml(t('fixtures.cols.division'))}</th>
                <th>${escapeHtml(t('fixtures.cols.field'))}</th>
                <th>${escapeHtml(t('fixtures.cols.time'))}</th>
                <th>${escapeHtml(t('fixtures.cols.home'))}</th>
                <th>${escapeHtml(t('fixtures.cols.away'))}</th>
              </tr>
            </thead>
            <tbody>${tableRows}</tbody>
          </table>
        </body>
      </html>
    `

    // Use an off-screen iframe so printing works even when pop-ups are blocked.
    try {
      const iframe = document.createElement('iframe')
      iframe.style.position = 'fixed'
      iframe.style.right = '-10000px'
      iframe.style.bottom = '0'
      iframe.style.width = '1024px'
      iframe.style.height = '768px'
      iframe.style.border = '0'
      iframe.style.visibility = 'hidden'
      iframe.setAttribute('aria-hidden', 'true')

      const cleanup = () => {
        if (iframe.parentNode) {
          iframe.parentNode.removeChild(iframe)
        }
      }

      let printed = false
      iframe.onload = () => {
        if (printed) return

        const frameWindow = iframe.contentWindow
        const frameDocument = iframe.contentDocument
        if (!frameWindow) {
          cleanup()
          setSnackbar({ message: t('fixtures.printBlocked'), severity: 'error' })
          return
        }
        if (!frameDocument?.body || !frameDocument.body.innerText.trim()) {
          return
        }

        printed = true
        frameWindow.onafterprint = cleanup
        frameWindow.focus()
        frameWindow.print()
        window.setTimeout(cleanup, 1500)
      }

      iframe.srcdoc = html
      document.body.appendChild(iframe)
    } catch {
      setSnackbar({ message: t('fixtures.printBlocked'), severity: 'error' })
    }
  }

  if (!leagueId) {
    return (
      <Alert severity="error" action={<Button component={RouterLink} to="/">{t('fixtures.goToLeagues')}</Button>}>
        {t('fixtures.noLeague')}
      </Alert>
    )
  }

  return (
    <Box>
      <Button component={RouterLink} to="/seasons" startIcon={<ArrowBackIcon />} size="small" sx={{ mb: 2 }}>
        {t('fixtures.backToSeasons')}
      </Button>
      <Typography variant="h5" component="h1" sx={{ mb: 2, fontWeight: 600 }}>
        {t('fixtures.title')}
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        {t('fixtures.description')}
      </Typography>

      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, alignItems: 'center', mb: 3 }}>
        <FormControl size="small" sx={{ minWidth: 220 }} disabled={seasonsLoading}>
          <InputLabel id="season-label">{t('fixtures.season')}</InputLabel>
          <Select
            labelId="season-label"
            label={t('fixtures.season')}
            value={seasonId}
            onChange={handleSeasonChange}
          >
            <MenuItem value="">
              <em>{t('fixtures.selectSeason')}</em>
            </MenuItem>
            {seasons.map((s) => (
              <MenuItem key={s.id} value={s.id}>
                {s.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        {!!seasonId && (
          <>
            <FormControl size="small" sx={{ minWidth: 180 }} disabled={!seasonId}>
              <InputLabel id="division-label">{t('fixtures.division')}</InputLabel>
              <Select
                labelId="division-label"
                label={t('fixtures.division')}
                value={divisionId}
                onChange={(e) => {
                  setDivisionId(e.target.value)
                  setTeamFilter('')
                }}
              >
                <MenuItem value="">
                  <em>{t('fixtures.selectDivision')}</em>
                </MenuItem>
                {divisions.map((d) => (
                  <MenuItem key={d.id} value={d.id}>
                    {d.name}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <FormControl size="small" sx={{ minWidth: 220 }} disabled={!hasVisibleFixtures}>
              <InputLabel id="team-filter-label">{t('fixtures.teamFilter')}</InputLabel>
              <Select
                labelId="team-filter-label"
                label={t('fixtures.teamFilter')}
                value={teamFilter}
                onChange={(e) => setTeamFilter(e.target.value)}
              >
                <MenuItem value="">
                  <em>{t('fixtures.allTeams')}</em>
                </MenuItem>
                {teamOptions.map((team) => (
                  <MenuItem key={team} value={team}>
                    {team}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <Button
              variant="outlined"
              startIcon={hasFixtures ? <RefreshIcon /> : <AutoFixHighIcon />}
              onClick={hasFixtures ? handleRegenerate : handleGenerate}
              disabled={generateMutation.isPending || !divisionId}
            >
              {hasFixtures ? t('fixtures.regenerate') : t('fixtures.generate')}
            </Button>
            <Button
              variant="outlined"
              startIcon={<UploadFileIcon />}
              onClick={() => setImportModalOpen(true)}
              disabled={!divisionId}
            >
              {t('fixtures.import')}
            </Button>
            {hasFixtures && (
              <Button variant="outlined" startIcon={<DownloadIcon />} onClick={handleDownload}>
                {t('fixtures.download')}
              </Button>
            )}
            {hasFixtures && (
              <Button variant="outlined" startIcon={<PrintIcon />} onClick={handlePrint}>
                {t('fixtures.print')}
              </Button>
            )}
            {hasFixtures && (
              <Button
                variant="contained"
                startIcon={commitMutation.isPending ? <CircularProgress size={18} color="inherit" /> : <SaveIcon />}
                onClick={handleSave}
                disabled={commitMutation.isPending || !isDraft}
              >
                {t('fixtures.save')}
              </Button>
            )}
            {hasFixtures && isDraft && (
              <Chip label={t('fixtures.draftStatus')} color="warning" size="small" />
            )}
            {hasFixtures && !isDraft && (
              <Chip label={t('fixtures.committedStatus')} color="success" size="small" />
            )}
            {teamFilter && (
              <Chip
                label={`${t('fixtures.filtered')} ${teamFilter}`}
                size="small"
                onDelete={() => setTeamFilter('')}
              />
            )}
          </>
        )}
      </Box>

      {snackbar && (
        <Snackbar
          open={!!snackbar}
          autoHideDuration={5000}
          onClose={() => setSnackbar(null)}
          message={snackbar.message}
          ContentProps={{
            sx: {
              bgcolor: snackbar.severity === 'error' ? 'error.main' : 'success.main',
              color: 'white',
            },
          }}
        />
      )}

      {fixturesLoading && seasonId && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress />
        </Box>
      )}

      {!fixturesLoading && seasonId && !hasFixtures && (
        <Typography color="text.secondary">
          {t('fixtures.emptyPrompt')}
        </Typography>
      )}
      {!fixturesLoading && seasonId && hasFixtures && !hasVisibleFixtures && (
        <Typography color="text.secondary">{t('fixtures.emptyForFilters')}</Typography>
      )}

      {!!seasonId && (
        configuredMatchDays.length > 0 ? (
          <Alert severity="info" sx={{ mb: 2 }}>
            {t('fixtures.matchDaySourceConfigured', { days: configuredMatchDaysText })}
          </Alert>
        ) : (
          <Alert severity="warning" sx={{ mb: 2 }}>
            {t('fixtures.matchDaySourceMissing')}
          </Alert>
        )
      )}

      {leagueId && seasonId && divisionId && (
        <ImportFixtureModal
          open={importModalOpen}
          onClose={() => setImportModalOpen(false)}
          leagueId={leagueId}
          seasonId={seasonId}
          divisionId={divisionId}
          onSuccess={() => {
            void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'seasons', seasonId, 'fixtures'] })
          }}
        />
      )}

      {!fixturesLoading && hasVisibleFixtures && (
        <TableContainer component={Paper} variant="outlined" sx={{ mb: 3 }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell><strong>{t('fixtures.cols.round')}</strong></TableCell>
                <TableCell><strong>{t('fixtures.cols.date')}</strong></TableCell>
                <TableCell><strong>{t('fixtures.cols.division')}</strong></TableCell>
                <TableCell><strong>{t('fixtures.cols.field')}</strong></TableCell>
                <TableCell><strong>{t('fixtures.cols.time')}</strong></TableCell>
                <TableCell><strong>{t('fixtures.cols.home')}</strong></TableCell>
                <TableCell><strong>{t('fixtures.cols.away')}</strong></TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {visibleRounds.map((round) => (
                <Fragment key={`round-${round.roundNumber}-${round.matchDate ?? 'no-date'}`}>
                  {round.matches.map((m, idx) => (
                    <TableRow key={`${round.roundNumber}-${idx}-${m.homeTeamDivisionSeasonId}-${m.awayTeamDivisionSeasonId}`}>
                      <TableCell>{idx === 0 ? round.roundNumber : ''}</TableCell>
                      <TableCell>{idx === 0 ? formatDate(round.matchDate, dateLocale) : ''}</TableCell>
                      <TableCell>{m.divisionName}</TableCell>
                      <TableCell>{m.fieldName}</TableCell>
                      <TableCell>{formatTime(m.kickoffTime)}</TableCell>
                      <TableCell>{m.homeTeamName}</TableCell>
                      <TableCell>{m.awayTeamName}</TableCell>
                    </TableRow>
                  ))}
                  {round.byeTeams.map((bye, byeIdx) => {
                    const showRoundHeader = round.matches.length === 0 && byeIdx === 0
                    return (
                      <TableRow key={`${round.roundNumber}-bye-${bye.divisionSeasonId}-${bye.teamDivisionSeasonId}`}>
                        <TableCell>{showRoundHeader ? round.roundNumber : ''}</TableCell>
                        <TableCell>{showRoundHeader ? formatDate(round.matchDate, dateLocale) : ''}</TableCell>
                        <TableCell>{bye.divisionName}</TableCell>
                        <TableCell>-</TableCell>
                        <TableCell>-</TableCell>
                        <TableCell>{bye.teamName}</TableCell>
                        <TableCell>{t('fixtures.bye')}</TableCell>
                      </TableRow>
                    )
                  })}
                </Fragment>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </Box>
  )
}
