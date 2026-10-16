
'use client'

import { useEffect, useState } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import api from '@/lib/api'
import type { Lead, Tag } from '@/types/crm'
import { Users, Plus, Search, Instagram, TrendingUp, Calendar, ChevronRight } from 'lucide-react'
import Link from 'next/link'

export default function LeadsPage() {
  const [leads, setLeads] = useState<Lead[]>([])
  const [tags, setTags] = useState<Tag[]>([])
  const [search, setSearch] = useState('')
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(false)
  const [showModal, setShowModal] = useState(false)

  // Novo Lead Form
  const [name, setName] = useState('')
  const [instagramUsername, setInstagramUsername] = useState('')
  const [phone, setPhone] = useState('')
  const [email, setEmail] = useState('')
  const [dealValue, setDealValue] = useState('0')
  const [selectedTagIds, setSelectedTagIds] = useState<string[]>([])

  useEffect(() => {
    loadLeads()
    loadTags()
  }, [])

  const loadLeads = async () => {
    setLoading(true)
    try {
      const res = await api.get('/api/leads', { params: { search, page: 1, pageSize: 50 } })
      setLeads(res.data.data.items)
      setTotal(res.data.data.total)
    } catch {
      // handle error
    } finally {
      setLoading(false)
    }
  }

  const loadTags = async () => {
    try {
      const res = await api.get('/api/tags')
      setTags(res.data.data)
    } catch {
      // handle error
    }
  }

  const handleCreateLead = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      await api.post('/api/leads', {
        name,
        instagramUsername,
        phone,
        email,
        dealValue: parseFloat(dealValue) || 0,
        tagIds: selectedTagIds
      })
      setShowModal(false)
      setName('')
      setInstagramUsername('')
      setPhone('')
      setEmail('')
      setDealValue('0')
      setSelectedTagIds([])
      loadLeads()
    } catch {
      alert('Erro ao cadastrar lead.')
    }
  }

  const getScoreBadge = (classification: string, score: number) => {
    const colors: Record<string, string> = {
      'Frio': 'bg-blue-100 text-blue-800',
      'Morno': 'bg-yellow-100 text-yellow-800',
      'Quente': 'bg-orange-100 text-orange-800',
      'Alta Intenção': 'bg-red-100 text-red-800'
    }
    return (
      <span className={`px-2 py-0.5 rounded-full text-xs font-semibold ${colors[classification] || 'bg-gray-100'}`}>
        {score} pts • {classification}
      </span>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold flex items-center gap-2">
            <Users className="h-8 w-8 text-primary" />
            CRM de Leads
          </h1>
          <p className="text-muted-foreground">{total} leads cadastrados</p>
        </div>
        <div className="flex gap-2">
          <Link href="/crm/pipeline">
            <Button variant="outline">Ver Funil Kanban</Button>
          </Link>
          <Button onClick={() => setShowModal(true)}>
            <Plus className="mr-2 h-4 w-4" /> Novo Lead
          </Button>
        </div>
      </div>

      <div className="flex gap-3">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
          <Input
            className="pl-9"
            placeholder="Buscar por nome, @instagram, email ou telefone..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && loadLeads()}
          />
        </div>
        <Button onClick={loadLeads} variant="secondary">Filtrar</Button>
      </div>

      {showModal && (
        <Card className="border-primary/50 shadow-md">
          <CardHeader>
            <CardTitle>Cadastrar Novo Lead</CardTitle>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleCreateLead} className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="text-sm font-medium">Nome Completo *</label>
                <Input value={name} onChange={(e) => setName(e.target.value)} required />
              </div>
              <div>
                <label className="text-sm font-medium">@ Usuário do Instagram</label>
                <Input value={instagramUsername} onChange={(e) => setInstagramUsername(e.target.value)} placeholder="ex: joaosilva" />
              </div>
              <div>
                <label className="text-sm font-medium">WhatsApp / Telefone</label>
                <Input value={phone} onChange={(e) => setPhone(e.target.value)} placeholder="(11) 99999-9999" />
              </div>
              <div>
                <label className="text-sm font-medium">E-mail</label>
                <Input type="email" value={email} onChange={(e) => setEmail(e.target.value)} />
              </div>
              <div>
                <label className="text-sm font-medium">Valor Estimado (R$)</label>
                <Input type="number" step="0.01" value={dealValue} onChange={(e) => setDealValue(e.target.value)} />
              </div>
              <div className="md:col-span-2 flex justify-end gap-2 mt-2">
                <Button type="button" variant="outline" onClick={() => setShowModal(false)}>Cancelar</Button>
                <Button type="submit">Salvar Lead</Button>
              </div>
            </form>
          </CardContent>
        </Card>
      )}

      <Card>
        <CardContent className="p-0">
          <table className="w-full">
            <thead>
              <tr className="border-b bg-muted/50 text-xs font-semibold text-muted-foreground uppercase">
                <th className="px-4 py-3 text-left">Lead</th>
                <th className="px-4 py-3 text-left">Tags</th>
                <th className="px-4 py-3 text-left">Estágio</th>
                <th className="px-4 py-3 text-left">Lead Score</th>
                <th className="px-4 py-3 text-left">Valor</th>
                <th className="px-4 py-3 text-left">Próximo Follow-up</th>
                <th className="px-4 py-3 text-right">Ação</th>
              </tr>
            </thead>
            <tbody className="divide-y text-sm">
              {leads.map((lead) => (
                <tr key={lead.id} className="hover:bg-muted/20 transition-colors">
                  <td className="px-4 py-3">
                    <div className="font-semibold">{lead.name}</div>
                    {lead.instagramUsername && (
                      <div className="flex items-center gap-1 text-xs text-pink-600">
                        <Instagram className="h-3 w-3" /> @{lead.instagramUsername}
                      </div>
                    )}
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex flex-wrap gap-1">
                      {lead.tags.map((t) => (
                        <span key={t.id} className="text-[10px] px-2 py-0.5 rounded-full font-medium" style={{ backgroundColor: `${t.color}20`, color: t.color }}>
                          {t.name}
                        </span>
                      ))}
                    </div>
                  </td>
                  <td className="px-4 py-3">
                    <span className="bg-primary/10 text-primary text-xs px-2 py-1 rounded font-medium">
                      {lead.pipelineStage}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    {getScoreBadge(lead.scoreClassification, lead.score)}
                  </td>
                  <td className="px-4 py-3 font-medium">
                    {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(lead.dealValue)}
                  </td>
                  <td className="px-4 py-3 text-xs text-muted-foreground">
                    {lead.nextFollowUpAt ? new Date(lead.nextFollowUpAt).toLocaleDateString('pt-BR') : 'Nenhum'}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <Link href={`/crm/leads/${lead.id}`}>
                      <Button size="sm" variant="ghost">
                        Detalhes <ChevronRight className="ml-1 h-4 w-4" />
                      </Button>
                    </Link>
                  </td>
                </tr>
              ))}
              {leads.length === 0 && (
                <tr>
                  <td colSpan={7} className="text-center py-10 text-muted-foreground">
                    Nenhum lead encontrado.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </CardContent>
      </Card>
    </div>
  )
}