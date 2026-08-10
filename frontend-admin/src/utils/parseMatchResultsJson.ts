import { matchCsvNamesToTeams, mappingsNeedReview, type TeamMatchCandidate } from './teamNameMatch'
import { normalizeTeamName, nameSimilarity } from './teamNameMatch'
import { splitCsvLine } from './parseTeamCsv'

export type JsonMatchResultRow = {
  homeTeam: string
  awayTeam: string
  homeScore: number | null
  awayScore: number | null
  status: string
}

export type JsonDivisionRoundBlock = {
  competition?: string
  division: string
  round: number
  matches: JsonMatchResultRow[]
  skippedByes: string[]
  skippedOther: string[]
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
  round: new Set(['fecha', 'round', 'jornada', 'fecha nro', 'nro fecha']),
  division: new Set(['division', 'división', 'categoria', 'categoría']),
  home: new Set(['equipo 1', 'equipo1', 'local', 'home', 'home_team', 'home team']),
  homeGoals: new Set(['goles equipo 1', 'goles equipo1', 'goles local', 'home_score', 'home score', 'gl']),
  away: new Set(['equipo 2', 'equipo2', 'visitante', 'away', 'away_team', 'away team']),
  awayGoals: new Set(['goles equipo 2', 'goles equipo2', 'goles visitante', 'away_score', 'away score', 'gv']),
  status: new Set(['estado', 'status', 'state']),
}

function findCol(headers: string[], aliases: Set<string>): number {
  return headers.findIndex((h) => aliases.has(headerKey(h)))
}

function parseScore(raw: string): number | null {
  const t = raw.trim()
  if (!t) return null
  const n = Number(t)
  return Number.isFinite(n) ? n : null
}

function mapEstadoToStatus(estado: string): string {
  const s = headerKey(estado)
  if (!s) return 'finished'
  if (s.includes('suspend')) return 'suspended'
  if (s.includes('libre') || s.includes('bye') || s.includes('free') || s.includes('descansa')) return 'bye'
  if (s.includes('pospuest') || s.includes('postpon')) return 'postponed'
  if (s.includes('cancel')) return 'cancelled'
  if (s.includes('final') || s.includes('jugad') || s.includes('complet') || s.includes('finish')) return 'finished'
  return estado.trim() || 'finished'
}

function isByeStatus(status: string): boolean {
  const s = status.trim().toLowerCase()
  return s === 'bye' || s === 'libre' || s === 'free' || s === 'rest'
}

function isSuspendedStatus(status: string): boolean {
  const s = status.trim().toLowerCase()
  return s === 'suspended' || s === 'postponed' || s.includes('suspend')
}

/**
 * CSV columns (header required):
 * fecha, division, Equipo 1, goles equipo 1, equipo 2, goles equipo 2, estado
 */
export function parseMatchResultsCsv(text: string): JsonDivisionRoundBlock[] {
  const raw = text.replace(/^\uFEFF/, '').replace(/\r\n/g, '\n').replace(/\r/g, '\n')
  const lines = raw.split('\n').map((l) => l.trim()).filter((l) => l.length > 0)
  if (lines.length < 2) {
    throw new Error('El CSV está vacío o solo tiene encabezado.')
  }

  const headers = splitCsvLine(lines[0])
  const iRound = findCol(headers, COL.round)
  const iDivision = findCol(headers, COL.division)
  const iHome = findCol(headers, COL.home)
  const iHomeGoals = findCol(headers, COL.homeGoals)
  const iAway = findCol(headers, COL.away)
  const iAwayGoals = findCol(headers, COL.awayGoals)
  const iStatus = findCol(headers, COL.status)

  if (iDivision < 0 || iHome < 0 || iAway < 0) {
    throw new Error(
      'El CSV necesita columnas: fecha, division, Equipo 1, goles equipo 1, equipo 2, goles equipo 2, estado.'
    )
  }

  type Acc = {
    division: string
    round: number
    matches: JsonMatchResultRow[]
    skippedByes: string[]
    skippedOther: string[]
  }
  const groups = new Map<string, Acc>()

  for (let r = 1; r < lines.length; r++) {
    const cells = splitCsvLine(lines[r])
    const division = (cells[iDivision] ?? '').trim()
    if (!division) {
      continue
    }
    const roundRaw = iRound >= 0 ? Number(cells[iRound] ?? 1) : 1
    const round = Number.isFinite(roundRaw) && roundRaw > 0 ? roundRaw : 1
    const homeTeam = (cells[iHome] ?? '').trim()
    const awayTeam = (cells[iAway] ?? '').trim()
    const homeScore = iHomeGoals >= 0 ? parseScore(cells[iHomeGoals] ?? '') : null
    const awayScore = iAwayGoals >= 0 ? parseScore(cells[iAwayGoals] ?? '') : null
    const status = mapEstadoToStatus(iStatus >= 0 ? (cells[iStatus] ?? '') : 'Finalizado')
    const indexLabel = `${division} fila ${r + 1}`

    const key = `${normalizeTeamName(division)}||${round}`
    if (!groups.has(key)) {
      groups.set(key, {
        division,
        round,
        matches: [],
        skippedByes: [],
        skippedOther: [],
      })
    }
    const group = groups.get(key)!

    if (isByeStatus(status) || (!homeTeam && awayTeam) || (homeTeam && !awayTeam)) {
      group.skippedByes.push(`${indexLabel}: libre — ${homeTeam || awayTeam || '(sin nombre)'}`)
      continue
    }

    if (!homeTeam || !awayTeam) {
      group.skippedOther.push(`${indexLabel}: sin local/visitante (omitido).`)
      continue
    }

    group.matches.push({
      homeTeam,
      awayTeam,
      homeScore,
      awayScore,
      status: isSuspendedStatus(status) ? 'suspended' : status,
    })
  }

  const blocks = [...groups.values()]
  if (blocks.length === 0) {
    throw new Error('No se encontraron filas de partidos en el CSV.')
  }
  return blocks
}

/** @deprecated Use parseMatchResultsCsv */
export function parseMatchResultsJson(text: string): JsonDivisionRoundBlock[] {
  const trimmed = text.trim()
  if (trimmed.startsWith('[') || trimmed.startsWith('{')) {
    throw new Error('Este importador ahora usa CSV. Descargá/exportá el archivo .csv (fecha, division, Equipo 1, …).')
  }
  return parseMatchResultsCsv(text)
}

export function matchDivisionName(
  jsonName: string,
  divisions: Array<{ id: string; name: string }>
): { divisionId: string; name: string; score: number } | null {
  const norm = normalizeTeamName(jsonName)
  if (!norm || divisions.length === 0) return null

  let best: { divisionId: string; name: string; score: number } | null = null
  for (const d of divisions) {
    const dNorm = normalizeTeamName(d.name)
    const stripped = normalizeTeamName(d.name.replace(/divisi[oó]n/gi, ' ').replace(/categor[ií]a/gi, ' '))
    let score = Math.max(nameSimilarity(norm, dNorm), nameSimilarity(norm, stripped))

    if (dNorm === norm || stripped === norm) score = 1
    else if (norm.length <= 2) {
      const tokens = new Set([...dNorm.split(' '), ...stripped.split(' ')].filter(Boolean))
      if (tokens.has(norm)) score = 1
    } else if (dNorm.includes(norm) || norm.includes(dNorm) || stripped.includes(norm) || norm.includes(stripped)) {
      score = Math.max(score, 0.92)
    } else {
      // Token overlap e.g. "45 ZONA A" vs "45 Zona A"
      const a = new Set(norm.split(' ').filter(Boolean))
      const b = new Set(dNorm.split(' ').filter(Boolean))
      let inter = 0
      for (const t of a) if (b.has(t)) inter++
      const union = new Set([...a, ...b]).size
      if (union > 0) score = Math.max(score, inter / union)
    }

    if (!best || score > best.score) best = { divisionId: d.id, name: d.name, score }
  }

  // When matching against a single scoped division, accept a lower bar.
  const threshold = divisions.length === 1 ? 0.45 : 0.65
  if (best && best.score >= threshold) return best
  return null
}

export function mapTeamNamesForDivision(
  names: string[],
  candidates: TeamMatchCandidate[]
) {
  return matchCsvNamesToTeams(names, candidates)
}

export { mappingsNeedReview }
