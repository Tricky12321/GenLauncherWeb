/**
 * Extracts the message from a backend error response ({ "error": "..." }),
 * falling back to a generic message.
 */
export function errorMessage(err: any): string {
  return err?.error?.error || err?.message || 'Unknown error';
}
