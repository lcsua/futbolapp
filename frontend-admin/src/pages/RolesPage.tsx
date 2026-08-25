import { useMemo, useState } from 'react'
import { Link as RouterLink, useNavigate } from 'react-router-dom'
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  FormGroup,
  IconButton,
  TextField,
  Typography,
  CircularProgress,
  Paper,
} from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline'
import EditIcon from '@mui/icons-material/Edit'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { rolesService, type RoleItem } from '../api/roles'
import { useLeagueId } from '../contexts/LeagueContext'

export function RolesPage() {
  const { t } = useTranslation()
  const leagueId = useLeagueId()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [editing, setEditing] = useState<RoleItem | null>(null)
  const [creating, setCreating] = useState(false)
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [selected, setSelected] = useState<string[]>([])
  const [formError, setFormError] = useState<string | null>(null)

  const { data: roles, isLoading, isError, error } = useQuery({
    queryKey: ['leagues', leagueId, 'roles'],
    queryFn: ({ signal }) => rolesService.getByLeagueId(leagueId!, signal),
    enabled: !!leagueId,
  })

  const { data: catalog = [] } = useQuery({
    queryKey: ['permissions'],
    queryFn: ({ signal }) => rolesService.getCatalog(signal),
  })

  const groupedCatalog = useMemo(() => {
    const groups = new Map<string, typeof catalog>()
    for (const item of catalog) {
      const list = groups.get(item.module) ?? []
      list.push(item)
      groups.set(item.module, list)
    }
    return Array.from(groups.entries())
  }, [catalog])

  const saveMutation = useMutation({
    mutationFn: () => {
      const payload = { name: name.trim(), description: description.trim(), permissionCodes: selected }
      if (editing) return rolesService.update(leagueId!, editing.id, payload)
      return rolesService.create(leagueId!, payload)
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'roles'] })
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'my-access'] })
      closeDialog()
    },
    onError: (err) => {
      setFormError(err instanceof Error ? err.message : t('roles.saveError'))
    },
  })

  const deleteMutation = useMutation({
    mutationFn: (roleId: string) => rolesService.remove(leagueId!, roleId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'roles'] })
    },
  })

  const openCreate = () => {
    setCreating(true)
    setEditing(null)
    setName('')
    setDescription('')
    setSelected([])
    setFormError(null)
  }

  const openEdit = (role: RoleItem) => {
    setCreating(false)
    setEditing(role)
    setName(role.name)
    setDescription(role.description ?? '')
    setSelected(role.permissions)
    setFormError(null)
  }

  const closeDialog = () => {
    setCreating(false)
    setEditing(null)
    setFormError(null)
  }

  const dialogOpen = creating || editing != null
  const isAdminRole = editing?.code === 'ADMIN'

  if (!leagueId) {
    return (
      <Alert severity="error" action={<Button onClick={() => navigate('/')}>{t('roles.goToLeagues')}</Button>}>
        {t('roles.noLeague')}
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
    return <Alert severity="error">{error instanceof Error ? error.message : t('roles.loadError')}</Alert>
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3, gap: 2, flexWrap: 'wrap' }}>
        <Typography variant="h5" component="h1" fontWeight={600}>
          {t('roles.title')}
        </Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate}>
          {t('roles.create')}
        </Button>
      </Box>

      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
        {(roles ?? []).map((role) => (
          <Paper key={role.id} variant="outlined" sx={{ p: 2 }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 2, alignItems: 'flex-start' }}>
              <Box>
                <Typography variant="h6">
                  {role.name}{' '}
                  {role.isSystem ? <Chip size="small" label={t('roles.system')} sx={{ ml: 1 }} /> : null}
                </Typography>
                {role.description ? (
                  <Typography variant="body2" color="text.secondary">
                    {role.description}
                  </Typography>
                ) : null}
                <Box sx={{ mt: 1, display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                  {role.permissions.map((code) => (
                    <Chip key={code} size="small" label={t(`permissions.${code}`, { defaultValue: code })} />
                  ))}
                </Box>
              </Box>
              <Box>
                <IconButton aria-label={t('roles.edit')} onClick={() => openEdit(role)} disabled={role.code === 'ADMIN'}>
                  <EditIcon />
                </IconButton>
                <IconButton
                  aria-label={t('roles.delete')}
                  disabled={role.isSystem || deleteMutation.isPending}
                  onClick={() => {
                    if (window.confirm(t('roles.deleteConfirm'))) deleteMutation.mutate(role.id)
                  }}
                >
                  <DeleteOutlineIcon />
                </IconButton>
              </Box>
            </Box>
          </Paper>
        ))}
      </Box>

      <Dialog open={dialogOpen} onClose={closeDialog} fullWidth maxWidth="sm">
        <DialogTitle>{editing ? t('roles.edit') : t('roles.create')}</DialogTitle>
        <DialogContent>
          {formError ? <Alert severity="error" sx={{ mb: 2 }}>{formError}</Alert> : null}
          <TextField
            label={t('roles.name')}
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            fullWidth
            disabled={!!editing?.isSystem}
            sx={{ mt: 1, mb: 2 }}
          />
          <TextField
            label={t('roles.description')}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            fullWidth
            sx={{ mb: 2 }}
          />
          <Typography variant="subtitle2" sx={{ mb: 1 }}>
            {t('roles.permissions')}
          </Typography>
          {isAdminRole ? (
            <Alert severity="info">{t('roles.adminLocked')}</Alert>
          ) : (
            groupedCatalog.map(([module, items]) => (
              <Box key={module} sx={{ mb: 1.5 }}>
                <Typography variant="caption" color="text.secondary">
                  {t(`permissionModules.${module}`, { defaultValue: module })}
                </Typography>
                <FormGroup>
                  {items.map((item) => (
                    <FormControlLabel
                      key={item.code}
                      control={
                        <Checkbox
                          checked={selected.includes(item.code)}
                          onChange={(e) => {
                            setSelected((current) =>
                              e.target.checked
                                ? [...current, item.code]
                                : current.filter((code) => code !== item.code),
                            )
                          }}
                        />
                      }
                      label={t(`permissions.${item.code}`, { defaultValue: item.name })}
                    />
                  ))}
                </FormGroup>
              </Box>
            ))
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={closeDialog}>{t('common.cancel')}</Button>
          <Button
            variant="contained"
            disabled={!name.trim() || saveMutation.isPending || isAdminRole}
            onClick={() => {
              setFormError(null)
              saveMutation.mutate()
            }}
          >
            {t('common.save')}
          </Button>
        </DialogActions>
      </Dialog>

      <Button component={RouterLink} to="/" size="small" sx={{ mt: 3 }}>
        {t('roles.goToLeagues')}
      </Button>
    </Box>
  )
}
