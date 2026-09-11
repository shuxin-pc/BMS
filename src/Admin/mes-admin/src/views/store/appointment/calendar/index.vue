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

    <!-- 日历主体 -->
    <div class="card">
      <div v-loading="loading" class="calendar-container">
        <!-- 技师/房间分组条：日/周视图通用（资源按列分组） -->
        <div v-if="viewType === 'day' || viewType === 'week'" class="groupbar">
          <div class="gb-info">
            <span class="gb-title">{{ dimension === 'technician' ? '技师分组' : '房间分组' }}</span>
            <span class="gb-dots">
              <span
                v-for="i in groupCount"
                :key="i"
                class="gb-dot"
                :class="{ on: i - 1 === groupIndex }"
                @click="groupIndex = i - 1"
              ></span>
            </span>
            <span class="gb-total">
              共 {{ allColumns.length }} {{ dimension === 'technician' ? '位' : '间' }}
            </span>
          </div>
          <div class="gb-ctrl">
            <el-button size="small" :disabled="groupIndex === 0" @click="handlePrevGroup">‹ 上一组</el-button>
            <span class="gb-num">{{ groupIndex + 1 }} / {{ groupCount }}</span>
            <el-button size="small" :disabled="groupIndex >= groupCount - 1" @click="handleNextGroup">下一组 ›</el-button>
          </div>
        </div>

        <!-- 日视图 -->
        <div v-if="viewType === 'day'" class="day-view">
          <!-- 列头行：time-corner 占位保证与时间轴起始对齐 -->
          <div class="day-header-row">
            <div class="time-corner"></div>
            <div
              v-for="col in currentGroupColumns"
              :key="col.key"
              class="column-header"
              :class="{ placeholder: col.placeholder }"
            >
              <template v-if="!col.placeholder">
                <div class="col-title">
                  <span class="col-dot" :class="{ on: col.appointments.length > 0 }"></span>{{ col.label }}
                </div>
                <div class="col-meta" :class="{ busy: col.appointments.length > 0 }">
                  {{ col.appointments.length > 0 ? `有预约 · ${col.appointments.length} 条` : '无预约' }}
                </div>
              </template>
              <div v-else class="col-title">—</div>
            </div>
          </div>
          <div class="time-grid">
            <div class="time-axis">
              <div
                v-for="tick in timeScale.ticks"
                :key="`${tick.hour}-${tick.isNextDay}`"
                class="time-slot"
                :class="{ 'next-day': tick.isNextDay }"
              >{{ tick.isNextDay ? `次日${tick.hour}:00` : `${tick.hour}:00` }}</div>
            </div>
            <div v-if="nowMarker.visible" class="now-tag" :style="{ top: nowMarker.top - 13 + 'px' }">现在</div>
            <div class="schedule-area">
              <div
                v-for="col in currentGroupColumns"
                :key="col.key"
                class="schedule-column"
                :class="{ placeholder: col.placeholder }"
              >
                <div class="column-body">
                  <div v-for="tick in timeScale.ticks" :key="`${tick.hour}-${tick.isNextDay}`" class="time-cell"></div>
                  <div v-if="nowMarker.visible" class="now-line" :style="{ top: nowMarker.top + 'px' }"></div>
                  <div
                    v-for="item in col.appointments"
                    :key="item.id"
                    class="appointment-block"
                    :class="`status-${item.status}`"
                    :style="getAppointmentStyle(item)"
                    @click="handleClickAppointment(item)"
                  >
                    <div class="block-time">
                      <span class="bt-range">{{ formatAppointmentTimeRange(item) }}</span>
                      <span class="block-status" :class="`st-${item.status}`">{{ getStatusText(item.status) }}</span>
                    </div>
                    <div class="block-customer">{{ item.customerName }}</div>
                    <!-- 项目名与资源名同一行：按技师展示时右侧显示房间名，按房间展示时右侧显示技师名 -->
                    <div class="block-info">
                      <span class="bi-service">{{ item.productName }}</span>
                      <span
                        v-if="dimension === 'technician' ? item.roomName : item.technicianName"
                        class="bi-resource"
                      >
                        {{ dimension === 'technician' ? item.roomName : item.technicianName }}
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- 周视图：按技师/房间分列 × 周一~周日为行 -->
        <div v-else class="week-view">
          <div class="week-grid">
            <!-- 列头行：time-corner 占位 + 资源列（复用日视图列头样式） -->
            <div class="day-header-row">
              <div class="time-corner"></div>
              <div
                v-for="col in currentGroupColumns"
                :key="col.key"
                class="column-header"
                :class="{ placeholder: col.placeholder }"
              >
                <template v-if="!col.placeholder">
                  <div class="col-title">
                    <span class="col-dot" :class="{ on: getWeekCount(col) > 0 }"></span>{{ col.label }}
                  </div>
                  <div class="col-meta" :class="{ busy: getWeekCount(col) > 0 }">
                    {{ getWeekCount(col) > 0 ? `本周 · ${getWeekCount(col)} 条` : '无预约' }}
                  </div>
                </template>
                <div v-else class="col-title">—</div>
              </div>
            </div>
            <!-- 主体：左侧周一~周日刻度 + 资源周列 -->
            <div class="week-body">
              <div class="week-axis">
                <div
                  v-for="day in weekDays"
                  :key="day.date"
                  class="week-axis-row"
                  :class="{ today: day.isToday }"
                >
                  <div class="war-name">{{ day.weekday }}</div>
                  <div class="war-date">{{ day.dateLabel }}</div>
                </div>
              </div>
              <div
                v-for="col in currentGroupColumns"
                :key="col.key"
                class="week-col"
                :class="{ placeholder: col.placeholder }"
              >
                <div
                  v-for="day in weekDays"
                  :key="day.date"
                  class="week-cell"
                  :class="{ today: day.isToday }"
                >
                  <div
                    v-for="item in getWeekCellAppointments(col, day.date)"
                    :key="item.id"
                    class="week-cell-item"
                    :class="`st-${item.status}`"
                    @click="handleClickAppointment(item)"
                  >
                    <span class="wci-time">{{ formatAppointmentTimeRange(item) }}</span>
                    <span class="wci-customer">{{ item.customerName }}</span>
                  </div>
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
            {{ formatAppointmentTimeRange(row) }}
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
      <el-descriptions :column="1" border label-width="88px" v-if="selectedAppointment">
        <el-descriptions-item label="预约编号">{{ selectedAppointment.appointmentNo }}</el-descriptions-item>
        <el-descriptions-item label="客户名称">{{ selectedAppointment.customerName }}</el-descriptions-item>
        <el-descriptions-item label="手机号">{{ selectedAppointment.customerPhone }}</el-descriptions-item>
        <el-descriptions-item label="服务项目">{{ selectedAppointment.productName }}</el-descriptions-item>
        <el-descriptions-item label="技师">{{ selectedAppointment.technicianName || '-' }}</el-descriptions-item>
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
        <el-descriptions-item label="房间">{{ selectedAppointment.roomName || '-' }}</el-descriptions-item>
        <el-descriptions-item label="预约时间">{{ formatDetailTimeRange(selectedAppointment) }}</el-descriptions-item>
        <el-descriptions-item label="完成时间">{{ formatShortDateTime(selectedAppointment.completeTime) }}</el-descriptions-item>
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
import { getTechnicians } from '@/api/technician'
import { getRoomList } from '@/api/room'
import { getStore } from '@/api/store'
import type { Appointment, CalendarViewType, CalendarDimension } from '@/api/appointment/types'
import type { Technician } from '@/api/technician/types'
import type { Room } from '@/api/room/types'
import { useSystemConfigStore } from '@/stores/systemConfig'
import { useUserStore } from '@/stores/user'
import { formatDate } from '@/utils/date'

const systemConfigStore = useSystemConfigStore()
const userStore = useUserStore()

// 视图类型与维度
const viewType = ref<CalendarViewType>('day')
const dimension = ref<CalendarDimension>('technician')

// 当前日期（用于翻页）
const currentDate = ref(new Date())

// 每小时对应像素高度
const HOUR_HEIGHT = 60

// 门店营业时间（"HH:mm - HH:mm"；未设置时为空字符串，刻度回退为 24 小时显示）
const businessHours = ref('')

// 解析 "HH:mm" 为当日分钟数
const parseTime = (s: string): number => {
  const [h, m] = s.split(':').map(Number)
  return h * 60 + m
}

// 时间轴刻度：以营业时间为基准，按当天预约动态扩充，保证每条预约落在其真实时间对应的刻度位置
// 规则：开始时间向下取整（08:30 → 08:00），结束时间向上取整（22:30 → 23:00）
// 夜班跨自然日（结束时刻 < 开始时刻）：刻度线性延伸至次日凌晨（如 20:00-次日02:00 → 20..23, 次日0..2）
// 当天存在营业时段之外的预约（如营业 22:30-08:30 当天有 14:45 的预约）时，
// 时间轴按当天预约向前/向后扩充；无此类预约的日期严格按营业时间显示
// 未设置营业时间：默认 24 小时刻度（0:00 - 24:00）
// 实现依赖 allAppointments / formatDate，定义于其后的数据区段
interface TimeTick {
  /** 小时（0-24） */
  hour: number
  /** 是否为次日凌晨刻度（夜班跨天场景） */
  isNextDay: boolean
}
interface TimeScale {
  /** 时间轴起点分钟（当日，如 22:00 → 1320） */
  startMin: number
  /** 时间轴终点分钟（绝对分钟，跨天时 > 1440，如 次日 09:00 → 1980） */
  endAbs: number
  /** 是否跨自然日（夜班） */
  spanMidnight: boolean
  /** 营业时段起点分钟（当日；未设置营业时间时为 0） */
  bizStartMin: number
  /** 营业时段终点分钟（绝对分钟，跨天时 > 1440；未设置营业时间时为 1440） */
  bizEndAbs: number
  /** 刻度列表 */
  ticks: TimeTick[]
}

// 分组列数：一组最多展示的技师/房间列数（配合内容区宽度，无需横向滚动条）
const GROUP_SIZE = 6
const groupIndex = ref(0)

// 数据
const loading = ref(false)
const allAppointments = ref<Appointment[]>([])
const allTechnicians = ref<Technician[]>([])
const allRooms = ref<Room[]>([])

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


// 时间轴刻度计算：营业时间为基准范围，再按当天（currentDate 对应日期）预约动态扩充
// 扩充规则：当天预约的开始时刻早于轴起点时向前扩展（向下取整），
// 结束时刻（跨天已折算 +1440）晚于轴终点时向后扩展（向上取整）
// 次日凌晨段的预约（起点分钟 <= 次日段上限）已由营业时段覆盖，不回拉起点，避免整轴拉到 0 点
const timeScale = computed<TimeScale>(() => {
  // 1. 营业时间基础范围（未设置或解析失败时回退 24 小时）
  const parts = businessHours.value ? businessHours.value.split('-').map(s => s.trim()) : []
  const startM = parts.length === 2 ? parseTime(parts[0]) : NaN
  const endM = parts.length === 2 ? parseTime(parts[1]) : NaN
  let bizStartMin: number
  let bizEndAbs: number
  if (isNaN(startM) || isNaN(endM)) {
    bizStartMin = 0
    bizEndAbs = 1440
  } else {
    bizStartMin = Math.floor(startM / 60) * 60
    let endRaw = Math.ceil(endM / 60) * 60
    if (endRaw < bizStartMin) endRaw += 1440 // 营业跨自然日
    bizEndAbs = endRaw
  }
  let axisStart = bizStartMin
  let axisEnd = bizEndAbs
  // 次日凌晨段上限：营业跨天时次日凌晨刻度覆盖 [0, 上限]（如营业至 08:30 → 上限 510）
  const morningLimit = bizEndAbs > 1440 ? bizEndAbs - 1440 : -1
  // 2. 按当天预约扩充范围
  const dateStr = formatDate(currentDate.value)
  for (const a of allAppointments.value) {
    // 一体格式开始时间取日期部分（YYYY-MM-DD）与当日比较；跨日预约归属开始日期所在日展示
    if (a.startTime.slice(0, 10) !== dateStr) continue
    const startAbs = parseTime(a.startTime.slice(11, 16))
    const endStr = a.endTime ? a.endTime.slice(11, 16) : a.startTime.slice(11, 16)
    let endAbs = parseTime(endStr)
    if (endAbs <= startAbs) endAbs += 1440 // 预约跨自然日
    if (startAbs > morningLimit) {
      axisStart = Math.min(axisStart, Math.floor(startAbs / 60) * 60)
    }
    axisEnd = Math.max(axisEnd, Math.ceil(endAbs / 60) * 60)
  }
  const spanMidnight = axisEnd > 1440 || axisEnd < axisStart
  // 3. 生成整点刻度
  const ticks: TimeTick[] = []
  const startHour = axisStart / 60
  if (spanMidnight) {
    // endAbs 为绝对分钟（如次日 09:00 → 1980），次日段小时需折算回当天基准（1980-1440=540 → 9）
    const nextDayEndHour = (axisEnd - 1440) / 60
    for (let h = startHour; h <= 23; h++) ticks.push({ hour: h, isNextDay: false })
    for (let h = 0; h <= nextDayEndHour; h++) ticks.push({ hour: h, isNextDay: true })
  } else {
    const endHour = axisEnd / 60
    for (let h = startHour; h <= endHour; h++) ticks.push({ hour: h, isNextDay: false })
  }
  return { startMin: axisStart, endAbs: axisEnd, spanMidnight, bizStartMin, bizEndAbs, ticks }
})

// 把某时刻（当日分钟数）映射为距刻度起点的分钟偏移
// 跨天营业：当日 [开始, 24:00) 归入首段；次日 [0:00, 结束] 归入延伸段
// 非跨天或其余情形：直接相对起点取差（时间轴已覆盖当天全部预约，偏移不会为负）
const timeToOffset = (minute: number): number => {
  const { startMin, endAbs, spanMidnight } = timeScale.value
  if (spanMidnight && minute >= startMin) return minute - startMin
  if (spanMidnight && minute <= endAbs - 1440) return (1440 - startMin) + minute
  return minute - startMin
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

/**
 * 有效预约判定：已取消(4)、爽约(5)不占用技师/房间资源，不计入排序与计数
 */
const isEffective = (a: Appointment): boolean => a.status !== 4 && a.status !== 5

// 当前维度资源 ID 字段名（预约上对应列）
const resourceKey = computed<'technicianId' | 'roomId'>(() =>
  dimension.value === 'technician' ? 'technicianId' : 'roomId'
)

// 日历列：把当前维度可见资源展开为列（含当天预约、有效计数）
// 展示规则：自家技师(source=1)与启用房间始终显示；平台技师(source=2)为按需资源，仅当天有预约记录才显示列
// 排序规则：有有效预约的资源排前面（按当天最早有效预约时间正序），空闲资源排后面（保持列表顺序）
const allColumns = computed(() => {
  const dateStr = formatDate(currentDate.value)
  // 一体格式开始时间取日期部分比较；跨日预约归属开始日期所在日展示
  const dayApps = allAppointments.value.filter(a => a.startTime.slice(0, 10) === dateStr)
  const key = resourceKey.value
  const visibleResources = dimension.value === 'technician'
    ? allTechnicians.value.filter(
        t => t.source === 1 || dayApps.some(a => a.technicianId != null && String(a.technicianId) === String(t.id))
      )
    : allRooms.value
  const cols = visibleResources.map(res => {
    const resApps = dayApps
      .filter(a => a[key] != null && String(a[key]) === String(res.id))
      .sort((a, b) => a.startTime.localeCompare(b.startTime))
    const effective = resApps.filter(isEffective)
    return {
      key: `res-${res.id}`,
      label: res.name,
      resourceId: res.id,
      appointments: resApps,
      // 有有效预约时取其最早一条一体时间戳作为排序键，否则为空（排后面）
      firstEffectiveTime: effective.length > 0 ? effective[0].startTime : null,
      placeholder: false
    }
  })
  cols.sort((a, b) => {
    if (a.firstEffectiveTime && b.firstEffectiveTime) {
      return a.firstEffectiveTime.localeCompare(b.firstEffectiveTime)
    }
    if (a.firstEffectiveTime) return -1
    if (b.firstEffectiveTime) return 1
    return 0
  })
  // 门店无任何技师/房间时兜底
  if (cols.length === 0) {
    return [{
      key: 'empty',
      label: '暂无',
      appointments: [] as Appointment[],
      firstEffectiveTime: null as string | null,
      placeholder: false
    }]
  }
  return cols
})

// 组数
const groupCount = computed(() => Math.max(1, Math.ceil(allColumns.value.length / GROUP_SIZE)))

// 当前组展示的列：不足一组用占位列补齐，保持列宽稳定（与交互稿一致）
const currentGroupColumns = computed(() => {
  const start = groupIndex.value * GROUP_SIZE
  const cols = allColumns.value.slice(start, start + GROUP_SIZE)
  while (cols.length < GROUP_SIZE) {
    cols.push({
      key: `ph-${cols.length}`,
      label: '',
      appointments: [] as Appointment[],
      firstEffectiveTime: null as string | null,
      placeholder: true
    })
  }
  return cols
})

// 上一组 / 下一组
const handlePrevGroup = () => {
  if (groupIndex.value > 0) groupIndex.value--
}

const handleNextGroup = () => {
  if (groupIndex.value < groupCount.value - 1) groupIndex.value++
}

// 按日期获取预约（当日预约列表弹窗用；一体格式开始时间取日期部分比较）
const getAppointmentsByDate = (date: string): Appointment[] => {
  return allAppointments.value
    .filter(a => a.startTime.slice(0, 10) === date)
    .sort((a, b) => a.startTime.localeCompare(b.startTime))
}

// 日历列结构（周视图取格子预约与列头计数共用）
interface CalendarColumn {
  key: string
  label: string
  resourceId?: number | string
  appointments: Appointment[]
  firstEffectiveTime: string | null
  placeholder: boolean
}

// 获取某资源列在指定日期的预约（周视图格子用）
const getWeekCellAppointments = (col: CalendarColumn, date: string): Appointment[] => {
  if (col.placeholder || col.resourceId == null) return []
  const key = resourceKey.value
  return allAppointments.value
    .filter(a => a.startTime.slice(0, 10) === date && a[key] != null && String(a[key]) === String(col.resourceId))
    .sort((a, b) => a.startTime.localeCompare(b.startTime))
}

// 获取某资源列整周的预约记录数（周视图列头计数用，不限状态：已取消/爽约也算有预约）
const getWeekCount = (col: CalendarColumn): number => {
  if (col.placeholder || col.resourceId == null) return 0
  const key = resourceKey.value
  return allAppointments.value.filter(a =>
    a[key] != null && String(a[key]) === String(col.resourceId) &&
    weekDays.value.some(d => d.date === a.startTime.slice(0, 10))
  ).length
}

// 当前时间指示线位置（相对时间轴起点）
// 仅在查看日期为今天且当前时刻处于营业时段内时显示 NOW 线，查看其他日期不显示
// 营业时段判断使用独立的 bizStartMin/bizEndAbs（与时间轴是否因预约扩充无关）
const nowMarker = computed(() => {
  const now = new Date()
  if (formatDate(currentDate.value) !== formatDate(now)) {
    return { visible: false, top: 0 }
  }
  const minute = now.getHours() * 60 + now.getMinutes()
  const { bizStartMin, bizEndAbs } = timeScale.value
  // 跨天营业：当前时刻在 [开始, 24:00) ∪ (0:00, 结束] 内即处于营业时段
  const bizSpan = bizEndAbs > 1440 || bizEndAbs < bizStartMin
  const inRange = bizSpan
    ? minute >= bizStartMin || minute <= bizEndAbs - 1440
    : minute >= bizStartMin && minute <= bizEndAbs
  if (!inRange) {
    return { visible: false, top: 0 }
  }
  return { visible: true, top: (timeToOffset(minute) / 60) * HOUR_HEIGHT }
})

// 计算预约块的位置样式（与时间轴刻度严格对齐）
// 时间轴已覆盖当天全部预约，预约起止时刻均落在轴内，直接按偏移定位
const getAppointmentStyle = (item: Appointment) => {
  // startTime/endTime 均为一体格式 "yyyy-MM-ddTHH:mm:ss"，取时间部分 "HH:mm" 解析
  const [startHour, startMin] = item.startTime.slice(11, 16).split(':').map(Number)
  const endStr = item.endTime ? item.endTime.slice(11, 16) : item.startTime.slice(11, 16)
  const [endHour, endMin] = endStr.split(':').map(Number)
  const startOffset = timeToOffset(startHour * 60 + startMin)
  let endOffset = timeToOffset(endHour * 60 + endMin)
  // 跨天预约（结束时刻早于开始时刻，如 22:00 开始次日 01:00 结束）：时长需跨过自然日补足
  if (endOffset <= startOffset) endOffset += 1440
  return {
    top: `${(startOffset / 60) * HOUR_HEIGHT}px`,
    height: `${Math.max(0, ((endOffset - startOffset) / 60) * HOUR_HEIGHT - 2)}px`
  }
}

// 格式化预约时间段展示（一体格式时间戳 → "HH:mm - HH:mm"，日历块内紧凑展示）
// startTime 恒存在取时间部分，endTime 缺失时回退为开始时刻
const formatAppointmentTimeRange = (item: Appointment): string => {
  const start = item.startTime ? item.startTime.slice(11, 16) : '-'
  const end = item.endTime ? item.endTime.slice(11, 16) : start
  return `${start} - ${end}`
}

// 格式化详情弹窗预约时间范围（一体格式时间戳，支持跨日）
// - 同日：如 "08-25 22:00 - 23:00"（只显示一次日期）
// - 跨日：如 "08-25 22:00 - 08-26 01:00"（结束日期不同时两端均显示日期）
// 日期取 [5,10) 得到 MM-DD、时间取 [11,16) 得到 HH:mm，拼接避免 ISO 中间 T
const formatDetailTimeRange = (item: Appointment): string => {
  if (!item.startTime) return '-'
  const start = `${item.startTime.slice(5, 10)} ${item.startTime.slice(11, 16)}`
  if (!item.endTime) return start
  const startDay = item.startTime.slice(0, 10)
  const endDay = item.endTime.slice(0, 10)
  return startDay === endDay
    ? `${start} - ${item.endTime.slice(11, 16)}`
    : `${start} - ${item.endTime.slice(5, 10)} ${item.endTime.slice(11, 16)}`
}

// 格式化短日期时间（完成时间展示）
// 例："2026-08-25T21:30:00" -> "08-25 21:30"；空值返回 "-"
const formatShortDateTime = (iso?: string): string => {
  if (!iso) return '-'
  return `${iso.slice(5, 10)} ${iso.slice(11, 16)}`
}

// 状态文本
const getStatusText = (status: number): string => {
  const map: Record<number, string> = { 1: '已预约', 2: '已到店', 3: '已完成', 4: '已取消', 5: '爽约' }
  return map[status] || '未知'
}

// 状态标签类型
const getStatusTagType = (status: number): '' | 'success' | 'info' | 'warning' | 'danger' => {
  const map: Record<number, '' | 'success' | 'info' | 'warning' | 'danger'> = {
    1: '',
    2: 'success',
    3: 'info',
    4: 'info',
    5: 'danger'
  }
  // 用 ?? 而非 ||：空字符串（已预约默认蓝色）是合法值，不能被 falsy 判断替换成 info 灰色
  return map[status] ?? 'info'
}

// 选中日期的预约（当日预约列表弹窗）
const selectedDateAppointments = computed(() => {
  if (!selectedDate.value) return []
  return getAppointmentsByDate(selectedDate.value)
})

const selectedDateLabel = computed(() => selectedDate.value)

// 加载在岗技师（自家 + 平台）/ 启用房间完整列表，作为日视图的列来源
const loadResources = async () => {
  try {
    const [techRes, roomRes] = await Promise.all([
      getTechnicians({ status: 1, pageIndex: 1, pageSize: 1000 }),
      getRoomList({ status: 1, pageIndex: 1, pageSize: 1000 })
    ])
    allTechnicians.value = techRes.list
    allRooms.value = roomRes.list
  } catch {
    // 资源列表加载失败静默处理，日历列回退为空
  }
}

// 加载当前门店营业时间（优先取已授权门店列表，缺失时按当前门店ID查询详情）
// 获取失败或未设置时保持空字符串，时间刻度回退为 24 小时显示
const loadBusinessHours = async () => {
  try {
    const storeId = userStore.currentStoreId
    if (!storeId) return
    const authorized = userStore.authorizedStores.find(s => String(s.id) === String(storeId))
    const hoursText = authorized?.businessHours || (await getStore(storeId)).businessHours
    businessHours.value = hoursText || ''
  } catch {
    businessHours.value = ''
  }
}

// 加载数据（预约 + 资源列表，日期/维度变化后重置分组）
const loadData = async () => {
  loading.value = true
  groupIndex.value = 0
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
    const res = await getAppointments({ startTimeStart: startDate, startTimeEnd: endDate, pageSize: 100 })
    // 一体格式开始时间直接使用（YYYY-MM-DDTHH:mm:ss），日期部分在展示/过滤时按需截取
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
  loadResources()
  loadBusinessHours()
  loadData()
})
</script>

<style scoped>
/* ============ 全局 ============ */
.appointment-calendar {
  width: 100%;
}

/* 卡片：方案3 玻璃拟态 + 霓虹渐变描边 */
.card {
  position: relative;
  border-radius: 16px;
  padding: 1px;
  background: linear-gradient(140deg, rgba(120, 170, 255, 0.5), rgba(150, 80, 255, 0.28) 35%, rgba(6, 212, 228, 0.35));
  box-shadow:
    0 0 0 1px rgba(255, 255, 255, 0.04),
    0 14px 40px rgba(0, 0, 0, 0.5),
    0 0 40px rgba(90, 120, 255, 0.12);
}

.mb-20 {
  margin-bottom: 20px;
}

.ml-12 {
  margin-left: 12px;
}

/* 操作栏 */
.calendar-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 16px;
  border-radius: 15px;
  overflow: hidden;
  background: linear-gradient(180deg, rgba(18, 22, 38, 0.96), rgba(12, 14, 28, 0.97));
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
  font-size: 15px;
  font-weight: 700;
  color: var(--text-primary);
  min-width: 140px;
  text-align: center;
  text-shadow: 0 0 12px rgba(140, 180, 255, 0.4);
}

.toolbar-right {
  display: flex;
  gap: 8px;
}

/* ============ 分组切换条 ============ */
.groupbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 16px;
  border-bottom: 1px solid rgba(140, 180, 255, 0.12);
  background: rgba(140, 180, 255, 0.03);
}

.gb-info {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 12px;
  color: var(--text-tertiary);
}

.gb-title {
  color: var(--text-secondary);
}

.gb-total {
  color: var(--text-tertiary);
}

.gb-dots {
  display: flex;
  gap: 5px;
}

.gb-dot {
  width: 14px;
  height: 4px;
  border-radius: 2px;
  background: rgba(255, 255, 255, 0.12);
  cursor: pointer;
  transition: all 0.3s;
}

.gb-dot.on {
  background: linear-gradient(90deg, #7dd3fc, #a78bfa);
  box-shadow: 0 0 8px rgba(125, 211, 252, 0.6);
}

.gb-ctrl {
  display: flex;
  align-items: center;
  gap: 8px;
}

.gb-num {
  font-size: 12px;
  font-weight: 700;
  color: var(--text-primary);
  min-width: 46px;
  text-align: center;
  font-family: 'JetBrains Mono', monospace;
}

/* ============ 日历容器 ============ */
.calendar-container {
  min-height: 600px;
  overflow: auto;
  border-radius: 15px;
  overflow: hidden;
  background: linear-gradient(180deg, rgba(18, 22, 38, 0.96), rgba(12, 14, 28, 0.97));
}

/* ============ 日视图 ============ */
.day-view .time-grid {
  display: flex;
  position: relative;
}

.time-axis {
  width: 60px;
  flex-shrink: 0;
  position: relative;
  border-right: 1px solid rgba(140, 180, 255, 0.14);
  background: rgba(15, 17, 32, 0.5);
}

.time-slot {
  height: 60px;
  padding: 4px 8px 0 0;
  font-size: 11px;
  color: var(--text-tertiary);
  text-align: right;
  font-family: 'JetBrains Mono', monospace;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
}

/* 夜班跨天：次日凌晨刻度弱化区分 */
.time-slot.next-day {
  color: rgba(6, 212, 228, 0.55);
  font-style: italic;
}

/* 当前时间指示线：贯穿各列，标签定位在横线右侧 */
.now-line {
  position: absolute;
  left: 0;
  right: 0;
  height: 0;
  z-index: 3;
  border-top: 1px solid rgba(6, 212, 228, 0.75);
  box-shadow: 0 0 8px rgba(6, 212, 228, 0.55);
  pointer-events: none;
}

.now-tag {
  position: absolute;
  /* 贴右定位在横线最右端，基准为 .time-grid，避免遮挡时间轴刻度与预约块 */
  right: 6px;
  z-index: 4;
  pointer-events: none;
  font-size: 9px;
  color: #fff;
  background: linear-gradient(135deg, #06d4e4, #22d3ee);
  padding: 1px 7px;
  border-radius: 6px;
  font-weight: 800;
  box-shadow: 0 0 12px rgba(6, 212, 228, 0.7);
  letter-spacing: 0.5px;
}

.schedule-area {
  flex: 1;
  display: flex;
  overflow-x: hidden;
}

/* 日视图列头行 */
.day-header-row {
  display: flex;
  border-bottom: 1px solid rgba(140, 180, 255, 0.14);
  background: rgba(140, 180, 255, 0.03);
}

.day-header-row .time-corner {
  width: 60px;
  flex-shrink: 0;
}

.column-header {
  flex: 1;
  min-width: 0;
  padding: 10px 6px;
  text-align: center;
  border-left: 1px solid rgba(140, 180, 255, 0.08);
  position: relative;
}

.column-header::after {
  content: '';
  position: absolute;
  left: 15%;
  right: 15%;
  bottom: 0;
  height: 1px;
  background: linear-gradient(90deg, transparent, rgba(125, 211, 252, 0.5), transparent);
}

.column-header.placeholder {
  opacity: 0.4;
}

.col-title {
  font-size: 13px;
  font-weight: 700;
  color: var(--text-primary);
  text-shadow: 0 0 10px rgba(140, 180, 255, 0.35);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.col-dot {
  display: inline-block;
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: #44506e;
  margin-right: 5px;
}

.col-dot.on {
  background: #34d399;
  box-shadow: 0 0 9px #34d399;
}

.col-meta {
  font-size: 10px;
  color: var(--text-tertiary);
  margin-top: 3px;
}

.col-meta.busy {
  color: var(--text-secondary);
}

/* 列与单元格 */
.schedule-column {
  flex: 1;
  min-width: 0;
  border-left: 1px solid rgba(140, 180, 255, 0.08);
}

.schedule-column.placeholder {
  background: repeating-linear-gradient(
    45deg,
    rgba(255, 255, 255, 0.012),
    rgba(255, 255, 255, 0.012) 8px,
    transparent 8px,
    transparent 16px
  );
}

.column-body {
  position: relative;
}

.time-cell {
  height: 60px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
}

/* 预约块 */
.appointment-block {
  position: absolute;
  left: 3px;
  right: 3px;
  box-sizing: border-box;
  padding: 3px 7px;
  border-radius: 10px;
  font-size: 12px;
  cursor: pointer;
  overflow: hidden;
  border: 1px solid rgba(255, 255, 255, 0.18);
  backdrop-filter: blur(6px);
  -webkit-backdrop-filter: blur(6px);
  transition: transform 0.18s ease, box-shadow 0.18s ease, filter 0.18s ease;
}

.appointment-block:hover {
  transform: translateY(-2px) scale(1.01);
  filter: brightness(1.15);
}

.block-time {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 4px;
  font-size: 10px;
  font-family: 'JetBrains Mono', monospace;
  opacity: 0.85;
  font-weight: 700;
  line-height: 1.2;
}

.bt-range {
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* 预约状态标签：颜色与块状态一致，紧凑不换行 */
.block-status {
  flex-shrink: 0;
  font-family: var(--el-font-family);
  font-size: 9px;
  font-weight: 700;
  line-height: 1;
  padding: 1.5px 5px;
  border-radius: 4px;
  white-space: nowrap;
}

.block-status.st-1 {
  color: #e0f2fe;
  background: rgba(56, 189, 248, 0.3);
}

.block-status.st-2 {
  color: #d1fae5;
  background: rgba(52, 211, 153, 0.3);
}

.block-status.st-3 {
  color: #ede9fe;
  background: rgba(167, 139, 250, 0.3);
}

.block-status.st-4,
.block-status.st-5 {
  color: #fecdd3;
  background: rgba(251, 113, 133, 0.3);
}

.block-customer {
  font-size: 12.5px;
  font-weight: 700;
  margin-top: 1px;
  line-height: 1.2;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* 项目名 + 资源名同一行：项目名主位可省略，资源名等宽小字靠右（左竖线分隔） */
.block-info {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-top: 1px;
  opacity: 0.85;
  line-height: 1.2;
}

.bi-service {
  font-size: 11px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.bi-resource {
  flex-shrink: 0;
  font-size: 10px;
  font-family: 'JetBrains Mono', monospace;
  color: rgba(255, 255, 255, 0.62);
  padding-left: 6px;
  border-left: 1px solid rgba(255, 255, 255, 0.18);
  white-space: nowrap;
}

/* 状态色：霓虹发光 */
.appointment-block.status-1 {
  background: linear-gradient(135deg, rgba(56, 189, 248, 0.34), rgba(99, 102, 241, 0.22));
  border-color: rgba(125, 211, 252, 0.45);
  box-shadow: 0 0 16px rgba(56, 189, 248, 0.35), inset 0 0 12px rgba(125, 211, 252, 0.08);
}

.appointment-block.status-1 .block-customer {
  color: #e0f2fe;
  text-shadow: 0 0 8px rgba(125, 211, 252, 0.5);
}

.appointment-block.status-2 {
  background: linear-gradient(135deg, rgba(52, 211, 153, 0.32), rgba(16, 185, 129, 0.2));
  border-color: rgba(110, 231, 183, 0.45);
  box-shadow: 0 0 16px rgba(52, 211, 153, 0.32), inset 0 0 12px rgba(110, 231, 183, 0.08);
}

.appointment-block.status-2 .block-customer {
  color: #d1fae5;
  text-shadow: 0 0 8px rgba(110, 231, 183, 0.5);
}

.appointment-block.status-3 {
  background: linear-gradient(135deg, rgba(167, 139, 250, 0.26), rgba(139, 92, 246, 0.16));
  border-color: rgba(196, 181, 253, 0.4);
  box-shadow: 0 0 14px rgba(167, 139, 250, 0.25);
}

.appointment-block.status-3 .block-customer {
  color: #ede9fe;
}

.appointment-block.status-4,
.appointment-block.status-5 {
  background: linear-gradient(135deg, rgba(251, 113, 133, 0.16), rgba(244, 63, 94, 0.08));
  border-color: rgba(251, 113, 133, 0.3);
  box-shadow: 0 0 10px rgba(251, 113, 133, 0.12);
  opacity: 0.8;
}

.appointment-block.status-4 .block-customer,
.appointment-block.status-5 .block-customer {
  color: #fecdd3;
}

/* ============ 周视图：按技师/房间分列 × 周一~周日为行 ============ */
.week-grid {
  min-width: 640px;
}

/* 列头复用日视图 day-header-row / column-header 样式，不再重复定义 */

.time-corner {
  width: 60px;
  flex-shrink: 0;
}

.week-body {
  display: flex;
}

/* 左侧周刻度（周一~周日） */
.week-axis {
  width: 60px;
  flex-shrink: 0;
  border-right: 1px solid rgba(140, 180, 255, 0.14);
  background: rgba(15, 17, 32, 0.5);
}

.week-axis-row {
  height: 96px;
  padding: 8px 8px 0 0;
  text-align: right;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  box-sizing: border-box;
}

.week-axis-row.today {
  background: rgba(125, 211, 252, 0.06);
  box-shadow: inset 2px 0 0 rgba(125, 211, 252, 0.5);
}

.war-name {
  font-size: 12px;
  font-weight: 700;
  color: var(--text-secondary);
}

.week-axis-row.today .war-name {
  color: #7dd3fc;
  text-shadow: 0 0 10px rgba(125, 211, 252, 0.5);
}

.war-date {
  font-size: 11px;
  color: var(--text-tertiary);
  margin-top: 3px;
  font-family: 'JetBrains Mono', monospace;
}

/* 资源周列 */
.week-col {
  flex: 1;
  min-width: 0;
  border-left: 1px solid rgba(140, 180, 255, 0.08);
}

.week-col.placeholder {
  background: repeating-linear-gradient(
    45deg,
    rgba(255, 255, 255, 0.012),
    rgba(255, 255, 255, 0.012) 8px,
    transparent 8px,
    transparent 16px
  );
}

/* 周格子：该资源当天的预约 */
.week-cell {
  height: 96px;
  padding: 4px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  box-sizing: border-box;
  overflow-y: auto;
}

.week-cell.today {
  background: rgba(125, 211, 252, 0.05);
}

/* 预约条目：HH:mm + 客户名，状态色左边框标识 */
.week-cell-item {
  display: flex;
  align-items: center;
  gap: 4px;
  margin-bottom: 2px;
  padding: 1.5px 5px;
  border-radius: 4px;
  border-left: 2px solid rgba(255, 255, 255, 0.25);
  cursor: pointer;
  white-space: nowrap;
  overflow: hidden;
  transition: filter 0.15s ease, transform 0.15s ease;
}

.week-cell-item:hover {
  filter: brightness(1.2);
  transform: translateX(1px);
}

.wci-time {
  flex-shrink: 0;
  font-size: 10px;
  font-family: 'JetBrains Mono', monospace;
  color: rgba(255, 255, 255, 0.6);
  font-weight: 700;
}

.wci-customer {
  flex: 1;
  min-width: 0;
  font-size: 11px;
  font-weight: 700;
  overflow: hidden;
  text-overflow: ellipsis;
}

.week-cell-item.st-1 {
  background: rgba(56, 189, 248, 0.14);
  border-left-color: #38bdf8;
}

.week-cell-item.st-1 .wci-customer {
  color: #e0f2fe;
}

.week-cell-item.st-2 {
  background: rgba(52, 211, 153, 0.14);
  border-left-color: #34d399;
}

.week-cell-item.st-2 .wci-customer {
  color: #d1fae5;
}

.week-cell-item.st-3 {
  background: rgba(167, 139, 250, 0.13);
  border-left-color: #a78bfa;
}

.week-cell-item.st-3 .wci-customer {
  color: #ede9fe;
}

.week-cell-item.st-4,
.week-cell-item.st-5 {
  background: rgba(251, 113, 133, 0.11);
  border-left-color: #fb7185;
  opacity: 0.85;
}

.week-cell-item.st-4 .wci-customer,
.week-cell-item.st-5 .wci-customer {
  color: #fecdd3;
}

/* ============ 表格样式覆盖 ============ */
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
