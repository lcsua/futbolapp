import { useState } from 'react'
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  Typography,
  Box,
  CircularProgress,
  Alert,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
} from '@mui/material'
import { fixturesService, type PreviewFixtureRow } from '../api/fixtures'
import { useTranslation } from 'react-i18next'
const CSV_PLACEHOLDER = `round,home_team,away_team
1,TIGRES,LEONES
1,PUMAS,HALCONES`

interface ImportFixtureModalProps {
  open: boolean
  onClose: () => void
  leagueId: string
  seasonId: string
  divisionId: string
  onSuccess: () => void
}

export function ImportFixtureModal({
  open,
  onClose,
  leagueId,
  seasonId,
  divisionId,
  onSuccess,
}: ImportFixtureModalProps) {
  const { t } = useTranslation()
  const [csvText, setCsvText] = useState('')
  const [preview, setPreview] = useState<{
    importType: string
    rows: PreviewFixtureRow[]
    errors: string[]
  } | null>(null)
  const [loading, setLoading] = useState(false)
  const [importing, setImporting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handlePreview = async () => {
    setError(null)
    setPreview(null)
    setLoading(true)
    try {
      const res = await fixturesService.previewImport(leagueId, {
        seasonId,
        divisionId,
        csvText: csvText.trim(),
      })
      setPreview({ importType: res.importType, rows: res.rows, errors: res.errors })
    } catch (e) {
      setError(e instanceof Error ? e.message : t('fixtures.importModal.errorLoadingPreview'))
    } finally {
      setLoading(false)
    }
  }

  const handleImport = async () => {
    if (!preview || preview.errors.length > 0) return
    setError(null)
    setImporting(true)
    try {
      const res = await fixturesService.importFixtures(leagueId, {
        seasonId,
        divisionId,
        csvText: csvText.trim(),
      })
      if (res.errors.length > 0) {
        setError(res.errors.join('\n'))
        return
      }
      onSuccess()
      onClose()
      reset()
    } catch (e) {
      setError(e instanceof Error ? e.message : t('fixtures.importModal.errorImporting'))
    } finally {
      setImporting(false)
    }
  }

  const reset = () => {
    setCsvText('')
    setPreview(null)
    setError(null)
  }

  const handleClose = () => {
    reset()
    onClose()
  }

  const canImport = preview && preview.rows.length > 0 && preview.errors.length === 0

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="md" fullWidth>
      <DialogTitle>{t('fixtures.importModal.title')}</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
          {t('fixtures.importModal.instructions')}
        </Typography>
        <TextField
          fullWidth
          multiline
          minRows={6}
          maxRows={12}
          placeholder={CSV_PLACEHOLDER}
          value={csvText}
          onChange={(e) => setCsvText(e.target.value)}
          variant="outlined"
          margin="dense"
          sx={{ fontFamily: 'monospace', fontSize: '0.875rem' }}
        />
        {error && (
          <Alert severity="error" sx={{ mt: 1 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}
        {loading && (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 2 }}>
            <CircularProgress size={28} />
          </Box>
        )}
        {preview && !loading && (
          <Box sx={{ mt: 2 }}>
            <Typography variant="subtitle2" color="text.secondary">
              {t('fixtures.importModal.formatDetected')} {preview.importType}
            </Typography>
            {preview.errors.length > 0 && (
              <Alert severity="warning" sx={{ mt: 1 }}>
                {preview.errors.map((err, i) => (
                  <div key={i}>{err}</div>
                ))}
              </Alert>
            )}
            {preview.rows.length > 0 && (
              <TableContainer component={Paper} variant="outlined" sx={{ mt: 1, maxHeight: 280 }}>
                <Table size="small" stickyHeader>
                  <TableHead>
                    <TableRow>
                      <TableCell>{t('fixtures.cols.round')}</TableCell>
                      {preview.importType !== 'Simple' && <TableCell>{t('fixtures.cols.date')}</TableCell>}
                      {preview.importType === 'Full' && (
                        <>
                          <TableCell>{t('fixtures.cols.time')}</TableCell>
                          <TableCell>{t('fixtures.cols.field')}</TableCell>
                        </>
                      )}
                      <TableCell>{t('fixtures.cols.home')}</TableCell>
                      <TableCell>{t('fixtures.cols.away')}</TableCell>
                      {preview.rows.some((r) => r.rowError) && <TableCell>{t('fixtures.importModal.errorCol')}</TableCell>}
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {preview.rows.map((row, i) => (
                      <TableRow key={i} sx={{ bgcolor: row.rowError ? 'error.light' : undefined }}>
                        <TableCell>{row.round}</TableCell>
                        {preview.importType !== 'Simple' && (
                          <TableCell>{row.date ?? '—'}</TableCell>
                        )}
                        {preview.importType === 'Full' && (
                          <>
                            <TableCell>{row.time ?? '—'}</TableCell>
                            <TableCell>{row.field ?? '—'}</TableCell>
                          </>
                        )}
                        <TableCell>{row.homeTeam}</TableCell>
                        <TableCell>{row.awayTeam}</TableCell>
                        {preview.rows.some((r) => r.rowError) && (
                          <TableCell sx={{ color: 'error.main' }}>{row.rowError ?? '—'}</TableCell>
                        )}
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            )}
          </Box>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose}>{t('common.cancel')}</Button>
        <Button onClick={handlePreview} disabled={!csvText.trim() || loading} variant="outlined">
          {t('fixtures.importModal.previewButton')}
        </Button>
        <Button
          onClick={handleImport}
          disabled={!canImport || importing}
          variant="contained"
        >
          {importing ? <CircularProgress size={24} /> : t('fixtures.importModal.importButton')}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
