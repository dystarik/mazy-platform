import type { components } from './schema'

// ── Enums ─────────────────────────────────────────────────────────────────────
// Bot service has its OWN PlatformType enum (BotPlatformType), distinct from
// the scenario service's PlatformType — values are even prefixed differently
// ("Bot_PLATFORM_TYPE_VK" vs "PLATFORM_TYPE_VK"). Don't conflate them.
export type BotPlatformType = components['schemas']['BotPlatformType']
export type BotStatus       = components['schemas']['BotStatus']

// ── Domain ────────────────────────────────────────────────────────────────────
export type BotListItem = components['schemas']['BotListItem']

// ── Requests / Responses ──────────────────────────────────────────────────────
export type CreateBotRequest                = components['schemas']['CreateBotRequest']
export type CreateBotWithoutProjectRequest  = components['schemas']['CreateBotWithoutProjectRequest']
export type CreateBotResponse               = components['schemas']['CreateBotResponse']
export type GetBotResponse                  = components['schemas']['GetBotResponse']
export type GetBotsByProjectResponse        = components['schemas']['GetBotsByProjectResponse']
export type GetBotsByUserIdResponse         = components['schemas']['GetBotsByUserIdResponse']
export type ActivateBotRequest              = components['schemas']['ActivateBotRequest']
export type DeactivateBotRequest            = components['schemas']['DeactivateBotRequest']
export type BindBotToProjectRequest         = components['schemas']['BindBotToProjectRequest']
export type UnbindBotFromProjectRequest     = components['schemas']['UnbindBotFromProjectRequest']
export type UpdateBotTokenRequest           = components['schemas']['UpdateBotTokenRequest']
export type ChangeBotScenarioVersionRequest = components['schemas']['ChangeBotScenarioVersionRequest']
