import { useEffect, useId, useState, type FormEvent } from 'react'
import { Field } from '../design-system/components/Field'
import { TextInput } from '../design-system/components/TextInput'
import { InputNumber } from '../design-system/components/InputNumber'
import { Select } from '../design-system/components/Select'
import { Upload } from '../design-system/components/Upload'
import { ColorPicker } from '../design-system/components/ColorPicker'
import { PersonIcon } from '../design-system/icons/overviewIcons'
import { Alert } from '../design-system/components/Alert'

/**
 * Design Lab — Form controls specimen (Input, Select, InputNumber, Upload, ColorPicker).
 * Interactive reference; not a product page.
 */
export function LabForm() {
  const [name, setName] = useState('RC Lens')
  const [shortName, setShortName] = useState('RCL')
  const [logoUrl, setLogoUrl] = useState<string | null>(null)
  const [logoUploading, setLogoUploading] = useState(false)
  const [primary, setPrimary] = useState('#C8102E')
  const [secondary, setSecondary] = useState('#001F5B')
  const [demoUploadUrl, setDemoUploadUrl] = useState<string | null>(null)
  const [selectValue, setSelectValue] = useState<string | null>('fr')
  const [numberValue, setNumberValue] = useState<number | null>(11)

  const nameId = useId()
  const shortId = useId()
  const searchId = useId()
  const selectId = useId()
  const numberId = useId()
  const primaryId = useId()
  const secondaryId = useId()

  useEffect(() => {
    return () => {
      if (logoUrl) {
        URL.revokeObjectURL(logoUrl)
      }
      if (demoUploadUrl) {
        URL.revokeObjectURL(demoUploadUrl)
      }
    }
  }, [logoUrl, demoUploadUrl])

  function onLogoChange(next: string | null) {
    setLogoUrl((prev) => {
      if (prev) {
        URL.revokeObjectURL(prev)
      }
      return next
    })
    if (next) {
      setLogoUploading(true)
      window.setTimeout(() => setLogoUploading(false), 900)
    } else {
      setLogoUploading(false)
    }
  }

  const nameDup =
    name.trim().toLocaleLowerCase('fr') === 'rc lens'
      ? 'Une équipe porte déjà ce nom'
      : undefined

  return (
    <div className="dlab-form">
      <header className="dlab-form__intro">
        <p className="ds-eyebrow">Design System</p>
        <h1 className="dlab-form__title">Form controls</h1>
        <p className="dlab-form__lede">
          TextInput, Select, InputNumber, Upload, ColorPicker, Field — coque
          commune (icône G, Vider). ColorPicker : éditeur HSV (HEX/RGB/HSB,
          pipette). Alert soft-fill. Focus = halo brand.
        </p>
      </header>

      <div className="dlab-form__grid">
        <section className="ds-panel dlab-form__panel" aria-label="Input">
          <h2 className="dlab-form__panel-title">Input</h2>
          <div className="dlab-form__stack" data-density="comfortable">
            <Field label="Idle" htmlFor={`${searchId}-idle`}>
              <TextInput
                id={`${searchId}-idle`}
                placeholder="Saisissez une valeur…"
              />
            </Field>
            <Field label="Avec icône" htmlFor={searchId} counter="12/100">
              <TextInput
                id={searchId}
                leadingIcon={<PersonIcon size="sm" />}
                placeholder="Rechercher…"
                defaultValue="Dupont"
                allowClear
              />
            </Field>
            <Field label="Clear + copy" htmlFor={`${searchId}-affix`}>
              <TextInput
                id={`${searchId}-affix`}
                defaultValue="À copier"
                allowClear
                allowCopy
              />
            </Field>
            <Field
              label="Erreur"
              htmlFor={`${searchId}-err`}
              message="Cette valeur est requise"
              messageTone="error"
            >
              <TextInput id={`${searchId}-err`} invalid defaultValue="" />
            </Field>
            <Field
              label="Attention"
              htmlFor={`${searchId}-warn`}
              message="Une équipe porte déjà ce nom"
              messageTone="warning"
            >
              <TextInput id={`${searchId}-warn`} defaultValue="RC Lens" />
            </Field>
          </div>
        </section>

        <section className="ds-panel dlab-form__panel" aria-label="Select">
          <h2 className="dlab-form__panel-title">Select</h2>
          <p className="dlab-form__hint ds-body">
            Ouverture : ArrowDown / Enter / Espace. Liste ouverte : flèches,
            Enter pour choisir, Escape (pile dismiss LIFO) pour fermer.
          </p>
          <div className="dlab-form__stack" data-density="comfortable">
            <Field label="Pays" htmlFor={selectId}>
              <Select
                id={selectId}
                value={selectValue}
                allowClear
                leadingIcon={<PersonIcon size="sm" />}
                options={[
                  { value: 'fr', label: 'France' },
                  { value: 'be', label: 'Belgique' },
                  { value: 'ch', label: 'Suisse' },
                ]}
                onChange={setSelectValue}
              />
            </Field>
            <Field label="Placeholder" htmlFor={`${selectId}-empty`}>
              <Select
                id={`${selectId}-empty`}
                value={null}
                placeholder="Choisir…"
                options={[
                  { value: 'a', label: 'Option A' },
                  { value: 'b', label: 'Option B' },
                ]}
                onChange={() => undefined}
              />
            </Field>
          </div>
        </section>

        <section className="ds-panel dlab-form__panel" aria-label="InputNumber">
          <h2 className="dlab-form__panel-title">InputNumber</h2>
          <div className="dlab-form__stack" data-density="comfortable">
            <Field label="Effectif" htmlFor={numberId}>
              <InputNumber
                id={numberId}
                value={numberValue}
                min={0}
                max={99}
                step={1}
                precision={0}
                allowClear
                onChange={setNumberValue}
              />
            </Field>
            <Field label="Avec préfixe" htmlFor={`${numberId}-prefix`}>
              <InputNumber
                id={`${numberId}-prefix`}
                defaultValue={41}
                min={0}
                max={255}
                leadingIcon={<span className="ds-color-picker__ch">R</span>}
              />
            </Field>
            <Field label="Sans steppers" htmlFor={`${numberId}-plain`}>
              <InputNumber
                id={`${numberId}-plain`}
                defaultValue={100}
                min={0}
                max={100}
                controls={false}
              />
            </Field>
          </div>
        </section>

        <section className="ds-panel dlab-form__panel" aria-label="Upload">
          <h2 className="dlab-form__panel-title">Upload</h2>
          <div className="dlab-form__upload-row">
            <figure className="dlab-form__specimen">
              <Upload
                name="Empty"
                value={null}
                emptyLabel="Image"
                onChange={() => undefined}
              />
              <figcaption>Vide</figcaption>
            </figure>
            <figure className="dlab-form__specimen">
              <Upload
                name="RC Lens"
                value={null}
                crestFallback
                primaryColor="#001F5B"
                emptyLabel="Image"
                onChange={() => undefined}
              />
              <figcaption>Rempli (crest)</figcaption>
            </figure>
            <figure className="dlab-form__specimen">
              <Upload
                name="RC Lens"
                value={null}
                crestFallback
                primaryColor="#001F5B"
                uploading
                emptyLabel="Image"
                onChange={() => undefined}
              />
              <figcaption>Loading</figcaption>
            </figure>
            <figure className="dlab-form__specimen">
              <Upload
                name="Interactif"
                value={demoUploadUrl}
                emptyLabel="Image"
                onChange={(next) => {
                  setDemoUploadUrl((prev) => {
                    if (prev) {
                      URL.revokeObjectURL(prev)
                    }
                    return next
                  })
                }}
              />
              <figcaption>Interactif</figcaption>
            </figure>
          </div>
          <p className="dlab-form__hint">
            Survol du crest → toolbar Retirer. Clic = file picker.
          </p>
        </section>

        <section className="ds-panel dlab-form__panel" aria-label="ColorPicker">
          <h2 className="dlab-form__panel-title">ColorPicker</h2>
          <div className="dlab-form__stack" data-density="comfortable">
            <Field label="Primaire" htmlFor={primaryId}>
              <ColorPicker
                id={primaryId}
                aria-label="Couleur primaire"
                value={primary}
                onChange={setPrimary}
              />
            </Field>
            <Field label="Secondaire" htmlFor={secondaryId}>
              <ColorPicker
                id={secondaryId}
                aria-label="Couleur secondaire"
                value={secondary}
                onChange={setSecondary}
              />
            </Field>
            <Field label="Sans Vider" htmlFor={`${primaryId}-noclear`}>
              <ColorPicker
                id={`${primaryId}-noclear`}
                aria-label="Sans vider"
                value="#808080"
                allowClear={false}
                onChange={() => undefined}
              />
            </Field>
          </div>
        </section>

        <section
          className="ds-panel dlab-form__panel dlab-form__panel--compose"
          aria-label="Form"
          data-density="comfortable"
        >
          <h2 className="dlab-form__panel-title">Form / Field</h2>
          <Alert tone="danger" role="status">
            Erreur serveur (exemple) — haut du formulaire.
          </Alert>
          <form
            className="ds-form"
            onSubmit={(event: FormEvent) => {
              event.preventDefault()
            }}
          >
            <Field
              label="Nom"
              htmlFor={nameId}
              required
              counter={`${name.length}/100`}
              message={nameDup}
              messageTone="warning"
            >
              <TextInput
                id={nameId}
                value={name}
                maxLength={100}
                required
                allowClear
                onChange={(event) => setName(event.target.value)}
              />
            </Field>
            <Field
              label="Nom court"
              htmlFor={shortId}
              required
              counter={`${shortName.length}/5`}
            >
              <TextInput
                id={shortId}
                value={shortName}
                maxLength={5}
                required
                allowClear
                onChange={(event) => setShortName(event.target.value)}
              />
            </Field>
            <Field label="Logo">
              <Upload
                name={name || 'Équipe'}
                value={logoUrl}
                primaryColor={primary || null}
                uploading={logoUploading}
                emptyLabel="Image"
                onChange={onLogoChange}
              />
            </Field>
            <div className="ds-form--inline">
              <Field label="Primaire" htmlFor={`${nameId}-p`}>
                <ColorPicker
                  id={`${nameId}-p`}
                  aria-label="Primaire"
                  value={primary}
                  onChange={setPrimary}
                />
              </Field>
              <Field label="Secondaire" htmlFor={`${nameId}-s`}>
                <ColorPicker
                  id={`${nameId}-s`}
                  aria-label="Secondaire"
                  value={secondary}
                  onChange={setSecondary}
                />
              </Field>
            </div>
            <div className="dlab-form__footer">
              <button type="submit" className="ds-btn ds-btn--primary">
                Ajouter
              </button>
            </div>
          </form>
        </section>
      </div>
    </div>
  )
}
