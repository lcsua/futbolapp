const HEADER_ZONE =
  /^(?:categor[ií]a\s+)?(.+?)\s*[—–-]\s*(sin\s+zona|zona\s+[a-záéíóúüñ0-9]+)\s*(?:\((\d+)\s*equipos?\))?\s*$/i
const HEADER_WITH_COUNT =
  /^(?:categor[ií]a\s+)?(.+?)\s*[—–-]\s*(\d+)\s*equipos?\s*$/i
const HEADER_CATEGORIA_ONLY = /^categor[ií]a\s+(.+)$/i
const QUOTED_SUFFIX = /^(.+?)\s*[“”"']([^“”"']{1,10})[“”"']\s*$/
const LETTER_SUFFIX = /^(.+?)\s+([A-Da-d])$/
const COLOR_SUFFIXES = ['Negro', 'Rojo', 'Blanco', 'Azul', 'Verde', 'Amarillo', 'Naranja']
const COLOR_SUFFIX = new RegExp(`^(.+?)\\s+(${COLOR_SUFFIXES.join('|')})$`, 'i')

export type ParsedRosterTeam = {
  raw: string
  name: string
  suffix: string | null
}

export type ParsedRosterCategory = {
  rawHeader: string
  categoryLabel: string
  divisionName: string
  shortYear: string | null
  expectedCount: number | null
  teams: ParsedRosterTeam[]
}

export type ParseCategoryRostersResult = {
  categories: ParsedRosterCategory[]
  warnings: string[]
}

export function normalizeKey(value: string) {
  return value
    .normalize('NFD')
    .replace(/\p{M}/gu, '')
    .toLowerCase()
    .replace(/^categoria\s+/i, '')
    .replace(/\s+/g, ' ')
    .trim()
}

export function shortYearLabel(categoryLabel: string): string | null {
  const match = categoryLabel.match(/(\d{2,4})\s*\/\s*(\d{2,4})/)
  if (!match) return null
  return `${match[1].slice(-2)}/${match[2].slice(-2)}`
}

export function importedTeamName(clubName: string, shortYear: string | null) {
  if (!shortYear) return clubName
  if (normalizeKey(clubName).includes(normalizeKey(shortYear))) return clubName
  return `${clubName} ${shortYear}`
}

export function publicTeamName(clubName: string, suffix: string | null) {
  return suffix ? `${clubName} ${suffix}` : clubName
}

function cleanClubName(name: string) {
  return name.replace(/\.+$/g, '').trim()
}

export function parseTeamLine(line: string): ParsedRosterTeam {
  const raw = line.trim()
  const quoted = raw.match(QUOTED_SUFFIX)
  if (quoted) {
    return { raw, name: cleanClubName(quoted[1]), suffix: quoted[2].trim() }
  }

  const color = raw.match(COLOR_SUFFIX)
  if (color) {
    const suffix = color[2].charAt(0).toUpperCase() + color[2].slice(1).toLowerCase()
    return { raw, name: cleanClubName(color[1]), suffix }
  }

  const letter = raw.match(LETTER_SUFFIX)
  if (letter) {
    return { raw, name: cleanClubName(letter[1]), suffix: letter[2].toUpperCase() }
  }

  return { raw, name: cleanClubName(raw), suffix: null }
}

function formatZone(zoneRaw: string) {
  const trimmed = zoneRaw.trim()
  if (/^sin\s+zona$/i.test(trimmed)) return null
  const zona = trimmed.match(/^zona\s+(.+)$/i)
  if (zona) {
    const token = zona[1].trim()
    const pretty = token.length <= 2 ? token.toUpperCase() : token
    return `Zona ${pretty}`
  }
  return trimmed
}

function parseCategoryHeader(
  line: string
): { categoryLabel: string; divisionName: string; expectedCount: number | null } | null {
  const withZone = line.match(HEADER_ZONE)
  if (withZone) {
    const categoryLabel = withZone[1].trim()
    const zone = formatZone(withZone[2])
    return {
      categoryLabel,
      divisionName: zone ? `${categoryLabel} ${zone}` : categoryLabel,
      expectedCount: withZone[3] ? Number(withZone[3]) : null,
    }
  }

  const withCount = line.match(HEADER_WITH_COUNT)
  if (withCount) {
    const categoryLabel = withCount[1].trim()
    return {
      categoryLabel,
      divisionName: categoryLabel,
      expectedCount: Number(withCount[2]),
    }
  }

  const categoriaOnly = line.match(HEADER_CATEGORIA_ONLY)
  if (categoriaOnly) {
    const categoryLabel = categoriaOnly[1].trim()
    return {
      categoryLabel,
      divisionName: categoryLabel,
      expectedCount: null,
    }
  }

  return null
}

function pushCurrent(
  categories: ParsedRosterCategory[],
  current: ParsedRosterCategory | null,
  warnings: string[]
) {
  if (!current) return
  if (current.expectedCount != null && current.expectedCount !== current.teams.length) {
    warnings.push(
      `${current.divisionName}: el encabezado dice ${current.expectedCount} equipo(s) y se leyeron ${current.teams.length}.`
    )
  }
  categories.push(current)
}

export function parseCategoryRosters(text: string): ParseCategoryRostersResult {
  const warnings: string[] = []
  const categories: ParsedRosterCategory[] = []
  let current: ParsedRosterCategory | null = null

  for (const rawLine of text.split(/\r?\n/)) {
    const line = rawLine.trim()
    if (!line) continue

    const header = parseCategoryHeader(line)
    if (header) {
      pushCurrent(categories, current, warnings)
      current = {
        rawHeader: line,
        categoryLabel: header.categoryLabel,
        divisionName: header.divisionName,
        shortYear: shortYearLabel(header.categoryLabel),
        expectedCount: header.expectedCount,
        teams: [],
      }
      continue
    }

    if (!current) {
      warnings.push(`Línea ignorada (falta un encabezado de categoría): "${line}"`)
      continue
    }

    const team = parseTeamLine(line)
    if (!team.name) continue
    if (team.suffix && team.suffix.length > 10) {
      current.teams.push({ raw: team.raw, name: team.raw, suffix: null })
      continue
    }
    current.teams.push(team)
  }

  pushCurrent(categories, current, warnings)
  return { categories, warnings }
}

export function findMatchingDivision<T extends { name: string }>(
  divisionName: string,
  divisions: T[]
): T | undefined {
  const want = normalizeKey(divisionName)
  return divisions.find((d) => normalizeKey(d.name) === want)
}

export function teamIdentityKey(team: Pick<ParsedRosterTeam, 'name' | 'suffix'>) {
  return `${normalizeKey(team.name)}::${normalizeKey(team.suffix ?? '')}`
}

export function formatTeamIdentity(team: Pick<ParsedRosterTeam, 'name' | 'suffix'>) {
  return team.suffix ? `${team.name} ${team.suffix}` : team.name
}

/** Same club+suffix twice in one category/zone. Same club in different categories is OK. */
export function findIntraCategoryDuplicates(categories: ParsedRosterCategory[]): string[] {
  const messages: string[] = []
  for (const category of categories) {
    const counts = new Map<string, { label: string; count: number }>()
    for (const team of category.teams) {
      const key = teamIdentityKey(team)
      const current = counts.get(key)
      if (current) current.count += 1
      else counts.set(key, { label: formatTeamIdentity(team), count: 1 })
    }
    for (const { label, count } of counts.values()) {
      if (count > 1) {
        messages.push(`${category.divisionName}: "${label}" aparece ${count} veces en la misma categoría.`)
      }
    }
  }
  return messages
}

export function findDuplicateCategoryNames(categories: ParsedRosterCategory[]): string[] {
  const counts = new Map<string, { label: string; count: number }>()
  for (const category of categories) {
    const key = normalizeKey(category.divisionName)
    const current = counts.get(key)
    if (current) current.count += 1
    else counts.set(key, { label: category.divisionName, count: 1 })
  }
  return [...counts.values()]
    .filter((item) => item.count > 1)
    .map((item) => `La categoría "${item.label}" aparece ${item.count} veces.`)
}
