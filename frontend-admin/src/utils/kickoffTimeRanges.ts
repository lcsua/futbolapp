/** Formato alineado con `SchedulingRulesJsonParser.TryParseKickoffRanges`: `[{"start":"09:00","end":"13:00"}]`. */

export type KickoffRangeRow = { id: string; start: string; end: string }

export function newKickoffRow(): KickoffRangeRow {
  const id =
    typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function'
      ? crypto.randomUUID()
      : `${Date.now()}-${Math.random().toString(16).slice(2)}`
  return { id, start: '', end: '' }
}

function normalizeTimeForInput(t: string): string {
  const s = t.trim()
  if (!s) return ''
  const m = s.match(/^(\d{1,2}):(\d{2})(?::(\d{2}))?/)
  if (!m) return ''
  const h = Math.min(23, Math.max(0, parseInt(m[1], 10)))
  const min = Math.min(59, Math.max(0, parseInt(m[2], 10)))
  return `${String(h).padStart(2, '0')}:${String(min).padStart(2, '0')}`
}

export function parseKickoffRangesJson(json: string | null | undefined): KickoffRangeRow[] {
  if (!json?.trim()) return []
  try {
    const arr = JSON.parse(json) as unknown
    if (!Array.isArray(arr)) return []
    const rows: KickoffRangeRow[] = []
    for (const el of arr) {
      if (!el || typeof el !== 'object') continue
      const o = el as Record<string, unknown>
      const start = normalizeTimeForInput(String(o.start ?? ''))
      const end = normalizeTimeForInput(String(o.end ?? ''))
      const row = newKickoffRow()
      rows.push({ ...row, start, end })
    }
    return rows
  } catch {
    return []
  }
}

/** Minutos desde medianoche; null si no es HH:mm válido. */
export function timeStringToMinutes(t: string): number | null {
  const m = t.trim().match(/^(\d{1,2}):(\d{2})$/)
  if (!m) return null
  const h = parseInt(m[1], 10)
  const min = parseInt(m[2], 10)
  if (h < 0 || h > 23 || min < 0 || min > 59) return null
  return h * 60 + min
}

/** Devuelve JSON para el API o `null` si no hay franjas válidas (heredar globales). */
export function kickoffRangesToAllowedJson(rows: KickoffRangeRow[]): string | null {
  const parts: { start: string; end: string }[] = []
  for (const r of rows) {
    const start = r.start.trim()
    const end = r.end.trim()
    if (!start || !end) continue
    const sm = timeStringToMinutes(start)
    const em = timeStringToMinutes(end)
    if (sm == null || em == null || sm >= em) continue
    parts.push({ start, end })
  }
  return parts.length ? JSON.stringify(parts) : null
}

/** Fila con ambas horas informadas pero fin ≤ inicio (el backend la ignoraría). */
export function hasInvalidFilledRange(rows: KickoffRangeRow[]): boolean {
  for (const r of rows) {
    const start = r.start.trim()
    const end = r.end.trim()
    if (!start || !end) continue
    const sm = timeStringToMinutes(start)
    const em = timeStringToMinutes(end)
    if (sm != null && em != null && sm >= em) return true
  }
  return false
}
