export interface DashboardData {
  from: string
  to: string
  period: string
  newLeads: number
  conversations: number
  hotLeads: number
  opportunities: number
  clients: number
  followUpsPending: number
  messagesReceived: number
  messagesSent: number
  aiReplies: number
  humanHandoffs: number
  campaignsSent: number
  contentPublished: number
  conversionRate: number
  totalPipelineValue: number
  funnel: { stage: string; count: number; totalValue: number }[]
  timeSeries: { date: string; leads: number; conversations: number; messages: number; clients: number }[]
  sources: { source: string; count: number }[]
  scoreDistribution: { classification: string; count: number }[]
}