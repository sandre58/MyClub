import { useState, type ChangeEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { uploadMedia } from '../api'
import { MutationError, PendingLabel } from '../ui'
import { TeamCrest } from './TeamCrest'

type LogoMediaFieldProps = {
  name: string
  value: string | null
  onChange: (logoMediaId: string | null) => void
  disabled?: boolean
  label: string
}

/**
 * File picker that uploads to Media and stores the resulting Guid.
 */
export function LogoMediaField({
  name,
  value,
  onChange,
  disabled = false,
  label,
}: LogoMediaFieldProps) {
  const { t } = useTranslation('organisation')
  const [uploading, setUploading] = useState(false)
  const [error, setError] = useState<unknown>(null)

  async function onFileChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file) {
      return
    }

    setUploading(true)
    setError(null)
    try {
      const metadata = await uploadMedia(file)
      onChange(metadata.id)
    } catch (err) {
      setError(err)
    } finally {
      setUploading(false)
    }
  }

  const busy = disabled || uploading

  return (
    <div className="field logo-media-field">
      <span className="logo-media-field__label">{label}</span>
      <div className="logo-media-field__row">
        <TeamCrest name={name} logoMediaId={value} size="md" />
        <input
          type="file"
          accept="image/png,image/jpeg,image/webp"
          disabled={busy}
          onChange={onFileChange}
          aria-label={label}
        />
        {value ? (
          <button
            type="button"
            className="ds-btn ds-btn--ghost"
            disabled={busy}
            onClick={() => onChange(null)}
          >
            {t('logo.clear')}
          </button>
        ) : null}
        {uploading ? <PendingLabel>{t('logo.uploading')}</PendingLabel> : null}
      </div>
      {error ? <MutationError error={error} /> : null}
    </div>
  )
}
