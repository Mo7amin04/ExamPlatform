export function formatDate(value: string | null | undefined, options: Intl.DateTimeFormatOptions = { dateStyle: 'medium' }) {
  if (!value) return '—'
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? '—' : new Intl.DateTimeFormat(undefined, options).format(date)
}

export function formatDateTime(value: string | null | undefined) {
  return formatDate(value, { dateStyle: 'medium', timeStyle: 'short' })
}

export function formatPoints(points: number) {
  const n = Number.isInteger(points) ? points.toString() : points.toFixed(2).replace(/\.?0+$/, '')
  return `${n} ${points === 1 ? 'pt' : 'pts'}`
}

export function formatDuration(minutes: number) {
  if (minutes < 60) return `${minutes} min`
  const h = Math.floor(minutes / 60)
  const m = minutes % 60
  return m ? `${h} h ${m} min` : `${h} h`
}

export function truncate(text: string, max = 120) {
  return text.length > max ? `${text.slice(0, max - 1)}…` : text
}

/** Splits "MultipleChoice" into "Multiple Choice". */
export function humanize(value: string) {
  return value.replace(/([a-z])([A-Z])/g, '$1 $2')
}

/** Converts an ISO string to the value format of <input type="datetime-local"> (local time). */
export function toDateTimeLocal(value: string | null | undefined) {
  if (!value) return ''
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return ''
  const pad = (n: number) => n.toString().padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

/** Converts a datetime-local value (local time) to an ISO UTC string. */
export function fromDateTimeLocal(value: string) {
  if (!value) return null
  const d = new Date(value)
  return Number.isNaN(d.getTime()) ? null : d.toISOString()
}
