/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_SEED_COMPETITION_ID?: string
  readonly VITE_API_PROXY_TARGET?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
