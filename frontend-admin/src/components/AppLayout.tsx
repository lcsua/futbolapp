import { useState } from 'react'
import { Outlet, Link as RouterLink, useLocation, useNavigate } from 'react-router-dom'
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
import { useAuth } from '../contexts/AuthContext'
import { LeagueSelector } from './LeagueSelector'
import { useTranslation } from 'react-i18next'

const DRAWER_WIDTH = 260

const NAV_ITEMS = [
  { to: '/', txKey: 'nav.leagues', icon: <EmojiEventsIcon /> },
  { to: '/seasons', txKey: 'nav.seasons', icon: <CalendarMonthIcon /> },
  { to: '/season-setup', txKey: 'nav.seasonSetup', icon: <SettingsIcon /> },
  { to: '/season-setup/advanced', txKey: 'nav.advancedSeasonSetup', icon: <SettingsIcon /> },
  { to: '/divisions', txKey: 'nav.divisions', icon: <ViewListIcon /> },
  { to: '/teams', txKey: 'nav.teams', icon: <GroupsIcon /> },
  { to: '/clubs', txKey: 'nav.clubs', icon: <ApartmentIcon /> },
  { to: '/teams/bulk', txKey: 'nav.bulkImport', icon: <UploadFileIcon /> },
  { to: '/fields', txKey: 'nav.fields', icon: <PlaceIcon /> },
  { to: '/fixtures', txKey: 'nav.fixtures', icon: <EventIcon /> },
  { to: '/matches', txKey: 'nav.matches', icon: <SportsSoccerIcon /> },
  { to: '/standings', txKey: 'nav.standings', icon: <LeaderboardIcon /> },
]

const COMPETITION_SETTINGS_ITEMS = [
  { to: '/competition-rules', txKey: 'nav.competitionRules', icon: <RuleIcon /> },
  { to: '/match-rules', txKey: 'nav.matchRules', icon: <ScheduleIcon /> },
]

export function AppLayout() {
  const { t } = useTranslation()
  const theme = useTheme()
  const isDesktop = useMediaQuery(theme.breakpoints.up('md'))
  const [mobileOpen, setMobileOpen] = useState(false)
  const location = useLocation()
  const navigate = useNavigate()
  const { user, logout } = useAuth()

  const handleLogout = () => {
    logout()
    navigate('/login', { replace: true })
  }

  const drawer = (
    <Box sx={{ pt: 2 }}>
      <List component="nav" sx={{ px: 1 }}>
        {NAV_ITEMS.map(({ to, txKey, icon }) => {
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
      <Typography variant="overline" color="text.secondary" sx={{ px: 2, pt: 2, pb: 0.5, display: 'block' }}>
        {t('nav.competitionSettings')}
      </Typography>
      <List component="nav" sx={{ px: 1 }}>
        {COMPETITION_SETTINGS_ITEMS.map(({ to, txKey, icon }) => (
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
        <Outlet />
      </Box>
    </Box>
  )
}
