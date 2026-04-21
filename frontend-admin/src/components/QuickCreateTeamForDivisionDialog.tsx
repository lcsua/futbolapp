import { useState } from 'react'
import { Dialog, DialogTitle, DialogContent, DialogActions, Button } from '@mui/material'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { TeamForm } from './TeamForm'
import { teamsService } from '../api/teams'
import type { TeamFormData } from '../api/types'

export type QuickCreateTeamForDivisionDialogProps = {
  open: boolean
  onClose: () => void
  /** Called after the team is created, assigned, and caches invalidated. */
  onCreated?: () => void
  leagueId: string
  seasonId: string
  divisionId: string
  divisionName: string
}

export function QuickCreateTeamForDivisionDialog({
  open,
  onClose,
  onCreated,
  leagueId,
  seasonId,
  divisionId,
  divisionName,
}: QuickCreateTeamForDivisionDialogProps) {
  const queryClient = useQueryClient()
  const [formError, setFormError] = useState<string | null>(null)

  const { data: clubs = [], isLoading: clubsLoading } = useQuery({
    queryKey: ['leagues', leagueId, 'clubs'],
    queryFn: ({ signal }) => teamsService.getClubsByLeague(leagueId, signal),
    enabled: open && !!leagueId,
  })

  const mutation = useMutation({
    mutationFn: async (data: TeamFormData) => {
      const created = await teamsService.create(leagueId, {
        name: data.name,
        suffix: data.suffix,
        clubId: data.clubId,
        shortName: data.shortName,
        email: data.email,
        seasonId,
        divisionId,
      })
      await teamsService.assignTeamToDivisionSeason(leagueId, seasonId, divisionId, created.id)
    },
    onSuccess: () => {
      setFormError(null)
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'seasons', seasonId, 'setup'] })
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'teams'] })
      onCreated?.()
      onClose()
    },
    onError: (err) => {
      setFormError(err instanceof Error ? err.message : 'Failed to create or assign team')
    },
  })

  const handleSubmit = (data: TeamFormData) => {
    setFormError(null)
    mutation.mutate(data)
  }

  return (
    <Dialog open={open} onClose={() => !mutation.isPending && onClose()} maxWidth="sm" fullWidth>
      <DialogTitle>New team — {divisionName}</DialogTitle>
      <DialogContent>
        <TeamForm
          key={`${divisionId}-${String(open)}`}
          clubs={clubs}
          clubsLoading={clubsLoading}
          onSubmit={handleSubmit}
          loading={mutation.isPending}
          error={formError}
          submitLabel={mutation.isPending ? 'Saving…' : 'Create and assign'}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={mutation.isPending}>
          Cancel
        </Button>
      </DialogActions>
    </Dialog>
  )
}
