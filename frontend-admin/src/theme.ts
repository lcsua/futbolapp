import { createTheme } from '@mui/material/styles'

export const theme = createTheme({
  palette: {
    primary: { main: '#1b5e20' },
    secondary: { main: '#ffc107' },
    background: { default: '#f5f5f5', paper: '#ffffff' },
  },
  shape: { borderRadius: 8 },
  typography: {
    fontFamily: '"Inter", "Roboto", "Helvetica", "Arial", sans-serif',
    h6: { fontWeight: 600 },
  },
  components: {
    MuiButton: {
      styleOverrides: {
        root: { textTransform: 'none' },
      },
    },
    MuiAppBar: {
      styleOverrides: {
        root: { boxShadow: 'none', borderBottom: '1px solid', borderColor: 'divider' },
      },
    },
  },
})
