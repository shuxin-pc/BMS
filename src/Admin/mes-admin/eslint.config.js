// ESLint flat config（ESLint v9+）
// 支持 Vue 3 + TypeScript
// 规则集：JS 推荐 + TS 推荐 + Vue essential（仅错误预防）
import js from '@eslint/js'
import globals from 'globals'
import pluginVue from 'eslint-plugin-vue'
import tseslint from 'typescript-eslint'

export default tseslint.config(
  // 忽略构建产物与依赖目录（flat config 默认会读取 .gitignore，此处补充非 git 场景的忽略项）
  {
    ignores: ['dist/**', 'node_modules/**', 'public/**']
  },
  // 浏览器全局对象（window/localStorage/HTMLElement/console 等），避免 no-undef 误报
  {
    languageOptions: {
      globals: {
        ...globals.browser
      }
    }
  },
  // JS 推荐规则
  js.configs.recommended,
  // TS 推荐规则
  ...tseslint.configs.recommended,
  // Vue 规则（essential 级别：仅错误预防）
  ...pluginVue.configs['flat/essential'],
  // Vue 文件中的 <script> 块使用 TypeScript 解析
  {
    files: ['**/*.vue'],
    languageOptions: {
      parserOptions: {
        parser: tseslint.parser
      }
    }
  },
  // 规则定制
  {
    rules: {
      // 下划线前缀参数为 API 签名契约（与 TS noUnusedParameters 官方行为一致），豁免未使用检查
      '@typescript-eslint/no-unused-vars': ['error', { argsIgnorePattern: '^_' }],
      // 路由页面 index.vue 为 Vue Router 页面级组件惯例，豁免多词组件名要求
      'vue/multi-word-component-names': ['error', { ignores: ['index'] }]
    }
  }
)
