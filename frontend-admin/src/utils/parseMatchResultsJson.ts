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
    const matches: JsonMatchResultRow[] = matchesRaw.map((m, j) => {
      const row = m as Record<string, unknown>
      const homeTeam = String(row.homeTeam ?? '').trim()
      const awayTeam = String(row.awayTeam ?? '').trim()
      if (!homeTeam || !awayTeam) {
        throw new Error(`Partido incompleto en ${division} #${j + 1}.`)
      }
      return {
        homeTeam,
        awayTeam,
        homeScore: row.homeScore == null || row.homeScore === '' ? null : Number(row.homeScore),
        awayScore: row.awayScore == null || row.awayScore === '' ? null : Number(row.awayScore),
        status: String(row.status ?? 'finished'),
      }
    })
    return {
      competition: o.competition != null ? String(o.competition) : undefined,
      division,
      round: Number.isFinite(round) ? round : 1,
      matches,
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
