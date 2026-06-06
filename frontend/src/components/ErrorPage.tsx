import { useRouteError, isRouteErrorResponse, useNavigate } from 'react-router-dom'

export default function ErrorPage() {
  const error = useRouteError()
  const navigate = useNavigate()

  const message = isRouteErrorResponse(error)
    ? error.statusText
    : error instanceof Error
    ? error.message
    : 'Erro inesperado'

  return (
    <div className="flex h-screen flex-col items-center justify-center gap-4 p-8">
      <p className="text-2xl font-semibold text-destructive">Algo deu errado</p>
      <p className="text-sm text-muted-foreground text-center max-w-md">{message}</p>
      <button
        onClick={() => navigate('/')}
        className="rounded-md bg-primary px-4 py-2 text-sm text-primary-foreground hover:opacity-90"
      >
        Voltar ao início
      </button>
    </div>
  )
}
