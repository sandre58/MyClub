import { Component, type ErrorInfo, type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { CheckIcon } from '../design-system/icons/contentIcons';
import i18n from '../i18n';

type PageErrorBoundaryProps = {
  children: ReactNode;
};

type PageErrorBoundaryState = {
  hasError: boolean;
};

/**
 * Contains render failures inside the page Outlet so shell chrome stays usable.
 */
export class PageErrorBoundary extends Component<
  PageErrorBoundaryProps,
  PageErrorBoundaryState
> {
  state: PageErrorBoundaryState = { hasError: false };

  static getDerivedStateFromError(): PageErrorBoundaryState {
    return { hasError: true };
  }

  componentDidCatch(error: Error, info: ErrorInfo): void {
    console.error('Page render failed', error, info.componentStack);
  }

  private handleRetry = () => {
    this.setState({ hasError: false });
  };

  render() {
    if (!this.state.hasError) {
      return this.props.children;
    }

    const t = i18n.getFixedT(null, 'common');

    return (
      <main id="main" className="shell-page">
        <header className="ds-group">
          <p className="ds-label">{t('unexpectedError.eyebrow')}</p>
          <h1 className="ds-heading">{t('unexpectedError.title')}</h1>
          <p className="ds-body">{t('unexpectedError.lede')}</p>
        </header>
        <p className="ds-group">
          <button
            type="button"
            className="ds-btn ds-btn--primary"
            onClick={this.handleRetry}
          >
            <CheckIcon size="sm" />
            {t('unexpectedError.retry')}
          </button>
          <Link className="ds-btn ds-btn--ghost" to="/">
            {t('unexpectedError.home')}
          </Link>
        </p>
      </main>
    );
  }
}
