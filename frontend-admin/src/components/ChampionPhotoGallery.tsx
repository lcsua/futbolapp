import { useEffect, useRef, useState } from 'react'
import {
  DndContext,
  PointerSensor,
  closestCenter,
  useSensor,
  useSensors,
  type DragEndEvent,
} from '@dnd-kit/core'
import { SortableContext, arrayMove, rectSortingStrategy, useSortable } from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import {
  Box,
  Button,
  CircularProgress,
  IconButton,
  TextField,
  Typography,
} from '@mui/material'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline'
import DragIndicatorIcon from '@mui/icons-material/DragIndicator'
import StarIcon from '@mui/icons-material/Star'
import StarBorderIcon from '@mui/icons-material/StarBorder'
import { useTranslation } from 'react-i18next'
import {
  validateChampionImage,
  type ChampionPhoto,
} from '../api/champions'

type ChampionPhotoGalleryProps = {
  photos: ChampionPhoto[]
  disabled?: boolean
  onUpload: (files: File[]) => Promise<void>
  onDelete: (photoId: string) => Promise<void>
  onCommit: (photos: ChampionPhoto[]) => Promise<void>
  onError: (message: string) => void
}

export function ChampionPhotoGallery({
  photos,
  disabled,
  onUpload,
  onDelete,
  onCommit,
  onError,
}: ChampionPhotoGalleryProps) {
  const { t } = useTranslation()
  const inputRef = useRef<HTMLInputElement | null>(null)
  const [items, setItems] = useState(photos)
  const [busy, setBusy] = useState(false)
  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 6 } }))

  useEffect(() => {
    setItems(photos)
  }, [photos])

  const commit = async (next: ChampionPhoto[]) => {
    setItems(next)
    setBusy(true)
    try {
      await onCommit(next)
    } catch (err) {
      setItems(photos)
      onError(err instanceof Error ? err.message : t('champions.photos.saveFailed'))
    } finally {
      setBusy(false)
    }
  }

  const handleFiles = async (list: FileList | null) => {
    const files = Array.from(list ?? [])
    if (inputRef.current) inputRef.current.value = ''
    if (files.length === 0) return

    for (const file of files) {
      const invalid = validateChampionImage(file)
      if (invalid) {
        onError(t(invalid))
        return
      }
    }

    setBusy(true)
    try {
      await onUpload(files)
    } catch (err) {
      onError(err instanceof Error ? err.message : t('champions.photos.uploadFailed'))
    } finally {
      setBusy(false)
    }
  }

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event
    if (!over || active.id === over.id) return
    const oldIndex = items.findIndex((photo) => photo.id === active.id)
    const newIndex = items.findIndex((photo) => photo.id === over.id)
    if (oldIndex < 0 || newIndex < 0) return
    const next = arrayMove(items, oldIndex, newIndex).map((photo, index) => ({
      ...photo,
      displayOrder: index,
    }))
    void commit(next)
  }

  const setCover = (photoId: string) => {
    void commit(items.map((photo) => ({ ...photo, isCover: photo.id === photoId })))
  }

  const setCaption = (photoId: string, caption: string) => {
    const current = items.find((photo) => photo.id === photoId)
    if (!current || (current.caption ?? '') === caption) return
    void commit(items.map((photo) => (photo.id === photoId ? { ...photo, caption: caption.trim() || null } : photo)))
  }

  const remove = async (photoId: string) => {
    if (!window.confirm(t('champions.photos.deleteConfirm'))) return
    setBusy(true)
    try {
      await onDelete(photoId)
    } catch (err) {
      onError(err instanceof Error ? err.message : t('champions.photos.deleteFailed'))
    } finally {
      setBusy(false)
    }
  }

  return (
    <Box sx={{ mt: 2 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 1, mb: 1 }}>
        <Typography variant="subtitle1">{t('champions.photos.title')}</Typography>
        <Button variant="outlined" size="small" disabled={disabled || busy} onClick={() => inputRef.current?.click()}>
          {busy ? <CircularProgress size={18} /> : t('champions.photos.add')}
        </Button>
        <input
          ref={inputRef}
          type="file"
          hidden
          multiple
          accept="image/jpeg,image/png,image/webp,.jpg,.jpeg,.png,.webp"
          onChange={(event) => void handleFiles(event.target.files)}
        />
      </Box>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
        {t('champions.photos.help')}
      </Typography>

      {items.length === 0 ? (
        <Typography variant="body2" color="text.secondary">
          {t('champions.photos.empty')}
        </Typography>
      ) : (
        <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
          <SortableContext items={items.map((photo) => photo.id)} strategy={rectSortingStrategy}>
            <Box sx={{ display: 'grid', gap: 1.5, gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' } }}>
              {items.map((photo) => (
                <SortableChampionPhoto
                  key={photo.id}
                  photo={photo}
                  disabled={disabled || busy}
                  onCover={() => setCover(photo.id)}
                  onCaption={(caption) => setCaption(photo.id, caption)}
                  onDelete={() => void remove(photo.id)}
                />
              ))}
            </Box>
          </SortableContext>
        </DndContext>
      )}
    </Box>
  )
}

function SortableChampionPhoto({
  photo,
  disabled,
  onCover,
  onCaption,
  onDelete,
}: {
  photo: ChampionPhoto
  disabled?: boolean
  onCover: () => void
  onCaption: (caption: string) => void
  onDelete: () => void
}) {
  const { t } = useTranslation()
  const { attributes, listeners, setNodeRef, transform, transition } = useSortable({ id: photo.id })
  const [caption, setCaption] = useState(photo.caption ?? '')

  useEffect(() => {
    setCaption(photo.caption ?? '')
  }, [photo.caption])

  return (
    <Box
      ref={setNodeRef}
      style={{ transform: CSS.Transform.toString(transform), transition }}
      sx={{
        display: 'grid',
        gridTemplateColumns: '88px 1fr',
        gap: 1,
        p: 1,
        border: '1px solid',
        borderColor: photo.isCover ? 'primary.main' : 'divider',
        borderRadius: 1,
        bgcolor: 'background.paper',
      }}
    >
      <Box
        component="img"
        src={photo.thumbUrl || photo.imageUrl}
        alt={caption || t('champions.photos.alt')}
        sx={{ width: 88, height: 88, objectFit: 'cover', borderRadius: 1, bgcolor: 'action.hover' }}
      />
      <Box sx={{ minWidth: 0 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
          <IconButton size="small" disabled={disabled} {...attributes} {...listeners} aria-label={t('champions.photos.reorder')}>
            <DragIndicatorIcon fontSize="small" />
          </IconButton>
          <IconButton
            size="small"
            color={photo.isCover ? 'primary' : 'default'}
            disabled={disabled}
            onClick={onCover}
            aria-label={t('champions.photos.cover')}
          >
            {photo.isCover ? <StarIcon fontSize="small" /> : <StarBorderIcon fontSize="small" />}
          </IconButton>
          <Typography variant="caption" sx={{ flex: 1 }} color={photo.isCover ? 'primary' : 'text.secondary'}>
            {photo.isCover ? t('champions.photos.coverLabel') : t('champions.photos.coverHint')}
          </Typography>
          <IconButton size="small" color="error" disabled={disabled} onClick={onDelete} aria-label={t('champions.photos.remove')}>
            <DeleteOutlineIcon fontSize="small" />
          </IconButton>
        </Box>
        <TextField
          fullWidth
          size="small"
          label={t('champions.photos.caption')}
          value={caption}
          disabled={disabled}
          onChange={(event) => setCaption(event.target.value)}
          onBlur={() => onCaption(caption)}
        />
      </Box>
    </Box>
  )
}
