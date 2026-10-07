import { Link } from 'react-router-dom'
import './PermissionDeniedPage.css'

interface PermissionDeniedPageProps {
  /** Message from the failed request, when there is one worth showing. */
  message?: string
}

export default function PermissionDeniedPage({ message }: PermissionDeniedPageProps) {
  return (
    <section className="permission-denied" role="alert">
      <p className="permission-denied-status">403</p>
      <h2 className="permission-denied-title">Permission denied</h2>
      <p className="permission-denied-message">
        {message ?? 'Your account does not have the role required to view this page.'}
      </p>
      <Link className="permission-denied-link" to="/projects">
        Back to projects
      </Link>
    </section>
  )
}
