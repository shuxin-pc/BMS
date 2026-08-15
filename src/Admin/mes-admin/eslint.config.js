// ESLint flat config（ESLint v9+）
// 支持 Vue 3 + TypeScript
// 规则集：JS 推荐 + TS 推荐 + Vue essential（仅错误预防，减少对现有代码的风格噪音）
import js from '@eslint/js'
import pluginVue from 'eslint-plugin-vue'
import tseslint from 'typescript-eslint'

export default tseslint.config(
  // 忽略构建产物与依赖目录（flat config 默认会读取 .gitignore，此处补充非 git 场景的忽略项）
  {
    ignores: ['dist/**', 'node_modules/**', 'public/**']
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
  }
)
