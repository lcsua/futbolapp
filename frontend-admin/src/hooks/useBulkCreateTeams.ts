import { useMutation, useQueryClient } from '@tanstack/react-query'
import { teamsService } from '../api/teams'

export function useBulkCreateTeams() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ leagueId, names }: { leagueId: string; names: string[] }) =>
      teamsService.bulkCreate(leagueId, names),
    onSuccess: (_, variables) => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', variables.leagueId, 'teams'] })
    },
  })
}
