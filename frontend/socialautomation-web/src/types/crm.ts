
export interface Tag {
  id: string
  name: string
  color: string
}

export interface Lead {
  id: string
  name: string
  instagramUsername?: string
  profileImage?: string
  phone?: string
  email?: string
  company?: string
  source: string
  status: string
  pipelineStage: string
  dealValue: number
  ownerName?: string
  score: number
  scoreClassification: string
  notes?: string
  lastInteractionAt?: string
  nextFollowUpAt?: string
  createdAt: string
  tags: Tag[]
}

export interface PipelineColumn {
  stage: string
  count: number
  totalValue: number
  leads: Lead[]
}

export interface LeadActivity {
  id: string
  userName: string
  type: string
  title: string
  description?: string
  createdAt: string
}

export interface CrmTask {
  id: string
  leadId: string
  leadName?: string
  assignedUserName?: string
  title: string
  description?: string
  dueDate: string
  status: string
  priority: string
  completedAt?: string
  createdAt: string
}