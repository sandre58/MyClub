import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { uploadMedia } from '../api'
import { Field } from '../design-system/components/Field'
import { Upload } from '../design-system/components/Upload'
import { mediaContentUrl } from '../design-system/TeamCrest'
import { MutationError } from '../ui'

type LogoMediaFieldProps = {
  name: string
  value: string | null
  onChange: (logoMediaId: string | null) => void
  disabled?: boolean
  label: string
  primaryColor?: string | null
}

/**
 * Feature adapter: DS Upload + Media API.
 * Lives under pages/ — not design-system (API + i18n feature).
 */
export function LogoMediaField({
  name,
  value,
  onChange,
  disabled = false,
  label,
  primaryColor = null,
}: LogoMediaFieldProps) {
  const { t } = useTranslation('organisation')
  const [uploading, setUploading] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const [objectUrl, setObjectUrl] = useState<string | null>(null)

  useEffect(() => {
    return () => {
      if (objectUrl) {
        URL.revokeObjectURL(objectUrl)
      }
    }
  }, [objectUrl])

  const previewUrl = objectUrl ?? (value ? mediaContentUrl(value) : null)

  async function onUploadChange(nextUrl: string | null, file: File | null) {
    if (!file || !nextUrl) {
      if (objectUrl) {
        URL.revokeObjectURL(objectUrl)
        setObjectUrl(null)
      }
      setError(null)
      onChange(null)
      return
    }

    if (objectUrl) {
      URL.revokeObjectURL(objectUrl)
    }
    setObjectUrl(nextUrl)
    setUploading(true)
    setError(null)
    try {
      const metadata = await uploadMedia(file)
      onChange(metadata.id)
      URL.revokeObjectURL(nextUrl)
      setObjectUrl(null)
    } catch (err) {
      setError(err)
      URL.revokeObjectURL(nextUrl)
      setObjectUrl(null)
    } finally {
      setUploading(false)
    }
  }

  return (
    <Field label={label}>
      <Upload
        name={name}
        value={previewUrl}
        primaryColor={primaryColor}
        crestFallback={!previewUrl && !uploading}
        uploading={uploading}
        disabled={disabled}
        emptyLabel={label}
        removeLabel={t('logo.clear')}
        onChange={(nextUrl, file) => {
          void onUploadChange(nextUrl, file)
        }}
      />
      {error ? <MutationError error={error} /> : null}
    </Field>
  )
}
