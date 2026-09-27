import { createSignal, Show } from 'solid-js'
import { toast } from '~/components/feedback/toast'
import { Button, IconButton } from '~/components/ui/Button'
import { Icon } from '~/components/ui/Icon'
import { aiApi } from '~/services/ai.api'
import { getErrorMessage } from '~/services/api'
import type { MaterialText } from '~/types/models'

const ALLOWED = ['.pdf', '.docx', '.pptx']
const MAX_BYTES = 10 * 1024 * 1024

/** Optional course material (.pdf/.docx/.pptx) used to ground AI generation. Validated client- and server-side. */
export function MaterialUpload(props: { value: MaterialText | null; onChange: (value: MaterialText | null) => void }) {
  const [uploading, setUploading] = createSignal(false)
  let input: HTMLInputElement | undefined

  const onFile = async (file: File | undefined) => {
    if (!file) return
    const ext = file.name.slice(file.name.lastIndexOf('.')).toLowerCase()
    if (!ALLOWED.includes(ext)) {
      toast.error('Unsupported file type', `Allowed: ${ALLOWED.join(', ')}`)
      return
    }
    if (file.size > MAX_BYTES) {
      toast.error('File too large', 'The maximum size is 10 MB.')
      return
    }
    setUploading(true)
    try {
      const result = await aiApi.extractMaterial(file)
      props.onChange(result)
      if (result.truncated) toast.info('Material truncated', 'Only the first part of the document will be used.')
    } catch (err) {
      toast.error('Could not read the file', getErrorMessage(err))
    } finally {
      setUploading(false)
      if (input) input.value = ''
    }
  }

  return (
    <div>
      <p class="mb-1.5 text-sm font-medium text-slate-700">Course material (optional)</p>
      <Show
        when={props.value}
        fallback={
          <div class="flex items-center gap-3 rounded-lg border border-dashed border-slate-300 p-3">
            <Icon name="upload" class="size-5 text-slate-400" />
            <p class="flex-1 text-xs text-slate-500">Upload lecture notes or slides (.pdf, .docx, .pptx, max 10 MB). Files are read once and not stored.</p>
            <Button size="sm" variant="secondary" loading={uploading()} onClick={() => input?.click()}>Choose file</Button>
          </div>
        }
      >
        {(material) => (
          <div class="flex items-center gap-3 rounded-lg border border-slate-200 bg-slate-50 p-3">
            <Icon name="file" class="size-5 text-brand-600" />
            <div class="min-w-0 flex-1">
              <p class="truncate text-sm font-medium text-slate-900">{material().fileName}</p>
              <p class="text-xs text-slate-500">{material().characterCount.toLocaleString()} characters extracted{material().truncated ? ' (truncated)' : ''}</p>
            </div>
            <IconButton icon="x" label="Remove material" onClick={() => props.onChange(null)} />
          </div>
        )}
      </Show>
      <input ref={input} type="file" class="hidden" accept={ALLOWED.join(',')} onChange={(e) => onFile(e.currentTarget.files?.[0])} />
    </div>
  )
}
