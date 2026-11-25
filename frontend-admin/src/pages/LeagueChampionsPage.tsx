import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Alert,
  Autocomplete,
  Avatar,
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Snackbar,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline'
import EditIcon from '@mui/icons-material/Edit'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import {
  championsService,
  type ChampionListFilters,
  type ChampionTeamOption,
  type LeagueChampion,
} from '../api/champions'
import { ChampionFormDialog } from '../components/ChampionFormDialog'
import { useActiveLeague, useLeagueId } from '../contexts/LeagueContext'

export function LeagueChampionsPage() {
  const { t } = useTranslation()
  const leagueId = useLeagueId()
  const activeLeague = useActiveLeague()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [filters, setFilters] = useState<ChampionListFilters>({})
  const [draft, setDraft] = useState({ year: '', championship: '', division: '', teamId: '' })
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [deleting, setDeleting] = useState<LeagueChampion | null>(null)
  const [snackbar, setSnackbar] = useState<{ message: string; severity: 'success' | 'error' } | null>(null)

  const optionsQuery = useQuery({
    queryKey: ['leagues', leagueId, 'champion-options'],
    queryFn: ({ signal }) => championsService.options(leagueId!, signal),
    enabled: !!leagueId,
  })

  const listQuery = useQuery({
    queryKey: ['leagues', leagueId, 'champions', 'list', filters],
    queryFn: ({ signal }) => championsService.list(leagueId!, filters, signal),
    enabled: !!leagueId,
  })

  const deleteMutation = useMutation({
    mutationFn: (champion: LeagueChampion) => championsService.remove(leagueId!, champion.id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'champions'] })
      setDeleting(null)
      setSnackbar({ message: t('champions.deleted'), severity: 'success' })
    },
    onError: (err) => {
      setSnackbar({
        message: err instanceof Error ? err.message : t('champions.deleteFailed'),
        severity: 'error',
      })
    },
  })

  if (!leagueId) {
    return (
      <Alert severity="error" action={<Button onClick={() => navigate('/')}>{t('champions.goToLeagues')}</Button>}>
        {t('champions.noLeague')}
      </Alert>
    )
  }

  const champions = listQuery.data ?? []
  const teams = optionsQuery.data?.teams ?? []
  const selectedTeam = teams.find((team) => team.id === draft.teamId) ?? null

  const applyFilters = () => {
    const year = draft.year.trim() ? Number(draft.year) : null
    setFilters({
      year: year && Number.isInteger(year) ? year : null,
      championship: draft.championship,
      division: draft.division,
      teamId: draft.teamId || undefined,
    })
  }

  const clearFilters = () => {
    setDraft({ year: '', championship: '', division: '', teamId: '' })
    setFilters({})
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2, gap: 2, flexWrap: 'wrap' }}>
        <Typography variant="h5" component="h1" sx={{ fontWeight: 600 }}>
          {t('champions.title', { league: activeLeague?.name ?? t('nav.leagues') })}
        </Typography>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={() => {
            setEditingId(null)
            setDialogOpen(true)
          }}
        >
          {t('champions.create')}
        </Button>
      </Box>
      <Typography color="text.secondary" sx={{ mb: 2 }}>
        {t('champions.intro')}
      </Typography>

      <Box sx={{ display: 'grid', gap: 1.5, gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', md: 'repeat(4, 1fr)' }, mb: 1.5 }}>
        <TextField
          label={t('champions.fields.year')}
          type="number"
          value={draft.year}
          onChange={(event) => setDraft((current) => ({ ...current, year: event.target.value }))}
        />
        <TextField
          label={t('champions.fields.championship')}
          value={draft.championship}
          onChange={(event) => setDraft((current) => ({ ...current, championship: event.target.value }))}
        />
        <TextField
          label={t('champions.fields.division')}
          value={draft.division}
          onChange={(event) => setDraft((current) => ({ ...current, division: event.target.value }))}
        />
        <Autocomplete
          options={teams}
          value={selectedTeam}
          onChange={(_, team: ChampionTeamOption | null) => setDraft((current) => ({ ...current, teamId: team?.id ?? '' }))}
          getOptionLabel={(team) => team.name}
          isOptionEqualToValue={(option, value) => option.id === value.id}
          renderInput={(params) => <TextField {...params} label={t('champions.fields.team')} />}
        />
      </Box>
      <Box sx={{ display: 'flex', gap: 1, mb: 2 }}>
        <Button variant="outlined" onClick={applyFilters}>{t('champions.filter')}</Button>
        <Button onClick={clearFilters}>{t('champions.clearFilters')}</Button>
      </Box>

      {listQuery.isLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress />
        </Box>
      ) : listQuery.isError ? (
        <Alert severity="error">{listQuery.error instanceof Error ? listQuery.error.message : t('champions.loadError')}</Alert>
      ) : champions.length === 0 ? (
        <Typography color="text.secondary">{t('champions.empty')}</Typography>
      ) : (
        <Box sx={{ overflowX: 'auto' }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>{t('champions.columns.shield')}</TableCell>
                <TableCell>{t('champions.columns.team')}</TableCell>
                <TableCell>{t('champions.columns.year')}</TableCell>
                <TableCell>{t('champions.columns.championship')}</TableCell>
                <TableCell>{t('champions.columns.division')}</TableCell>
                <TableCell align="right">{t('champions.columns.photos')}</TableCell>
                <TableCell align="right">{t('champions.columns.actions')}</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {champions.map((champion) => (
                <TableRow key={champion.id} hover>
                  <TableCell>
                    <Avatar src={champion.teamLogoUrl ?? undefined} alt={champion.teamName} variant="rounded">
                      {champion.teamName.slice(0, 1)}
                    </Avatar>
                  </TableCell>
                  <TableCell>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      {champion.teamName}
                      {champion.teamIsArchived ? <Chip size="small" color="warning" label={t('champions.archived')} /> : null}
                    </Box>
                  </TableCell>
                  <TableCell>{champion.year}</TableCell>
                  <TableCell>{champion.championshipName}</TableCell>
                  <TableCell>{champion.divisionName}</TableCell>
                  <TableCell align="right">{champion.photoCount}</TableCell>
                  <TableCell align="right">
                    <Button
                      size="small"
                      startIcon={<EditIcon />}
                      onClick={() => {
                        setEditingId(champion.id)
                        setDialogOpen(true)
                      }}
                    >
                      {t('champions.edit')}
                    </Button>
                    <Button size="small" color="error" startIcon={<DeleteOutlineIcon />} onClick={() => setDeleting(champion)}>
                      {t('champions.delete')}
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Box>
      )}

      <ChampionFormDialog
        open={dialogOpen}
        leagueId={leagueId}
        championId={editingId}
        onClose={() => {
          setDialogOpen(false)
          setEditingId(null)
        }}
        onSaved={(message) => setSnackbar({ message, severity: 'success' })}
      />

      <Dialog open={!!deleting} onClose={() => !deleteMutation.isPending && setDeleting(null)} fullWidth maxWidth="sm">
        <DialogTitle>{t('champions.deleteConfirmTitle')}</DialogTitle>
        <DialogContent>
          <DialogContentText>
            {t('champions.deleteConfirm', {
              team: deleting?.teamName ?? '',
              year: deleting?.year ?? '',
              championship: deleting?.championshipName ?? '',
              division: deleting?.divisionName ?? '',
            })}
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleting(null)} disabled={deleteMutation.isPending}>{t('common.cancel')}</Button>
          <Button color="error" variant="contained" disabled={deleteMutation.isPending} onClick={() => deleting && deleteMutation.mutate(deleting)}>
            {deleteMutation.isPending ? <CircularProgress size={22} color="inherit" /> : t('champions.delete')}
          </Button>
        </DialogActions>
      </Dialog>

      <Snackbar
        open={!!snackbar}
        autoHideDuration={5000}
        onClose={() => setSnackbar(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        {snackbar ? (
          <Alert onClose={() => setSnackbar(null)} severity={snackbar.severity} variant="filled" sx={{ width: '100%' }}>
            {snackbar.message}
          </Alert>
        ) : undefined}
      </Snackbar>
    </Box>
  )
}
