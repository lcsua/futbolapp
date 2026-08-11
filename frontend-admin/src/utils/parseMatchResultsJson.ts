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
  return s === 'suspended' || s.includes('suspend')
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

function divisionAliases(name: string): string[] {
  const dNorm = normalizeTeamName(name)
  const stripped = normalizeTeamName(name.replace(/divisi[oó]n/gi, ' ').replace(/categor[ií]a/gi, ' '))
  return [...new Set([dNorm, stripped].filter(Boolean))]
}

function isShortDivisionCode(norm: string): boolean {
  // "A", "B", "C", "D1" — not "45 ZONA B"
  return /^[A-Z0-9]{1,3}$/.test(norm)
}

function tokenJaccard(a: string, b: string): number {
  const sa = new Set(a.split(' ').filter(Boolean))
  const sb = new Set(b.split(' ').filter(Boolean))
  if (sa.size === 0 || sb.size === 0) return 0
  let inter = 0
  for (const t of sa) if (sb.has(t)) inter++
  const union = new Set([...sa, ...sb]).size
  return union === 0 ? 0 : inter / union
}

function trailingDivisionMarker(norm: string): string | null {
  const parts = norm.split(' ').filter(Boolean)
  if (parts.length === 0) return null
  const last = parts[parts.length - 1]!
  // Single letter / short code at the end: A, B, C, D1, etc.
  if (/^[A-Z]$/.test(last) || /^[A-Z]\d{0,2}$/.test(last)) return last
  return null
}

/**
 * Match a CSV division label to a league division.
 * Short codes ("B") only match exact "B" / "Division B", never "45 Zona B".
 * Near-twins like "45 Zona A" vs "45 Zona B" do not fuzzily cross-match.
 */
export function matchDivisionName(
  jsonName: string,
  divisions: Array<{ id: string; name: string }>
): { divisionId: string; name: string; score: number } | null {
  const norm = normalizeTeamName(jsonName)
  if (!norm || divisions.length === 0) return null

  const csvIsShort = isShortDivisionCode(norm)
  const csvMarker = trailingDivisionMarker(norm)
  let best: { divisionId: string; name: string; score: number } | null = null
  let secondBest = 0

  for (const d of divisions) {
    const aliases = divisionAliases(d.name)
    let score = 0

    for (const alias of aliases) {
      if (alias === norm) {
        score = 1
        break
      }
    }
    if (score === 1) {
      if (!best || score > best.score) {
        secondBest = best?.score ?? 0
        best = { divisionId: d.id, name: d.name, score }
      } else if (score > secondBest) {
        secondBest = score
      }
      continue
    }

    const dNorm = aliases[0]!
    const dIsShort = aliases.some(isShortDivisionCode)

    // Never pair short code "B" with multi-word "45 ZONA B" (either direction).
    if (csvIsShort || dIsShort) {
      score = 0
    } else {
      score = Math.max(
        nameSimilarity(norm, dNorm),
        ...aliases.map((a) => nameSimilarity(norm, a)),
        tokenJaccard(norm, dNorm)
      )
      const divMarker = trailingDivisionMarker(dNorm)
      // "45 ZONA A" must not soft-match "45 ZONA B" just because they share most tokens.
      if (csvMarker && divMarker && csvMarker !== divMarker) {
        score = 0
      } else if (score < 0.85) {
        score = 0
      }
    }

    if (score > 0) {
      if (!best || score > best.score) {
        secondBest = best?.score ?? 0
        best = { divisionId: d.id, name: d.name, score }
      } else if (score > secondBest) {
        secondBest = score
      }
    }
  }

  if (!best) return null
  // Exact short-code / full-name equality.
  if (best.score >= 1) return best
  // Fuzzy: require clear winner so near-twins don't collide.
  if (best.score >= 0.85 && best.score - secondBest >= 0.05) return best
  return null
}

export function mapTeamNamesForDivision(
  names: string[],
  candidates: TeamMatchCandidate[],
  aliasByNormalized?: Map<string, string>
) {
  return matchCsvNamesToTeams(names, candidates, { aliasByNormalized })
}

export { mappingsNeedReview }
