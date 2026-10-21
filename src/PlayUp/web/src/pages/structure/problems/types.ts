export type StructureProblemSource =
  'lifecycle.anticipated' | 'lifecycle.mutation.api';

export type NormalizedLifecycleAnticipated = {
  source: 'lifecycle.anticipated';
  wireCode: string;
  messageKey: string;
};

export type NormalizedLifecycleMutation = {
  source: StructureProblemSource;
  wireCode: string | null;
  messageKey: string;
};
