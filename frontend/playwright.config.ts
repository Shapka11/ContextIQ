import { defineConfig } from '@playwright/test'
import path from 'node:path'

export default defineConfig({
  testDir: './e2e',
  workers: 1,
  retries: 0,
  reporter: 'list',
  use: {
    baseURL: process.env.E2E_BASE_URL || 'http://localhost:5173',
    viewport: { width: 1440, height: 1100 },
    reducedMotion: 'reduce',
    permissions: ['microphone'],
    launchOptions: {
      args: [
        '--use-fake-ui-for-media-stream',
        '--use-fake-device-for-media-stream',
        `--use-file-for-fake-audio-capture=${path.resolve('e2e/fixtures/synthetic_440hz.wav')}`,
      ],
    },
  },
})
