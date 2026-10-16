
export interface InstagramAccount {
  id: string
  instagramBusinessId: string
  pageName: string
  username: string
  name?: string
  profilePictureUrl?: string
  status: string
  webhookSubscribed: boolean
  tokenExpiresAt?: string
  createdAt: string
}
React