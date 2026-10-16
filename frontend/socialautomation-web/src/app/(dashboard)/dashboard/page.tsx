
'use client'

import { useAuthStore } from '@/stores/authStore'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Users, MessageSquare, TrendingUp, Target } from 'lucide-react'

const stats = [
  { label: 'Leads', value: '0', icon: Users, color: 'text-blue-600' },
  { label: 'Conversas', value: '0', icon: MessageSquare, color: 'text-green-600' },
  { label: 'Leads Quentes', value: '0', icon: TrendingUp, color: 'text-orange-600' },
  { label: 'Oportunidades', value: '0', icon: Target, color: 'text-purple-600' },
]

export default function DashboardPage() {
  const { user } = useAuthStore()

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Dashboard</h1>
        <p className="text-muted-foreground">
          Bem-vindo, {user?.name}!
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {stats.map((stat) => {
          const Icon = stat.icon
          return (
            <Card key={stat.label}>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">{stat.label}</CardTitle>
                <Icon className={`h-4 w-4 ${stat.color}`} />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{stat.value}</div>
              </CardContent>
            </Card>
          )
        })}
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Primeiros passos</CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          <div className="flex items-center gap-3 rounded-lg border p-3">
            <div className="flex h-8 w-8 items-center justify-center rounded-full bg-primary/10 text-sm font-bold text-primary">1</div>
            <div>
              <p className="font-medium">Conecte seu Instagram</p>
              <p className="text-sm text-muted-foreground">Em breve — Fase 2</p>
            </div>
          </div>
          <div className="flex items-center gap-3 rounded-lg border p-3">
            <div className="flex h-8 w-8 items-center justify-center rounded-full bg-primary/10 text-sm font-bold text-primary">2</div>
            <div>
              <p className="font-medium">Configure seu agente de IA</p>
              <p className="text-sm text-muted-foreground">Em breve — Fase 5</p>
            </div>
          </div>
          <div className="flex items-center gap-3 rounded-lg border p-3">
            <div className="flex h-8 w-8 items-center justify-center rounded-full bg-primary/10 text-sm font-bold text-primary">3</div>
            <div>
              <p className="font-medium">Comece a receber leads</p>
              <p className="text-sm text-muted-foreground">Em breve — Fase 3</p>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}