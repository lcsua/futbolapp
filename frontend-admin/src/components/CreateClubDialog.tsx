import { useState } from 'react'
import { Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, TextField, Typography } from '@mui/material'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { teamsService } from '../api/teams'

interface CreateClubDialogProps {
  open: boolean
  leagueId: string
  onClose: () => void
}

export function CreateClubDialog({ open, leagueId, onClose }: CreateClubDialogProps) {
  const queryClient = useQueryClient()
  const [name, setName] = useState('')
  const [logoUrl, setLogoUrl] = useState('')
  const [logoFile, setLogoFile] = useState<File | null>(null)
  const [error, setError] = useState<string | null>(null)

  const createMutation = useMutation({
    mutationFn: async () => {
      let finalLogoUrl = logoUrl.trim() || undefined
      if (logoFile) {
        const upload = await teamsService.uploadImage(leagueId, logoFile)
        finalLogoUrl = upload.url
      }

      return teamsService.createClub(leagueId, {
        name: name.trim(),
        logoUrl: finalLogoUrl,
      })
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'clubs'] })
      setName('')
      setLogoUrl('')
      setLogoFile(null)
      setError(null)
      onClose()
    },
    onError: (err) => {
      setError(err instanceof Error ? err.message : 'Failed to create club')
    },
  })

  const handleClose = () => {
    if (createMutation.isPending) return
    setError(null)
    setLogoFile(null)
    onClose()
  }

  const handleCreate = () => {
    if (!name.trim()) return
    setError(null)
    createMutation.mutate()
  }

  return (
    <Dialog open={open} onClose={handleClose} fullWidth maxWidth="sm">
      <DialogTitle>Create club</DialogTitle>
      <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
        {error ? <Alert severity="error">{error}</Alert> : null}
        <TextField
          label="Club name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          required
          autoFocus
          disabled={createMutation.isPending}
        />
        <TextField
          label="Logo URL (optional)"
          value={logoUrl}
          onChange={(e) => setLogoUrl(e.target.value)}
          disabled={createMutation.isPending}
        />
        <div>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
            Or upload logo file (jpg, png, gif, webp)
          </Typography>
          <input
            type="file"
            accept="image/jpeg,image/png,image/gif,image/webp"
            disabled={createMutation.isPending}
            onChange={(e) => setLogoFile(e.target.files?.[0] ?? null)}
          />
          {logoFile ? (
            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.5 }}>
              Selected: {logoFile.name}
            </Typography>
          ) : null}
        </div>
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose} disabled={createMutation.isPending}>
          Cancel
        </Button>
        <Button onClick={handleCreate} variant="contained" disabled={createMutation.isPending || !name.trim()}>
          Create
        </Button>
      </DialogActions>
    </Dialog>
  )
}
