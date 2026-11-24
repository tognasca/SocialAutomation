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
  totalAudience: number
  qualifiedCount: number
  bookedCount: number
  acceptedCount: number
  reelScript?: string
  socialCaption?: string
  captureMaterial?: string
  publicCaptureEnabled: boolean
  publicCaptureTitle?: string
  publicCaptureIntro?: string
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

type Plan = { maxVehicles: number; name: string; price: number; terms: string }

function ProposalPlansEditor({ campaignId }: { campaignId: string }) {
  const [plans, setPlans] = useState<Plan[] | null>(null)
  const [feedback, setFeedback] = useState('')
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    const controller = new AbortController()
    void api.get(`/api/campaigns/${campaignId}/proposal-plans`, { signal: controller.signal })
      .then(response => setPlans(response.data.data))
      .catch(error => { if (!controller.signal.aborted) setFeedback(errorMessage(error)) })
    return () => controller.abort()
  }, [campaignId])

  function update(index: number, changes: Partial<Plan>) {
    setPlans(current => current?.map((plan, i) => i === index ? { ...plan, ...changes } : plan) ?? null)
  }

  async function save(event: FormEvent) {
    event.preventDefault()
    if (!plans || busy) return
    setBusy(true)
    setFeedback('')
    try {
      const sorted = [...plans].sort((a, b) => a.maxVehicles - b.maxVehicles)
      await api.put(`/api/campaigns/${campaignId}/proposal-plans`, sorted)
      setPlans(sorted)
      setFeedback('Planos salvos.')
    } catch (error) {
      setFeedback(errorMessage(error))
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="mt-4 space-y-3 border-t pt-3">
      <h3 className="font-semibold">Proposta comercial</h3>
      <p className="text-sm text-muted-foreground">Para usar as faixas, a primeira pergunta deve ser sobre a quantidade de veículos. Peça que o lead responda com um número.</p>
      {plans && <form onSubmit={save} className="space-y-3">
        {plans.map((plan, index) => (
          <fieldset key={index} disabled={busy} className="space-y-2 rounded border p-3">
            <legend className="px-1 text-sm">Plano {index + 1}</legend>
            <label className="block text-sm">Nome
              <Input required maxLength={100} value={plan.name} onChange={event => update(index, { name: event.target.value })} />
            </label>
            <div className="grid gap-2 sm:grid-cols-2">
              <label className="block text-sm">Até quantos veículos
                <Input required type="number" min={1} max={999999} value={plan.maxVehicles || ''} onChange={event => update(index, { maxVehicles: +event.target.value })} />
              </label>
              <label className="block text-sm">Preço em reais
                <Input required type="number" min={0.01} max={9999999999.99} step={0.01} value={plan.price || ''} onChange={event => update(index, { price: +event.target.value })} />
              </label>
            </div>
            <label className="block text-sm">Condições e periodicidade
              <textarea required maxLength={4000} className="block w-full rounded border bg-background p-2"
                value={plan.terms} onChange={event => update(index, { terms: event.target.value })} />
            </label>
            <Button type="button" size="sm" variant="ghost" onClick={() => setPlans(plans.filter((_, i) => i !== index))}>Remover plano</Button>
          </fieldset>
        ))}
        <div className="flex flex-wrap gap-2">
          <Button type="button" size="sm" variant="outline" disabled={busy || plans.length >= 10} onClick={() =>
            setPlans([...plans, { maxVehicles: 0, name: '', price: 0, terms: '' }])}>Adicionar plano</Button>
          <Button size="sm" disabled={busy}>Salvar planos</Button>
        </div>
      </form>}
      {feedback && <p role="status" className="text-sm">{feedback}</p>}
    </div>
  )
}

type ChannelMetrics = { channel: string; visits: number; diagnoses: number; demos: number; accepted: number }

function PublicCaptureEditor({ campaign, refresh }: { campaign: Campaign; refresh: () => Promise<void> }) {
  const isStock = campaign.commentKeyword?.toLowerCase() === 'estoque'
  const [title, setTitle] = useState(campaign.publicCaptureTitle || (isStock
    ? 'Quais carros estão prendendo dinheiro no seu estoque?' : campaign.name))
  const [intro, setIntro] = useState(campaign.publicCaptureIntro || (isStock
    ? 'Faça o diagnóstico rápido e receba um checklist para decidir quais veículos merecem atenção primeiro.' : ''))
  const [channel, setChannel] = useState('instagram_bio')
  const [metrics, setMetrics] = useState<ChannelMetrics[]>()
  const [notice, setNotice] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const publicPath = `/captura/${campaign.id}`
  const sharePath = channel ? `${publicPath}?utm_source=${encodeURIComponent(channel)}` : publicPath

  async function publish(enabled: boolean) {
    setBusy(true)
    setError('')
    try {
      await api.put(`/api/campaigns/${campaign.id}/public-capture`, { enabled, title, intro })
      await refresh()
    } catch (failure) {
      setError(errorMessage(failure))
    } finally {
      setBusy(false)
    }
  }

  async function loadMetrics() {
    setError('')
    try {
      const response = await api.get(`/api/campaigns/${campaign.id}/public-capture/metrics`)
      setMetrics(response.data.data)
    } catch (failure) {
      setError(errorMessage(failure))
    }
  }

  async function copyLink() {
    setNotice('')
    setError('')
    try {
      await navigator.clipboard.writeText(new URL(sharePath, location.origin).href)
      setNotice('Link copiado.')
    } catch {
      setError('Abra a página e copie o endereço na barra do navegador.')
    }
  }

  return (
    <details className="space-y-3 rounded border p-3">
      <summary className="cursor-pointer text-sm font-semibold">Página pública de captura</summary>
      <form className="mt-3 space-y-3 text-sm" onSubmit={event => { event.preventDefault(); void publish(true) }}>
        <label className="block">Título da página<Input value={title} onChange={event => setTitle(event.target.value)} required maxLength={160} /></label>
        <label className="block">Descrição da oferta
          <textarea value={intro} onChange={event => setIntro(event.target.value)} required maxLength={800} rows={3}
            className="block w-full rounded border p-2" />
        </label>
        <div className="flex flex-wrap gap-2">
          <Button size="sm" disabled={busy}>{campaign.publicCaptureEnabled ? 'Salvar página' : 'Publicar página'}</Button>
          {campaign.publicCaptureEnabled && <Button type="button" variant="outline" size="sm" disabled={busy}
            onClick={() => publish(false)}>Despublicar</Button>}
        </div>
      </form>
      {campaign.publicCaptureEnabled && <div className="mt-3 space-y-2 text-sm">
        <label className="block">Origem para o link
          <select className="block w-full rounded border p-2" value={channel} onChange={event => setChannel(event.target.value)}>
            <option value="">Direto</option>
            <option value="instagram_bio">Bio do Instagram</option>
            <option value="instagram_stories">Stories</option>
            <option value="anuncios">Anúncios</option>
            <option value="whatsapp">WhatsApp</option>
          </select>
        </label>
        <div className="flex flex-wrap items-center gap-3">
          <a className="underline" href={sharePath} target="_blank" rel="noreferrer">Abrir página</a>
          <Button variant="outline" size="sm" onClick={copyLink}>Copiar link</Button>
        </div>
      </div>}
      <Button variant="ghost" size="sm" onClick={loadMetrics}>Atualizar funil por origem</Button>
      {metrics && (metrics.length ? <div className="overflow-x-auto">
        <table className="w-full text-left text-sm">
          <thead><tr>{['Origem', 'Visitas', 'Diagnósticos', 'Demos', 'Aceites'].map(label =>
            <th className="px-2 py-1" key={label}>{label}</th>)}</tr></thead>
          <tbody>{metrics.map(row => <tr className="border-t" key={row.channel}>
            {[row.channel, row.visits, row.diagnoses, row.demos, row.accepted].map((value, index) =>
              <td className="px-2 py-1" key={index}>{value}</td>)}
          </tr>)}</tbody>
        </table>
      </div> : <p className="text-sm text-muted-foreground">Ainda não há visitas registradas.</p>)}
      {notice && <p role="status" className="text-sm">{notice}</p>}
      {error && <p role="alert" className="text-sm text-destructive">{error}</p>}
    </details>
  )
}

const emptyCapture = { reelScript: '', socialCaption: '', questions: '', captureMaterial: '' }

const stockCapture = {
  reelScript: 'Abra com: “Você sabe quais carros estão parados há mais de 45 dias na sua loja?” Mostre uma lista de estoque e destaque a data de entrada de três veículos. Feche com: “Comente ESTOQUE e eu te mando um checklist rápido para decidir o que fazer com os carros parados.”',
  socialCaption: 'Carro parado ocupa espaço e prende capital. Quer revisar seu estoque em 10 minutos? Comente ESTOQUE e receba um checklist pelo Direct.',
  questions: 'Quantos veículos você tem no estoque hoje? Responda só com o número.\nQual é o maior desafio hoje: veículos parados, precificação ou organização do estoque?',
  captureMaterial: 'Checklist de estoque em 10 minutos\n\n1. Liste os veículos com data de entrada e custo total.\n2. Separe os que estão há mais de 45 dias e confira as consultas da última semana.\n3. Escolha uma ação para cada veículo parado: atualizar fotos, revisar preço ou reforçar a divulgação.\n4. Revise a lista toda semana e acompanhe o que gerou contatos.',
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
  const [capture, setCapture] = useState(emptyCapture)
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

  function applyStockTemplate() {
    setName('AT Motors · Diagnóstico de estoque')
    setKeyword('ESTOQUE')
    setMessage('Oi! Preparei um checklist rápido para revisar os carros parados na sua loja. Responda QUERO para receber por aqui.')
    setCapture(stockCapture)
  }

  async function save(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    setError('')
    try {
      await api.post('/api/campaigns', {
        name, instagramAccountId: accountId, instagramMediaId: mediaId.trim(),
        commentKeyword: keyword.trim(), messageTemplate: message,
        reelScript: capture.reelScript, socialCaption: capture.socialCaption, captureMaterial: capture.captureMaterial,
        captureQuestions: capture.questions.split('\n').map(question => question.trim()).filter(Boolean)
      })
      setName('')
      setMediaId('')
      setKeyword('')
      setMessage('')
      setCapture(emptyCapture)
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
              <Button type="button" variant="outline" onClick={applyStockTemplate}>Usar modelo de diagnóstico de estoque</Button>
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
              <details open={Boolean(capture.captureMaterial || capture.reelScript)}>
                <summary className="cursor-pointer font-medium">Oferta de captura e conteúdo do Reel</summary>
                <div className="mt-3 space-y-3">
                  <label className="block">Roteiro do Reel
                    <textarea maxLength={4000} rows={3} className="block w-full rounded border p-2" value={capture.reelScript}
                      onChange={event => setCapture({ ...capture, reelScript: event.target.value })} />
                  </label>
                  <label className="block">Legenda com chamada para comentar
                    <textarea maxLength={2200} rows={3} className="block w-full rounded border p-2" value={capture.socialCaption}
                      onChange={event => setCapture({ ...capture, socialCaption: event.target.value })} />
                  </label>
                  <label className="block">Perguntas de qualificação (uma por linha)
                    <textarea rows={3} className="block w-full rounded border p-2" value={capture.questions}
                      onChange={event => setCapture({ ...capture, questions: event.target.value })} />
                  </label>
                  <label className="block">Material entregue após a qualificação
                    <textarea maxLength={800} rows={5} className="block w-full rounded border p-2" value={capture.captureMaterial}
                      onChange={event => setCapture({ ...capture, captureMaterial: event.target.value })} />
                  </label>
                </div>
              </details>
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
                {campaign.commentKeyword && <p className="text-sm text-muted-foreground">
                  {campaign.totalAudience} comentários processados · {campaign.qualifiedCount} qualificados · {campaign.bookedCount} demos agendadas · {campaign.acceptedCount} propostas aceitas
                </p>}
                {(campaign.reelScript || campaign.socialCaption || campaign.captureMaterial) && <details>
                  <summary className="cursor-pointer text-sm font-medium">Ver conteúdo de captura</summary>
                  {[
                    ['Roteiro', campaign.reelScript],
                    ['Legenda', campaign.socialCaption],
                    ['Material do Direct', campaign.captureMaterial],
                  ].map(([label, value]) => value && <div key={label} className="mt-2 text-sm">
                    <p className="font-medium">{label}</p>
                    <p className="whitespace-pre-wrap">{value}</p>
                  </div>)}
                </details>}
                {campaign.captureMaterial && <PublicCaptureEditor campaign={campaign} refresh={loadCampaigns} />}
                {campaign.commentKeyword && <Button disabled={busy} variant="outline" size="sm" onClick={() => setEnabled(campaign)}>
                  {campaign.status === 'Ativa' ? 'Pausar' : 'Ativar'}
                </Button>}
                {campaign.commentKeyword && <Button variant="ghost" size="sm" onClick={() => setEditingFlow(editingFlow === campaign.id ? '' : campaign.id)}>
                  {editingFlow === campaign.id ? 'Fechar fluxo' : 'Configurar qualificação e agenda'}
                </Button>}
                {editingFlow === campaign.id && <div key={campaign.id}>
                  <SalesFlowEditor campaignId={campaign.id} />
                  <ProposalPlansEditor campaignId={campaign.id} />
                </div>}
              </div>
            ))}
            <Button variant="ghost" onClick={loadCampaigns}>Atualizar resultados</Button>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
