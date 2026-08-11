import { splitCsvLine } from './parseTeamCsv'
import type { ImportPlayerItem, PlayerPosition } from '../api/players'

const FIRST_ALIASES = new Set(['nombre', 'name', 'firstname', 'first_name', 'primer nombre'])
const LAST_ALIASES = new Set(['apellido', 'lastname', 'last_name', 'surname'])
const NICK_ALIASES = new Set(['apodo', 'nickname', 'nick', 'alias'])
const DOC_ALIASES = new Set(['dni', 'document', 'documento', 'cedula', 'cédula'])
const POS_ALIASES = new Set(['posicion', 'posición', 'position', 'pos'])

function normalizeHeader(value: string) {
  return value.trim().toLowerCase()
}

function mapPosition(raw: string): PlayerPosition | undefined {
  const v = raw.trim().toLowerCase()
  if (!v) return undefined
  if (['gk', 'arquero', 'portero', 'arquera'].includes(v)) return 'GK'
  if (['def', 'defensor', 'defensa', 'defender'].includes(v)) return 'DEF'
  if (['mid', 'mediocampista', 'volante', 'medio'].includes(v)) return 'MID'
  if (['fwd', 'delantero', 'delantera', 'forward', 'atacante'].includes(v)) return 'FWD'
  return undefined
}

/** CSV expected: nombre,apellido[,apodo][,dni][,posicion] — header optional. */
export function parsePlayersFromCsv(text: string): ImportPlayerItem[] {
  const raw = text.replace(/^\uFEFF/, '').replace(/\r\n/g, '\n').replace(/\r/g, '\n')
  const lines = raw.split('\n').map((l) => l.trim()).filter((l) => l.length > 0)
  if (lines.length === 0) return []

  const first = splitCsvLine(lines[0]).map(normalizeHeader)
  const firstIdx = first.findIndex((c) => FIRST_ALIASES.has(c))
  const lastIdx = first.findIndex((c) => LAST_ALIASES.has(c))
  const hasHeader = firstIdx >= 0 && lastIdx >= 0

  let iFirst = 0
  let iLast = 1
  let iNick = 2
  let iDoc = 3
  let iPos = 4
  let dataLines = lines

  if (hasHeader) {
    iFirst = firstIdx
    iLast = lastIdx
    iNick = first.findIndex((c) => NICK_ALIASES.has(c))
    iDoc = first.findIndex((c) => DOC_ALIASES.has(c))
    iPos = first.findIndex((c) => POS_ALIASES.has(c))
    dataLines = lines.slice(1)
  }

  const players: ImportPlayerItem[] = []
  for (const line of dataLines) {
    const cells = splitCsvLine(line)
    const firstName = (cells[iFirst] ?? '').trim()
    const lastName = (cells[iLast] ?? '').trim()
    if (!firstName || !lastName) continue

    const nickname = iNick >= 0 ? (cells[iNick] ?? '').trim() : (cells[2] ?? '').trim()
    const document = iDoc >= 0 ? (cells[iDoc] ?? '').trim() : (hasHeader ? '' : (cells[3] ?? '').trim())
    const posRaw = iPos >= 0 ? (cells[iPos] ?? '').trim() : (hasHeader ? '' : (cells[4] ?? '').trim())

    players.push({
      firstName,
      lastName,
      ...(nickname ? { nickname } : {}),
      ...(document ? { document } : {}),
      ...(mapPosition(posRaw) ? { position: mapPosition(posRaw) } : {}),
    })
  }

  return players
}
