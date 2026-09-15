export function stripLeadingBrand(
  productName: string,
  brand: string | undefined,
): string {
  if (!brand?.trim()) return productName

  const brandPrefix = new RegExp(`^${escapeRegExp(brand.trim())}\\s+`, 'i')
  return productName.replace(brandPrefix, '')
}

export function toTitleCase(input: string): string {
  return input.replace(/\S+/g, (word) =>
    /[A-Z]/.test(word) ? word : word.charAt(0).toUpperCase() + word.slice(1),
  )
}

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
}
