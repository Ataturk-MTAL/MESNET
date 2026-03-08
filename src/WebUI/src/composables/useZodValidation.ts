/**
 * Zod şema validasyonu — form errors objesini otomatik doldurur.
 * Kullanım: `if (!zodValidate(schema, formData, errors)) return`
 */
export function zodValidate<T>(
  schema: {
    safeParse: (data: unknown) => {
      success: boolean
      error?: { issues: { path: (string | number)[]; message: string }[] }
      data?: T
    }
  },
  data: unknown,
  errors: Record<string, string>,
): data is T {
  for (const key of Object.keys(errors)) errors[key] = ''
  const result = schema.safeParse(data)
  if (result.success) return true
  for (const issue of result.error!.issues) {
    const field = issue.path[0]
    if (field && typeof field === 'string' && !errors[field]) {
      errors[field] = issue.message
    }
  }
  return false
}
