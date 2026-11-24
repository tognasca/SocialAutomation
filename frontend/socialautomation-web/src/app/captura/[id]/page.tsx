'use client'

import { useEffect, useState, type FormEvent } from 'react'
import { useParams } from 'next/navigation'

type Capture = { name: string; companyName: string; caption?: string; questions: string[] }
type Slot = { id: number; startsAt: string }
type Diagnosis = { material: string; slots: Slot[]; hasContact: boolean; hasProposalPlans: boolean; bookedSlot?: Slot }
type Proposal = { message: string; url?: string }

const time = new Intl.DateTimeFormat('pt-BR', {
  dateStyle: 'full', timeStyle: 'short', timeZone: 'America/Sao_Paulo',
})
const input = 'mt-2 block w-full rounded-xl border border-slate-300 bg-white px-4 py-3 text-slate-900 focus-visible:outline-indigo-600'
const action = 'rounded-xl bg-indigo-700 px-5 py-3 text-sm font-semibold text-white hover:bg-indigo-800 focus-visible:outline-indigo-600 disabled:opacity-50'

export default function PublicCapturePage() {
  const { id } = useParams<{ id: string }>()
  const endpoint = `${(process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5010').replace(/\/$/, '')}/api/public/campaigns/${encodeURIComponent(id)}`
  const [visitor, setVisitor] = useState('')
  const [capture, setCapture] = useState<Capture>()
  const [answers, setAnswers] = useState<string[]>([])
  const [diagnosis, setDiagnosis] = useState<Diagnosis>()
  const [booking, setBooking] = useState<Slot>()
  const [proposal, setProposal] = useState<Proposal>()
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [contactRequested, setContactRequested] = useState(false)

  useEffect(() => {
    const controller = new AbortController()
    const key = `campaign-visit:${id}`
    let session = crypto.randomUUID()
    try {
      session = sessionStorage.getItem(key) || session
      sessionStorage.setItem(key, session)
    } catch {
      // The form remains usable when session storage is blocked.
    }
    setVisitor(session)
    setCapture(undefined)
    setDiagnosis(undefined)
    setBooking(undefined)
    setProposal(undefined)
    setContactRequested(false)
    setError('')
    const channel = new URLSearchParams(location.search).get('utm_source') || 'direto'
    void fetch(`${endpoint}/visit`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ visitor: session, channel }), signal: controller.signal,
    }).then(async response => {
      if (!response.ok) throw new Error(response.status === 404 ? 'Esta campanha está indisponível.' : 'Não foi possível abrir a campanha.')
      const data: Capture = await response.json()
      setCapture(data)
      setAnswers(data.questions.map(() => ''))
    }).catch(cause => {
      if (!controller.signal.aborted) setError(cause.message)
    })
    return () => controller.abort()
  }, [id, endpoint])

  async function post<T>(path: string, data = {}): Promise<T> {
    const response = await fetch(`${endpoint}/${path}`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ visitor, ...data }),
    })
    if (!response.ok) {
      const failure = await response.json().catch(() => null)
      throw new Error(failure?.error || 'Não foi possível concluir. Tente novamente.')
    }
    return response.json()
  }

  async function submit<T>(path: string, data: object, done: (result: T) => void) {
    setBusy(true)
    setError('')
    try {
      done(await post<T>(path, data))
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Não foi possível concluir.')
    } finally {
      setBusy(false)
    }
  }

  function saveDiagnosis(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const fields = new FormData(event.currentTarget)
    const request = contactRequested ? {
      answers, consent: fields.has('consent'), name: fields.get('name'),
      email: fields.get('email'), phone: fields.get('phone'),
    } : { answers, consent: false }
    void submit<Diagnosis>('diagnosis', request, result => {
      setDiagnosis(result)
      setBooking(result.bookedSlot)
      setContactRequested(false)
    })
  }

  return (
    <main className="min-h-screen bg-slate-950 px-4 py-10 text-white sm:py-16">
      <div className="mx-auto max-w-2xl space-y-7">
        {capture && <>
          <header className="space-y-4">
            <p className="text-sm font-semibold text-indigo-300">{capture.companyName}</p>
            <h1 className="text-3xl font-bold tracking-tight sm:text-5xl">{capture.name}</h1>
            {capture.caption && <p className="whitespace-pre-line text-lg text-slate-300">{capture.caption}</p>}
          </header>
          <section className="space-y-5 rounded-3xl bg-slate-50 p-5 text-slate-900 sm:p-8">
            {!diagnosis && <form className="space-y-5" onSubmit={saveDiagnosis}>
              {capture.questions.map((question, index) => (
                <label className="block font-medium" key={index}>{question}
                  <input required maxLength={300} className={input} value={answers[index] ?? ''}
                    onChange={event => setAnswers(current => current.map((answer, i) => i === index ? event.target.value : answer))} />
                </label>
              ))}
              <button className={action} disabled={busy}>Ver diagnóstico</button>
            </form>}
            {diagnosis && <>
              <h2 className="text-xl font-bold">Seu material</h2>
              <p className="whitespace-pre-line text-slate-700">{diagnosis.material}</p>
              {!diagnosis.hasContact && !contactRequested &&
                <button type="button" className={action} onClick={() => setContactRequested(true)}>
                  {diagnosis.slots.length ? 'Quero agendar uma demonstração' : 'Quero receber contato'}
                </button>}
              {contactRequested && <form className="space-y-4 border-t border-slate-200 pt-5" onSubmit={saveDiagnosis}>
                <label className="block">Nome<input className={input} name="name" autoComplete="name" maxLength={255} required /></label>
                <label className="block">E-mail<input className={input} name="email" type="email" autoComplete="email" maxLength={255} /></label>
                <label className="block">WhatsApp<input className={input} name="phone" type="tel" autoComplete="tel" maxLength={30} /></label>
                <p className="text-sm text-slate-600">Preencha pelo menos um canal de contato.</p>
                <label className="flex items-start gap-3 text-sm">
                  <input className="mt-1" type="checkbox" name="consent" required />
                  Autorizo {capture.companyName} a entrar em contato sobre este diagnóstico e a demonstração.
                </label>
                <button className={action} disabled={busy}>Confirmar contato</button>
              </form>}
              {diagnosis.hasContact && !booking && (diagnosis.slots.length ? <div className="space-y-3 border-t border-slate-200 pt-5">
                <h2 className="text-xl font-bold">Agende uma demonstração</h2>
                <p className="text-sm text-slate-600">Horários de Brasília</p>
                <div className="grid gap-2">
                  {diagnosis.slots.map(slot => <button key={slot.id} className="rounded-xl border border-slate-300 bg-white px-4 py-3 text-left hover:border-indigo-700 disabled:opacity-50"
                    disabled={busy} onClick={() => submit<Slot>('book', { slotId: slot.id }, setBooking)}>
                    {time.format(new Date(slot.startsAt))}
                  </button>)}
                </div>
                <button className="text-sm font-medium text-indigo-700 underline" disabled={busy}
                  onClick={() => submit<Diagnosis>('diagnosis', { answers, consent: false }, result => {
                    setDiagnosis(result)
                    setBooking(result.bookedSlot)
                  })}>Atualizar horários</button>
              </div> : <p role="status">Contato registrado. Aguarde o retorno da equipe.</p>)}
              {booking && <div className="space-y-3 border-t border-slate-200 pt-5">
                <p role="status">Demonstração confirmada para {time.format(new Date(booking.startsAt))} (Brasília).</p>
                {diagnosis.hasProposalPlans && !proposal &&
                  <button className={action} disabled={busy} onClick={() => submit<Proposal>('proposal', {}, setProposal)}>Consultar proposta</button>}
                {proposal && <p>{proposal.message}{proposal.url && <> <a className="font-semibold text-indigo-700 underline" href={proposal.url}>Ver proposta</a></>}</p>}
              </div>}
            </>}
          </section>
        </>}
        {!capture && !error && <p role="status">Carregando campanha…</p>}
        {error && <p role="alert" className="rounded-xl bg-rose-950 px-4 py-3 text-rose-100">{error}</p>}
      </div>
    </main>
  )
}
