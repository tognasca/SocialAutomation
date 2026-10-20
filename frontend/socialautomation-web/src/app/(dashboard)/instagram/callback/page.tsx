
'use client'

import { useEffect, useState } from 'react'
import { useRouter, useSearchParams } from 'next/navigation'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import api from '@/lib/api'
import { Loader2, CheckCircle2, XCircle } from 'lucide-react'

export default function InstagramCallbackPage() {
  const router = useRouter()
  const searchParams = useSearchParams()
  const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading')
  const [errorMessage, setErrorMessage] = useState('')

  useEffect(() => {
    const code = searchParams.get('code')
    const state = searchParams.get('state')
    const error = searchParams.get('error_description') || searchParams.get('error')

    if (error) {
      setStatus('error')
      setErrorMessage(error)
      return
    }

    if (!code || !state) {
      setStatus('error')
      setErrorMessage('Parâmetros de autorização ausentes.')
      return
    }
debugger;
    const exchangeCode = async () => {
      try {
        const redirectUri = `${window.location.origin}/instagram/callback`
        await api.post('/api/instagram/connect', {
          code,
          state,
          redirectUri
        })
        setStatus('success')
        setTimeout(() => {
          router.push('/instagram')
        }, 2000)
      } catch (err: any) {
        setStatus('error')
        setErrorMessage(err.response?.data?.error || 'Erro ao autenticar com a Meta.')
      }
    }

    exchangeCode()
  }, [searchParams, router])

  return (
    <div className="flex min-h-[60vh] items-center justify-center">
      <Card className="w-full max-w-md text-center p-6">
        <CardHeader>
          <CardTitle>Conectando Instagram</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          {status === 'loading' && (
            <div className="flex flex-col items-center gap-3">
              <Loader2 className="h-10 w-10 animate-spin text-primary" />
              <p className="text-sm text-muted-foreground">Validando credenciais e sincronizando páginas...</p>
            </div>
          )}

          {status === 'success' && (
            <div className="flex flex-col items-center gap-3 text-green-600">
              <CheckCircle2 className="h-12 w-12" />
              <p className="font-semibold">Instagram conectado com sucesso!</p>
              <p className="text-xs text-muted-foreground">Redirecionando...</p>
            </div>
          )}

          {status === 'error' && (
            <div className="flex flex-col items-center gap-3 text-destructive">
              <XCircle className="h-12 w-12" />
              <p className="font-semibold">Falha na conexão</p>
              <p className="text-sm">{errorMessage}</p>
              <button
                onClick={() => router.push('/instagram')}
                className="mt-4 text-sm text-primary underline"
              >
                Voltar para Contas
              </button>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}