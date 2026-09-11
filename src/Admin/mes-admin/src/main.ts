import { createApp } from 'vue'
import './styles/dark-theme.css'
import './styles/popper-glow-line.css'
import App from './App.vue'
import router from './router'
import { createPinia } from 'pinia'
import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'
import * as ElementPlusIconsVue from '@element-plus/icons-vue'
import { useUserStore } from '@/stores/user'
import { registerDynamicRoutes } from './router/modules'

const app = createApp(App)
const pinia = createPinia()

// 注册所有图标
for (const [key, component] of Object.entries(ElementPlusIconsVue)) {
  app.component(key, component)
}

app.use(ElementPlus)
app.use(pinia)

// 已登录用户：在 app.use(router) 前预加载用户信息并注册动态路由
// Vue Router 在 app.use(router) 时会同步 resolve 当前 URL（install -> push -> resolve 调用链）
// 动态路由必须提前注册，否则会输出 "No match found" 警告
// 对未授权或不存在的路径，提前 replaceState 到首页，避免初始 resolve 警告
const token = localStorage.getItem('token')
if (token) {
  const userStore = useUserStore(pinia)
  try {
    await userStore.getUserInfo()
    await userStore.getAuthorizedSubsystems()
    await userStore.getMenus()
    const registeredPaths = new Set<string>()
    registerDynamicRoutes(router, userStore.menus, registeredPaths)

    // 当前 URL 未匹配静态路由或已注册动态路由时，replaceState 到首页
    // 避免 app.use(router) 时 resolve 未注册路径触发 "No match found" 警告
    const currentPath = window.location.hash.replace(/^#/, '') || '/'
    const staticPaths = ['/login', '/no-permission', '/store/no-store', '/system/profile']
    if (currentPath !== '/' && !staticPaths.includes(currentPath) && !registeredPaths.has(currentPath)) {
      const firstPath = userStore.firstAuthorizedLeafPath || '/no-permission'
      window.history.replaceState({}, '', `#${firstPath}`)
    }
  } catch {
    // token 过期或加载失败：清空 token，让路由守卫跳登录页
    localStorage.removeItem('token')
  }
}

app.use(router)
app.mount('#app')
