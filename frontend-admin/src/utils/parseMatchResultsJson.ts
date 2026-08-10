import { matchCsvNamesToTeams, mappingsNeedReview, type TeamMatchCandidate } from './teamNameMatch'
import { normalizeTeamName, nameSimilarity } from './teamNameMatch'

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
  /** Informational: byes / libres skipped from the JSON. */
  skippedByes: string[]
  /** Informational: rows skipped for other reasons. */
  skippedOther: string[]
}

function isByeStatus(status: string): boolean {
  const s = status.trim().toLowerCase()
  return s === 'bye' || s === 'libre' || s === 'free' || s === 'rest'
}

function isSuspendedStatus(status: string): boolean {
  const s = status.trim().toLowerCase()
  return s === 'suspended' || s === 'postponed' || s === 'suspended_match'
}

function teamLabel(value: unknown): string {
  if (value == null) return ''
  return String(value).trim()
}

function parseScore(value: unknown): number | null {
  if (value == null || value === '') return null
  const n = Number(value)
  return Number.isFinite(n) ? n : null
}

export function parseMatchResultsJson(text: string): JsonDivisionRoundBlock[] {
  const data = JSON.parse(text) as unknown
  if (!Array.isArray(data)) {
    throw new Error('El JSON debe ser un array de divisiones/fechas.')
  }

  return data.map((block, idx) => {
    if (!block || typeof block !== 'object') {
      throw new Error(`Bloque inválido en índice ${idx}.`)
    }
    const o = block as Record<string, unknown>
    const division = String(o.division ?? '').trim()
    if (!division) throw new Error(`Falta "division" en el bloque ${idx + 1}.`)
    const round = Number(o.round ?? 1)
    const matchesRaw = o.matches
    if (!Array.isArray(matchesRaw) || matchesRaw.length === 0) {
      throw new Error(`Sin partidos en división "${division}".`)
    }

    const matches: JsonMatchResultRow[] = []
    const skippedByes: string[] = []
    const skippedOther: string[] = []

    matchesRaw.forEach((m, j) => {
      const row = (m ?? {}) as Record<string, unknown>
      const status = String(row.status ?? 'finished')
      const homeTeam = teamLabel(row.homeTeam)
      const awayTeam = teamLabel(row.awayTeam)
      const homeScore = parseScore(row.homeScore)
      const awayScore = parseScore(row.awayScore)
      const indexLabel = `${division} #${j + 1}`

      // Libre / bye (equipos impares): un solo equipo, sin rival.
      if (isByeStatus(status) || (!homeTeam && awayTeam) || (homeTeam && !awayTeam)) {
        const who = homeTeam || awayTeam || '(sin nombre)'
        skippedByes.push(`${indexLabel}: libre — ${who}`)
        return
      }

      if (!homeTeam || !awayTeam) {
        skippedOther.push(`${indexLabel}: partido sin local/visitante (omitido).`)
        return
      }

      // Suspendidos / sin marcador: se importan igual (backend → POSTPONED).
      if (isSuspendedStatus(status) || (homeScore == null && awayScore == null && status.toLowerCase() !== 'finished')) {
        matches.push({
          homeTeam,
          awayTeam,
          homeScore,
          awayScore,
          status: isSuspendedStatus(status) ? status : status || 'suspended',
        })
        return
      }

      matches.push({
        homeTeam,
        awayTeam,
        homeScore,
        awayScore,
        status,
      })
    })

    if (matches.length === 0 && skippedByes.length === 0 && skippedOther.length === 0) {
      throw new Error(`Sin partidos en división "${division}".`)
    }

    return {
      competition: o.competition != null ? String(o.competition) : undefined,
      division,
      round: Number.isFinite(round) ? round : 1,
      matches,
      skippedByes,
      skippedOther,
    }
  })
}

export function matchDivisionName(
  jsonName: string,
  divisions: Array<{ id: string; name: string }>
): { divisionId: string; name: string; score: number } | null {
  const norm = normalizeTeamName(jsonName)
  let best: { divisionId: string; name: string; score: number } | null = null
  for (const d of divisions) {
    const score = Math.max(
      nameSimilarity(norm, normalizeTeamName(d.name)),
      nameSimilarity(norm, normalizeTeamName(d.name.replace(/divisi[oó]n/gi, '').trim()))
    )
    // Exact letter match e.g. "A" vs "Division A" / "A"
    const letters = normalizeTeamName(d.name).split(' ')
    if (letters.includes(norm) || normalizeTeamName(d.name) === norm) {
      return { divisionId: d.id, name: d.name, score: 1 }
    }
    if (!best || score > best.score) best = { divisionId: d.id, name: d.name, score }
  }
  if (best && best.score >= 0.72) return best
  return null
}

export function mapTeamNamesForDivision(
  names: string[],
  candidates: TeamMatchCandidate[]
) {
  return matchCsvNamesToTeams(names, candidates)
}

export { mappingsNeedReview }
