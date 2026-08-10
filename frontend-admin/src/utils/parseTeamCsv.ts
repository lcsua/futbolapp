/** Parse a teams CSV (header optional). Uses first column, or the column named Equipo/Team/Nombre. */

function splitCsvLine(line: string): string[] {
  const cells: string[] = []
  let current = ''
  let inQuotes = false
  for (let i = 0; i < line.length; i++) {
    const ch = line[i]
    if (inQuotes) {
      if (ch === '"') {
        if (line[i + 1] === '"') {
          current += '"'
          i++
        } else {
          inQuotes = false
        }
      } else {
        current += ch
      }
    } else if (ch === '"') {
      inQuotes = true
    } else if (ch === ',') {
      cells.push(current.trim())
      current = ''
    } else {
      current += ch
    }
  }
  cells.push(current.trim())
  return cells
}

const HEADER_ALIASES = new Set(['equipo', 'team', 'nombre', 'name', 'club', 'equipos'])

export function parseTeamNamesFromCsv(text: string): string[] {
  const raw = text.replace(/^\uFEFF/, '').replace(/\r\n/g, '\n').replace(/\r/g, '\n')
  const lines = raw.split('\n').map((l) => l.trim()).filter((l) => l.length > 0)
  if (lines.length === 0) return []

  const firstCells = splitCsvLine(lines[0])
  const headerIdx = firstCells.findIndex((c) => HEADER_ALIASES.has(c.toLowerCase()))
  const hasHeader = headerIdx >= 0
  const colIndex = hasHeader ? headerIdx : 0
  const dataLines = hasHeader ? lines.slice(1) : lines

  const names: string[] = []
  const seen = new Set<string>()
  for (const line of dataLines) {
    const cells = splitCsvLine(line)
    const name = (cells[colIndex] ?? '').trim()
    if (!name) continue
    const key = name.toLocaleLowerCase('es')
    if (seen.has(key)) continue
    seen.add(key)
    names.push(name)
  }
  return names
}
