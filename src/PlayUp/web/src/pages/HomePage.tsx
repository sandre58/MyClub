import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  type SubmitEvent,
  type RefObject,
  useEffect,
  useId,
  useRef,
  useState,
} from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate } from 'react-router-dom';
import { createCompetition, fetchCompetitions } from '../api';
import { Dialog } from '../design-system/components/Dialog';
import { Field } from '../design-system/components/Field';
import { HomeBrand } from '../design-system/components/HomeBrand';
import { TextInput } from '../design-system/components/TextInput';
import { TeamCrest } from '../design-system/TeamCrest';
import { ChevronRightIcon } from '../design-system/icons/shellIcons';
import { PlusIcon } from '../design-system/icons/contentIcons';
import '../design-system/fonts';
import '../design-system/index.css';
import { queryKeys } from '../queryKeys';
import { toIntlLocale } from '../i18n/intlLocale';
import {
  CompetitionStatusBadge,
  ErrorState,
  LoadingState,
  MutationError,
  PendingLabel,
} from '../ui';
import { declaredSchedule } from '../shell/competitionPeriod';
import { PreferencesMenu } from '../shell/PreferencesMenu';
import { useThemeRoot } from '../theme/useThemeRoot';
import {
  COMPETITION_NAME_MAX_LENGTH,
  type CompetitionListItem,
} from '../types';

/**
 * Accueil hub ops — hors Shell (`.ds-root` seul).
 * Choisir / créer une compétition. Shell V1 commence sur `/competitions/:id…`.
 */
export function HomePage() {
  useThemeRoot();
  const { t } = useTranslation('home');
  const { t: tc } = useTranslation('competitions');
  const { t: tCommon } = useTranslation('common');
  const [dialogOpen, setDialogOpen] = useState(false);
  const createTriggerRef = useRef<HTMLButtonElement>(null);

  const query = useQuery({
    queryKey: queryKeys.competitions.all,
    queryFn: fetchCompetitions,
  });

  const openCreate = () => setDialogOpen(true);
  const closeCreate = () => setDialogOpen(false);

  const isEmpty = query.data !== undefined && query.data.length === 0;

  return (
    <div
      className="ds-root ds-home"
      data-font="plex"
      data-palette="slate"
      data-density="standard"
    >
      <a className="ds-skip-link" href="#main">
        {tCommon('skipToContent')}
      </a>

      <PreferencesMenu className="shell-preferences--home" />

      <main id="main" className="ds-home__main">
        <HomeBrand lede={isEmpty ? tc('emptyHint') : t('lede')} />

        {query.isPending && <LoadingState size="home" />}

        {query.isError && <ErrorState error={query.error} />}

        {isEmpty && (
          <div className="ds-empty">
            <CreateButton
              buttonRef={createTriggerRef}
              onClick={openCreate}
              label={tc('create.cta')}
            />
          </div>
        )}

        {query.isError && (
          <CompetitionSection
            items={[]}
            onCreateClick={openCreate}
            createTriggerRef={createTriggerRef}
            showList={false}
          />
        )}

        {query.data && query.data.length > 0 && (
          <CompetitionSection
            items={query.data}
            onCreateClick={openCreate}
            createTriggerRef={createTriggerRef}
            showList
          />
        )}

        <CreateCompetitionDialog
          open={dialogOpen}
          onClose={closeCreate}
          returnFocusRef={createTriggerRef}
        />
      </main>
    </div>
  );
}

function CreateButton({
  onClick,
  label,
  buttonRef,
}: {
  onClick: () => void;
  label: string;
  buttonRef?: RefObject<HTMLButtonElement | null>;
}) {
  return (
    <button
      ref={buttonRef}
      type="button"
      className="ds-btn ds-btn--primary"
      onClick={onClick}
    >
      <PlusIcon size="sm" />
      {label}
    </button>
  );
}

function CompetitionSection({
  items,
  onCreateClick,
  createTriggerRef,
  showList,
}: {
  items: CompetitionListItem[];
  onCreateClick: () => void;
  createTriggerRef: RefObject<HTMLButtonElement | null>;
  showList: boolean;
}) {
  const { t } = useTranslation('competitions');

  return (
    <section
      className="ds-group"
      aria-labelledby="accueil-competitions-heading"
    >
      <div className="ds-home__section-head">
        <h2
          id="accueil-competitions-heading"
          className="ds-home__section-title"
        >
          {t('listLabel')}
        </h2>
        <CreateButton
          buttonRef={createTriggerRef}
          onClick={onCreateClick}
          label={t('create.cta')}
        />
      </div>

      {showList && (
        <div className="ds-home__panel">
          <ul className="ds-home__list" aria-label={t('listLabel')}>
            {items.map((item) => (
              <li key={item.id}>
                <CompetitionRow item={item} />
              </li>
            ))}
          </ul>
        </div>
      )}
    </section>
  );
}

function CompetitionRow({ item }: { item: CompetitionListItem }) {
  const { t, i18n } = useTranslation('competitions');
  const schedule = declaredSchedule(
    item.scheduledStart,
    item.scheduledEnd,
    toIntlLocale(i18n.language),
  );
  const scheduleLabel =
    schedule?.kind === 'both'
      ? t('schedule.both', { start: schedule.start, end: schedule.end })
      : schedule?.kind === 'start'
        ? t('schedule.from', { date: schedule.date })
        : schedule?.kind === 'end'
          ? t('schedule.until', { date: schedule.date })
          : null;

  return (
    <Link
      className="ds-interactive-row ds-home__row"
      to={`/competitions/${item.id}`}
    >
      <span className="ds-home__row-main">
        <TeamCrest name={item.name} logoMediaId={item.logoMediaId} size="md" />
        <span className="ds-home__row-copy">
          <span className="ds-home__row-name">{item.name}</span>
          {scheduleLabel ? (
            <span className="ds-home__row-meta">{scheduleLabel}</span>
          ) : null}
        </span>
      </span>
      <span className="ds-home__row-aside">
        <CompetitionStatusBadge status={item.status} />
        <span className="ds-interactive-row__chevron" aria-hidden="true">
          <ChevronRightIcon size="sm" />
        </span>
      </span>
    </Link>
  );
}

function CreateCompetitionDialog({
  open,
  onClose,
  returnFocusRef,
}: {
  open: boolean;
  onClose: () => void;
  returnFocusRef: RefObject<HTMLButtonElement | null>;
}) {
  const { t } = useTranslation('competitions');
  const { t: tCommon } = useTranslation('common');
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const formId = useId();
  const nameId = useId();
  const [name, setName] = useState('');

  useEffect(() => {
    if (open) {
      setName('');
    }
  }, [open]);

  const mutation = useMutation({
    mutationFn: () => createCompetition({ name: name.trim() }),
    onSuccess: async (created) => {
      await queryClient.invalidateQueries({
        queryKey: queryKeys.competitions.all,
      });
      void navigate(`/competitions/${created.id}/structure`);
    },
  });

  const trimmed = name.trim();
  const canSubmit =
    trimmed.length > 0 &&
    trimmed.length <= COMPETITION_NAME_MAX_LENGTH &&
    !mutation.isPending;

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('create.heading')}
      closeLabel={tCommon('close')}
      closeDisabled={mutation.isPending}
      returnFocusRef={returnFocusRef}
      size="sm"
      footer={
        <>
          <button
            type="button"
            className="ds-btn ds-btn--ghost"
            disabled={mutation.isPending}
            onClick={onClose}
          >
            {tCommon('cancel')}
          </button>
          <button
            type="submit"
            form={formId}
            className="ds-btn ds-btn--primary"
            disabled={!canSubmit}
          >
            {mutation.isPending ? (
              <PendingLabel>{t('create.submitting')}</PendingLabel>
            ) : (
              t('create.submit')
            )}
          </button>
        </>
      }
    >
      <form
        id={formId}
        className="ds-form"
        data-density="comfortable"
        onSubmit={(event: SubmitEvent) => {
          event.preventDefault();
          if (!canSubmit) {
            return;
          }
          mutation.mutate();
        }}
      >
        <Field
          label={t('create.nameLabel')}
          htmlFor={nameId}
          required
          counter={`${name.length}/${COMPETITION_NAME_MAX_LENGTH}`}
        >
          <TextInput
            id={nameId}
            value={name}
            onChange={(event) => setName(event.target.value)}
            disabled={mutation.isPending}
            placeholder={t('create.namePlaceholder')}
            maxLength={COMPETITION_NAME_MAX_LENGTH}
            required
            autoComplete="off"
            allowClear
          />
        </Field>

        {mutation.isError && <MutationError error={mutation.error} />}
      </form>
    </Dialog>
  );
}
