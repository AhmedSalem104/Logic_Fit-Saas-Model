import { Component, type ErrorInfo, type ReactNode } from 'react';
import { ErrorState } from './ui';

type Props = { children: ReactNode };
type State = { hasError: boolean };

export class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false };

  static getDerivedStateFromError(): State {
    return { hasError: true };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    // Do not log component props or request/session material.
    console.error('logicfit.web.render_error', { name: error.name, componentStack: info.componentStack });
  }

  render() {
    return this.state.hasError ? <ErrorState message="تعذر عرض الواجهة. أعد تحميل الصفحة." /> : this.props.children;
  }
}
