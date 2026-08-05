import { cpSync, rmSync, mkdirSync, existsSync } from 'node:fs'
import { resolve, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'

const __dirname = dirname(fileURLToPath(import.meta.url))
const root = resolve(__dirname, '..')

const docfxSite = resolve(root, 'docfx/_site')
const distDocfx = resolve(root, '.vitepress/dist/docfx')

if (!existsSync(docfxSite)) {
  console.error('Error: DocFX build output not found at', docfxSite)
  console.error('Run "npm run build:docfx" first.')
  process.exit(1)
}

if (existsSync(distDocfx)) {
  rmSync(distDocfx, { recursive: true, force: true })
}

mkdirSync(distDocfx, { recursive: true })
cpSync(docfxSite, distDocfx, { recursive: true })

console.log('Copied DocFX output to .vitepress/dist/docfx/')
