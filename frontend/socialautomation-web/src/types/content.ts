export interface ReelScene {
  sceneOrder: number
  scriptText: string
  visualDescription?: string
  durationSeconds: number
}

export interface ContentItem {
  id: string
  type: string
  title: string
  caption?: string
  hashtags: string[]
  cta?: string
  mediaUrls: string[]
  mediaFormat: string
  status: string
  scheduledAt?: string
  publishedAt?: string
  errorMessage?: string
  createdAt: string
  scenes: ReelScene[]
}

export interface GeneratedAiContent {
  title: string
  caption: string
  cta: string
  hashtags: string[]
  visualSuggestion: string
  reelScenes: ReelScene[]
}