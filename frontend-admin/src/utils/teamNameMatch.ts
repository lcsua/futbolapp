export type TeamMatchCandidate = {
  id: string
  name: string
  displayName?: string | null
  shortName?: string | null
  suffix?: string | null
}

export type TeamMatchAction = 'match' | 'create'

export type TeamCsvRowMapping = {
  csvName: string
  normalizedCsv: string
  action: TeamMatchAction
  /** Selected existing team when action is match */
  teamId: string | null
  /** Best similarity score seen (0–1) */
  score: number
  /** Suggested candidates for the UI dropdown */
  candidates: Array<{ teamId: string; label: string; score: number }>
  /** True when the user should review this row before applying */
  needsReview: boolean
  reason: 'exact' | 'high' | 'fuzzy' | 'ambiguous' | 'none'
}

/** Word-ish token boundary: abbreviations like Bº don't always allow \b after the symbol. */
const ABBREVIATIONS: Array<[RegExp, string]> = [
  [/(^|[\s/])B[º°O]\.?(?=[\s/]|$)/gi, '$1BARRIO'],
  [/(^|[\s/])BARRIO(?=[\s/]|$)/gi, '$1BARRIO'],
  [/(^|[\s/])ATL\.?(?=[\s/]|$)/gi, '$1ATLETICO'],
  [/(^|[\s/])ATLETICO(?=[\s/]|$)/gi, '$1ATLETICO'],
  [/(^|[\s/])DEF\.?(?=[\s/]|$)/gi, '$1DEFENSORES'],
  [/(^|[\s/])DEFENSORES?(?=[\s/]|$)/gi, '$1DEFENSORES'],
  [/(^|[\s/])DEP\.?(?=[\s/]|$)/gi, '$1DEPORTIVO'],
  [/(^|[\s/])DEPORTIVO(?=[\s/]|$)/gi, '$1DEPORTIVO'],
  [/(^|[\s/])STO\.?(?=[\s/]|$)/gi, '$1SANTO'],
  [/(^|[\s/])ST\.?(?=[\s/]|$)/gi, '$1SANTO'],
  [/(^|[\s/])SANTO(?=[\s/]|$)/gi, '$1SANTO'],
  [/(^|[\s/])F\.?\s*C\.?(?=[\s/]|$)/gi, '$1FC'],
  [/(^|[\s/])C\.?\s*A\.?(?=[\s/]|$)/gi, '$1CA'],
  [/(^|[\s/])LAS\s+PTS(?=[\s/]|$)/gi, '$1LAS PIEDRAS'],
  [/(^|[\s/])PTS(?=[\s/]|$)/gi, '$1PIEDRAS'],
]

export function normalizeTeamName(input: string): string {
  let s = input.trim().toUpperCase()
  s = s.normalize('NFD').replace(/\p{M}/gu, '')
  for (const [re, repl] of ABBREVIATIONS) {
    s = s.replace(re, repl)
  }
  s = s.replace(/['"”“´`]/g, '')
  s = s.replace(/[^A-Z0-9\s]/g, ' ')
  s = s.replace(/\s+/g, ' ').trim()
  return s
}

function levenshtein(a: string, b: string): number {
  if (a === b) return 0
  if (!a.length) return b.length
  if (!b.length) return a.length
  const prev = new Array<number>(b.length + 1)
  const curr = new Array<number>(b.length + 1)
  for (let j = 0; j <= b.length; j++) prev[j] = j
  for (let i = 1; i <= a.length; i++) {
    curr[0] = i
    for (let j = 1; j <= b.length; j++) {
      const cost = a[i - 1] === b[j - 1] ? 0 : 1
      curr[j] = Math.min(curr[j - 1] + 1, prev[j] + 1, prev[j - 1] + cost)
    }
    for (let j = 0; j <= b.length; j++) prev[j] = curr[j]
  }
  return prev[b.length]
}

/** Similarity ratio in [0, 1]. */
export function nameSimilarity(a: string, b: string): number {
  if (!a && !b) return 1
  if (!a || !b) return 0
  if (a === b) return 1
  const dist = levenshtein(a, b)
  const maxLen = Math.max(a.length, b.length)
  return 1 - dist / maxLen
}

function teamLabels(team: TeamMatchCandidate): string[] {
  const labels = [team.name, team.displayName, team.shortName]
    .filter((x): x is string => !!x && x.trim().length > 0)
  if (team.suffix) {
    labels.push(`${team.name} ${team.suffix}`)
    if (team.displayName) labels.push(`${team.displayName}`)
  }
  return [...new Set(labels)]
}

function formatTeamLabel(team: TeamMatchCandidate): string {
  const base = team.displayName || team.name
  return team.suffix ? `${base} (${team.suffix})` : base
}

const HIGH = 0.9
const FUZZY = 0.72
const MARGIN = 0.06

export function matchCsvNamesToTeams(
  csvNames: string[],
  teams: TeamMatchCandidate[],
  options?: {
    alreadyAssignedIds?: Set<string>
    /** normalized CSV alias → teamId (must belong to `teams` to apply). */
    aliasByNormalized?: Map<string, string>
  }
): TeamCsvRowMapping[] {
  const assigned = options?.alreadyAssignedIds ?? new Set<string>()
  const aliasByNormalized = options?.aliasByNormalized
  const teamIdSet = new Set(teams.map((t) => t.id))
  const indexed = teams.map((team) => ({
    team,
    norms: teamLabels(team).map(normalizeTeamName).filter(Boolean),
  }))

  const usedTeamIds = new Set<string>()
  const rows: TeamCsvRowMapping[] = []

  for (const csvName of csvNames) {
    const normalizedCsv = normalizeTeamName(csvName)
    const aliasedTeamId = aliasByNormalized?.get(normalizedCsv)
    if (aliasedTeamId && teamIdSet.has(aliasedTeamId)) {
      const team = teams.find((t) => t.id === aliasedTeamId)!
      usedTeamIds.add(aliasedTeamId)
      rows.push({
        csvName,
        normalizedCsv,
        action: 'match',
        teamId: aliasedTeamId,
        score: 1,
        candidates: [{ teamId: aliasedTeamId, label: formatTeamLabel(team), score: 1 }],
        needsReview: assigned.has(aliasedTeamId),
        reason: 'exact',
      })
      continue
    }

    const scored: Array<{ teamId: string; label: string; score: number }> = []

    for (const { team, norms } of indexed) {
      let best = 0
      for (const n of norms) {
        best = Math.max(best, nameSimilarity(normalizedCsv, n))
      }
      if (best > 0) {
        scored.push({ teamId: team.id, label: formatTeamLabel(team), score: best })
      }
    }

    scored.sort((a, b) => b.score - a.score || a.label.localeCompare(b.label, 'es'))
    const candidates = scored.slice(0, 8)
    const top = candidates[0]
    const second = candidates[1]

    let action: TeamMatchAction = 'create'
    let teamId: string | null = null
    let needsReview = false
    let reason: TeamCsvRowMapping['reason'] = 'none'
    let score = top?.score ?? 0

    if (top && top.score >= 0.999) {
      action = 'match'
      teamId = top.teamId
      reason = 'exact'
      needsReview = false
    } else if (
      top &&
      top.score >= HIGH &&
      (!second || top.score - second.score >= MARGIN) &&
      !usedTeamIds.has(top.teamId)
    ) {
      action = 'match'
      teamId = top.teamId
      reason = 'high'
      needsReview = false
    } else if (top && top.score >= FUZZY) {
      action = 'match'
      teamId = top.teamId
      reason = second && top.score - second.score < MARGIN ? 'ambiguous' : 'fuzzy'
      needsReview = true
    } else {
      action = 'create'
      teamId = null
      reason = 'none'
      // No ambiguous existing match — create without forcing review alone.
      needsReview = false
    }

    if (action === 'match' && teamId && assigned.has(teamId)) {
      // Already in another division this season: force review so user can create/remap.
      needsReview = true
      reason = reason === 'exact' || reason === 'high' ? 'fuzzy' : reason
    }

    if (action === 'match' && teamId) {
      if (usedTeamIds.has(teamId) && reason !== 'exact') {
        needsReview = true
        reason = 'ambiguous'
      }
      usedTeamIds.add(teamId)
    }

    rows.push({
      csvName,
      normalizedCsv,
      action,
      teamId,
      score,
      candidates,
      needsReview,
      reason,
    })
  }

  return rows
}

/** Items suitable for POST team-name-aliases from confirmed match mappings. */
export function aliasUpsertsFromMappings(
  mappings: Array<{ csvName: string; action: TeamMatchAction; teamId: string | null }>
): Array<{ teamId: string; alias: string }> {
  const seen = new Set<string>()
  const items: Array<{ teamId: string; alias: string }> = []
  for (const m of mappings) {
    if (m.action !== 'match' || !m.teamId || !m.csvName.trim()) continue
    const key = `${m.teamId}|${normalizeTeamName(m.csvName)}`
    if (seen.has(key)) continue
    seen.add(key)
    items.push({ teamId: m.teamId, alias: m.csvName.trim() })
  }
  return items
}

export function mappingsNeedReview(rows: TeamCsvRowMapping[]): boolean {
  return rows.some((r) => r.needsReview)
}
