/** i18n key under `structure` for a hub next-step code from `StructureHubNextStepDto`. */
export function structurePageHeadHubStepLabelKey(code: string): string {
  switch (code) {
    case 'PrepareStage':
      return 'readiness.hubStep.PrepareStage';
    case 'StartStage':
      return 'readiness.hubStep.StartStage';
    case 'CompleteStage':
      return 'readiness.hubStep.CompleteStage';
    case 'ResumeStage':
      return 'readiness.hubStep.ResumeStage';
    case 'ApplyDraw':
      return 'readiness.hubStep.ApplyDraw';
    case 'RunDraw':
      return 'readiness.hubStep.RunDraw';
    case 'MaterializeMatches':
    case 'MaterializeFromOccupiedSlots':
      return 'readiness.hubStep.MaterializeMatches';
    case 'BuildSchedule':
      return 'readiness.hubStep.BuildSchedule';
    case 'RunSwissRounds':
      return 'readiness.hubStep.RunSwissRounds';
    default:
      return 'readiness.hubStep.unknown';
  }
}
