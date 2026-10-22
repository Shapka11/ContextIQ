export function Orb({ small = false }: { small?: boolean }) {
  return (
    <div className={`orb-scene ${small ? 'orb-small' : ''}`} aria-hidden="true">
      <div className="orb-orbit orbit-one" />
      <div className="orb-orbit orbit-two" />
      <div className="orb">
        <div className="orb-core" />
      </div>
      <span className="orb-star star-one" />
      <span className="orb-star star-two" />
      <span className="orb-star star-three" />
    </div>
  )
}
