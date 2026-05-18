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

    <!-- 图表区域 -->
    <el-row :gutter="20" class="chart-row">
      <el-col :span="16">
        <div class="chart-card">
          <div class="card-header">
            <h3 class="card-title">
              <span class="title-icon"></span>
              近7日生产趋势
            </h3>
            <div class="chart-tabs">
              <div
                v-for="tab in chartTabs"
                :key="tab.value"
                class="chart-tab"
                :class="{ active: chartType === tab.value }"
                @click="chartType = tab.value"
              >
                {{ tab.label }}
              </div>
            </div>
          </div>
          <div ref="lineChartRef" class="chart-content line-chart"></div>
        </div>
      </el-col>
      <el-col :span="8">
        <div class="chart-card">
          <div class="card-header">
            <h3 class="card-title">
              <span class="title-icon"></span>
              工单状态分布
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
              待办任务
            </h3>
            <el-button type="primary" size="small" link>查看全部</el-button>
          </div>
          <div class="table-container">
            <el-table :data="todoList" :show-header="false" class="tech-table">
              <el-table-column prop="title" min-width="200">
                <template #default="{ row }">
                  <div class="todo-item">
                    <span class="priority-dot" :class="row.level"></span>
                    <el-tag :type="row.level === 'high' ? 'danger' : row.level === 'medium' ? 'warning' : 'info'" size="small" class="priority-tag">
                      {{ row.level === 'high' ? '高' : row.level === 'medium' ? '中' : '低' }}
                    </el-tag>
                    <span class="todo-title">{{ row.title }}</span>
                  </div>
                </template>
              </el-table-column>
              <el-table-column prop="time" width="120" align="right">
                <template #default="{ row }">
                  <span class="time-text">{{ row.time }}</span>
                </template>
              </el-table-column>
            </el-table>
          </div>
        </div>
      </el-col>
      <el-col :span="12">
        <div class="chart-card">
          <div class="card-header">
            <h3 class="card-title">
              <span class="title-icon"></span>
              系统公告
            </h3>
            <el-button type="primary" size="small" link>更多</el-button>
          </div>
          <div class="notice-list">
            <div
              v-for="(item, index) in noticeList"
              :key="index"
              class="notice-item"
              :class="{ 'is-new': item.isNew }"
            >
              <div class="notice-indicator"></div>
              <div class="notice-content">
                <span class="notice-title">{{ item.title }}</span>
                <span class="notice-time">{{ item.time }}</span>
              </div>
              <el-icon v-if="item.isNew" class="new-icon"><Collection /></el-icon>
            </div>
          </div>
        </div>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
  import { ref, onMounted, markRaw } from 'vue'
  import * as echarts from 'echarts'
  import { ArrowUp, ArrowDown, User, UserFilled, Document, Odometer, Collection } from '@element-plus/icons-vue'

  const lineChartRef = ref<HTMLElement>()
  const pieChartRef = ref<HTMLElement>()
  const chartType = ref('output')

  // 当前日期
  const currentDate = new Date().toLocaleDateString('zh-CN', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    weekday: 'long'
  })

  // 统计卡片数据 - 使用 markRaw 避免图标组件被响应式化
  const stats = ref([
    {
      label: '用户总数',
      value: '1,286',
      change: '12.5%',
      changeType: 'increase',
      changeLabel: '较上月',
      icon: markRaw(User),
      iconClass: 'blue',
      glowClass: 'glow-blue'
    },
    {
      label: '在线用户',
      value: '238',
      unit: '人',
      change: '8.2%',
      changeType: 'increase',
      changeLabel: '较昨日',
      icon: markRaw(UserFilled),
      iconClass: 'green',
      glowClass: 'glow-green'
    },
    {
      label: '工单总数',
      value: '5,732',
      change: '18.3%',
      changeType: 'increase',
      changeLabel: '较上月',
      icon: markRaw(Document),
      iconClass: 'orange',
      glowClass: 'glow-orange'
    },
    {
      label: '完成率',
      value: '94.2',
      unit: '%',
      change: '1.5%',
      changeType: 'decrease',
      changeLabel: '较上月',
      icon: markRaw(Odometer),
      iconClass: 'cyan',
      glowClass: 'glow-cyan'
    }
  ])

  // 图表标签
  const chartTabs = [
    { label: '产量', value: 'output' },
    { label: '工时', value: 'worktime' },
    { label: '合格率', value: 'qualified' }
  ]

  const todoList = ref([
    { title: '审批生产工单WO20260317001', level: 'high', time: '2小时前' },
    { title: '物料短缺预警，请及时处理', level: 'high', time: '3小时前' },
    { title: '设备保养计划提醒', level: 'medium', time: '1天前' },
    { title: '质量报告待审核', level: 'medium', time: '1天前' },
    { title: '员工考勤异常处理', level: 'low', time: '2天前' }
  ])

  const noticeList = ref([
    { title: '系统将于2026年3月20日0点进行版本升级', time: '2026-03-17', isNew: true },
    { title: '关于加强生产数据安全管理的通知', time: '2026-03-15', isNew: true },
    { title: '新功能：物料追溯模块上线公告', time: '2026-03-12', isNew: false },
    { title: '第二季度生产计划安排通知', time: '2026-03-10', isNew: false },
    { title: '系统性能优化完成公告', time: '2026-03-08', isNew: false }
  ])

  const initLineChart = () => {
    if (!lineChartRef.value) return
    const chart = echarts.init(lineChartRef.value)
    const option = {
      tooltip: {
        trigger: 'axis',
        backgroundColor: '#1a2332',
        borderColor: '#2d3a4d',
        textStyle: {
          color: '#e8f4f8'
        },
        axisPointer: {
          type: 'cross',
          crossStyle: {
            color: '#06d4e4'
          }
        }
      },
      legend: {
        data: ['计划产量', '实际产量'],
        textStyle: {
          color: '#b8c5d0'
        },
        itemWidth: 12,
        itemHeight: 12,
        itemGap: 20
      },
      grid: {
        left: '3%',
        right: '4%',
        bottom: '3%',
        containLabel: true
      },
      xAxis: {
        type: 'category',
        boundaryGap: false,
        data: ['3/11', '3/12', '3/13', '3/14', '3/15', '3/16', '3/17'],
        axisLine: {
          lineStyle: {
            color: '#2d3a4d'
          }
        },
        axisLabel: {
          color: '#6b7a8a'
        },
        axisTick: {
          show: false
        }
      },
      yAxis: {
        type: 'value',
        axisLine: {
          show: false
        },
        axisLabel: {
          color: '#6b7a8a'
        },
        splitLine: {
          lineStyle: {
            color: '#2d3a4d',
            type: 'dashed'
          }
        }
      },
      series: [
        {
          name: '计划产量',
          type: 'line',
          smooth: true,
          symbol: 'circle',
          symbolSize: 8,
          showSymbol: false,
          lineStyle: {
            width: 3,
            color: new echarts.graphic.LinearGradient(0, 0, 1, 0, [
              { offset: 0, color: '#5b9bff' },
              { offset: 1, color: '#3b82f6' }
            ])
          },
          itemStyle: {
            color: '#5b9bff',
            borderColor: '#1a2332',
            borderWidth: 2
          },
          areaStyle: {
            color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
              { offset: 0, color: 'rgba(91, 155, 255, 0.3)' },
              { offset: 1, color: 'rgba(91, 155, 255, 0)' }
            ])
          },
          data: [120, 132, 101, 134, 90, 230, 210]
        },
        {
          name: '实际产量',
          type: 'line',
          smooth: true,
          symbol: 'circle',
          symbolSize: 8,
          showSymbol: false,
          lineStyle: {
            width: 3,
            color: new echarts.graphic.LinearGradient(0, 0, 1, 0, [
              { offset: 0, color: '#10fa9e' },
              { offset: 1, color: '#10b981' }
            ])
          },
          itemStyle: {
            color: '#10fa9e',
            borderColor: '#1a2332',
            borderWidth: 2
          },
          areaStyle: {
            color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
              { offset: 0, color: 'rgba(16, 250, 158, 0.3)' },
              { offset: 1, color: 'rgba(16, 250, 158, 0)' }
            ])
          },
          data: [125, 135, 98, 140, 95, 228, 215]
        }
      ]
    }
    chart.setOption(option)
  }

  const initPieChart = () => {
    if (!pieChartRef.value) return
    const chart = echarts.init(pieChartRef.value)
    const option = {
      tooltip: {
        trigger: 'item',
        backgroundColor: '#1a2332',
        borderColor: '#2d3a4d',
        textStyle: {
          color: '#e8f4f8'
        }
      },
      legend: {
        orient: 'vertical',
        right: '5%',
        top: 'center',
        textStyle: {
          color: '#b8c5d0'
        },
        itemWidth: 10,
        itemHeight: 10,
        itemGap: 12
      },
      series: [
        {
          name: '工单状态',
          type: 'pie',
          radius: ['50%', '75%'],
          center: ['35%', '50%'],
          avoidLabelOverlap: false,
          itemStyle: {
            borderRadius: 6,
            borderColor: '#1a2332',
            borderWidth: 3
          },
          label: {
            show: false,
            position: 'center'
          },
          emphasis: {
            scale: true,
            scaleSize: 10,
            label: {
              show: true,
              fontSize: '16',
              fontWeight: 'bold',
              color: '#e8f4f8',
              formatter: '{b}\n{d}%'
            }
          },
          labelLine: {
            show: false
          },
          data: [
            { value: 1048, name: '已完成', itemStyle: { color: '#10fa9e' } },
            { value: 735, name: '执行中', itemStyle: { color: '#5b9bff' } },
            { value: 580, name: '待开工', itemStyle: { color: '#fbbf24' } },
            { value: 484, name: '待审核', itemStyle: { color: '#6b7a8a' } },
            { value: 300, name: '异常', itemStyle: { color: '#ff5757' } }
          ]
        }
      ]
    }
    chart.setOption(option)
  }

  onMounted(() => {
    initLineChart()
    initPieChart()
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
    margin-bottom: 8px;
    display: flex;
    align-items: baseline;
    gap: 4px;
  }

  .value-number {
    font-family: 'JetBrains Mono', monospace;
    letter-spacing: -1px;
  }

  .value-unit {
    font-size: 16px;
    color: var(--text-tertiary);
  }

  .stat-change {
    font-size: 12px;
    display: flex;
    align-items: center;
    gap: 4px;
  }

  .stat-change.increase {
    color: var(--success);
  }

  .stat-change.decrease {
    color: var(--danger);
  }

  .stat-sub {
    color: var(--text-tertiary);
    margin-left: 4px;
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

  /* 图表区域 */
  .chart-row {
    margin-bottom: 24px;
  }

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

  .chart-tabs {
    display: flex;
    gap: 4px;
    background: var(--bg-elevated);
    padding: 4px;
    border-radius: var(--radius-md);
  }

  .chart-tab {
    padding: 6px 16px;
    font-size: 13px;
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
    font-weight: 500;
  }

  .chart-content {
    padding: 20px 24px;
  }

  .line-chart {
    height: 320px;
  }

  .pie-chart {
    height: 320px;
  }

  /* 表格样式 */
  .table-container {
    padding: 0 8px 16px 8px;
  }

  .tech-table {
    background: transparent !important;
  }

  :deep(.el-table) {
    --el-table-bg-color: transparent !important;
    --el-table-text-color: var(--text-primary) !important;
    --el-table-border-color: transparent !important;
    --el-table-header-bg-color: transparent !important;
    --el-table-row-hover-bg-color: var(--bg-hover) !important;
    background-color: transparent !important;
    color: var(--text-primary);
  }

  :deep(.el-table th.el-table__cell) {
    background: transparent !important;
    color: var(--text-tertiary) !important;
    font-weight: 600;
    font-size: 11px;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    border-bottom: 1px solid var(--border-primary) !important;
    padding: 12px 0;
  }

  :deep(.el-table td.el-table__cell) {
    background-color: transparent !important;
    color: var(--text-primary) !important;
    border-bottom: 1px solid var(--border-primary) !important;
    padding: 14px 0;
  }

  :deep(.el-table__row) {
    background-color: transparent !important;
  }

  :deep(.el-table__row:hover > td.el-table__cell) {
    background-color: var(--bg-hover) !important;
  }

  /* 表格滚动容器 */
  :deep(.el-table__body-wrapper) {
    background-color: transparent !important;
  }

  :deep(.el-table__empty-block) {
    background-color: transparent !important;
  }

  .todo-item {
    display: flex;
    align-items: center;
    gap: 10px;
  }

  .priority-dot {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    flex-shrink: 0;
  }

  .priority-dot.high { background: var(--danger); box-shadow: 0 0 6px var(--danger); }
  .priority-dot.medium { background: var(--warning); box-shadow: 0 0 6px var(--warning); }
  .priority-dot.low { background: var(--info); box-shadow: 0 0 6px var(--info); }

  .priority-tag {
    flex-shrink: 0;
  }

  .todo-title {
    flex: 1;
    font-size: 14px;
    color: var(--text-primary);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .time-text {
    font-size: 12px;
    color: var(--text-tertiary);
    font-family: 'JetBrains Mono', monospace;
  }

  /* 公告列表 */
  .notice-list {
    padding: 8px 24px 20px 24px;
  }

  .notice-item {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 14px 16px;
    border-radius: var(--radius-md);
    transition: all 0.3s;
    cursor: pointer;
    border: 1px solid transparent;
    margin-bottom: 8px;
  }

  .notice-item:hover {
    background: var(--bg-hover);
    border-color: var(--border-primary);
  }

  .notice-item.is-new {
    background: rgba(6, 212, 228, 0.05);
  }

  .notice-indicator {
    width: 4px;
    height: 32px;
    border-radius: 2px;
    background: var(--border-primary);
    flex-shrink: 0;
  }

  .notice-item.is-new .notice-indicator {
    background: var(--primary);
    box-shadow: 0 0 8px var(--primary-glow);
  }

  .notice-content {
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 4px;
  }

  .notice-title {
    font-size: 14px;
    color: var(--text-primary);
  }

  .notice-item.is-new .notice-title {
    font-weight: 500;
  }

  .notice-time {
    font-size: 12px;
    color: var(--text-tertiary);
    font-family: 'JetBrains Mono', monospace;
  }

  .new-icon {
    color: var(--primary);
    font-size: 16px;
  }
</style>
