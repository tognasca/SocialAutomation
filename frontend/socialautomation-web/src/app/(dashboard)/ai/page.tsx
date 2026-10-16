'use client'

import { useEffect, useState } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import api from '@/lib/api'
import { Bot, BookOpen, Save } from 'lucide-react'

export default function AiPage() {
  const [agents, setAgents] = useState<any[]>([])
  const [docs, setDocs] = useState<any[]>([])

  const [name, setName] = useState('Assistente Comercial')
  const [objective, setObjective] = useState('Qualificar leads e agendar atendimento')
  const [companyName, setCompanyName] = useState('')
  const [products, setProducts] = useState('')
  const [instructions, setInstructions] = useState('Seja objetivo, educado e persuasivo.')
  const [handoffRules, setHandoffRules] = useState('atendente,humano,reclamação,orçamento')

  const [docTitle, setDocTitle] = useState('')
  const [docContent, setDocContent] = useState('')

  useEffect(() => {
    loadAll()
  }, [])

  const loadAll = async () => {
    const [a, d] = await Promise.all([
      api.get('/api/ai/agents'),
      api.get('/api/ai/knowledge/documents')
    ])
    setAgents(a.data.data || [])
    setDocs(d.data.data || [])
  }

  const createAgent = async (e: React.FormEvent) => {
    e.preventDefault()
    await api.post('/api/ai/agents', {
      name,
      isActive: true,
      isDefault: true,
      personality: 'profissional',
      tone: 'consultivo',
      objective,
      companyName,
      productsServices: products,
      instructions,
      handoffRules,
      modelProvider: 'openai',
      modelName: 'gpt-4o-mini',
      temperature: 0.4,
      maxTokens: 800
    })
    loadAll()
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
            <form onSubmit={createAgent} className="space-y-3">
              <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Nome do agente" required />
              <Input value={companyName} onChange={(e) => setCompanyName(e.target.value)} placeholder="Nome da empresa" />
              <Input value={objective} onChange={(e) => setObjective(e.target.value)} placeholder="Objetivo" />
              <textarea className="w-full border rounded-md p-2 text-sm" rows={3} value={products} onChange={(e) => setProducts(e.target.value)} placeholder="Produtos e serviços" />
              <textarea className="w-full border rounded-md p-2 text-sm" rows={3} value={instructions} onChange={(e) => setInstructions(e.target.value)} placeholder="Instruções" />
              <Input value={handoffRules} onChange={(e) => setHandoffRules(e.target.value)} placeholder="Regras de handoff (separadas por vírgula)" />
              <Button type="submit"><Save className="h-4 w-4 mr-2" /> Salvar agente</Button>
            </form>

            <div className="mt-4 space-y-2">
              {agents.map((a) => (
                <div key={a.id} className="border rounded p-2 text-sm">
                  <div className="font-semibold">{a.name} {a.isDefault ? '• padrão' : ''}</div>
                  <div className="text-muted-foreground">{a.modelProvider}/{a.modelName}</div>
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