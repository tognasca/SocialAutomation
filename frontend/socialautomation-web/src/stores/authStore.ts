// frontend/socialautomation-web/src/stores/authStore.ts

import { create } from 'zustand'
import api from '@/lib/api'
import type { UserInfo, LoginRequest, RegisterRequest, ApiResponse, LoginResponse } from '@/types/auth'

interface AuthState {
  user: UserInfo | null
  isAuthenticated: boolean
  isLoading: boolean
  login: (data: LoginRequest) => Promise<void>
  register: (data: RegisterRequest) => Promise<void>
  logout: () => Promise<void>
  loadUser: () => void
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isAuthenticated: false,
  isLoading: true,

  login: async (data: LoginRequest) => {
    const response = await api.post<ApiResponse<LoginResponse>>('/api/auth/login', data)
    const { accessToken, refreshToken, user } = response.data.data!

    localStorage.setItem('accessToken', accessToken)
    localStorage.setItem('refreshToken', refreshToken)
    localStorage.setItem('user', JSON.stringify(user))

    set({ user, isAuthenticated: true, isLoading: false })
  },

  register: async (data: RegisterRequest) => {
    const response = await api.post<ApiResponse<LoginResponse>>('/api/auth/register', data)
    const { accessToken, refreshToken, user } = response.data.data!

    localStorage.setItem('accessToken', accessToken)
    localStorage.setItem('refreshToken', refreshToken)
    localStorage.setItem('user', JSON.stringify(user))

    set({ user, isAuthenticated: true, isLoading: false })
  },

  logout: async () => {
    try {
      await api.post('/api/auth/logout')
    } catch {
      // ignore
    }
    localStorage.removeItem('accessToken')
    localStorage.removeItem('refreshToken')
    localStorage.removeItem('user')
    set({ user: null, isAuthenticated: false, isLoading: false })
  },

  loadUser: () => {
    if (typeof window !== 'undefined') {
      const userStr = localStorage.getItem('user')
      const token = localStorage.getItem('accessToken')

      if (userStr && token) {
        set({ user: JSON.parse(userStr), isAuthenticated: true, isLoading: false })
      } else {
        set({ isLoading: false })
      }
    }
  },
}))