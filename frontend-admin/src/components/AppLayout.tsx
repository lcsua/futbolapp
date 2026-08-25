import { useState } from 'react'
import { Outlet, Link as RouterLink, useLocation, useNavigate, Navigate } from 'react-router-dom'
import {
  AppBar,
  Box,
  Button,
  Drawer,
  IconButton,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material'
import MenuIcon from '@mui/icons-material/Menu'
import SportsSoccerIcon from '@mui/icons-material/SportsSoccer'
import EmojiEventsIcon from '@mui/icons-material/EmojiEvents'
import LeaderboardIcon from '@mui/icons-material/Leaderboard'
import CalendarMonthIcon from '@mui/icons-material/CalendarMonth'
import ViewListIcon from '@mui/icons-material/ViewList'
import GroupsIcon from '@mui/icons-material/Groups'
import ApartmentIcon from '@mui/icons-material/Apartment'
import PlaceIcon from '@mui/icons-material/Place'
import SettingsIcon from '@mui/icons-material/Settings'
import RuleIcon from '@mui/icons-material/Rule'
import ScheduleIcon from '@mui/icons-material/Schedule'
import EventIcon from '@mui/icons-material/Event'
import UploadFileIcon from '@mui/icons-material/UploadFile'
import PeopleIcon from '@mui/icons-material/People'
import AdminPanelSettingsIcon from '@mui/icons-material/AdminPanelSettings'
import { useAuth } from '../contexts/AuthContext'
import { LeagueSelector } from './LeagueSelector'
import { useTranslation } from 'react-i18next'
import { usePermissions } from '../contexts/PermissionContext'

const DRAWER_WIDTH = 260

const NAV_ITEMS = [
  { to: '/', txKey: 'nav.leagues', icon: <EmojiEventsIcon />, permission: null },
  { to: '/seasons', txKey: 'nav.seasons', icon: <CalendarMonthIcon />, permission: 'seasons' },
  { to: '/season-setup', txKey: 'nav.seasonSetup', icon: <SettingsIcon />, permission: 'season_setup' },
  { to: '/season-setup/advanced', txKey: 'nav.advancedSeasonSetup', icon: <SettingsIcon />, permission: 'season_setup' },
  { to: '/divisions', txKey: 'nav.divisions', icon: <ViewListIcon />, permission: 'divisions' },
  { to: '/teams', txKey: 'nav.teams', icon: <GroupsIcon />, permission: 'teams' },
  { to: '/clubs', txKey: 'nav.clubs', icon: <ApartmentIcon />, permission: 'clubs' },
  { to: '/teams/bulk', txKey: 'nav.bulkImport', icon: <UploadFileIcon />, permission: 'teams' },
  { to: '/fields', txKey: 'nav.fields', icon: <PlaceIcon />, permission: 'fields' },
  { to: '/fixtures', txKey: 'nav.fixtures', icon: <EventIcon />, permission: 'fixtures' },
  { to: '/matches', txKey: 'nav.matches', icon: <SportsSoccerIcon />, permission: 'matches' },
  { to: '/standings', txKey: 'nav.standings', icon: <LeaderboardIcon />, permission: 'standings' },
]

const COMPETITION_SETTINGS_ITEMS = [
  { to: '/competition-rules', txKey: 'nav.competitionRules', icon: <RuleIcon />, permission: 'competition_rules' },
  { to: '/match-rules', txKey: 'nav.matchRules', icon: <ScheduleIcon />, permission: 'match_rules' },
]

const ADMIN_ITEMS = [
  { to: '/users', txKey: 'nav.users', icon: <PeopleIcon />, permission: 'users' },
  { to: '/roles', txKey: 'nav.roles', icon: <AdminPanelSettingsIcon />, permission: 'roles' },
]

function permissionForPath(pathname: string): string | null {
  if (pathname === '/') return null
  if (pathname === '/leagues/new' || /^\/leagues\/[^/]+\/edit$/.test(pathname)) return 'leagues'
  const path = pathname.replace(/^\/leagues\/[^/]+/, '') || '/'
  if (path === '/' || path === '') return null
  if (path.startsWith('/users')) return 'users'
  if (path.startsWith('/roles')) return 'roles'
  if (path.startsWith('/matches')) return 'matches'
  if (path.startsWith('/standings')) return 'standings'
  if (path.startsWith('/fixtures')) return 'fixtures'
  if (path.startsWith('/fields')) return 'fields'
  if (path.startsWith('/clubs')) return 'clubs'
  if (path.startsWith('/teams')) return 'teams'
  if (path.startsWith('/divisions')) return 'divisions'
  if (path.startsWith('/season-setup') || path.includes('division-scheduling')) return 'season_setup'
  if (path.startsWith('/seasons')) return 'seasons'
  if (path.startsWith('/competition-rules')) return 'competition_rules'
  if (path.startsWith('/match-rules')) return 'match_rules'
  return null
}

export function AppLayout() {
  const { t } = useTranslation()
  const theme = useTheme()
  const isDesktop = useMediaQuery(theme.breakpoints.up('md'))
  const [mobileOpen, setMobileOpen] = useState(false)
  const location = useLocation()
  const navigate = useNavigate()
  const { user, logout } = useAuth()
  const { hasPermission, isLoading: permissionsLoading, permissions } = usePermissions()
  const requiredPermission = permissionForPath(location.pathname)

  const visibleNavItems = NAV_ITEMS.filter((item) => !item.permission || hasPermission(item.permission))
  const visibleSettingsItems = COMPETITION_SETTINGS_ITEMS.filter((item) => hasPermission(item.permission))
  const visibleAdminItems = ADMIN_ITEMS.filter((item) => hasPermission(item.permission))

  const handleLogout = () => {
    logout()
    navigate('/login', { replace: true })
  }

  const drawer = (
    <Box sx={{ pt: 2 }}>
      <List component="nav" sx={{ px: 1 }}>
        {visibleNavItems.map(({ to, txKey, icon }) => {
          const selected =
            to === '/'
              ? location.pathname === '/' || location.pathname.startsWith('/leagues')
              : to === '/teams'
                ? location.pathname === '/teams' || (location.pathname.startsWith('/teams/') && location.pathname !== '/teams/bulk')
                : location.pathname === to || location.pathname.startsWith(to + '/')
          return (
          <ListItemButton
            key={to}
            component={RouterLink}
            to={to}
            selected={selected}
            onClick={() => !isDesktop && setMobileOpen(false)}
          >
            <ListItemIcon>{icon}</ListItemIcon>
            <ListItemText primary={t(txKey)} />
          </ListItemButton>
          )
        })}
      </List>
      {visibleSettingsItems.length > 0 ? (
        <>
          <Typography variant="overline" color="text.secondary" sx={{ px: 2, pt: 2, pb: 0.5, display: 'block' }}>
            {t('nav.competitionSettings')}
          </Typography>
          <List component="nav" sx={{ px: 1 }}>
            {visibleSettingsItems.map(({ to, txKey, icon }) => (
              <ListItemButton
                key={to}
                component={RouterLink}
                to={to}
                selected={location.pathname === to || location.pathname.includes(to.slice(1))}
                onClick={() => !isDesktop && setMobileOpen(false)}
              >
                <ListItemIcon>{icon}</ListItemIcon>
                <ListItemText primary={t(txKey)} />
              </ListItemButton>
            ))}
          </List>
        </>
      ) : null}
      {visibleAdminItems.length > 0 ? (
        <>
          <Typography variant="overline" color="text.secondary" sx={{ px: 2, pt: 2, pb: 0.5, display: 'block' }}>
            {t('nav.administration')}
          </Typography>
          <List component="nav" sx={{ px: 1 }}>
            {visibleAdminItems.map(({ to, txKey, icon }) => (
              <ListItemButton
                key={to}
                component={RouterLink}
                to={to}
                selected={location.pathname === to || location.pathname.startsWith(to + '/')}
                onClick={() => !isDesktop && setMobileOpen(false)}
              >
                <ListItemIcon>{icon}</ListItemIcon>
                <ListItemText primary={t(txKey)} />
              </ListItemButton>
            ))}
          </List>
        </>
      ) : null}
    </Box>
  )

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      <AppBar
        position="fixed"
        color="default"
        sx={{
          width: { md: `calc(100% - ${DRAWER_WIDTH}px)` },
          ml: { md: `${DRAWER_WIDTH}px` },
          boxShadow: 'none',
          borderBottom: '1px solid',
          borderColor: 'divider',
        }}
      >
        <Toolbar sx={{ minHeight: { xs: 56, sm: 64 } }}>
          <IconButton
            edge="start"
            color="inherit"
            aria-label="open menu"
            onClick={() => setMobileOpen(true)}
            sx={{ mr: 1, display: { md: 'none' } }}
          >
            <MenuIcon />
          </IconButton>
          <SportsSoccerIcon color="primary" sx={{ mr: 1.5, display: { xs: 'none', sm: 'block' } }} />
          <Typography variant="h6" component="h1" sx={{ fontWeight: 600, fontSize: { xs: '1rem', sm: '1.25rem' } }}>
            {t('app.title')}
          </Typography>
          <Box sx={{ flexGrow: 1, display: 'flex', justifyContent: 'center' }}>
            <LeagueSelector />
          </Box>
          {user && (
            <Box sx={{ display: { xs: 'none', sm: 'flex' }, alignItems: 'center', gap: 1.5 }}>
              <Typography variant="body2" color="text.secondary">
                {user.email}
              </Typography>
              <Button size="small" variant="outlined" onClick={handleLogout}>
                {t('common.logout')}
              </Button>
            </Box>
          )}
        </Toolbar>
      </AppBar>

      <Drawer
        variant={isDesktop ? 'permanent' : 'temporary'}
        open={isDesktop ? true : mobileOpen}
        onClose={() => setMobileOpen(false)}
        ModalProps={{ keepMounted: true }}
        sx={{
          width: DRAWER_WIDTH,
          flexShrink: 0,
          '& .MuiDrawer-paper': {
            width: DRAWER_WIDTH,
            boxSizing: 'border-box',
            top: { xs: 56, sm: 64 },
            pt: 2,
            height: { xs: 'calc(100vh - 56px)', sm: 'calc(100vh - 64px)' },
            borderRight: '1px solid',
            borderColor: 'divider',
          },
        }}
      >
        {drawer}
      </Drawer>

      <Box
        component="main"
        sx={{
          flexGrow: 1,
          width: { md: `calc(100% - ${DRAWER_WIDTH}px)` },
          minHeight: '100vh',
          mt: { xs: 7, sm: 8 },
          p: { xs: 2, sm: 3 },
          mx: location.pathname.includes('season-setup/advanced') ? 0 : 'auto',
          maxWidth: location.pathname.includes('season-setup/advanced') ? 'none' : 1000,
        }}
      >
        {permissionsLoading && requiredPermission ? null : requiredPermission && !hasPermission(requiredPermission) ? (
          <Navigate to={permissions.includes('matches') ? '/matches' : '/'} replace />
        ) : (
          <Outlet />
        )}
      </Box>
    </Box>
  )
}
