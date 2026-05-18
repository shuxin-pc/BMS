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
        :default-active="$route.path"
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
              :class="{ active: currentSubsystemId === subsystem.id }"
              :title="subsystem.name"
              @click="handleSubsystemClick(subsystem.id)"
            >
              <div class="icon-glow"></div>
              <el-icon><component :is="getIconComponent(subsystem.icon)" /></el-icon>
              <span class="icon-label">{{ subsystem.name }}</span>
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

          <!-- 通知图标 -->
          <div class="header-icon-btn" title="通知">
            <el-badge :value="3" :max="99" class="notification-badge">
              <el-icon><Bell /></el-icon>
            </el-badge>
          </div>

          <!-- 用户菜单 -->
          <el-dropdown @command="handleCommand">
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
              <el-dropdown-menu class="user-dropdown">
                <el-dropdown-item command="profile">
                  <el-icon><User /></el-icon>
                  个人中心
                </el-dropdown-item>
                <el-dropdown-item command="setting">
                  <el-icon><Setting /></el-icon>
                  系统设置
                </el-dropdown-item>
                <el-dropdown-item divided command="logout">
                  <el-icon><SwitchButton /></el-icon>
                  退出登录
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
            <component :is="Component" :key="$route.fullPath" />
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
import { recordAuditLog } from '@/api/system'
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
import type { Subsystem } from '@/api/system/types'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()
const systemConfig = useSystemConfigStore()
const isCollapse = ref(false)
const searchQuery = ref('')
const menus = ref<any[]>([])
const menuRef = ref()

// 监听 menus 变化，自动展开一级菜单
watch(menus, async (newMenus) => {
  if (newMenus.length > 0) {
    const paths = newMenus
      .filter(menu => menu.children && menu.children.length > 0)
      .map(menu => menu.path)

    // 等待下一帧，确保 el-menu 已渲染
    await nextTick()
    await nextTick()
    await new Promise(r => setTimeout(r, 100))

    if (menuRef.value && paths.length > 0) {
      // 使用 open 方法展开菜单
      paths.forEach(path => {
        menuRef.value.open(path)
      })
      // 强制刷新展开状态
      setTimeout(() => {
        menuRef.value.close(paths[0])
        menuRef.value.open(paths[0])
      }, 200)
    }
  }
}, { immediate: true })

const userInfo = computed(() => userStore.userInfo)

// 授权的子系统列表 - 从 userStore 获取
const subsystems = computed(() => userStore.authorizedSubsystems)
// 当前选中的子系统ID - 从 userStore 获取
const currentSubsystemId = computed(() => userStore.currentSubsystemId)

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
 * 切换子系统后更新左侧菜单
 */
const handleSubsystemClick = async (subsystemId: number) => {
  if (subsystemId === currentSubsystemId.value) return

  // 切换子系统
  await userStore.switchSubsystem(subsystemId)

  // 更新本地菜单数据
  menus.value = userStore.menus
}

const handleCommand = (command: string) => {
  if (command === 'logout') {
    ElMessageBox.confirm('确定要退出登录吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    }).then(async () => {
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
        console.error('记录登出审计日志失败', error)
      }

      // 清除本地状态并跳转
      userStore.logout()
      ElMessage.success('退出成功')
      router.push('/login')
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

/* 用户下拉菜单 */
.user-dropdown {
  background: var(--bg-primary) !important;
  border: 1px solid var(--border-primary) !important;
  padding: 8px !important;
  box-shadow: var(--shadow-lg) !important;
}

.user-dropdown :deep(.el-dropdown-menu__item) {
  padding: 10px 16px;
  color: var(--text-secondary);
  border-radius: var(--radius-md);
  margin: 2px 0;
}

.user-dropdown :deep(.el-dropdown-menu__item:hover) {
  background: var(--bg-hover);
  color: var(--primary);
}

.user-dropdown :deep(.el-dropdown-menu__item.is-divided) {
  border-top: 1px solid var(--border-primary);
  margin-top: 8px;
  padding-top: 10px;
}

.user-dropdown :deep(.el-popper__arrow) {
  display: none;
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
