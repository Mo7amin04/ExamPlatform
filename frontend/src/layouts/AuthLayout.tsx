import type { ParentProps } from 'solid-js'
import { Icon } from '~/components/ui/Icon'
import { locale, setLocale, t } from '~/utils/i18n'

export function AuthLayout(props: ParentProps) {
  return (
    <div class="flex min-h-screen">
      <aside class="relative hidden w-[44%] flex-col justify-between overflow-hidden bg-gradient-to-br from-brand-800 via-brand-700 to-violet-700 p-10 text-white lg:flex">
        <div class="flex items-center gap-3">
          <div class="flex size-10 items-center justify-center rounded-lg bg-white/15">
            <Icon name="exam" class="size-5" />
          </div>
          <span class="text-lg font-semibold">{t('app.name')}</span>
        </div>
        <div class="max-w-md">
          <h2 class="text-3xl font-semibold leading-tight">Build better exams, faster.</h2>
          <p class="mt-4 text-brand-100">
            Manage your courses and question bank, assemble exams, draft questions with AI assistance and export
            print-ready papers — all in one place.
          </p>
        </div>
        <p class="text-sm text-brand-200">{t('app.tagline')}</p>
      </aside>
      <main class="flex flex-1 flex-col">
        <div class="flex justify-end p-4">
          <button
            type="button"
            class="inline-flex items-center gap-1.5 rounded-md px-2 py-1 text-sm text-slate-600 hover:bg-slate-100"
            onClick={() => setLocale(locale() === 'en' ? 'ar' : 'en')}
          >
            <Icon name="globe" /> {t('common.language')}
          </button>
        </div>
        <div class="flex flex-1 items-center justify-center px-4 pb-16">{props.children}</div>
      </main>
    </div>
  )
}
