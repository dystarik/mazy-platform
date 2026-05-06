import type { components } from './schema'

export type GetUserDataRecordsResponse = components['schemas']['GetUserDataRecordsResponse']
export type UserDataRecordItem = components['schemas']['UserDataRecordItem']

export interface GetUserDataRecordsParams {
  schemaId?: string
  scenarioVersion?: number
  botId?: string
  platformUserId?: string
  includeArchived?: boolean
  pageSize?: number
  pageOffset?: number
}
