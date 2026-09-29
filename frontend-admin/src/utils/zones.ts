/**
 * Zones of the same category are separate divisions whose names only differ by the trailing
 * "Zona X" ("Sub 13 Zona A", "Sub 13 - Zona B"). Keep in sync with backend DivisionCategory.cs.
 */
const ZONE_SUFFIX = /\s*[-—–]?\s*zona\s+\S+\s*$/i

export function isZone(divisionName: string): boolean {
  return ZONE_SUFFIX.test(divisionName)
}

export function categoryKey(divisionName: string): string {
  return divisionName.replace(ZONE_SUFFIX, '').trim().toLowerCase()
}

/** The division itself plus the other zones of its category (just itself when it is not a zone). */
export function zoneGroupIds(divisions: Array<{ id: string; name: string }>, divisionId: string): string[] {
  const division = divisions.find((d) => d.id === divisionId)
  if (!division) return []
  if (!isZone(division.name)) return [division.id]
  const key = categoryKey(division.name)
  return divisions.filter((d) => isZone(d.name) && categoryKey(d.name) === key).map((d) => d.id)
}

/**
 * Teams an import for `divisionId` may reference: its own teams first, then the teams of its sibling
 * zones (interzonal matches).
 */
export function zoneGroupTeams<T extends { id: string }>(
  setupDivisions: Array<{ divisionId: string; divisionName: string; teams: T[] }>,
  divisionId: string,
): T[] {
  const groupIds = zoneGroupIds(
    setupDivisions.map((d) => ({ id: d.divisionId, name: d.divisionName })),
    divisionId,
  )
  const ordered = [divisionId, ...groupIds.filter((id) => id !== divisionId)]
  const seen = new Set<string>()
  const teams: T[] = []
  for (const id of ordered) {
    for (const team of setupDivisions.find((d) => d.divisionId === id)?.teams ?? []) {
      if (seen.has(team.id)) continue
      seen.add(team.id)
      teams.push(team)
    }
  }
  return teams
}
