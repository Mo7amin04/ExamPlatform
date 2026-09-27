import { A } from '@solidjs/router'
import type { JSX } from 'solid-js'
import { Button } from '~/components/ui/Button'
import { Icon } from '~/components/ui/Icon'
import { ExportMenu } from '~/features/exports/ExportMenu'

/** Screen-only toolbar above printable documents. */
export function PrintToolbar(props: { examId: string; title: string; children?: JSX.Element }) {
  return (
    <div class="no-print sticky top-0 z-10 border-b border-slate-200 bg-white/95 backdrop-blur">
      <div class="mx-auto flex max-w-5xl flex-wrap items-center gap-2 px-4 py-3">
        <A href={`/exams/${props.examId}/edit`} class="inline-flex items-center gap-1.5 rounded-md px-2 py-1.5 text-sm text-slate-600 hover:bg-slate-100">
          <Icon name="chevronLeft" class="size-4 rtl:rotate-180" /> Back to builder
        </A>
        <span class="mx-2 hidden h-5 w-px bg-slate-200 sm:block" />
        <span class="me-auto truncate text-sm font-semibold text-slate-900">{props.title}</span>
        {props.children}
        <ExportMenu examId={props.examId} />
        <Button icon="printer" onClick={() => window.print()}>Print</Button>
      </div>
    </div>
  )
}
