<template>
  <div class="appointment-list">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="客户名称/手机号">
            <el-input
              v-model="searchForm.keyword"
              placeholder="姓名或手机号"
              clearable
              style="width: 170px"
            />
          </el-form-item>
          <el-form-item label="预约编号">
            <el-input
              v-model="searchForm.appointmentNo"
              placeholder="请输入预约编号"
              clearable
              style="width: 160px"
            />
          </el-form-item>
          <el-form-item label="状态">
            <el-select v-model="searchForm.status" placeholder="全部" clearable style="width: 120px">
              <el-option label="已预约" :value="1" />
              <el-option label="已到店" :value="2" />
              <el-option label="已完成" :value="3" />
              <el-option label="已取消" :value="4" />
              <el-option label="爽约" :value="5" />
            </el-select>
          </el-form-item>
          <el-form-item label="日期范围">
            <el-date-picker
              v-model="searchForm.dateRange"
              type="daterange"
              range-separator="至"
              start-placeholder="开始日期"
              end-placeholder="结束日期"
              value-format="YYYY-MM-DD"
              style="width: 240px"
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

    <!-- 操作栏 -->
    <div class="table-toolbar">
      <div class="toolbar-left">
        <el-button type="primary" @click="handleAdd" v-if="hasPermission('store:appointment:list:add')">
          <el-icon><Plus /></el-icon>
          新增预约
        </el-button>
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
        <el-table-column prop="appointmentNo" label="预约编号" width="150" />
        <el-table-column prop="customerName" label="客户名称" width="120" />
        <el-table-column prop="customerPhone" label="手机号" width="115" />
        <el-table-column prop="productName" label="服务项目" min-width="120" show-overflow-tooltip />
        <el-table-column label="技师" width="120">
          <template #default="{ row }">
            {{ row.technicianName || '-' }}
          </template>
        </el-table-column>
        <el-table-column label="技师来源" width="90">
          <template #default="{ row }">
            <el-tag
              v-if="row.technicianSource"
              :type="getTechnicianSourceTagType(row.technicianSource)"
              size="small"
              effect="plain"
            >
              {{ getTechnicianSourceText(row.technicianSource) }}
            </el-tag>
            <span v-else>-</span>
          </template>
        </el-table-column>
        <el-table-column label="预约时间" width="200">
          <template #default="{ row }">
            {{ formatTimeRange(row.startTime, row.endTime) }}
          </template>
        </el-table-column>
        <!-- 完成时间：状态列左侧，已完成预约显示完成时刻 -->
        <el-table-column label="完成时间" width="120">
          <template #default="{ row }">
            {{ formatShortDateTime(row.completeTime) }}
          </template>
        </el-table-column>
        <el-table-column label="状态" width="90">
          <template #default="{ row }">
            <el-tag :type="getStatusTagType(row.status)" size="small" effect="dark">
              {{ getStatusText(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="185" fixed="right">
          <template #default="{ row }">
            <div class="operation-buttons">
              <!-- 详情：操作列最前面，灰色与客户档案页一致 -->
              <el-button link type="info" size="small" @click="handleView(row)">
                <el-icon><View /></el-icon>
                详情
              </el-button>
              <!-- 编辑：独立入口，位于详情之后；仅已预约/已到店可编辑 -->
              <el-button
                link
                type="primary"
                size="small"
                :disabled="!canEdit(row.status)"
                @click="handleEdit(row)"
                v-if="hasPermission('store:appointment:list:edit')"
              >
                <el-icon><Edit /></el-icon>
                编辑
              </el-button>
              <!-- 操作入口：状态流转（按流转矩阵动态显示）+ 预约转订单 -->
              <el-dropdown
                v-if="getStatusActions(row.status).length > 0 || canConvertToOrder(row.status)"
                trigger="click"
                placement="bottom-end"
                popper-class="appointment-more-popper popper-glow-line"
                @command="(command: string) => handleCommand(command, row)"
              >
                <el-button link type="primary" size="small">
                  更多操作
                  <el-icon class="el-icon--right"><ArrowDown /></el-icon>
                </el-button>
                <template #dropdown>
                  <el-dropdown-menu>
                    <el-dropdown-item
                      v-for="action in getStatusActions(row.status)"
                      :key="action.value"
                      :command="`status:${action.value}`"
                      :class="{ 'is-danger': action.value === 4 }"
                    >
                      <el-icon :size="14"><component :is="getStatusActionIcon(action.value)" /></el-icon>
                      <span>{{ action.label }}</span>
                    </el-dropdown-item>
                    <el-dropdown-item
                      v-if="canConvertToOrder(row.status)"
                      divided
                      command="toOrder"
                      class="convert-order-item"
                    >
                      <el-icon :size="14"><ShoppingCart /></el-icon>
                      <span>预约转订单</span>
                    </el-dropdown-item>
                  </el-dropdown-menu>
                </template>
              </el-dropdown>
            </div>
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

    <!-- 新增预约弹窗 -->
    <el-dialog
      v-model="dialogVisible"
      :title="editId ? '编辑预约' : '新增预约'"
      width="640px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="100px"
      >
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="客户" prop="customerId">
              <el-select
                v-model="formData.customerId"
                filterable
                remote
                reserve-keyword
                :remote-method="loadCustomers"
                :loading="customerLoading"
                placeholder="搜索客户名称/手机号"
                style="width: 100%"
                @change="handleCustomerChange"
              >
                <el-option
                  v-for="c in customerOptions"
                  :key="c.id"
                  :label="c.name"
                  :value="c.id"
                >
                  <span>{{ c.name }}（{{ c.phone }}）</span>
                </el-option>
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="手机号">
              <el-input v-model="formData.phone" placeholder="选择客户后自动填充" disabled />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="开始时间" prop="startTime">
          <el-date-picker
            v-model="formData.startTime"
            type="datetime"
            placeholder="请选择开始时间"
            format="YYYY-MM-DD HH:mm"
            value-format="YYYY-MM-DDTHH:mm:ss"
            style="width: 100%"
            @blur="handleStartTimeBlur"
          />
        </el-form-item>
        <el-form-item label="结束时间" prop="endTime">
          <!-- 结束时间按服务时长自动计算（后端权威计算，前端预览）；支持跨日，日期会自动进位到次日 -->
          <el-date-picker
            v-model="formData.endTime"
            type="datetime"
            placeholder="自动计算"
            format="YYYY-MM-DD HH:mm"
            value-format="YYYY-MM-DDTHH:mm:ss"
            style="width: 100%"
            disabled
          />
        </el-form-item>
        <el-form-item label="服务项目" prop="productId">
          <el-select
            v-model="formData.productId"
            filterable
            placeholder="请选择服务项目"
            style="width: 100%"
            @change="handleServiceProductChange"
          >
            <el-option
              v-for="p in serviceProductOptions"
              :key="p.id"
              :label="p.name"
              :value="p.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="商家技师">
          <el-select
            v-model="formData.merchantTechnicianId"
            filterable
            clearable
            placeholder="请选择商家技师"
            popper-class="technician-select-popper"
            style="width: 100%"
            @change="handleMerchantTechnicianChange"
          >
            <el-option
              v-for="t in merchantTechnicianOptions"
              :key="t.id"
              :label="t.name"
              :value="t.id"
            >
              <div class="technician-option">
                <span :class="{ 'resource-occupied': technicianOccupancyMap.get(t.id)?.isOccupied }">
                  {{ t.name }}
                  <el-tag v-if="technicianOccupancyMap.get(t.id)?.isOccupied" type="danger" size="small" effect="plain">
                    占用
                  </el-tag>
                </span>
                <span class="technician-skill">{{ formatSkills(t.skillCategoryNames) }}</span>
              </div>
            </el-option>
          </el-select>
        </el-form-item>
        <el-form-item label="平台技师">
          <el-select
            v-model="formData.platformTechnicianId"
            filterable
            clearable
            placeholder="请选择平台技师"
            popper-class="technician-select-popper"
            style="width: 100%"
            @change="handlePlatformTechnicianChange"
          >
            <el-option
              v-for="t in platformTechnicianOptions"
              :key="t.id"
              :label="t.name"
              :value="t.id"
            >
              <div class="technician-option">
                <span>{{ t.name }}</span>
                <span class="technician-skill">{{ formatSkills(t.skillCategoryNames) }}</span>
              </div>
            </el-option>
          </el-select>
        </el-form-item>
        <el-form-item label="房间/床位">
          <el-select
            v-model="formData.roomId"
            placeholder="请选择房间/床位"
            clearable
            style="width: 100%"
            @change="handleRoomChange"
          >
            <el-option
              v-for="room in roomOptions"
              :key="room.id"
              :label="room.name"
              :value="room.id"
            >
              <span :class="{ 'resource-occupied': roomOccupancyMap.get(room.id)?.isOccupied }">
                {{ room.name }}（{{ room.roomType === 1 ? '房间' : '床位' }}）
                <el-tag v-if="roomOccupancyMap.get(room.id)?.isOccupied" type="danger" size="small" effect="plain">
                  占用
                </el-tag>
              </span>
            </el-option>
          </el-select>
        </el-form-item>
        <el-form-item label="设备">
          <el-select
            v-model="formData.equipmentId"
            placeholder="请选择设备"
            clearable
            filterable
            style="width: 100%"
          >
            <el-option
              v-for="eq in equipmentOptions"
              :key="eq.id"
              :label="eq.name"
              :value="eq.id"
            >
              <span :class="{ 'resource-occupied': equipmentOccupancyMap.get(eq.id)?.isOccupied }">
                {{ eq.name }}
                <el-tag v-if="equipmentOccupancyMap.get(eq.id)?.isOccupied" type="danger" size="small" effect="plain">
                  占用
                </el-tag>
              </span>
            </el-option>
          </el-select>
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="formData.remark" type="textarea" :rows="3" placeholder="请输入备注" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          确定
        </el-button>
      </template>
    </el-dialog>

    <!-- 预约详情弹窗 -->
    <el-dialog v-model="detailVisible" title="预约详情" width="500px">
      <el-descriptions :column="1" border label-width="88px" v-if="detailData">
        <el-descriptions-item label="预约编号">{{ detailData.appointmentNo }}</el-descriptions-item>
        <el-descriptions-item label="客户名称">{{ detailData.customerName }}</el-descriptions-item>
        <el-descriptions-item label="手机号">{{ detailData.customerPhone }}</el-descriptions-item>
        <el-descriptions-item label="服务项目">{{ detailData.productName }}</el-descriptions-item>
        <el-descriptions-item label="技师">
          {{ detailData.technicianName || '-' }}
        </el-descriptions-item>
        <el-descriptions-item label="技师来源">
          <el-tag
            v-if="detailData.technicianSource"
            :type="getTechnicianSourceTagType(detailData.technicianSource)"
            size="small"
            effect="plain"
          >
            {{ getTechnicianSourceText(detailData.technicianSource) }}
          </el-tag>
          <span v-else>-</span>
        </el-descriptions-item>
        <el-descriptions-item label="房间">{{ detailData.roomName || '-' }}</el-descriptions-item>
        <el-descriptions-item label="设备">{{ detailData.equipmentName || '-' }}</el-descriptions-item>
        <el-descriptions-item label="预约时间">{{ formatTimeRange(detailData.startTime, detailData.endTime) }}</el-descriptions-item>
        <el-descriptions-item label="完成时间">{{ formatShortDateTime(detailData.completeTime) }}</el-descriptions-item>
        <el-descriptions-item label="状态">
          <el-tag :type="getStatusTagType(detailData.status)" size="small" effect="dark">
            {{ getStatusText(detailData.status) }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="备注">{{ detailData.remark || '-' }}</el-descriptions-item>
      </el-descriptions>
      <template #footer>
        <el-button @click="detailVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, watch, computed } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus, View, Edit, ArrowDown, Position, Finished, CircleCloseFilled, ShoppingCart } from '@element-plus/icons-vue'
import {
  getAppointments,
  createAppointment,
  updateAppointment,
  getRooms
} from '@/api/appointment'
import { getCustomers } from '@/api/customer'
import type { Customer } from '@/api/customer/types'
import { getTechniciansAvailableByService } from '@/api/technician'
import type { Technician } from '@/api/technician/types'
import { getProducts } from '@/api/product'
import type { Product } from '@/api/product/types'
import { getAllEquipments, getAvailableEquipmentsByService } from '@/api/equipment'
import type { Equipment } from '@/api/equipment/types'
import { useSystemConfigStore } from '@/stores/systemConfig'
import { getStore } from '@/api/store'
import { useUserStore } from '@/stores/user'
import type { Appointment, AppointmentUpdate, TechnicianSource, RoomOption } from '@/api/appointment/types'
import { getResourceAvailability, type ResourceAvailabilityDto } from '@/api/resource'
import { toLocalDateTime } from '@/utils/time'

const systemConfigStore = useSystemConfigStore()
const router = useRouter()
const userStore = useUserStore()
const hasPermission = (permissionCode: string) => userStore.hasPermission(permissionCode)

// 编辑状态（editId 非空表示编辑模式，复用新增弹窗表单；editingRow 保存原始行用于回传时间戳字段）
const editId = ref<number | undefined>(undefined)
const editingRow = ref<Appointment | null>(null)

// 搜索表单
const searchForm = reactive({
  keyword: '',
  appointmentNo: '',
  status: undefined as number | undefined,
  dateRange: [] as string[]
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<Appointment[]>([])

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getAppointments({
      keyword: searchForm.keyword || undefined,
      appointmentNo: searchForm.appointmentNo || undefined,
      status: searchForm.status,
      startTimeStart: searchForm.dateRange?.[0],
      startTimeEnd: searchForm.dateRange?.[1],
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    tableData.value = res.list
    pagination.total = res.total
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
  searchForm.keyword = ''
  searchForm.appointmentNo = ''
  searchForm.status = undefined
  searchForm.dateRange = []
  handleSearch()
}

/**
 * 获取技师来源文本
 * @param source 技师来源
 * @returns 来源文本
 */
const getTechnicianSourceText = (source?: number): string => {
  if (source === 1) return '商家技师'
  if (source === 2) return '平台技师'
  return '-'
}

/**
 * 获取技师来源标签类型
 * @param source 技师来源
 * @returns el-tag 的 type
 */
const getTechnicianSourceTagType = (source?: number): '' | 'success' => {
  if (source === 1) return ''
  if (source === 2) return 'success'
  return ''
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

/**
 * 格式化预约时间段（一体格式时间戳，支持跨日）
 * - 同日：如 "08-25 22:00 - 23:00"（只显示一次日期）
 * - 跨日：如 "08-25 22:00 - 08-26 01:00"（结束日期不同时两端均显示日期）
 * 直接基于 ISO 字符串截取，避免 Date 解析的时区偏移
 * @param startTime 开始时间 ISO 字符串（YYYY-MM-DDTHH:mm:ss）
 * @param endTime 结束时间 ISO 字符串（YYYY-MM-DDTHH:mm:ss）
 * @returns 时间段文本，无开始时间返回 "-"
 */
const formatTimeRange = (startTime?: string, endTime?: string): string => {
  if (!startTime) return '-'
  // 一体格式 "YYYY-MM-DDTHH:mm:ss"，日期取 [5,10) 得到 MM-DD，时间取 [11,16) 得到 HH:mm，拼接避免中间 T
  const start = `${startTime.slice(5, 10)} ${startTime.slice(11, 16)}` // MM-DD HH:mm
  if (!endTime) return start
  const startDay = startTime.slice(0, 10)
  const endDay = endTime.slice(0, 10)
  // 同日只显示一次日期，跨日两端均显示（含日期避免混淆）
  return startDay === endDay
    ? `${start} - ${endTime.slice(11, 16)}`
    : `${start} - ${endTime.slice(5, 10)} ${endTime.slice(11, 16)}`
}

/**
 * 格式化短日期时间（完成时间列展示）
 * 例："2026-08-25T21:30:00" -> "08-25 21:30"；空值返回 "-"
 * @param iso ISO 时间戳字符串
 * @returns MM-DD HH:mm 或 "-"
 */
const formatShortDateTime = (iso?: string): string => {
  if (!iso) return '-'
  return `${iso.slice(5, 10)} ${iso.slice(11, 16)}`
}

// 新增弹窗
const dialogVisible = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()

const formData = reactive({
  customerId: undefined as number | undefined,
  customerName: '',
  phone: '',
  productId: undefined as number | undefined,
  // duration 仅用于前端预览 EndTime，不提交后端（后端根据 ProductId 查 ServiceProduct.Duration 权威计算）
  duration: undefined as number | undefined,
  technicianId: undefined as number | undefined,
  technicianName: '',
  // 商家技师/平台技师分别单独选择，互斥（选择其一清空另一）
  merchantTechnicianId: undefined as number | undefined,
  platformTechnicianId: undefined as number | undefined,
  technicianSource: 1 as TechnicianSource,
  roomId: undefined as number | undefined,
  roomName: '',
  equipmentId: undefined as number | undefined,
  // 开始/结束时间均为一体格式（YYYY-MM-DDTHH:mm:ss），结束时间按服务时长自动计算，支持跨日
  startTime: '',
  endTime: '',
  remark: ''
})

// 当前门店营业时间（"HH:mm - HH:mm"；未设置时为空字符串，不进行营业时间校验）
const businessHours = ref('')

// 加载当前门店营业时间（优先取已授权门店列表，缺失时按当前门店ID查询详情）
// 获取失败或未设置时保持空字符串，跳过营业时间校验
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

// 解析 "HH:mm" 为当日分钟数
const parseTime = (s: string): number => {
  const [h, m] = s.split(':').map(Number)
  return h * 60 + m
}

/**
 * 判断时刻是否处于营业时段内（支持夜班跨自然日，如 22:30-08:30）
 * @param minute 当日分钟数
 * @param startM 营业开始分钟
 * @param endM 营业结束分钟
 * @returns 是否在营业时段内
 */
const isInBusinessHours = (minute: number, startM: number, endM: number): boolean => {
  // 跨天：夜间段 [startM, 24:00) 与凌晨段 [0:00, endM]
  if (endM < startM) return minute >= startM || minute <= endM
  return minute >= startM && minute <= endM
}

/**
 * 获取当前表单时段超出营业时间的提示文本
 * 未设置营业时间、或开始与结束时间均在营业时段内时返回 null
 * @returns 超出营业时间的提示文本（合规时为 null）
 */
const getBusinessHoursWarning = (): string | null => {
  if (!businessHours.value || !formData.startTime) return null
  const parts = businessHours.value.split('-').map(s => s.trim())
  if (parts.length !== 2) return null
  const startM = parseTime(parts[0])
  const endM = parseTime(parts[1])
  // 开始/结束时间为一体格式（YYYY-MM-DDTHH:mm:ss），取第 11-15 位得到 HH:mm
  const [sh, sm] = formData.startTime.slice(11, 16).split(':').map(Number)
  const startInRange = isInBusinessHours(sh * 60 + sm, startM, endM)
  let endInRange = true
  if (formData.endTime) {
    const [eh, em] = formData.endTime.slice(11, 16).split(':').map(Number)
    endInRange = isInBusinessHours(eh * 60 + em, startM, endM)
  }
  if (startInRange && endInRange) return null
  const rangeLabel = `营业时间 ${parts[0]} - ${parts[1]}`
  if (!startInRange && !endInRange) return `开始与结束时间均超出${rangeLabel}`
  if (!startInRange) return `开始时间超出${rangeLabel}`
  return `结束时间超出${rangeLabel}`
}

// 开始时间输入框失去焦点后校验是否超出营业时间（结束时间为自动计算，随开始时间联动一并校验）
// 超出时以确认窗口提示，用户确认即关闭（不改变已选时间）；未设置营业时间时不触发
const handleStartTimeBlur = async () => {
  const warning = getBusinessHoursWarning()
  if (!warning) return
  try {
    await ElMessageBox.confirm(`请确认：${warning}`, '超出营业时间', {
      type: 'warning',
      confirmButtonText: '确定',
      cancelButtonText: '取消'
    })
  } catch {
    // 用户取消仅关闭窗口，不做额外处理
  }
}

/**
 * 校验开始时间不能早于当前时间（一体格式时间戳，含日期比较）
 * @param _rule 规则对象（未使用）
 * @param value 开始时间（YYYY-MM-DDTHH:mm:ss）
 * @param callback 校验回调
 */
const validateAppointmentTime = (_rule: unknown, value: string, callback: (error?: Error) => void) => {
  const start = value || formData.startTime
  if (!start) {
    callback()
    return
  }
  const startDate = new Date(start)
  if (Number.isNaN(startDate.getTime())) {
    callback()
    return
  }
  const now = new Date()
  // 忽略秒级误差（datetime picker 秒恒为 00，与当前时刻秒比较会误报），按分钟粒度比较
  const startMinute = new Date(
    startDate.getFullYear(), startDate.getMonth(), startDate.getDate(),
    startDate.getHours(), startDate.getMinutes()
  )
  const nowMinute = new Date(now.getFullYear(), now.getMonth(), now.getDate(), now.getHours(), now.getMinutes())
  if (startMinute.getTime() < nowMinute.getTime()) {
    callback(new Error('开始时间不能早于当前时间'))
    return
  }
  callback()
}

const formRules: FormRules = {
  customerId: [{ required: true, message: '请选择客户', trigger: 'change' }],
  productId: [{ required: true, message: '请选择服务项目', trigger: 'change' }],
  startTime: [
    { required: true, message: '请选择开始时间', trigger: 'change' },
    { validator: validateAppointmentTime, trigger: 'change' }
  ]
}

// 重置表单
const resetForm = () => {
  formData.customerId = undefined
  formData.customerName = ''
  formData.phone = ''
  formData.productId = undefined
  formData.duration = undefined
  formData.technicianId = undefined
  formData.technicianName = ''
  formData.merchantTechnicianId = undefined
  formData.platformTechnicianId = undefined
  formData.technicianSource = 1
  formData.roomId = undefined
  formData.roomName = ''
  formData.equipmentId = undefined
  formData.startTime = ''
  formData.endTime = ''
  formData.remark = ''
}

// 房间列表
const roomOptions = ref<RoomOption[]>([])

/**
 * 加载可用房间列表
 */
const loadRooms = async () => {
  try {
    roomOptions.value = await getRooms()
  } catch {
    roomOptions.value = []
  }
}

/**
 * 选择房间时同步房间名称
 * @param roomId 选中的房间ID
 */
const handleRoomChange = (roomId: number) => {
  const room = roomOptions.value.find(r => r.id === roomId)
  formData.roomName = room ? room.name : ''
}

// 设备列表
const equipmentOptions = ref<Equipment[]>([])

/**
 * 加载全部设备列表（用于下拉选择，仅显示状态正常的设备）
 */
const loadEquipments = async () => {
  try {
    equipmentOptions.value = await getAllEquipments(1)
  } catch {
    equipmentOptions.value = []
  }
}

// 客户列表（远程搜索）
const customerOptions = ref<Customer[]>([])
const customerLoading = ref(false)

/**
 * 远程搜索客户
 * @param keyword 搜索关键字（名称或手机号）
 */
const loadCustomers = async (keyword?: string) => {
  customerLoading.value = true
  try {
    const res = await getCustomers({ name: keyword, pageIndex: 1, pageSize: 50 })
    customerOptions.value = res.list
  } catch {
    customerOptions.value = []
  } finally {
    customerLoading.value = false
  }
}

/**
 * 选择客户后自动填充客户名称和手机号
 * @param customerId 选中的客户ID
 */
const handleCustomerChange = (customerId: number) => {
  const customer = customerOptions.value.find(c => c.id === customerId)
  if (customer) {
    formData.customerName = customer.name
    formData.phone = customer.phone
  } else {
    formData.customerName = ''
    formData.phone = ''
  }
}

// 商家技师列表（按服务项目适用技能过滤）
const merchantTechnicianOptions = ref<Technician[]>([])
// 平台技师列表（平台技师技能无法对齐当前门店技能分类，保持全部可选）
const platformTechnicianOptions = ref<Technician[]>([])

/**
 * 资源可用性状态（技师/房间/设备）
 * 用于预约创建/编辑时按当前日期+时段查询占用情况，列表中标红
 * key 规则：occupancyMap.technicians.get(technicianId) = 占用信息
 */
const resourceAvailability = ref<ResourceAvailabilityDto | null>(null)
const isLoadingAvailability = ref(false)

/**
 * 技师占用 Map（id -> IsOccupied）
 */
const technicianOccupancyMap = computed(() => {
  const map = new Map<number, { isOccupied: boolean; conflictInfo?: string }>()
  if (resourceAvailability.value) {
    for (const t of resourceAvailability.value.technicians) {
      map.set(t.id, { isOccupied: t.isOccupied, conflictInfo: t.conflictInfo })
    }
  }
  return map
})

/**
 * 房间占用 Map（id -> IsOccupied）
 */
const roomOccupancyMap = computed(() => {
  const map = new Map<number, { isOccupied: boolean; conflictInfo?: string }>()
  if (resourceAvailability.value) {
    for (const r of resourceAvailability.value.rooms) {
      map.set(r.id, { isOccupied: r.isOccupied, conflictInfo: r.conflictInfo })
    }
  }
  return map
})

/**
 * 设备占用 Map（id -> IsOccupied）
 */
const equipmentOccupancyMap = computed(() => {
  const map = new Map<number, { isOccupied: boolean; conflictInfo?: string }>()
  if (resourceAvailability.value) {
    for (const e of resourceAvailability.value.equipments) {
      map.set(e.id, { isOccupied: e.isOccupied, conflictInfo: e.conflictInfo })
    }
  }
  return map
})

/**
 * 加载当前预约时段内的资源占用情况
 * 触发时机：日期/开始时间/结束时间变更时
 */
const loadResourceAvailability = async () => {
  // 必须有开始时间 + 结束时间才计算占用区间（一体格式，跨日时结束时间可能为次日）
  if (!formData.startTime || !formData.endTime) {
    resourceAvailability.value = null
    return
  }
  isLoadingAvailability.value = true
  try {
    resourceAvailability.value = await getResourceAvailability(formData.startTime, formData.endTime)
  } catch {
    // 静默失败：占用查询失败不影响主流程
    resourceAvailability.value = null
  } finally {
    isLoadingAvailability.value = false
  }
}

// 监听开始/结束时间变化，实时刷新资源占用情况（技师/房间/设备列表标红）
// 若已选服务项目，同步重新加载房间/设备候选列表（按服务项目所需类型过滤）
watch(
  () => [formData.startTime, formData.endTime],
  () => {
    loadResourceAvailability()
    if (formData.productId) {
      handleServiceProductChange(formData.productId)
    }
  }
)

// 监听开始时间变化，根据服务项目时长自动重算结束时间（前端预览，后端会权威计算；跨日自动进位）
watch(
  () => formData.startTime,
  () => {
    recalcEndTime()
  }
)

// 开始时间变化时，重新校验"开始时间不能早于当前时间"
watch(
  () => formData.startTime,
  () => {
    if (dialogVisible.value && formRef.value) {
      formRef.value.validateField('startTime')
    }
  }
)

/**
 * 加载商家技师与平台技师列表（并行请求，按服务项目适用技能过滤）
 * - 已选服务项目：按服务项目适用技能过滤（树形展开匹配，选父级自动匹配子级）
 * - 未选服务项目：返回指定来源全部启用技师
 */
const loadTechnicians = async () => {
  try {
    // 服务项目技能过滤需按商品主档ID传参：后端以 masterId 反查租户内 ServiceProduct 子表，
    // 再按其技能关联过滤（serviceProductId 参数接收的是 ServiceProduct.Id，前端商品列表拿不到该 ID）
    const masterId = serviceProductOptions.value.find(p => p.id === formData.productId)?.masterId
    const [merchantList, platformList] = await Promise.all([
      getTechniciansAvailableByService(undefined, masterId, 1),
      getTechniciansAvailableByService(undefined, masterId, 2)
    ])
    merchantTechnicianOptions.value = merchantList
    platformTechnicianOptions.value = platformList
  } catch {
    merchantTechnicianOptions.value = []
    platformTechnicianOptions.value = []
  }
}

/**
 * 格式化技师技能展示文本
 * @param skills 技能分类名称列表
 * @returns 展示文本（无技能时提示未设置）
 */
const formatSkills = (skills?: string[]): string => {
  if (!skills || skills.length === 0) return '未设置技能'
  return `技能：${skills.join('、')}`
}

/**
 * 选择商家技师：同步技师ID/名称/来源，并清空平台技师保证互斥
 * @param technicianId 选中的商家技师ID（清空时为 undefined）
 */
const handleMerchantTechnicianChange = (technicianId: number | undefined) => {
  if (technicianId !== undefined) {
    formData.platformTechnicianId = undefined
    formData.technicianSource = 1
    formData.technicianId = technicianId
    formData.technicianName = merchantTechnicianOptions.value.find(t => t.id === technicianId)?.name ?? ''
  } else {
    formData.technicianId = undefined
    formData.technicianName = ''
  }
}

/**
 * 选择平台技师：同步技师ID/名称/来源，并清空商家技师保证互斥
 * @param technicianId 选中的平台技师ID（清空时为 undefined）
 */
const handlePlatformTechnicianChange = (technicianId: number | undefined) => {
  if (technicianId !== undefined) {
    formData.merchantTechnicianId = undefined
    formData.technicianSource = 2
    formData.technicianId = technicianId
    formData.technicianName = platformTechnicianOptions.value.find(t => t.id === technicianId)?.name ?? ''
  } else {
    formData.technicianId = undefined
    formData.technicianName = ''
  }
}

// 服务项目列表
const serviceProductOptions = ref<Product[]>([])

/**
 * 加载服务项目列表（仅商品类型为"服务项目"的上架商品）
 */
const loadServiceProducts = async () => {
  try {
    const res = await getProducts({ type: 2, status: 1, pageIndex: 1, pageSize: 200 })
    serviceProductOptions.value = res.list
  } catch {
    serviceProductOptions.value = []
  }
}

/**
 * 根据开始时间和服务时长重新计算结束时间（前端预览用）
 * 基于 Date 计算，跨日时自动进位到次日日期；后端会根据 ProductId 关联的 ServiceProduct.Duration 权威计算 EndTime，前端不可篡改
 */
const recalcEndTime = () => {
  if (!formData.startTime || !formData.duration) {
    formData.endTime = ''
    return
  }
  const start = new Date(formData.startTime)
  if (Number.isNaN(start.getTime())) {
    formData.endTime = ''
    return
  }
  const end = new Date(start.getTime() + formData.duration * 60000)
  const pad = (n: number) => String(n).padStart(2, '0')
  formData.endTime = `${end.getFullYear()}-${pad(end.getMonth() + 1)}-${pad(end.getDate())}T${pad(end.getHours())}:${pad(end.getMinutes())}:00`
}

/**
 * 选择服务项目后，根据服务项目所需资源类型重新加载房间/设备候选列表
 * - 房间：加载全部启用房间，前端按 ServiceProduct.RequiredRoomType 过滤
 * - 设备：按 ServiceProductEquipment 关联的设备类型过滤（不传时间参数，仅按类型过滤、不做时段冲突排除）
 * - 占用情况由 loadResourceAvailability 按当前时段统一标红（与技师下拉行为一致），保存时后端做权威冲突校验
 * @param productId 选中的服务项目商品ID
 */
const handleServiceProductChange = async (productId: number) => {
  const product = serviceProductOptions.value.find(p => p.id === productId)
  formData.duration = product?.duration
  recalcEndTime()

  // 若当前已选房间不在新的候选列表中，清空选择避免错配
  const previousRoomId = formData.roomId
  try {
    // 加载全部启用房间，前端按服务项目所需房间类型过滤（占用与否均可选，由占用标签标红提示）
    const allRooms = await getRooms()
    roomOptions.value = product?.requiredRoomType
      ? allRooms.filter(r => r.roomType === product.requiredRoomType)
      : allRooms
  } catch {
    // 接口失败时回退为加载全部启用房间
    roomOptions.value = await getRooms()
  }

  // 当前已选房间不在候选列表中则清空
  if (previousRoomId && !roomOptions.value.some(r => r.id === previousRoomId)) {
    formData.roomId = undefined
    formData.roomName = ''
  }

  // 设备处理：按服务项目 ServiceProductEquipment 关联的设备类型过滤（占用与否均可选，由占用标签标红提示）
  const previousEquipmentId = formData.equipmentId
  try {
    // 按商品主档ID传参：后端以 masterId 反查租户内 ServiceProduct 子表（serviceProductId 接收的是 ServiceProduct.Id，前端商品列表拿不到该 ID）
    const masterId = serviceProductOptions.value.find(p => p.id === productId)?.masterId
    if (masterId) {
      // 不传时间参数：后端仅按设备类型过滤、不做时段冲突排除，下拉始终展示全部匹配设备
      equipmentOptions.value = await getAvailableEquipmentsByService(undefined, masterId)
    } else {
      // 未选中服务项目：加载全部正常状态设备供用户选择
      equipmentOptions.value = await getAllEquipments(1)
    }
  } catch {
    // 接口失败时回退为加载全部正常状态设备
    equipmentOptions.value = await getAllEquipments(1)
  }

  // 当前已选设备不在候选列表中则清空
  if (previousEquipmentId && !equipmentOptions.value.some(e => e.id === previousEquipmentId)) {
    formData.equipmentId = undefined
  }

  // 技师过滤：按服务项目适用技能联动（树形展开匹配，选父级自动匹配子级），并清空已选技师
  const previousTechnicianId = formData.technicianId
  await loadTechnicians()
  const stillAvailable = merchantTechnicianOptions.value.some(t => t.id === previousTechnicianId)
    || platformTechnicianOptions.value.some(t => t.id === previousTechnicianId)
  if (previousTechnicianId && !stillAvailable) {
    formData.technicianId = undefined
    formData.technicianName = ''
    formData.merchantTechnicianId = undefined
    formData.platformTechnicianId = undefined
  }
}

// 新增
const handleAdd = () => {
  resetForm()
  // 切换到新增模式（清除编辑态）
  editId.value = undefined
  editingRow.value = null
  // 开始时间默认填入当前时刻（一体格式 YYYY-MM-DDTHH:mm:ss，结束时间由 recalcEndTime 按时长自动计算）
  formData.startTime = toLocalDateTime(new Date())
  loadRooms()
  loadEquipments()
  loadCustomers()
  loadTechnicians()
  loadServiceProducts()
  loadBusinessHours()
  dialogVisible.value = true
}

// 提交（新增 / 编辑共用）
const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (valid) {
      // 营业时间校验：超出时二次确认，用户确认后才保存（未设置营业时间则跳过）
      const warning = getBusinessHoursWarning()
      if (warning) {
        try {
          await ElMessageBox.confirm(`${warning}，是否仍要保存？`, '超出营业时间', {
            type: 'warning',
            confirmButtonText: '仍要保存',
            cancelButtonText: '取消'
          })
        } catch {
          return
        }
      }
      submitLoading.value = true
      try {
        const payload = {
          customerId: formData.customerId,
          customerName: formData.customerName,
          customerPhone: formData.phone,
          productId: formData.productId!,
          technicianId: formData.technicianId || undefined,
          roomId: formData.roomId || undefined,
          equipmentId: formData.equipmentId || undefined,
          // 一体格式开始时间（后端自动按 ServiceProduct.Duration 计算 EndTime，前端不传 EndTime）
          startTime: formData.startTime,
          remark: formData.remark || undefined
        }
        if (editId.value && editingRow.value) {
          // 编辑模式：保持原状态不变（状态流转走"更多"下拉），
          // 原样回传时间戳字段，避免后端 UpdateAsync 将其清空
          await updateAppointment({
            ...payload,
            id: editId.value,
            status: editingRow.value.status,
            confirmTime: editingRow.value.confirmTime ?? null,
            arrivalTime: editingRow.value.arrivalTime ?? null,
            completeTime: editingRow.value.completeTime ?? null
          })
          ElMessage.success('编辑预约成功')
        } else {
          await createAppointment(payload)
          ElMessage.success('创建预约成功')
        }
        dialogVisible.value = false
        loadData()
      } catch (error) {
        ElMessage.error((error as Error).message || '保存失败')
      } finally {
        submitLoading.value = false
      }
    }
  })
}

/**
 * 获取当前状态允许的状态操作项（与后端 AppointmentStatusTransition 流转矩阵一致）
 * - 已预约(1)：到店 / 完成 / 取消
 * - 已到店(2)：完成 / 取消
 * - 终态（已完成/已取消/爽约）：无可操作项
 * @param status 当前状态
 * @returns 允许的状态操作项列表
 */
/**
 * 预约是否可编辑（仅已预约/已到店可编辑，终态不可编辑）
 * @param status 当前状态
 * @returns 可编辑返回 true
 */
const canEdit = (status: number): boolean => status === 1 || status === 2

const getStatusActions = (status: number): Array<{ value: number; label: string }> => {
  const actions: Array<{ value: number; label: string }> = []
  if (status === 1) {
    actions.push({ value: 2, label: '到店' })
    actions.push({ value: 3, label: '完成' })
    actions.push({ value: 4, label: '取消' })
  } else if (status === 2) {
    actions.push({ value: 3, label: '完成' })
    actions.push({ value: 4, label: '取消' })
  }
  return actions
}

/**
 * 状态操作对应的图标（用于下拉菜单项视觉标识）
 * - 到店：定位箭头；完成：对勾；取消：圆环叉
 * @param status 目标状态（2-到店，3-完成，4-取消）
 * @returns 图标组件
 */
const getStatusActionIcon = (status: number) => {
  if (status === 2) return Position
  if (status === 3) return Finished
  return CircleCloseFilled
}

/**
 * 预约是否可转订单（已预约/已到店可转，终态不可转）
 * @param status 当前状态
 * @returns 可转返回 true
 */
const canConvertToOrder = (status: number): boolean => status === 1 || status === 2

/**
 * 构造状态流转的完整更新 DTO
 * 后端 UpdateAsync 仅接收 AppointmentUpdateDto（含 CustomerId>0 必填校验），
 * 且会原样写回 ConfirmTime/ArrivalTime/CompleteTime，故状态操作也必须提交完整字段
 * @param row 预约行数据
 * @param status 目标状态
 * @returns 完整更新 DTO
 */
const buildStatusUpdatePayload = (row: Appointment, status: number): AppointmentUpdate => ({
  id: row.id,
  customerId: row.customerId,
  customerName: row.customerName,
  customerPhone: row.customerPhone,
  // 一体格式开始时间原样回传（后端按 ServiceProduct.Duration 重新计算 EndTime）
  startTime: row.startTime,
  productId: row.productId,
  status,
  technicianId: row.technicianId,
  roomId: row.roomId,
  equipmentId: row.equipmentId,
  remark: row.remark,
  confirmTime: row.confirmTime ?? null,
  arrivalTime: row.arrivalTime ?? null,
  completeTime: row.completeTime ?? null
})

/**
 * 统一处理预约状态流转（到店/完成/取消）
 * @param row 预约行数据
 * @param status 目标状态（2-到店，3-完成，4-取消）
 */
const handleStatusChange = async (row: Appointment, status: number) => {
  const statusText = getStatusText(status)
  let confirmText = ''
  let type: 'success' | 'warning' = 'warning'
  if (status === 2) {
    confirmText = `确认客户 "${row.customerName}" 已到店吗？`
    type = 'success'
  } else if (status === 3) {
    confirmText = `确认客户 "${row.customerName}" 的服务已完成吗？`
    type = 'success'
  } else {
    confirmText = `确定要取消 "${row.customerName}" 的预约吗？`
  }
  try {
    await ElMessageBox.confirm(confirmText, `${statusText}确认`, {
      type,
      confirmButtonText: '确定',
      cancelButtonText: '取消'
    })
    await updateAppointment(buildStatusUpdatePayload(row, status))
    const successMessages: Record<number, string> = {
      2: '已标记到店',
      3: '已标记完成',
      4: '已取消预约'
    }
    ElMessage.success(successMessages[status] || '操作成功')
    loadData()
  } catch (error) {
    if (error !== 'cancel') ElMessage.error('操作失败')
  }
}

/**
 * 预约转订单：当前页面弹出确认框，确认后跳转快速开单页自动加入购物车
 * POS 页面读取 appointmentId 参数后直接将该预约的商品加入购物车（不再弹窗）
 * @param row 预约行数据
 */
const handleAppointmentToOrder = async (row: Appointment) => {
  try {
    await ElMessageBox.confirm(
      `确认将客户 "${row.customerName}" 的预约转为订单吗？`,
      '预约转订单',
      {
        type: 'info',
        confirmButtonText: '确定',
        cancelButtonText: '取消'
      }
    )
    router.push({ path: '/store/pos/quick', query: { appointmentId: row.id } })
  } catch {
    // 用户取消确认，不跳转
  }
}

/**
 * 处理"更多"下拉菜单命令
 * @param command 命令标识：status:{目标状态} 或 toOrder
 * @param row 预约行数据
 */
const handleCommand = (command: string, row: Appointment) => {
  if (command === 'toOrder') {
    handleAppointmentToOrder(row)
    return
  }
  if (command.startsWith('status:')) {
    handleStatusChange(row, Number(command.split(':')[1]))
  }
}

/**
 * 打开编辑弹窗，回填预约数据（复用新增弹窗表单）
 * 提交时走 updateAppointment，保持原状态不变；状态流转通过"更多"下拉完成
 * @param row 预约行数据
 */
const handleEdit = async (row: Appointment) => {
  editingRow.value = row
  editId.value = row.id
  formData.customerId = row.customerId
  formData.customerName = row.customerName
  formData.phone = row.customerPhone
  formData.productId = row.productId
  // 技师回填（按来源区分商家/平台技师选择框，保持互斥）
  formData.technicianId = row.technicianId
  formData.technicianName = row.technicianName || ''
  formData.technicianSource = row.technicianSource ?? 1
  if (row.technicianSource === 2) {
    formData.platformTechnicianId = row.technicianId
    formData.merchantTechnicianId = undefined
  } else {
    formData.merchantTechnicianId = row.technicianId
    formData.platformTechnicianId = undefined
  }
  formData.roomId = row.roomId
  formData.roomName = ''
  formData.equipmentId = row.equipmentId
  // 一体格式开始时间直接回填（datetime picker 完整解析 YYYY-MM-DDTHH:mm:ss）
  formData.startTime = row.startTime
  formData.remark = row.remark || ''
  // 并行加载下拉数据（服务项目列表用于回填时长，技师按服务项目技能过滤）
  await Promise.all([loadRooms(), loadEquipments(), loadServiceProducts(), loadTechnicians(), loadBusinessHours()])
  const product = serviceProductOptions.value.find(p => p.id === row.productId)
  formData.duration = product?.duration
  // EndTime 在 duration 就绪后回填：startTime 的 watch 会触发 recalcEndTime，
  // 若 duration 尚未加载会将其清空（竞态），放在此处可避免回填值被覆盖
  // EndTime 为 ISO 8601 完整时间戳（yyyy-MM-ddTHH:mm:ss），后端权威计算，直接回填展示
  formData.endTime = row.endTime ?? ''
  // 客户下拉回显：当前客户不在候选列表中时插入首项（远程搜索可能不包含该客户）
  if (!customerOptions.value.some(c => c.id === row.customerId)) {
    customerOptions.value.unshift({
      id: row.customerId,
      name: row.customerName,
      phone: row.customerPhone,
      gender: 0,
      totalPoints: 0,
      balance: 0,
      totalConsume: 0,
      authorizationStatus: 0,
      createdAt: row.createdAt
    } as Customer)
  }
  dialogVisible.value = true
}

// 详情弹窗
const detailVisible = ref(false)
const detailData = ref<Appointment | null>(null)

const handleView = (row: Appointment) => {
  detailData.value = row
  detailVisible.value = true
}

onMounted(async () => {
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  pagination.pageSize = systemConfigStore.defaultPageSize
  loadData()
})
</script>

<style scoped>
.appointment-list {
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

/* 各列内容单行显示不换行（Element Plus 默认 white-space: normal 会折行），配合列宽控制 */
:deep(.el-table .cell) {
  white-space: nowrap;
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
}

.toolbar-right {
  display: flex;
  gap: 8px;
}

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}

/* 资源占用标红（技师/房间/设备选择列表中的已占用条目） */
.resource-occupied {
  color: var(--el-color-danger, #f56c6c);
  font-weight: 600;
}

/* 技师下拉选项：名称 + 技能展示（选择技师时展示技能） */
.technician-option {
  display: flex;
  flex-direction: column;
  line-height: 1.4;
}

.technician-skill {
  font-size: 12px;
  color: var(--text-tertiary);
}

/* 操作列按钮容器：保持单行不换行 */
.operation-buttons {
  display: flex;
  align-items: center;
  gap: 2px;
  white-space: nowrap;
}
</style>

<!-- 收纳下拉菜单（popper-class 定位，teleport 到 body 需非 scoped 样式） -->
<style>
/* 技师下拉选项（teleport 到 body 需非 scoped 样式）
 * 选项为两行自定义内容（名称+占用标签 / 技能），默认固定 34px 行高 + overflow:hidden 会裁切底部文字
 * 需让 item 高度自适应内容，避免占用标签撑高后底部文字显示不全 */
.technician-select-popper .el-select-dropdown__item {
  height: auto;
  min-height: 34px;
  line-height: 1.4;
  padding-top: 6px;
  padding-bottom: 6px;
  white-space: normal;
}

/* 预约操作收纳下拉菜单：深色层次 + 发光边框 + 圆角 */
.appointment-more-popper {
  padding: 4px !important;
  border: 1px solid var(--border-secondary) !important;
  border-radius: var(--radius-md) !important;
  background: var(--bg-elevated) !important;
  box-shadow: var(--shadow-lg), 0 0 0 1px var(--bg-glow) !important;
}

/* popper 箭头：发光横线由公共样式 src/styles/popper-glow-line.css 提供 */
/* 通过 popper-class 追加 "popper-glow-line" 工具类启用 */

.appointment-more-popper .el-dropdown-menu {
  background: transparent;
  padding: 0;
}

/* 菜单项：图标 + 文字，圆角悬浮，hover 青色 */
.appointment-more-popper .el-dropdown-menu__item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 12px;
  margin: 2px 0;
  border-radius: var(--radius-sm);
  color: var(--text-secondary);
  font-size: 13px;
  line-height: 1.4;
  position: relative;
  transition: color 0.2s ease, background-color 0.2s ease;
}

/* hover 左侧青色指示条滑入 */
.appointment-more-popper .el-dropdown-menu__item::before {
  content: '';
  position: absolute;
  left: 0;
  top: 50%;
  transform: translateY(-50%) scaleY(0);
  width: 2px;
  height: 14px;
  border-radius: 2px;
  background: var(--primary);
  box-shadow: 0 0 8px var(--primary-glow);
  transition: transform 0.2s ease;
}

.appointment-more-popper .el-dropdown-menu__item:hover::before {
  transform: translateY(-50%) scaleY(1);
}

.appointment-more-popper .el-dropdown-menu__item:hover {
  color: var(--primary);
  background: var(--bg-hover);
}

.appointment-more-popper .el-dropdown-menu__item .el-icon {
  color: var(--text-tertiary);
  transition: color 0.2s ease;
}

.appointment-more-popper .el-dropdown-menu__item:hover .el-icon {
  color: var(--primary);
}

/* 危险操作（取消）：红色标识 */
.appointment-more-popper .el-dropdown-menu__item.is-danger {
  color: var(--danger);
}

.appointment-more-popper .el-dropdown-menu__item.is-danger .el-icon {
  color: var(--danger);
}

.appointment-more-popper .el-dropdown-menu__item.is-danger::before {
  background: var(--danger);
  box-shadow: 0 0 8px var(--shadow-glow-danger);
}

.appointment-more-popper .el-dropdown-menu__item.is-danger:hover {
  color: #ff8080;
  background: rgba(255, 87, 87, 0.08);
}

.appointment-more-popper .el-dropdown-menu__item.is-danger:hover .el-icon {
  color: #ff8080;
}

/* 预约转订单：青色强调 */
.appointment-more-popper .el-dropdown-menu__item.convert-order-item {
  color: var(--primary);
}

.appointment-more-popper .el-dropdown-menu__item.convert-order-item .el-icon {
  color: var(--primary);
}

.appointment-more-popper .el-dropdown-menu__item.convert-order-item:hover {
  background: var(--bg-glow);
}

/* 分隔线（divided 默认样式）：适配深色主题 */
.appointment-more-popper .el-dropdown-menu__item--divided {
  border-top: 1px solid var(--border-primary);
  margin-top: 4px;
  padding-top: 6px;
}

.appointment-more-popper .el-dropdown-menu__item--divided::before {
  display: none;
}
</style>
