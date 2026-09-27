// Domain types mirroring the API DTOs. Enums are serialized as strings by the API.

export const QUESTION_TYPES = [
  'MultipleChoice',
  'TrueFalse',
  'MultipleSelect',
  'ShortAnswer',
  'Essay',
  'FillBlank',
  'Matching',
  'Ordering',
] as const
export type QuestionType = (typeof QUESTION_TYPES)[number]

export const DIFFICULTIES = ['Easy', 'Medium', 'Hard'] as const
export type Difficulty = (typeof DIFFICULTIES)[number]

export const BLOOM_LEVELS = ['Remember', 'Understand', 'Apply', 'Analyze', 'Evaluate', 'Create'] as const
export type BloomLevel = (typeof BLOOM_LEVELS)[number]

export const QUESTION_STATUSES = ['Draft', 'Approved', 'Archived'] as const
export type QuestionStatus = (typeof QUESTION_STATUSES)[number]

export type QuestionSource = 'Manual' | 'AI'

export const EXAM_TYPES = ['Quiz', 'Midterm', 'Final', 'Assignment', 'Practice'] as const
export type ExamType = (typeof EXAM_TYPES)[number]

export const EXAM_STATUSES = ['Draft', 'Ready', 'Published', 'Archived'] as const
export type ExamStatus = (typeof EXAM_STATUSES)[number]

export type RoleName = 'Admin' | 'Teacher'

// ----- Auth & users -----

export interface CurrentUser {
  id: string
  fullName: string
  email: string
  roles: RoleName[]
}

export interface LoginResponse {
  accessToken: string
  expiresAt: string
  user: CurrentUser
}

export interface User {
  id: string
  fullName: string
  email: string
  isActive: boolean
  roles: RoleName[]
  createdAt: string
  lastLoginAt: string | null
}

// ----- Departments, courses, topics -----

export interface Department {
  id: string
  name: string
  code: string
  description: string | null
  courseCount: number
  createdAt: string
}

export interface CourseTeacher {
  id: string
  fullName: string
  email: string
}

export interface Topic {
  id: string
  courseId: string
  name: string
  description: string | null
  order: number
  questionCount: number
}

export interface Course {
  id: string
  departmentId: string
  departmentName: string
  code: string
  name: string
  description: string | null
  creditHours: number
  topicCount: number
  questionCount: number
  examCount: number
  teachers: CourseTeacher[]
  createdAt: string
}

export interface CourseDetail extends Omit<Course, 'topicCount'> {
  topics: Topic[]
}

// ----- Questions -----

export interface QuestionOptionInput {
  text: string
  isCorrect: boolean
  matchText?: string | null
}

export interface QuestionOption extends QuestionOptionInput {
  id: string
  order: number
}

export interface QuestionContent {
  text: string
  type: QuestionType
  difficulty: Difficulty
  bloomLevel: BloomLevel
  points: number
  explanation?: string | null
  expectedAnswer?: string | null
  options: QuestionOptionInput[]
}

export interface QuestionInput extends QuestionContent {
  courseId: string
  topicId?: string | null
  status?: QuestionStatus | null
  tags: string[]
}

export interface Question {
  id: string
  courseId: string
  courseCode: string
  courseName: string
  topicId: string | null
  topicName: string | null
  text: string
  type: QuestionType
  difficulty: Difficulty
  bloomLevel: BloomLevel
  points: number
  explanation: string | null
  expectedAnswer: string | null
  status: QuestionStatus
  source: QuestionSource
  options: QuestionOption[]
  tags: string[]
  usageCount: number
  isUsedInPublishedExam: boolean
  createdAt: string
  updatedAt: string | null
}

export interface QuestionListItem {
  id: string
  courseId: string
  courseCode: string
  topicId: string | null
  topicName: string | null
  text: string
  type: QuestionType
  difficulty: Difficulty
  bloomLevel: BloomLevel
  points: number
  status: QuestionStatus
  source: QuestionSource
  optionCount: number
  usageCount: number
  tags: string[]
  createdAt: string
  updatedAt: string | null
}

export interface QuestionFilters {
  page: number
  pageSize: number
  courseId?: string
  topicId?: string
  type?: QuestionType
  difficulty?: Difficulty
  bloomLevel?: BloomLevel
  status?: QuestionStatus
  source?: QuestionSource
  search?: string
  excludeExamId?: string
}

// ----- Exams -----

export interface ExamSummary {
  id: string
  courseId: string
  courseCode: string
  courseName: string
  title: string
  type: ExamType
  status: ExamStatus
  durationMinutes: number
  totalPoints: number
  examDate: string | null
  questionCount: number
  createdAt: string
  updatedAt: string | null
}

export interface ExamQuestion {
  questionId: string
  order: number
  points: number
  section: string | null
  text: string
  type: QuestionType
  difficulty: Difficulty
  bloomLevel: BloomLevel
  questionStatus: QuestionStatus
  topicName: string | null
  defaultPoints: number
  options: QuestionOption[]
}

export interface ExamDetail {
  id: string
  courseId: string
  courseCode: string
  courseName: string
  title: string
  description: string | null
  instructions: string | null
  type: ExamType
  status: ExamStatus
  durationMinutes: number
  totalPoints: number
  examDate: string | null
  versionCount: number
  questions: ExamQuestion[]
  createdAt: string
  updatedAt: string | null
}

export interface ExamInput {
  courseId: string
  title: string
  description?: string | null
  instructions?: string | null
  type: ExamType
  durationMinutes: number
  examDate?: string | null
}

export interface ExamFilters {
  page: number
  pageSize: number
  courseId?: string
  status?: ExamStatus
  type?: ExamType
  search?: string
}

// ----- Preview -----

export interface ExamHeader {
  universityName: string
  departmentName: string
  courseCode: string
  courseName: string
  title: string
  description: string | null
  instructions: string | null
  type: ExamType
  status: ExamStatus
  examDate: string | null
  durationMinutes: number
  totalPoints: number
  questionCount: number
}

export interface PreviewItem {
  label: string
  text: string
}

export interface PreviewQuestion {
  number: number
  section: string | null
  text: string
  type: QuestionType
  points: number
  options: PreviewItem[]
  matchItems: PreviewItem[]
  answerLines: number
}

export interface ExamPreview {
  header: ExamHeader
  questions: PreviewQuestion[]
}

export interface AnswerKeyItem {
  number: number
  section: string | null
  questionText: string
  type: QuestionType
  points: number
  correctAnswer: string
  explanation: string | null
}

export interface ExamAnswerKey {
  header: ExamHeader
  items: AnswerKeyItem[]
}

// ----- AI -----

export interface GeneratedQuestion extends QuestionContent {
  isValid: boolean
  issues: string[]
}

export interface GenerateQuestionsRequest {
  courseId: string
  topicId?: string | null
  numberOfQuestions: number
  questionType: QuestionType
  difficulty: Difficulty
  bloomLevel: BloomLevel
  additionalInstructions?: string
  sourceMaterial?: string
}

export interface GenerateQuestionsResult {
  generationId: string
  provider: string
  model: string | null
  questions: GeneratedQuestion[]
}

export interface MaterialText {
  fileName: string
  text: string
  characterCount: number
  truncated: boolean
}

// ----- Dashboard -----

export interface DashboardCourse {
  id: string
  code: string
  name: string
  departmentName: string
  questionCount: number
  examCount: number
}

export interface DashboardQuestion {
  id: string
  courseCode: string
  text: string
  type: QuestionType
  difficulty: Difficulty
  status: QuestionStatus
  source: QuestionSource
  createdAt: string
}

export interface Dashboard {
  courseCount: number
  questionCount: number
  draftExamCount: number
  publishedExamCount: number
  aiGeneratedPendingReviewCount: number
  courses: DashboardCourse[]
  recentExams: ExamSummary[]
  recentQuestions: DashboardQuestion[]
}
