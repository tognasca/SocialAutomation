export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  tenantName: string
  name: string
  email: string
  password: string
  confirmPassword: string
  phone?: string
}

export interface UserInfo {
  id: string
  name: string
  email: string
  avatarUrl?: string
  tenantId: string
  tenantName: string
  roles: string[]
  permissions: string[]
}

export interface LoginResponse {
  accessToken: string
  refreshToken: string
  expiresIn: number
  user: UserInfo
}

export interface ApiResponse<T> {
  success: boolean
  data?: T
  error?: string
}