import { createBrowserRouter, Navigate } from 'react-router-dom'
import { lazy } from 'react'
const AppLayout = lazy(() =>
  import('../components/layout/AppLayout').then((module) => ({ default: module.AppLayout })),
)
const WelcomePage = lazy(() => import('../pages/WelcomePage'))
const OnboardingPage = lazy(() => import('../pages/OnboardingPage'))
const DashboardPage = lazy(() => import('../pages/DashboardPage'))
const ResultsPage = lazy(() => import('../pages/ResultsPage'))
const HistoryPage = lazy(() => import('../pages/HistoryPage'))
const PhysicsPage = lazy(() => import('../pages/PhysicsPage'))
const ProfilePage = lazy(() => import('../pages/ProfilePage'))
import { Protected } from '../components/layout/Protected'
import { EmptyState } from '../components/ui/states'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <WelcomePage />,
    errorElement: (
      <EmptyState
        title="Не удалось открыть страницу"
        text="Обновите страницу или вернитесь на главную."
      >
        <a href="/">На главную</a>
      </EmptyState>
    ),
  },
  { path: '/onboarding', element: <OnboardingPage /> },
  {
    element: <AppLayout />,
    children: [
      { path: '/physics', element: <PhysicsPage /> },
      {
        element: <Protected />,
        children: [
          { path: '/dashboard', element: <DashboardPage /> },
          { path: '/results', element: <ResultsPage /> },
          { path: '/history', element: <HistoryPage /> },
          { path: '/profile', element: <ProfilePage /> },
        ],
      },
    ],
  },
  { path: '*', element: <Navigate to="/" replace /> },
])
