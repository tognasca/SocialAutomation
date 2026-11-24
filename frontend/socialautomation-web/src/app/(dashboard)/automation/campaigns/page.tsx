'use client'

import { useCallback, useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { isAxiosError } from 'axios'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import api from '@/lib/api'
import type { InstagramAccount } from '@/types/instagram'

type PublishedContent = {
  id: string
  title: string
  instagramAccountId: string
  instagramPublicationId?: string
  status: string
  type: string
}

type Campaign = {
  id: string
  name: string
  instagramMediaId?: string
  commentKeyword?: string
  status: string
  replyCount: number
  sentCount: number
  failedCount: number
}

function errorMessage(error: unknown) {
  return isAxiosError(error) ? error.response?.data?.error || error.message : 'Não foi possível concluir a operação.'
}

type SalesSettings = { questions: string[]; slots: { id: number; startsAt: string; bookedAt?: string }[] }

function SalesFlowEditor({ campaignId }: { campaignId: string }) {
  const [settings, setSettings] = useState<SalesSettings | null>(null)
  const [questions, setQuestions] = useState('')
  const [slot, setSlot] = useState('')
  const [feedback, setFeedback] = useState('')
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    const { data } = await api.get(`/api/campaigns/${campaignId}/sales-flow`)
    const loaded: SalesSettings = data.data
    setSettings(loaded)
    setQuestions(loaded.questions.join('\n'))
  }, [campaignId])

  useEffect(() => {
    void load().catch(error => setFeedback(errorMessage(error)))
  }, [load])

  async function save(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    setFeedback('')
    try {
      await api.put(`/api/campaigns/${campaignId}/sales-flow`, {
        questions: questions.split('\n').map(line => line.trim()).filter(Boolean),
        newSlots: slot ? [`${slot}:00-03:00`] : []
      })
      await load()
      setSlot('')
      setFeedback('Fluxo salvo.')
    } catch (error) {
      setFeedback(errorMessage(error))
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="mt-3 space-y-3 border-t pt-3">
      {settings && <form className="space-y-3" onSubmit={save}>
        <label className="block text-sm">Perguntas de qualificação, uma por linha
          <textarea className="block w-full rounded border p-2" rows={4} value={questions}
            placeholder={'Quantos veículos sua loja tem em estoque?\nQual é a maior dificuldade na gestão hoje?'}
            onChange={event => setQuestions(event.target.value)} />
        </label>
        <p className="text-sm text-muted-foreground">Deixe as perguntas vazias para desativar o fluxo comercial.</p>
        <label className="block text-sm">Adicionar horário de demonstração (Brasília)
          <Input type="datetime-local" step={1800} value={slot} onChange={event => setSlot(event.target.value)} />
        </label>
        <Button size="sm" disabled={busy}>Salvar fluxo</Button>
        {settings.slots.length > 0 && <ul className="space-y-1 text-sm">
          {settings.slots.map(item => (
            <li key={item.id}>{new Date(item.startsAt).toLocaleString('pt-BR', { timeZone: 'America/Sao_Paulo' })} · {item.bookedAt ? 'Reservado' : 'Disponível'}</li>
          ))}
        </ul>}
      </form>}
      {feedback && <p role="status" className="text-sm">{feedback}</p>}
    </div>
  )
}

export default function CampaignsPage() {
  const [accounts, setAccounts] = useState<InstagramAccount[]>([])
  const [publications, setPublications] = useState<PublishedContent[]>([])
  const [campaigns, setCampaigns] = useState<Campaign[]>([])
  const [accountId, setAccountId] = useState('')
  const [name, setName] = useState('')
  const [mediaId, setMediaId] = useState('')
  const [keyword, setKeyword] = useState('')
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [editingFlow, setEditingFlow] = useState('')

  const loadCampaigns = useCallback(async () => {
    try {
      const response = await api.get('/api/campaigns')
      setCampaigns(response.data.data)
    } catch (error) {
      setError(errorMessage(error))
    }
  }, [])

  useEffect(() => {
    void loadCampaigns()
    void api.get('/api/content').then(response => {
      setPublications((response.data.data as PublishedContent[]).filter(item =>
        item.status === 'Publicado' && item.instagramPublicationId && item.type !== 'Story'))
    }).catch(() => {})

    void api.get('/api/instagram/accounts').then(response => {
      setAccounts((response.data.data as InstagramAccount[]).filter(account => account.status === 'Active'))
    }).catch(error => setError(errorMessage(error)))
  }, [loadCampaigns])

  async function save(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    setError('')
    try {
      await api.post('/api/campaigns', {
        name, instagramAccountId: accountId, instagramMediaId: mediaId.trim(),
        commentKeyword: keyword.trim(), messageTemplate: message
      })
      setName('')
      setMediaId('')
      setKeyword('')
      setMessage('')
      await loadCampaigns()
    } catch (error) {
      setError(errorMessage(error))
    } finally {
      setBusy(false)
    }
  }

  async function setEnabled(campaign: Campaign) {
    setBusy(true)
    setError('')
    try {
      await api.put(`/api/campaigns/${campaign.id}/comment-status`, null,
        { params: { enabled: campaign.status !== 'Ativa' } })
      await loadCampaigns()
    } catch (error) {
      setError(errorMessage(error))
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="space-y-6">
      <h1 className="text-3xl font-bold">Campanhas</h1>
      {error && <p role="alert" className="text-sm text-destructive">{error}</p>}
      <div className="grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader><CardTitle>Comentário para Direct</CardTitle></CardHeader>
          <CardContent>
            <form onSubmit={save} className="space-y-4">
              <label className="block">Nome da campanha
                <Input required maxLength={255} value={name} onChange={e => setName(e.target.value)} />
              </label>
              <label className="block">Conta do Instagram
                <select required className="block w-full rounded border p-2" value={accountId} onChange={e => { setAccountId(e.target.value); setMediaId('') }}>
                  <option value="">Selecione a conta</option>
                  {accounts.map(account => <option key={account.id} value={account.id}>@{account.username}</option>)}
                </select>
              </label>
              {publications.some(item => item.instagramAccountId === accountId) && <label className="block">Publicações do SocialAutomation
                <select className="block w-full rounded border p-2" value={publications.some(item => item.instagramPublicationId === mediaId) ? mediaId : ''}
                  onChange={e => setMediaId(e.target.value)}>
                  <option value="">Selecione uma publicação ou informe o ID</option>
                  {publications.filter(item => item.instagramAccountId === accountId).map(item =>
                    <option key={item.id} value={item.instagramPublicationId}>{item.title}</option>)}
                </select>
              </label>}
              <label className="block">ID da publicação nessa conta
                <Input required inputMode="numeric" pattern="[0-9]{1,40}" value={mediaId} onChange={e => setMediaId(e.target.value)} />
              </label>
              <label className="block">Palavra-chave
                <Input required maxLength={50} placeholder="DEMO" value={keyword} onChange={e => setKeyword(e.target.value)} />
              </label>
              <label className="block">Primeira mensagem privada
                <textarea required maxLength={1000} rows={4} className="block w-full rounded border p-2"
                  value={message} onChange={e => setMessage(e.target.value)} />
              </label>
              <p className="text-sm text-muted-foreground">Convide a pessoa a responder para continuar pelo Inbox.</p>
              <Button disabled={busy || !accountId}>Salvar pausada</Button>
            </form>
          </CardContent>
        </Card>
        <Card>
          <CardHeader><CardTitle>Campanhas cadastradas</CardTitle></CardHeader>
          <CardContent className="space-y-3">
            {!campaigns.length && <p className="text-sm text-muted-foreground">Nenhuma campanha cadastrada.</p>}
            {campaigns.map(campaign => (
              <div key={campaign.id} className="space-y-2 rounded border p-3">
                <p className="font-semibold">{campaign.name} · {campaign.status}</p>
                {campaign.commentKeyword && <p className="text-sm">Gatilho {campaign.commentKeyword} · Publicação {campaign.instagramMediaId}</p>}
                <p className="text-sm text-muted-foreground">{campaign.sentCount} envios confirmados · {campaign.replyCount} respostas · {campaign.failedCount} falhas</p>
                {campaign.commentKeyword && <Button disabled={busy} variant="outline" size="sm" onClick={() => setEnabled(campaign)}>
                  {campaign.status === 'Ativa' ? 'Pausar' : 'Ativar'}
                </Button>}
                {campaign.commentKeyword && <Button variant="ghost" size="sm" onClick={() => setEditingFlow(editingFlow === campaign.id ? '' : campaign.id)}>
                  {editingFlow === campaign.id ? 'Fechar fluxo' : 'Configurar qualificação e agenda'}
                </Button>}
                {editingFlow === campaign.id && <SalesFlowEditor key={campaign.id} campaignId={campaign.id} />}
              </div>
            ))}
            <Button variant="ghost" onClick={loadCampaigns}>Atualizar resultados</Button>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
