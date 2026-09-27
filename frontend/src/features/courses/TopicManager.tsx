import { createSignal, For, Show } from 'solid-js'
import { EmptyState } from '~/components/feedback/States'
import { toast } from '~/components/feedback/toast'
import { TextArea, TextInput } from '~/components/forms/Fields'
import { Button, IconButton } from '~/components/ui/Button'
import { Modal, useConfirm } from '~/components/ui/Dialog'
import { Card, CardHeader } from '~/components/ui/Primitives'
import { getErrorMessage, getFieldErrors } from '~/services/api'
import { coursesApi } from '~/services/courses.api'
import type { Topic } from '~/types/models'

export function TopicManager(props: { courseId: string; topics: Topic[]; onChanged: () => void }) {
  const [editing, setEditing] = createSignal<Topic | null>(null)
  const [open, setOpen] = createSignal(false)
  const [name, setName] = createSignal('')
  const [description, setDescription] = createSignal('')
  const [errors, setErrors] = createSignal<Record<string, string>>({})
  const [saving, setSaving] = createSignal(false)
  const confirm = useConfirm()

  const openEditor = (topic: Topic | null) => {
    setEditing(topic)
    setName(topic?.name ?? '')
    setDescription(topic?.description ?? '')
    setErrors({})
    setOpen(true)
  }

  const save = async (e: SubmitEvent) => {
    e.preventDefault()
    setSaving(true)
    try {
      const current = editing()
      const body = { name: name(), description: description() || null, order: current?.order ?? null }
      if (current) await coursesApi.updateTopic(current.id, body)
      else await coursesApi.createTopic(props.courseId, body)
      toast.success(current ? 'Topic updated' : 'Topic added')
      setOpen(false)
      props.onChanged()
    } catch (err) {
      const fe = getFieldErrors(err)
      setErrors(fe)
      if (!Object.keys(fe).length) toast.error('Could not save topic', getErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  const remove = (topic: Topic) =>
    confirm.ask({
      title: `Delete topic "${topic.name}"?`,
      description: 'Questions in this topic stay in the question bank without a topic.',
      confirmLabel: 'Delete topic',
      onConfirm: async () => {
        try {
          await coursesApi.removeTopic(topic.id)
          toast.success('Topic deleted')
          props.onChanged()
        } catch (err) {
          toast.error('Could not delete topic', getErrorMessage(err))
        }
      },
    })

  return (
    <Card>
      <CardHeader title="Topics" description="Organize questions by syllabus topic." actions={<Button size="sm" icon="plus" onClick={() => openEditor(null)}>Add topic</Button>} />
      <Show when={props.topics.length} fallback={<EmptyState icon="book" title="No topics yet" description="Add topics to classify your questions." />}>
        <ol class="divide-y divide-slate-100">
          <For each={props.topics}>
            {(topic, i) => (
              <li class="flex items-start gap-3 px-5 py-3">
                <span class="mt-0.5 flex size-6 shrink-0 items-center justify-center rounded-full bg-slate-100 text-xs font-semibold text-slate-600">{i() + 1}</span>
                <div class="min-w-0 flex-1">
                  <p class="text-sm font-medium text-slate-900">{topic.name}</p>
                  <Show when={topic.description}><p class="text-xs text-slate-500">{topic.description}</p></Show>
                </div>
                <span class="text-xs text-slate-500">{topic.questionCount} questions</span>
                <IconButton icon="edit" label={`Edit ${topic.name}`} onClick={() => openEditor(topic)} />
                <IconButton icon="trash" label={`Delete ${topic.name}`} tone="danger" onClick={() => remove(topic)} />
              </li>
            )}
          </For>
        </ol>
      </Show>

      <Modal
        open={open()}
        onOpenChange={setOpen}
        title={editing() ? 'Edit topic' : 'Add topic'}
        size="sm"
        footer={
          <>
            <Button variant="secondary" onClick={() => setOpen(false)}>Cancel</Button>
            <Button type="submit" form="topic-form" loading={saving()}>Save</Button>
          </>
        }
      >
        <form id="topic-form" class="space-y-4" onSubmit={save}>
          <TextInput label="Name" required value={name()} onInput={(e) => setName(e.currentTarget.value)} error={errors().name} />
          <TextArea label="Description" value={description()} onInput={(e) => setDescription(e.currentTarget.value)} error={errors().description} />
        </form>
      </Modal>
      <confirm.Dialog />
    </Card>
  )
}
