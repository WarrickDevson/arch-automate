/**
 * IFC file persistence via Supabase Storage (bucket: "ifc-models").
 *
 * Path convention inside the bucket:
 *   {tenantId}/{projectId}/model.ifc
 *
 * IndexedDB is used as a client-side cache so the file is only downloaded
 * once per device; subsequent opens are instant.
 */

import { supabase } from '@/utils/supabase'
import { saveIfc, loadIfc } from '@/lib/ifcCache'

const BUCKET = 'ifc-models'

/**
 * Upload an IFC ArrayBuffer to Supabase Storage and return the storage path.
 * Always overwrites (upsert) so re-uploading a revised file just replaces it.
 *
 * @param {string} tenantId
 * @param {string} projectId
 * @param {ArrayBuffer} buffer  Raw bytes of the .ifc file
 * @returns {Promise<string>}   The storage path, e.g. "{tenantId}/{projectId}/model.ifc"
 */
export async function uploadIfc(tenantId, projectId, buffer) {
  const path = `${tenantId}/${projectId}/model.ifc`
  const blob = new Blob([buffer], { type: 'application/octet-stream' })

  const { error } = await supabase.storage.from(BUCKET).upload(path, blob, {
    upsert: true,
    contentType: 'application/octet-stream',
  })

  if (error) throw new Error(`IFC upload failed: ${error.message}`)

  // Cache locally so the next open doesn't need a network round-trip
  await saveIfc(projectId, buffer).catch(() => {})

  return path
}

/**
 * Download an IFC file from Supabase Storage for the given storage path.
 * Checks the local IndexedDB cache first; only downloads if the cache is cold.
 *
 * @param {string} projectId   Used as the cache key
 * @param {string} storagePath The path returned by uploadIfc / stored in the DB
 * @returns {Promise<ArrayBuffer>}
 */
export async function downloadIfc(projectId, storagePath) {
  // 1. Cache hit → skip the network entirely
  const cached = await loadIfc(projectId).catch(() => null)
  if (cached) return cached

  // 2. Download from Supabase Storage
  const { data, error } = await supabase.storage.from(BUCKET).download(storagePath)
  if (error) throw new Error(`IFC download failed: ${error.message}`)

  const buffer = await data.arrayBuffer()

  // 3. Warm the cache for next time
  await saveIfc(projectId, buffer).catch(() => {})

  return buffer
}
