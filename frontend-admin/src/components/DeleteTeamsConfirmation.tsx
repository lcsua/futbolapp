import { useEffect, useState } from 'react'
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  TextField,
  Typography,
} from '@mui/material'
import type { Team } from '../api/types'

export const BULK_DELETE_CONFIRM_WORD = 'ELIMINAR'

export function getTeamDisplayName(team: Pick<Team, 'displayName' | 'name'>) {
  return (team.displayName ?? team.name).trim()
}

export function getDeleteTeamsConfirmExpected(teams: Team[]) {
  if (teams.length === 1) return getTeamDisplayName(teams[0])
  return BULK_DELETE_CONFIRM_WORD
}

interface DeleteTeamsConfirmationBodyProps {
  teams: Team[]
  confirmText: string
  onConfirmTextChange: (value: string) => void
  disabled?: boolean
}

export function DeleteTeamsConfirmationBody({
  teams,
  confirmText,
  onConfirmTextChange,
  disabled,
}: DeleteTeamsConfirmationBodyProps) {
  const isSingle = teams.length === 1
  const expected = getDeleteTeamsConfirmExpected(teams)
  const teamName = isSingle ? expected : null

  return (
    <>
      <Alert severity="error" sx={{ mb: 2 }}>
        Esta acción <strong>no se puede deshacer</strong>. Se borrarán el o los equipos y su plantel.
        Solo se pueden eliminar equipos que nunca se asignaron a una temporada, división ni fixture.
      </Alert>

      {isSingle ? (
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Se va a eliminar el equipo &quot;{teamName}&quot;.
        </Typography>
      ) : (
        <>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
            Se van a eliminar <strong>{teams.length}</strong> equipo{teams.length === 1 ? '' : 's'}:
          </Typography>
          <Box
            component="ul"
            sx={{
              maxHeight: 280,
              overflow: 'auto',
              border: 1,
              borderColor: 'divider',
              borderRadius: 1,
              px: 2.5,
              py: 1,
              mb: 2,
              mt: 0,
            }}
          >
            {teams.map((team) => (
              <Typography key={team.id} component="li" variant="body2" sx={{ py: 0.25 }}>
                {getTeamDisplayName(team)}
                {team.clubName ? ` — ${team.clubName}` : ''}
              </Typography>
            ))}
          </Box>
        </>
      )}

      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        {isSingle
          ? 'Para confirmar, escribí el nombre exacto del equipo:'
          : `Para confirmar, escribí ${BULK_DELETE_CONFIRM_WORD}:`}
      </Typography>
      <TextField
        autoFocus
        fullWidth
        label={isSingle ? 'Nombre del equipo' : 'Confirmación'}
        placeholder={expected}
        value={confirmText}
        onChange={(e) => onConfirmTextChange(e.target.value)}
        disabled={disabled}
      />
    </>
  )
}

interface ConfirmDeleteTeamsDialogProps {
  open: boolean
  teams: Team[]
  loading?: boolean
  onClose: () => void
  onConfirm: () => void
}

export function ConfirmDeleteTeamsDialog({
  open,
  teams,
  loading,
  onClose,
  onConfirm,
}: ConfirmDeleteTeamsDialogProps) {
  const [confirmText, setConfirmText] = useState('')

  useEffect(() => {
    if (open) setConfirmText('')
  }, [open])

  const expected = getDeleteTeamsConfirmExpected(teams)
  const canConfirm = teams.length > 0 && confirmText.trim() === expected
  const isSingle = teams.length === 1

  return (
    <Dialog
      open={open}
      onClose={() => !loading && onClose()}
      maxWidth={isSingle ? 'sm' : 'md'}
      fullWidth
    >
      <DialogTitle>
        {isSingle
          ? 'Eliminar equipo de forma permanente'
          : `Eliminar ${teams.length} equipos de forma permanente`}
      </DialogTitle>
      <DialogContent>
        <DeleteTeamsConfirmationBody
          teams={teams}
          confirmText={confirmText}
          onConfirmTextChange={setConfirmText}
          disabled={loading}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={loading}>
          Cancelar
        </Button>
        <Button
          color="error"
          variant="contained"
          onClick={onConfirm}
          disabled={!canConfirm || loading}
        >
          {loading ? <CircularProgress size={22} color="inherit" /> : 'Eliminar definitivamente'}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
