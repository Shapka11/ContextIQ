import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import type { MoodEntry } from '../../types/api'
import { dateLabel } from '../../lib/utils'
export function MoodCharts({ entries }: { entries: MoodEntry[] }) {
  const data = entries.map((entry) => ({
    date: dateLabel(entry.createdAt),
    valence: entry.analysis.valence,
    energy: entry.analysis.energy,
  }))
  return (
    <div className="chart-grid">
      {[
        {
          key: 'valence',
          title: 'Эмоциональная окраска',
          sub: 'От −1 (негативная) до +1 (позитивная)',
          color: '#a28bfa',
          domain: [-1, 1],
        },
        {
          key: 'energy',
          title: 'Уровень энергии',
          sub: 'От 0 (низкая) до 1 (высокая)',
          color: '#6acbc6',
          domain: [0, 1],
        },
      ].map((chart) => (
        <section className="panel chart-panel" key={chart.key}>
          <div className="section-heading">
            <h3>{chart.title}</h3>
            <span className="chart-dot" style={{ background: chart.color }} />
          </div>
          <p className="field-hint">{chart.sub}</p>
          <div
            className="chart-container"
            role="img"
            aria-label={`${chart.title}: ${entries.length} записей`}
          >
            <ResponsiveContainer width="100%" height="100%" minWidth={0}>
              <LineChart data={data} margin={{ top: 12, right: 16, bottom: 6, left: -28 }}>
                <CartesianGrid vertical={false} stroke="var(--line)" />
                <XAxis
                  dataKey="date"
                  tick={{ fill: 'var(--muted)', fontSize: 10 }}
                  tickLine={false}
                  axisLine={false}
                  minTickGap={40}
                />
                <YAxis
                  domain={chart.domain}
                  tick={{ fill: 'var(--muted)', fontSize: 10 }}
                  tickLine={false}
                  axisLine={false}
                />
                <Tooltip
                  contentStyle={{
                    background: 'var(--panel)',
                    border: '1px solid var(--line)',
                    borderRadius: 12,
                    color: 'var(--text)',
                  }}
                />
                <Line
                  name={chart.title}
                  type="linear"
                  dataKey={chart.key}
                  stroke={chart.color}
                  strokeWidth={2.5}
                  dot={{ r: 4, fill: chart.color, strokeWidth: 0 }}
                  activeDot={{ r: 6 }}
                  isAnimationActive={false}
                />
              </LineChart>
            </ResponsiveContainer>
          </div>
          <details className="chart-table">
            <summary>Точные значения</summary>
            <table>
              <thead>
                <tr>
                  <th>Дата</th>
                  <th>{chart.title}</th>
                </tr>
              </thead>
              <tbody>
                {entries.map((entry, i) => (
                  <tr key={entry.id}>
                    <td>{data[i].date}</td>
                    <td>
                      {chart.key === 'valence' ? entry.analysis.valence : entry.analysis.energy}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </details>
        </section>
      ))}
    </div>
  )
}
