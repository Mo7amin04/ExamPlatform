import { A, useLocation, useNavigate } from '@solidjs/router'
import { createEffect, createSignal, For, Show, type ParentProps } from 'solid-js'
import { Button, IconButton } from '~/components/ui/Button'
import { Icon, type IconName } from '~/components/ui/Icon'
import { auth } from '~/stores/auth.store'
import { locale, setLocale, t, type TranslationKey } from '~/utils/i18n'

interface NavItem {
  href: string
  label: TranslationKey
  icon: IconName
  color: string
  match?: (path: string) => boolean
}

const navItems: NavItem[] = [
  { href: '/dashboard', label: 'nav.dashboard', icon: 'dashboard', color: 'bg-brand-200' },
  { href: '/grade-levels', label: 'nav.gradeLevels', icon: 'calendar', color: 'bg-peach' },
  { href: '/courses', label: 'nav.courses', icon: 'book', color: 'bg-mint', match: (p) => p.startsWith('/courses') && !p.endsWith('/questions') },
  { href: '/questions', label: 'nav.questions', icon: 'questions', color: 'bg-butter', match: (p) => p.startsWith('/questions') || /^\/courses\/[^/]+\/questions/.test(p) },
  { href: '/exams', label: 'nav.exams', icon: 'exam', color: 'bg-peach' },
  { href: '/ai/generate', label: 'nav.ai', icon: 'sparkles', color: 'bg-butter' },
  { href: '/settings', label: 'nav.settings', icon: 'settings', color: 'bg-brand-100' },
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
    <nav class="flex h-full flex-col overflow-y-auto" aria-label={t('Main navigation')}>
      <A href="/dashboard" class="flex items-center gap-3 px-5 py-5">
        <div class="sticker size-10 -rotate-6 bg-butter">
          <Icon name="exam" class="size-5" />
        </div>
        <span class="font-display text-lg font-extrabold text-ink">{t('app.name')}</span>
      </A>
      <ul class="flex-1 space-y-1.5 px-3 pb-4">
        <For each={navItems}>
          {(item) => (
            <li>
              <A
                href={item.href}
                class="nav-link flex items-center gap-3 px-2 py-1.5 text-sm"
                classList={{ 'is-current': isActive(item) }}
              >
                <span class={`sticker size-8 rounded-lg ${item.color}`}>
                  <Icon name={item.icon} class="size-[17px]" />
                </span>
                {t(item.label)}
              </A>
            </li>
          )}
        </For>
      </ul>
      <div class="border-t-2 border-ink bg-brand-50 p-3">
        <div class="flex items-center gap-3 rounded-lg px-2 py-2">
          <div class="sticker size-9 rounded-full bg-mint text-xs font-bold">
            {initials()}
          </div>
          <div class="min-w-0 flex-1">
            <p class="truncate text-sm font-medium text-slate-900">{auth.state.user?.fullName}</p>
            <p class="truncate text-xs text-slate-500">{auth.state.user?.roles.map((role) => t(role)).join(locale() === 'ar' ? '، ' : ', ')}</p>
          </div>
          <button
            type="button"
            onClick={signOut}
            class="icon-button p-1.5 text-ink hover:bg-brand-100"
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
      <aside class="fixed inset-y-0 start-0 z-30 hidden w-64 border-e-2 border-ink bg-white lg:block">
        <Sidebar />
      </aside>

      {/* Mobile drawer */}
      <Show when={mobileOpen()}>
        <div class="fixed inset-0 z-40 bg-slate-900/40 lg:hidden" onClick={() => setMobileOpen(false)} />
        <aside class="fixed inset-y-0 start-0 z-50 w-64 border-e-2 border-ink bg-white shadow-xl lg:hidden">
          <Sidebar />
        </aside>
      </Show>

      <div class="lg:ps-64">
        <header class="sticky top-0 z-20 flex h-16 items-center gap-3 border-b-2 border-ink bg-paper px-4 sm:px-6">
          <IconButton icon="menu" label={t('nav.open')} class="lg:hidden" aria-expanded={mobileOpen()} onClick={() => setMobileOpen(true)} />
          <span class="font-display text-sm font-bold text-ink lg:hidden">{t('app.name')}</span>
          <div class="flex-1" />
          <A
            href="/ai/generate"
            class="action-button hidden items-center gap-1.5 bg-butter px-3 py-1.5 text-sm text-ink hover:bg-amber-300 sm:inline-flex"
          >
            <Icon name="sparkles" /> {t('action.generate')}
          </A>
          <Button
            variant="secondary"
            size="sm"
            icon="globe"
            onClick={() => setLocale(locale() === 'en' ? 'ar' : 'en')}
          >
            {t('common.language')}
          </Button>
        </header>
        <main class="mx-auto max-w-7xl px-4 py-6 sm:px-6 lg:py-8">{props.children}</main>
      </div>
    </div>
  )
}
