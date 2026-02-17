import { Link as RouterLink, useLocation } from 'react-router-dom'
import {
  Box,
  Drawer,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
} from '@mui/material'
import CloseIcon from '@mui/icons-material/Close'
import DashboardIcon from '@mui/icons-material/Dashboard'
import GroupIcon from '@mui/icons-material/Group'
import IconButton from '@mui/material/IconButton'

const NAV_ITEMS = [
  { to: '/', label: 'Leagues', icon: <DashboardIcon /> },
  { to: '/seasons', label: 'Seasons', icon: <GroupIcon /> },
]

type SidebarProps = {
  open: boolean
  onClose: () => void
}

export function Sidebar({ open, onClose }: SidebarProps) {
  const location = useLocation()

  return (
    <Drawer
      variant="temporary"
      open={open}
      onClose={onClose}
      ModalProps={{ keepMounted: true }}
      sx={{
        display: { xs: 'block', sm: 'none' },
        '& .MuiDrawer-paper': { boxSizing: 'border-box', width: 260 },
      }}
    >
      <Box sx={{ pt: 1, display: 'flex', justifyContent: 'flex-end' }}>
        <IconButton onClick={onClose} aria-label="close menu">
          <CloseIcon />
        </IconButton>
      </Box>
      <List component="nav" sx={{ px: 1 }}>
        {NAV_ITEMS.map(({ to, label, icon }) => (
          <ListItemButton
            key={to}
            component={RouterLink}
            to={to}
            selected={location.pathname === to}
            onClick={onClose}
          >
            <ListItemIcon>{icon}</ListItemIcon>
            <ListItemText primary={label} />
          </ListItemButton>
        ))}
      </List>
    </Drawer>
  )
}

