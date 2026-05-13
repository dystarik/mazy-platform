import { describe, it } from 'node:test'
import assert from 'node:assert/strict'

import {
  buildPreviewCommand,
  buildSmokeUrl,
  isAcceptableSmokeHtml,
} from './smoke-preview.mjs'

describe('smoke preview helpers', () => {
  it('builds the root smoke URL from host and port', () => {
    assert.equal(
      buildSmokeUrl({ host: '127.0.0.1', port: '4173' }).toString(),
      'http://127.0.0.1:4173/',
    )
  })

  it('runs Vite preview directly so the smoke check can stop it cleanly', () => {
    const command = buildPreviewCommand({
      cwd: '/repo/apps/web-client',
      host: '127.0.0.1',
      port: '4173',
    })

    assert.equal(command.command, process.execPath)
    assert.match(command.args[0], /node_modules[\\/]vite[\\/]bin[\\/]vite\.js$/)
    assert.deepEqual(command.args.slice(1), [
      'preview',
      '--host',
      '127.0.0.1',
      '--port',
      '4173',
      '--strictPort',
    ])
  })

  it('accepts built Vite html that mounts the Vue app and loads module assets', () => {
    const html = `
      <!doctype html>
      <html lang="en">
        <head>
          <script type="module" crossorigin src="/assets/index-abc123.js"></script>
        </head>
        <body><div id="app"></div></body>
      </html>
    `

    assert.equal(isAcceptableSmokeHtml(html), true)
  })

  it('rejects html that does not look like the built app shell', () => {
    assert.equal(isAcceptableSmokeHtml(''), false)
    assert.equal(isAcceptableSmokeHtml('<html><body>not mazy</body></html>'), false)
    assert.equal(isAcceptableSmokeHtml('<div id="app"></div>'), false)
  })
})
