/** Client-side parse of league fixture CSV (mirrors backend ImportFixturesUseCase). */

export type FixtureImportType = 'Simple' | 'WithDate' | 'Full'

export type ParsedFixtureCsvRow = {
  round: number
  date?: string | null
  time?: string | null
  field?: string | null
  homeTeam: string
  awayTeam: string
  /** 1-based data row index for messages */
  rowNumber: number
  rowError?: string
}

export type ParseFixtureCsvResult = {
  importType: FixtureImportType
  rows: ParsedFixtureCsvRow[]
  errors: string[]
}

function splitCsvLine(line: string): string[] {
  const result: string[] = []
  let current = ''
  let inQuotes = false
  for (let i = 0; i < line.length; i++) {
    const c = line[i]
    if (c === '"') {
      inQuotes = !inQuotes
    } else if (c === ',' && !inQuotes) {
      result.push(current)
      current = ''
    } else {
      current += c
    }
  }
  result.push(current)
  return result
}

function csvEscape(value: string): string {
  if (/[",\n\r]/.test(value)) {
    return `"${value.replace(/"/g, '""')}"`
  }
  return value
}

export function parseFixtureCsv(csvText: string): ParseFixtureCsvResult {
  const errors: string[] = []
  const lines = csvText
    .replace(/\r\n/g, '\n')
    .replace(/\r/g, '\n')
    .split('\n')
    .map((l) => l.trim())
    .filter((l) => l.length > 0)

  if (lines.length === 0) {
    return { importType: 'Simple', rows: [], errors: ['El CSV está vacío.'] }
  }

  const dataLines: string[][] = []
  for (const line of lines) {
    const cells = splitCsvLine(line).map((c) => c.trim())
    if (cells.length === 0) continue
    if (cells.length !== 3 && cells.length !== 4 && cells.length !== 6) {
      return {
        importType: 'Simple',
        rows: [],
        errors: [
          `Cantidad de columnas no soportada: ${cells.length}. Usá 3 (round,home_team,away_team), 4 (round,date,home_team,away_team) o 6 (round,date,time,field,home_team,away_team).`,
        ],
      }
    }
    // Skip optional header if first cell is not an integer
    if (dataLines.length === 0 && !/^\d+$/.test(cells[0] ?? '')) continue
    dataLines.push(cells)
  }

  if (dataLines.length === 0) {
    return { importType: 'Simple', rows: [], errors: ['No se encontraron filas de datos.'] }
  }

  const colCount = dataLines[0].length
  const importType: FixtureImportType =
    colCount === 3 ? 'Simple' : colCount === 4 ? 'WithDate' : 'Full'

  const rows: ParsedFixtureCsvRow[] = []
  for (let i = 0; i < dataLines.length; i++) {
    const cells = dataLines[i]
    const rowNumber = i + 1
    if (cells.length !== colCount) {
      errors.push(`Fila ${rowNumber}: se esperaban ${colCount} columnas, hay ${cells.length}.`)
      continue
    }
    const round = Number.parseInt(cells[0] ?? '', 10)
    if (!Number.isFinite(round) || round < 1) {
      errors.push(`Fila ${rowNumber}: número de fecha (round) inválido.`)
      continue
    }

    let date: string | null = null
    let time: string | null = null
    let field: string | null = null
    let homeTeam: string
    let awayTeam: string

    if (colCount === 3) {
      homeTeam = cells[1] ?? ''
      awayTeam = cells[2] ?? ''
    } else if (colCount === 4) {
      date = cells[1] || null
      homeTeam = cells[2] ?? ''
      awayTeam = cells[3] ?? ''
    } else {
      date = cells[1] || null
      time = cells[2] || null
      field = cells[3] || null
      homeTeam = cells[4] ?? ''
      awayTeam = cells[5] ?? ''
    }

    let rowError: string | undefined
    if (!homeTeam.trim() || !awayTeam.trim()) {
      rowError = 'Local y visitante son obligatorios.'
      errors.push(`Fila ${rowNumber}: ${rowError}`)
    } else if (homeTeam.trim().toLowerCase() === awayTeam.trim().toLowerCase()) {
      rowError = 'Local y visitante no pueden ser el mismo equipo.'
      errors.push(`Fila ${rowNumber}: ${rowError}`)
    }

    rows.push({
      round,
      date,
      time,
      field,
      homeTeam: homeTeam.trim(),
      awayTeam: awayTeam.trim(),
      rowNumber,
      rowError,
    })
  }

  return { importType, rows, errors }
}

/** Unique team names appearing in valid rows (preserves first-seen order). */
export function uniqueFixtureTeamNames(rows: ParsedFixtureCsvRow[]): string[] {
  const seen = new Set<string>()
  const names: string[] = []
  for (const row of rows) {
    if (row.rowError) continue
    for (const name of [row.homeTeam, row.awayTeam]) {
      const key = name.toLowerCase()
      if (seen.has(key)) continue
      seen.add(key)
      names.push(name)
    }
  }
  return names
}

/**
 * Rebuild CSV text after mapping CSV names → canonical Team.Name values.
 * `nameMap` keys are original csv names (case-insensitive lookup applied).
 */
export function rebuildFixtureCsv(
  importType: FixtureImportType,
  rows: ParsedFixtureCsvRow[],
  resolveTeamName: (csvName: string) => string | null,
): { csvText: string; rows: ParsedFixtureCsvRow[]; errors: string[] } {
  const errors: string[] = []
  const outRows: ParsedFixtureCsvRow[] = []
  const lines: string[] = []

  for (const row of rows) {
    if (row.rowError) {
      errors.push(`Fila ${row.rowNumber}: ${row.rowError}`)
      continue
    }
    const home = resolveTeamName(row.homeTeam)
    const away = resolveTeamName(row.awayTeam)
    if (!home || !away) {
      errors.push(`Fila ${row.rowNumber}: equipo sin mapear (${row.homeTeam} / ${row.awayTeam}).`)
      continue
    }
    if (home.toLowerCase() === away.toLowerCase()) {
      errors.push(`Fila ${row.rowNumber}: local y visitante resuelven al mismo equipo.`)
      continue
    }

    const resolved: ParsedFixtureCsvRow = {
      ...row,
      homeTeam: home,
      awayTeam: away,
    }
    outRows.push(resolved)

    if (importType === 'Simple') {
      lines.push([String(row.round), csvEscape(home), csvEscape(away)].join(','))
    } else if (importType === 'WithDate') {
      lines.push([String(row.round), csvEscape(row.date ?? ''), csvEscape(home), csvEscape(away)].join(','))
    } else {
      lines.push(
        [
          String(row.round),
          csvEscape(row.date ?? ''),
          csvEscape(row.time ?? ''),
          csvEscape(row.field ?? ''),
          csvEscape(home),
          csvEscape(away),
        ].join(','),
      )
    }
  }

  return { csvText: lines.join('\n'), rows: outRows, errors }
}
