import { AudioLines } from 'lucide-react'
import { Link } from 'react-router-dom'
export function Brand() {
  return (
    <Link to="/" className="brand" aria-label="ContextIQ — главная">
      <span className="brand-icon">
        <AudioLines size={23} />
      </span>
      <span>
        Context<span className="brand-iq">IQ</span>
        <span className="brand-dot">.</span>
      </span>
    </Link>
  )
}
