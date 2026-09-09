import { useRef, type ChangeEvent, type KeyboardEvent } from 'react';
import { TeamCrest } from '../TeamCrest';
import { PlusIcon, TrashIcon } from '../icons/overviewIcons';

export type UploadProps = {
  /** Crest initials when using letter-mark fallback. */
  name: string;
  /** Object URL / remote URL; null = empty unless crestFallback. */
  value: string | null;
  primaryColor?: string | null;
  /** Show letter crest as filled state without an image URL (Lab demos). */
  crestFallback?: boolean;
  uploading?: boolean;
  disabled?: boolean;
  accept?: string;
  emptyLabel?: string;
  removeLabel?: string;
  onChange: (nextUrl: string | null, file: File | null) => void;
};

/**
 * Single picture-card upload — crest/image in card, hover remove, loading overlay.
 * Ant Upload picture-card interaction; Play’Up tokens.
 */
export function Upload({
  name,
  value,
  primaryColor,
  crestFallback = false,
  uploading = false,
  disabled = false,
  accept = 'image/png,image/jpeg,image/webp',
  emptyLabel = 'Importer',
  removeLabel = 'Retirer',
  onChange,
}: UploadProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const filled = Boolean(value) || crestFallback;
  const busy = disabled || uploading;

  function onFileChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0] ?? null;
    event.target.value = '';
    if (!file) {
      return;
    }
    const url = URL.createObjectURL(file);
    onChange(url, file);
  }

  function clear(event: {
    stopPropagation: () => void;
    preventDefault: () => void;
  }) {
    event.stopPropagation();
    event.preventDefault();
    if (busy) {
      return;
    }
    onChange(null, null);
  }

  function onCardKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      if (!busy) {
        inputRef.current?.click();
      }
    }
  }

  return (
    <div
      className="ds-upload"
      data-filled={filled ? 'true' : 'false'}
      data-uploading={uploading ? 'true' : 'false'}
      data-disabled={disabled ? 'true' : 'false'}
      role="button"
      tabIndex={busy ? -1 : 0}
      aria-label={emptyLabel}
      aria-busy={uploading || undefined}
      onKeyDown={onCardKeyDown}
      onClick={() => {
        if (!busy) {
          inputRef.current?.click();
        }
      }}
    >
      <input
        ref={inputRef}
        className="ds-upload__file"
        type="file"
        accept={accept}
        disabled={busy}
        tabIndex={-1}
        aria-hidden="true"
        hidden={uploading}
        onChange={onFileChange}
        onClick={(event) => event.stopPropagation()}
      />

      {value ? (
        <span className="ds-upload__preview">
          <img className="ds-upload__img" src={value} alt="" />
        </span>
      ) : crestFallback ? (
        <span className="ds-upload__preview">
          <TeamCrest name={name} primaryColor={primaryColor} size="lg" />
        </span>
      ) : uploading ? null : (
        <span className="ds-upload__empty">
          <PlusIcon size="md" aria-hidden="true" />
          <span className="ds-upload__empty-label">{emptyLabel}</span>
        </span>
      )}

      {filled && !uploading ? (
        <div className="ds-upload__toolbar">
          <button
            type="button"
            className="ds-upload__toolbar-btn"
            aria-label={removeLabel}
            title={removeLabel}
            disabled={busy}
            onClick={clear}
          >
            <TrashIcon size="sm" aria-hidden="true" />
          </button>
        </div>
      ) : null}

      {uploading ? (
        <span className="ds-upload__loading" aria-hidden="true">
          <span className="ds-spinner" />
        </span>
      ) : null}
    </div>
  );
}
