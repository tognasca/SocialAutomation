
'use client'

import { useState } from 'react'
import { useRouter } from 'next/navigation'
import { useAuthStore } from '@/stores/authStore'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

export default function LoginPage() {
  const [isLogin, setIsLogin] = useState(true)
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [name, setName] = useState('')
  const [tenantName, setTenantName] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const { login, register } = useAuthStore()
  const router = useRouter()

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setLoading(true)

    try {
      if (isLogin) {
        await login({ email, password })
      } else {
        if (password !== confirmPassword) {
          setError('As senhas não conferem.')
          setLoading(false)
          return
        }
        await register({
          tenantName,
          name,
          email,
          password,
          confirmPassword,
        })
      }
      router.push('/dashboard')
    } catch (err: any) {
      setError(err.response?.data?.error || 'Ocorreu um erro. Tente novamente.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <Card className="w-full max-w-md">
      <CardHeader className="text-center">
        <CardTitle className="text-2xl text-primary">SocialAutomation</CardTitle>
        <CardDescription>
          {isLogin ? 'Entre na sua conta' : 'Crie sua conta'}
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-4">
          {!isLogin && (
            <>
              <div>
                <label className="text-sm font-medium">Nome da empresa</label>
                <Input
                  value={tenantName}
                  onChange={(e) => setTenantName(e.target.value)}
                  placeholder="Minha Empresa"
                  required
                />
              </div>
              <div>
                <label className="text-sm font-medium">Seu nome</label>
                <Input
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder="João Silva"
                  required
                />
              </div>
            </>
          )}

          <div>
            <label className="text-sm font-medium">Email</label>
            <Input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="seu@email.com"
              required
            />
          </div>

          <div>
            <label className="text-sm font-medium">Senha</label>
            <Input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              required
            />
          </div>

          {!isLogin && (
            <div>
              <label className="text-sm font-medium">Confirmar senha</label>
              <Input
                type="password"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                placeholder="••••••••"
                required
              />
            </div>
          )}

          {error && (
            <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
              {error}
            </div>
          )}

          <Button type="submit" className="w-full" disabled={loading}>
            {loading ? 'Carregando...' : isLogin ? 'Entrar' : 'Criar conta'}
          </Button>

          <div className="text-center text-sm">
            {isLogin ? (
              <p>
                Não tem conta?{' '}
                <button
                  type="button"
                  onClick={() => { setIsLogin(false); setError('') }}
                  className="text-primary hover:underline"
                >
                  Criar conta
                </button>
              </p>
            ) : (
              <p>
                Já tem conta?{' '}
                <button
                  type="button"
                  onClick={() => { setIsLogin(true); setError('') }}
                  className="text-primary hover:underline"
                >
                  Entrar
                </button>
              </p>
            )}
          </div>
        </form>
      </CardContent>
    </Card>
  )
}