
'use client'

import { useEffect, useState, useRef } from 'react'
import { Card } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import api from '@/lib/api'
import type { Conversation, ChatMessage } from '@/types/inbox'
import { MessageSquare, Send, Bot, User, Phone, Tag, MoreVertical } from 'lucide-react'

export default function InboxPage() {
  const [conversations, setConversations] = useState<Conversation[]>([])
  const [activeConv, setActiveConv] = useState<Conversation | null>(null)
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [newMessage, setNewMessage] = useState('')
  const [loading, setLoading] = useState(false)
  const messagesEndRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    loadConversations()
    const interval = setInterval(loadConversations, 10000) // Polling a cada 10s
    return () => clearInterval(interval)
  }, [])

  useEffect(() => {
    if (activeConv) {
      loadMessages(activeConv.id)
    }
  }, [activeConv])

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  const loadConversations = async () => {
    const res = await api.get('/api/inbox/conversations', { params: { page: 1, pageSize: 50 } })
    setConversations(res.data.data.items)
  }

  const loadMessages = async (id: string) => {
    const res = await api.get(`/api/inbox/conversations/${id}/messages`)
    setMessages(res.data.data)
  }

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!activeConv || !newMessage.trim()) return

    try {
      setLoading(true)
      const res = await api.post(`/api/inbox/conversations/${activeConv.id}/messages`, {
        content: newMessage
      })
      setMessages([...messages, res.data.data])
      setNewMessage('')
      loadConversations() // Atualiza preview
    } catch {
      alert('Erro ao enviar mensagem')
    } finally {
      setLoading(false)
    }
  }

  const toggleMode = async () => {
    if (!activeConv) return
    const newMode = activeConv.mode === 'HUMANO' ? 'IA_ATIVA' : 'HUMANO'
    await api.put(`/api/inbox/conversations/${activeConv.id}/mode`, { mode: newMode })
    setActiveConv({ ...activeConv, mode: newMode })
    loadConversations()
  }

  return (
    <div className="h-[calc(100vh-8rem)] flex bg-background border rounded-xl overflow-hidden shadow-sm">
      {/* Lista Lateral */}
      <div className="w-1/3 border-r flex flex-col bg-muted/20">
        <div className="p-4 border-b bg-background">
          <h2 className="text-xl font-bold flex items-center gap-2">
            <MessageSquare className="h-5 w-5 text-primary" /> Inbox
          </h2>
        </div>
        <div className="flex-1 overflow-y-auto">
          {conversations.map(conv => (
            <div
              key={conv.id}
              onClick={() => setActiveConv(conv)}
              className={`p-4 border-b cursor-pointer hover:bg-muted/50 transition-colors ${activeConv?.id === conv.id ? 'bg-primary/5 border-l-4 border-l-primary' : ''}`}
            >
              <div className="flex justify-between items-start mb-1">
                <h3 className="font-semibold text-sm truncate pr-2">{conv.leadName}</h3>
                <span className="text-[10px] text-muted-foreground whitespace-nowrap">
                  {new Date(conv.lastMessageAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                </span>
              </div>
              <p className="text-xs text-muted-foreground truncate mb-2">
                {conv.lastMessagePreview || 'Nenhuma mensagem ainda'}
              </p>
              <div className="flex justify-between items-center">
                <span className={`text-[10px] px-1.5 py-0.5 rounded font-medium ${conv.mode === 'HUMANO' ? 'bg-blue-100 text-blue-700' : 'bg-purple-100 text-purple-700'}`}>
                  {conv.mode === 'HUMANO' ? 'Humano' : 'IA Ativa'}
                </span>
                {conv.unreadCount > 0 && (
                  <span className="bg-primary text-primary-foreground text-[10px] font-bold px-1.5 py-0.5 rounded-full">
                    {conv.unreadCount}
                  </span>
                )}
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Área do Chat */}
      <div className="flex-1 flex flex-col bg-slate-50/50">
        {activeConv ? (
          <>
            {/* Cabecalho do Chat */}
            <div className="h-16 border-b bg-background flex items-center justify-between px-6">
              <div className="flex items-center gap-3">
                <div className="h-10 w-10 bg-primary/10 rounded-full flex items-center justify-center text-primary font-bold">
                  {activeConv.leadName.charAt(0).toUpperCase()}
                </div>
                <div>
                  <h3 className="font-bold text-sm">{activeConv.leadName}</h3>
                  <p className="text-xs text-muted-foreground">@{activeConv.instagramUsername || 'instagram'}</p>
                </div>
              </div>
              <div className="flex items-center gap-3">
                <Button variant="outline" size="sm" onClick={toggleMode} className="h-8">
                  {activeConv.mode === 'HUMANO' ? (
                    <><Bot className="h-3 w-3 mr-1" /> Ativar IA</>
                  ) : (
                    <><User className="h-3 w-3 mr-1" /> Assumir (Humano)</>
                  )}
                </Button>
                <Button variant="ghost" size="icon"><MoreVertical className="h-4 w-4" /></Button>
              </div>
            </div>

            {/* Mensagens */}
            <div className="flex-1 overflow-y-auto p-6 space-y-4">
              {messages.map(msg => {
                const isMine = msg.senderType === 'User' || msg.senderType === 'System' || msg.senderType === 'AI';
                return (
                  <div key={msg.id} className={`flex flex-col ${isMine ? 'items-end' : 'items-start'}`}>
                    <div className={`max-w-[70%] p-3 rounded-2xl text-sm ${isMine ? 'bg-primary text-primary-foreground rounded-tr-none' : 'bg-white border shadow-sm rounded-tl-none'}`}>
                      {msg.content}
                    </div>
                    <span className="text-[10px] text-muted-foreground mt-1 px-1">
                      {new Date(msg.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                      {isMine && msg.senderType === 'AI' && ' • IA'}
                    </span>
                  </div>
                )
              })}
              <div ref={messagesEndRef} />
            </div>

            {/* Input Form */}
            <div className="p-4 bg-background border-t">
              {activeConv.mode === 'IA_ATIVA' && (
                <div className="mb-2 text-xs text-purple-600 bg-purple-50 p-2 rounded text-center font-medium">
                  A Inteligência Artificial está respondendo este lead. Clique em "Assumir" para intervir.
                </div>
              )}
              <form onSubmit={handleSendMessage} className="flex gap-2">
                <Input
                  className="flex-1 rounded-full bg-muted/50 border-transparent focus-visible:ring-1"
                  placeholder="Digite uma mensagem..."
                  value={newMessage}
                  onChange={e => setNewMessage(e.target.value)}
                  disabled={loading || activeConv.mode === 'IA_ATIVA'}
                />
                <Button type="submit" size="icon" className="rounded-full h-10 w-10" disabled={loading || !newMessage.trim() || activeConv.mode === 'IA_ATIVA'}>
                  <Send className="h-4 w-4" />
                </Button>
              </form>
            </div>
          </>
        ) : (
          <div className="flex-1 flex flex-col items-center justify-center text-muted-foreground">
            <MessageSquare className="h-16 w-16 mb-4 opacity-20" />
            <p>Selecione uma conversa para iniciar o atendimento</p>
          </div>
        )}
      </div>
    </div>
  )
}