import { render } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import { StructureDiagnosticIssueIcon } from './StructureDiagnosticIssueIcon';

describe('StructureDiagnosticIssueIcon', () => {
  it('uses distinct icons per diagnostic tone', () => {
    const { container: danger } = render(
      <StructureDiagnosticIssueIcon tone="danger" />,
    );
    const { container: attention } = render(
      <StructureDiagnosticIssueIcon tone="attention" />,
    );
    const { container: progress } = render(
      <StructureDiagnosticIssueIcon tone="progress" />,
    );

    expect(danger.querySelector('.lucide-circle-alert')).toBeInTheDocument();
    expect(attention.querySelector('.lucide-triangle-alert')).toBeInTheDocument();
    expect(progress.querySelector('.lucide-package-open')).toBeInTheDocument();
  });
});
