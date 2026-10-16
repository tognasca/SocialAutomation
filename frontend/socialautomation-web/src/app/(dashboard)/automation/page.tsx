'use client'

import { useEffect, useState } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import api from '@/lib/api'
import type { AutomationFlow } from '@/types/automation'
import { Zap, Plus, Play, Pause, Send } from 'lucide-react'
import Link from 'next/link'

export default function AutomationPage() {
  const [flows, setFlows] = useState<AutomationFlow[]>([])
  const [showModal, setShowModal] = useState(false)
  const [name, setName] = useState('')
  const [triggerType, setTriggerType] = useState('LeadCriado')

  useEffect(() => {
    loadFlows()
  }, [])

  const loadFlows = async () => {
    const res = await api.get('/api/automations')
    setFlows(res.data.data)
  }

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    await api.post('/api/automations', {
      name,
      triggerType,
      isActive: true,
      nodes: [
        { stepOrder: 0, nodeType: 'Action', actionOrCondition: 'CriarTarefaFollowUp', configJson: '{"title":"Follow-up Automático"}' }
      ]
    })
    setShowModal(false)
    setName('')
    loadFlows()
  }

  const toggleFlow = async (id: string, current: boolean) => {
    await api.put(`/api/automations/${id}/toggle`, { isActive: !current })
    loadFlows()
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold flex items-center gap-2">
            <Zap className="h-8 w-8 text-primary" /> Fluxos de Automação
          </h1>
          <p className="text-muted-foreground">Crie regras automáticas com Triggers, Conditions, Actions e Delays.</p>
        </div>
        <div className="flex gap-2">
          <Link href="/automation/campaigns">
            <Button variant="outline"><Send className="mr-2 h-4 w-4" /> Campanhas</Button>
          </Link>
          <Button onClick={() => setShowModal(true)}><Plus className="mr-2 h-4 w-4" /> Novo Fluxo</Button>
        </div>
      </div>

      {showModal && (
        <Card className="max-w-md">
          <CardHeader><CardTitle>Novo Fluxo de Automação</CardTitle></CardHeader>
          <CardContent>
            <form onSubmit={handleCreate} className="space-y-4">
              <div>
                <label className="text-sm font-medium">Nome da Automação</label>
                <Input value={name} onChange={e => setName(e.target.value)} required placeholder="Ex: Boas-vindas para Novos Leads" />
              </div>
              <div>
                <label className="text-sm font-medium">Gatilho (Trigger)</label>
                <select className="w-full border rounded p-2 text-sm" value={triggerType} onChange={e => setTriggerType(e.target.value)}>
                  <option value="LeadCriado">Novo Lead Criado</option>
                  <option value="MensagemRecebida">Nova Mensagem Recebida</option>
                  <option value="TagAdicionada">Tag Adicionada</option>
                  <option value="StatusAlterado">Status Alterado</option>
                </select>
              </div>
              <div className="flex justify-end gap-2">
                <Button type="button" variant="outline" onClick={() => setShowModal(false)}>Cancelar</Button>
                <Button type="submit">Criar Fluxo</Button>
              </div>
            </form>
          </CardContent>
        </Card>
      )}

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {flows.map(f => (
          <Card key={f.id} className="p-4 flex flex-col justify-between">
            <div>
              <div className="flex justify-between items-start mb-2">
                <h3 className="font-bold text-base">{f.name}</h3>
                <span className={`text-[10px] px-2 py-0.5 rounded font-bold ${f.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-600'}`}>
                  {f.isActive ? 'Ativo' : 'Pausado'}
                </span>
              </div>
              <p className="text-xs text-muted-foreground mb-4">Gatilho: <span className="font-semibold text-primary">{f.triggerType}</span></p>
            </div>
            <div className="flex justify-between items-center border-t pt-3 text-xs text-muted-foreground">
              <span>{f.executionCount} execuções</span>
              <Button size="sm" variant="ghost" onClick={() => toggleFlow(f.id, f.isActive)}>
                {f.isActive ? <Pause className="h-4 w-4 text-orange-600" /> : <Play className="h-4 w-4 text-green-600" />}
              </Button>
            </div>
          </Card>
        ))}
      </div>
    </div>
  )
}