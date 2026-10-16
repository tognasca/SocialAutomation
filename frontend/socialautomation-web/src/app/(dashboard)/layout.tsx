
'use client'

import { useAuth } from '@/hooks/useAuth'
import MainLayout from '@/components/layout/MainLayout'

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  const { isLoading, isAuthenticated } = useAuth()

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="text-muted-foreground">Carregando...</div>
      </div>
    )
  }

  if (!isAuthenticated) {
    return null
  }

  return <MainLayout>{children}</MainLayout>
}