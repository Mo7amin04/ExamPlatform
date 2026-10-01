const base = import.meta.env.BASE_URL.replace(/\/$/, '')

/** Removes the deployment base path (e.g. /ExamPlatform on GitHub Pages) so paths can be passed to the router. */
export function toAppPath(pathname: string) {
  return base && pathname.startsWith(base) ? pathname.slice(base.length) || '/' : pathname
}
