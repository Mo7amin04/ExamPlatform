import { A, useLocation, useNavigate } from '@solidjs/router'
import { createEffect, createSignal, For, Show, type ParentProps } from 'solid-js'
import { Icon, type IconName } from '~/components/ui/Icon'
import { auth } from '~/stores/auth.store'
import { locale, setLocale, t, type TranslationKey } from '~/utils/i18n'

interface NavItem {
  href: string
  label: TranslationKey
  icon: IconName
  match?: (path: string) => boolean
}

const navItems: NavItem[] = [
  { href: '/dashboard', label: 'nav.dashboard', icon: 'dashboard' },
  { href: '/departments', label: 'nav.departments', icon: 'building' },
  { href: '/courses', label: 'nav.courses', icon: 'book', match: (p) => p.startsWith('/courses') && !p.endsWith('/questions') },
  { href: '/questions', label: 'nav.questions', icon: 'questions', match: (p) => p.startsWith('/questions') || /^\/courses\/[^/]+\/questions/.test(p) },
  { href: '/exams', label: 'nav.exams', icon: 'exam' },
  { href: '/ai/generate', label: 'nav.ai', icon: 'sparkles' },
  { href: '/settings', label: 'nav.settings', icon: 'settings' },
]

export function DashboardLayout(props: ParentProps) {
  const location = useLocation()
  const navigate = useNavigate()
  const [mobileOpen, setMobileOpen] = createSignal(false)

  createEffect(() => {
    location.pathname
    setMobileOpen(false)
  })

  const isActive = (item: NavItem) =>
    item.match ? item.match(location.pathname) : location.pathname === item.href || location.pathname.startsWith(item.href + '/')

  const initials = () =>
    (auth.state.user?.fullName ?? '?')
      .replace(/^Dr\.?\s+/i, '')
      .split(' ')
      .map((p) => p[0])
      .slice(0, 2)
      .join('')
      .toUpperCase()

  const signOut = () => {
    auth.logout()
    navigate('/login', { replace: true })
  }

  const Sidebar = () => (
    <nav class="flex h-full flex-col" aria-label="Main navigation">
      <A href="/dashboard" class="flex items-center gap-3 px-5 py-5">
        <div class="flex size-9 items-center justify-center rounded-lg bg-brand-600 text-white">
          <Icon name="exam" class="size-5" />
        </div>
        <span class="text-base font-semibold text-slate-900">{t('app.name')}</span>
      </A>
      <ul class="flex-1 space-y-0.5 px-3">
        <For each={navItems}>
          {(item) => (
            <li>
              <A
                href={item.href}
                aria-current={isActive(item) ? 'page' : undefined}
                class={`flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors ${
                  isActive(item) ? 'bg-brand-50 text-brand-700' : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
                }`}
              >
                <Icon name={item.icon} class="size-[18px]" />
                {t(item.label)}
              </A>
            </li>
          )}
        </For>
      </ul>
      <div class="border-t border-slate-200 p-3">
        <div class="flex items-center gap-3 rounded-lg px-2 py-2">
          <div class="flex size-9 shrink-0 items-center justify-center rounded-full bg-slate-200 text-xs font-semibold text-slate-700">
            {initials()}
          </div>
          <div class="min-w-0 flex-1">
            <p class="truncate text-sm font-medium text-slate-900">{auth.state.user?.fullName}</p>
            <p class="truncate text-xs text-slate-500">{auth.state.user?.roles.join(', ')}</p>
          </div>
          <button
            type="button"
            onClick={signOut}
            class="rounded-md p-1.5 text-slate-500 hover:bg-slate-100 hover:text-slate-900"
            title={t('nav.signOut')}
            aria-label={t('nav.signOut')}
          >
            <Icon name="logout" class="size-4 rtl:rotate-180" />
          </button>
        </div>
      </div>
    </nav>
  )

  return (
    <div class="min-h-screen">
      {/* Desktop sidebar */}
      <aside class="fixed inset-y-0 start-0 z-30 hidden w-64 border-e border-slate-200 bg-white lg:block">
        <Sidebar />
      </aside>

      {/* Mobile drawer */}
      <Show when={mobileOpen()}>
        <div class="fixed inset-0 z-40 bg-slate-900/40 lg:hidden" onClick={() => setMobileOpen(false)} />
        <aside class="fixed inset-y-0 start-0 z-50 w-64 bg-white shadow-xl lg:hidden">
          <Sidebar />
        </aside>
      </Show>

      <div class="lg:ps-64">
        <header class="sticky top-0 z-20 flex h-14 items-center gap-3 border-b border-slate-200 bg-white/90 px-4 backdrop-blur sm:px-6">
          <button
            type="button"
            class="rounded-md p-1.5 text-slate-600 hover:bg-slate-100 lg:hidden"
            onClick={() => setMobileOpen(true)}
            aria-label="Open navigation"
          >
            <Icon name="menu" class="size-5" />
          </button>
          <div class="flex-1" />
          <A
            href="/ai/generate"
            class="hidden items-center gap-1.5 rounded-md px-2.5 py-1.5 text-sm font-medium text-violet-700 hover:bg-violet-50 sm:inline-flex"
          >
            <Icon name="sparkles" /> {t('action.generate')}
          </A>
          <button
            type="button"
            class="inline-flex items-center gap-1.5 rounded-md px-2.5 py-1.5 text-sm text-slate-600 hover:bg-slate-100"
            onClick={() => setLocale(locale() === 'en' ? 'ar' : 'en')}
          >
            <Icon name="globe" /> {t('common.language')}
          </button>
        </header>
        <main class="mx-auto max-w-7xl px-4 py-6 sm:px-6 lg:py-8">{props.children}</main>
      </div>
    </div>
  )
}
