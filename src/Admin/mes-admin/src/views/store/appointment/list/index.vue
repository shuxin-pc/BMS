<template>
  <div class="appointment-list">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="客户名称">
            <el-input
              v-model="searchForm.customerName"
              placeholder="请输入客户名称"
              clearable
              style="width: 160px"
            />
          </el-form-item>
          <el-form-item label="手机号">
            <el-input
              v-model="searchForm.phone"
              placeholder="请输入手机号"
              clearable
              style="width: 150px"
            />
          </el-form-item>
          <el-form-item label="状态">
            <el-select v-model="searchForm.status" placeholder="全部" clearable style="width: 120px">
              <el-option label="待确认" :value="1" />
              <el-option label="已预约" :value="2" />
              <el-option label="已到店" :value="3" />
              <el-option label="已完成" :value="4" />
              <el-option label="已取消" :value="5" />
              <el-option label="爽约" :value="6" />
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
        <el-button type="primary" @click="handleAdd">
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
        <el-table-column prop="customerName" label="客户名称" width="100" />
        <el-table-column prop="customerPhone" label="手机号" width="130" />
        <el-table-column prop="productName" label="服务项目" width="120" show-overflow-tooltip />
        <el-table-column label="技师" width="90">
          <template #default="{ row }">
            <!-- TODO: 后端不返回 technicianName，需关联技师信息 -->
            {{ row.technicianId ? `ID:${row.technicianId}` : '-' }}
          </template>
        </el-table-column>
        <el-table-column label="技师来源" width="100">
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
        <el-table-column prop="appointmentDate" label="预约日期" width="120" />
        <el-table-column label="时间段" width="130">
          <template #default="{ row }">
            {{ row.appointmentTime }} - {{ row.endTime }}
          </template>
        </el-table-column>
        <el-table-column label="状态" width="90">
          <template #default="{ row }">
            <el-tag :type="getStatusTagType(row.status)" size="small" effect="dark">
              {{ getStatusText(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="remark" label="备注" min-width="120" show-overflow-tooltip />
        <el-table-column label="操作" width="220" fixed="right">
          <template #default="{ row }">
            <el-button
              v-if="row.status === 1"
              link
              type="success"
              size="small"
              @click="handleConfirm(row)"
            >
              <el-icon><Check /></el-icon>
              确认
            </el-button>
            <el-button
              v-if="row.status === 1 || row.status === 2"
              link
              type="primary"
              size="small"
              @click="handleArrive(row)"
            >
              <el-icon><CircleCheck /></el-icon>
              到店
            </el-button>
            <el-button
              v-if="row.status === 1 || row.status === 2"
              link
              type="danger"
              size="small"
              @click="handleCancel(row)"
            >
              <el-icon><Close /></el-icon>
              取消
            </el-button>
            <el-button
              link
              type="info"
              size="small"
              @click="handleView(row)"
            >
              <el-icon><View /></el-icon>
              详情
            </el-button>
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
      title="新增预约"
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
            style="width: 100%"
            @change="handleMerchantTechnicianChange"
          >
            <el-option
              v-for="t in merchantTechnicianOptions"
              :key="t.id"
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
            style="width: 100%"
            @change="handlePlatformTechnicianChange"
          >
            <el-option
              v-for="t in platformTechnicianOptions"
              :key="t.id"
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
            placeholder="请选择设备（可选）"
            clearable
            filterable
            style="width: 100%"
          >
            <el-option
              v-for="eq in equipmentOptions"
              :key="eq.id"
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
        <el-form-item label="预约日期" prop="appointmentDate">
          <el-date-picker
            v-model="formData.appointmentDate"
            type="date"
            placeholder="请选择日期"
            value-format="YYYY-MM-DD"
            style="width: 100%"
          />
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="开始时间" prop="startTime">
              <el-time-picker
                v-model="formData.startTime"
                format="HH:mm"
                value-format="HH:mm"
                placeholder="开始时间"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="结束时间">
              <el-time-picker
                v-model="formData.endTime"
                format="HH:mm"
                value-format="HH:mm"
                placeholder="自动计算"
                disabled
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>
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
      <el-descriptions :column="1" border v-if="detailData">
        <el-descriptions-item label="预约编号">{{ detailData.appointmentNo }}</el-descriptions-item>
        <el-descriptions-item label="客户名称">{{ detailData.customerName }}</el-descriptions-item>
        <el-descriptions-item label="手机号">{{ detailData.customerPhone }}</el-descriptions-item>
        <el-descriptions-item label="服务项目">{{ detailData.productName }}</el-descriptions-item>
        <el-descriptions-item label="技师">
          <!-- TODO: 后端不返回 technicianName，需关联技师信息 -->
          {{ detailData.technicianId ? `技师ID: ${detailData.technicianId}` : '-' }}
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
        <el-descriptions-item label="房间">
          <!-- TODO: 后端不返回 roomName，需关联房间信息 -->
          {{ detailData.roomId ? `房间ID: ${detailData.roomId}` : '-' }}
        </el-descriptions-item>
        <el-descriptions-item label="设备">
          {{ detailData.equipmentId ? `设备ID: ${detailData.equipmentId}` : '-' }}
        </el-descriptions-item>
        <el-descriptions-item label="预约日期">{{ detailData.appointmentDate }}</el-descriptions-item>
        <el-descriptions-item label="时间段">{{ detailData.appointmentTime }} - {{ detailData.endTime }}</el-descriptions-item>
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
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus, Check, CircleCheck, Close, View } from '@element-plus/icons-vue'
import {
  getAppointments,
  createAppointment,
  updateAppointmentStatus,
  getRooms,
  getAvailableRoomsByServiceProduct
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
import type { Appointment, TechnicianSource, RoomOption } from '@/api/appointment/types'
import { getResourceAvailability, type ResourceAvailabilityDto } from '@/api/resource'

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  customerName: '',
  phone: '',
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
      customerName: searchForm.customerName || undefined,
      phone: searchForm.phone || undefined,
      status: searchForm.status,
      appointmentDateStart: searchForm.dateRange?.[0],
      appointmentDateEnd: searchForm.dateRange?.[1],
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    tableData.value = res.list
    pagination.total = res.total
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
  searchForm.customerName = ''
  searchForm.phone = ''
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
  appointmentDate: '',
  startTime: '',
  endTime: '',
  remark: ''
})

/**
 * 格式化当天日期（YYYY-MM-DD）
 * @returns 当天日期字符串
 */
const formatToday = (): string => {
  const now = new Date()
  const y = now.getFullYear()
  const m = String(now.getMonth() + 1).padStart(2, '0')
  const d = String(now.getDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}

/**
 * 校验预约日期 + 开始时间不能早于当前时间
 * - 预约日期早于今天：不合法
 * - 预约日期为今天但开始时间早于当前时刻：不合法
 * @param _rule 规则对象（未使用）
 * @param _value 当前字段值（未使用，直接读取 formData 组合校验）
 * @param callback 校验回调
 */
const validateAppointmentTime = (_rule: unknown, _value: unknown, callback: (error?: Error) => void) => {
  if (!formData.appointmentDate || !formData.startTime) {
    callback()
    return
  }
  const today = formatToday()
  if (formData.appointmentDate < today) {
    callback(new Error('预约日期不能早于今天'))
    return
  }
  if (formData.appointmentDate === today) {
    const now = new Date()
    const nowMinutes = now.getHours() * 60 + now.getMinutes()
    const [sh, sm] = formData.startTime.split(':').map(Number)
    if (!isNaN(sh) && !isNaN(sm) && sh * 60 + sm < nowMinutes) {
      callback(new Error('开始时间不能早于当前时间'))
      return
    }
  }
  callback()
}

const formRules: FormRules = {
  customerId: [{ required: true, message: '请选择客户', trigger: 'change' }],
  productId: [{ required: true, message: '请选择服务项目', trigger: 'change' }],
  appointmentDate: [
    { required: true, message: '请选择预约日期', trigger: 'change' },
    { validator: validateAppointmentTime, trigger: 'change' }
  ],
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
  formData.appointmentDate = ''
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
 * 加载全部设备列表（用于下拉选择）
 */
const loadEquipments = async () => {
  try {
    equipmentOptions.value = await getAllEquipments()
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
  // 必须有日期 + 开始时间 + 结束时间才计算占用区间
  if (!formData.appointmentDate || !formData.startTime || !formData.endTime) {
    resourceAvailability.value = null
    return
  }
  isLoadingAvailability.value = true
  try {
    const start = `${formData.appointmentDate} ${formData.startTime}:00`
    const end = `${formData.appointmentDate} ${formData.endTime}:00`
    resourceAvailability.value = await getResourceAvailability(start, end)
  } catch (e) {
    // 静默失败：占用查询失败不影响主流程
    resourceAvailability.value = null
  } finally {
    isLoadingAvailability.value = false
  }
}

// 监听日期/时间变化，实时刷新资源占用情况（技师/房间/设备列表标红）
// 若已选服务项目，同步重新加载可用房间（按 RequiredRoomType + 时段冲突过滤）
watch(
  () => [formData.appointmentDate, formData.startTime, formData.endTime],
  () => {
    loadResourceAvailability()
    if (formData.productId) {
      handleServiceProductChange(formData.productId)
    }
  }
)

// 监听开始时间变化，根据服务项目时长自动重算结束时间（前端预览，后端会权威计算）
watch(
  () => formData.startTime,
  () => {
    recalcEndTime()
  }
)

// 预约日期或开始时间变化时，重新校验"预约时间不能早于当前时间"
watch(
  () => [formData.appointmentDate, formData.startTime],
  () => {
    if (dialogVisible.value && formRef.value) {
      formRef.value.validateField(['appointmentDate', 'startTime'])
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
    const [merchantList, platformList] = await Promise.all([
      getTechniciansAvailableByService(formData.productId, undefined, 1),
      getTechniciansAvailableByService(formData.productId, undefined, 2)
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
 * 后端会根据 ProductId 关联的 ServiceProduct.Duration 权威计算 EndTime，前端不可篡改
 */
const recalcEndTime = () => {
  if (!formData.startTime || !formData.duration) {
    formData.endTime = ''
    return
  }
  const [h, m] = formData.startTime.split(':').map(Number)
  if (isNaN(h) || isNaN(m)) {
    formData.endTime = ''
    return
  }
  const totalMinutes = h * 60 + m + formData.duration
  const eh = Math.floor(totalMinutes / 60) % 24
  const em = totalMinutes % 60
  formData.endTime = `${String(eh).padStart(2, '0')}:${String(em).padStart(2, '0')}`
}

/**
 * 选择服务项目后，根据服务项目所需房间类型与当前时段重新加载可用房间
 * - 时段已确定：调用后端接口按 RequiredRoomType + 时段冲突过滤
 * - 时段未确定：加载全部启用房间，前端本地按 RequiredRoomType 过滤
 * @param productId 选中的服务项目商品ID
 */
const handleServiceProductChange = async (productId: number) => {
  const product = serviceProductOptions.value.find(p => p.id === productId)
  formData.duration = product?.duration
  recalcEndTime()

  // 若当前已选房间不在新的可用列表中，清空选择避免错配
  const previousRoomId = formData.roomId

  try {
    if (formData.appointmentDate && formData.startTime && formData.endTime) {
      const start = `${formData.appointmentDate} ${formData.startTime}:00`
      const end = `${formData.appointmentDate} ${formData.endTime}:00`
      roomOptions.value = await getAvailableRoomsByServiceProduct(productId, start, end)
    } else {
      // 时段未确定：加载全部启用房间，前端按 RequiredRoomType 过滤
      const allRooms = await getRooms()
      roomOptions.value = product?.requiredRoomType
        ? allRooms.filter(r => r.roomType === product.requiredRoomType)
        : allRooms
    }
  } catch {
    // 接口失败时回退为加载全部启用房间
    roomOptions.value = await getRooms()
  }

  // 当前已选房间不在可用列表中则清空
  if (previousRoomId && !roomOptions.value.some(r => r.id === previousRoomId)) {
    formData.roomId = undefined
    formData.roomName = ''
  }

  // 设备处理：按服务项目 ServiceProductEquipment 关联的设备类型过滤，时段已确定时排除冲突设备
  // 与房间处理保持一致：时段已确定调用接口过滤，时段未确定或接口失败时回退全量
  const previousEquipmentId = formData.equipmentId
  try {
    if (formData.appointmentDate && formData.startTime && formData.endTime) {
      const start = `${formData.appointmentDate} ${formData.startTime}:00`
      const end = `${formData.appointmentDate} ${formData.endTime}:00`
      equipmentOptions.value = await getAvailableEquipmentsByService(productId, start, end)
    } else {
      // 时段未确定：无法做时段冲突过滤，加载全部设备供用户选择
      equipmentOptions.value = await getAllEquipments()
    }
  } catch {
    // 接口失败时回退为加载全部设备
    equipmentOptions.value = await getAllEquipments()
  }

  // 当前已选设备不在可用列表中则清空
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
  // 预约日期默认填入当天
  formData.appointmentDate = formatToday()
  loadRooms()
  loadEquipments()
  loadCustomers()
  loadTechnicians()
  loadServiceProducts()
  dialogVisible.value = true
}

// 提交
const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (valid) {
      submitLoading.value = true
      try {
        await createAppointment({
          customerId: formData.customerId,
          customerName: formData.customerName,
          customerPhone: formData.phone,
          productId: formData.productId!,
          technicianId: formData.technicianId || undefined,
          roomId: formData.roomId || undefined,
          equipmentId: formData.equipmentId || undefined,
          appointmentDate: formData.appointmentDate,
          appointmentTime: formData.startTime,
          remark: formData.remark || undefined
        })
        ElMessage.success('创建预约成功')
        dialogVisible.value = false
        loadData()
      } catch (error: any) {
        ElMessage.error(error.message || '创建失败')
      } finally {
        submitLoading.value = false
      }
    }
  })
}

// 确认预约
const handleConfirm = async (row: Appointment) => {
  try {
    await ElMessageBox.confirm(`确认预约 "${row.customerName}" 吗？`, '确认', {
      type: 'info',
      confirmButtonText: '确定',
      cancelButtonText: '取消'
    })
    await updateAppointmentStatus({ id: row.id, status: 2 })
    ElMessage.success('已确认')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') ElMessage.error('操作失败')
  }
}

// 标记到店
const handleArrive = async (row: Appointment) => {
  try {
    await ElMessageBox.confirm(`确认客户 "${row.customerName}" 已到店吗？`, '到店确认', {
      type: 'success',
      confirmButtonText: '确定',
      cancelButtonText: '取消'
    })
    await updateAppointmentStatus({ id: row.id, status: 3 })
    ElMessage.success('已标记到店')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') ElMessage.error('操作失败')
  }
}

// 取消预约
const handleCancel = async (row: Appointment) => {
  try {
    await ElMessageBox.confirm(`确定要取消 "${row.customerName}" 的预约吗？`, '警告', {
      type: 'warning',
      confirmButtonText: '确定取消',
      cancelButtonText: '保留'
    })
    await updateAppointmentStatus({ id: row.id, status: 5 })
    ElMessage.success('已取消预约')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') ElMessage.error('操作失败')
  }
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
</style>
