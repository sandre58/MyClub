import { useEffect, useId, useState, type SubmitEvent } from 'react';
import { Clock3 } from 'lucide-react';
import { Alert } from '../design-system/components/Alert';
import {
  ChoiceSwatch,
  ChoiceTile,
} from '../design-system/components/ChoiceTile';
import { ColorPicker } from '../design-system/components/ColorPicker';
import { Field } from '../design-system/components/Field';
import { FormGroup } from '../design-system/components/FormGroup';
import { FormSection } from '../design-system/components/FormSection';
import { InputNumber } from '../design-system/components/InputNumber';
import {
  OutcomePoints,
  OutcomePointsCard,
} from '../design-system/components/OutcomePoints';
import { pointsBaremeWarning } from '../design-system/components/pointsBaremeWarning';
import { ReorderList } from '../design-system/components/ReorderList';
import { Select } from '../design-system/components/Select';
import { Switch } from '../design-system/components/Switch';
import { SwitchPanel } from '../design-system/components/SwitchPanel';
import { TextInput } from '../design-system/components/TextInput';
import { Upload } from '../design-system/components/Upload';
import { LucideIcon } from '../design-system/icons/Icon';
import {
  CrossIcon,
  EqualIcon,
  PersonIcon,
  PlusIcon,
  TrophyIcon,
} from '../design-system/icons/overviewIcons';

const CRITERION_OPTIONS = [
  { value: 'Points', label: 'Points' },
  { value: 'GoalDifference', label: 'Différence de buts' },
  { value: 'GoalsFor', label: 'Buts marqués' },
  { value: 'HeadToHead', label: 'Confrontations directes' },
  { value: 'Wins', label: 'Victoires' },
] as const;

type Criterion = (typeof CRITERION_OPTIONS)[number]['value'];

const DURATION_PRESETS = [30, 40, 45] as const;

/**
 * Design Lab — all form / editor DS controls in one board.
 */
export function LabForm() {
  const [name, setName] = useState('RC Lens');
  const [shortName, setShortName] = useState('RCL');
  const [logoUrl, setLogoUrl] = useState<string | null>(null);
  const [logoUploading, setLogoUploading] = useState(false);
  const [primary, setPrimary] = useState('#C8102E');
  const [secondary, setSecondary] = useState('#001F5B');
  const [demoUploadUrl, setDemoUploadUrl] = useState<string | null>(null);
  const [selectValue, setSelectValue] = useState<string | null>('fr');
  const [numberValue, setNumberValue] = useState<number | null>(11);
  const [periodMinutes, setPeriodMinutes] = useState<number | null>(45);
  const [extraTime, setExtraTime] = useState(false);
  const [etMinutes, setEtMinutes] = useState<number | null>(15);
  const [standaloneSwitch, setStandaloneSwitch] = useState(true);
  const [winPoints, setWinPoints] = useState<number | null>(3);
  const [drawPoints, setDrawPoints] = useState<number | null>(1);
  const [lossPoints, setLossPoints] = useState<number | null>(0);
  const [cards, setCards] = useState({ yellow: true, red: true, white: false });
  const [criteria, setCriteria] = useState<Criterion[]>([
    'Points',
    'GoalDifference',
    'GoalsFor',
    'HeadToHead',
  ]);
  const [addCriterion, setAddCriterion] = useState<string | null>(null);

  const nameId = useId();
  const shortId = useId();
  const searchId = useId();
  const selectId = useId();
  const numberId = useId();
  const durationId = useId();
  const etId = useId();
  const primaryId = useId();
  const secondaryId = useId();
  const addId = useId();
  const winId = useId();
  const drawId = useId();
  const lossId = useId();

  const baremeWarning = pointsBaremeWarning(
    winPoints,
    drawPoints,
    lossPoints,
    'Barème inhabituel : on attend Victoire ≥ Nul ≥ Défaite.',
  );
  const availableCriteria = CRITERION_OPTIONS.filter(
    (option) => !criteria.includes(option.value),
  );

  useEffect(() => {
    return () => {
      if (logoUrl) {
        URL.revokeObjectURL(logoUrl);
      }
      if (demoUploadUrl) {
        URL.revokeObjectURL(demoUploadUrl);
      }
    };
  }, [logoUrl, demoUploadUrl]);

  function onLogoChange(next: string | null) {
    setLogoUrl((prev) => {
      if (prev) {
        URL.revokeObjectURL(prev);
      }
      return next;
    });
    if (next) {
      setLogoUploading(true);
      window.setTimeout(() => setLogoUploading(false), 900);
    } else {
      setLogoUploading(false);
    }
  }

  function criterionLabel(value: Criterion): string {
    return (
      CRITERION_OPTIONS.find((option) => option.value === value)?.label ?? value
    );
  }

  const nameDup =
    name.trim().toLocaleLowerCase('fr') === 'rc lens'
      ? 'Une équipe porte déjà ce nom'
      : undefined;

  return (
    <div className="dlab-form">
      <header className="dlab-form__intro">
        <p className="ds-eyebrow">Design System</p>
        <h1 className="dlab-form__title">Form controls</h1>
        <p className="dlab-form__lede">
          Tous les contrôles d’édition DS : Field, TextInput, Select,
          InputNumber (end / split + suffixe), Switch / SwitchPanel,
          FormSection, FormGroup, OutcomePoints, ReorderList, ChoiceTile,
          Upload, ColorPicker. Surface Règlement = garde-fous produit à part.
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
            <Field label="Split (− / +)" htmlFor={`${numberId}-split`}>
              <InputNumber
                id={`${numberId}-split`}
                defaultValue={8}
                min={0}
                max={99}
                controlsLayout="split"
              />
            </Field>
            <Field label="Durée (minutes)" htmlFor={durationId}>
              <InputNumber
                id={durationId}
                value={periodMinutes}
                min={1}
                max={120}
                controlsLayout="split"
                leadingIcon={<LucideIcon icon={Clock3} size="sm" />}
                suffix="min"
                onChange={setPeriodMinutes}
              />
            </Field>
            <div
              className="dlab-form__presets"
              role="group"
              aria-label="Presets durée"
            >
              {DURATION_PRESETS.map((minutes) => (
                <button
                  key={minutes}
                  type="button"
                  className={[
                    'ds-chip',
                    periodMinutes === minutes
                      ? 'ds-chip--accent'
                      : 'ds-chip--soft',
                    'dlab-form__preset-btn',
                  ].join(' ')}
                  onClick={() => setPeriodMinutes(minutes)}
                >
                  {minutes} min
                </button>
              ))}
            </div>
            <p className="dlab-form__hint">
              Durée = number libre (ex. 13) + suffixe « min » ; presets =
              raccourcis qui écrivent la valeur.
            </p>
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

        <section className="ds-panel dlab-form__panel" aria-label="Switch">
          <h2 className="dlab-form__panel-title">Switch / SwitchPanel</h2>
          <div className="dlab-form__stack" data-density="comfortable">
            <div className="dlab-editor__switch-row">
              <span className="ds-body">Standalone</span>
              <Switch
                checked={standaloneSwitch}
                onChange={setStandaloneSwitch}
                label="Exemple"
              />
            </div>
            <SwitchPanel
              title="Prolongations"
              description="Corps inert / atténué lorsque le switch est off."
              checked={extraTime}
              onChange={setExtraTime}
            >
              <Field label="Durée d'une période" htmlFor={etId}>
                <InputNumber
                  id={etId}
                  value={etMinutes}
                  min={1}
                  max={30}
                  controlsLayout="split"
                  suffix="min"
                  onChange={setEtMinutes}
                />
              </Field>
            </SwitchPanel>
          </div>
        </section>

        <section
          className="ds-panel dlab-form__panel"
          aria-label="OutcomePoints"
        >
          <h2 className="dlab-form__panel-title">OutcomePoints</h2>
          <div className="dlab-form__stack" data-density="comfortable">
            <Field
              label="Barème de points"
              message={
                baremeWarning ??
                'Min 0. Soft-warn si Victoire < Nul ou Nul < Défaite.'
              }
              messageTone={baremeWarning ? 'warning' : 'hint'}
            >
              <OutcomePoints aria-label="Barème de points">
                <OutcomePointsCard
                  tone="win"
                  label="Victoire"
                  icon={<TrophyIcon size="sm" />}
                  value={
                    <InputNumber
                      id={winId}
                      value={winPoints}
                      min={0}
                      max={99}
                      controlsLayout="split"
                      aria-label="Points victoire"
                      onChange={setWinPoints}
                    />
                  }
                />
                <OutcomePointsCard
                  tone="draw"
                  label="Nul"
                  icon={<EqualIcon size="sm" />}
                  value={
                    <InputNumber
                      id={drawId}
                      value={drawPoints}
                      min={0}
                      max={99}
                      controlsLayout="split"
                      aria-label="Points nul"
                      onChange={setDrawPoints}
                    />
                  }
                />
                <OutcomePointsCard
                  tone="loss"
                  label="Défaite"
                  icon={<CrossIcon size="sm" />}
                  value={
                    <InputNumber
                      id={lossId}
                      value={lossPoints}
                      min={0}
                      max={99}
                      controlsLayout="split"
                      aria-label="Points défaite"
                      onChange={setLossPoints}
                    />
                  }
                />
              </OutcomePoints>
            </Field>
          </div>
        </section>

        <section className="ds-panel dlab-form__panel" aria-label="ReorderList">
          <h2 className="dlab-form__panel-title">ReorderList</h2>
          <div className="dlab-form__stack" data-density="comfortable">
            <p className="dlab-form__hint">
              Critère Points épinglé en #1 — non déplaçable, non retirable.
            </p>
            <ReorderList
              items={criteria}
              getKey={(item) => item}
              minMoveIndex={1}
              canDrag={(item) => item !== 'Points'}
              canRemove={(item) => item !== 'Points'}
              onReorder={(next) => {
                const withoutPoints = next.filter((item) => item !== 'Points');
                setCriteria(['Points', ...withoutPoints]);
              }}
              onRemove={(item) => {
                if (item === 'Points') {
                  return;
                }
                setCriteria((prev) => prev.filter((entry) => entry !== item));
              }}
              aria-label="Ordre de départage"
              renderContent={(item) => criterionLabel(item)}
            />
            <Field label="Ajouter un critère" htmlFor={addId}>
              <Select
                id={addId}
                value={addCriterion}
                placeholder="Ajouter un critère…"
                leadingIcon={<PlusIcon size="sm" />}
                options={availableCriteria.map((option) => ({
                  value: option.value,
                  label: option.label,
                }))}
                onChange={(next) => {
                  setAddCriterion(null);
                  if (next && next !== 'Points') {
                    setCriteria((prev) => [...prev, next as Criterion]);
                  }
                }}
              />
            </Field>
          </div>
        </section>

        <section className="ds-panel dlab-form__panel" aria-label="ChoiceTile">
          <h2 className="dlab-form__panel-title">ChoiceTile</h2>
          <div className="ds-choice-tile-row" role="group" aria-label="Cartons">
            <ChoiceTile
              label="Jaune"
              selected={cards.yellow}
              leading={<ChoiceSwatch color="#F5C518" label="Jaune" />}
              onChange={(selected) =>
                setCards((prev) => ({ ...prev, yellow: selected }))
              }
            />
            <ChoiceTile
              label="Rouge"
              selected={cards.red}
              leading={<ChoiceSwatch color="#E11D48" label="Rouge" />}
              onChange={(selected) =>
                setCards((prev) => ({ ...prev, red: selected }))
              }
            />
            <ChoiceTile
              label="Blanc"
              selected={cards.white}
              leading={<ChoiceSwatch color="#F8FAFC" label="Blanc" />}
              onChange={(selected) =>
                setCards((prev) => ({ ...prev, white: selected }))
              }
            />
          </div>
        </section>

        <section className="ds-panel dlab-form__panel" aria-label="FormSection">
          <h2 className="dlab-form__panel-title">FormSection / FormGroup</h2>
          <p className="dlab-form__hint">
            Chrome famille partagée hub lecture + Dialog édition (
            <code>description?</code>). <code>FormGroup</code> = sous-section
            structurelle (titre ≠ label Field).
          </p>
          <FormSection
            title="Équipes"
            description="Bornes du nombre d’équipes participantes."
            icon={<PersonIcon size="md" />}
          >
            <div className="ds-form--inline">
              <Field label="Minimum">
                <InputNumber
                  defaultValue={8}
                  min={2}
                  max={64}
                  controlsLayout="split"
                />
              </Field>
              <Field label="Maximum">
                <InputNumber
                  defaultValue={16}
                  min={2}
                  max={64}
                  controlsLayout="split"
                />
              </Field>
            </div>
            <FormGroup
              title="Forfait"
              description="Score attribué en cas de forfait."
            >
              <div className="ds-form--inline">
                <Field label="Vainqueur">
                  <InputNumber
                    defaultValue={3}
                    min={0}
                    max={20}
                    controlsLayout="split"
                  />
                </Field>
                <Field label="Perdant">
                  <InputNumber
                    defaultValue={0}
                    min={0}
                    max={20}
                    controlsLayout="split"
                  />
                </Field>
              </div>
            </FormGroup>
          </FormSection>
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
                name="Interactif"
                value={demoUploadUrl}
                emptyLabel="Image"
                onChange={(next) => {
                  setDemoUploadUrl((prev) => {
                    if (prev) {
                      URL.revokeObjectURL(prev);
                    }
                    return next;
                  });
                }}
              />
              <figcaption>Interactif</figcaption>
            </figure>
          </div>
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
            onSubmit={(event: SubmitEvent) => {
              event.preventDefault();
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
            <div className="dlab-form__footer">
              <button type="submit" className="ds-btn ds-btn--primary">
                Ajouter
              </button>
            </div>
          </form>
        </section>
      </div>
    </div>
  );
}
