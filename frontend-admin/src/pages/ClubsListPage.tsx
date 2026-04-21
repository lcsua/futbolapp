import { useState } from 'react'
import { Alert, Box, Button, Card, CardContent, CircularProgress, Typography } from '@mui/material'
import EditIcon from '@mui/icons-material/Edit'
import AddIcon from '@mui/icons-material/Add'
import { useQuery } from '@tanstack/react-query'
import { useLeagueId, useActiveLeague } from '../contexts/LeagueContext'
import { teamsService } from '../api/teams'
import type { Club } from '../api/types'
import { CreateClubDialog } from '../components/CreateClubDialog'
import { EditClubDialog } from '../components/EditClubDialog'

export function ClubsListPage() {
  const leagueId = useLeagueId()
  const activeLeague = useActiveLeague()
  const [createOpen, setCreateOpen] = useState(false)
  const [editingClub, setEditingClub] = useState<Club | null>(null)

  const { data: clubs = [], isLoading, isError, error } = useQuery({
    queryKey: ['leagues', leagueId, 'clubs'],
    queryFn: ({ signal }) => teamsService.getClubsByLeague(leagueId!, signal),
    enabled: !!leagueId,
  })

  if (!leagueId) return <Alert severity="error">No league selected.</Alert>

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress />
      </Box>
    )
  }

  if (isError) {
    return <Alert severity="error">{error instanceof Error ? error.message : 'Failed to load clubs'}</Alert>
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3, gap: 2, flexWrap: 'wrap' }}>
        <Typography variant="h5" component="h1" sx={{ fontWeight: 600 }}>
          {activeLeague?.name ?? 'League'} — Clubs
        </Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
          Create club
        </Button>
      </Box>

      {clubs.length === 0 ? (
        <Typography color="text.secondary">No clubs yet.</Typography>
      ) : (
        <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', md: 'repeat(2, 1fr)' } }}>
          {clubs.map((club) => (
            <Card key={club.id} variant="outlined">
              <CardContent>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 2 }}>
                  <Box sx={{ display: 'flex', gap: 1.5, alignItems: 'center' }}>
                    {club.logoUrl ? (
                      <Box
                        component="img"
                        src={club.logoUrl}
                        alt={`${club.name} logo`}
                        sx={{ width: 44, height: 44, objectFit: 'contain', borderRadius: 1, border: 1, borderColor: 'divider' }}
                      />
                    ) : null}
                    <Box>
                      <Typography variant="h6">{club.name}</Typography>
                      {club.logoUrl ? (
                        <Typography variant="caption" color="text.secondary" sx={{ wordBreak: 'break-all' }}>
                          {club.logoUrl}
                        </Typography>
                      ) : null}
                    </Box>
                  </Box>
                  <Button size="small" startIcon={<EditIcon />} onClick={() => setEditingClub(club)}>
                    Edit
                  </Button>
                </Box>
              </CardContent>
            </Card>
          ))}
        </Box>
      )}

      <CreateClubDialog open={createOpen} leagueId={leagueId} onClose={() => setCreateOpen(false)} />
      <EditClubDialog open={!!editingClub} leagueId={leagueId} club={editingClub} onClose={() => setEditingClub(null)} />
    </Box>
  )
}
