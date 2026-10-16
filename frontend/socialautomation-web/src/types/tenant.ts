// frontend/socialautomation-web/src/types/tenant.ts

export interface Tenant {
  id: string
  name: string
  slug: string
  document?: string
  email?: string
  phone?: string
  plan: string
  status: string
  maxUsers: number
  maxInstagramAccounts: number
  createdAt: string
}