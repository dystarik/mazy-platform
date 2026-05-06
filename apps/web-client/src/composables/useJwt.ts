interface JwtPayload {
  jti: string
  sub: string
  email: string
  exp: number
  iat: number | string
  nbf: number
  iss: string
  aud: string
}

export function decodeJwt(token: string): JwtPayload {
  const parts = token.split('.')
  if (parts.length !== 3) {
    throw new Error('Невалидный JWT токен')
  }
  const payload = parts[1]!
  const decoded = atob(payload)
  return JSON.parse(decoded) as JwtPayload
}

export function getTokenExpiration(token: string): Date {
  const { exp } = decodeJwt(token)
  return new Date(exp * 1000)
}

export function isTokenExpired(token: string): boolean {
  return getTokenExpiration(token) < new Date()
}
