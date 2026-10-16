export interface AutomationFlow {
  id: string
  name: string
  description?: string
  triggerType: string
  isActive: boolean
  executionCount: number
  createdAt: string
}

export interface Campaign {
  id: string
  name: string
  messageTemplate: string
  status: string
  totalAudience: number
  sentCount: number
  failedCount: number
  createdAt: string
}