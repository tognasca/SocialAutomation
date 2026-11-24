'use client'

import { useEffect, useState } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import api from '@/lib/api'
import { isAxiosError } from 'axios'
import { Bot, BookOpen, Save } from 'lucide-react'

type Provider = 'openai' | 'ollama'

type Agent = {
  id: string
  name: string
  isDefault: boolean
  modelProvider: string
  modelName: string
  objective?: string
  companyName?: string
  productsServices?: string
  instructions?: string
  handoffRules?: string
}

const defaultModels: Record<Provider, string> = { openai: 'gpt-4o-mini', ollama: 'qwen2.5:3b' }

export default function AiPage() {
  const [agents, setAgents] = useState<Agent[]>([])
  const [docs, setDocs] = useState<any[]>([])

  const [name, setName] = useState('Assistente Comercial')
  const [objective, setObjective] = useState('Qualificar leads e agendar atendimento')
  const [companyName, setCompanyName] = useState('')
  const [products, setProducts] = useState('')
  const [instructions, setInstructions] = useState('Seja objetivo, educado e persuasivo.')
  const [handoffRules, setHandoffRules] = useState('atendente,humano,reclamação,orçamento')

  const [provider, setProvider] = useState<Provider>('openai')
  const [model, setModel] = useState(defaultModels.openai)
  const [editingAgent, setEditingAgent] = useState<Agent | null>(null)
  const [saving, setSaving] = useState(false)
  const [saveMessage, setSaveMessage] = useState('')

  const [docTitle, setDocTitle] = useState('')
  const [docContent, setDocContent] = useState('')

  const editAgent = (agent: Agent) => {
    setEditingAgent(agent)
    setName(agent.name)
    setObjective(agent.objective ?? '')
    setCompanyName(agent.companyName ?? '')
    setProducts(agent.productsServices ?? '')
    setInstructions(agent.instructions ?? '')
    setHandoffRules(agent.handoffRules ?? '')
    const selected = agent.modelProvider === 'ollama' ? 'ollama' : 'openai'
    setProvider(selected)
    setModel(agent.modelName || defaultModels[selected])
    setSaveMessage('')
  }

  useEffect(() => {
    void loadAll().catch(() => setSaveMessage('Não foi possível carregar os agentes.'))
  }, [])

  const loadAll = async () => {
    const [a, d] = await Promise.all([
      api.get('/api/ai/agents'),
      api.get('/api/ai/knowledge/documents')
    ])
    setAgents(a.data.data || [])
    setDocs(d.data.data || [])
  }

  const saveAgent = async (e: React.FormEvent) => {
    e.preventDefault()
    if (saving || !model.trim()) return
    setSaving(true)
    setSaveMessage('')
    try {
      const request = {
        ...(editingAgent ?? { isActive: true, isDefault: true, personality: 'profissional', tone: 'consultivo', temperature: 0.4, maxTokens: 800 }),
        name,
        objective,
        companyName,
        productsServices: products,
        instructions,
        handoffRules,
        modelProvider: provider,
        modelName: model.trim()
      }

      if (editingAgent) await api.put(`/api/ai/agents/${editingAgent.id}`, request)
      else await api.post('/api/ai/agents', request)

      await loadAll()
      setSaveMessage('Agente salvo.')
    } catch (error) {
      setSaveMessage(isAxiosError(error) ? error.response?.data?.error ?? 'Não foi possível salvar o agente.' : 'Não foi possível salvar o agente.')
    } finally {
      setSaving(false)
    }
  }

  const createDoc = async (e: React.FormEvent) => {
    e.preventDefault()
    await api.post('/api/ai/knowledge/documents', {
      title: docTitle,
      content: docContent,
      sourceType: 'manual'
    })
    setDocTitle('')
    setDocContent('')
    loadAll()
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold flex items-center gap-2">
          <Bot className="h-8 w-8 text-primary" /> Inteligência Artificial
        </h1>
        <p className="text-muted-foreground">Configure agentes, base de conhecimento e handoff humano.</p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <CardTitle>Agente padrão</CardTitle>
          </CardHeader>
          <CardContent>
            <form onSubmit={saveAgent} className="space-y-3">
              <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Nome do agente" required />
              <Input value={companyName} onChange={(e) => setCompanyName(e.target.value)} placeholder="Nome da empresa" />
              <Input value={objective} onChange={(e) => setObjective(e.target.value)} placeholder="Objetivo" />
              <textarea className="w-full border rounded-md p-2 text-sm" rows={3} value={products} onChange={(e) => setProducts(e.target.value)} placeholder="Produtos e serviços" />
              <textarea className="w-full border rounded-md p-2 text-sm" rows={3} value={instructions} onChange={(e) => setInstructions(e.target.value)} placeholder="Instruções" />
              <Input value={handoffRules} onChange={(e) => setHandoffRules(e.target.value)} placeholder="Regras de handoff (separadas por vírgula)" />
              <label className="block text-sm">Provedor do agente
                <select className="mt-1 block w-full rounded border bg-background p-2" value={provider} onChange={(e) => {
                  const selected = e.target.value === 'ollama' ? 'ollama' : 'openai'
                  setProvider(selected)
                  setModel(defaultModels[selected])
                }}>
                  <option value="openai">OpenAI</option>
                  <option value="ollama">Ollama</option>
                </select>
              </label>
              <label className="block text-sm">Modelo
                <Input className="mt-1" value={model} onChange={(e) => setModel(e.target.value)} required maxLength={100} />
              </label>
              <Button type="submit" disabled={saving || !model.trim()}><Save className="h-4 w-4 mr-2" />{saving ? 'Salvando…' : 'Salvar agente'}</Button>
              {editingAgent && <Button type="button" variant="outline" className="ml-2" disabled={saving} onClick={() => {
                setEditingAgent(null)
                setSaveMessage('')
              }}>Criar outro agente</Button>}
              {saveMessage && <p role="status" className="text-sm">{saveMessage}</p>}
            </form>

            <div className="mt-4 space-y-2">
              {agents.map((a) => (
                <div key={a.id} className="border rounded p-2 text-sm">
                  <div className="font-semibold">{a.name} {a.isDefault ? '• padrão' : ''}</div>
                  <div className="text-muted-foreground">{a.modelProvider}/{a.modelName}</div>
                  <Button type="button" variant="outline" size="sm" className="mt-2" onClick={() => editAgent(a)} disabled={saving}>Editar agente</Button>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2"><BookOpen className="h-5 w-5" /> Base de conhecimento</CardTitle>
          </CardHeader>
          <CardContent>
            <form onSubmit={createDoc} className="space-y-3">
              <Input value={docTitle} onChange={(e) => setDocTitle(e.target.value)} placeholder="Título do documento" required />
              <textarea className="w-full border rounded-md p-2 text-sm" rows={8} value={docContent} onChange={(e) => setDocContent(e.target.value)} placeholder="Conteúdo (FAQ, preços, políticas...)" required />
              <Button type="submit">Adicionar documento</Button>
            </form>

            <div className="mt-4 space-y-2 max-h-80 overflow-auto">
              {docs.map((d) => (
                <div key={d.id} className="border rounded p-2 text-sm">
                  <div className="font-semibold">{d.title}</div>
                  <div className="text-muted-foreground line-clamp-2">{d.content}</div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}