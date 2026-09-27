import { For, Show, type JSX } from 'solid-js'
import type { Pagination as PaginationMeta } from '~/types/api'
import { IconButton } from '~/components/ui/Button'

export interface Column<T> {
  header: string
  cell: (row: T) => JSX.Element
  class?: string
  headerClass?: string
}

/** Simple, accessible data table. Loading/empty/error states are handled by the caller. */
export function DataTable<T>(props: { columns: Column<T>[]; rows: T[]; rowKey: (row: T) => string; onRowClick?: (row: T) => void }) {
  return (
    <div class="overflow-x-auto">
      <table class="min-w-full divide-y divide-slate-200 text-sm">
        <thead class="bg-slate-50">
          <tr>
            <For each={props.columns}>
              {(col) => (
                <th scope="col" class={`px-4 py-2.5 text-start text-xs font-semibold uppercase tracking-wide text-slate-500 ${col.headerClass ?? ''}`}>
                  {col.header}
                </th>
              )}
            </For>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-100 bg-white">
          <For each={props.rows}>
            {(row) => (
              <tr
                data-key={props.rowKey(row)}
                class={props.onRowClick ? 'cursor-pointer hover:bg-slate-50' : 'hover:bg-slate-50/60'}
                onClick={() => props.onRowClick?.(row)}
              >
                <For each={props.columns}>{(col) => <td class={`px-4 py-3 align-top ${col.class ?? ''}`}>{col.cell(row)}</td>}</For>
              </tr>
            )}
          </For>
        </tbody>
      </table>
    </div>
  )
}

export function Pagination(props: { pagination: PaginationMeta; onPageChange: (page: number) => void }) {
  const from = () => (props.pagination.totalCount === 0 ? 0 : (props.pagination.page - 1) * props.pagination.pageSize + 1)
  const to = () => Math.min(props.pagination.page * props.pagination.pageSize, props.pagination.totalCount)
  return (
    <div class="flex items-center justify-between gap-3 border-t border-slate-100 px-4 py-3 text-sm text-slate-600">
      <span>
        <Show when={props.pagination.totalCount > 0} fallback="No results">
          Showing <strong>{from()}</strong>–<strong>{to()}</strong> of <strong>{props.pagination.totalCount}</strong>
        </Show>
      </span>
      <div class="flex items-center gap-1">
        <IconButton
          icon="chevronLeft"
          label="Previous page"
          class="rtl:rotate-180"
          disabled={props.pagination.page <= 1}
          onClick={() => props.onPageChange(props.pagination.page - 1)}
        />
        <span class="px-2 tabular-nums">
          {props.pagination.page} / {Math.max(1, props.pagination.totalPages)}
        </span>
        <IconButton
          icon="chevronRight"
          label="Next page"
          class="rtl:rotate-180"
          disabled={props.pagination.page >= props.pagination.totalPages}
          onClick={() => props.onPageChange(props.pagination.page + 1)}
        />
      </div>
    </div>
  )
}
