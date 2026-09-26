import { useState } from 'react'
import { Link as RouterLink, useNavigate } from 'react-router-dom'
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  IconButton,
  InputLabel,
  MenuItem,
  Select,
  Snackbar,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
  CircularProgress,
} from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline'
import LockResetIcon from '@mui/icons-material/LockReset'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { usersService, type LeagueUser } from '../api/users'
import { rolesService } from '../api/roles'
import { useAuth } from '../contexts/AuthContext'
import { useLeagueId } from '../contexts/LeagueContext'

export function UsersPage() {
  const { t } = useTranslation()
  const leagueId = useLeagueId()
  const { user } = useAuth()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [open, setOpen] = useState(false)
  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [roleId, setRoleId] = useState('')
  const [formError, setFormError] = useState<string | null>(null)
  const [resetUser, setResetUser] = useState<LeagueUser | null>(null)
  const [resetPassword, setResetPassword] = useState('')
  const [resetError, setResetError] = useState<string | null>(null)
  const [snackbar, setSnackbar] = useState<string | null>(null)

  const { data: users, isLoading, isError, error } = useQuery({
    queryKey: ['leagues', leagueId, 'users'],
    queryFn: ({ signal }) => usersService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })

  const { data: roles = [] } = useQuery({
    queryKey: ['leagues', leagueId, 'roles'],
    queryFn: ({ signal }) => rolesService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })

  const createMutation = useMutation({
    mutationFn: () =>
      usersService.create(leagueId!, {
        fullName: fullName.trim(),
        email: email.trim(),
        password,
        roleId,
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'users'] })
      setOpen(false)
      setFullName('')
      setEmail('')
      setPassword('')
      setRoleId('')
      setFormError(null)
    },
    onError: (err) => {
      setFormError(err instanceof Error ? err.message : t('users.createError'))
    },
  })

  const updateRoleMutation = useMutation({
    mutationFn: ({ userId, nextRoleId }: { userId: string; nextRoleId: string }) =>
      usersService.updateRole(leagueId!, userId, nextRoleId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'users'] })
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'my-access'] })
    },
  })

  const removeMutation = useMutation({
    mutationFn: (userId: string) => usersService.remove(leagueId!, userId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'users'] })
    },
  })

  const resetPasswordMutation = useMutation({
    mutationFn: () => usersService.setPassword(leagueId!, resetUser!.userId, resetPassword),
    onSuccess: () => {
      setResetUser(null)
      setResetPassword('')
      setResetError(null)
      setSnackbar(t('users.resetPasswordSuccess'))
    },
    onError: (err) => {
      setResetError(err instanceof Error ? err.message : t('users.resetPasswordError'))
    },
  })

  if (!leagueId) {
    return (
      <Alert severity="error" action={<Button onClick={() => navigate('/')}>{t('users.goToLeagues')}</Button>}>
        {t('users.noLeague')}
      </Alert>
    )
  }

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress />
      </Box>
    )
  }

  if (isError) {
    return <Alert severity="error">{error instanceof Error ? error.message : t('users.loadError')}</Alert>
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3, gap: 2, flexWrap: 'wrap' }}>
        <Typography variant="h5" component="h1" fontWeight={600}>
          {t('users.title')}
        </Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setOpen(true)}>
          {t('users.create')}
        </Button>
      </Box>

      <Alert severity="info" sx={{ mb: 2 }}>
        {t('users.hint')}
      </Alert>

      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>{t('users.fullName')}</TableCell>
            <TableCell>{t('users.email')}</TableCell>
            <TableCell>{t('users.role')}</TableCell>
            <TableCell align="right">{t('users.actions')}</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {(users ?? []).map((member) => (
            <TableRow key={member.userId}>
              <TableCell>{member.fullName}</TableCell>
              <TableCell>{member.email}</TableCell>
              <TableCell>
                <FormControl size="small" sx={{ minWidth: 180 }}>
                  <Select
                    value={member.roleId ?? ''}
                    onChange={(e) =>
                      updateRoleMutation.mutate({ userId: member.userId, nextRoleId: e.target.value })
                    }
                    disabled={updateRoleMutation.isPending}
                  >
                    {roles.map((role) => (
                      <MenuItem key={role.id} value={role.id}>
                        {role.name}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </TableCell>
              <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
                <Button
                  size="small"
                  startIcon={<LockResetIcon />}
                  disabled={resetPasswordMutation.isPending}
                  onClick={() => {
                    setResetUser(member)
                    setResetPassword('')
                    setResetError(null)
                  }}
                >
                  {t('users.resetPassword')}
                </Button>
                <IconButton
                  aria-label={t('users.remove')}
                  disabled={member.userId === user?.userId || removeMutation.isPending}
                  onClick={() => {
                    if (window.confirm(t('users.removeConfirm'))) {
                      removeMutation.mutate(member.userId)
                    }
                  }}
                >
                  <DeleteOutlineIcon />
                </IconButton>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>

      <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>{t('users.create')}</DialogTitle>
        <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
          {formError ? <Alert severity="error">{formError}</Alert> : null}
          <TextField
            label={t('users.fullName')}
            value={fullName}
            onChange={(e) => setFullName(e.target.value)}
            required
            fullWidth
            sx={{ mt: 1 }}
          />
          <TextField
            label={t('users.email')}
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            fullWidth
          />
          <TextField
            label={t('users.password')}
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            helperText={t('users.passwordHint')}
            fullWidth
          />
          <FormControl fullWidth required>
            <InputLabel id="new-user-role">{t('users.role')}</InputLabel>
            <Select
              labelId="new-user-role"
              label={t('users.role')}
              value={roleId}
              onChange={(e) => setRoleId(e.target.value)}
            >
              {roles.map((role) => (
                <MenuItem key={role.id} value={role.id}>
                  {role.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>{t('common.cancel')}</Button>
          <Button
            variant="contained"
            disabled={!fullName.trim() || !email.trim() || !roleId || password.trim().length < 6 || createMutation.isPending}
            onClick={() => {
              setFormError(null)
              createMutation.mutate()
            }}
          >
            {t('users.create')}
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog
        open={!!resetUser}
        onClose={() => {
          setResetUser(null)
          setResetPassword('')
          setResetError(null)
        }}
        fullWidth
        maxWidth="sm"
      >
        <DialogTitle>{t('users.resetPasswordTitle')}</DialogTitle>
        <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
          {resetError ? <Alert severity="error">{resetError}</Alert> : null}
          <Typography variant="body2" sx={{ mt: 1 }}>
            {resetUser?.fullName}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {resetUser?.email}
          </Typography>
          <TextField
            label={t('users.password')}
            type="password"
            value={resetPassword}
            onChange={(e) => setResetPassword(e.target.value)}
            helperText={t('users.resetPasswordHint')}
            autoFocus
            fullWidth
          />
        </DialogContent>
        <DialogActions>
          <Button
            onClick={() => {
              setResetUser(null)
              setResetPassword('')
              setResetError(null)
            }}
          >
            {t('common.cancel')}
          </Button>
          <Button
            variant="contained"
            disabled={resetPassword.trim().length < 6 || resetPasswordMutation.isPending}
            onClick={() => {
              setResetError(null)
              resetPasswordMutation.mutate()
            }}
          >
            {resetPasswordMutation.isPending ? <CircularProgress size={22} color="inherit" /> : t('users.resetPassword')}
          </Button>
        </DialogActions>
      </Dialog>

      <Snackbar
        open={!!snackbar}
        autoHideDuration={4000}
        onClose={() => setSnackbar(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        {snackbar ? (
          <Alert onClose={() => setSnackbar(null)} severity="success" variant="filled" sx={{ width: '100%' }}>
            {snackbar}
          </Alert>
        ) : undefined}
      </Snackbar>

      <Button component={RouterLink} to="/" size="small" sx={{ mt: 3 }}>
        {t('users.goToLeagues')}
      </Button>
    </Box>
  )
}
