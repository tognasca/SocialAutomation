
'use client'

import Image from 'next/image'
import { useEffect, useState } from 'react'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import api from '@/lib/api'
import { isAxiosError } from 'axios'
import { Input } from '@/components/ui/input'
import type { InstagramAccount } from '@/types/instagram'
import { Instagram, CheckCircle, AlertTriangle, Trash2, RefreshCw } from 'lucide-react'

type SearchProfile = { username: string; name?: string; biography?: string; followers_count?: number; media_count?: number }
type SearchPost = { id: string; caption?: string; permalink?: string; like_count?: number; comments_count?: number }

export default function InstagramAccountsPage() {
  const [accounts, setAccounts] = useState<InstagramAccount[]>([])
  const [loading, setLoading] = useState(false)
  const [connecting, setConnecting] = useState(false)
  const [error, setError] = useState('')

  const [searchAccount, setSearchAccount] = useState('')
  const [query, setQuery] = useState('')
  const [hashtag, setHashtag] = useState(true)
  const [searching, setSearching] = useState(false)
  const [searchMessage, setSearchMessage] = useState('')
  const [results, setResults] = useState<SearchProfile | SearchPost[] | null>(null)

  async function search(event: React.FormEvent) {
    event.preventDefault()
    setSearching(true)
    setResults(null)
    setSearchMessage('')
    try {
      const res = await api.get('/api/instagram/search', { params: { accountId: searchAccount, query, hashtag } })
      setResults(res.data.data)
      if (!res.data.data || (Array.isArray(res.data.data) && !res.data.data.length))
        setSearchMessage('Nenhum resultado encontrado.')
    } catch (err) {
      setSearchMessage(isAxiosError(err) ? err.response?.data?.error ?? 'Erro ao pesquisar.' : 'Erro ao pesquisar.')
    } finally {
      setSearching(false)
    }
  }

  useEffect(() => {
    loadAccounts()
  }, [])

  const loadAccounts = async () => {
    setLoading(true)
    try {
      const res = await api.get('/api/instagram/accounts')
      setAccounts(res.data.data)
    } catch {
      setError('Erro ao carregar contas do Instagram.')
    } finally {
      setLoading(false)
    }
  }

  const handleConnectInstagram = async () => {
    setConnecting(true)
    setError('')
    try {
      const redirectUri = `${window.location.origin}/instagram/callback`
      const res = await api.get('/api/instagram/oauth-url', {
        params: { redirectUri }
      })
      window.location.href = res.data.data.authorizationUrl
    } catch (err: any) {
      setError(err.response?.data?.error || 'Erro ao iniciar conexão com a Meta.')
      setConnecting(false)
    }
  }

  const handleDisconnect = async (id: string) => {
    if (!confirm('Deseja realmente desconectar esta conta do Instagram?')) return
    try {
      await api.delete(`/api/instagram/accounts/${id}`)
      loadAccounts()
    } catch {
      setError('Erro ao desconectar conta.')
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold flex items-center gap-2">
            <Instagram className="h-8 w-8 text-pink-600" />
            Contas do Instagram
          </h1>
          <p className="text-muted-foreground">
            Conecte suas contas comerciais ou profissionais do Instagram via Meta Graph API Oficial.
          </p>
        </div>
        <Button onClick={handleConnectInstagram} disabled={connecting} className="bg-gradient-to-r from-purple-600 to-pink-600 hover:from-purple-700 hover:to-pink-700 text-white">
          <Instagram className="mr-2 h-4 w-4" />
          {connecting ? 'Redirecionando...' : 'Conectar Instagram'}
        </Button>
      </div>

      {error && (
        <div className="rounded-md bg-destructive/10 p-4 text-sm text-destructive flex items-center gap-2">
          <AlertTriangle className="h-4 w-4" />
          {error}
        </div>
      )}

      <Card>
        <CardHeader>
          <CardTitle>Pesquisa no Instagram</CardTitle>
          <CardDescription>Veja publicações por hashtag ou consulte um perfil profissional pelo @usuário.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <form onSubmit={search} className="grid gap-3 sm:grid-cols-2">
            <label>Conta
              <select required className="block w-full rounded border p-2" value={searchAccount} onChange={e => setSearchAccount(e.target.value)}>
                <option value="">Selecione</option>
                {accounts.filter(account => account.status === 'Active').map(account =>
                  <option key={account.id} value={account.id}>@{account.username}</option>)}
              </select>
            </label>
            <label>Tipo de pesquisa
              <select className="block w-full rounded border p-2" value={hashtag ? 'hashtag' : 'profile'} onChange={e => setHashtag(e.target.value === 'hashtag')}>
                <option value="hashtag">Hashtag</option>
                <option value="profile">Perfil profissional</option>
              </select>
            </label>
            <label>{hashtag ? 'Hashtag' : 'Usuário'}
              <Input value={query} required maxLength={101} onChange={e => setQuery(e.target.value)} />
            </label>
            <Button className="self-end" disabled={searching || !searchAccount}>{searching ? 'Pesquisando…' : 'Pesquisar'}</Button>
          </form>
          {searchMessage && <p role="status" className="text-sm">{searchMessage}</p>}
          {Array.isArray(results) ? (
            <ul className="space-y-2">
              {results.map(post => (
                <li key={post.id} className="rounded border p-3">
                  <p className="line-clamp-3">{post.caption || 'Publicação do Instagram'}</p>
                  <p className="text-sm text-muted-foreground">{post.like_count ?? 0} curtidas · {post.comments_count ?? 0} comentários</p>
                  {post.permalink?.startsWith('https://www.instagram.com/') &&
                    <a className="text-sm underline" href={post.permalink} target="_blank" rel="noreferrer">Abrir no Instagram</a>}
                </li>
              ))}
            </ul>
          ) : results && (
            <div className="rounded border p-3">
              <p className="font-semibold">@{results.username} {results.name}</p>
              <p>{results.biography}</p>
              <p className="text-sm text-muted-foreground">{results.followers_count ?? 0} seguidores · {results.media_count ?? 0} publicações</p>
            </div>
          )}
        </CardContent>
      </Card>

      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
        {accounts.map((acc) => (
          <Card key={acc.id} className="relative overflow-hidden border shadow-sm">
            <CardHeader className="flex flex-row items-center gap-4 pb-2">
              {acc.profilePictureUrl ? (
                <Image
                  unoptimized
                  width={56}
                  height={56}
                  src={acc.profilePictureUrl}
                  alt={acc.username}
                  className="h-14 w-14 rounded-full object-cover border"
                />
              ) : (
                <div className="flex h-14 w-14 items-center justify-center rounded-full bg-pink-100 text-pink-600 font-bold text-lg">
                  @{acc.username.substring(0, 2).toUpperCase()}
                </div>
              )}
              <div>
                <CardTitle className="text-lg">@{acc.username}</CardTitle>
                <CardDescription>{acc.pageName}</CardDescription>
              </div>
            </CardHeader>
            <CardContent className="space-y-3 pt-2">
              <div className="flex items-center justify-between text-sm">
                <span className="text-muted-foreground">Status:</span>
                <span className="flex items-center gap-1 text-xs font-semibold text-green-700 bg-green-100 px-2 py-0.5 rounded-full">
                  <CheckCircle className="h-3.5 w-3.5" /> Ativo
                </span>
              </div>
              <div className="flex items-center justify-between text-sm">
                <span className="text-muted-foreground">Webhooks:</span>
                <span className="text-xs font-medium text-muted-foreground">
                  {acc.webhookSubscribed ? 'Sincronizado' : 'Pendente'}
                </span>
              </div>
              <div className="flex items-center justify-between text-sm">
                <span className="text-muted-foreground">Validade do Token:</span>
                <span className="text-xs font-medium text-muted-foreground">
                  {acc.tokenExpiresAt ? new Date(acc.tokenExpiresAt).toLocaleDateString('pt-BR') : '60 dias'}
                </span>
              </div>

              <div className="pt-2 flex justify-end">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => handleDisconnect(acc.id)}
                  className="text-destructive hover:bg-destructive/10"
                >
                  <Trash2 className="h-4 w-4 mr-1" /> Desconectar
                </Button>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      {!loading && accounts.length === 0 && (
        <Card className="text-center p-12">
          <Instagram className="h-12 w-12 text-muted-foreground mx-auto mb-4 opacity-50" />
          <h3 className="text-lg font-semibold">Nenhuma conta conectada</h3>
          <p className="text-sm text-muted-foreground max-w-md mx-auto mt-1 mb-6">
            Conecte sua conta do Instagram Business ou Creator vinculada a uma Página do Facebook para habilitar CRM, inbox e automações.
          </p>
          <Button onClick={handleConnectInstagram} className="bg-gradient-to-r from-purple-600 to-pink-600 text-white">
            Conectar primeira conta
          </Button>
        </Card>
      )}
    </div>
  )
}