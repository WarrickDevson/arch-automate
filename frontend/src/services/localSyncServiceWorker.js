// Minimal mock for local service worker registration
export async function registerLocalSyncWorker() {
  if ('serviceWorker' in navigator) {
    try {
      // Mock result
      return Promise.resolve()
    } catch (e) {
      console.warn('Local Sync SW registration failed', e)
    }
  }
}
