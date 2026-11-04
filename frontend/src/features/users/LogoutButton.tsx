import { useNavigate } from 'react-router-dom'
import { logout } from '../../services/session'

export default function LogoutButton() {
  const navigate = useNavigate()

  const handleLogout = () => {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <button className="logout-button" onClick={handleLogout}>
      Log out
    </button>
  )
}
