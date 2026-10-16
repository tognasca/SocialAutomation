'use client'

import { useEffect, useState } from 'react'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import api from '@/lib/api'
import type { ContentItem, GeneratedAiContent } from '@/types/content'
import { FileText, Sparkles, Calendar, Video, Image, Plus, CheckCircle, Clock } from 'lucide-react'

export default function ContentPage() {
  const [items, setItems] = useState<ContentItem[]>([])
  const [topic, setTopic] = useState('')
  const [contentType, setContentType] = useState('Post')
  const [generating, setGenerating] = useState(false)
  const [generated, setGenerated] = useState<GeneratedAiContent | null>(null)
  const [scheduledAt, setScheduledAt] = useState('')

  useEffect(() => {
    loadContent()
  }, [])

  const loadContent = async () => {
    const res = await api.get('/api/content')
    setItems(res.data.data)
  }

  const handleGenerateAi = async (e: React.FormEvent) => {
    e.preventDefault()
    setGenerating(true)
    try {
      const res = await api.post('/api/content/generate-ai', {
        topic,
        contentType,
        tone: 'persuasivo e amigável'
      })
      setGenerated(res.data.data)
    } catch {
      alert('Erro ao gerar conteúdo com IA.')
    } finally {
      setGenerating(false)
    }
  }

  const handleSaveContent = async () => {
    if (!generated) return
    try {
      await api.post('/api/content', {
        type: contentType,
        title: generated.title,
        caption: generated.caption,
        hashtags: generated.hashtags,
        cta: generated.cta,
        scheduledAt: scheduledAt ? new Date(scheduledAt).toISOString() : null,
        scenes: generated.reelScenes
      })
      setGenerated(null)
      setTopic('')
      setScheduledAt('')
      loadContent()
    } catch {
      alert('Erro ao salvar publicação.')
    }
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold flex items-center gap-2">
          <FileText className="h-8 w-8 text-primary" /> Gerador & Calendário de Conteúdo
        </h1>
        <p className="text-muted-foreground">Crie posts, carrosséis e roteiros de Reels com IA e agende suas publicações.</p>
      </div>

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
                  onChange={(e) => setContentType(e.target.value)}
                >
                  <option value="Post">Feed Post / Carrossel</option>
                  <option value="Reel">Reel com Roteiro em Cenas</option>
                  <option value="Story">Story Engajador</option>
                </select>
              </div>

              <Button type="submit" disabled={generating} className="w-full bg-gradient-to-r from-purple-600 to-indigo-600 text-white">
                {generating ? 'IA Gerando Conteúdo...' : 'Gerar Publicação com IA'}
              </Button>
            </form>

            {generated && (
              <div className="mt-6 p-4 border rounded-xl bg-purple-50/50 space-y-3 text-sm">
                <h3 className="font-bold text-base text-purple-900">{generated.title}</h3>
                <p className="whitespace-pre-line text-muted-foreground">{generated.caption}</p>
                <p className="font-semibold text-primary">CTA: {generated.cta}</p>
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

                <div className="pt-3 border-t flex items-center gap-2">
                  <Input
                    type="datetime-local"
                    value={scheduledAt}
                    onChange={(e) => setScheduledAt(e.target.value)}
                    className="text-xs"
                  />
                  <Button onClick={handleSaveContent} size="sm">
                    {scheduledAt ? 'Agendar' : 'Salvar Rascunho'}
                  </Button>
                </div>
              </div>
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
                      {item.type}
                    </span>
                  </div>
                  <p className="text-xs text-muted-foreground line-clamp-1">{item.caption}</p>
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