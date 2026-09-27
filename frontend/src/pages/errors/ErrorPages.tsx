import { A } from '@solidjs/router'
import { Icon } from '~/components/ui/Icon'

function ErrorLayout(props: { code: string; title: string; description: string }) {
  return (
    <div class="flex min-h-[60vh] flex-col items-center justify-center px-6 text-center">
      <div class="mb-4 flex size-14 items-center justify-center rounded-full bg-slate-100 text-slate-500">
        <Icon name="alert" class="size-7" />
      </div>
      <p class="text-sm font-semibold text-brand-600">{props.code}</p>
      <h1 class="mt-1 text-2xl font-semibold text-slate-900">{props.title}</h1>
      <p class="mt-2 max-w-md text-sm text-slate-500">{props.description}</p>
      <A href="/dashboard" class="mt-6 rounded-lg bg-brand-600 px-4 py-2 text-sm font-medium text-white hover:bg-brand-700">
        Back to dashboard
      </A>
    </div>
  )
}

export function ForbiddenPage() {
  return (
    <ErrorLayout
      code="403"
      title="Access denied"
      description="You do not have permission to view this page. Contact an administrator if you believe this is a mistake."
    />
  )
}

export function NotFoundPage() {
  return <ErrorLayout code="404" title="Page not found" description="The page you are looking for does not exist or was moved." />
}
