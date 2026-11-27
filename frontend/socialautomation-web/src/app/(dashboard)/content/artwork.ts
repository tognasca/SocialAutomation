export type Slide = { text: string; photo?: File }
export type Brand = { name: string; background: string; foreground: string; logo?: File; template: 'editorial' | 'photo' }

export async function renderArtwork(slide: Slide, brand: Brand, story: boolean) {
  const canvas = document.createElement('canvas')
  canvas.width = 1080
  canvas.height = story ? 1920 : 1350
  const context = canvas.getContext('2d')
  if (!context) throw new Error('Não foi possível montar a arte.')

  const { width, height } = canvas
  const inset = story ? 230 : 90
  context.fillStyle = brand.background
  context.fillRect(0, 0, width, height)

  if (slide.photo) {
    const photo = await createImageBitmap(slide.photo)
    const photoHeight = brand.template === 'photo' ? height : Math.round(height * 0.46)
    const scale = Math.max(width / photo.width, photoHeight / photo.height)
    const scaledWidth = photo.width * scale
    const scaledHeight = photo.height * scale
    context.save()
    context.beginPath()
    context.rect(0, 0, width, photoHeight)
    context.clip()
    context.drawImage(photo, (width - scaledWidth) / 2, (photoHeight - scaledHeight) / 2, scaledWidth, scaledHeight)
    context.restore()
    photo.close()

    if (brand.template === 'photo') {
      context.fillStyle = brand.background + 'dd'
      context.fillRect(0, 0, width, height)
    }
  }

  const brandY = slide.photo && brand.template === 'editorial' ? Math.round(height * 0.46) + 90 : inset
  let brandX = 88
  if (brand.logo) {
    const logo = await createImageBitmap(brand.logo)
    const scale = Math.min(115 / logo.width, 88 / logo.height)
    context.drawImage(logo, brandX, brandY, logo.width * scale, logo.height * scale)
    brandX += 144
    logo.close()
  }
  context.fillStyle = brand.foreground
  context.font = 'bold 32px sans-serif'
  context.textBaseline = 'middle'
  context.fillText(brand.name, brandX, brandY + 44, width - brandX - 88)

  const textTop = brandY + 170
  const availableHeight = height - inset - textTop
  const maxWidth = width - 176
  const fontFamily = brand.template === 'editorial' ? 'Georgia, serif' : 'sans-serif'
  let fontSize = 100
  let lines: string[]
  do {
    context.font = `bold ${fontSize}px ${fontFamily}`
    lines = wrapText(context, slide.text, maxWidth)
    if (lines.length * fontSize * 1.16 <= availableHeight) break
    fontSize -= 4
  } while (fontSize >= 32)
  if (fontSize < 32) throw new Error('Reduza o texto desta página para caber na arte.')

  context.textBaseline = 'top'
  lines.forEach((line, index) => context.fillText(line, 88, textTop + index * fontSize * 1.16))
  return canvas
}

function wrapText(context: CanvasRenderingContext2D, text: string, width: number) {
  const lines: string[] = []
  for (const paragraph of text.split('\n')) {
    let line = ''
    for (const word of paragraph.trim().split(/\s+/)) {
      const candidate = line ? `${line} ${word}` : word
      if (context.measureText(candidate).width <= width) {
        line = candidate
        continue
      }
      if (line) lines.push(line)
      line = ''
      for (const character of word) {
        if (context.measureText(line + character).width > width) {
          lines.push(line)
          line = ''
        }
        line += character
      }
    }
    lines.push(line)
  }
  return lines
}

export async function createJpeg(slide: Slide, brand: Brand, story: boolean) {
  const canvas = await renderArtwork(slide, brand, story)
  return new Promise<Blob>((resolve, reject) => {
    canvas.toBlob(blob => blob ? resolve(blob) : reject(new Error('Falha ao exportar a arte.')), 'image/jpeg', 0.88)
  })
}
