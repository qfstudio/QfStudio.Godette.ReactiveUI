import { createRequire } from 'node:module'
import { defineConfig } from 'vitepress'

const require = createRequire(import.meta.url)

// DocFX artifacts are deployed as static subdirectories and do not go through VitePress routing.
// Use full external URLs to avoid interference from locale routing.
const docfxUrl = 'https://qfstudio.github.io/QfStudio.Godette.ReactiveUI/docfx/'

export default defineConfig({
  title: 'QfStudio.Godette.ReactiveUI',
  description: 'ReactiveUI integration for Godot Engine',
  base: '/QfStudio.Godette.ReactiveUI/',
  srcDir: '../Docs/Tutorials',

  head: [
    ['meta', { name: 'google-site-verification', content: '4fA6AlqB1c819hhyKGEsEqA6BQ8Et-sIbUILkaF3zpI' }]
  ],

  sitemap: {
    hostname: 'https://qfstudio.github.io/QfStudio.Godette.ReactiveUI/'
  },

  // Workaround: When srcDir points outside the project root, Vite cannot resolve modules such as vue.
  // This plugin manually resolves module paths, based on a solution provided by an official VitePress maintainer.
  // https://github.com/vuejs/vitepress/issues/4612
  vite: {
    plugins: [
      {
        name: 'node-resolve-from-different-root',
        resolveId(id) {
          try {
            const resolved = require.resolve(id)
            if (resolved) return { id: resolved }
          } catch (e) {}
        }
      }
    ]
  },

  locales: {
    root: {
      label: 'English',
      lang: 'en',
      themeConfig: {
        nav: [
          { text: 'Guide', link: '/guide/getting-started' },
          { text: 'API', link: docfxUrl },
          {
            text: 'GitHub',
            link: 'https://github.com/qfstudio/QfStudio.Godette.ReactiveUI'
          }
        ],
        sidebar: {
          '/guide/': [
            {
              text: 'Getting Started',
              items: [
                { text: 'Quick Start', link: '/guide/getting-started' },
                { text: 'Core Concepts', link: '/guide/concepts' }
              ]
            },
            {
              text: 'Binding',
              items: [
                { text: 'Data Binding', link: '/guide/data-binding' },
                { text: 'Command Binding', link: '/guide/command-binding' },
                { text: 'Collection Binding', link: '/guide/collection-binding' },
                { text: 'Validation', link: '/guide/validation' },
                { text: 'Interaction', link: '/guide/interaction' }
              ]
            },
            {
              text: 'Reactive Extensions',
              items: [
                { text: 'Activation Lifecycle', link: '/guide/activation' },
                { text: 'Signal to Observable', link: '/guide/signal-observable' },
                { text: 'Schedulers', link: '/guide/schedulers' },
                { text: 'Frame Operators', link: '/guide/operators' }
              ]
            },
            {
              text: 'Navigation',
              items: [
                { text: 'View Locator', link: '/guide/view-locator' },
                { text: 'Routing', link: '/guide/routing' }
              ]
            }
          ]
        }
      }
    },
    zh: {
      label: '简体中文',
      lang: 'zh-CN',
      link: '/zh/',
      themeConfig: {
        nav: [
          { text: '指南', link: '/zh/guide/getting-started' },
          { text: 'API', link: docfxUrl },
          {
            text: 'GitHub',
            link: 'https://github.com/qfstudio/QfStudio.Godette.ReactiveUI'
          }
        ],
        sidebar: {
          '/zh/guide/': [
            {
              text: '入门',
              items: [
                { text: '快速开始', link: '/zh/guide/getting-started' },
                { text: '核心概念', link: '/zh/guide/concepts' }
              ]
            },
            {
              text: '绑定',
              items: [
                { text: '数据绑定', link: '/zh/guide/data-binding' },
                { text: '命令绑定', link: '/zh/guide/command-binding' },
                { text: '集合绑定', link: '/zh/guide/collection-binding' },
                { text: '验证', link: '/zh/guide/validation' },
                { text: 'Interaction', link: '/zh/guide/interaction' }
              ]
            },
            {
              text: 'Reactive Extensions',
              items: [
                { text: '激活生命周期', link: '/zh/guide/activation' },
                { text: '信号转 Observable', link: '/zh/guide/signal-observable' },
                { text: '调度器', link: '/zh/guide/schedulers' },
                { text: '帧运算符', link: '/zh/guide/operators' }
              ]
            },
            {
              text: '导航',
              items: [
                { text: '视图定位器', link: '/zh/guide/view-locator' },
                { text: '路由', link: '/zh/guide/routing' }
              ]
            }
          ]
        }
      }
    }
  },

  themeConfig: {
    socialLinks: [
      {
        icon: 'github',
        link: 'https://github.com/qfstudio/QfStudio.Godette.ReactiveUI'
      }
    ]
  }
})
