import { splitCsvLine } from './parseTeamCsv'

export type ScheduleCsvRow = {
  fieldName: string
  homeTeam: string
  awayTeam: string
  /** Canonical HH:mm */
  startTime: string
  rawStartTime: string
}

function headerKey(h: string): string {
  return h
    .trim()
    .toLowerCase()
    .normalize('NFD')
    .replace(/\p{M}/gu, '')
    .replace(/\s+/g, ' ')
}

const COL = {
  field: new Set(['cancha', 'field', 'venue', 'campo']),
  home: new Set(['local', 'home', 'equipo 1', 'equipo1', 'home_team', 'home team']),
  away: new Set(['visitante', 'away', 'equipo 2', 'equipo2', 'away_team', 'away team']),
  timeReal: new Set(['horario_real', 'horario real', 'hora', 'horario', 'time', 'kickoff', 'hora real']),
  timeTol: new Set(['horario_tolerancia', 'horario tolerancia', 'tolerancia']),
}

function findCol(headers: string[], aliases: Set<string>): number {
  return headers.findIndex((h) => aliases.has(headerKey(h)))
}

/** Parse "12:00 Hs" / "13:10" → "HH:mm" or null. */
export function parseKickoffTime(raw: string): string | null {
  let s = raw.trim()
  if (!s) return null
  s = s.replace(/\s*Hs\.?$/i, '').trim()
  s = s.replace('.', ':')
  const m = /^(\d{1,2}):(\d{2})(?::\d{2})?$/.exec(s)
  if (!m) return null
  const hh = Number(m[1])
  const mm = Number(m[2])
  if (!Number.isFinite(hh) || !Number.isFinite(mm) || hh < 0 || hh > 23 || mm < 0 || mm > 59) return null
  return `${String(hh).padStart(2, '0')}:${String(mm).padStart(2, '0')}`
}

/**
 * CSV: cancha,local,horario_tolerancia,horario_real,visitante
 */
export function parseScheduleCsv(text: string): ScheduleCsvRow[] {
  const raw = text.replace(/^\uFEFF/, '').replace(/\r\n/g, '\n').replace(/\r/g, '\n')
  const lines = raw.split('\n').map((l) => l.trim()).filter((l) => l.length > 0)
  if (lines.length < 2) {
    throw new Error('El CSV está vacío o solo tiene encabezado.')
  }

  const headers = splitCsvLine(lines[0])
  const iField = findCol(headers, COL.field)
  const iHome = findCol(headers, COL.home)
  const iAway = findCol(headers, COL.away)
  const iReal = findCol(headers, COL.timeReal)
  const iTol = findCol(headers, COL.timeTol)

  if (iField < 0 || iHome < 0 || iAway < 0 || (iReal < 0 && iTol < 0)) {
    throw new Error(
      'El CSV necesita columnas: cancha, local, horario_real (o tolerancia), visitante.'
    )
  }

  const rows: ScheduleCsvRow[] = []
  for (let r = 1; r < lines.length; r++) {
    const cells = splitCsvLine(lines[r])
    const fieldName = (cells[iField] ?? '').trim()
    const homeTeam = (cells[iHome] ?? '').trim()
    const awayTeam = (cells[iAway] ?? '').trim()
    const realRaw = iReal >= 0 ? (cells[iReal] ?? '').trim() : ''
    const tolRaw = iTol >= 0 ? (cells[iTol] ?? '').trim() : ''
    const rawStartTime = realRaw || tolRaw
    if (!fieldName && !homeTeam && !awayTeam) continue
    if (!homeTeam || !awayTeam) {
      throw new Error(`Fila ${r + 1}: faltan local o visitante.`)
    }
    const startTime = parseKickoffTime(rawStartTime)
    if (!startTime) {
      throw new Error(`Fila ${r + 1}: horario inválido "${rawStartTime || '(vacío)'}".`)
    }
    rows.push({
      fieldName,
      homeTeam,
      awayTeam,
      startTime,
      rawStartTime,
    })
  }

  if (rows.length === 0) {
    throw new Error('No hay filas de partidos en el CSV.')
  }
  return rows
}
