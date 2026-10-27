import { lazy, Suspense, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Dialog } from '../../design-system/components/Dialog';
import { WaitMark } from '../../design-system/components/WaitMark';
import type { StructureDrawDialogProps } from './StructureDrawDialog';

const StructureDrawDialog = lazy(() =>
  import('./StructureDrawDialog').then((module) => ({
    default: module.StructureDrawDialog,
  })),
);

/**
 * The draw dialog is off the Structure first paint. Load it on the first open
 * and keep it mounted afterwards so the dialog exit transition still runs.
 */
export function StructureDrawDialogHost(props: StructureDrawDialogProps) {
  const [keepMounted, setKeepMounted] = useState(props.open);
  if (props.open && !keepMounted) {
    setKeepMounted(true);
  }

  if (!props.open && !keepMounted) {
    return null;
  }

  return (
    <Suspense
      fallback={
        props.open ? (
          <DrawDialogChunkFallback
            stageName={props.stage.name}
            onClose={props.onClose}
          />
        ) : null
      }
    >
      <StructureDrawDialog {...props} />
    </Suspense>
  );
}

function DrawDialogChunkFallback({
  stageName,
  onClose,
}: {
  stageName: string;
  onClose: () => void;
}) {
  const { t } = useTranslation('structure');
  const { t: tCommon } = useTranslation('common');

  return (
    <Dialog
      open
      onClose={onClose}
      title={t('fiche.drawWorkflow.title')}
      description={stageName}
      size="lg"
      closeLabel={tCommon('close')}
    >
      <WaitMark size="region">{t('fiche.drawWorkflow.loading')}</WaitMark>
    </Dialog>
  );
}
