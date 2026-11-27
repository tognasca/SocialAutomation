'use client'

import { useEffect, useState } from 'react'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import api from '@/lib/api'
import { isAxiosError } from 'axios'
import type { InstagramAccount } from '@/types/instagram'
import type { ContentItem, GeneratedAiContent } from '@/types/content'
import { FileText, Sparkles, Calendar, Clock } from 'lucide-react'
import { ArtworkEditor } from './artwork-editor'
import { createJpeg, type Brand, type Slide } from './artwork'

type CaptureCampaign = { id: string; name: string; publicCaptureEnabled: boolean }
const defaultBrand: Brand = { name: '', background: '#17243a', foreground: '#f8eedc', template: 'editorial' }

export default function ContentPage() {
  const [items, setItems] = useState<ContentItem[]>([])
  const [topic, setTopic] = useState('')
  const [contentType, setContentType] = useState('Post')
  const [generating, setGenerating] = useState(false)
  const [generated, setGenerated] = useState<GeneratedAiContent | null>(null)
  const [scheduledAt, setScheduledAt] = useState('')
  const [accounts, setAccounts] = useState<InstagramAccount[]>([])
  const [accountId, setAccountId] = useState('')
  const [mediaUrl, setMediaUrl] = useState('')
  const [brand, setBrand] = useState<Brand>(defaultBrand)
  const [slides, setSlides] = useState<Slide[]>([])
  const [useArtwork, setUseArtwork] = useState(true)
  const [campaigns, setCampaigns] = useState<CaptureCampaign[]>([])
  const [campaignId, setCampaignId] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    void loadContent().catch(() => setError('Erro ao carregar publicações.'))
    const refresh = window.setInterval(() => void loadContent().catch(() => {}), 10000)
    void api.get('/api/instagram/publication-accounts')
      .then(res => setAccounts(res.data.data.filter((account: InstagramAccount) => account.status === 'Active')))
      .catch(() => setError('Não foi possível carregar as contas de publicação.'))
    void api.get('/api/campaigns')
      .then(res => setCampaigns((res.data.data as CaptureCampaign[]).filter(campaign => campaign.publicCaptureEnabled)))
      .catch(() => {})
    return () => window.clearInterval(refresh)
  }, [])

  const loadContent = async () => {
    const res = await api.get('/api/content')
    setItems(res.data.data)
  }

  const carousel = contentType === 'Carousel'
  const story = contentType === 'Story'
  const studioActive = carousel || (useArtwork && contentType !== 'Reel')
  const canPublish = Boolean(accountId && (studioActive ? slides.length && slides.every(slide => slide.text.trim() || slide.photo) : mediaUrl.trim()))

  function prepareArtwork(content: GeneratedAiContent) {
    setGenerated(content)
    const titles = carousel && (content.slideTexts?.length ?? 0) >= 2
      ? content.slideTexts!
      : carousel ? [content.title, content.cta || ''] : [content.title]
    setSlides(titles.map(title => ({ text: title.slice(0, 180) })))
  }

  const handleGenerateAi = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setGenerating(true)
    try {
      const res = await api.post('/api/content/generate-ai', {
        topic,
        contentType,
        tone: 'persuasivo e amigável'
      })
      prepareArtwork(res.data.data)
    } catch {
      setError('Erro ao gerar conteúdo com IA.')
    } finally {
      setGenerating(false)
    }
  }

  const handleSaveContent = async (publishNow = false) => {
    if (!generated?.title.trim() || saving) return
    const date = publishNow ? new Date().toISOString() : scheduledAt ? new Date(scheduledAt).toISOString() : null
    setSaving(true)
    setError('')
    try {
      let mediaUrls = mediaUrl.trim() ? [mediaUrl.trim()] : []
      if (studioActive) {
        const images = []
        for (const slide of slides) images.push(await createJpeg(slide, brand, story))
        mediaUrls = []
        for (const image of images) {
          const form = new FormData()
          form.append('file', image, 'arte.jpg')
          const response = await api.post('/api/content/media', form, {
            headers: { 'Content-Type': 'multipart/form-data' }
          })
          mediaUrls.push(response.data.data.url)
        }
      }

      await api.post('/api/content', {
        type: contentType,
        title: generated.title,
        caption: generated.caption,
        hashtags: generated.hashtags,
        cta: generated.cta,
        scheduledAt: date,
        instagramAccountId: accountId || null,
        mediaUrls,
        mediaFormat: studioActive ? story ? '9:16' : '4:5' : undefined,
        scenes: generated.reelScenes
      })
      setGenerated(null)
      setTopic('')
      setScheduledAt('')
      setMediaUrl('')
      setCampaignId('')
      void loadContent().catch(() => setError('Publicação salva. Atualize a página para recarregar a lista.'))
    } catch (err) {
      setError(isAxiosError(err) ? err.response?.data?.error ?? 'Erro ao salvar publicação.' : err instanceof Error ? err.message : 'Erro ao salvar publicação.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold flex items-center gap-2">
          <FileText className="h-8 w-8 text-primary" /> Estúdio de Conteúdo
        </h1>
        <p className="text-muted-foreground">Crie artes e legendas, revise as páginas e agende no Instagram.</p>
      </div>

      {error && <p role="alert" className="text-sm text-destructive">{error}</p>}

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Formulário de Geração IA */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Sparkles className="h-5 w-5 text-purple-600" /> Criar Conteúdo com IA
            </CardTitle>
            <CardDescription>Informe o tema para a IA criar título, legenda, hashtags e roteiro.</CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleGenerateAi} className="space-y-4">
              <div>
                <label className="text-sm font-medium">Tema / Ideia da Publicação</label>
                <Input
                  value={topic}
                  maxLength={255}
                  onChange={(e) => setTopic(e.target.value)}
                  placeholder="Ex: 3 Dicas para aumentar o engajamento no Instagram"
                  required
                />
              </div>

              <div>
                <label className="text-sm font-medium">Tipo de Conteúdo</label>
                <select
                  className="w-full border rounded-md p-2 text-sm"
                  value={contentType}
                  disabled={generating || saving}
                  onChange={(e) => { setContentType(e.target.value); setGenerated(null); setMediaUrl('') }}
                >
                  <option value="Post">Imagem de Feed</option>
                  <option value="Carousel">Carrossel</option>
                  <option value="Reel">Reel com Roteiro em Cenas</option>
                  <option value="Story">Story Engajador</option>
                </select>
              </div>

              <Button type="submit" disabled={generating || saving} className="w-full bg-gradient-to-r from-purple-600 to-indigo-600 text-white">
                {generating ? 'IA Gerando Conteúdo...' : 'Gerar Publicação com IA'}
              </Button>
              <Button type="button" variant="outline" onClick={() => prepareArtwork({
                title: topic.trim(), caption: '', hashtags: [], cta: '', visualSuggestion: '', reelScenes: []
              })} disabled={generating || saving || !topic.trim()}>Compor sem IA</Button>
            </form>

            {generated && (
              <fieldset disabled={saving || generating} className="mt-6 min-w-0 p-4 border rounded-xl bg-muted/30 space-y-3 text-sm">
                <label className="block">Título
                  <Input maxLength={255} value={generated.title} onChange={e => setGenerated({ ...generated, title: e.target.value })} />
                </label>
                {contentType !== 'Story' && <label className="block">Legenda
                  <textarea className="block min-h-20 w-full rounded border p-2" value={generated.caption}
                    onChange={e => setGenerated({ ...generated, caption: e.target.value })} />
                </label>}
                {!story && <label className="block">Chamada para ação
                  <Input maxLength={255} value={generated.cta} onChange={event => setGenerated({ ...generated, cta: event.target.value })} />
                </label>}
                {!story && campaigns.length > 0 && <div className="space-y-2">
                  <label className="block">Página de captura da campanha
                    <select className="block w-full rounded border bg-background p-2" value={campaignId}
                      onChange={event => setCampaignId(event.target.value)}>
                      <option value="">Selecione uma campanha publicada</option>
                      {campaigns.map(campaign => <option key={campaign.id} value={campaign.id}>{campaign.name}</option>)}
                    </select>
                  </label>
                  {campaignId && <Button type="button" size="sm" variant="outline" onClick={() => {
                    const url = new URL(`/captura/${campaignId}?utm_source=instagram`, window.location.origin).href
                    setGenerated({ ...generated, caption: [generated.caption, url].filter(Boolean).join('\n\n') })
                    setCampaignId('')
                  }}>Inserir endereço na legenda</Button>}
                </div>}
                <div className="flex flex-wrap gap-1">
                  {generated.hashtags.map((h, i) => (
                    <span key={i} className="text-xs text-purple-700 bg-purple-100 px-2 py-0.5 rounded">{h}</span>
                  ))}
                </div>

                {generated.reelScenes.length > 0 && (
                  <div className="pt-2 border-t space-y-2">
                    <p className="font-bold text-xs uppercase text-purple-900">Roteiro do Reel ({generated.reelScenes.length} Cenas):</p>
                    {generated.reelScenes.map((s) => (
                      <div key={s.sceneOrder} className="p-2 bg-white rounded border text-xs">
                        <span className="font-bold">Cena {s.sceneOrder} ({s.durationSeconds}s):</span> {s.scriptText}
                      </div>
                    ))}
                  </div>
                )}

                {!carousel && contentType !== 'Reel' && <label className="flex items-center gap-2">
                  <input type="checkbox" checked={useArtwork} onChange={event => setUseArtwork(event.target.checked)} />
                  Criar imagem no estúdio
                </label>}
                {studioActive && <ArtworkEditor brand={brand} slides={slides} onBrandChange={setBrand}
                  onSlidesChange={setSlides} carousel={carousel} story={story} />}

                <div className="pt-3 border-t space-y-2">
                  <label className="block">Conta do Instagram
                    <select className="block w-full rounded border p-2" value={accountId} onChange={e => setAccountId(e.target.value)}>
                      <option value="">Selecione uma conta</option>
                      {accounts.map(account => <option key={account.id} value={account.id}>@{account.username}</option>)}
                    </select>
                  </label>
                  {!studioActive && <label className="block">{contentType === 'Reel' ? 'URL pública do vídeo' : 'URL pública da imagem'}
                    <Input type="url" placeholder="https://" value={mediaUrl} onChange={e => setMediaUrl(e.target.value)} />
                  </label>}
                  <label className="block">Data e hora do agendamento
                  <Input
                    type="datetime-local"
                    value={scheduledAt}
                    onChange={(e) => setScheduledAt(e.target.value)}
                    className="text-xs"
                  />
                  </label>
                  <div className="flex flex-wrap gap-2">
                  <Button onClick={() => handleSaveContent()} disabled={saving || !generated.title.trim() || (!!scheduledAt && !canPublish)} size="sm">
                    {scheduledAt ? 'Agendar' : 'Salvar rascunho'}
                  </Button>
                  <Button onClick={() => handleSaveContent(true)} disabled={saving || !generated.title.trim() || !canPublish} size="sm">
                    Publicar agora
                  </Button>
                  </div>
                </div>
              </fieldset>
            )}
          </CardContent>
        </Card>

        {/* Lista / Calendário Editorial */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Calendar className="h-5 w-5 text-primary" /> Minhas Publicações
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            {items.map((item) => (
              <div key={item.id} className="p-3 border rounded-lg flex items-center justify-between hover:bg-muted/20 transition-colors">
                <div className="space-y-1">
                  <div className="flex items-center gap-2">
                    <span className="font-bold text-sm">{item.title}</span>
                    <span className="text-[10px] px-2 py-0.5 rounded font-semibold bg-primary/10 text-primary">
                      {item.type === 'Carousel' ? 'Carrossel' : item.type}
                    </span>
                  </div>
                  <p className="text-xs text-muted-foreground line-clamp-1">{item.caption}</p>
                  {item.errorMessage && <p className="text-xs text-destructive">{item.errorMessage}</p>}
                  <div className="flex items-center gap-2 text-[11px] text-muted-foreground">
                    <Clock className="h-3 w-3" />
                    {item.scheduledAt ? `Agendado: ${new Date(item.scheduledAt).toLocaleString('pt-BR')}` : 'Rascunho'}
                  </div>
                </div>

                <div>
                  <span className={`text-xs px-2 py-1 rounded-full font-semibold ${
                    item.status === 'Publicado' ? 'bg-green-100 text-green-800' :
                    item.status === 'Agendado' ? 'bg-blue-100 text-blue-800' : 'bg-gray-100 text-gray-800'
                  }`}>
                    {item.status}
                  </span>
                </div>
              </div>
            ))}

            {items.length === 0 && (
              <p className="text-center text-sm text-muted-foreground py-8">
                Nenhum conteúdo criado ainda. Gere seu primeiro post acima!
              </p>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}