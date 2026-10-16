export interface Conversation {
  id: string
  leadId: string
  leadName: string
  instagramUsername?: string
  leadProfileImage?: string
  assignedUserId?: string
  assignedUserName?: string
  mode: string
  status: string
  unreadCount: number
  lastMessagePreview?: string
  lastMessageAt: string
  scoreClassification?: string
}

export interface ChatMessage {
  id: string
  senderType: string
  senderId?: string
  content: string
  status: string
  createdAt: string
}