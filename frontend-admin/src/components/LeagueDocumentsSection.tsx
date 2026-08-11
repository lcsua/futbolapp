import { useMemo, useState } from 'react'
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  FormControlLabel,
  IconButton,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import DeleteIcon from '@mui/icons-material/Delete'
import EditIcon from '@mui/icons-material/Edit'
import UploadFileIcon from '@mui/icons-material/UploadFile'
import CategoryIcon from '@mui/icons-material/Category'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  leagueDocumentsService,
  type DocumentCategory,
  type LeagueDocument,
} from '../api/leagueDocuments'

const MAX_PDF_BYTES = 10 * 1024 * 1024
const MAX_IMAGE_BYTES = 5 * 1024 * 1024

type DocFormState = {
  categoryId: string
  title: string
  description: string
  documentDate: string
  isPublished: boolean
  file: File | null
}

const emptyDocForm = (categoryId = ''): DocFormState => ({
  categoryId,
  title: '',
  description: '',
  documentDate: '',
  isPublished: true,
  file: null,
})

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

function isImageContentType(contentType: string): boolean {
  return contentType.toLowerCase().startsWith('image/')
}

type Props = {
  leagueId: string
}

export function LeagueDocumentsSection({ leagueId }: Props) {
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)

  const [categoryDialogOpen, setCategoryDialogOpen] = useState(false)
  const [categoryName, setCategoryName] = useState('')
  const [categoryRequiresDate, setCategoryRequiresDate] = useState(false)

  const [docDialogOpen, setDocDialogOpen] = useState(false)
  const [editingDoc, setEditingDoc] = useState<LeagueDocument | null>(null)
  const [docForm, setDocForm] = useState<DocFormState>(emptyDocForm())

  const categoriesQuery = useQuery({
    queryKey: ['leagues', leagueId, 'document-categories'],
    queryFn: ({ signal }) => leagueDocumentsService.getCategories(leagueId, signal),
  })

  const documentsQuery = useQuery({
    queryKey: ['leagues', leagueId, 'documents'],
    queryFn: ({ signal }) => leagueDocumentsService.getDocuments(leagueId, undefined, signal),
  })

  const categories = useMemo(
    () => [...(categoriesQuery.data ?? [])].sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name)),
    [categoriesQuery.data],
  )

  const documentsByCategory = useMemo(() => {
    const map = new Map<string, LeagueDocument[]>()
    for (const doc of documentsQuery.data ?? []) {
      const list = map.get(doc.categoryId) ?? []
      list.push(doc)
      map.set(doc.categoryId, list)
    }
    for (const list of map.values()) {
      list.sort((a, b) => a.sortOrder - b.sortOrder || a.title.localeCompare(b.title))
    }
    return map
  }, [documentsQuery.data])

  const invalidate = async () => {
    await queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'document-categories'] })
    await queryClient.invalidateQueries({ queryKey: ['leagues', leagueId, 'documents'] })
  }

  const seedMutation = useMutation({
    mutationFn: () => leagueDocumentsService.seedDefaults(leagueId),
    onSuccess: async () => {
      setError(null)
      await invalidate()
    },
    onError: (err) => setError(err instanceof Error ? err.message : 'No se pudieron crear las categorías'),
  })

  const createCategoryMutation = useMutation({
    mutationFn: () =>
      leagueDocumentsService.createCategory(leagueId, {
        name: categoryName.trim(),
        requiresDocumentDate: categoryRequiresDate,
      }),
    onSuccess: async () => {
      setCategoryDialogOpen(false)
      setCategoryName('')
      setCategoryRequiresDate(false)
      setError(null)
      await invalidate()
    },
    onError: (err) => setError(err instanceof Error ? err.message : 'No se pudo crear la categoría'),
  })

  const deleteCategoryMutation = useMutation({
    mutationFn: (categoryId: string) => leagueDocumentsService.deleteCategory(leagueId, categoryId),
    onSuccess: async () => {
      setError(null)
      await invalidate()
    },
    onError: (err) => setError(err instanceof Error ? err.message : 'No se pudo eliminar la categoría'),
  })

  const saveDocMutation = useMutation({
    mutationFn: async () => {
      const category = categories.find((c) => c.id === docForm.categoryId)
      if (!category) throw new Error('Elegí una categoría')
      const title = docForm.title.trim()
      if (!title) throw new Error('El título es obligatorio')
      if (category.requiresDocumentDate && !docForm.documentDate) {
        throw new Error('Esta categoría requiere la fecha de la resolución')
      }

      let upload: {
        url: string
        relativeUrl: string
        contentType: string
        fileSizeBytes: number
        originalFileName: string
      } | null = null

      if (docForm.file) {
        const file = docForm.file
        const isPdf = file.type === 'application/pdf' || file.name.toLowerCase().endsWith('.pdf')
        const max = isPdf ? MAX_PDF_BYTES : MAX_IMAGE_BYTES
        if (file.size > max) {
          throw new Error(isPdf ? 'El PDF no puede superar 10 MB' : 'La imagen no puede superar 5 MB')
        }
        upload = await leagueDocumentsService.upload(leagueId, file)
      } else if (!editingDoc) {
        throw new Error('Seleccioná un archivo (PDF o imagen)')
      }

      if (editingDoc) {
        await leagueDocumentsService.updateDocument(leagueId, editingDoc.id, {
          title,
          description: docForm.description.trim() || null,
          documentDate: docForm.documentDate || null,
          isPublished: docForm.isPublished,
          sortOrder: editingDoc.sortOrder,
          ...(upload
            ? {
                fileUrl: upload.relativeUrl,
                relativePath: upload.relativeUrl,
                contentType: upload.contentType,
                fileSizeBytes: upload.fileSizeBytes,
                originalFileName: upload.originalFileName,
              }
            : {}),
        })
      } else {
        await leagueDocumentsService.createDocument(leagueId, {
          categoryId: docForm.categoryId,
          title,
          description: docForm.description.trim() || null,
          documentDate: docForm.documentDate || null,
          fileUrl: upload!.relativeUrl,
          relativePath: upload!.relativeUrl,
          contentType: upload!.contentType,
          fileSizeBytes: upload!.fileSizeBytes,
          originalFileName: upload!.originalFileName,
          isPublished: docForm.isPublished,
        })
      }
    },
    onSuccess: async () => {
      setDocDialogOpen(false)
      setEditingDoc(null)
      setDocForm(emptyDocForm())
      setError(null)
      await invalidate()
    },
    onError: (err) => setError(err instanceof Error ? err.message : 'No se pudo guardar el documento'),
  })

  const deleteDocMutation = useMutation({
    mutationFn: (documentId: string) => leagueDocumentsService.deleteDocument(leagueId, documentId),
    onSuccess: async () => {
      setError(null)
      await invalidate()
    },
    onError: (err) => setError(err instanceof Error ? err.message : 'No se pudo eliminar el documento'),
  })

  const openCreateDoc = (category?: DocumentCategory) => {
    setEditingDoc(null)
    setDocForm(emptyDocForm(category?.id ?? categories[0]?.id ?? ''))
    setDocDialogOpen(true)
  }

  const openEditDoc = (doc: LeagueDocument) => {
    setEditingDoc(doc)
    setDocForm({
      categoryId: doc.categoryId,
      title: doc.title,
      description: doc.description ?? '',
      documentDate: doc.documentDate ?? '',
      isPublished: doc.isPublished,
      file: null,
    })
    setDocDialogOpen(true)
  }

  const selectedCategory = categories.find((c) => c.id === docForm.categoryId)
  const loading = categoriesQuery.isLoading || documentsQuery.isLoading

  return (
    <Paper variant="outlined" sx={{ p: 2.5, mt: 3 }}>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} alignItems={{ sm: 'center' }} justifyContent="space-between" sx={{ mb: 2 }}>
        <Box>
          <Typography variant="h6" sx={{ fontWeight: 600 }}>
            Documentación de la liga
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Mapas, reglamentos, resoluciones y otra información visible en la página pública. PDF hasta 10 MB; imágenes hasta 5 MB.
          </Typography>
        </Box>
        <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
          {categories.length === 0 && (
            <Button
              size="small"
              variant="outlined"
              onClick={() => seedMutation.mutate()}
              disabled={seedMutation.isPending}
            >
              Crear categorías por defecto
            </Button>
          )}
          <Button size="small" variant="outlined" startIcon={<CategoryIcon />} onClick={() => setCategoryDialogOpen(true)}>
            Nueva categoría
          </Button>
          <Button
            size="small"
            variant="contained"
            startIcon={<UploadFileIcon />}
            onClick={() => openCreateDoc()}
            disabled={categories.length === 0}
          >
            Subir documento
          </Button>
        </Stack>
      </Stack>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {loading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 3 }}>
          <CircularProgress size={28} />
        </Box>
      )}

      {!loading && categories.length === 0 && (
        <Alert severity="info">
          No hay categorías. Podés crear las predeterminadas (“Información útil” y “Resoluciones”) o agregar las tuyas.
        </Alert>
      )}

      {!loading &&
        categories.map((category) => {
          const docs = documentsByCategory.get(category.id) ?? []
          return (
            <Box key={category.id} sx={{ mb: 3 }}>
              <Stack direction="row" alignItems="center" spacing={1} sx={{ mb: 1 }}>
                <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
                  {category.name}
                </Typography>
                {category.requiresDocumentDate && <Chip size="small" label="Requiere fecha" />}
                {!category.isActive && <Chip size="small" color="warning" label="Inactiva" />}
                <Box sx={{ flex: 1 }} />
                <Button size="small" onClick={() => openCreateDoc(category)}>
                  Agregar
                </Button>
                <IconButton
                  size="small"
                  aria-label="Eliminar categoría"
                  onClick={() => {
                    if (docs.length > 0) {
                      setError('Eliminá primero los documentos de la categoría')
                      return
                    }
                    if (window.confirm(`¿Eliminar la categoría “${category.name}”?`)) {
                      deleteCategoryMutation.mutate(category.id)
                    }
                  }}
                >
                  <DeleteIcon fontSize="small" />
                </IconButton>
              </Stack>

              {docs.length === 0 ? (
                <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                  Sin documentos en esta categoría.
                </Typography>
              ) : (
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Título</TableCell>
                      <TableCell>Archivo</TableCell>
                      <TableCell>Fecha</TableCell>
                      <TableCell>Estado</TableCell>
                      <TableCell align="right">Acciones</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {docs.map((doc) => (
                      <TableRow key={doc.id}>
                        <TableCell>
                          <Typography variant="body2" sx={{ fontWeight: 500 }}>
                            {doc.title}
                          </Typography>
                          {doc.description && (
                            <Typography variant="caption" color="text.secondary" display="block">
                              {doc.description}
                            </Typography>
                          )}
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2">
                            {isImageContentType(doc.contentType) ? 'Imagen' : 'PDF'} · {formatBytes(doc.fileSizeBytes)}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {doc.originalFileName}
                          </Typography>
                        </TableCell>
                        <TableCell>{doc.documentDate ?? '—'}</TableCell>
                        <TableCell>
                          <Chip
                            size="small"
                            label={doc.isPublished ? 'Publicado' : 'Borrador'}
                            color={doc.isPublished ? 'success' : 'default'}
                            variant="outlined"
                          />
                        </TableCell>
                        <TableCell align="right">
                          <IconButton size="small" aria-label="Editar" onClick={() => openEditDoc(doc)}>
                            <EditIcon fontSize="small" />
                          </IconButton>
                          <IconButton
                            size="small"
                            aria-label="Eliminar"
                            onClick={() => {
                              if (window.confirm(`¿Eliminar “${doc.title}”?`)) {
                                deleteDocMutation.mutate(doc.id)
                              }
                            }}
                          >
                            <DeleteIcon fontSize="small" />
                          </IconButton>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </Box>
          )
        })}

      <Dialog open={categoryDialogOpen} onClose={() => setCategoryDialogOpen(false)} fullWidth maxWidth="xs">
        <DialogTitle>Nueva categoría</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            <TextField
              label="Nombre"
              value={categoryName}
              onChange={(e) => setCategoryName(e.target.value)}
              fullWidth
              required
              autoFocus
            />
            <FormControlLabel
              control={
                <Checkbox
                  checked={categoryRequiresDate}
                  onChange={(e) => setCategoryRequiresDate(e.target.checked)}
                />
              }
              label="Requiere fecha (p. ej. resoluciones)"
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCategoryDialogOpen(false)}>Cancelar</Button>
          <Button
            variant="contained"
            disabled={!categoryName.trim() || createCategoryMutation.isPending}
            onClick={() => createCategoryMutation.mutate()}
          >
            Crear
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={docDialogOpen} onClose={() => setDocDialogOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>{editingDoc ? 'Editar documento' : 'Subir documento'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            <FormControl fullWidth disabled={!!editingDoc}>
              <InputLabel id="doc-cat-label">Categoría</InputLabel>
              <Select
                labelId="doc-cat-label"
                label="Categoría"
                value={docForm.categoryId}
                onChange={(e) => setDocForm((f) => ({ ...f, categoryId: e.target.value }))}
              >
                {categories.map((c) => (
                  <MenuItem key={c.id} value={c.id}>
                    {c.name}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <TextField
              label="Título"
              value={docForm.title}
              onChange={(e) => setDocForm((f) => ({ ...f, title: e.target.value }))}
              fullWidth
              required
            />
            <TextField
              label="Descripción (opcional)"
              value={docForm.description}
              onChange={(e) => setDocForm((f) => ({ ...f, description: e.target.value }))}
              fullWidth
              multiline
              minRows={2}
            />
            {selectedCategory?.requiresDocumentDate && (
              <TextField
                label="Fecha de la resolución"
                type="date"
                value={docForm.documentDate}
                onChange={(e) => setDocForm((f) => ({ ...f, documentDate: e.target.value }))}
                InputLabelProps={{ shrink: true }}
                fullWidth
                required
              />
            )}
            <Button variant="outlined" component="label" startIcon={<UploadFileIcon />}>
              {docForm.file
                ? docForm.file.name
                : editingDoc
                  ? 'Reemplazar archivo (opcional)'
                  : 'Elegir PDF o imagen'}
              <input
                type="file"
                hidden
                accept=".pdf,image/jpeg,image/png,image/webp,image/gif,application/pdf"
                onChange={(e) => setDocForm((f) => ({ ...f, file: e.target.files?.[0] ?? null }))}
              />
            </Button>
            <FormControlLabel
              control={
                <Checkbox
                  checked={docForm.isPublished}
                  onChange={(e) => setDocForm((f) => ({ ...f, isPublished: e.target.checked }))}
                />
              }
              label="Publicado en la web"
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDocDialogOpen(false)}>Cancelar</Button>
          <Button
            variant="contained"
            disabled={saveDocMutation.isPending}
            onClick={() => saveDocMutation.mutate()}
          >
            {saveDocMutation.isPending ? 'Guardando…' : 'Guardar'}
          </Button>
        </DialogActions>
      </Dialog>
    </Paper>
  )
}
