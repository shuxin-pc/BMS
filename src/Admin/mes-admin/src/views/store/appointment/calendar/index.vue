<template>
  <div class="appointment-calendar">
    <!-- 操作栏 -->
    <div class="card mb-20">
      <div class="calendar-toolbar">
        <div class="toolbar-left">
          <el-radio-group v-model="viewType" @change="handleViewChange">
            <el-radio-button value="day">日视图</el-radio-button>
            <el-radio-button value="week">周视图</el-radio-button>
          </el-radio-group>
          <el-radio-group v-model="dimension" class="ml-12" @change="loadData">
            <el-radio-button value="technician">按技师</el-radio-button>
            <el-radio-button value="room">按房间</el-radio-button>
          </el-radio-group>
        </div>
        <div class="toolbar-center">
          <el-button circle @click="handlePrev">
            <el-icon><ArrowLeft /></el-icon>
          </el-button>
          <span class="current-period">{{ currentPeriodLabel }}</span>
          <el-button circle @click="handleNext">
            <el-icon><ArrowRight /></el-icon>
          </el-button>
          <el-button @click="handleToday">今天</el-button>
        </div>
        <div class="toolbar-right">
          <el-button circle @click="loadData">
            <el-icon><Refresh /></el-icon>
          </el-button>
        </div>
      </div>
    </div>

    <!-- 日历网格 -->
    <div class="card">
      <div v-loading="loading" class="calendar-container">
        <!-- 日视图 -->
        <div v-if="viewType === 'day'" class="day-view">
          <div class="time-grid">
            <div class="time-axis">
              <div v-for="hour in hours" :key="hour" class="time-slot">
                {{ hour }}:00
              </div>
            </div>
            <div class="schedule-area">
              <div v-for="col in scheduleColumns" :key="col.key" class="schedule-column">
                <div class="column-header">{{ col.label }}</div>
                <div class="column-body">
                  <div v-for="hour in hours" :key="hour" class="time-cell"></div>
                  <div
                    v-for="item in col.appointments"
                    :key="item.id"
                    class="appointment-block"
                    :style="getAppointmentStyle(item)"
                    :class="`status-${item.status}`"
                    @click="handleClickAppointment(item)"
                  >
                    <div class="block-time">{{ item.appointmentTime }} - {{ item.endTime }}</div>
                    <div class="block-customer">{{ item.customerName }}</div>
                    <div class="block-service">{{ item.productName }}</div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- 周视图 -->
        <div v-else class="week-view">
          <div class="week-grid">
            <div class="week-header">
              <div class="time-corner"></div>
              <div v-for="day in weekDays" :key="day.date" class="day-header" :class="{ today: day.isToday }">
                <div class="day-name">{{ day.weekday }}</div>
                <div class="day-date">{{ day.dateLabel }}</div>
              </div>
            </div>
            <div class="week-body">
              <div class="time-axis">
                <div v-for="hour in hours" :key="hour" class="time-slot">
                  {{ hour }}:00
                </div>
              </div>
              <div v-for="day in weekDays" :key="day.date" class="day-column">
                <div v-for="hour in hours" :key="hour" class="time-cell"></div>
                <div
                  v-for="item in getAppointmentsByDate(day.date)"
                  :key="item.id"
                  class="appointment-block"
                  :style="getAppointmentStyle(item)"
                  :class="`status-${item.status}`"
                  @click="handleClickAppointment(item)"
                >
                  <div class="block-time">{{ item.appointmentTime }} - {{ item.endTime }}</div>
                  <div class="block-customer">{{ item.customerName }}</div>
                  <div class="block-service">{{ item.productName }}</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 当日预约列表弹窗 -->
    <el-dialog v-model="dayDialogVisible" :title="`${selectedDateLabel} 的预约`" width="720px">
      <el-table :data="selectedDateAppointments" style="width: 100%">
        <el-table-column prop="appointmentNo" label="预约编号" width="150" />
        <el-table-column prop="customerName" label="客户名称" width="100" />
        <el-table-column prop="serviceName" label="服务项目" width="120" />
        <el-table-column prop="technicianName" label="技师" width="80" />
        <el-table-column label="时间段" width="130">
          <template #default="{ row }">
            {{ row.startTime }} - {{ row.endTime }}
          </template>
        </el-table-column>
        <el-table-column label="状态" width="90">
          <template #default="{ row }">
            <el-tag :type="getStatusTagType(row.status)" size="small" effect="dark">
              {{ getStatusText(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
      </el-table>
      <template #footer>
        <el-button @click="dayDialogVisible = false">关闭</el-button>
      </template>
    </el-dialog>

    <!-- 预约详情弹窗 -->
    <el-dialog v-model="detailDialogVisible" title="预约详情" width="500px">
      <el-descriptions :column="1" border v-if="selectedAppointment">
        <el-descriptions-item label="预约编号">{{ selectedAppointment.appointmentNo }}</el-descriptions-item>
        <el-descriptions-item label="客户名称">{{ selectedAppointment.customerName }}</el-descriptions-item>
        <el-descriptions-item label="手机号">{{ selectedAppointment.customerPhone }}</el-descriptions-item>
        <el-descriptions-item label="服务项目">{{ selectedAppointment.productName }}</el-descriptions-item>
        <el-descriptions-item label="技师">
          <!-- TODO: 后端 Appointment 不返回 technicianName，需关联技师信息 -->
          {{ selectedAppointment.technicianId ? `技师ID: ${selectedAppointment.technicianId}` : '-' }}
        </el-descriptions-item>
        <el-descriptions-item label="技师来源">
          <el-tag
            v-if="selectedAppointment.technicianSource === 1"
            type=""
            size="small"
            effect="plain"
          >商家技师</el-tag>
          <el-tag
            v-else-if="selectedAppointment.technicianSource === 2"
            type="success"
            size="small"
            effect="plain"
          >平台技师</el-tag>
          <span v-else>-</span>
        </el-descriptions-item>
        <el-descriptions-item label="房间">
          <!-- TODO: 后端 Appointment 不返回 roomName，需关联房间信息 -->
          {{ selectedAppointment.roomId ? `房间ID: ${selectedAppointment.roomId}` : '-' }}
        </el-descriptions-item>
        <el-descriptions-item label="预约日期">{{ selectedAppointment.appointmentDate }}</el-descriptions-item>
        <el-descriptions-item label="时间段">{{ selectedAppointment.appointmentTime }} - {{ selectedAppointment.endTime }}</el-descriptions-item>
        <el-descriptions-item label="状态">
          <el-tag :type="getStatusTagType(selectedAppointment.status)" size="small" effect="dark">
            {{ getStatusText(selectedAppointment.status) }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="备注">{{ selectedAppointment.remark || '-' }}</el-descriptions-item>
      </el-descriptions>
      <template #footer>
        <el-button @click="detailDialogVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { ArrowLeft, ArrowRight, Refresh } from '@element-plus/icons-vue'
import { getAppointments } from '@/api/appointment'
import type { Appointment, CalendarViewType, CalendarDimension } from '@/api/appointment/types'
import { useSystemConfigStore } from '@/stores/systemConfig'

const systemConfigStore = useSystemConfigStore()

// 视图类型
const viewType = ref<CalendarViewType>('day')
const dimension = ref<CalendarDimension>('technician')

// 当前日期（用于翻页）
const currentDate = ref(new Date())

// 时间轴：9:00 - 21:00
const hours = Array.from({ length: 13 }, (_, i) => i + 9)

// 数据
const loading = ref(false)
const allAppointments = ref<Appointment[]>([])

// 弹窗
const dayDialogVisible = ref(false)
const detailDialogVisible = ref(false)
const selectedDate = ref('')
const selectedAppointment = ref<Appointment | null>(null)

// 当前周期标签
const currentPeriodLabel = computed(() => {
  const y = currentDate.value.getFullYear()
  const m = String(currentDate.value.getMonth() + 1).padStart(2, '0')
  const d = String(currentDate.value.getDate()).padStart(2, '0')
  if (viewType.value === 'day') {
    return `${y}-${m}-${d}`
  }
  // 周视图：显示本周范围
  const weekStart = new Date(currentDate.value)
  const weekEnd = new Date(currentDate.value)
  const dayOfWeek = currentDate.value.getDay() || 7
  weekStart.setDate(weekStart.getDate() - dayOfWeek + 1)
  weekEnd.setDate(weekEnd.getDate() - dayOfWeek + 7)
  const sM = String(weekStart.getMonth() + 1).padStart(2, '0')
  const sD = String(weekStart.getDate()).padStart(2, '0')
  const eM = String(weekEnd.getMonth() + 1).padStart(2, '0')
  const eD = String(weekEnd.getDate()).padStart(2, '0')
  return `${sM}.${sD} - ${eM}.${eD}`
})

// 日期格式化
const formatDate = (date: Date): string => {
  const y = date.getFullYear()
  const m = String(date.getMonth() + 1).padStart(2, '0')
  const d = String(date.getDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}

// 周天数列表
const weekDays = computed(() => {
  const weekdays = ['周日', '周一', '周二', '周三', '周四', '周五', '周六']
  const dayOfWeek = currentDate.value.getDay() || 7
  const result = []
  const today = formatDate(new Date())
  for (let i = 1; i <= 7; i++) {
    const d = new Date(currentDate.value)
    d.setDate(d.getDate() - dayOfWeek + i)
    const dateStr = formatDate(d)
    result.push({
      date: dateStr,
      weekday: weekdays[d.getDay()],
      dateLabel: `${d.getMonth() + 1}/${d.getDate()}`,
      isToday: dateStr === today
    })
  }
  return result
})

// 日视图：按维度（技师/房间）分列
const scheduleColumns = computed(() => {
  const dateStr = formatDate(currentDate.value)
  const dayAppointments = allAppointments.value.filter(a => a.appointmentDate === dateStr)
  const key = dimension.value === 'technician' ? 'technicianId' : 'roomId'
  const labels = dimension.value === 'technician' ? '技师' : '房间'

  // 提取所有列
  const colMap = new Map<string, Appointment[]>()
  dayAppointments.forEach(a => {
    const colName = a[key] != null ? String(a[key]) : '未分配'
    if (!colMap.has(colName)) colMap.set(colName, [])
    colMap.get(colName)!.push(a)
  })
  if (colMap.size === 0) {
    colMap.set('暂无', [])
  }

  return Array.from(colMap.entries()).map(([name, appointments]) => ({
    key: name,
    label: `${labels}：${name}`,
    appointments: appointments.sort((a, b) => a.appointmentTime.localeCompare(b.appointmentTime))
  }))
})

// 按日期获取预约
const getAppointmentsByDate = (date: string): Appointment[] => {
  return allAppointments.value
    .filter(a => a.appointmentDate === date)
    .sort((a, b) => a.appointmentTime.localeCompare(b.appointmentTime))
}

// 计算预约块的位置样式
const getAppointmentStyle = (item: Appointment) => {
  const startHour = parseInt(item.appointmentTime.split(':')[0])
  const startMin = parseInt(item.appointmentTime.split(':')[1])
  const endStr = item.endTime || item.appointmentTime
  const endHour = parseInt(endStr.split(':')[0])
  const endMin = parseInt(endStr.split(':')[1])
  const startOffset = (startHour - 9) * 60 + startMin
  const duration = (endHour - startHour) * 60 + (endMin - startMin)
  const cellHeight = 60 // 每小时 60px
  return {
    top: `${(startOffset / 60) * cellHeight}px`,
    height: `${(duration / 60) * cellHeight - 2}px`
  }
}

// 状态文本
const getStatusText = (status: number): string => {
  const map: Record<number, string> = { 1: '待确认', 2: '已预约', 3: '已到店', 4: '已完成', 5: '已取消', 6: '爽约' }
  return map[status] || '未知'
}

// 状态标签类型
const getStatusTagType = (status: number): '' | 'success' | 'info' | 'warning' | 'danger' => {
  const map: Record<number, '' | 'success' | 'info' | 'warning' | 'danger'> = {
    1: 'warning',
    2: '',
    3: 'success',
    4: 'info',
    5: 'danger',
    6: 'danger'
  }
  return map[status] || 'info'
}

// 选中日期的预约
const selectedDateAppointments = computed(() => {
  if (!selectedDate.value) return []
  return getAppointmentsByDate(selectedDate.value)
})

const selectedDateLabel = computed(() => selectedDate.value)

// 加载数据
const loadData = async () => {
  loading.value = true
  try {
    // 计算查询范围
    let startDate: string
    let endDate: string
    if (viewType.value === 'day') {
      startDate = formatDate(currentDate.value)
      endDate = startDate
    } else {
      const dayOfWeek = currentDate.value.getDay() || 7
      const ws = new Date(currentDate.value)
      ws.setDate(ws.getDate() - dayOfWeek + 1)
      const we = new Date(currentDate.value)
      we.setDate(we.getDate() - dayOfWeek + 7)
      startDate = formatDate(ws)
      endDate = formatDate(we)
    }
    const res = await getAppointments({ appointmentDateStart: startDate, appointmentDateEnd: endDate, pageSize: 100 })
    allAppointments.value = res.list
  } catch {
    ElMessage.error('加载预约数据失败')
  } finally {
    loading.value = false
  }
}

// 视图切换
const handleViewChange = () => {
  loadData()
}

// 上一页
const handlePrev = () => {
  const d = new Date(currentDate.value)
  if (viewType.value === 'day') {
    d.setDate(d.getDate() - 1)
  } else {
    d.setDate(d.getDate() - 7)
  }
  currentDate.value = d
  loadData()
}

// 下一页
const handleNext = () => {
  const d = new Date(currentDate.value)
  if (viewType.value === 'day') {
    d.setDate(d.getDate() + 1)
  } else {
    d.setDate(d.getDate() + 7)
  }
  currentDate.value = d
  loadData()
}

// 回到今天
const handleToday = () => {
  currentDate.value = new Date()
  loadData()
}

// 点击预约
const handleClickAppointment = (item: Appointment) => {
  selectedAppointment.value = item
  detailDialogVisible.value = true
}

onMounted(async () => {
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  loadData()
})
</script>

<style scoped>
.appointment-calendar {
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

.ml-12 {
  margin-left: 12px;
}

/* 日历操作栏 */
.calendar-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 24px;
}

.toolbar-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.toolbar-center {
  display: flex;
  align-items: center;
  gap: 8px;
}

.current-period {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
  min-width: 140px;
  text-align: center;
}

.toolbar-right {
  display: flex;
  gap: 8px;
}

/* 日历容器 */
.calendar-container {
  min-height: 600px;
  overflow: auto;
}

/* 日视图 */
.day-view .time-grid {
  display: flex;
}

.time-axis {
  width: 60px;
  flex-shrink: 0;
  border-right: 1px solid var(--border-primary);
}

.time-slot {
  height: 60px;
  padding: 4px 8px;
  font-size: 12px;
  color: var(--text-tertiary);
  text-align: right;
  border-bottom: 1px solid var(--border-primary);
}

.schedule-area {
  flex: 1;
  display: flex;
  overflow-x: auto;
}

.schedule-column {
  flex: 1;
  min-width: 180px;
  border-right: 1px solid var(--border-primary);
}

.column-header {
  padding: 10px;
  font-size: 13px;
  font-weight: 600;
  color: var(--text-primary);
  text-align: center;
  border-bottom: 1px solid var(--border-primary);
  background: var(--bg-tertiary);
}

.column-body {
  position: relative;
}

.time-cell {
  height: 60px;
  border-bottom: 1px solid var(--border-primary);
}

/* 预约块 */
.appointment-block {
  position: absolute;
  left: 4px;
  right: 4px;
  padding: 6px 8px;
  border-radius: 6px;
  font-size: 12px;
  cursor: pointer;
  overflow: hidden;
  border-left: 3px solid var(--primary);
  background: rgba(64, 158, 255, 0.1);
  transition: all 0.2s;
}

.appointment-block:hover {
  transform: scale(1.02);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
}

.appointment-block.status-1 {
  border-left-color: #e6a23c;
  background: rgba(230, 162, 60, 0.12);
}

.appointment-block.status-2 {
  border-left-color: var(--primary);
  background: rgba(64, 158, 255, 0.12);
}

.appointment-block.status-3 {
  border-left-color: #67c23a;
  background: rgba(103, 194, 58, 0.12);
}

.appointment-block.status-4 {
  border-left-color: #909399;
  background: rgba(144, 147, 153, 0.12);
}

.appointment-block.status-5 {
  border-left-color: #f56c6c;
  background: rgba(245, 108, 108, 0.12);
}

.appointment-block.status-6 {
  border-left-color: #f56c6c;
  background: rgba(245, 108, 108, 0.12);
}

.block-time {
  font-weight: 600;
  color: var(--text-primary);
}

.block-customer {
  color: var(--text-primary);
  margin-top: 2px;
}

.block-service {
  color: var(--text-tertiary);
  margin-top: 2px;
}

/* 周视图 */
.week-grid {
  min-width: 800px;
}

.week-header {
  display: flex;
  border-bottom: 1px solid var(--border-primary);
}

.time-corner {
  width: 60px;
  flex-shrink: 0;
}

.day-header {
  flex: 1;
  text-align: center;
  padding: 10px;
  border-right: 1px solid var(--border-primary);
}

.day-header.today {
  background: rgba(64, 158, 255, 0.08);
}

.day-name {
  font-size: 12px;
  color: var(--text-tertiary);
}

.day-date {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
  margin-top: 4px;
}

.week-body {
  display: flex;
}

.week-body .time-axis {
  border-right: 1px solid var(--border-primary);
}

.day-column {
  flex: 1;
  position: relative;
  border-right: 1px solid var(--border-primary);
  min-width: 100px;
}

.day-column .time-cell {
  height: 60px;
  border-bottom: 1px solid var(--border-primary);
}

.day-column .appointment-block {
  left: 2px;
  right: 2px;
}

/* 表格样式覆盖 */
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
</style>
