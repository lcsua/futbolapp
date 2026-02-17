import { useState, useMemo } from 'react'
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  CircularProgress,
  Snackbar,
  TextField,
  Typography,
} from '@mui/material'
import { useBulkCreateTeams } from '../hooks/useBulkCreateTeams'
import { useLeagueId } from '../contexts/LeagueContext'

function parseNames(text: string): string[] {
  return text
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter((line) => line.length > 0)
}

export function BulkTeamImportPage() {
  const leagueId = useLeagueId()
  const [text, setText] = useState('')
  const [snackbar, setSnackbar] = useState<{ open: boolean; message: string; severity: 'success' | 'error' }>({ open: false, message: '', severity: 'success' })
  const [lastResult, setLastResult] = useState<{ total: number; created: number } | null>(null)

  const bulkCreate = useBulkCreateTeams()

  const names = useMemo(() => parseNames(text), [text])
  const detectedCount = names.length
  const canSubmit = leagueId != null && detectedCount > 0

  const handleImport = () => {
    if (!leagueId || names.length === 0) return
    bulkCreate.mutate(
      { leagueId, names },
      {
        onSuccess: (data) => {
          const created = data?.createdIds?.length ?? 0
          setLastResult({ total: names.length, created })
          setText('')
          setSnackbar({
            open: true,
            message: `Imported ${created} team(s).`,
            severity: 'success',
          })
        },
        onError: (err) => {
          setSnackbar({
            open: true,
            message: err instanceof Error ? err.message : 'Import failed',
            severity: 'error',
          })
        },
      }
    )
  }

  const handleCloseSnackbar = () => {
    setSnackbar((s) => ({ ...s, open: false }))
  }

  const skipped = lastResult ? lastResult.total - lastResult.created : 0

  return (
    <Box>
      <Typography variant="h5" component="h1" sx={{ mb: 3, fontWeight: 600 }}>
        Bulk Team Import
      </Typography>

      {!leagueId && (
        <Alert severity="warning" sx={{ mb: 3 }}>
          Please select a league before importing teams.
        </Alert>
      )}

      <Card variant="outlined" sx={{ maxWidth: 720 }}>
        <CardContent>
          <TextField
            fullWidth
            multiline
            minRows={8}
            maxRows={20}
            placeholder="Paste one team name per line..."
            value={text}
            onChange={(e) => setText(e.target.value)}
            disabled={!leagueId}
            sx={{ mb: 2 }}
          />

          <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
            Detected {detectedCount} team name{detectedCount !== 1 ? 's' : ''}
          </Typography>

          <Button
            variant="contained"
            onClick={handleImport}
            disabled={!canSubmit || bulkCreate.isPending}
            sx={{ minWidth: 140 }}
          >
            {bulkCreate.isPending ? <CircularProgress size={24} color="inherit" /> : 'Import Teams'}
          </Button>

          {lastResult != null && (
            <Box sx={{ mt: 3, p: 2, bgcolor: 'action.hover', borderRadius: 1 }}>
              <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                Last import summary
              </Typography>
              <Typography variant="body2">
                Total lines detected: {lastResult.total}
              </Typography>
              <Typography variant="body2">
                Created: {lastResult.created}
              </Typography>
              <Typography variant="body2">
                Skipped: {skipped}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Created IDs: {lastResult.created}
              </Typography>
            </Box>
          )}
        </CardContent>
      </Card>

      <Snackbar
        open={snackbar.open}
        autoHideDuration={6000}
        onClose={handleCloseSnackbar}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert onClose={handleCloseSnackbar} severity={snackbar.severity} variant="filled" sx={{ width: '100%' }}>
          {snackbar.message}
        </Alert>
      </Snackbar>
    </Box>
  )
}
