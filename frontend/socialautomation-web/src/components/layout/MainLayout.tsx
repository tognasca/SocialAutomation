
import Sidebar from './Sidebar'
import Header from './Header'

export default function MainLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-screen">
      <Sidebar />
      <Header />
      <main className="ml-64 mt-16 p-6">{children}</main>
    </div>
  )
}