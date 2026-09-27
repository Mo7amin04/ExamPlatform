import { unwrap } from 'solid-js/store'

/** Deep-copies plain data, including values held in Solid stores (whose proxies structuredClone rejects). */
export function deepClone<T>(value: T): T {
  return structuredClone(unwrap(value))
}
