<template>
  <div class="technician-statistics">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" class="search-form-inline">
          <el-form-item label="时间范围">
            <el-date-picker
              v-model="dateRange"
              type="daterange"
              range-separator="至"
              start-placeholder="开始日期"
              end-placeholder="结束日期"
              value-format="YYYY-MM-DD"
              style="width: 260px"
            />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" @click="handleSearch">
              <el-icon><Search /></el-icon>
              搜索
            </el-button>
            <el-button @click="handleReset">
              <el-icon><Refresh /></el-icon>
              重置
            </el-button>
          </el-form-item>
        </el-form>
      </div>
    </div>

    <!-- 纯平台技师门店提示 -->
    <div v-if="isPurePlatformStore" class="card mb-20 pure-platform-tip">
      <el-alert
        :title="platformStoreMessage"
        type="info"
        :closable="false"
        show-icon
      />
    </div>

    <!-- 图表区域（纯平台技师门店时隐藏） -->
    <div v-if="!isPurePlatformStore" class="card mb-20">
      <div class="chart-header">
        <div class="chart-title">技师服务人次与费用对比</div>
      </div>
      <div ref="barChartRef" class="chart-container"></div>
    </div>

    <!-- 操作栏（纯平台技师门店时隐藏） -->
    <div v-if="!isPurePlatformStore" class="table-toolbar">
      <div class="toolbar-left">
        <span class="toolbar-tip">共 {{ pagination.total }} 条统计记录</span>
      </div>
      <div class="toolbar-right">
        <el-button circle @click="loadData">
          <el-icon><Refresh /></el-icon>
        </el-button>
      </div>
    </div>

    <!-- 表格区域（纯平台技师门店时隐藏） -->
    <div v-if="!isPurePlatformStore" class="card">
      <el-table
        v-loading="tableLoading"
        :data="tableData"
        style="width: 100%"
      >
        <el-table-column prop="technicianName" label="技师姓名" min-width="140">
          <template #default="{ row }">
            <div class="technician-info">
              <div class="technician-avatar">
                <el-icon><User /></el-icon>
              </div>
              <div class="technician-detail">
                <div class="technician-name">{{ row.technicianName }}</div>
              </div>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="serviceCount" label="服务人次" width="100" align="center" />
        <el-table-column label="服务总时长" width="120" align="center">
          <template #default="{ row }">
            {{ formatDuration(row.serviceMinutes) }}
          </template>
        </el-table-column>
        <el-table-column prop="totalCustomerCount" label="总客户数" width="100" align="center" />
        <el-table-column label="回头客率" width="140" align="center">
          <template #default="{ row }">
            <el-progress
              :percentage="calcReturnRate(row.returnCustomerCount, row.totalCustomerCount)"
              :stroke-width="14"
              :text-inside="true"
              :color="returnRateColor(calcReturnRate(row.returnCustomerCount, row.totalCustomerCount))"
            />
          </template>
        </el-table-column>
        <el-table-column label="技师服务费用汇总" width="150" align="right">
          <template #default="{ row }">
            <span class="commission-text">¥{{ formatNumber(row.totalTechnicianFee) }}</span>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <div class="pagination-container">
        <el-pagination
          v-model:current-page="pagination.pageIndex"
          v-model:page-size="pagination.pageSize"
          :page-sizes="systemConfigStore.defaultPageSizes"
          :total="pagination.total"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="loadData"
          @current-change="loadData"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, onBeforeUnmount, nextTick } from 'vue'
import { ElMessage } from 'element-plus'
import { Search, Refresh, User } from '@element-plus/icons-vue'
import * as echarts from 'echarts'
import { getTechnicianStatisticReport } from '@/api/staff'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { TechnicianStatisticReport } from '@/api/staff/types'

const systemConfigStore = useSystemConfigStore()

// 时间范围
const dateRange = ref<[string, string] | null>(null)

// 纯平台技师门店标识
const isPurePlatformStore = ref(false)
const platformStoreMessage = ref('')

// 表格数据
const tableLoading = ref(false)
const tableData = ref<TechnicianStatisticReport[]>([])

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

// 图表
const barChartRef = ref<HTMLElement>()
let chartInstance: echarts.ECharts | null = null

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getTechnicianStatisticReport({
      startDate: dateRange.value?.[0],
      endDate: dateRange.value?.[1],
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    // 纯平台技师门店：仅展示提示，隐藏报表
    isPurePlatformStore.value = res.isPurePlatformStore
    platformStoreMessage.value = res.message || ''
    tableData.value = res.items
    pagination.total = res.total
    if (!isPurePlatformStore.value) {
      renderChart()
    }
  } catch {
    ElMessage.error('加载数据失败')
  } finally {
    tableLoading.value = false
  }
}

// 搜索
const handleSearch = () => {
  pagination.pageIndex = 1
  loadData()
}

// 重置
const handleReset = () => {
  dateRange.value = null
  handleSearch()
}

// 渲染柱状图
const renderChart = () => {
  nextTick(() => {
    if (!barChartRef.value) return
    if (!chartInstance) {
      chartInstance = echarts.init(barChartRef.value)
    }
    const names = tableData.value.map(item => item.technicianName)
    const counts = tableData.value.map(item => item.serviceCount)
    const fees = tableData.value.map(item => item.totalTechnicianFee)

    chartInstance.setOption({
      tooltip: {
        trigger: 'axis',
        axisPointer: {
          type: 'shadow'
        }
      },
      legend: {
        data: ['服务人次', '技师服务费用(元)'],
        top: 10,
        textStyle: {
          color: '#b8c5d0'
        }
      },
      grid: {
        left: '3%',
        right: '4%',
        bottom: '3%',
        top: 50,
        containLabel: true
      },
      xAxis: {
        type: 'category',
        data: names,
        axisLine: { lineStyle: { color: '#b8c5d0' } },
        axisLabel: { color: '#b8c5d0' }
      },
      yAxis: [
        {
          type: 'value',
          name: '服务人次',
          axisLine: { lineStyle: { color: '#b8c5d0' } },
          axisLabel: { color: '#b8c5d0' },
          splitLine: { lineStyle: { color: 'rgba(184,197,208,0.1)' } }
        },
        {
          type: 'value',
          name: '技师服务费用(元)',
          axisLine: { lineStyle: { color: '#b8c5d0' } },
          axisLabel: { color: '#b8c5d0' },
          splitLine: { show: false }
        }
      ],
      series: [
        {
          name: '服务人次',
          type: 'bar',
          data: counts,
          barWidth: '30%',
          itemStyle: {
            color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
              { offset: 0, color: '#06d4e4' },
              { offset: 1, color: '#0a8a9a' }
            ]),
            borderRadius: [4, 4, 0, 0]
          }
        },
        {
          name: '技师服务费用(元)',
          type: 'bar',
          yAxisIndex: 1,
          data: fees,
          barWidth: '30%',
          itemStyle: {
            color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
              { offset: 0, color: '#f5a623' },
              { offset: 1, color: '#b87b15' }
            ]),
            borderRadius: [4, 4, 0, 0]
          }
        }
      ]
    })
  })
}

// 窗口大小变化时重绘
const handleResize = () => {
  chartInstance?.resize()
}

// 格式化金额
const formatNumber = (num: number | undefined): string => {
  if (num === null || num === undefined) return '0.00'
  return num.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

// 格式化时长（分钟转为小时+分钟）
const formatDuration = (minutes: number | undefined): string => {
  if (!minutes) return '0分钟'
  const hours = Math.floor(minutes / 60)
  const mins = minutes % 60
  if (hours === 0) return `${mins}分钟`
  if (mins === 0) return `${hours}小时`
  return `${hours}小时${mins}分钟`
}

// 计算回头客率（百分比，保留1位小数）
const calcReturnRate = (returnCount: number, totalCount: number): number => {
  if (!totalCount || totalCount === 0) return 0
  return Math.round((returnCount / totalCount) * 1000) / 10
}

// 回头客率颜色
const returnRateColor = (rate: number): string => {
  if (rate >= 60) return '#67c23a'
  if (rate >= 40) return '#e6a23c'
  return '#f56c6c'
}

onMounted(async () => {
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  pagination.pageSize = systemConfigStore.defaultPageSize
  await loadData()
  window.addEventListener('resize', handleResize)
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', handleResize)
  chartInstance?.dispose()
  chartInstance = null
})
</script>

<style scoped>
.technician-statistics {
  width: 100%;
}

/* 卡片样式 */
.card {
  background: var(--bg-tertiary);
  border-radius: var(--radius-lg);
  border: 1px solid var(--border-primary);
  overflow: hidden;
}

.mb-20 {
  margin-bottom: 20px;
}

/* 表格样式 */
:deep(.el-table) {
  --el-table-bg-color: transparent !important;
  --el-table-text-color: var(--text-primary) !important;
  --el-table-border-color: transparent !important;
  --el-table-header-bg-color: var(--bg-tertiary) !important;
  --el-table-row-hover-bg-color: var(--bg-hover) !important;
}

:deep(.el-table th.el-table__cell) {
  background: var(--bg-tertiary) !important;
  color: var(--text-tertiary) !important;
  font-weight: 600;
  border-bottom: 1px solid var(--border-primary) !important;
}

:deep(.el-table td.el-table__cell) {
  background-color: var(--bg-tertiary) !important;
  border-bottom: 1px solid var(--border-primary) !important;
}

:deep(.el-table__row:hover > td.el-table__cell) {
  background-color: var(--bg-hover) !important;
}

/* 搜索区域 */
.search-form {
  padding: 20px 24px 0;
}

.search-form-inline {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

/* 纯平台技师门店提示 */
.pure-platform-tip {
  padding: 16px 24px;
}

/* 操作栏 */
.table-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
  padding: 0 4px;
}

.toolbar-left {
  display: flex;
  gap: 12px;
  align-items: center;
}

.toolbar-tip {
  color: var(--text-tertiary);
  font-size: 14px;
}

.toolbar-right {
  display: flex;
  gap: 8px;
}

/* 图表区域 */
.chart-header {
  padding: 16px 24px 0;
}

.chart-title {
  font-size: 15px;
  font-weight: 600;
  color: var(--text-primary);
}

.chart-container {
  width: 100%;
  height: 320px;
  padding: 12px;
}

/* 技师信息 */
.technician-info {
  display: flex;
  align-items: center;
  gap: 12px;
}

.technician-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: var(--bg-hover);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.technician-avatar .el-icon {
  color: var(--text-tertiary);
  font-size: 16px;
}

.technician-detail {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.technician-name {
  font-weight: 500;
  color: var(--text-primary);
}

.commission-text {
  color: #f5a623;
  font-weight: 600;
}

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}
</style>
