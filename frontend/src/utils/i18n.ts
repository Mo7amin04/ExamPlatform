import { createRoot, createSignal } from 'solid-js'

/**
 * Minimal i18n + direction handling. The shell (navigation, auth, common actions) is translated;
 * layouts use logical CSS properties (ms-/me-/start-/end-) so switching to Arabic flips the UI to RTL.
 */
export type Locale = 'en' | 'ar'

const dictionaries = {
  en: {
    'app.name': 'Exam Platform',
    'app.tagline': 'Exam management for university teachers',
    'nav.dashboard': 'Dashboard',
    'nav.departments': 'Departments',
    'nav.courses': 'Courses',
    'nav.questions': 'Question Bank',
    'nav.exams': 'Exams',
    'nav.ai': 'AI Generator',
    'nav.settings': 'Settings',
    'nav.signOut': 'Sign out',
    'auth.title': 'Sign in to your account',
    'auth.email': 'Email',
    'auth.password': 'Password',
    'auth.submit': 'Sign in',
    'common.language': 'العربية',
    'action.createQuestion': 'Create Question',
    'action.createExam': 'Create Exam',
    'action.generate': 'Generate Questions',
  },
  ar: {
    'app.name': 'منصة الاختبارات',
    'app.tagline': 'إدارة الاختبارات لأعضاء هيئة التدريس',
    'nav.dashboard': 'لوحة التحكم',
    'nav.departments': 'الأقسام',
    'nav.courses': 'المقررات',
    'nav.questions': 'بنك الأسئلة',
    'nav.exams': 'الاختبارات',
    'nav.ai': 'مولد الأسئلة الذكي',
    'nav.settings': 'الإعدادات',
    'nav.signOut': 'تسجيل الخروج',
    'auth.title': 'تسجيل الدخول إلى حسابك',
    'auth.email': 'البريد الإلكتروني',
    'auth.password': 'كلمة المرور',
    'auth.submit': 'تسجيل الدخول',
    'common.language': 'English',
    'action.createQuestion': 'إنشاء سؤال',
    'action.createExam': 'إنشاء اختبار',
    'action.generate': 'توليد أسئلة',
  },
} as const

export type TranslationKey = keyof (typeof dictionaries)['en']

const STORAGE_KEY = 'exam-platform.locale'

function initialLocale(): Locale {
  try {
    return localStorage.getItem(STORAGE_KEY) === 'ar' ? 'ar' : 'en'
  } catch {
    return 'en'
  }
}

const i18n = createRoot(() => {
  const [locale, setLocaleSignal] = createSignal<Locale>(initialLocale())

  const apply = (value: Locale) => {
    document.documentElement.lang = value
    document.documentElement.dir = value === 'ar' ? 'rtl' : 'ltr'
  }
  apply(locale())

  const setLocale = (value: Locale) => {
    setLocaleSignal(value)
    apply(value)
    try {
      localStorage.setItem(STORAGE_KEY, value)
    } catch {
      /* ignore */
    }
  }

  const t = (key: TranslationKey) => dictionaries[locale()][key] ?? dictionaries.en[key]

  return { locale, setLocale, t }
})

export const { locale, setLocale, t } = i18n
