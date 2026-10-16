
'use client'

import Link from 'next/link'
import { usePathname } from 'next/navigation'
import { cn } from '@/lib/utils'
import {
  LayoutDashboard,
  Users,
  MessageSquare,
  Settings,
  Instagram,
  Bot,
  BarChart3,
  Zap,
  FileText,
} from 'lucide-react'


const menuItems = [
  { label: 'Dashboard', href: '/dashboard', icon: LayoutDashboard },
  { label: 'CRM', href: '/crm/leads', icon: Users, disabled: false }, // <-- Habilitado
  { label: 'Inbox', href: '/inbox', icon: MessageSquare, disabled: false }, // <-- Habilitado { label: 'Automação', href: '/automation', icon: Zap, disabled: true },
  { label: 'Conteúdo', href: '/content', icon: FileText, disabled: false }, // <-- Habilitado
  { label: 'IA', href: '/ai', icon: Bot, disabled: false },// <-- Habilitado
  { label: 'Automação', href: '/automation', icon: Zap, disabled: false },// <-- Habilitado
  { label: 'Instagram', href: '/instagram', icon: Instagram, disabled: false },// <-- Habilitado
  { label: 'Analytics', href: '/analytics', icon: BarChart3, disabled: false },// <-- Habilitado
  { label: 'Configurações', href: '/settings', icon: Settings },
]

export default function Sidebar() {
  const pathname = usePathname()

  return (
    <aside className="fixed left-0 top-0 z-40 h-screen w-64 border-r bg-background">
      <div className="flex h-16 items-center border-b px-6">
        <h1 className="text-xl font-bold text-primary">SocialAuto</h1>
      </div>

      <nav className="space-y-1 p-4">
        {menuItems.map((item) => {
          const Icon = item.icon
          const isActive = pathname.startsWith(item.href)

          return (
            <Link
              key={item.href}
              href={item.disabled ? '#' : item.href}
              className={cn(
                'flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition-colors',
                isActive
                  ? 'bg-primary/10 text-primary font-medium'
                  : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground',
                item.disabled && 'opacity-50 cursor-not-allowed'
              )}
            >
              <Icon className="h-4 w-4" />
              {item.label}
              {item.disabled && (
                <span className="ml-auto text-xs bg-muted px-1.5 py-0.5 rounded">Em breve</span>
              )}
            </Link>
          )
        })}
      </nav>
    </aside>
  )
}