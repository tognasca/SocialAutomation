import { useEffect, useRef, useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { createJpeg, renderArtwork, type Brand, type Slide } from './artwork'

type Props = {
  brand: Brand
  slides: Slide[]
  carousel: boolean
  story: boolean
  onBrandChange: (brand: Brand) => void
  onSlidesChange: (slides: Slide[]) => void
}

export function ArtworkEditor({ brand, slides, carousel, story, onBrandChange, onSlidesChange }: Props) {
  const [active, setActive] = useState(0)
  const [error, setError] = useState('')
  const preview = useRef<HTMLCanvasElement>(null)
  const index = Math.min(active, slides.length - 1)
  const slide = slides[index]

  useEffect(() => {
    let current = true
    void renderArtwork(slide, brand, story).then(art => {
      if (!current || !preview.current) return
      const canvas = preview.current
      canvas.width = art.width
      canvas.height = art.height
      canvas.getContext('2d')?.drawImage(art, 0, 0)
      setError('')
    }).catch((cause: Error) => {
      if (!current) return
      const canvas = preview.current
      canvas?.getContext('2d')?.clearRect(0, 0, canvas.width, canvas.height)
      setError(cause.message)
    })
    return () => { current = false }
  }, [slide, brand, story])

  function changeSlide(next: Slide) {
    onSlidesChange(slides.map((item, position) => position === index ? next : item))
  }

  function moveSlide(offset: number) {
    const next = [...slides]
    next.splice(index + offset, 0, ...next.splice(index, 1))
    onSlidesChange(next)
    setActive(index + offset)
  }

  async function download() {
    try {
      const blob = await createJpeg(slide, brand, story)
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = `arte-${index + 1}.jpg`
      link.click()
      window.setTimeout(() => URL.revokeObjectURL(url), 1000)
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Falha ao exportar a arte.')
    }
  }

  return (
    <section className="space-y-4 rounded-xl border bg-background p-4" aria-label="Editor de arte">
      <div className="grid gap-3 sm:grid-cols-2">
        <label>Nome da marca
          <Input value={brand.name} maxLength={48} onChange={event => onBrandChange({ ...brand, name: event.target.value })} />
        </label>
        <label>Modelo
          <select className="block w-full rounded border bg-background p-2" value={brand.template}
            onChange={event => onBrandChange({ ...brand, template: event.target.value === 'photo' ? 'photo' : 'editorial' })}>
            <option value="editorial">Editorial</option>
            <option value="photo">Foto de fundo</option>
          </select>
        </label>
        <label className="flex items-center gap-2">Fundo
          <input type="color" value={brand.background} onChange={event => onBrandChange({ ...brand, background: event.target.value })} />
        </label>
        <label className="flex items-center gap-2">Texto
          <input type="color" value={brand.foreground} onChange={event => onBrandChange({ ...brand, foreground: event.target.value })} />
        </label>
      </div>

      <label className="block">Logotipo
        <Input type="file" accept="image/jpeg,image/png,image/webp" key={brand.logo ? 'logo' : 'no-logo'}
          onChange={event => onBrandChange({ ...brand, logo: event.target.files?.[0] })} />
      </label>
      {brand.logo && <Button type="button" size="sm" variant="ghost" onClick={() => onBrandChange({ ...brand, logo: undefined })}>Remover logotipo</Button>}

      {carousel && <div className="flex flex-wrap gap-2" aria-label="Páginas do carrossel">
        {slides.map((_, position) => <Button type="button" key={position} size="sm"
          variant={position === index ? 'default' : 'outline'} aria-pressed={position === index}
          onClick={() => setActive(position)}>{position + 1}</Button>)}
        <Button type="button" size="sm" variant="outline" disabled={slides.length >= 10} onClick={() => {
          onSlidesChange([...slides, { text: '' }])
          setActive(slides.length)
        }}>Adicionar página</Button>
      </div>}

      <label className="block">Texto da arte
        <textarea className="block w-full rounded border bg-background p-2" rows={3} maxLength={180}
          value={slide.text} onChange={event => changeSlide({ ...slide, text: event.target.value })} />
      </label>
      <label className="block">Foto da página
        <Input key={`${index}-${slide.photo ? 'photo' : 'no-photo'}`} type="file" accept="image/jpeg,image/png,image/webp"
          onChange={event => changeSlide({ ...slide, photo: event.target.files?.[0] })} />
      </label>
      <div className="flex flex-wrap gap-2">
        {slide.photo && <Button type="button" variant="ghost" size="sm" onClick={() => changeSlide({ ...slide, photo: undefined })}>Remover foto</Button>}
        {carousel && <>
          <Button type="button" size="sm" variant="outline" disabled={index === 0} onClick={() => moveSlide(-1)}>Mover antes</Button>
          <Button type="button" size="sm" variant="outline" disabled={index === slides.length - 1} onClick={() => moveSlide(1)}>Mover depois</Button>
          <Button type="button" size="sm" variant="ghost" disabled={slides.length <= 2}
            onClick={() => onSlidesChange(slides.filter((_, position) => index !== position))}>Excluir página</Button>
        </>}
      </div>
      {error && <p role="alert" className="text-destructive">{error}</p>}
      <canvas ref={preview} aria-label={`Prévia da página ${index + 1}`} className="mx-auto h-auto max-h-[480px] max-w-full rounded-lg" />
      <Button type="button" variant="outline" size="sm" disabled={Boolean(error)} onClick={() => void download()}>Baixar página em JPEG</Button>
    </section>
  )
}
