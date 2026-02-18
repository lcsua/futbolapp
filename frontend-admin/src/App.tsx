import { Routes, Route, Navigate } from 'react-router-dom'
import { AuthProvider } from './contexts/AuthContext'
import { LeagueProvider } from './contexts/LeagueContext'
import { AppLayout } from './components/AppLayout'
import { ProtectedRoute } from './components/ProtectedRoute'
import { LeagueScopedRoute } from './components/LeagueScopedRoute'
import { LeaguesListPage } from './pages/LeaguesListPage'
import { CreateLeaguePage } from './pages/CreateLeaguePage'
import { EditLeaguePage } from './pages/EditLeaguePage'
import { SeasonsListPage } from './pages/SeasonsListPage'
import { CreateSeasonPage } from './pages/CreateSeasonPage'
import { EditSeasonPage } from './pages/EditSeasonPage'
import { DivisionsListPage } from './pages/DivisionsListPage'
import { CreateDivisionPage } from './pages/CreateDivisionPage'
import { EditDivisionPage } from './pages/EditDivisionPage'
import { TeamsListPage } from './pages/TeamsListPage'
import { CreateTeamPage } from './pages/CreateTeamPage'
import { EditTeamPage } from './pages/EditTeamPage'
import { BulkTeamImportPage } from './pages/BulkTeamImportPage'
import { SeasonSetupPage } from './pages/SeasonSetupPage'
import { AdvancedSeasonSetupPage } from './pages/AdvancedSeasonSetupPage'
import { FieldsListPage } from './pages/FieldsListPage'
import { CreateFieldPage } from './pages/CreateFieldPage'
import { EditFieldPage } from './pages/EditFieldPage'
import { CompetitionRulesPage } from './pages/CompetitionRulesPage'
import { MatchRulesPage } from './pages/MatchRulesPage'
import { LoginPage } from './pages/LoginPage'

export default function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route
          path="/"
          element={
            <ProtectedRoute>
              <LeagueProvider>
                <AppLayout />
              </LeagueProvider>
            </ProtectedRoute>
          }
        >
          <Route index element={<LeaguesListPage />} />
          <Route path="leagues/new" element={<CreateLeaguePage />} />
          <Route path="leagues/:leagueId/edit" element={<EditLeaguePage />} />
          <Route path="leagues/:leagueId/seasons" element={<SeasonsListPage />} />
          <Route path="leagues/:leagueId/seasons/new" element={<CreateSeasonPage />} />
          <Route path="leagues/:leagueId/seasons/:seasonId/edit" element={<EditSeasonPage />} />
          <Route path="leagues/:leagueId/divisions" element={<DivisionsListPage />} />
          <Route path="leagues/:leagueId/divisions/new" element={<CreateDivisionPage />} />
          <Route path="leagues/:leagueId/divisions/:divisionId/edit" element={<EditDivisionPage />} />
          <Route path="leagues/:leagueId/teams" element={<TeamsListPage />} />
          <Route path="leagues/:leagueId/teams/new" element={<CreateTeamPage />} />
          <Route path="leagues/:leagueId/teams/bulk" element={<BulkTeamImportPage />} />
          <Route path="leagues/:leagueId/teams/:teamId/edit" element={<EditTeamPage />} />
          <Route path="leagues/:leagueId/season-setup" element={<SeasonSetupPage />} />
          <Route path="leagues/:leagueId/season-setup/advanced" element={<AdvancedSeasonSetupPage />} />
          <Route path="leagues/:leagueId/fields" element={<FieldsListPage />} />
          <Route path="leagues/:leagueId/fields/new" element={<CreateFieldPage />} />
          <Route path="leagues/:leagueId/fields/:fieldId/edit" element={<EditFieldPage />} />
          <Route path="leagues/:leagueId/competition-rules" element={<CompetitionRulesPage />} />
          <Route path="leagues/:leagueId/match-rules" element={<MatchRulesPage />} />
          <Route element={<LeagueScopedRoute />}>
            <Route path="seasons" element={<SeasonsListPage />} />
            <Route path="seasons/new" element={<CreateSeasonPage />} />
            <Route path="seasons/:seasonId/edit" element={<EditSeasonPage />} />
            <Route path="divisions" element={<DivisionsListPage />} />
            <Route path="divisions/new" element={<CreateDivisionPage />} />
            <Route path="divisions/:divisionId/edit" element={<EditDivisionPage />} />
            <Route path="teams" element={<TeamsListPage />} />
            <Route path="teams/new" element={<CreateTeamPage />} />
            <Route path="teams/bulk" element={<BulkTeamImportPage />} />
            <Route path="teams/:teamId/edit" element={<EditTeamPage />} />
            <Route path="season-setup" element={<SeasonSetupPage />} />
            <Route path="season-setup/advanced" element={<AdvancedSeasonSetupPage />} />
          <Route path="fields" element={<FieldsListPage />} />
          <Route path="fields/new" element={<CreateFieldPage />} />
          <Route path="fields/:fieldId/edit" element={<EditFieldPage />} />
          <Route path="competition-rules" element={<CompetitionRulesPage />} />
          <Route path="match-rules" element={<MatchRulesPage />} />
          </Route>
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </AuthProvider>
  )
}
