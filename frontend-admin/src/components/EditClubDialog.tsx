import { useEffect, useState } from 'react'
import { Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, TextField, Typography } from '@mui/material'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { Club } from '../api/types'
import { teamsService } from '../api/teams'

interface EditClubDialogProps {
  open: boolean
  leagueId: string
  club: Club | null
  onClose: () => void
}

export function EditClubDialog({ open, leagueId, club, onClose }: EditClubDialogProps) {
  const queryClient = useQueryClient()
  const [name, setName] = useState('')
  const [logoUrl, setLogoUrl] = useState('')
  const [logoFile, setLogoFile] = useState<File | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!club) return
    setName(club.name)
    setLogoUrl(club.logoUrl ?? '')
    setLogoFile(null)
    setError(null)
  }, [club])

  const updateMutation = useMutation({
    mutationFn: async () => {
      if (!club) return
      let finalLogoUrl = logoUrl.trim() || undefined
      if (logoFile) {
        const upload = await teamsService.uploadImage(leagueId, logoFile)
        finalLogoUrl = upload.url
      }
      await teamsService.updateClub(leagueId, club.id, { name: name.trim(), logoUrl: finalLogoUrl })
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'clubs'] })
      onClose()
    },
    onError: (err) => {
      setError(err instanceof Error ? err.message : 'Failed to update club')
    },
  })

  const handleSave = () => {
    if (!club || !name.trim()) return
    setError(null)
    updateMutation.mutate()
  }

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>Edit club</DialogTitle>
      <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
        {error ? <Alert severity="error">{error}</Alert> : null}
        <TextField
          label="Club name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          required
          disabled={updateMutation.isPending}
        />
        <TextField
          label="Logo URL (optional)"
          value={logoUrl}
          onChange={(e) => setLogoUrl(e.target.value)}
          disabled={updateMutation.isPending}
        />
        <div>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
            Replace logo by uploading file
          </Typography>
          <input
            type="file"
            accept="image/jpeg,image/png,image/gif,image/webp"
            disabled={updateMutation.isPending}
            onChange={(e) => setLogoFile(e.target.files?.[0] ?? null)}
          />
        </div>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={updateMutation.isPending}>Cancel</Button>
        <Button onClick={handleSave} variant="contained" disabled={updateMutation.isPending || !name.trim()}>
          Save
        </Button>
      </DialogActions>
    </Dialog>
  )
}
