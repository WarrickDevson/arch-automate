import apiClient from './apiClient'

const STATUS_MAP = {
  0: 'Open',
  1: 'InProgress',
  2: 'Resolved',
  3: 'Disputed',
}

const CATEGORY_MAP = {
  0: 'Zoning',
  1: 'BuildingLines',
  2: 'Parking',
  3: 'Accessibility',
  4: 'StructuralDocumentation',
  5: 'FireCompliance',
  6: 'Drainage',
  7: 'Other',
}

function normaliseEnum(value, map, fallback) {
  if (typeof value === 'number') return map[value] ?? fallback
  if (typeof value === 'string' && value.trim()) return value
  return fallback
}

function normalise(row) {
  return {
    id: row.Id ?? row.id,
    projectId: row.ProjectId ?? row.projectId,
    sourceDocument: row.SourceDocument ?? row.sourceDocument ?? '',
    clauseReference: row.ClauseReference ?? row.clauseReference ?? '',
    commentText: row.CommentText ?? row.commentText ?? '',
    parsedAction: row.ParsedAction ?? row.parsedAction ?? '',
    category: normaliseEnum(row.Category ?? row.category, CATEGORY_MAP, 'Other'),
    status: normaliseEnum(row.Status ?? row.status, STATUS_MAP, 'Open'),
    receivedAt: row.ReceivedAt ?? row.receivedAt,
    resolvedAt: row.ResolvedAt ?? row.resolvedAt ?? null,
  }
}

export const rejectionsService = {
  async getProjectRejections(projectId) {
    const { data } = await apiClient.get(`/rejections/projects/${projectId}/rejections`)
    return (data ?? []).map(normalise)
  },

  async updateStatus(projectId, rejectionId, status) {
    const { data } = await apiClient.patch(
      `/rejections/projects/${projectId}/rejections/${rejectionId}/status`,
      { status },
    )
    return normalise(data)
  },
}
