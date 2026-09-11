<template>
  <div class="check-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="商品名称">
            <el-input
              v-model="searchForm.productName"
              placeholder="请输入商品名称"
              clearable
              style="width: 180px"
            />
          </el-form-item>
          <el-form-item label="盘点日期">
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
        <el-button v-if="hasPermission('store:purchase-inventory:check:add')" type="primary" @click="handleAdd()">
          <el-icon><Plus /></el-icon>
          新增盘点
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
        <el-table-column prop="productName" label="商品名称" width="200" show-overflow-tooltip />
        <el-table-column prop="productCode" label="商品编码" width="110" />
        <el-table-column label="账面库存" width="110" align="center">
          <template #default="{ row }">
            {{ formatNumber(row.beforeQuantity) }}
          </template>
        </el-table-column>
        <el-table-column label="实际数量" width="110" align="center">
          <template #default="{ row }">
            {{ formatNumber(row.actualQuantity) }}
          </template>
        </el-table-column>
        <el-table-column label="差异数量" width="110" align="center">
          <template #default="{ row }">
            <span :class="getDiffClass(row.diffQuantity)">
              {{ formatDiff(row.diffQuantity) }}
            </span>
          </template>
        </el-table-column>
        <el-table-column label="差异金额" width="160" align="right" show-overflow-tooltip>
          <template #default="{ row }">
            <span :class="getDiffClass(row.diffQuantity)">
              {{ row.diffAmount != null ? formatCurrency(row.diffAmount) : '-' }}
            </span>
          </template>
        </el-table-column>
        <el-table-column prop="operatorName" label="操作人" width="160" />
        <el-table-column label="盘点时间" width="165">
          <template #default="{ row }">
            {{ formatDate(row.checkTime) }}
          </template>
        </el-table-column>
        <el-table-column label="状态" width="120" align="center">
          <template #default="{ row }">
            <el-tag :type="getStatusTagType(row.status)" size="small">
              {{ getStatusLabel(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" min-width="160" align="center" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleDetail(row)">详情</el-button>
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

    <!-- 新增盘点弹窗 -->
    <el-dialog
      v-model="dialogVisible"
      title="新增盘点"
      width="760px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="100px"
      >
        <el-form-item label="商品" prop="productId">
          <el-select
            v-model="formData.productId"
            placeholder="请选择商品"
            filterable
            style="width: 100%"
            @change="handleProductChange"
          >
            <el-option
              v-for="item in productOptions"
              :key="item.id"
              :label="`${item.name}（${item.code}）`"
              :value="item.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="账面库存">
          <span class="book-stock">{{ formData.beforeQuantity != null ? formatNumber(formData.beforeQuantity) : '--' }}</span>
        </el-form-item>
        <el-form-item label="实际数量" prop="actualQuantity">
          <el-input-number
            v-model="formData.actualQuantity"
            :min="0"
            :precision="0"
            :step="1"
            placeholder="请输入实际盘点数量"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item label="差异数量">
          <span :class="getDiffClass(formData.diffQuantity)">
            {{ formData.diffQuantity != null ? formatDiff(formData.diffQuantity) : '--' }}
          </span>
          <span v-if="formData.diffQuantity !== null && formData.diffQuantity !== 0" class="diff-label">
            （{{ formData.diffQuantity > 0 ? '盘盈' : '盘亏' }}）
          </span>
        </el-form-item>
        <el-form-item label="差异金额">
          <span v-if="formData.diffAmount != null" :class="getDiffClass(formData.diffQuantity)">
            {{ formatCurrency(formData.diffAmount) }}
          </span>
          <span v-else class="diff-pending">提交后由后端按批次实际单价计算</span>
        </el-form-item>

        <!-- 盘亏：批次扣减选择区（差异为负时显示，留空走 FIFO 兜底） -->
        <template v-if="formData.diffQuantity !== null && formData.diffQuantity < 0">
          <el-divider content-position="left">
            <span class="batch-divider-title">盘亏批次扣减</span>
            <span class="batch-divider-hint">留空按先进先出自动扣减</span>
          </el-divider>
          <div v-if="formData.batches.length === 0" class="batch-empty">
            当前商品无在库批次，提交时后端将返回"无可用库存批次"
          </div>
          <div v-else>
            <el-table
              :data="formData.batches"
              class="batch-table"
              size="small"
              border
              style="width: 100%"
            >
              <el-table-column label="选择" width="56" align="center">
                <template #default="{ row }">
                  <el-checkbox v-model="row.selected" />
                </template>
              </el-table-column>
              <el-table-column prop="batchNo" label="批次号" min-width="140" show-overflow-tooltip />
              <el-table-column label="在库" width="80" align="right">
                <template #default="{ row }">
                  {{ formatNumber(row.quantity) }}
                </template>
              </el-table-column>
              <el-table-column label="单价" width="100" align="right">
                <template #default="{ row }">
                  ¥{{ formatNumber(row.unitPrice) }}
                </template>
              </el-table-column>
              <el-table-column label="到期日" width="150">
                <template #default="{ row }">
                  <span v-if="!row.expirationDate" class="batch-no-expiry">无</span>
                  <div v-else class="batch-expiry">
                    <span>{{ row.expirationDate.split('T')[0] }}</span>
                    <el-tag
                      v-if="getExpirationStatus(row.expirationDate).type === 'expired'"
                      type="danger"
                      size="small"
                      effect="dark"
                    >过期</el-tag>
                    <el-tag
                      v-else-if="getExpirationStatus(row.expirationDate).type === 'warning'"
                      type="warning"
                      size="small"
                    >{{ getExpirationStatus(row.expirationDate).label }}</el-tag>
                  </div>
                </template>
              </el-table-column>
              <el-table-column label="扣减数量" width="150" align="center">
                <template #default="{ row }">
                  <el-input-number
                    v-if="row.selected"
                    v-model="row.deductQty"
                    :min="0"
                    :max="row.quantity"
                    :precision="0"
                    :step="1"
                    size="small"
                    style="width: 120px"
                  />
                  <span v-else class="batch-deduct-placeholder">—</span>
                </template>
              </el-table-column>
            </el-table>
            <div class="batch-summary">
              <span class="batch-summary-label">
                已选扣减：<strong>{{ formatNumber(selectedDeductTotal) }}</strong>
                / 需扣减：<strong class="batch-summary-required">{{ formatNumber(-formData.diffQuantity) }}</strong>
              </span>
              <span
                v-if="selectedDeductTotal > 0 && selectedDeductTotal !== -formData.diffQuantity"
                class="batch-warn"
              >
                ⚠ 合计需与需扣减量匹配，否则后端将拒绝
              </span>
              <span v-else-if="selectedDeductTotal === 0" class="batch-fifo">
                未选批次，将按先进先出自动扣减
              </span>
            </div>
          </div>
        </template>

        <!-- 盘盈：批次匹配区（差异为正时显示，累加到已有批次） -->
        <template v-if="formData.diffQuantity !== null && formData.diffQuantity > 0">
          <el-divider content-position="left">
            <span class="gain-divider-title">盘盈批次匹配</span>
            <span class="batch-divider-hint">选择已有批次，库存将累加到原批次</span>
          </el-divider>

          <!-- 匹配方式：按过期日期 / 无效期（同一操作层级，分段控件） -->
          <el-form-item label="匹配方式">
            <el-segmented v-model="gainMatchMode" :options="gainMatchModeOptions" />
          </el-form-item>

          <!-- 按过期日期模式：生产日期/保质期/过期日期，复刻采购订单明细自动填充逻辑 -->
          <template v-if="gainMatchMode === 'expiry'">
            <el-form-item label="生产日期">
              <el-date-picker
                v-model="formData.gainProductionDate"
                type="date"
                value-format="YYYY-MM-DD"
                placeholder="选择生产日期"
                clearable
                style="width: 100%"
              />
            </el-form-item>
            <el-form-item label="保质期(天)">
              <el-input-number
                v-model="formData.gainShelfLifeDays"
                :min="1"
                :step="1"
                :precision="0"
                placeholder="保质期天数"
                style="width: 100%"
              />
            </el-form-item>
            <el-form-item label="过期日期">
              <el-date-picker
                v-model="formData.gainExpirationDate"
                type="date"
                value-format="YYYY-MM-DD"
                placeholder="选择过期日期"
                clearable
                style="width: 100%"
              />
            </el-form-item>
          </template>
          <el-form-item v-else label="匹配说明">
            <span class="gain-no-expiry-hint">将列出该商品未录入效期（无过期日期）的批次</span>
          </el-form-item>

          <!-- 批次候选列表：根据过期日期或无效期加载，表格多选并录入累加数量 -->
          <el-form-item label="匹配批次">
            <div v-if="gainMatchMode === 'expiry' && !formData.gainExpirationDate" class="gain-batch-feedback gain-batch-empty">
              请先选择过期日期，以匹配对应批次
            </div>
            <div v-else-if="gainBatchOptions.length === 0" class="gain-batch-feedback gain-batch-empty">
              {{ gainMatchMode === 'expiry' ? `无过期日期为 ${formData.gainExpirationDate} 的批次` : '该商品无无效期（无过期日期）批次' }}
            </div>
            <template v-else>
              <el-table
                :data="gainBatchOptions"
                class="batch-table"
                size="small"
                border
                style="width: 100%"
              >
                <el-table-column label="选择" width="56" align="center">
                  <template #default="{ row }">
                    <el-checkbox v-model="row.selected" @change="(val: boolean) => handleGainCheckChange(row, val)" />
                  </template>
                </el-table-column>
                <el-table-column prop="batchNo" label="批次号" min-width="140" show-overflow-tooltip />
                <el-table-column label="在库" width="80" align="right">
                  <template #default="{ row }">
                    {{ formatNumber(row.quantity) }}
                  </template>
                </el-table-column>
                <el-table-column label="到期日" width="110">
                  <template #default="{ row }">
                    <span v-if="!row.expirationDate" class="batch-no-expiry">无效期</span>
                    <span v-else>{{ formatDateStr(row.expirationDate) }}</span>
                  </template>
                </el-table-column>
                <el-table-column label="累加数量" width="150" align="center">
                  <template #default="{ row }">
                    <el-input-number
                      v-if="row.selected"
                      v-model="row.gainQty"
                      :min="0"
                      :precision="0"
                      :step="1"
                      size="small"
                      style="width: 120px"
                      @change="() => handleGainCandidateQtyChange(row)"
                    />
                    <span v-else class="batch-deduct-placeholder">—</span>
                  </template>
                </el-table-column>
              </el-table>
              <!-- 已选批次：跨效期累积（切换匹配条件/效期不清空），可调整累加数量或移除 -->
              <div v-if="gainSelectedBatches.length > 0" class="gain-selected-block">
                <div class="gain-selected-title">
                  已选批次
                  <span class="gain-selected-hint">可切换其他效期继续累加，勾选的批次会累积到此列表</span>
                </div>
                <el-table
                  :data="gainSelectedBatches"
                  class="batch-table"
                  size="small"
                  border
                  style="width: 100%"
                >
                  <el-table-column prop="batchNo" label="批次号" min-width="140" show-overflow-tooltip />
                  <el-table-column label="到期日" width="120">
                    <template #default="{ row }">
                      <span v-if="!row.expirationDate" class="batch-no-expiry">无效期</span>
                      <span v-else>{{ formatDateStr(row.expirationDate) }}</span>
                    </template>
                  </el-table-column>
                  <el-table-column label="累加数量" width="150" align="center">
                    <template #default="{ row }">
                      <el-input-number
                        v-model="row.gainQty"
                        :min="0"
                        :precision="0"
                        :step="1"
                        size="small"
                        style="width: 120px"
                        @change="() => handleGainSelectedQtyChange(row)"
                      />
                    </template>
                  </el-table-column>
                  <el-table-column label="" width="70" align="center">
                    <template #default="{ row }">
                      <el-button link type="danger" size="small" @click="removeGainSelected(row)">移除</el-button>
                    </template>
                  </el-table-column>
                </el-table>
              </div>
              <div class="batch-summary">
                <span class="batch-summary-label">
                  已选累加：<strong>{{ formatNumber(selectedGainTotal) }}</strong>
                  / 需累加：<strong class="batch-summary-required">{{ formatNumber(formData.diffQuantity) }}</strong>
                </span>
                <span
                  v-if="selectedGainTotal > 0 && selectedGainTotal !== formData.diffQuantity"
                  class="batch-warn"
                >
                  ⚠ 合计需与盘盈数量匹配，否则后端将拒绝
                </span>
                <span v-else-if="selectedGainTotal === 0" class="batch-fifo">
                  请勾选批次并录入累加数量
                </span>
              </div>
            </template>
          </el-form-item>
        </template>

        <el-form-item label="备注" prop="remark">
          <el-input v-model="formData.remark" type="textarea" :rows="3" placeholder="请输入盘点说明/差异原因" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          确定
        </el-button>
      </template>
    </el-dialog>

    <!-- 盘点详情弹窗 -->
    <el-dialog
      v-model="detailVisible"
      title="盘点详情"
      width="720px"
      :close-on-click-modal="false"
    >
      <div v-loading="detailLoading" class="detail-body">
        <template v-if="detailData">
          <el-descriptions :column="2" border>
            <el-descriptions-item label="商品名称">{{ detailData.productName }}</el-descriptions-item>
            <el-descriptions-item label="商品编码">{{ detailData.productCode || '-' }}</el-descriptions-item>
            <el-descriptions-item label="账面库存">{{ formatNumber(detailData.beforeQuantity) }}</el-descriptions-item>
            <el-descriptions-item label="实际数量">{{ formatNumber(detailData.actualQuantity) }}</el-descriptions-item>
            <el-descriptions-item label="差异数量">
              <span :class="getDiffClass(detailData.diffQuantity)">{{ formatDiff(detailData.diffQuantity) }}</span>
              <span v-if="detailData.diffQuantity > 0" class="diff-label">（盘盈）</span>
              <span v-else-if="detailData.diffQuantity < 0" class="diff-label">（盘亏）</span>
            </el-descriptions-item>
            <el-descriptions-item label="差异金额">
              <span v-if="detailData.diffAmount != null" :class="getDiffClass(detailData.diffQuantity)">
                {{ formatCurrency(detailData.diffAmount) }}
              </span>
              <span v-else class="diff-pending">—</span>
            </el-descriptions-item>
            <el-descriptions-item label="状态">
              <el-tag :type="getStatusTagType(detailData.status)" size="small">{{ getStatusLabel(detailData.status) }}</el-tag>
            </el-descriptions-item>
            <el-descriptions-item label="操作人">{{ detailData.operatorName || '-' }}</el-descriptions-item>
            <el-descriptions-item label="盘点时间">{{ formatDate(detailData.checkTime) }}</el-descriptions-item>
            <el-descriptions-item label="备注" :span="2">{{ detailData.remark || '-' }}</el-descriptions-item>
          </el-descriptions>

          <div class="detail-batch-title">
            批次明细
            <span v-if="detailData.diffQuantity > 0" class="detail-batch-dir">（盘盈累加）</span>
            <span v-else-if="detailData.diffQuantity < 0" class="detail-batch-dir">（盘亏扣减）</span>
          </div>
          <el-table
            :data="detailData.batches || []"
            class="batch-table"
            size="small"
            border
            style="width: 100%"
          >
            <el-table-column prop="batchNo" label="批次号" min-width="160" show-overflow-tooltip />
            <el-table-column label="数量" width="90" align="right">
              <template #default="{ row }">
                {{ formatNumber(row.quantity) }}
              </template>
            </el-table-column>
            <el-table-column label="单价" width="110" align="right">
              <template #default="{ row }">
                ¥{{ formatNumber(row.unitPrice ?? 0) }}
              </template>
            </el-table-column>
            <el-table-column label="金额" width="120" align="right">
              <template #default="{ row }">
                <span :class="getDiffClass(detailData.diffQuantity)">
                  {{ formatBatchAmount(row, detailData.diffQuantity) }}
                </span>
              </template>
            </el-table-column>
            <el-table-column label="过期日期" width="120">
              <template #default="{ row }">
                <span v-if="row.expirationDate">{{ formatDateStr(row.expirationDate) }}</span>
                <span v-else class="batch-no-expiry">无效期</span>
              </template>
            </el-table-column>
          </el-table>
          <div v-if="!detailData.batches || detailData.batches.length === 0" class="detail-batch-empty">
            本次盘点暂无批次明细记录
          </div>
        </template>
      </div>
      <template #footer>
        <el-button @click="detailVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus } from '@element-plus/icons-vue'
import {
  getInventoryCheckList,
  getInventoryCheckById,
  createAndSubmitInventoryCheck,
  checkProductCheckedToday,
  getProductOptionsForCheck,
  getBatchOptionsForCheck
} from '@/api/inventory-check'
import { useSystemConfigStore } from '@/stores/systemConfig'
import { useUserStore } from '@/stores/user'
import type { InventoryCheck, InventoryCheckProductOption, InventoryCheckBatchOption, InventoryCheckBatchLookup, InventoryCheckBatch, GainBatchItem, BatchDeductItem } from '@/api/inventory-check/types'

const systemConfigStore = useSystemConfigStore()
const userStore = useUserStore()
const hasPermission = (permissionCode: string) => userStore.hasPermission(permissionCode)

// 搜索表单
const searchForm = reactive({
  productName: '',
  dateRange: [] as string[]
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<InventoryCheck[]>([])

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

// 下拉选项
const productOptions = ref<InventoryCheckProductOption[]>([])

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getInventoryCheckList({
      productName: searchForm.productName || undefined,
      startDate: searchForm.dateRange?.[0] || undefined,
      endDate: searchForm.dateRange?.[1] || undefined,
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

// 加载下拉选项
const loadOptions = async () => {
  try {
    productOptions.value = await getProductOptionsForCheck()
  } catch {
    ElMessage.error('加载选项数据失败')
  }
}

// 搜索
const handleSearch = () => {
  pagination.pageIndex = 1
  loadData()
}

// 重置
const handleReset = () => {
  searchForm.productName = ''
  searchForm.dateRange = []
  handleSearch()
}

// 弹窗
const dialogVisible = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()

// 详情弹窗
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<InventoryCheck | null>(null)

// 批次选项前端展示态（在 InventoryCheckBatchOption 基础上加 selected/deductQty）
interface BatchSelection extends InventoryCheckBatchOption {
  selected: boolean
  deductQty: number
}

// 盘盈候选批次选择态（在 InventoryCheckBatchLookup 基础上加 selected/gainQty）
interface GainBatchSelection extends InventoryCheckBatchLookup {
  selected: boolean
  gainQty: number
}

const formData = reactive({
  productId: undefined as number | undefined,
  beforeQuantity: null as number | null,
  unitCost: null as number | null,
  actualQuantity: 0,
  diffQuantity: null as number | null,
  diffAmount: null as number | null,
  remark: '',
  // 盘亏批次选择
  batches: [] as BatchSelection[],
  // 盘盈批次匹配：候选批次勾选+累加数量（按过期日期/无效期加载），日期字段用于匹配与自动填充
  gainBatches: [] as GainBatchSelection[],
  gainProductionDate: '',
  gainShelfLifeDays: null as number | null,
  gainExpirationDate: ''
})

const formRules: FormRules = {
  productId: [
    { required: true, message: '请选择商品', trigger: 'change' }
  ],
  actualQuantity: [
    { required: true, message: '请输入实际盘点数量', trigger: 'blur' },
    { type: 'number', min: 0, message: '实际数量不能为负数', trigger: 'blur' }
  ]
}

// 已选批次扣减合计
const selectedDeductTotal = computed(() => {
  return formData.batches
    .filter(b => b.selected)
    .reduce((sum, b) => sum + (b.deductQty || 0), 0)
})

// 临期判定阈值（天）：到期日在阈值内视为临期，提示操作员关注
const NEAR_EXPIRY_DAYS = 30

// 批次到期日状态：过期 / 临期 / 正常，用于盘亏批次表格标签展示
const getExpirationStatus = (dateStr?: string): { type: 'expired' | 'warning' | 'normal', label?: string } => {
  if (!dateStr) return { type: 'normal' }
  const exp = new Date(dateStr)
  if (isNaN(exp.getTime())) return { type: 'normal' }
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  exp.setHours(0, 0, 0, 0)
  const diffDays = Math.floor((exp.getTime() - today.getTime()) / (1000 * 60 * 60 * 24))
  if (diffDays < 0) return { type: 'expired', label: '过期' }
  if (diffDays <= NEAR_EXPIRY_DAYS) return { type: 'warning', label: `临期${diffDays}天` }
  return { type: 'normal' }
}

// ========== 盘盈批次匹配（按过期日期 / 无效期选择累加批次） ==========
// 匹配方式：expiry 按过期日期 / no-expiry 无效期（未录入效期批次）
const gainMatchMode = ref<'expiry' | 'no-expiry'>('expiry')
const gainMatchModeOptions = [
  { label: '按过期日期', value: 'expiry' },
  { label: '无效期', value: 'no-expiry' }
]
// 匹配到的候选批次选择态列表（含已用完/已过期批次）
const gainBatchOptions = ref<GainBatchSelection[]>([])
const gainBatchListLoading = ref(false)

// 已选累加批次（跨效期累积：候选勾选后按对象引用加入，切换效期/匹配方式不清空）
const gainSelectedBatches = ref<GainBatchSelection[]>([])

// 已选批次累加合计（跨效期累计）
const selectedGainTotal = computed(() => {
  return gainSelectedBatches.value
    .reduce((sum, b) => sum + (b.gainQty || 0), 0)
})

// 日期展示：ISO 日期字符串取日期部分（YYYY-MM-DD）
const formatDateStr = (dateStr?: string): string => {
  if (!dateStr) return '-'
  return dateStr.split('T')[0]
}

// 加载批次候选列表：按匹配方式传参（按过期日期精确匹配 / 无效期）
// 候选仅按效期筛选加载（避免全量批次过多）；已选批次（gainSelectedBatches）跨效期累积，切换筛选不清空
const loadGainBatchOptions = async () => {
  if (!formData.productId) return
  // 按过期日期模式：未选择过期日期时不加载，清空列表
  if (gainMatchMode.value === 'expiry' && !formData.gainExpirationDate) {
    formData.gainBatches = []
    gainBatchOptions.value = []
    return
  }
  gainBatchListLoading.value = true
  try {
    const params = gainMatchMode.value === 'expiry'
      ? { expirationDate: formData.gainExpirationDate }
      : { noExpiry: true }
    const list = await getBatchOptionsForCheck(formData.productId, params)
    // 候选映射选择态：已在已选集合中的批次回显勾选与累加数量（对象引用变更，已选持久保留）
    formData.gainBatches = list.map(b => {
      const sel = gainSelectedBatches.value.find(s => s.id === b.id)
      return { ...b, selected: !!sel, gainQty: sel?.gainQty ?? 0 }
    })
    gainBatchOptions.value = formData.gainBatches
  } catch {
    ElMessage.error('加载批次列表失败')
    formData.gainBatches = []
    gainBatchOptions.value = []
  } finally {
    gainBatchListLoading.value = false
  }
}

// 候选勾选切换：勾选时加入已选（同批次已选则仅同步累加数量），取消时从已选移除
const handleGainCheckChange = (row: GainBatchSelection, checked: boolean) => {
  if (checked) {
    const existing = gainSelectedBatches.value.find(b => b.id === row.id)
    if (existing) {
      existing.gainQty = row.gainQty
    } else {
      gainSelectedBatches.value.push(row)
    }
  } else {
    const idx = gainSelectedBatches.value.findIndex(b => b.id === row.id)
    if (idx >= 0) gainSelectedBatches.value.splice(idx, 1)
  }
}

// 候选累加数量变化：同步到已选集合中的同名批次（若已加入，候选与已选对象引用可能不同）
const handleGainCandidateQtyChange = (row: GainBatchSelection) => {
  const sel = gainSelectedBatches.value.find(b => b.id === row.id)
  if (sel) sel.gainQty = row.gainQty
}

// 已选累加数量变化：同步回当前候选中的同名批次（若存在）
const handleGainSelectedQtyChange = (row: GainBatchSelection) => {
  const cand = gainBatchOptions.value.find(b => b.id === row.id)
  if (cand) cand.gainQty = row.gainQty
}

// 移除已选批次：同步取消当前候选中的勾选状态
const removeGainSelected = (row: GainBatchSelection) => {
  const idx = gainSelectedBatches.value.findIndex(b => b.id === row.id)
  if (idx >= 0) gainSelectedBatches.value.splice(idx, 1)
  const cand = gainBatchOptions.value.find(b => b.id === row.id)
  if (cand) cand.selected = false
}

// 匹配方式或过期日期变化（含自动填充联动）→ 清空勾选并重新加载候选列表
watch(
  () => [gainMatchMode.value, formData.gainExpirationDate],
  () => {
    loadGainBatchOptions()
  }
)

// 商品选择变化时带出账面库存、成本价、在库批次
const handleProductChange = async (productId: number) => {
  const product = productOptions.value.find(p => p.id === productId)
  if (product) {
    formData.beforeQuantity = product.stock
    formData.unitCost = product.costPrice ?? null
    formData.actualQuantity = product.stock
    // 同步在库批次到前端选择态
    formData.batches = (product.batches || []).map(b => ({
      ...b,
      selected: false,
      deductQty: 0
    }))
    // 切换商品时重置盘盈批次匹配（已选/候选批次、日期字段、候选列表、匹配方式）
    formData.gainBatches = []
    gainSelectedBatches.value = []
    formData.gainProductionDate = ''
    formData.gainShelfLifeDays = null
    formData.gainExpirationDate = ''
    gainMatchMode.value = 'expiry'
    gainBatchOptions.value = []
    calculateDiff()

    // 软约束：查询当日该商品是否已盘点，已盘则提示是否继续作为纠错盘点
    try {
      const hasChecked = await checkProductCheckedToday(productId)
      if (hasChecked) {
        await ElMessageBox.confirm(
          '当日已存在该商品的盘点记录，本次是否作为纠错盘点继续？',
          '提示',
          { confirmButtonText: '继续盘点', cancelButtonText: '取消', type: 'warning' }
        ).catch(() => {
          // 用户取消，重置商品选择
          formData.productId = undefined
          formData.beforeQuantity = null
          formData.actualQuantity = 0
          formData.diffQuantity = null
          formData.batches = []
        })
      }
    } catch {
      // 查询失败不阻塞流程
    }
  }
}

// 自动计算差异（差异金额改为后端返回，前端不再自算）
const calculateDiff = () => {
  if (formData.beforeQuantity != null && formData.actualQuantity != null) {
    formData.diffQuantity = formData.actualQuantity - formData.beforeQuantity
    // 差异金额由后端按批次实际单价计算并持久化，前端不预填
    formData.diffAmount = null
  }
}

// 重置表单
const resetFormData = () => {
  formData.productId = undefined
  formData.beforeQuantity = null
  formData.unitCost = null
  formData.actualQuantity = 0
  formData.diffQuantity = null
  formData.diffAmount = null
  formData.remark = ''
  formData.batches = []
  formData.gainBatches = []
  gainSelectedBatches.value = []
  formData.gainProductionDate = ''
  formData.gainShelfLifeDays = null
  formData.gainExpirationDate = ''
  gainMatchMode.value = 'expiry'
  gainBatchOptions.value = []
}

// 新增
const handleAdd = () => {
  resetFormData()
  dialogVisible.value = true
}

// 查看详情：拉取最新记录（含商品信息与批次明细）并弹出详情弹窗
const handleDetail = async (row: InventoryCheck) => {
  detailVisible.value = true
  detailLoading.value = true
  detailData.value = null
  try {
    detailData.value = await getInventoryCheckById(row.id)
  } catch {
    ElMessage.error('加载详情失败')
  } finally {
    detailLoading.value = false
  }
}

// 盘点单状态标签类型（颜色）
const getStatusTagType = (status: number): 'info' | 'success' | 'warning' => {
  switch (status) {
    case 0: return 'info'      // 草稿
    case 1: return 'success'   // 已完成
    case 2: return 'warning'   // 已取消
    default: return 'info'
  }
}

// 盘点单状态文本
const getStatusLabel = (status: number): string => {
  switch (status) {
    case 0: return '草稿'
    case 1: return '已完成'
    case 2: return '已取消'
    default: return '未知'
  }
}

// 提交表单：原子操作（创建并提交），事务内完成不产生草稿残留
const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return

    // 盘亏指定批次模式：校验合计与差异数量匹配
    const diff = formData.diffQuantity ?? 0
    const selectedBatches = formData.batches.filter(b => b.selected && b.deductQty > 0)
    if (diff < 0 && selectedBatches.length > 0) {
      const total = selectedBatches.reduce((s, b) => s + b.deductQty, 0)
      if (total !== -diff) {
        ElMessage.error(`指定批次扣减合计 ${total} 与需扣减量 ${-diff} 不匹配`)
        return
      }
    }

    // 盘盈校验：必须已选至少一个批次并录入累加数量，且合计与盘盈数量匹配
    if (diff > 0) {
      if (gainSelectedBatches.value.length === 0) {
        ElMessage.error('请选择盘盈累加批次并录入累加数量')
        return
      }
      const gainTotal = gainSelectedBatches.value.reduce((s, b) => s + (b.gainQty || 0), 0)
      if (gainTotal !== diff) {
        ElMessage.error(`指定批次累加合计 ${gainTotal} 与需累加量 ${diff} 不匹配`)
        return
      }
    }

    submitLoading.value = true
    try {
      // 原子操作：创建并提交盘点单（事务内完成，不产生草稿残留）
      const payload: import('@/api/inventory-check/types').CreateAndSubmitRequest = {
        productId: formData.productId!,
        beforeQuantity: formData.beforeQuantity ?? 0,
        actualQuantity: formData.actualQuantity,
        remark: formData.remark || undefined
      }
      if (diff < 0 && selectedBatches.length > 0) {
        // 盘亏指定批次模式
        payload.deductBatches = selectedBatches.map(b => ({
          batchId: b.id,
          quantity: b.deductQty
        } as BatchDeductItem))
      }
      // 盘亏未选批次 -> 不传 deductBatches，后端走 FIFO 兜底
      if (diff > 0 && gainSelectedBatches.value.length > 0) {
        // 盘盈：传已选批次累加明细，后端逐个批次累加库存（单价/日期等由原批次决定，前端不传）
        payload.gainBatches = gainSelectedBatches.value
          .filter(b => (b.gainQty || 0) > 0)
          .map(b => ({
            batchId: b.id,
            quantity: b.gainQty
          } as GainBatchItem))
      }

      await createAndSubmitInventoryCheck(payload)
      ElMessage.success('盘点成功')
      dialogVisible.value = false
      loadData()
    } catch (error) {
      ElMessage.error((error as Error).message || '盘点失败')
    } finally {
      submitLoading.value = false
    }
  })
}

// 获取差异样式类（盘盈绿色，盘亏红色，无差异默认色）
const getDiffClass = (diff: number | null) => {
  if (diff == null || diff === 0) return 'diff-zero'
  return diff > 0 ? 'diff-positive' : 'diff-negative'
}

// 格式化差异数量（带正负号）
const formatDiff = (diff: number) => {
  if (diff > 0) return `+${formatNumber(diff)}`
  return formatNumber(diff)
}

// 格式化金额
const formatCurrency = (amount: number) => {
  const absAmount = Math.abs(amount)
  const formatted = absAmount.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
  return amount >= 0 ? `¥${formatted}` : `-¥${formatted}`
}

// 格式化批次明细金额（按盘点方向取正负号：盘盈为正、盘亏为负）
const formatBatchAmount = (row: InventoryCheckBatch, direction: number) => {
  const amount = (row.unitPrice ?? 0) * row.quantity
  return formatCurrency(direction > 0 ? amount : direction < 0 ? -amount : amount)
}

// 格式化数字
const formatNumber = (num: number) => {
  return num.toLocaleString('zh-CN', { minimumFractionDigits: 0, maximumFractionDigits: 2 })
}


// 监听实际数量变化，自动计算差异
import { watch } from 'vue'
import { formatDateTime as formatDate } from '@/utils/date'
import { formatDate as formatLocalDate } from '@/utils/date'
watch(() => formData.actualQuantity, () => {
  calculateDiff()
})

// 盘盈：生产日期+保质期天数变化时自动计算过期日期
watch(
  () => [formData.gainProductionDate, formData.gainShelfLifeDays],
  () => {
    if (formData.gainProductionDate && formData.gainShelfLifeDays && formData.gainShelfLifeDays > 0) {
      const date = new Date(formData.gainProductionDate)
      date.setDate(date.getDate() + formData.gainShelfLifeDays)
      formData.gainExpirationDate = formatLocalDate(date)
    }
  }
)

// 盘盈：过期日期变更时（保质期已输入）反推计算生产日期
watch(
  () => formData.gainExpirationDate,
  () => {
    if (formData.gainExpirationDate && formData.gainShelfLifeDays && formData.gainShelfLifeDays > 0) {
      const date = new Date(formData.gainExpirationDate)
      date.setDate(date.getDate() - formData.gainShelfLifeDays)
      formData.gainProductionDate = formatLocalDate(date)
    }
  }
)

onMounted(async () => {
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  pagination.pageSize = systemConfigStore.defaultPageSize
  await loadOptions()
  loadData()
})
</script>

<style scoped>
.check-management {
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

/* 差异样式 */
.diff-positive {
  color: var(--el-color-success);
  font-weight: 600;
}

.diff-negative {
  color: var(--el-color-danger);
  font-weight: 600;
}

.diff-zero {
  color: var(--text-tertiary);
}

/* 账面库存展示 */
.book-stock {
  font-weight: 600;
  color: var(--el-color-primary);
}

/* 差异标签 */
.diff-label {
  margin-left: 8px;
  color: var(--text-tertiary);
  font-size: 14px;
}

/* 差异金额待计算提示 */
.diff-pending {
  color: var(--text-tertiary);
  font-size: 13px;
  font-style: italic;
}

/* 盘亏批次扣减区分隔标题 */
.batch-divider-title {
  font-weight: 600;
  color: var(--el-color-danger);
}

.batch-divider-hint {
  margin-left: 8px;
  font-size: 12px;
  color: var(--text-tertiary);
  font-weight: normal;
}

/* 盘盈批次录入区分隔标题（绿色，区别于盘亏红色） */
.gain-divider-title {
  font-weight: 600;
  color: var(--el-color-success);
}

/* 盘盈批次匹配反馈与空态提示 */
.gain-batch-feedback {
  display: flex;
  align-items: center;
  gap: 4px;
  margin-top: 6px;
  font-size: 13px;
}

.gain-batch-empty {
  color: var(--text-tertiary);
}

.gain-no-expiry-hint {
  color: var(--text-tertiary);
  font-size: 13px;
}

/* 已选批次块（跨效期累积） */
.gain-selected-block {
  margin-top: 12px;
  padding-top: 10px;
  border-top: 1px dashed var(--el-border-color);
}

.gain-selected-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
  font-size: 13px;
  color: var(--el-color-success);
}

.gain-selected-hint {
  font-size: 12px;
  font-weight: normal;
  color: var(--text-tertiary);
}

/* 批次表格 */
.batch-table {
  margin-top: 4px;
}

.batch-table :deep(.el-table__header-wrapper th) {
  background: var(--bg-hover) !important;
  color: var(--text-secondary) !important;
  font-weight: 600;
}

.batch-table :deep(.el-table__row:hover > td) {
  background-color: var(--bg-hover) !important;
}

/* 到期日单元格 */
.batch-expiry {
  display: flex;
  align-items: center;
  gap: 6px;
}

.batch-no-expiry {
  color: var(--text-tertiary);
}

/* 未选中时的扣减数量占位 */
.batch-deduct-placeholder {
  color: var(--text-tertiary);
}

/* 批次合计区 */
.batch-summary {
  margin-top: 10px;
  padding: 10px 12px;
  background: #f5f7fa;
  border: 1px solid #e5e7eb;
  border-radius: 6px;
  font-size: 13px;
  color: #4b5563;
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
}

.batch-summary-label strong {
  color: #1f2937;
}

.batch-summary-required {
  color: var(--el-color-danger);
}

.batch-warn {
  color: var(--el-color-danger);
  font-weight: 600;
}

.batch-fifo {
  color: var(--el-color-primary);
}

.batch-empty {
  padding: 12px;
  color: var(--text-tertiary);
  font-size: 13px;
  background: var(--bg-hover);
  border-radius: 4px;
}

/* 详情弹窗 */
.detail-body {
  min-height: 120px;
}

.detail-batch-title {
  margin: 16px 0 8px;
  font-weight: 600;
  font-size: 14px;
  color: #1f2937;
}

.detail-batch-dir {
  margin-left: 6px;
  font-size: 12px;
  font-weight: normal;
  color: #6b7280;
}

.detail-batch-empty {
  padding: 12px;
  color: #6b7280;
  font-size: 13px;
  background: #f5f7fa;
  border: 1px solid #e5e7eb;
  border-radius: 6px;
}
</style>
