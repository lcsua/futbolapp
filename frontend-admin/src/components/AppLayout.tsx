import { useState } from 'react'
import { Outlet, Link as RouterLink, useLocation } from 'react-router-dom'
import {
  AppBar,
  Box,
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
import CalendarMonthIcon from '@mui/icons-material/CalendarMonth'
import ViewListIcon from '@mui/icons-material/ViewList'
import GroupsIcon from '@mui/icons-material/Groups'
import PlaceIcon from '@mui/icons-material/Place'
import SettingsIcon from '@mui/icons-material/Settings'
import RuleIcon from '@mui/icons-material/Rule'
import ScheduleIcon from '@mui/icons-material/Schedule'
import UploadFileIcon from '@mui/icons-material/UploadFile'
import { useAuth } from '../contexts/AuthContext'
import { LeagueSelector } from './LeagueSelector'

const APP_TITLE = 'Football Admin'
const DRAWER_WIDTH = 260

const NAV_ITEMS = [
  { to: '/', label: 'Leagues', icon: <EmojiEventsIcon /> },
  { to: '/seasons', label: 'Seasons', icon: <CalendarMonthIcon /> },
  { to: '/season-setup', label: 'Season setup', icon: <SettingsIcon /> },
  { to: '/season-setup/advanced', label: 'Advanced season setup', icon: <SettingsIcon /> },
  { to: '/divisions', label: 'Divisions', icon: <ViewListIcon /> },
  { to: '/teams', label: 'Teams', icon: <GroupsIcon /> },
  { to: '/teams/bulk', label: 'Bulk Import', icon: <UploadFileIcon /> },
  { to: '/fields', label: 'Fields', icon: <PlaceIcon /> },
]

const COMPETITION_SETTINGS_ITEMS = [
  { to: '/competition-rules', label: 'Competition rules', icon: <RuleIcon /> },
  { to: '/match-rules', label: 'Match rules', icon: <ScheduleIcon /> },
]

export function AppLayout() {
  const theme = useTheme()
  const isDesktop = useMediaQuery(theme.breakpoints.up('md'))
  const [mobileOpen, setMobileOpen] = useState(false)
  const location = useLocation()
  const { user } = useAuth()

  const drawer = (
    <Box sx={{ pt: 2 }}>
      <List component="nav" sx={{ px: 1 }}>
        {NAV_ITEMS.map(({ to, label, icon }) => {
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
            <ListItemText primary={label} />
          </ListItemButton>
          )
        })}
      </List>
      <Typography variant="overline" color="text.secondary" sx={{ px: 2, pt: 2, pb: 0.5, display: 'block' }}>
        Competition settings
      </Typography>
      <List component="nav" sx={{ px: 1 }}>
        {COMPETITION_SETTINGS_ITEMS.map(({ to, label, icon }) => (
          <ListItemButton
            key={to}
            component={RouterLink}
            to={to}
            selected={location.pathname === to || location.pathname.includes(to.slice(1))}
            onClick={() => !isDesktop && setMobileOpen(false)}
          >
            <ListItemIcon>{icon}</ListItemIcon>
            <ListItemText primary={label} />
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
            {APP_TITLE}
          </Typography>
          <Box sx={{ flexGrow: 1, display: 'flex', justifyContent: 'center' }}>
            <LeagueSelector />
          </Box>
          {user && (
            <Typography variant="body2" color="text.secondary" sx={{ display: { xs: 'none', sm: 'block' } }}>
              {user.email}
            </Typography>
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
          mx: 'auto',
          maxWidth: 1000,
        }}
      >
        <Outlet />
      </Box>
    </Box>
  )
}
