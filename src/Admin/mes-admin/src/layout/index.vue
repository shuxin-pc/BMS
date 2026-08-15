<template>
  <div class="layout-container">
    <!-- 背景装饰 -->
    <div class="bg-decoration">
      <div class="glow-orb glow-orb-1"></div>
      <div class="glow-orb glow-orb-2"></div>
      <div class="grid-lines"></div>
    </div>

    <!-- 侧边栏 -->
    <div class="sidebar-container" :class="{ 'is-collapse': isCollapse }">
      <div class="sidebar-logo" v-if="systemConfig.loaded && systemConfig.systemLogo">
        <div class="logo-wrapper">
          <img v-if="isBase64Image(systemConfig.systemLogo)" :src="systemConfig.systemLogo" class="logo-icon" alt="logo">
          <div v-else class="logo-icon" v-html="systemConfig.systemLogo"></div>
        </div>
        <transition name="fade">
          <span v-show="!isCollapse" class="logo-text">
            <span class="logo-title">{{ systemConfig.systemShortName }}</span>
            <span class="logo-version">{{ systemConfig.systemVersion }}</span>
          </span>
        </transition>
      </div>
      <div class="collapse-trigger" @click="toggleCollapse" title="折叠/展开菜单">
        <el-icon><Fold v-if="!isCollapse" /><Expand v-if="isCollapse" /></el-icon>
      </div>
      <el-menu
        ref="menuRef"
        :default-active="activeMenu"
        :collapse="isCollapse"
        :unique-opened="true"
        router
        class="sidebar-menu"
      >
        <template v-for="menu in menus" :key="menu.id">
          <el-sub-menu v-if="menu.children && menu.children.length > 0" :index="menu.path">
            <template #title>
              <el-icon><component :is="menu.icon" /></el-icon>
              <span>{{ menu.name }}</span>
            </template>
            <el-menu-item
              v-for="subMenu in menu.children"
              :key="subMenu.id"
              :index="subMenu.path"
            >
              <el-icon><component :is="subMenu.icon" /></el-icon>
              <span>{{ subMenu.name }}</span>
            </el-menu-item>
          </el-sub-menu>
          <el-menu-item v-else :index="menu.path">
            <el-icon><component :is="menu.icon" /></el-icon>
            <span>{{ menu.name }}</span>
          </el-menu-item>
        </template>
      </el-menu>

      <!-- 侧边栏底部装饰 -->
      <div class="sidebar-footer">
        <div class="system-status">
          <span class="status-dot online"></span>
          <transition name="fade">
            <span v-show="!isCollapse" class="status-text">系统正常</span>
          </transition>
        </div>
        <transition name="fade">
          <div v-show="!isCollapse" class="copyright-text">
            {{ systemConfig.systemCopyright }}
          </div>
        </transition>
      </div>
    </div>

    <!-- 主内容区域 -->
    <div class="main-container">
      <!-- 顶部导航栏 -->
      <div class="header-container">
        <div class="header-left">
          <!-- 子系统入口图标 -->
          <div class="subsystem-icons">
            <div
              v-for="subsystem in subsystems"
              :key="subsystem.id"
              class="subsystem-icon"
              :class="{ active: currentSubsystemId === String(subsystem.id) }"
              :title="subsystem.name"
              @click="handleSubsystemClick(subsystem.id)"
            >
              <div class="icon-glow"></div>
              <el-icon><component :is="getIconComponent(subsystem.icon)" /></el-icon>
              <span class="icon-label">{{ subsystem.name }}</span>
            </div>
          </div>

          <!-- 门店切换器（仅 store 子系统显示） -->
          <div v-if="isStoreSubsystem" class="store-switcher">
            <!-- 单店场景：纯展示店名（不显示下拉箭头，无 click 行为） -->
            <div v-if="authorizedStores.length === 1" class="store-switcher-single">
              <el-icon class="store-icon"><OfficeBuilding /></el-icon>
              <span class="store-name">{{ currentStoreName }}</span>
            </div>

            <!-- 多店场景：显示 el-dropdown 可切换（默认选中创建时间最早的门店） -->
            <el-dropdown
              v-else-if="authorizedStores.length > 1"
              trigger="click"
              @command="handleStoreSwitch"
            >
              <div class="store-switcher-trigger">
                <el-icon class="store-icon"><OfficeBuilding /></el-icon>
                <span class="store-name">{{ currentStoreName || '请选择门店' }}</span>
                <el-icon class="arrow-icon"><ArrowDown /></el-icon>
              </div>
              <template #dropdown>
                <el-dropdown-menu class="store-dropdown">
                  <el-dropdown-item
                    v-for="store in authorizedStores"
                    :key="store.id"
                    :command="store.id"
                    :class="{ 'is-active': String(store.id) === currentStoreId }"
                  >
                    <el-icon><OfficeBuilding /></el-icon>
                    {{ store.name }}
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>

            <!-- 0 店场景：占位提示 -->
            <div v-else class="store-switcher-empty">
              <el-icon class="store-icon"><OfficeBuilding /></el-icon>
              <span class="store-name">暂无授权门店</span>
            </div>
          </div>
        </div>
        <div class="header-right">
          <!-- 搜索框 -->
          <div class="header-search">
            <el-input
              v-model="searchQuery"
              placeholder="搜索功能、数据..."
              :prefix-icon="SearchIcon"
              size="default"
            />
          </div>

          <!-- 消息铃铛 -->
          <MessageBell />

          <!-- 用户菜单 -->
          <el-dropdown @command="handleCommand" popper-class="user-dropdown-popper">
            <div class="user-info">
              <div class="user-avatar">
                <el-avatar :size="36" :src="userInfo.avatar">
                  <el-icon><User /></el-icon>
                </el-avatar>
                <div class="avatar-ring"></div>
              </div>
              <div class="user-details">
                <span class="username">{{ userInfo.realName }}</span>
                <span class="user-role">管理员</span>
              </div>
              <el-icon class="arrow-icon"><ArrowDown /></el-icon>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <div class="user-dropdown-header">
                  <span class="user-status-dot"></span>
                  <span class="user-account">{{ userInfo.userName }}</span>
                </div>
                <el-dropdown-item command="profile">
                  <el-icon><User /></el-icon>
                  <span>个人中心</span>
                  <span class="dropdown-arrow">›</span>
                </el-dropdown-item>
                <el-dropdown-item command="setting" v-if="userStore.hasPermission('system:config:view')">
                  <el-icon><Setting /></el-icon>
                  <span>系统设置</span>
                  <span class="dropdown-arrow">›</span>
                </el-dropdown-item>
                <div class="user-dropdown-divider"></div>
                <el-dropdown-item command="logout" class="is-logout">
                  <el-icon><SwitchButton /></el-icon>
                  <span>退出登录</span>
                  <span class="dropdown-arrow">›</span>
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </div>

      <!-- 内容区域 -->
      <div class="content-container">
        <router-view v-slot="{ Component }">
          <transition name="fade-transform" mode="out-in">
            <!-- key 拼入 currentStoreId：门店切换时 key 变化，强制当前页组件重新挂载以刷新数据 -->
            <component :is="Component" :key="$route.fullPath + (currentStoreId ? `_${currentStoreId}` : '')" />
          </transition>
        </router-view>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch, markRaw, nextTick } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useUserStore } from '@/stores/user'
import { useSystemConfigStore } from '@/stores/systemConfig'
import { useMessageHub } from '@/composables/useMessageHub'
import { recordAuditLog } from '@/api/system'
import MessageBell from './components/MessageBell.vue'
import {
  Fold, Expand, ArrowDown, User, Setting, SwitchButton, Box, Calendar, List, Check, Tools, Search, Bell,
  DataAnalysis, PieChart, TrendCharts, Histogram, Monitor, Printer,
  Connection, Link, Cloudy,
  Star, Flag, House, Grid, Menu,
  Upload, Download, Picture, Camera, VideoCamera, OfficeBuilding, School,
  CreditCard, Money, Phone, ChatDotRound, ChatLineRound, Pointer, Aim, Coordinate,
  Location, LocationInformation, MagicStick, Brush, Sunrise, Sunny, Moon,
  PartlyCloudy, Drizzling, Pouring, Lightning, Sunset
} from '@element-plus/icons-vue'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()
const systemConfig = useSystemConfigStore()
const { start: startMessageHub, stop: stopMessageHub } = useMessageHub()
const isCollapse = ref(false)
const searchQuery = ref('')
const menus = ref<any[]>([])
const menuRef = ref()
// el-menu 选中项：用 ref 控制，菜单数据异步加载后需强制触发选中
const activeMenu = ref(route.path)

// 路由变化时同步选中项
watch(() => route.path, (path) => {
  activeMenu.value = path
})

// 查找当前路由对应的父级菜单路径（用于展开 sub-menu）
const findParentMenuPath = (menus: any[], targetPath: string): string | null => {
  for (const menu of menus) {
    if (menu.children && menu.children.length > 0) {
      const found = menu.children.some((child: any) => child.path === targetPath)
      if (found) return menu.path
    }
  }
  return null
}

// 监听 menus 变化，展开当前菜单的父级并触发选中
watch(menus, async (newMenus) => {
  if (newMenus.length > 0) {
    // 等待下一帧，确保 el-menu 已渲染
    await nextTick()
    await nextTick()
    await new Promise(r => setTimeout(r, 100))

    // 只展开当前路由对应的父级菜单（unique-opened 只允许展开一个）
    if (menuRef.value) {
      const parentPath = findParentMenuPath(newMenus, route.path)
      if (parentPath) {
        menuRef.value.open(parentPath)
      }
    }

    // 菜单数据异步加载后，强制 el-menu 重新选中当前路由
    await nextTick()
    activeMenu.value = ''
    await nextTick()
    activeMenu.value = route.path
  }
}, { immediate: true })

const userInfo = computed(() => userStore.userInfo)

// 授权的子系统列表 - 从 userStore 获取
const subsystems = computed(() => userStore.authorizedSubsystems)
// 当前选中的子系统ID - 从 userStore 获取
const currentSubsystemId = computed(() => userStore.currentSubsystemId)
// 是否为 store 子系统（控制门店切换器显示）
const isStoreSubsystem = computed(() => userStore.isStoreSubsystem)
// 授权门店列表
const authorizedStores = computed(() => userStore.authorizedStores)
// 当前门店ID
const currentStoreId = computed(() => userStore.currentStoreId)
// 当前门店名称
const currentStoreName = computed(() => userStore.currentStoreName)

// 图标名称到组件的映射
// 图标组件使用 markRaw 避免响应式开销（名称与 IconPicker 保持一致）
const iconMap: Record<string, any> = {
  Setting: markRaw(Setting),
  Box: markRaw(Box),
  Calendar: markRaw(Calendar),
  List: markRaw(List),
  Check: markRaw(Check),
  Tools: markRaw(Tools),
  User: markRaw(User),
  Search: markRaw(Search),
  Bell: markRaw(Bell),
  // 额外添加 IconPicker 中的其他图标
  DataAnalysis: markRaw(DataAnalysis),
  PieChart: markRaw(PieChart),
  TrendCharts: markRaw(TrendCharts),
  Histogram: markRaw(Histogram),
  Monitor: markRaw(Monitor),
  Printer: markRaw(Printer),
  Connection: markRaw(Connection),
  Link: markRaw(Link),
  Star: markRaw(Star),
  Flag: markRaw(Flag),
  House: markRaw(House),
  Grid: markRaw(Grid),
  Menu: markRaw(Menu),
  Fold: markRaw(Fold),
  Expand: markRaw(Expand),
  Upload: markRaw(Upload),
  Download: markRaw(Download),
  Picture: markRaw(Picture),
  Camera: markRaw(Camera),
  VideoCamera: markRaw(VideoCamera),
  OfficeBuilding: markRaw(OfficeBuilding),
  School: markRaw(School),
  CreditCard: markRaw(CreditCard),
  Money: markRaw(Money),
  Phone: markRaw(Phone),
  ChatDotRound: markRaw(ChatDotRound),
  ChatLineRound: markRaw(ChatLineRound),
  Pointer: markRaw(Pointer),
  Aim: markRaw(Aim),
  Coordinate: markRaw(Coordinate),
  Location: markRaw(Location),
  LocationInformation: markRaw(LocationInformation),
  MagicStick: markRaw(MagicStick),
  Brush: markRaw(Brush),
  Sunrise: markRaw(Sunrise),
  Sunny: markRaw(Sunny),
  Moon: markRaw(Moon),
  Cloudy: markRaw(Cloudy),
  PartlyCloudy: markRaw(PartlyCloudy),
  Drizzling: markRaw(Drizzling),
  Pouring: markRaw(Pouring),
  Lightning: markRaw(Lightning),
  Sunset: markRaw(Sunset)
}

// 动态获取图标组件
const getIconComponent = (iconName?: string) => {
  if (!iconName) return iconMap.Setting
  return iconMap[iconName] || iconMap.Setting
}

// 图标组件使用 markRaw 避免响应式开销
const SearchIcon = markRaw(Search)

// 判断是否为 base64 图片格式
const isBase64Image = (value: string): boolean => {
  return value.startsWith('data:image')
}

// 面包屑（预留）
const _breadcrumbs = computed(() => {
  const matched = route.matched.filter(item => item.meta && item.meta.title)
  return matched.map(item => ({
    path: item.path,
    title: item.meta.title
  }))
})
void _breadcrumbs // 占位，避免未使用警告

const toggleCollapse = () => {
  isCollapse.value = !isCollapse.value
}

/**
 * 处理子系统点击切换
 * 切换子系统后更新左侧菜单，并跳转到该子系统的首页
 * 新子系统无授权菜单时跳转无权限页
 */
const handleSubsystemClick = async (subsystemId: number | string) => {
  const id = String(subsystemId)
  if (id === currentSubsystemId.value) return

  // 切换子系统
  await userStore.switchSubsystem(id)

  // 更新本地菜单数据
  menus.value = userStore.menus

  // 跳转到该子系统的首页（已授权菜单中排序第1的叶子菜单）
  const firstPath = userStore.firstAuthorizedLeafPath
  if (firstPath && firstPath !== route.path) {
    router.push(firstPath)
  } else if (!firstPath) {
    // 新子系统无授权菜单，跳转无权限页
    router.push('/no-permission')
  }
}

/**
 * 处理门店切换
 * 切换后无需重新加载菜单（菜单由子系统决定，与门店无关）
 */
const handleStoreSwitch = async (storeId: number | string) => {
  const id = String(storeId)
  if (id === currentStoreId.value) return
  await userStore.switchStore(id)
  ElMessage.success(`已切换到门店：${currentStoreName.value}`)
}

const handleCommand = (command: string) => {
  if (command === 'logout') {
    ElMessageBox.confirm('确定要退出登录吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    }).then(async () => {
      // 断开 SignalR 消息连接（登出前触发，避免无效重连）
      await stopMessageHub()

      // 记录登出审计日志
      try {
        await recordAuditLog({
          operationType: 'Logout',
          userId: userStore.userInfo.id,
          userName: userStore.userInfo.userName,
          realName: userStore.userInfo.realName,
          tenantId: userStore.userInfo.tenantId as number,
          responseStatus: 200,
          requestPath: '/logout'
        })
      } catch (error) {
        // 记录登出审计日志失败，不影响退出流程
      }

      // 清除本地状态并跳转
      await userStore.logout()
      ElMessage.success('退出成功')
      router.push('/login')
    }).catch(() => {
      // 用户点击取消或关闭对话框，无需处理
    })
  } else if (command === 'profile') {
    router.push('/system/profile')
  } else if (command === 'setting') {
    router.push('/system/system-configs')
  }
}

// 加载权限数据
const loadPermissionData = async () => {
  await userStore.getAuthorizedSubsystems()
  await userStore.getMenus()
  menus.value = userStore.menus

  // 刷新场景下，若当前已在 store 子系统，需补载授权门店列表
  if (userStore.isStoreSubsystem && userStore.authorizedStores.length === 0) {
    await userStore.getAuthorizedStores()
  }
}

onMounted(async () => {
  // 确保系统配置已加载（刷新后 store 状态丢失）
  if (!systemConfig.loaded) {
    await systemConfig.loadSystemConfigs()
  }

  // 如果权限数据已加载（路由守卫已处理），则不再重复加载
  // 只有在数据为空时才加载
  if (userStore.authorizedSubsystems.length === 0) {
    await loadPermissionData()
  } else {
    menus.value = userStore.menus
  }

  // 建立 SignalR 消息连接（登录后触发）
  startMessageHub()
})

// 监听用户 ID 变化，当切换用户时重新加载权限数据
// 只有从无用户（0）变为有新用户时才重新加载
watch(() => userStore.userInfo.id, (newId, oldId) => {
  // oldId 为 0 或无效值，且 newId > 0 = 新用户登录
  if (newId > 0 && (!oldId || oldId === 0)) {
    loadPermissionData()
  }
})
</script>

<style scoped>
/* 布局容器 */
.layout-container {
  display: flex;
  width: 100%;
  height: 100%;
  position: relative;
  overflow: hidden;
}

/* 背景装饰 */
.bg-decoration {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  pointer-events: none;
  z-index: 0;
  overflow: hidden;
}

.glow-orb {
  position: absolute;
  border-radius: 50%;
  filter: blur(80px);
  opacity: 0.15;
  animation: float 20s ease-in-out infinite;
}

.glow-orb-1 {
  width: 600px;
  height: 600px;
  background: var(--primary);
  top: -200px;
  right: -100px;
}

.glow-orb-2 {
  width: 400px;
  height: 400px;
  background: var(--info);
  bottom: -100px;
  left: -100px;
  animation-delay: -10s;
}

@keyframes float {
  0%, 100% { transform: translate(0, 0); }
  25% { transform: translate(30px, -30px); }
  50% { transform: translate(-20px, 20px); }
  75% { transform: translate(20px, 30px); }
}

.grid-lines {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-image:
    linear-gradient(rgba(6, 212, 228, 0.03) 1px, transparent 1px),
    linear-gradient(90deg, rgba(6, 212, 228, 0.03) 1px, transparent 1px);
  background-size: 30px 30px;
}

/* 侧边栏 */
.sidebar-container {
  width: 220px;
  background: var(--bg-primary);
  transition: width 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  position: relative;
  z-index: 1001;
  border-right: 1px solid var(--border-primary);
  display: flex;
  flex-direction: column;
}

.sidebar-container.is-collapse {
  width: 72px;
}

.sidebar-logo {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 70px;
  padding: 12px 16px;
  background: var(--bg-primary);
  border-bottom: 1px solid var(--border-primary);
  gap: 12px;
}

.logo-wrapper {
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.logo-icon {
  width: 36px;
  height: 36px;
  color: var(--primary);
  filter: drop-shadow(0 0 8px var(--primary-glow));
}

.logo-icon :deep(svg) {
  width: 100%;
  height: 100%;
}

.logo-text {
  display: flex;
  align-items: baseline;
  gap: 6px;
  white-space: nowrap;
  overflow: hidden;
}

.logo-title {
  font-size: 20px;
  font-weight: 700;
  color: var(--text-primary);
  letter-spacing: 2px;
}

.logo-version {
  font-size: 11px;
  font-weight: 500;
  color: var(--primary);
  padding: 2px 6px;
  background: rgba(6, 212, 228, 0.15);
  border-radius: 4px;
}

.collapse-trigger {
  position: absolute;
  right: -14px;
  top: 24px;
  width: 28px;
  height: 28px;
  border-radius: 50%;
  background: var(--bg-tertiary);
  border: 1px solid var(--border-secondary);
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  color: var(--text-tertiary);
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  font-size: 14px;
  z-index: 1002;
  box-shadow: var(--shadow-md);
}

.collapse-trigger:hover {
  background: var(--primary);
  color: var(--bg-primary);
  border-color: var(--primary);
  box-shadow: var(--shadow-glow-primary);
  transform: scale(1.1);
}

.sidebar-container.is-collapse .collapse-trigger {
  right: -14px;
}

/* 侧边栏菜单 */
.sidebar-menu {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
  padding: 12px 8px;
}

:deep(.el-menu) {
  border-right: none !important;
  background: transparent !important;
}

:deep(.el-menu-item),
:deep(.el-sub-menu__title) {
  height: 44px;
  line-height: 44px;
  margin: 4px 0;
  border-radius: var(--radius-md);
  color: var(--text-tertiary) !important;
  transition: all 0.3s ease;
}

:deep(.el-menu-item:hover),
:deep(.el-sub-menu__title:hover) {
  background: var(--bg-hover) !important;
  color: var(--text-primary) !important;
}

:deep(.el-menu-item.is-active) {
  color: var(--primary) !important;
  background: rgba(6, 212, 228, 0.1) !important;
  position: relative;
}

:deep(.el-menu-item.is-active)::before {
  content: '';
  position: absolute;
  left: 0;
  top: 50%;
  transform: translateY(-50%);
  height: 24px;
  width: 3px;
  background: var(--primary);
  border-radius: 0 2px 2px 0;
  box-shadow: 0 0 10px var(--primary);
}

:deep(.el-sub-menu.is-active > .el-sub-menu__title) {
  color: var(--primary) !important;
}

:deep(.el-sub-menu .el-menu) {
  padding-left: 8px;
}

/* 侧边栏底部 */
.sidebar-footer {
  padding: 16px;
  border-top: 1px solid var(--border-primary);
}

.system-status {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 12px;
  background: var(--bg-tertiary);
  border-radius: var(--radius-md);
  border: 1px solid var(--border-primary);
}

.status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  flex-shrink: 0;
}

.status-dot.online {
  background: var(--success);
  box-shadow: 0 0 8px var(--success);
  animation: pulse 2s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.5; }
}

.status-text {
  font-size: 12px;
  color: var(--text-tertiary);
}

.copyright-text {
  font-size: 10px;
  color: var(--text-disabled);
  text-align: center;
  margin-top: 8px;
  padding-top: 8px;
  border-top: 1px solid var(--border-primary);
}

/* 主内容区域 */
.main-container {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  background: var(--bg-secondary);
  position: relative;
  z-index: 1;
}

/* 顶部导航栏 */
.header-container {
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: 70px;
  padding: 0 24px;
  background: linear-gradient(180deg, var(--bg-primary) 0%, rgba(15, 20, 25, 0.9) 100%);
  color: white;
  border-bottom: 1px solid var(--border-primary);
  position: relative;
}

.header-container::after {
  content: '';
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  height: 1px;
  background: linear-gradient(90deg, transparent, var(--primary), transparent);
  opacity: 0.5;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 20px;
}

.subsystem-icons {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 0 8px;
}

.subsystem-icon {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  width: 72px;
  height: 56px;
  border-radius: var(--radius-md);
  background: var(--bg-tertiary);
  border: 1px solid var(--border-primary);
  cursor: pointer;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  color: var(--text-tertiary);
  font-size: 20px;
  position: relative;
  overflow: hidden;
}

.icon-glow {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 2px;
  background: var(--primary);
  transform: scaleX(0);
  transition: transform 0.3s ease;
}

.subsystem-icon:hover .icon-glow,
.subsystem-icon.active .icon-glow {
  transform: scaleX(1);
}

.icon-label {
  font-size: 10px;
  margin-top: 4px;
  white-space: nowrap;
  font-weight: 500;
}

.subsystem-icon:hover,
.subsystem-icon.active {
  background: rgba(6, 212, 228, 0.1);
  border-color: var(--primary);
  color: var(--primary);
  transform: translateY(-2px);
  box-shadow: var(--shadow-glow-primary);
}

.subsystem-icon.active {
  background: rgba(6, 212, 228, 0.15);
}

/* 门店切换器 */
.store-switcher {
  display: flex;
  align-items: center;
  margin-left: 12px;
}

.store-switcher-trigger {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  height: 40px;
  border-radius: var(--radius-md);
  background: var(--bg-tertiary);
  border: 1px solid var(--primary);
  cursor: pointer;
  transition: all 0.3s ease;
  color: var(--primary);
  box-shadow: 0 0 0 1px var(--primary-glow);
}

.store-switcher-trigger:hover {
  background: rgba(6, 212, 228, 0.1);
  box-shadow: var(--shadow-glow-primary);
}

/* 单店场景：纯展示样式（与触发器类似但无 hover 效果、无 click 行为） */
.store-switcher-single {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  height: 40px;
  border-radius: var(--radius-md);
  background: var(--bg-tertiary);
  border: 1px solid var(--border-primary);
  color: var(--text-secondary);
  cursor: default;
  user-select: none;
}

.store-switcher-single .store-name {
  font-size: 13px;
  font-weight: 500;
  max-width: 160px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* 0 店场景：次要色占位 */
.store-switcher-empty {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  height: 40px;
  border-radius: var(--radius-md);
  background: var(--bg-tertiary);
  border: 1px dashed var(--border-secondary);
  color: var(--text-disabled);
  cursor: default;
  user-select: none;
}

.store-switcher-empty .store-icon {
  font-size: 16px;
}

.store-switcher-empty .store-name {
  font-size: 13px;
  font-weight: 400;
}

.store-switcher-trigger .store-icon {
  font-size: 16px;
}

.store-switcher-trigger .store-name {
  font-size: 13px;
  font-weight: 500;
  max-width: 160px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.store-switcher-trigger .arrow-icon {
  font-size: 12px;
}

.store-dropdown {
  background: var(--bg-primary) !important;
  border: 1px solid var(--border-primary) !important;
  padding: 8px !important;
  box-shadow: var(--shadow-lg) !important;
  max-height: 360px;
  overflow-y: auto;
}

.store-dropdown :deep(.el-dropdown-menu__item) {
  padding: 10px 16px;
  color: var(--text-secondary);
  border-radius: var(--radius-md);
  margin: 2px 0;
  display: flex;
  align-items: center;
  gap: 8px;
}

.store-dropdown :deep(.el-dropdown-menu__item:hover) {
  background: var(--bg-hover);
  color: var(--primary);
}

.store-dropdown :deep(.el-dropdown-menu__item.is-active) {
  color: var(--primary);
  background: rgba(6, 212, 228, 0.1);
  font-weight: 500;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 16px;
  height: 100%;
}

/* 搜索框 */
.header-search {
  width: 280px;
}

.header-search :deep(.el-input__wrapper) {
  background: var(--bg-tertiary) !important;
  border: 1px solid var(--border-primary);
  border-radius: var(--radius-lg);
  box-shadow: none !important;
}

.header-search :deep(.el-input__wrapper:hover),
.header-search :deep(.el-input__wrapper.is-focus) {
  border-color: var(--primary) !important;
  box-shadow: 0 0 0 2px var(--primary-glow) !important;
}

/* 头部图标按钮 */
.header-icon-btn {
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: var(--radius-md);
  background: var(--bg-tertiary);
  border: 1px solid var(--border-primary);
  cursor: pointer;
  color: var(--text-tertiary);
  transition: all 0.3s ease;
  position: relative;
}

.header-icon-btn:hover {
  background: var(--bg-hover);
  border-color: var(--primary);
  color: var(--primary);
}

.notification-badge :deep(.el-badge__content) {
  background: var(--danger);
  border: none;
}

/* 用户信息 */
.user-info {
  display: flex;
  align-items: center;
  gap: 12px;
  height: 100%;
  padding: 0 12px;
  cursor: pointer;
  transition: background 0.3s;
  border-radius: var(--radius-md);
}

.user-info:hover {
  background: var(--bg-hover);
}

.user-avatar {
  position: relative;
}

.avatar-ring {
  position: absolute;
  top: -3px;
  left: -3px;
  right: -3px;
  bottom: -3px;
  border-radius: 50%;
  border: 2px solid var(--primary);
  opacity: 0.5;
  animation: ring-pulse 2s ease-in-out infinite;
}

@keyframes ring-pulse {
  0%, 100% { transform: scale(1); opacity: 0.5; }
  50% { transform: scale(1.1); opacity: 0.2; }
}

.user-details {
  display: flex;
  flex-direction: column;
}

.username {
  font-size: 14px;
  color: var(--text-primary);
  font-weight: 500;
  line-height: 1.3;
}

.user-role {
  font-size: 11px;
  color: var(--primary);
}

.user-info .arrow-icon {
  font-size: 12px;
  color: var(--text-tertiary);
}

/* 内容区域 */
.content-container {
  flex: 1;
  padding: 24px;
  overflow-y: auto;
  background: var(--bg-secondary);
  position: relative;
}

/* 过渡动画 */
.fade-transform-enter-active,
.fade-transform-leave-active {
  transition: all 0.3s ease;
}

.fade-transform-enter-from {
  opacity: 0;
  transform: translateX(-30px);
}

.fade-transform-leave-to {
  opacity: 0;
  transform: translateX(30px);
}

/* 淡入淡出 */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>

<!-- 用户下拉菜单 popper 全局样式：el-dropdown 的 popper 默认 teleport 到 body 下，scoped 样式无法生效，必须用全局样式 -->
<style>
/* popper 容器（方案 E：极简发光融合） */
.user-dropdown-popper.el-popper {
  width: 180px;
  background: linear-gradient(180deg, var(--bg-tertiary) 0%, var(--bg-primary) 100%) !important;
  border: 1px solid var(--border-glow) !important;
  border-radius: var(--radius-lg) !important;
  box-shadow: var(--shadow-lg), 0 0 0 1px rgba(6, 212, 228, 0.3), 0 0 32px rgba(6, 212, 228, 0.25) !important;
  padding: 6px 6px 6px 14px !important;
}

/* 恢复顶部箭头显示（原代码隐藏了，方案 E 保留箭头） */
.user-dropdown-popper .el-popper__arrow::before {
  background: var(--bg-tertiary) !important;
  border-color: var(--border-glow) !important;
}

/* 菜单容器重置 */
.user-dropdown-popper .el-dropdown-menu {
  background: transparent !important;
  border: none !important;
  box-shadow: none !important;
  padding: 0 !important;
  display: flex;
  flex-direction: column;
}

/* 用户信息头部 */
.user-dropdown-popper .user-dropdown-header {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 10px 10px 8px;
  margin-bottom: 4px;
  border-bottom: 1px solid var(--border-primary);
  position: relative;
  background: radial-gradient(ellipse at top left, rgba(6, 212, 228, 0.1) 0%, transparent 70%);
}

.user-dropdown-popper .user-dropdown-header::after {
  content: '';
  position: absolute;
  bottom: -1px;
  left: 10px;
  width: 40px;
  height: 1px;
  background: var(--primary);
  box-shadow: 0 0 6px var(--primary), 0 0 12px var(--primary-glow);
}

.user-dropdown-popper .user-status-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: var(--success);
  box-shadow: 0 0 6px var(--success), 0 0 10px rgba(16, 250, 158, 0.5);
  flex-shrink: 0;
  animation: user-status-pulse 2s ease-in-out infinite;
}

@keyframes user-status-pulse {
  0%, 100% { opacity: 1; transform: scale(1); }
  50% { opacity: 0.6; transform: scale(0.85); }
}

.user-dropdown-popper .user-account {
  font-size: 13px;
  color: var(--text-secondary);
  font-family: 'JetBrains Mono', monospace;
  flex: 1;
  letter-spacing: 0.5px;
}

/* 菜单项 */
.user-dropdown-popper .el-dropdown-menu__item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 10px;
  border-radius: var(--radius-md);
  color: var(--text-secondary);
  transition: all 0.2s ease;
  font-size: 13px;
  position: relative;
  margin: 0;
}

.user-dropdown-popper .el-dropdown-menu__item:hover {
  background: rgba(6, 212, 228, 0.08) !important;
  color: var(--primary) !important;
  box-shadow: inset 0 0 12px rgba(6, 212, 228, 0.15), 0 0 0 1px rgba(6, 212, 228, 0.2);
}

.user-dropdown-popper .el-dropdown-menu__item .el-icon {
  width: 16px;
  height: 16px;
  color: var(--text-tertiary);
  transition: all 0.2s ease;
}

.user-dropdown-popper .el-dropdown-menu__item:hover .el-icon {
  color: var(--primary);
  filter: drop-shadow(0 0 6px var(--primary-glow));
}

/* 菜单项右侧箭头 */
.user-dropdown-popper .dropdown-arrow {
  margin-left: auto;
  opacity: 0.5;
  color: var(--text-tertiary);
  font-size: 14px;
  transition: all 0.2s ease;
  line-height: 1;
}

.user-dropdown-popper .el-dropdown-menu__item:hover .dropdown-arrow {
  opacity: 1;
  color: var(--primary);
  transform: translateX(2px);
  filter: drop-shadow(0 0 4px var(--primary-glow));
}

/* 分隔线（退出登录上方，渐变淡出） */
.user-dropdown-popper .user-dropdown-divider {
  height: 1px;
  background: linear-gradient(90deg, transparent, var(--border-primary) 20%, var(--border-primary) 80%, transparent);
  margin: 4px 10px;
}

/* 退出登录项 hover 红色发光 */
.user-dropdown-popper .el-dropdown-menu__item.is-logout:hover {
  background: rgba(255, 87, 87, 0.08) !important;
  color: var(--danger) !important;
  box-shadow: inset 0 0 12px rgba(255, 87, 87, 0.15), 0 0 0 1px rgba(255, 87, 87, 0.2);
}

.user-dropdown-popper .el-dropdown-menu__item.is-logout:hover .el-icon {
  color: var(--danger);
  filter: drop-shadow(0 0 6px rgba(255, 87, 87, 0.5));
}

.user-dropdown-popper .el-dropdown-menu__item.is-logout:hover .dropdown-arrow {
  color: var(--danger);
  filter: drop-shadow(0 0 4px rgba(255, 87, 87, 0.5));
}
</style>
