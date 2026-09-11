<template>
  <div class="dashboard-container">
    <!-- 欢迎横幅 -->
    <div class="welcome-banner">
      <div class="banner-content">
        <div class="banner-text">
          <h1 class="banner-title">欢迎回来，{{ displayName }}</h1>
          <p class="banner-subtitle">今天是 {{ currentDate }}，系统运行正常</p>
        </div>
        <div class="banner-decoration">
          <div class="deco-line"></div>
          <div class="deco-line"></div>
          <div class="deco-line"></div>
        </div>
      </div>
      <div class="banner-glow"></div>
    </div>

    <!-- 统计卡片 -->
    <el-row :gutter="20" class="stat-row">
      <el-col v-for="(stat, index) in statsCards" :key="stat.label" :span="statSpan">
        <div class="stat-card" :style="{ '--delay': index * 0.1 + 's' }">
          <div class="stat-glow" :class="stat.glowClass"></div>
          <div class="stat-content">
            <div class="stat-info">
              <p class="stat-label">{{ stat.label }}</p>
              <h3 class="stat-value">
                <span class="value-number">{{ stat.value }}</span>
              </h3>
            </div>
            <div class="stat-icon" :class="stat.iconClass">
              <el-icon><component :is="stat.icon" /></el-icon>
            </div>
          </div>
          <div class="stat-border"></div>
        </div>
      </el-col>
    </el-row>

    <!-- 功能快捷入口 -->
    <div class="chart-card quick-card">
      <div class="card-header">
        <h3 class="card-title">
          <span class="title-icon"></span>
          功能快捷入口
        </h3>
      </div>
      <div class="quick-grid">
        <div
          v-for="menu in quickMenus"
          :key="menu.id"
          class="quick-item"
          @click="navigateTo(menu.path)"
        >
          <div class="quick-icon" :class="quickIconClass">
            <el-icon><component :is="getIconComponent(menu.icon)" /></el-icon>
          </div>
          <span class="quick-name">{{ menu.name }}</span>
        </div>
        <el-empty v-if="quickMenus.length === 0" description="暂无可用的功能入口" :image-size="60" />
      </div>
    </div>

    <!-- 最近审计日志 -->
    <el-row :gutter="20">
      <el-col :span="24">
        <div class="chart-card">
          <div class="card-header">
            <h3 class="card-title">
              <span class="title-icon"></span>
              最近审计日志
            </h3>
            <el-button type="primary" size="small" link @click="router.push('/system/audit-logs')">查看全部</el-button>
          </div>
          <div class="audit-list" v-loading="loading">
            <div v-for="log in recentLogs" :key="log.id" class="audit-item">
              <el-tag :type="getOperationTypeColor(log.operationType)" size="small" effect="dark" class="audit-tag">
                {{ getOperationTypeLabel(log.operationType) }}
              </el-tag>
              <span class="audit-user">{{ log.realName || log.userName || '-' }}</span>
              <span class="audit-content" :title="log.operationContent">{{ log.operationContent || '-' }}</span>
              <span class="audit-ip">{{ log.requestIp || '-' }}</span>
              <span class="audit-time">{{ formatTime(log.createdTime) }}</span>
            </div>
            <el-empty
              v-if="!loading && recentLogs.length === 0"
              description="暂无审计日志"
              :image-size="60"
            />
          </div>
        </div>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
  import { ref, computed, onMounted, markRaw } from 'vue'
  import { useRouter } from 'vue-router'
  import { ElMessage } from 'element-plus'
  import {
    User, Lock, Menu, OfficeBuilding, House, Document, Tools, Grid, Promotion, Setting
  } from '@element-plus/icons-vue'
  import type { Component } from 'vue'
  import type { Menu as MenuType } from '@/api/system'
  import { getDashboardStats, getAuditLogs } from '@/api/system'
  import type { DashboardStats, AuditLog } from '@/api/system'
  import { useUserStore } from '@/stores/user'

  const router = useRouter()
  const userStore = useUserStore()

  const loading = ref(false)
  const statsData = ref<DashboardStats | null>(null)
  const recentLogs = ref<AuditLog[]>([])

  const userInfo = computed(() => userStore.userInfo)

  const displayName = computed(() => {
    return userInfo.value?.realName || userInfo.value?.userName || '管理员'
  })

  const currentDate = new Date().toLocaleDateString('zh-CN', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    weekday: 'long'
  })

  // 统计卡片：租户总数仅平台租户显示
  const statsCards = computed(() => {
    if (!statsData.value) return []
    const cards: Array<{ label: string; value: number; icon: Component; iconClass: string; glowClass: string }> = [
      { label: '用户总数', value: statsData.value.userCount, icon: markRaw(User), iconClass: 'blue', glowClass: 'glow-blue' },
      { label: '角色总数', value: statsData.value.roleCount, icon: markRaw(Lock), iconClass: 'green', glowClass: 'glow-green' }
    ]
    if (statsData.value.isPlatformTenant) {
      cards.push({
        label: '租户总数',
        value: statsData.value.tenantCount ?? 0,
        icon: markRaw(House),
        iconClass: 'orange',
        glowClass: 'glow-orange'
      })
    }
    cards.push({
      label: '今日审计操作',
      value: statsData.value.todayAuditCount,
      icon: markRaw(Document),
      iconClass: 'cyan',
      glowClass: 'glow-cyan'
    })
    return cards
  })

  const statSpan = computed(() => Math.floor(24 / Math.max(statsCards.value.length, 1)))

  // 功能快捷入口：从当前用户已授权菜单中收集页面菜单（排除首页自身）
  const quickMenus = computed(() => {
    const collect = (list: MenuType[]): MenuType[] => {
      const result: MenuType[] = []
      for (const menu of list) {
        if (menu.type === 1 && menu.component && menu.path) {
          // 页面菜单：排除首页自身，避免入口重复
          if (menu.component !== 'dashboard/index' && menu.path !== '/dashboard') {
            result.push(menu)
          }
        } else if (menu.children && menu.children.length > 0) {
          result.push(...collect(menu.children))
        }
      }
      return result
    }
    return collect(userStore.menus)
  })

  // 菜单 path 可能是相对路径（如 system/users），跳转前统一转为绝对路径
  const navigateTo = (path?: string) => {
    if (!path) return
    router.push(path.startsWith('/') ? path : `/${path}`)
  }

  // 图标名称到组件的映射（与 layout/iconMap 命名保持一致）
  const iconMap: Record<string, Component> = {
    User: markRaw(User),
    Lock: markRaw(Lock),
    Menu: markRaw(Menu),
    OfficeBuilding: markRaw(OfficeBuilding),
    House: markRaw(House),
    Document: markRaw(Document),
    Tools: markRaw(Tools),
    Grid: markRaw(Grid),
    Promotion: markRaw(Promotion),
    Setting: markRaw(Setting)
  }

  const getIconComponent = (iconName?: string) => {
    if (!iconName) return iconMap.Setting
    return iconMap[iconName] || iconMap.Setting
  }

  const quickIconClass = 'icon-primary'

  // 操作类型映射（与审计日志页保持一致）
  const getOperationTypeLabel = (type: string) => {
    const map: Record<string, string> = {
      Login: '登录',
      Logout: '登出',
      Create: '新增',
      Update: '修改',
      Delete: '删除'
    }
    return map[type] || type
  }

  const getOperationTypeColor = (type: string) => {
    const map: Record<string, string> = {
      Login: 'success',
      Logout: 'info',
      Create: 'success',
      Update: 'warning',
      Delete: 'danger'
    }
    return map[type] || 'info'
  }

  const formatTime = (time?: string) => {
    if (!time) return '-'
    return time.replace('T', ' ').slice(0, 19)
  }

  onMounted(async () => {
    loading.value = true
    try {
      const [statsResult, logsResult] = await Promise.allSettled([
        getDashboardStats(),
        getAuditLogs({ pageIndex: 1, pageSize: 8 })
      ])
      if (statsResult.status === 'fulfilled') {
        statsData.value = statsResult.value
      } else {
        ElMessage.error(statsResult.reason?.message || '获取统计数据失败')
      }
      if (logsResult.status === 'fulfilled') {
        recentLogs.value = logsResult.value.list
      } else {
        ElMessage.error(logsResult.reason?.message || '获取审计日志失败')
      }
    } finally {
      loading.value = false
    }
  })
</script>

<style scoped>
  .dashboard-container {
    padding: 0;
  }

  /* 欢迎横幅 */
  .welcome-banner {
    position: relative;
    background: linear-gradient(135deg, var(--bg-tertiary) 0%, var(--bg-elevated) 100%);
    border-radius: var(--radius-lg);
    padding: 32px 40px;
    margin-bottom: 24px;
    overflow: hidden;
    border: 1px solid var(--border-primary);
  }

  .banner-content {
    position: relative;
    z-index: 1;
  }

  .banner-title {
    font-size: 28px;
    font-weight: 700;
    color: var(--text-primary);
    margin-bottom: 8px;
    letter-spacing: 1px;
  }

  .banner-subtitle {
    font-size: 14px;
    color: var(--text-tertiary);
  }

  .banner-decoration {
    position: absolute;
    right: 40px;
    top: 50%;
    transform: translateY(-50%);
    display: flex;
    gap: 8px;
  }

  .deco-line {
    width: 4px;
    height: 60px;
    border-radius: 2px;
  }

  .deco-line:nth-child(1) {
    background: linear-gradient(180deg, var(--primary), transparent);
    animation: deco-pulse 2s ease-in-out infinite;
  }

  .deco-line:nth-child(2) {
    background: linear-gradient(180deg, var(--info), transparent);
    animation: deco-pulse 2s ease-in-out infinite 0.3s;
  }

  .deco-line:nth-child(3) {
    background: linear-gradient(180deg, var(--success), transparent);
    animation: deco-pulse 2s ease-in-out infinite 0.6s;
  }

  @keyframes deco-pulse {
    0%, 100% { opacity: 0.5; height: 60px; }
    50% { opacity: 1; height: 80px; }
  }

  .banner-glow {
    position: absolute;
    top: -50%;
    right: -20%;
    width: 400px;
    height: 400px;
    background: radial-gradient(circle, rgba(6, 212, 228, 0.15) 0%, transparent 70%);
    pointer-events: none;
  }

  /* 统计卡片 */
  .stat-row {
    margin-bottom: 24px;
  }

  .stat-card {
    position: relative;
    background: var(--bg-tertiary);
    border-radius: var(--radius-lg);
    padding: 24px;
    overflow: hidden;
    border: 1px solid var(--border-primary);
    transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
    animation: stat-appear 0.5s ease-out backwards;
    animation-delay: var(--delay, 0s);
  }

  @keyframes stat-appear {
    from {
      opacity: 0;
      transform: translateY(20px);
    }
    to {
      opacity: 1;
      transform: translateY(0);
    }
  }

  .stat-card:hover {
    transform: translateY(-4px);
    border-color: var(--border-secondary);
    box-shadow: var(--shadow-lg);
  }

  .stat-glow {
    position: absolute;
    top: 0;
    right: 0;
    width: 120px;
    height: 120px;
    border-radius: 50%;
    filter: blur(40px);
    opacity: 0.3;
    pointer-events: none;
  }

  .stat-glow.glow-blue { background: var(--info); }
  .stat-glow.glow-green { background: var(--success); }
  .stat-glow.glow-orange { background: var(--warning); }
  .stat-glow.glow-cyan { background: var(--primary); }

  .stat-content {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    position: relative;
    z-index: 1;
  }

  .stat-label {
    font-size: 13px;
    color: var(--text-tertiary);
    margin-bottom: 8px;
    font-weight: 500;
  }

  .stat-value {
    font-size: 36px;
    font-weight: 700;
    color: var(--text-primary);
    display: flex;
    align-items: baseline;
    gap: 4px;
    margin: 0;
  }

  .value-number {
    font-family: 'JetBrains Mono', monospace;
    letter-spacing: -1px;
  }

  .stat-icon {
    width: 56px;
    height: 56px;
    border-radius: var(--radius-md);
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 26px;
    color: white;
    position: relative;
  }

  .stat-icon::after {
    content: '';
    position: absolute;
    inset: 0;
    border-radius: var(--radius-md);
    background: linear-gradient(135deg, rgba(255,255,255,0.2) 0%, transparent 50%);
  }

  .stat-icon.blue { background: linear-gradient(135deg, #5b9bff 0%, #3b82f6 100%); }
  .stat-icon.green { background: linear-gradient(135deg, #10fa9e 0%, #10b981 100%); }
  .stat-icon.orange { background: linear-gradient(135deg, #fbbf24 0%, #f59e0b 100%); }
  .stat-icon.cyan { background: linear-gradient(135deg, #22d3ee 0%, #06b6d4 100%); }

  .stat-border {
    position: absolute;
    bottom: 0;
    left: 0;
    right: 0;
    height: 2px;
    background: linear-gradient(90deg, transparent, var(--primary), transparent);
    opacity: 0;
    transition: opacity 0.3s;
  }

  .stat-card:hover .stat-border {
    opacity: 1;
  }

  /* 卡片通用 */
  .chart-card {
    background: var(--bg-tertiary);
    border-radius: var(--radius-lg);
    box-shadow: var(--shadow-md);
    overflow: hidden;
    border: 1px solid var(--border-primary);
    position: relative;
  }

  .chart-card::before {
    content: '';
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    height: 2px;
    background: linear-gradient(90deg, transparent, var(--primary), transparent);
    opacity: 0;
    transition: opacity 0.3s;
  }

  .chart-card:hover::before {
    opacity: 1;
  }

  .quick-card {
    margin-bottom: 24px;
  }

  .card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 20px 24px;
    border-bottom: 1px solid var(--border-primary);
    position: relative;
  }

  .card-header::after {
    content: '';
    position: absolute;
    bottom: 0;
    left: 24px;
    width: 40px;
    height: 2px;
    background: var(--primary);
  }

  .card-title {
    font-size: 16px;
    font-weight: 600;
    color: var(--text-primary);
    margin: 0;
    display: flex;
    align-items: center;
    gap: 10px;
  }

  .title-icon {
    width: 4px;
    height: 16px;
    background: var(--primary);
    border-radius: 2px;
  }

  /* 快捷入口宫格 */
  .quick-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(120px, 1fr));
    gap: 12px;
    padding: 20px 24px 24px;
  }

  .quick-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 10px;
    padding: 18px 8px;
    border-radius: var(--radius-md);
    border: 1px solid var(--border-primary);
    background: var(--bg-elevated);
    cursor: pointer;
    transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
  }

  .quick-item:hover {
    transform: translateY(-3px);
    border-color: var(--primary);
    box-shadow: var(--shadow-md);
  }

  .quick-icon {
    width: 44px;
    height: 44px;
    border-radius: var(--radius-md);
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 22px;
    color: white;
    background: linear-gradient(135deg, #5b9bff 0%, #3b82f6 100%);
  }

  .quick-icon.icon-primary {
    background: linear-gradient(135deg, #22d3ee 0%, #06b6d4 100%);
  }

  .quick-name {
    font-size: 13px;
    color: var(--text-primary);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    max-width: 100%;
  }

  /* 审计日志列表 */
  .audit-list {
    padding: 8px 24px 20px;
    min-height: 120px;
  }

  .audit-item {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 12px 16px;
    border-radius: var(--radius-md);
    transition: background 0.25s;
    border: 1px solid transparent;
    margin-bottom: 8px;
  }

  .audit-item:hover {
    background: var(--bg-hover);
    border-color: var(--border-primary);
  }

  .audit-tag {
    flex-shrink: 0;
    width: 52px;
    justify-content: center;
  }

  .audit-user {
    width: 100px;
    flex-shrink: 0;
    font-size: 14px;
    color: var(--text-primary);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .audit-content {
    flex: 1;
    font-size: 13px;
    color: var(--text-secondary);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .audit-ip {
    width: 130px;
    flex-shrink: 0;
    font-size: 12px;
    color: var(--text-tertiary);
    font-family: 'JetBrains Mono', monospace;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .audit-time {
    width: 160px;
    flex-shrink: 0;
    text-align: right;
    font-size: 12px;
    color: var(--text-tertiary);
    font-family: 'JetBrains Mono', monospace;
  }
</style>
