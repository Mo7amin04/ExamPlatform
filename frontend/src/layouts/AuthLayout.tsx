import type { ParentProps } from 'solid-js'
import { Button } from '~/components/ui/Button'
import { ExamMascot } from '~/components/ui/ExamMascot'
import { Icon } from '~/components/ui/Icon'
import { locale, setLocale, t } from '~/utils/i18n'

export function AuthLayout(props: ParentProps) {
  return (
    <div class="flex min-h-screen gap-6 p-4 lg:p-6">
      <aside class="panel hidden w-[44%] flex-col justify-between gap-8 bg-brand-100 p-10 lg:flex">
        <div class="flex items-center gap-3">
          <div class="sticker size-10 -rotate-6 bg-butter">
            <Icon name="exam" class="size-5" />
          </div>
          <span class="font-display text-lg font-extrabold text-ink">{t('app.name')}</span>
        </div>
        <div class="mx-auto w-full max-w-md">
          <ExamMascot class="mx-auto mb-5 w-4/5 max-w-80" />
          <h2 class="font-display text-4xl font-extrabold leading-tight text-ink xl:text-5xl">{t('auth.heroTitle')}</h2>
          <p class="mt-4 text-ink">{t('auth.heroDescription')}</p>
        </div>
        <p class="text-sm font-semibold text-brand-800">{t('app.tagline')}</p>
      </aside>
      <main class="flex min-w-0 flex-1 flex-col">
        <div class="flex justify-end py-2">
          <Button
            variant="secondary"
            size="sm"
            icon="globe"
            onClick={() => setLocale(locale() === 'en' ? 'ar' : 'en')}
          >
            {t('common.language')}
          </Button>
        </div>
        <div class="flex flex-1 items-center justify-center py-8 lg:px-4 lg:pb-16">{props.children}</div>
      </main>
    </div>
  )
}
