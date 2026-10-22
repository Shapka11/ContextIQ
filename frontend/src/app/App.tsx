import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { RouterProvider } from 'react-router-dom'
import { MotionConfig } from 'motion/react'
import { Toaster } from 'sonner'
import { router } from './router'
import { ApiError } from '../api/http'
import { Suspense } from 'react'
import { Skeleton } from '../components/ui/states'
const client = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: (count, error) =>
        count < 1 && (!(error instanceof ApiError) || ![401, 403, 404, 429].includes(error.status)),
      refetchOnWindowFocus: false,
    },
    mutations: { retry: false },
  },
})
export default function App() {
  return (
    <QueryClientProvider client={client}>
      <MotionConfig reducedMotion="user">
        <Suspense
          fallback={
            <div className="main-content">
              <Skeleton count={2} />
            </div>
          }
        >
          <RouterProvider router={router} />
        </Suspense>
        <Toaster
          position="bottom-right"
          closeButton
          containerAriaLabel="Уведомления"
          toastOptions={{
            closeButtonAriaLabel: 'Закрыть уведомление',
            style: { background: 'var(--panel)', color: 'var(--text)', borderColor: 'var(--line)' },
          }}
        />
      </MotionConfig>
    </QueryClientProvider>
  )
}
