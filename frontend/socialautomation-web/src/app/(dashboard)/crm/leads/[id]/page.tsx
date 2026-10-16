
'use client'

import { useEffect, useState } from 'react'
import { useParams } from 'next/navigation'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import api from '@/lib/api'
import type { Lead, LeadActivity, CrmTask } from '@/types/crm'
import { User, Instagram, Phone, Mail, DollarSign, Calendar, Clock, Plus, CheckCircle2 } from 'lucide-react'

export default function LeadDetailPage() {
  const { id } = useParams()
  const [lead, setLead] = useState<Lead | null>(null)
  const [activities, setActivities] = useState<LeadActivity[]>([])
  const [tasks, setTasks] = useState<CrmTask[]>([])

  // Nova Tarefa Form
  const [taskTitle, setTaskTitle] = useState('')
  const [taskDueDate, setTaskDueDate] = useState('')

  useEffect(() => {
    if (id) {
      loadLead()
      loadActivities()
      loadTasks()
    }
  }, [id])

  const loadLead = async () => {
    const res = await api.get(`/api/leads/${id}`)
    setLead(res.data.data)
  }

  const loadActivities = async () => {
    const res = await api.get(`/api/leads/${id}/activities`)
    setActivities(res.data.data)
  }

  const loadTasks = async () => {
    const res = await api.get(`/api/tasks`, { params: { leadId: id } })
    setTasks(res.data.data.items)
  }

  const handleCreateTask = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!taskTitle || !taskDueDate) return

    await api.post('/api/tasks', {
      leadId: id,
      title: taskTitle,
      dueDate: new Date(taskDueDate).toISOString(),
      priority: 'Alta'
    })
    setTaskTitle('')
    setTaskDueDate('')
    loadTasks()
    loadActivities()
  }

  const handleCompleteTask = async (taskId: string) => {
    await api.put(`/api/tasks/${taskId}/complete`)
    loadTasks()
    loadActivities()
  }

  if (!lead) return <div>Carregando detalhes do lead...</div>

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{lead.name}</h1>
          <p className="text-muted-foreground flex items-center gap-2">
            Status: <span className="font-semibold text-primary">{lead.pipelineStage}</span> •
            Score: <span className="font-semibold">{lead.score} pts ({lead.scoreClassification})</span>
          </p>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Painel Esquerdo: Dados do Lead */}
        <Card className="lg:col-span-1 space-y-4 p-4">
          <CardHeader className="p-0">
            <CardTitle className="text-lg">Informações do Contato</CardTitle>
          </CardHeader>
          <CardContent className="p-0 space-y-3 text-sm">
            {lead.instagramUsername && (
              <div className="flex items-center gap-2 text-pink-600">
                <Instagram className="h-4 w-4" /> @{lead.instagramUsername}
              </div>
            )}
            {lead.phone && (
              <div className="flex items-center gap-2">
                <Phone className="h-4 w-4 text-muted-foreground" /> {lead.phone}
              </div>
            )}
            {lead.email && (
              <div className="flex items-center gap-2">
                <Mail className="h-4 w-4 text-muted-foreground" /> {lead.email}
              </div>
            )}
            <div className="flex items-center gap-2">
              <DollarSign className="h-4 w-4 text-muted-foreground" />
              Valor: {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(lead.dealValue)}
            </div>

            <div className="pt-2 border-t">
              <label className="text-xs font-bold text-muted-foreground uppercase">Tags</label>
              <div className="flex flex-wrap gap-1 mt-1">
                {lead.tags.map((t) => (
                  <span key={t.id} className="text-xs px-2 py-0.5 rounded font-medium" style={{ backgroundColor: `${t.color}20`, color: t.color }}>
                    {t.name}
                  </span>
                ))}
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Painel Central e Direito: Timeline e Follow-ups */}
        <div className="lg:col-span-2 space-y-6">
          {/* Central de Tarefas / Follow-ups */}
          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-lg flex items-center gap-2">
                <Clock className="h-5 w-5 text-primary" /> Follow-ups & Tarefas
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <form onSubmit={handleCreateTask} className="flex gap-2">
                <Input
                  placeholder="Nova tarefa ou lembrete de follow-up..."
                  value={taskTitle}
                  onChange={(e) => setTaskTitle(e.target.value)}
                  required
                />
                <Input
                  type="datetime-local"
                  className="w-56"
                  value={taskDueDate}
                  onChange={(e) => setTaskDueDate(e.target.value)}
                  required
                />
                <Button type="submit">Agendar</Button>
              </form>

              <div className="space-y-2">
                {tasks.map((t) => (
                  <div key={t.id} className="flex items-center justify-between p-2 rounded border bg-muted/20 text-sm">
                    <div className="flex items-center gap-2">
                      <Button
                        size="icon"
                        variant="ghost"
                        className="h-6 w-6"
                        onClick={() => handleCompleteTask(t.id)}
                        disabled={t.status === 'Concluida'}
                      >
                        <CheckCircle2 className={`h-4 w-4 ${t.status === 'Concluida' ? 'text-green-600' : 'text-muted-foreground'}`} />
                      </Button>
                      <span className={t.status === 'Concluida' ? 'line-through text-muted-foreground' : 'font-medium'}>
                        {t.title}
                      </span>
                    </div>
                    <span className="text-xs text-muted-foreground">
                      {new Date(t.dueDate).toLocaleString('pt-BR')}
                    </span>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>

          {/* Timeline de Atividades */}
          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-lg flex items-center gap-2">
                <Calendar className="h-5 w-5 text-primary" /> Histórico de Atividades
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="relative pl-6 space-y-4 border-l-2 border-primary/20">
                {activities.map((act) => (
                  <div key={act.id} className="relative">
                    <div className="absolute -left-[31px] top-1 h-3 w-3 rounded-full bg-primary" />
                    <p className="font-semibold text-sm">{act.title}</p>
                    <p className="text-xs text-muted-foreground">{act.description}</p>
                    <p className="text-[10px] text-muted-foreground/70 mt-0.5">
                      {new Date(act.createdAt).toLocaleString('pt-BR')} • por {act.userName}
                    </p>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}