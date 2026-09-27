export const config = {
  apiUrl: process.env.NEXT_PUBLIC_API_URL ?? "",
  uploadUrl: process.env.NEXT_PUBLIC_UPLOAD_URL ?? process.env.NEXT_PUBLIC_API_URL ?? "",
  debugUserId: process.env.NEXT_PUBLIC_DEBUG_USER_ID,
} as const;
