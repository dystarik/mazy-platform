import type { components } from './schema'

export type GetScenarioDraftResponse     = components['schemas']['GetScenarioDraftResponse']
export type GetReleasedScenarioResponse  = components['schemas']['GetReleasedScenarioResponse']
export type GetScenarioByVersionResponse = components['schemas']['GetScenarioByVersionResponse']
export type UpdateScenarioDraftRequest   = components['schemas']['UpdateScenarioDraftRequest']
export type PromoteToReleaseRequest      = components['schemas']['PromoteToReleaseRequest']
export type RollbackReleaseRequest       = components['schemas']['RollbackReleaseRequest']
export type GetVersionHistoryResponse    = components['schemas']['GetVersionHistoryResponse']
export type VersionItem                  = components['schemas']['VersionItem']
