<template>
  <div class="sample-report">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="名称">
            <el-input
              v-model="searchForm.name"
              placeholder="请输入名称"
              clearable
              style="width: 180px"
            />
          </el-form-item>
          <el-form-item label="类型">
            <el-select v-model="searchForm.type" placeholder="全部" clearable style="width: 120px">
              <el-option label="样品" :value="4" />
              <el-option label="赠品" :value="5" />
            </el-select>
          </el-form-item>
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

    <!-- 图表区域 -->
    <div class="card mb-20">
      <div class="chart-header">
        <div class="chart-title">样品 vs 赠品发出占比</div>
      </div>
      <div ref="pieChartRef" class="chart-container"></div>
    </div>

    <!-- 操作栏 -->
    <div class="table-toolbar">
      <div class="toolbar-left">
        <span class="toolbar-tip">共 {{ pagination.total }} 条统计记录</span>
      </div>
      <div class="toolbar-right">
        <el-button circle @click="loadData">
          <el-icon><Refresh /></el-icon>
        </el-button>
      </div>
    </div>

    <!-- 表格区域 -->
    <div class="card">
      <el-table
        v-loading="tableLoading"
        :data="tableData"
        style="width: 100%"
      >
        <el-table-column label="名称/类型" min-width="200">
          <template #default="{ row }">
            <div class="sample-info">
              <div class="sample-detail">
                <div class="sample-name">{{ row.name }}</div>
                <div class="sample-type-text">
                  <el-tag :type="row.type === 4 ? 'primary' : 'success'" size="small" effect="plain">
                    {{ row.type === 4 ? '样品' : '赠品' }}
                  </el-tag>
                </div>
              </div>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="receiveCount" label="领用数量" width="110" align="center" />
        <el-table-column prop="outboundCount" label="出库数量" width="110" align="center" />
        <el-table-column label="合计发出" width="110" align="center">
          <template #default="{ row }">
            <span class="total-text">{{ row.totalIssued }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="currentStock" label="当前库存" width="110" align="center" />
        <el-table-column label="领用占比" width="160">
          <template #default="{ row }">
            <el-progress
              :percentage="row.receiveRatio"
              :stroke-width="14"
              :text-inside="true"
              :color="ratioColor(row.receiveRatio)"
            />
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
import { Search, Refresh } from '@element-plus/icons-vue'
import * as echarts from 'echarts'
import { getSampleReports } from '@/api/sample'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { SampleReport, SampleType } from '@/api/sample/types'

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  name: '',
  type: undefined as SampleType | undefined
})

// 时间范围
const dateRange = ref<[string, string] | null>(null)

// 表格数据
const tableLoading = ref(false)
const tableData = ref<SampleReport[]>([])

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

// 图表
const pieChartRef = ref<HTMLElement>()
let chartInstance: echarts.ECharts | null = null

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getSampleReports({
      name: searchForm.name || undefined,
      type: searchForm.type,
      startDate: dateRange.value?.[0],
      endDate: dateRange.value?.[1],
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    tableData.value = res.list
    pagination.total = res.total
    renderChart()
  } catch (error) {
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
  searchForm.name = ''
  searchForm.type = undefined
  dateRange.value = null
  handleSearch()
}

// 渲染饼图
const renderChart = () => {
  nextTick(() => {
    if (!pieChartRef.value) return
    if (!chartInstance) {
      chartInstance = echarts.init(pieChartRef.value)
    }
    // 计算样品与赠品的合计发出占比
    let sampleTotal = 0
    let giftTotal = 0
    tableData.value.forEach(item => {
      if (item.type === 4) {
        sampleTotal += item.totalIssued
      } else {
        giftTotal += item.totalIssued
      }
    })

    chartInstance.setOption({
      tooltip: {
        trigger: 'item',
        formatter: '{a} <br/>{b}: {c} ({d}%)'
      },
      legend: {
        orient: 'horizontal',
        bottom: 10,
        textStyle: {
          color: '#b8c5d0'
        }
      },
      series: [
        {
          name: '发出占比',
          type: 'pie',
          radius: ['40%', '70%'],
          center: ['50%', '45%'],
          avoidLabelOverlap: false,
          itemStyle: {
            borderRadius: 8,
            borderColor: 'rgba(0,0,0,0.1)',
            borderWidth: 2
          },
          label: {
            show: true,
            position: 'center',
            formatter: '合计发出\n{c|' + (sampleTotal + giftTotal) + '}',
            rich: {
              c: {
                fontSize: 24,
                fontWeight: 'bold',
                color: '#06d4e4'
              }
            },
            color: '#b8c5d0',
            fontSize: 13
          },
          emphasis: {
            label: {
              show: true,
              fontSize: 16,
              fontWeight: 'bold'
            }
          },
          labelLine: {
            show: false
          },
          data: [
            {
              value: sampleTotal,
              name: '样品',
              itemStyle: {
                color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
                  { offset: 0, color: '#06d4e4' },
                  { offset: 1, color: '#0a8a9a' }
                ])
              }
            },
            {
              value: giftTotal,
              name: '赠品',
              itemStyle: {
                color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
                  { offset: 0, color: '#f5a623' },
                  { offset: 1, color: '#b87b15' }
                ])
              }
            }
          ]
        }
      ]
    })
  })
}

// 窗口大小变化时重绘
const handleResize = () => {
  chartInstance?.resize()
}

// 领用占比颜色
const ratioColor = (ratio: number): string => {
  if (ratio >= 60) return '#06d4e4'
  if (ratio >= 30) return '#f5a623'
  return '#67c23a'
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
.sample-report {
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

/* 样品信息 */
.sample-info {
  display: flex;
  align-items: center;
  gap: 12px;
}

.sample-detail {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.sample-name {
  font-weight: 500;
  color: var(--text-primary);
}

.sample-type-text {
  display: flex;
  gap: 4px;
}

.total-text {
  color: var(--primary);
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
