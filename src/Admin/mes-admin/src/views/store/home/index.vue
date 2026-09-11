<template>
  <div class="dashboard-container">
    <!-- 欢迎横幅 -->
    <div class="welcome-banner">
      <div class="banner-content">
        <div class="banner-text">
          <h1 class="banner-title">欢迎回来，管理员</h1>
          <p class="banner-subtitle">今天是 {{ currentDate }}，管理系统运行正常</p>
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
      <el-col :span="6" v-for="(stat, index) in stats" :key="index">
        <div class="stat-card" :style="{ '--delay': index * 0.1 + 's' }">
          <div class="stat-glow" :class="stat.glowClass"></div>
          <div class="stat-content">
            <div class="stat-info">
              <p class="stat-label">{{ stat.label }}</p>
              <h3 class="stat-value">
                <span class="value-number">{{ stat.value }}</span>
                <span class="value-unit" v-if="stat.unit">{{ stat.unit }}</span>
              </h3>
              <p class="stat-change" :class="stat.changeType">
                <el-icon v-if="stat.changeType === 'increase'"><ArrowUp /></el-icon>
                <el-icon v-else><ArrowDown /></el-icon>
                {{ stat.change }}
                <span class="stat-sub">{{ stat.changeLabel }}</span>
              </p>
            </div>
            <div class="stat-icon" :class="stat.iconClass">
              <el-icon><component :is="stat.icon" /></el-icon>
            </div>
          </div>
          <div class="stat-border"></div>
        </div>
      </el-col>
    </el-row>

    <!-- 今日经营概览横条 -->
    <div class="overview-banner">
      <div class="overview-item">
        <span class="overview-label">今日消费客户</span>
        <span class="overview-value">{{ overview.consumeCustomerCount }}</span>
        <span class="overview-unit">人</span>
      </div>
      <div class="overview-divider"></div>
      <div class="overview-item">
        <span class="overview-label">今日新客</span>
        <span class="overview-value">{{ overview.newCustomerCount }}</span>
        <span class="overview-unit">人</span>
      </div>
      <div class="overview-divider"></div>
      <div class="overview-item">
        <span class="overview-label">今日预约</span>
        <span class="overview-value">{{ overview.appointmentCount }}</span>
        <span class="overview-unit">个</span>
      </div>
    </div>

    <!-- 图表区域 -->
    <el-row :gutter="20" class="chart-row">
      <el-col :span="16">
        <div class="chart-card">
          <div class="card-header">
            <h3 class="card-title">
              <span class="title-icon"></span>
              本月营收趋势
            </h3>
            <div class="chart-tabs">
              <div
                v-for="tab in chartTabs"
                :key="tab.value"
                class="chart-tab"
                :class="{ active: chartType === tab.value }"
                @click="switchChartTab(tab.value)"
              >
                {{ tab.label }}
              </div>
            </div>
          </div>
          <div ref="trendChartRef" class="chart-content trend-chart"></div>
        </div>
      </el-col>
      <el-col :span="8">
        <div class="chart-card">
          <div class="card-header">
            <h3 class="card-title">
              <span class="title-icon"></span>
              营收构成
            </h3>
          </div>
          <div ref="pieChartRef" class="chart-content pie-chart"></div>
        </div>
      </el-col>
    </el-row>

    <!-- 列表区域 -->
    <el-row :gutter="20">
      <el-col :span="12">
        <div class="chart-card">
          <div class="card-header">
            <h3 class="card-title">
              <span class="title-icon"></span>
              预警提醒
            </h3>
            <el-button type="primary" size="small" link @click="goAlertPage">查看全部</el-button>
          </div>
          <div class="alert-list">
            <div v-if="alertList.length === 0" class="alert-empty">暂无预警提醒</div>
            <div
              v-for="(item, index) in alertList"
              :key="index"
              class="alert-item"
            >
              <span class="alert-dot" :class="item.level"></span>
              <div class="alert-info">
                <div class="alert-title">{{ item.title }}</div>
                <div class="alert-desc">{{ item.desc }}</div>
              </div>
              <el-tag
                :type="item.level === 'danger' ? 'danger' : item.level === 'warning' ? 'warning' : 'info'"
                size="small"
                class="alert-tag"
              >
                {{ item.tagText }}
              </el-tag>
              <span class="alert-time">{{ item.time }}</span>
            </div>
          </div>
        </div>
      </el-col>
      <el-col :span="12">
        <div class="chart-card">
          <div class="card-header">
            <h3 class="card-title">
              <span class="title-icon"></span>
              热门商品/服务 TOP 5
            </h3>
            <div class="rank-toggles">
              <div class="toggle-group">
                <div
                  v-for="tab in rankTypeTabs"
                  :key="tab.value"
                  class="toggle-item"
                  :class="{ active: rankType === tab.value }"
                  @click="switchRankType(tab.value)"
                >
                  {{ tab.label }}
                </div>
              </div>
              <div class="toggle-group">
                <div
                  v-for="tab in rankSortTabs"
                  :key="tab.value"
                  class="toggle-item"
                  :class="{ active: rankSort === tab.value }"
                  @click="switchRankSort(tab.value)"
                >
                  {{ tab.label }}
                </div>
              </div>
            </div>
          </div>
          <div class="rank-list">
            <div
              v-for="(item, index) in rankList"
              :key="index"
              class="rank-item"
            >
              <div class="rank-number" :class="rankClass(index)">{{ index + 1 }}</div>
              <div class="rank-info">
                <div class="rank-name">{{ item.name }}</div>
                <div class="rank-bar">
                  <div
                    class="rank-bar-fill"
                    :style="{ width: item.percent + '%' }"
                  ></div>
                </div>
              </div>
              <div class="rank-value">
                <template v-if="rankSort === 'amount'">¥{{ item.value }}</template>
                <template v-else>{{ item.value }}次</template>
              </div>
            </div>
          </div>
        </div>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, nextTick, markRaw } from 'vue'
import { useRouter } from 'vue-router'
import * as echarts from 'echarts'
import { useUserStore } from '@/stores/user'
import { ArrowUp, ArrowDown, Money, ShoppingCart, TrendCharts, WarningFilled } from '@element-plus/icons-vue'
import {
  getDashboardSummary,
  getMonthlyTrend,
  getTopProducts,
  getRevenueComposition,
  getDashboardAlerts
} from '@/api/statistics'
import type { DailyStat, ProductSalesStat, ProductType, DashboardAlert } from '@/api/statistics/types'

const router = useRouter()

const trendChartRef = ref<HTMLElement>()
const pieChartRef = ref<HTMLElement>()
let trendChart: echarts.ECharts | null = null
let pieChart: echarts.ECharts | null = null

// 今日经营概览（消费客户/新客/预约）
const overview = ref({
  consumeCustomerCount: 0,
  newCustomerCount: 0,
  appointmentCount: 0
})

const chartType = ref<'revenue' | 'orders' | 'profit'>('revenue')

// 数据加载状态
const loading = ref(false)

// 当前日期
const currentDate = new Date().toLocaleDateString('zh-CN', {
  year: 'numeric',
  month: 'long',
  day: 'numeric',
  weekday: 'long'
})

// 图表标签
const chartTabs = [
  { label: '营收', value: 'revenue' },
  { label: '订单', value: 'orders' },
  { label: '毛利', value: 'profit' }
]

// 统计卡片数据（从API加载）
const stats = ref([
  {
    label: '今日营收',
    value: '--',
    unit: '元',
    change: '--',
    changeType: 'increase' as 'increase' | 'decrease',
    changeLabel: '较昨日',
    icon: markRaw(Money),
    iconClass: 'blue',
    glowClass: 'glow-blue'
  },
  {
    label: '今日订单',
    value: '--',
    unit: '笔',
    change: '--',
    changeType: 'increase' as 'increase' | 'decrease',
    changeLabel: '较昨日',
    icon: markRaw(ShoppingCart),
    iconClass: 'green',
    glowClass: 'glow-green'
  },
  {
    label: '今日毛利',
    value: '--',
    unit: '元',
    change: '--',
    changeType: 'increase' as 'increase' | 'decrease',
    changeLabel: '较昨日',
    icon: markRaw(TrendCharts),
    iconClass: 'orange',
    glowClass: 'glow-orange'
  },
  {
    label: '库存预警',
    value: '--',
    unit: '项',
    change: '--',
    changeType: 'decrease' as 'increase' | 'decrease',
    changeLabel: '较昨日',
    icon: markRaw(WarningFilled),
    iconClass: 'red',
    glowClass: 'glow-red'
  }
])

// 预警提醒数据（从API加载：库存预警/批次临期/项目卡到期/客户生日聚合）
const alertList = ref<DashboardAlert[]>([])

// 热门商品 TOP 5（从API加载）
const rankList = ref([
  { name: '--', value: '--', percent: 0 }
])

// 排名切换：商品/服务 + 金额/次数
const rankTypeTabs = [
  { label: '商品', value: 'product' as const },
  { label: '服务', value: 'service' as const }
]
const rankSortTabs = [
  { label: '金额', value: 'amount' as const },
  { label: '次数', value: 'count' as const }
]
const rankType = ref<'product' | 'service'>('product')
const rankSort = ref<'amount' | 'count'>('amount')

// 月度趋势数据（从API加载后填充）
let monthlyStats: DailyStat[] = []
const days = ref<string[]>([])
const chartDataMap = ref<{
  revenue: number[]
  orders: number[]
  profit: number[]
}>({
  revenue: [],
  orders: [],
  profit: []
})

// 排名样式
const rankClass = (index: number) => {
  if (index === 0) return 'top-1'
  if (index === 1) return 'top-2'
  if (index === 2) return 'top-3'
  return ''
}

// 格式化数字（千分位）
const formatNumber = (value: number): string => {
  return value.toLocaleString('zh-CN')
}

// 计算环比变化
const calcChange = (today: number, yesterday: number): { change: string; changeType: 'increase' | 'decrease' } => {
  if (yesterday === 0) return { change: '--', changeType: 'increase' }
  const diff = ((today - yesterday) / yesterday * 100).toFixed(1)
  return {
    change: Math.abs(parseFloat(diff)) + '%',
    changeType: today >= yesterday ? 'increase' : 'decrease'
  }
}

// 加载首页数据
const loadDashboardData = async () => {
  loading.value = true
  try {
    // 兜底：若当前是 store 子系统且未选择门店（边缘场景：路由守卫未覆盖的情况），先加载门店列表
    // 避免后续 store API 请求因 X-Store-Id 为空而失败（后端 DashboardAppService 会返回"请选择门店"）
    const userStore = useUserStore()
    if (userStore.isStoreSubsystem && !userStore.currentStoreId) {
      await userStore.getAuthorizedStores()
    }

    const now = new Date()
    const year = now.getFullYear()
    const month = now.getMonth() + 1

    // 并行加载所有数据（排名列表单独按当前切换状态加载）
    const [summary, trend] = await Promise.all([
      getDashboardSummary(),
      getMonthlyTrend({ year, month })
    ])

    // 填充今日经营概览
    overview.value = {
      consumeCustomerCount: summary.todayConsumeCustomerCount,
      newCustomerCount: summary.todayNewCustomerCount,
      appointmentCount: summary.todayAppointmentCount
    }

    // 填充统计卡片（今日数据从 summary 获取，环比从月度趋势的昨日数据获取）
    monthlyStats = trend
    const todayIndex = now.getDate() - 1
    const yesterday = monthlyStats[todayIndex - 1] || monthlyStats[monthlyStats.length - 1] || { revenue: 0, orderCount: 0, grossProfit: 0 }

    const revenueChange = calcChange(summary.todayRevenue, yesterday.revenue)
    const orderChange = calcChange(summary.todayOrderCount, yesterday.orderCount)
    const profitChange = calcChange(summary.todayGrossProfit, yesterday.grossProfit)

    stats.value = [
      {
        label: '今日营收',
        value: formatNumber(summary.todayRevenue),
        unit: '元',
        change: revenueChange.change,
        changeType: revenueChange.changeType,
        changeLabel: '较昨日',
        icon: markRaw(Money),
        iconClass: 'blue',
        glowClass: 'glow-blue'
      },
      {
        label: '今日订单',
        value: formatNumber(summary.todayOrderCount),
        unit: '笔',
        change: orderChange.change,
        changeType: orderChange.changeType,
        changeLabel: '较昨日',
        icon: markRaw(ShoppingCart),
        iconClass: 'green',
        glowClass: 'glow-green'
      },
      {
        label: '今日毛利',
        value: formatNumber(summary.todayGrossProfit),
        unit: '元',
        change: profitChange.change,
        changeType: profitChange.changeType,
        changeLabel: '较昨日',
        icon: markRaw(TrendCharts),
        iconClass: 'orange',
        glowClass: 'glow-orange'
      },
      {
        label: '库存预警',
        value: String(summary.inventoryAlertCount),
        unit: '项',
        change: '--',
        changeType: 'decrease',
        changeLabel: '未处理',
        icon: markRaw(WarningFilled),
        iconClass: 'red',
        glowClass: 'glow-red'
      }
    ]

    // 填充趋势图表数据
    days.value = monthlyStats.map(s => s.statDate.substring(8) + '日')
    chartDataMap.value = {
      revenue: monthlyStats.map(s => s.revenue),
      orders: monthlyStats.map(s => s.orderCount),
      profit: monthlyStats.map(s => s.grossProfit)
    }

    // 加载排名列表（按当前切换状态）
    await loadRankList()

    // 加载预警提醒（独立加载，失败不影响主数据）
    await loadAlertList()

    // 更新图表
    await nextTick()
    updateTrendChart()
    await initPieChart()
  } catch (error) {
    console.error('加载首页数据失败', error)
  } finally {
    loading.value = false
  }
}

// 加载热门排行（按 rankType/rankSort 切换状态）
const loadRankList = async () => {
  try {
    const now = new Date()
    // 商品=零售(1)+耗材(3)；服务=服务(2)+项目卡(4)
    const productTypes: ProductType[] = rankType.value === 'product' ? [1, 3] : [2, 4]
    const list = await getTopProducts({
      year: now.getFullYear(),
      month: now.getMonth() + 1,
      top: 5,
      sortBy: rankSort.value,
      productTypes
    })

    if (list.length === 0) {
      rankList.value = [{ name: '暂无数据', value: '--' as const, percent: 0 }]
      return
    }

    // 按当前排序维度计算最大值用于百分比
    const maxValue = rankSort.value === 'amount'
      ? Math.max(...list.map(i => i.salesAmount))
      : Math.max(...list.map(i => i.salesCount))

    rankList.value = list.map((item: ProductSalesStat) => {
      const raw = rankSort.value === 'amount' ? item.salesAmount : item.salesCount
      return {
        name: item.productName,
        value: formatNumber(raw),
        percent: maxValue > 0 ? Math.round((raw / maxValue) * 100) : 0
      }
    })
  } catch (error) {
    console.error('加载热门排行失败', error)
  }
}

// 加载预警提醒列表（库存预警/批次临期/项目卡到期/客户生日聚合）
const loadAlertList = async () => {
  try {
    alertList.value = await getDashboardAlerts()
  } catch (error) {
    console.error('加载预警提醒失败', error)
  }
}

// 跳转商品预警页（查看全部）
const goAlertPage = () => {
  router.push('/store/product/inventory/alert')
}

// 切换排名类型（商品/服务）
const switchRankType = (value: 'product' | 'service') => {
  if (rankType.value === value) return
  rankType.value = value
  loadRankList()
}

// 切换排名排序（金额/次数）
const switchRankSort = (value: 'amount' | 'count') => {
  if (rankSort.value === value) return
  rankSort.value = value
  loadRankList()
}

// 初始化营收趋势图
const initTrendChart = () => {
  if (!trendChartRef.value) return
  trendChart = echarts.init(trendChartRef.value)
  updateTrendChart()
}

// 更新趋势图数据
const updateTrendChart = () => {
  if (!trendChart) return
  const seriesName = chartTabs.find(t => t.value === chartType.value)?.label || '营收'
  const data = chartDataMap.value[chartType.value]

  trendChart.setOption({
    tooltip: {
      trigger: 'axis',
      backgroundColor: '#1a2332',
      borderColor: '#2d3a4d',
      textStyle: { color: '#e8f4f8' }
    },
    grid: { left: '3%', right: '4%', bottom: '3%', containLabel: true },
    xAxis: {
      type: 'category',
      data: days.value,
      axisLine: { lineStyle: { color: '#3d4f63' } },
      axisLabel: { color: '#6b7a8a', fontSize: 11 },
      axisTick: { show: false }
    },
    yAxis: {
      type: 'value',
      axisLine: { show: false },
      axisLabel: { color: '#6b7a8a', fontSize: 11 },
      splitLine: { lineStyle: { color: '#2d3a4d', type: 'dashed' } }
    },
    series: [{
      name: seriesName,
      type: 'line',
      smooth: true,
      data: data,
      symbol: 'circle',
      symbolSize: 6,
      lineStyle: { color: '#06d4e4', width: 2 },
      itemStyle: { color: '#06d4e4', borderColor: '#0a0e17', borderWidth: 2 },
      areaStyle: {
        color: {
          type: 'linear', x: 0, y: 0, x2: 0, y2: 1,
          colorStops: [
            { offset: 0, color: 'rgba(6, 212, 228, 0.3)' },
            { offset: 1, color: 'rgba(6, 212, 228, 0)' }
          ]
        }
      }
    }]
  })
}

// 切换图表 Tab
const switchChartTab = (value: string) => {
  chartType.value = value as 'revenue' | 'orders' | 'profit'
  updateTrendChart()
}

// 初始化营收构成饼图
const initPieChart = async () => {
  if (!pieChartRef.value) return
  if (!pieChart) {
    pieChart = echarts.init(pieChartRef.value)
  }

  // 从API获取营收构成数据
  const composition = await getRevenueComposition()
  const colors = ['#06d4e4', '#5b9bff', '#10fa9e', '#fbbf24']

  pieChart.setOption({
    tooltip: {
      trigger: 'item',
      backgroundColor: '#1a2332',
      borderColor: '#3d4f63',
      textStyle: { color: '#e8f4f8' }
    },
    legend: {
      bottom: '5%',
      textStyle: { color: '#b8c5d0', fontSize: 12 },
      itemWidth: 10,
      itemHeight: 10
    },
    series: [{
      type: 'pie',
      radius: ['40%', '65%'],
      center: ['50%', '42%'],
      avoidLabelOverlap: false,
      itemStyle: {
        borderRadius: 6,
        borderColor: '#1a2332',
        borderWidth: 2
      },
      label: { show: false },
      emphasis: {
        label: {
          show: true,
          fontSize: 14,
          fontWeight: 'bold',
          color: '#e8f4f8'
        }
      },
      data: composition.map((item, index) => ({
        value: item.value,
        name: item.name,
        itemStyle: { color: colors[index % colors.length] }
      }))
    }]
  })
}

// 窗口 resize 处理
const handleResize = () => {
  trendChart?.resize()
  pieChart?.resize()
}

onMounted(async () => {
  await nextTick()
  initTrendChart()
  window.addEventListener('resize', handleResize)
  // 从API加载数据并更新图表
  await loadDashboardData()
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', handleResize)
  trendChart?.dispose()
  pieChart?.dispose()
})
</script>

<style scoped>
.dashboard-container {
  width: 100%;
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
  margin-bottom: 20px;
}

/* 今日经营概览横条 */
.overview-banner {
  display: flex;
  align-items: center;
  gap: 24px;
  background: var(--bg-tertiary);
  border: 1px solid var(--border-primary);
  border-radius: var(--radius-lg);
  padding: 16px 24px;
  margin-bottom: 20px;
}

.overview-item {
  display: flex;
  align-items: baseline;
  gap: 8px;
}

.overview-label {
  font-size: 13px;
  color: var(--text-tertiary);
  font-weight: 500;
}

.overview-value {
  font-size: 24px;
  font-weight: 700;
  color: var(--primary);
  font-family: 'JetBrains Mono', monospace;
}

.overview-unit {
  font-size: 13px;
  color: var(--text-tertiary);
}

.overview-divider {
  width: 1px;
  height: 32px;
  background: var(--border-primary);
}

.stat-card {
  background: var(--bg-tertiary);
  border: 1px solid var(--border-primary);
  border-radius: var(--radius-lg);
  padding: 20px 24px;
  position: relative;
  overflow: hidden;
  transition: all 0.3s ease;
  animation: card-fade-in 0.5s ease forwards;
  animation-delay: var(--delay, 0s);
  opacity: 0;
}

@keyframes card-fade-in {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}

.stat-card:hover {
  border-color: var(--border-secondary);
  box-shadow: var(--shadow-lg);
  transform: translateY(-2px);
}

.stat-glow {
  position: absolute;
  top: -50%;
  right: -30%;
  width: 200px;
  height: 200px;
  border-radius: 50%;
  opacity: 0.08;
  pointer-events: none;
}

.glow-blue { background: var(--info); }
.glow-green { background: var(--success); }
.glow-orange { background: var(--warning); }
.glow-red { background: var(--danger); }

.stat-content {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  position: relative;
  z-index: 1;
}

.stat-info {
  flex: 1;
}

.stat-label {
  font-size: 13px;
  color: var(--text-tertiary);
  margin: 0 0 8px 0;
  font-weight: 500;
}

.stat-value {
  display: flex;
  align-items: baseline;
  gap: 4px;
  margin: 0 0 8px 0;
}

.value-number {
  font-size: 28px;
  font-weight: 700;
  color: var(--text-primary);
  font-family: 'JetBrains Mono', monospace;
}

.value-unit {
  font-size: 14px;
  color: var(--text-tertiary);
}

.stat-change {
  font-size: 12px;
  display: flex;
  align-items: center;
  gap: 4px;
  margin: 0;
}

.stat-change.increase { color: var(--success); }
.stat-change.decrease { color: var(--danger); }

.stat-sub {
  color: var(--text-tertiary);
  margin-left: 4px;
}

.stat-icon {
  width: 44px;
  height: 44px;
  border-radius: var(--radius-md);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 22px;
  color: #fff;
}

.stat-icon.blue {
  background: linear-gradient(135deg, #5b9bff 0%, #3b82f6 100%);
  box-shadow: 0 0 20px rgba(6, 212, 228, 0.3);
}
.stat-icon.green {
  background: linear-gradient(135deg, #10fa9e 0%, #10b981 100%);
  box-shadow: 0 0 20px rgba(16, 250, 158, 0.3);
}
.stat-icon.orange {
  background: linear-gradient(135deg, #fbbf24 0%, #f59e0b 100%);
  box-shadow: 0 0 20px rgba(251, 191, 36, 0.3);
}
.stat-icon.red {
  background: linear-gradient(135deg, #ff5757 0%, #ef4444 100%);
  box-shadow: 0 0 20px rgba(255, 87, 87, 0.3);
}

.stat-border {
  position: absolute;
  top: 0;
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

/* 图表卡片 */
.chart-row {
  margin-bottom: 20px;
}

.chart-card {
  background: var(--bg-tertiary);
  border: 1px solid var(--border-primary);
  border-radius: var(--radius-lg);
  overflow: hidden;
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

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 18px 24px;
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
  font-size: 15px;
  font-weight: 600;
  color: var(--text-primary);
  display: flex;
  align-items: center;
  gap: 8px;
  margin: 0;
}

.title-icon {
  width: 4px;
  height: 16px;
  background: var(--primary);
  border-radius: 2px;
}

.chart-tabs {
  display: flex;
  gap: 4px;
  background: var(--bg-secondary);
  padding: 3px;
  border-radius: var(--radius-md);
}

.chart-tab {
  padding: 4px 14px;
  font-size: 12px;
  color: var(--text-tertiary);
  cursor: pointer;
  border-radius: var(--radius-sm);
  transition: all 0.3s;
}

.chart-tab:hover {
  color: var(--text-primary);
}

.chart-tab.active {
  background: var(--primary);
  color: var(--bg-primary);
  font-weight: 600;
}

/* 排名区切换按钮组 */
.rank-toggles {
  display: flex;
  gap: 8px;
}

.toggle-group {
  display: flex;
  gap: 2px;
  background: var(--bg-secondary);
  padding: 2px;
  border-radius: var(--radius-sm);
}

.toggle-item {
  padding: 3px 10px;
  font-size: 12px;
  color: var(--text-tertiary);
  cursor: pointer;
  border-radius: var(--radius-sm);
  transition: all 0.3s;
}

.toggle-item:hover {
  color: var(--text-primary);
}

.toggle-item.active {
  background: var(--primary);
  color: var(--bg-primary);
  font-weight: 600;
}

.chart-content {
  padding: 16px;
  height: 320px;
}

/* 预警提醒列表 */
.alert-list {
  padding: 8px 0;
  max-height: 320px;
  overflow-y: auto;
}

.alert-empty {
  padding: 40px 0;
  text-align: center;
  font-size: 13px;
  color: var(--text-tertiary);
}

.alert-item {
  display: flex;
  align-items: center;
  padding: 12px 24px;
  border-bottom: 1px solid var(--border-primary);
  gap: 12px;
  transition: all 0.3s;
}

.alert-item:hover {
  background: var(--bg-hover);
}

.alert-item:last-child {
  border-bottom: none;
}

.alert-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  flex-shrink: 0;
}

.alert-dot.danger {
  background: var(--danger);
  box-shadow: 0 0 8px var(--danger);
}
.alert-dot.warning {
  background: var(--warning);
  box-shadow: 0 0 8px var(--warning);
}
.alert-dot.info {
  background: var(--info);
  box-shadow: 0 0 8px var(--info);
}

.alert-info {
  flex: 1;
  min-width: 0;
}

.alert-title {
  font-size: 13px;
  color: var(--text-primary);
  margin-bottom: 2px;
}

.alert-desc {
  font-size: 12px;
  color: var(--text-tertiary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.alert-tag {
  flex-shrink: 0;
}

.alert-time {
  font-size: 12px;
  color: var(--text-tertiary);
  font-family: 'JetBrains Mono', monospace;
  flex-shrink: 0;
  min-width: 50px;
  text-align: right;
}

/* 热门商品排行 */
.rank-list {
  padding: 0 24px;
  max-height: 320px;
  overflow-y: auto;
}

.rank-item {
  display: flex;
  align-items: center;
  padding: 12px 0;
  border-bottom: 1px solid var(--border-primary);
  gap: 12px;
}

.rank-item:last-child {
  border-bottom: none;
}

.rank-number {
  width: 24px;
  height: 24px;
  border-radius: var(--radius-sm);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 12px;
  font-weight: 700;
  font-family: 'JetBrains Mono', monospace;
  background: var(--bg-secondary);
  color: var(--text-tertiary);
  flex-shrink: 0;
}

.rank-number.top-1 {
  background: linear-gradient(135deg, #fbbf24 0%, #f59e0b 100%);
  color: var(--bg-primary);
}
.rank-number.top-2 {
  background: linear-gradient(135deg, #5b9bff 0%, #3b82f6 100%);
  color: var(--bg-primary);
}
.rank-number.top-3 {
  background: linear-gradient(135deg, #10fa9e 0%, #10b981 100%);
  color: var(--bg-primary);
}

.rank-info {
  flex: 1;
  min-width: 0;
}

.rank-name {
  font-size: 13px;
  color: var(--text-primary);
  margin-bottom: 4px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.rank-bar {
  height: 4px;
  background: var(--bg-secondary);
  border-radius: 2px;
  overflow: hidden;
}

.rank-bar-fill {
  height: 100%;
  background: linear-gradient(135deg, #06d4e4 0%, #0891b2 100%);
  border-radius: 2px;
  transition: width 0.5s ease;
}

.rank-value {
  font-size: 13px;
  color: var(--primary);
  font-weight: 600;
  font-family: 'JetBrains Mono', monospace;
  min-width: 70px;
  text-align: right;
  flex-shrink: 0;
}
</style>
