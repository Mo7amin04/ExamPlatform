import { createRoot, createSignal } from 'solid-js'
import { uiMessages } from './uiMessages'

/**
 * Shared interface language and document direction. The selected locale is saved across visits.
 */
export type Locale = 'en' | 'ar'

const dictionaries = {
  en: {
    'app.name': 'Exam Platform',
    'app.tagline': 'Exam management for school teachers',
    'nav.dashboard': 'Dashboard',
    'nav.gradeLevels': 'Grade Levels',
    'nav.courses': 'Subjects',
    'nav.questions': 'Question Bank',
    'nav.exams': 'Exams',
    'nav.ai': 'AI Generator',
    'nav.settings': 'Settings',
    'nav.signOut': 'Sign out',
    'nav.open': 'Open navigation',
    'auth.heroTitle': 'Build better exams, faster.',
    'auth.heroDescription': 'Manage subjects, organize your question bank, and turn your ideas into print-ready exams.',
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
    'app.tagline': 'إدارة الاختبارات لمعلّمي المدارس',
    'nav.dashboard': 'لوحة التحكم',
    'nav.gradeLevels': 'الصفوف الدراسية',
    'nav.courses': 'المواد الدراسية',
    'nav.questions': 'بنك الأسئلة',
    'nav.exams': 'الاختبارات',
    'nav.ai': 'مولد الأسئلة الذكي',
    'nav.settings': 'الإعدادات',
    'nav.signOut': 'تسجيل الخروج',
    'nav.open': 'فتح قائمة التنقل',
    'auth.heroTitle': 'صمّم اختبارات أفضل في وقت أقل.',
    'auth.heroDescription': 'أدر المواد الدراسية ونظّم بنك الأسئلة وحوّل أفكارك إلى اختبارات جاهزة للطباعة.',
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

export type TranslationKey = keyof (typeof dictionaries)['en'] | keyof typeof uiMessages

const schoolMessages: ReadonlyMap<string, TranslationKey> = new Map([
  ['Remove all questions before moving the exam to another course.', 'Remove all questions before moving the exam to another subject.'],
  ['The course has questions or exams and cannot be deleted.', 'The subject has questions or exams and cannot be deleted.'],
  ['The department still has courses. Move or delete them first.', 'Move or delete the subjects in this grade level before deleting it.'],
  ['The selected department does not exist.', 'The selected grade level does not exist.'],
  ['A school year with this code already exists.', 'A grade level with this code already exists.'],
  ['The question is used in exams and cannot be moved to another course.', 'The question is used in exams and cannot be moved to another subject.'],
  ['The teacher is already assigned to this course.', 'The teacher is already assigned to this subject.'],
  ['The teacher is not assigned to this course.', 'The teacher is not assigned to this subject.'],
  ['The topic does not belong to the selected course.', 'The topic does not belong to the selected subject.'],
  ['You are not assigned to this course.', 'You are not assigned to this subject.'],
])

function isUiMessage(key: string): key is keyof typeof uiMessages {
  return Object.prototype.hasOwnProperty.call(uiMessages, key)
}

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
    document.title = dictionaries[value]['app.name']
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

  const t = (key: TranslationKey, values?: Record<string, string | number>) => {
    const language = locale()
    const message = isUiMessage(key) ? (language === 'ar' ? uiMessages[key] : key) : dictionaries[language][key] ?? key
    return values ? message.replace(/\{(\w+)\}/g, (match, name: string) => String(values[name] ?? match)) : message
  }

  const translateMessage = (message: string) => {
    const key = schoolMessages.get(message) ?? message
    return isUiMessage(key) ? t(key) : message
  }

  return { locale, setLocale, t, translateMessage }
})

export const { locale, setLocale, t, translateMessage } = i18n
