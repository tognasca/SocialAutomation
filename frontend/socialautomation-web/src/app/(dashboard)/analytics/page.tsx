'use client'

import { useEffect, useState } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import api from '@/lib/api'
import type { DashboardData } from '@/types/analytics'
import {
  BarChart3,
  Users,
  MessageSquare,
  Flame,
  Target,
  CheckCircle2,
  Bot,
  FileText,
  TrendingUp
} from 'lucide-react'

export default function AnalyticsPage() {
  const [period, setPeriod] = useState('30d')
  const [data, setData] = useState<DashboardData | null>(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    loadDashboard()
  }, [period])

  const loadDashboard = async () => {
    setLoading(true)
    try {
      const res = await api.get('/api/analytics/dashboard', { params: { period } })
      setData(res.data.data)
    } catch {
      setData(null)
    } finally {
      setLoading(false)
    }
  }

  const money = (v: number) =>
    new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(v || 0)

  const maxFunnel = Math.max(...(data?.funnel?.map(f => f.count) || [1]), 1)
  const maxSeries = Math.max(...(data?.timeSeries?.map(t => t.leads + t.messages) || [1]), 1)

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-4 flex-wrap">
        <div>
          <h1 className="text-3xl font-bold flex items-center gap-2">
            <BarChart3 className="h-8 w-8 text-primary" /> Analytics
          </h1>
          <p className="text-muted-foreground">
            Resultados de CRM, Inbox, IA, Campanhas e Conteúdo
          </p>
        </div>

        <div className="flex gap-2">
          {[
            { key: 'today', label: 'Hoje' },
            { key: '7d', label: '7 dias' },
            { key: '30d', label: '30 dias' },
            { key: '90d', label: '90 dias' },
          ].map(p => (
            <Button
              key={p.key}
              variant={period === p.key ? 'default' : 'outline'}
              size="sm"
              onClick={() => setPeriod(p.key)}
            >
              {p.label}
            </Button>
          ))}
        </div>
      </div>

      {loading && <div className="text-sm text-muted-foreground">Carregando métricas...</div>}

      {data && (
        <>
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            <MetricCard icon={Users} label="Novos Leads" value={data.newLeads} />
            <MetricCard icon={MessageSquare} label="Conversas" value={data.conversations} />
            <MetricCard icon={Flame} label="Leads Quentes" value={data.hotLeads} />
            <MetricCard icon={Target} label="Oportunidades" value={data.opportunities} />
            <MetricCard icon={CheckCircle2} label="Clientes" value={data.clients} />
            <MetricCard icon={Bot} label="Respostas IA" value={data.aiReplies} />
            <MetricCard icon={FileText} label="Conteúdos Publicados" value={data.contentPublished} />
            <MetricCard icon={TrendingUp} label="Conversão" value={`${data.conversionRate}%`} />
          </div>

          <div className="grid gap-4 lg:grid-cols-3">
            <Card className="lg:col-span-2">
              <CardHeader>
                <CardTitle>Atividade no período</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-2">
                  {data.timeSeries.map(point => (
                    <div key={point.date} className="grid grid-cols-12 gap-2 items-center text-xs">
                      <div className="col-span-2 text-muted-foreground">
                        {new Date(point.date).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' })}
                      </div>
                      <div className="col-span-10">
                        <div className="h-3 rounded bg-muted overflow-hidden flex">
                          <div
                            className="h-full bg-primary/80"
                            style={{ width: `${(point.leads / maxSeries) * 100}%` }}
                            title={`Leads: ${point.leads}`}
                          />
                          <div
                            className="h-full bg-emerald-500/80"
                            style={{ width: `${(point.messages / maxSeries) * 100}%` }}
                            title={`Mensagens: ${point.messages}`}
                          />
                        </div>
                      </div>
                    </div>
                  ))}
                  {data.timeSeries.length === 0 && (
                    <p className="text-sm text-muted-foreground">Sem dados no período.</p>
                  )}
                </div>
                <div className="mt-3 flex gap-4 text-xs text-muted-foreground">
                  <span className="flex items-center gap-1"><i className="h-2 w-2 rounded-full bg-primary inline-block" /> Leads</span>
                  <span className="flex items-center gap-1"><i className="h-2 w-2 rounded-full bg-emerald-500 inline-block" /> Mensagens</span>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Resumo financeiro do funil</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3 text-sm">
                <div className="flex justify-between">
                  <span>Valor no pipeline</span>
                  <strong>{money(data.totalPipelineValue)}</strong>
                </div>
                <div className="flex justify-between">
                  <span>Mensagens recebidas</span>
                  <strong>{data.messagesReceived}</strong>
                </div>
                <div className="flex justify-between">
                  <span>Mensagens enviadas</span>
                  <strong>{data.messagesSent}</strong>
                </div>
                <div className="flex justify-between">
                  <span>Handoffs humanos</span>
                  <strong>{data.humanHandoffs}</strong>
                </div>
                <div className="flex justify-between">
                  <span>Campanhas enviadas</span>
                  <strong>{data.campaignsSent}</strong>
                </div>
                <div className="flex justify-between">
                  <span>Follow-ups pendentes</span>
                  <strong>{data.followUpsPending}</strong>
                </div>
              </CardContent>
            </Card>
          </div>

          <div className="grid gap-4 lg:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle>Funil de vendas</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                {data.funnel.map(stage => (
                  <div key={stage.stage}>
                    <div className="flex justify-between text-sm mb-1">
                      <span className="font-medium">{stage.stage}</span>
                      <span className="text-muted-foreground">
                        {stage.count} • {money(stage.totalValue)}
                      </span>
                    </div>
                    <div className="h-2 rounded bg-muted overflow-hidden">
                      <div
                        className="h-full bg-primary"
                        style={{ width: `${(stage.count / maxFunnel) * 100}%` }}
                      />
                    </div>
                  </div>
                ))}
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Origem dos leads & score</CardTitle>
              </CardHeader>
              <CardContent className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <p className="text-xs font-semibold text-muted-foreground mb-2 uppercase">Origem</p>
                  <div className="space-y-2">
                    {data.sources.map(s => (
                      <div key={s.source} className="flex justify-between text-sm">
                        <span>{s.source}</span>
                        <strong>{s.count}</strong>
                      </div>
                    ))}
                    {data.sources.length === 0 && (
                      <p className="text-sm text-muted-foreground">Sem dados</p>
                    )}
                  </div>
                </div>
                <div>
                  <p className="text-xs font-semibold text-muted-foreground mb-2 uppercase">Score</p>
                  <div className="space-y-2">
                    {data.scoreDistribution.map(s => (
                      <div key={s.classification} className="flex justify-between text-sm">
                        <span>{s.classification}</span>
                        <strong>{s.count}</strong>
                      </div>
                    ))}
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>
        </>
      )}
    </div>
  )
}

function MetricCard({
  icon: Icon,
  label,
  value,
}: {
  icon: any
  label: string
  value: number | string
}) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium">{label}</CardTitle>
        <Icon className="h-4 w-4 text-muted-foreground" />
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold">{value}</div>
      </CardContent>
    </Card>
  )
}