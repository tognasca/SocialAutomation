
'use client'

import { useEffect, useState } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import api from '@/lib/api'
import type { Tenant } from '@/types/tenant'

export default function SettingsPage() {
  const [tenant, setTenant] = useState<Tenant | null>(null)
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [phone, setPhone] = useState('')
  const [loading, setLoading] = useState(false)
  const [message, setMessage] = useState('')

  useEffect(() => {
    loadTenant()
  }, [])

  const loadTenant = async () => {
    try {
      const response = await api.get('/api/tenants/current')
      const t = response.data.data
      setTenant(t)
      setName(t.name)
      setEmail(t.email || '')
      setPhone(t.phone || '')
    } catch {
      // handle error
    }
  }

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true)
    setMessage('')

    try {
      await api.put('/api/tenants/current', { name, email, phone })
      setMessage('Configurações salvas com sucesso!')
    } catch {
      setMessage('Erro ao salvar.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="space-y-6">
      <h1 className="text-3xl font-bold">Configurações</h1>

      <Card>
        <CardHeader>
          <CardTitle>Dados da empresa</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSave} className="space-y-4 max-w-md">
            <div>
              <label className="text-sm font-medium">Nome</label>
              <Input value={name} onChange={(e) => setName(e.target.value)} required />
            </div>
            <div>
              <label className="text-sm font-medium">Email</label>
              <Input type="email" value={email} onChange={(e) => setEmail(e.target.value)} />
            </div>
            <div>
              <label className="text-sm font-medium">Telefone</label>
              <Input value={phone} onChange={(e) => setPhone(e.target.value)} />
            </div>

            {message && (
              <div className="rounded-md bg-primary/10 p-3 text-sm text-primary">{message}</div>
            )}

            <Button type="submit" disabled={loading}>
              {loading ? 'Salvando...' : 'Salvar'}
            </Button>
          </form>
        </CardContent>
      </Card>

      {tenant && (
        <Card>
          <CardHeader>
            <CardTitle>Informações do plano</CardTitle>
          </CardHeader>
          <CardContent className="space-y-2">
            <p><strong>Plano:</strong> {tenant.plan}</p>
            <p><strong>Max. usuários:</strong> {tenant.maxUsers}</p>
            <p><strong>Max. contas Instagram:</strong> {tenant.maxInstagramAccounts}</p>
            <p><strong>Status:</strong> {tenant.status}</p>
          </CardContent>
        </Card>
      )}
    </div>
  )
}