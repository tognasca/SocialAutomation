
'use client'

import { useEffect, useState } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import api from '@/lib/api'
import type { PipelineColumn, Lead } from '@/types/crm'
import { Kanban, Instagram, ArrowRight, ArrowLeft } from 'lucide-react'
import Link from 'next/link'

export default function PipelineKanbanPage() {
  const [board, setBoard] = useState<PipelineColumn[]>([])
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    loadBoard()
  }, [])

  const loadBoard = async () => {
    setLoading(true)
    try {
      const res = await api.get('/api/pipeline/board')
      setBoard(res.data.data)
    } catch {
      // handle error
    } finally {
      setLoading(false)
    }
  }

  const handleMoveStage = async (leadId: string, currentStageIndex: number, direction: 'next' | 'prev') => {
    const nextIndex = direction === 'next' ? currentStageIndex + 1 : currentStageIndex - 1
    if (nextIndex < 0 || nextIndex >= board.length) return

    const targetStage = board[nextIndex].stage

    try {
      await api.put(`/api/leads/${leadId}/stage`, {
        stage: targetStage,
        order: 0
      })
      loadBoard()
    } catch {
      alert('Erro ao mover lead.')
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold flex items-center gap-2">
            <Kanban className="h-8 w-8 text-primary" />
            Funil de Vendas (Kanban)
          </h1>
          <p className="text-muted-foreground">Arraste ou mova os leads entre as etapas do pipeline.</p>
        </div>
        <Link href="/crm/leads">
          <Button variant="outline">Ver em Lista</Button>
        </Link>
      </div>

      <div className="flex gap-4 overflow-x-auto pb-4">
        {board.map((col, idx) => (
          <div key={col.stage} className="min-w-[300px] max-w-[300px] flex-shrink-0 bg-muted/40 rounded-xl p-3 border flex flex-col h-[78vh]">
            <div className="flex justify-between items-center mb-3 px-1">
              <h3 className="font-bold text-sm">{col.stage}</h3>
              <span className="bg-background px-2 py-0.5 rounded-full text-xs font-semibold shadow-sm">
                {col.count}
              </span>
            </div>
            <div className="text-xs text-muted-foreground mb-3 px-1 font-medium">
              Total: {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(col.totalValue)}
            </div>

            <div className="space-y-3 overflow-y-auto flex-1 pr-1">
              {col.leads.map((lead) => (
                <Card key={lead.id} className="p-3 shadow-sm hover:shadow transition-shadow bg-background">
                  <div className="flex justify-between items-start mb-2">
                    <Link href={`/crm/leads/${lead.id}`} className="font-semibold text-sm hover:text-primary">
                      {lead.name}
                    </Link>
                    <span className="text-xs font-bold text-green-700 bg-green-50 px-1.5 py-0.5 rounded">
                      {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL', maximumFractionDigits: 0 }).format(lead.dealValue)}
                    </span>
                  </div>

                  {lead.instagramUsername && (
                    <div className="flex items-center gap-1 text-xs text-pink-600 mb-2">
                      <Instagram className="h-3 w-3" /> @{lead.instagramUsername}
                    </div>
                  )}

                  <div className="flex flex-wrap gap-1 mb-3">
                    {lead.tags.map((t) => (
                      <span key={t.id} className="text-[9px] px-1.5 py-0.5 rounded font-medium" style={{ backgroundColor: `${t.color}20`, color: t.color }}>
                        {t.name}
                      </span>
                    ))}
                  </div>

                  <div className="flex justify-between items-center pt-2 border-t text-xs">
                    <Button
                      size="sm"
                      variant="ghost"
                      className="h-7 px-2"
                      disabled={idx === 0}
                      onClick={() => handleMoveStage(lead.id, idx, 'prev')}
                    >
                      <ArrowLeft className="h-3 w-3" />
                    </Button>
                    <span className="text-[10px] text-muted-foreground">{lead.scoreClassification}</span>
                    <Button
                      size="sm"
                      variant="ghost"
                      className="h-7 px-2"
                      disabled={idx === board.length - 1}
                      onClick={() => handleMoveStage(lead.id, idx, 'next')}
                    >
                      <ArrowRight className="h-3 w-3" />
                    </Button>
                  </div>
                </Card>
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}