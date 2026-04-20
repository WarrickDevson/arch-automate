/**
 * Caches IFC file ArrayBuffers in IndexedDB, keyed by project ID.
 * This lets Viewer3D auto-restore the last loaded model when the user
 * navigates away and returns — without needing a server upload.
 */

const DB_NAME = 'arch-automate-ifc'
const STORE = 'models'
const VERSION = 1

function openDb() {
  return new Promise((resolve, reject) => {
    const req = indexedDB.open(DB_NAME, VERSION)
    req.onupgradeneeded = (e) => {
      e.target.result.createObjectStore(STORE)
    }
    req.onsuccess = (e) => resolve(e.target.result)
    req.onerror = (e) => reject(e.target.error)
  })
}

export async function saveIfc(projectId, arrayBuffer) {
  const db = await openDb()
  return new Promise((resolve, reject) => {
    const tx = db.transaction(STORE, 'readwrite')
    tx.objectStore(STORE).put(arrayBuffer, projectId)
    tx.oncomplete = () => resolve()
    tx.onerror = (e) => reject(e.target.error)
  })
}

export async function loadIfc(projectId) {
  const db = await openDb()
  return new Promise((resolve, reject) => {
    const tx = db.transaction(STORE, 'readonly')
    const req = tx.objectStore(STORE).get(projectId)
    req.onsuccess = (e) => resolve(e.target.result ?? null)
    req.onerror = (e) => reject(e.target.error)
  })
}

export async function deleteIfc(projectId) {
  const db = await openDb()
  return new Promise((resolve, reject) => {
    const tx = db.transaction(STORE, 'readwrite')
    tx.objectStore(STORE).delete(projectId)
    tx.oncomplete = () => resolve()
    tx.onerror = (e) => reject(e.target.error)
  })
}
