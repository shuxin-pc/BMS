<template>
  <div class="daily-settlement-management">
    <!-- 今日待日结汇总卡片 -->
    <div class="summary-cards mb-20">
      <div class="summary-header">
        <div class="summary-title">
          <span class="title-text">今日经营汇总</span>
          <span class="title-date">{{ todaySummary.date }}</span>
          <el-tag v-if="todaySettlement?.status === 1" type="success" size="small" effect="dark">已确认</el-tag>
          <el-tag v-else-if="todaySettlement?.status === 0" type="warning" size="small" effect="dark">待确认</el-tag>
          <el-tag v-else type="info" size="small" effect="dark">未汇总</el-tag>
        </div>
        <div class="summary-action">
          <!-- 未汇总：执行汇总日结 -->
          <el-button
            v-if="!todaySummary.isSettled"
            type="primary"
            :loading="settleLoading"
            @click="handleSummarize"
          >
            <el-icon><Check /></el-icon>
            汇总日结
          </el-button>
          <!-- 待确认：确认 + 重算 -->
          <template v-else-if="todaySettlement?.status === 0">
            <el-button type="success" :loading="confirmLoading" @click="handleConfirm(todaySettlement)">
              <el-icon><Check /></el-icon>
              确认
            </el-button>
            <el-button :loading="recalcLoading" @click="handleRecalculate(todaySettlement.id)">
              重算
            </el-button>
          </template>
          <!-- 已确认：反日结 -->
          <el-button
            v-else-if="todaySettlement?.status === 1"
            type="warning"
            @click="handleReverse(todaySettlement)"
          >
            反日结
          </el-button>
        </div>
      </div>
      <div class="summary-grid">
        <div class="summary-item">
          <div class="summary-label">总营收</div>
          <div class="summary-value revenue">{{ formatMoney(todaySummary.totalRevenue) }}</div>
        </div>
        <div class="summary-item">
          <div class="summary-label">订单数</div>
          <div class="summary-value">{{ todaySummary.orderCount }}</div>
        </div>
        <div class="summary-item">
          <div class="summary-label">总退款</div>
          <div class="summary-value refund">{{ formatMoney(todaySummary.totalRefund) }}</div>
        </div>
        <div class="summary-item">
          <div class="summary-label">净营收</div>
          <div class="summary-value" :class="todaySummary.netRevenue < 0 ? 'net-negative' : (todaySummary.netRevenue > 0 ? 'revenue' : '')">
            {{ formatMoney(todaySummary.netRevenue) }}
          </div>
        </div>
        <div class="summary-item">
          <div class="summary-label">储值充值</div>
          <div class="summary-value recharge">{{ formatMoney(todaySummary.totalStoredValueRecharge) }}</div>
        </div>
        <div class="summary-item">
          <div class="summary-label">储值消费</div>
          <div class="summary-value consume">{{ formatMoney(todaySummary.totalStoredValueConsume) }}</div>
        </div>
      </div>
      <!-- 退款大于营收异常提示条 -->
      <div v-if="todaySummary.isRefundExceedRevenue" class="refund-exceed-tip">
        <el-icon><WarningFilled /></el-icon>
        <span>当日退款 {{ formatMoney(todaySummary.totalRefund) }} 大于营收 {{ formatMoney(todaySummary.totalRevenue) }}，请确认是否有异常退款</span>
      </div>
    </div>

    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="日结日期">
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
          <el-form-item label="状态">
            <el-select v-model="searchForm.status" placeholder="全部状态" clearable style="width: 120px">
              <el-option label="待确认" :value="0" />
              <el-option label="已确认" :value="1" />
            </el-select>
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
        <span class="toolbar-title">日结记录列表</span>
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
        :row-class-name="rowClassName"
        style="width: 100%"
      >
        <el-table-column label="日结日期" width="120">
          <template #default="{ row }">
            <el-link type="primary" :underline="false" @click="handleViewDetail(row)">
              {{ row.settlementDate }}
            </el-link>
          </template>
        </el-table-column>
        <el-table-column label="总营收" width="120" align="right">
          <template #default="{ row }">
            <span class="amount-text">{{ formatMoney(row.totalRevenue) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="总退款" width="110" align="right">
          <template #default="{ row }">
            <span class="amount-text" :class="{ danger: row.totalRefund > 0 }">
              {{ formatMoney(row.totalRefund) }}
            </span>
          </template>
        </el-table-column>
        <el-table-column label="净营收" width="120" align="right">
          <template #default="{ row }">
            <span class="amount-text" :class="{ danger: row.netRevenue < 0, success: row.netRevenue > 0 }">
              {{ formatMoney(row.netRevenue) }}
            </span>
          </template>
        </el-table-column>
        <el-table-column label="退款率" width="90" align="right">
          <template #default="{ row }">
            <span :class="{ 'ratio-danger': row.refundRatio > 1, 'ratio-warning': row.refundRatio > 0 && row.refundRatio <= 1 }">
              {{ formatRefundRatio(row.refundRatio) }}
            </span>
          </template>
        </el-table-column>
        <el-table-column label="储值充值" width="120" align="right">
          <template #default="{ row }">
            {{ formatMoney(row.totalStoredValueRecharge) }}
          </template>
        </el-table-column>
        <el-table-column label="储值消费" width="120" align="right">
          <template #default="{ row }">
            {{ formatMoney(row.totalStoredValueConsume) }}
          </template>
        </el-table-column>
        <el-table-column prop="orderCount" label="订单数" width="80" align="center" />
        <el-table-column label="状态" width="90">
          <template #default="{ row }">
            <el-tag v-if="row.status === 1" type="success" size="small" effect="dark">已确认</el-tag>
            <el-tag v-else type="warning" size="small" effect="dark">待确认</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="日结时间" width="170">
          <template #default="{ row }">
            {{ formatDateTime(row.settlementTime) }}
          </template>
        </el-table-column>
        <el-table-column prop="remark" label="备注" min-width="120" show-overflow-tooltip />
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button type="info" link size="small" @click="handleViewDetail(row)">详情</el-button>
            <template v-if="row.status === 0">
              <el-button type="primary" link size="small" @click="handleConfirm(row)">确认</el-button>
              <el-tooltip content="重新汇总" placement="top">
                <el-button type="primary" link size="small" :loading="recalcLoading" @click="handleRecalculate(row.id)">
                  <el-icon><Refresh /></el-icon>
                </el-button>
              </el-tooltip>
            </template>
            <template v-else>
              <el-button type="warning" link size="small" @click="handleReverse(row)">反日结</el-button>
            </template>
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

    <!-- 汇总日结确认弹窗 -->
    <el-dialog
      v-model="summarizeDialogVisible"
      title="确认汇总日结"
      width="500px"
      :close-on-click-modal="false"
    >
      <el-alert
        title="汇总日结将创建待确认记录，确认后数据锁定。无订单日也会生成 0 数据记录以便核对。"
        type="warning"
        :closable="false"
        show-icon
        class="mb-20"
      />
      <el-form label-width="100px">
        <el-form-item label="日结日期">
          <span>{{ todaySummary.date }}</span>
        </el-form-item>
        <el-form-item label="总营收">
          <span class="amount-text">{{ formatMoney(todaySummary.totalRevenue) }}</span>
        </el-form-item>
        <el-form-item label="订单数">
          <span>{{ todaySummary.orderCount }} 单</span>
        </el-form-item>
        <el-form-item label="总退款">
          <span class="amount-text danger">{{ formatMoney(todaySummary.totalRefund) }}</span>
        </el-form-item>
        <el-form-item label="储值充值">
          <span>{{ formatMoney(todaySummary.totalStoredValueRecharge) }}</span>
        </el-form-item>
        <el-form-item label="储值消费">
          <span>{{ formatMoney(todaySummary.totalStoredValueConsume) }}</span>
        </el-form-item>
        <el-form-item label="备注">
          <el-input
            v-model="summarizeRemark"
            type="textarea"
            :rows="3"
            placeholder="可填写日结备注（可选）"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="summarizeDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="settleLoading" @click="handleSummarizeConfirm">
          确认汇总
        </el-button>
      </template>
    </el-dialog>

    <!-- 确认日结弹窗（展示防漏单校验警告） -->
    <el-dialog
      v-model="confirmDialogVisible"
      title="确认日结"
      width="500px"
      :close-on-click-modal="false"
    >
      <el-alert
        v-if="validationWarnings.length > 0"
        title="防漏单校验提醒"
        type="warning"
        :closable="false"
        show-icon
        class="mb-20"
      />
      <div v-if="validationWarnings.length > 0" class="warning-list mb-20">
        <div v-for="(w, i) in validationWarnings" :key="i" class="warning-item">• {{ w }}</div>
      </div>
      <el-alert
        v-else
        title="校验通过，无异常提醒"
        type="success"
        :closable="false"
        show-icon
        class="mb-20"
      />
      <div class="view-detail-link mb-20">
        <el-link type="primary" :underline="false" @click="handleOpenDetailFromConfirm">
          <el-icon><Document /></el-icon>
          查看完整明细
        </el-link>
      </div>
      <div class="confirm-tip">确认后日结数据将锁定，如需修改请使用反日结。</div>
      <template #footer>
        <el-button @click="confirmDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="confirmLoading" @click="handleConfirmSubmit">
          确认日结
        </el-button>
      </template>
    </el-dialog>

    <!-- 反日结弹窗 -->
    <el-dialog
      v-model="reverseDialogVisible"
      title="反日结"
      width="500px"
      :close-on-click-modal="false"
    >
      <el-alert
        title="反日结后状态回退为待确认，可重新汇总或确认。允许跳过中间日期。"
        type="warning"
        :closable="false"
        show-icon
        class="mb-20"
      />
      <el-form label-width="100px">
        <el-form-item label="日结日期">
          <span>{{ reverseTarget?.settlementDate }}</span>
        </el-form-item>
        <el-form-item label="反日结原因">
          <el-input
            v-model="reverseReason"
            type="textarea"
            :rows="3"
            placeholder="可填写反日结原因（可选，建议填写）"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="reverseDialogVisible = false">取消</el-button>
        <el-button type="warning" :loading="reverseLoading" @click="handleReverseSubmit">
          确认反日结
        </el-button>
      </template>
    </el-dialog>

    <!-- 日结详情弹窗 -->
    <el-dialog
      v-model="detailDialogVisible"
      :title="`日结详情 - ${detailData?.settlementDate ?? ''}`"
      :width="dialogWidth"
      :close-on-click-modal="false"
    >
      <!-- 骨架屏：加载中展示 -->
      <div v-if="detailLoading" class="detail-skeleton">
        <el-skeleton :rows="2" animated />
        <div class="skeleton-section">
          <el-skeleton-item variant="text" style="width: 30%; height: 20px" />
          <el-skeleton :rows="4" animated />
        </div>
        <div class="skeleton-section">
          <el-skeleton-item variant="text" style="width: 30%; height: 20px" />
          <el-skeleton :rows="3" animated />
        </div>
        <div class="skeleton-section">
          <el-skeleton-item variant="text" style="width: 30%; height: 20px" />
          <el-skeleton :rows="5" animated />
        </div>
      </div>
      <div v-else-if="detailData" class="detail-content">
        <!-- 营收明细 -->
        <div class="detail-section">
          <div class="section-title">营收明细</div>
          <div class="section-grid">
            <div class="detail-item">
              <span class="detail-label">总营收</span>
              <span class="detail-value amount-text success">{{ formatMoney(detailData.totalRevenue) }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">现金类营收</span>
              <span class="detail-value">{{ formatMoney(detailData.cashRevenue) }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">储值扣款营收</span>
              <span class="detail-value">{{ formatMoney(detailData.storedValueRevenue) }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">积分抵扣（不计入营收）</span>
              <span class="detail-value">{{ formatMoney(detailData.pointsDeductAmount) }}</span>
            </div>
          </div>
        </div>

        <!-- 退款与净营收 -->
        <div class="detail-section">
          <div class="section-title">退款与净营收</div>
          <div class="section-grid">
            <div class="detail-item">
              <span class="detail-label">总退款</span>
              <span class="detail-value amount-text" :class="{ danger: detailData.totalRefund > 0 }">
                {{ formatMoney(detailData.totalRefund) }}
              </span>
            </div>
            <div class="detail-item">
              <span class="detail-label">退款率</span>
              <span class="detail-value" :class="{ danger: detailData.refundRatio > 1 }">
                {{ formatRefundRatio(detailData.refundRatio) }}
              </span>
            </div>
            <div class="detail-item">
              <span class="detail-label">净营收</span>
              <span class="detail-value amount-text" :class="{ danger: detailData.netRevenue < 0, success: detailData.netRevenue > 0 }">
                {{ formatMoney(detailData.netRevenue) }}
              </span>
            </div>
          </div>
        </div>

        <!-- 成本与毛利 -->
        <div class="detail-section">
          <div class="section-title">成本与毛利</div>
          <div class="section-grid">
            <div class="detail-item">
              <span class="detail-label">总成本</span>
              <span class="detail-value">{{ formatMoney(detailData.totalCost) }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">销售出库成本</span>
              <span class="detail-value">{{ formatMoney(detailData.salesOutboundCost) }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">疗程卡核销成本</span>
              <span class="detail-value">{{ formatMoney(detailData.treatmentCardOutboundCost) }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">总毛利（未扣退款）</span>
              <span class="detail-value">{{ formatMoney(detailData.totalGrossProfit) }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">净毛利（扣除退款）</span>
              <span class="detail-value amount-text" :class="{ danger: detailData.netGrossProfit < 0, success: detailData.netGrossProfit > 0 }">
                {{ formatMoney(detailData.netGrossProfit) }}
              </span>
            </div>
          </div>
        </div>

        <!-- 营业外支出 -->
        <div class="detail-section">
          <div class="section-title">营业外支出</div>
          <div class="section-grid">
            <div class="detail-item">
              <span class="detail-label">盘亏损失</span>
              <span class="detail-value">{{ formatMoney(detailData.inventoryLossAmount) }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">样品赠品费用</span>
              <span class="detail-value">{{ formatMoney(detailData.sampleGiftAmount) }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">营业外支出合计</span>
              <span class="detail-value">{{ formatMoney(detailData.totalOperatingExpense) }}</span>
            </div>
          </div>
        </div>

        <!-- 资产变动 -->
        <div class="detail-section">
          <div class="section-title">资产变动（不计损益）</div>
          <div class="section-grid">
            <div class="detail-item">
              <span class="detail-label">调拨出库</span>
              <span class="detail-value">{{ formatMoney(detailData.transferOutAmount) }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">调拨入库</span>
              <span class="detail-value">{{ formatMoney(detailData.transferInAmount) }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">采购退货</span>
              <span class="detail-value">{{ formatMoney(detailData.purchaseReturnAmount) }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">资产变动净额</span>
              <span class="detail-value">{{ formatMoney(detailData.netTransferAmount) }}</span>
            </div>
          </div>
        </div>

        <!-- 储值流水 -->
        <div class="detail-section">
          <div class="section-title">储值流水</div>
          <div class="section-grid">
            <div class="detail-item">
              <span class="detail-label">储值充值</span>
              <span class="detail-value">{{ formatMoney(detailData.totalStoredValueRecharge) }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">储值消费</span>
              <span class="detail-value">{{ formatMoney(detailData.totalStoredValueConsume) }}</span>
            </div>
          </div>
        </div>

        <!-- 业务指标 -->
        <div class="detail-section">
          <div class="section-title">业务指标</div>
          <div class="section-grid">
            <div class="detail-item">
              <span class="detail-label">订单数</span>
              <span class="detail-value">{{ detailData.orderCount }} 单</span>
            </div>
          </div>
        </div>

        <!-- 审计信息 -->
        <div class="detail-section">
          <div class="section-title">审计信息</div>
          <div class="section-grid">
            <div class="detail-item">
              <span class="detail-label">汇总时间</span>
              <span class="detail-value">{{ formatDateTime(detailData.settlementTime) }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">操作员</span>
              <span class="detail-value">{{ getUserName(detailData.operatorId) }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">状态</span>
              <span class="detail-value">
                <el-tag v-if="detailData.status === 1" type="success" size="small" effect="dark">已确认</el-tag>
                <el-tag v-else type="warning" size="small" effect="dark">待确认</el-tag>
              </span>
            </div>
            <div class="detail-item">
              <span class="detail-label">确认人</span>
              <span class="detail-value">{{ getUserName(detailData.confirmedBy) }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">确认时间</span>
              <span class="detail-value">{{ detailData.confirmedTime ? formatDateTime(detailData.confirmedTime) : '-' }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">反日结人</span>
              <span class="detail-value">{{ getUserName(detailData.reversedBy) }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">反日结时间</span>
              <span class="detail-value">{{ detailData.reversedTime ? formatDateTime(detailData.reversedTime) : '-' }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">反日结原因</span>
              <span class="detail-value">{{ detailData.reversedReason || '-' }}</span>
            </div>
            <div class="detail-item detail-item-full">
              <span class="detail-label">备注</span>
              <span class="detail-value">{{ detailData.remark || '-' }}</span>
            </div>
          </div>
        </div>
      </div>
      <template #footer>
        <el-button @click="detailDialogVisible = false">关闭</el-button>
        <template v-if="detailData">
          <el-button v-if="detailData.status === 0" type="primary" :loading="confirmLoading" @click="handleConfirmFromDetail">确认日结</el-button>
          <el-button v-else type="warning" :loading="reverseLoading" @click="handleReverseFromDetail">反日结</el-button>
        </template>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, onUnmounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Search, Refresh, Check, WarningFilled, Document } from '@element-plus/icons-vue'
import { useSystemConfigStore } from '@/stores/systemConfig'
import {
  getTodaySummary,
  getDailySettlements,
  getDailySettlement,
  executeDailySettlement,
  confirmSettlement,
  reverseSettlement,
  validateSettlement,
  recalculateSettlement
} from '@/api/settlement'
import { getUserList } from '@/api/system'
import type { User } from '@/api/system'
import type { DailySettlement, TodaySummary, SettlementStatus } from '@/api/settlement/types'
import { formatMoney, formatRefundRatio } from './utils'

const systemConfigStore = useSystemConfigStore()

// 用户列表缓存（用于审计信息显示姓名）
const userList = ref<User[]>([])
const userMap = computed(() => {
  const map = new Map<number, string>()
  userList.value.forEach(u => {
    map.set(u.id, u.realName || u.userName || `用户${u.id}`)
  })
  return map
})

// 通过 ID 获取用户姓名
const getUserName = (id?: number | null): string => {
  if (!id) return '-'
  return userMap.value.get(id) ?? `用户${id}`
}

// 详情弹窗响应式宽度
const dialogWidth = ref(window.innerWidth < 768 ? '95vw' : '720px')
const handleResize = () => {
  dialogWidth.value = window.innerWidth < 768 ? '95vw' : '720px'
}

// 今日汇总数据
const todaySummary = ref<TodaySummary>({
  date: '',
  totalRevenue: 0,
  totalRefund: 0,
  totalStoredValueRecharge: 0,
  totalStoredValueConsume: 0,
  orderCount: 0,
  isSettled: false,
  netRevenue: 0,
  isRefundExceedRevenue: false
})

// 今日已存在的日结记录（用于判断状态与按钮显示）
const todaySettlement = ref<DailySettlement | null>(null)

// 搜索表单
const searchForm = reactive({
  dateRange: [] as string[],
  status: undefined as SettlementStatus | undefined
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<DailySettlement[]>([])

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

// 汇总日结弹窗
const summarizeDialogVisible = ref(false)
const settleLoading = ref(false)
const summarizeRemark = ref('')

// 确认日结弹窗
const confirmDialogVisible = ref(false)
const confirmLoading = ref(false)
const confirmTarget = ref<DailySettlement | null>(null)
const validationWarnings = ref<string[]>([])

// 反日结弹窗
const reverseDialogVisible = ref(false)
const reverseLoading = ref(false)
const reverseTarget = ref<DailySettlement | null>(null)
const reverseReason = ref('')

// 重算 loading
const recalcLoading = ref(false)

// 详情弹窗
const detailDialogVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<DailySettlement | null>(null)

// 加载今日汇总
const loadTodaySummary = async () => {
  try {
    todaySummary.value = await getTodaySummary()
    // 若已存在日结记录，获取详情以判断状态
    if (todaySummary.value.isSettled && todaySummary.value.settlementId) {
      try {
        todaySettlement.value = await getDailySettlement(todaySummary.value.settlementId)
      } catch {
        todaySettlement.value = null
      }
    } else {
      todaySettlement.value = null
    }
  } catch {
    ElMessage.error('加载今日汇总失败')
  }
}

// 加载日结记录列表
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getDailySettlements({
      startDate: searchForm.dateRange?.[0],
      endDate: searchForm.dateRange?.[1],
      status: searchForm.status,
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    tableData.value = res.list
    pagination.total = res.total
  } catch {
    ElMessage.error('加载日结记录失败')
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
  searchForm.dateRange = []
  searchForm.status = undefined
  handleSearch()
}

// 点击汇总日结
const handleSummarize = () => {
  if (todaySummary.value.isSettled) {
    ElMessage.warning('今日已存在日结记录')
    return
  }
  summarizeRemark.value = ''
  summarizeDialogVisible.value = true
}

// 确认汇总日结
const handleSummarizeConfirm = async () => {
  settleLoading.value = true
  try {
    await executeDailySettlement({ remark: summarizeRemark.value || undefined })
    ElMessage.success('汇总成功，待确认')
    summarizeDialogVisible.value = false
    await loadTodaySummary()
    await loadData()
  } catch (error: any) {
    ElMessage.error(error.message || '汇总失败')
  } finally {
    settleLoading.value = false
  }
}

// 点击确认日结（先校验）
const handleConfirm = async (row: DailySettlement) => {
  confirmTarget.value = row
  validationWarnings.value = []
  confirmDialogVisible.value = true
  try {
    const result = await validateSettlement(row.id)
    validationWarnings.value = result.warnings
    if (!result.canConfirm) {
      // 重复日结等阻止场景，直接提示并关闭
      ElMessage.warning(result.warnings[0] || '该记录不可确认')
      confirmDialogVisible.value = false
      return
    }
  } catch (error: any) {
    ElMessage.error(error.message || '校验失败')
    confirmDialogVisible.value = false
  }
}

// 提交确认日结
const handleConfirmSubmit = async () => {
  if (!confirmTarget.value) return
  confirmLoading.value = true
  try {
    await confirmSettlement(confirmTarget.value.id)
    ElMessage.success('日结已确认')
    confirmDialogVisible.value = false
    await loadTodaySummary()
    await loadData()
  } catch (error: any) {
    ElMessage.error(error.message || '确认失败')
  } finally {
    confirmLoading.value = false
  }
}

// 重算
const handleRecalculate = async (id: number) => {
  recalcLoading.value = true
  try {
    await recalculateSettlement(id)
    ElMessage.success('已重新汇总')
    await loadTodaySummary()
    await loadData()
  } catch (error: any) {
    ElMessage.error(error.message || '重算失败')
  } finally {
    recalcLoading.value = false
  }
}

// 点击反日结
const handleReverse = (row: DailySettlement) => {
  reverseTarget.value = row
  reverseReason.value = ''
  reverseDialogVisible.value = true
}

// 提交反日结
const handleReverseSubmit = async () => {
  if (!reverseTarget.value) return
  reverseLoading.value = true
  try {
    await reverseSettlement(reverseTarget.value.id, { reason: reverseReason.value || undefined })
    ElMessage.success('已反日结')
    reverseDialogVisible.value = false
    await loadTodaySummary()
    await loadData()
  } catch (error: any) {
    ElMessage.error(error.message || '反日结失败')
  } finally {
    reverseLoading.value = false
  }
}

// 格式化日期时间
const formatDateTime = (dateStr: string): string => {
  if (!dateStr) return ''
  const dt = new Date(dateStr)
  if (isNaN(dt.getTime())) return dateStr
  const date = dt.toISOString().split('T')[0]
  const time = dt.toTimeString().split(' ')[0]
  return `${date} ${time}`
}

// 行样式：退款大于营收时高亮
const rowClassName = ({ row }: { row: DailySettlement }): string => {
  return row.isRefundExceedRevenue ? 'row-refund-exceed' : ''
}

// 打开详情弹窗
const handleViewDetail = async (row: DailySettlement) => {
  detailDialogVisible.value = true
  detailLoading.value = true
  detailData.value = null
  try {
    detailData.value = await getDailySettlement(row.id)
  } catch (error: any) {
    ElMessage.error(error.message || '加载详情失败')
    detailDialogVisible.value = false
  } finally {
    detailLoading.value = false
  }
}

// 从确认弹窗打开详情弹窗
const handleOpenDetailFromConfirm = async () => {
  if (!confirmTarget.value) return
  confirmDialogVisible.value = false
  await handleViewDetail(confirmTarget.value)
}

// 从详情弹窗触发确认
const handleConfirmFromDetail = async () => {
  if (!detailData.value) return
  const row = detailData.value
  detailDialogVisible.value = false
  await handleConfirm(row)
}

// 从详情弹窗触发反日结
const handleReverseFromDetail = async () => {
  if (!detailData.value) return
  const row = detailData.value
  detailDialogVisible.value = false
  handleReverse(row)
}

onMounted(async () => {
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  pagination.pageSize = systemConfigStore.defaultPageSize
  loadTodaySummary()
  loadData()
  // 加载用户列表用于审计信息显示姓名（仅加载一次）
  try {
    userList.value = await getUserList()
  } catch {
    // 加载失败不影响主流程，审计信息将显示 ID
    userList.value = []
  }
  window.addEventListener('resize', handleResize)
})

onUnmounted(() => {
  window.removeEventListener('resize', handleResize)
})
</script>

<style scoped>
.daily-settlement-management {
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

/* 今日汇总卡片 */
.summary-cards {
  background: var(--bg-tertiary);
  border-radius: var(--radius-lg);
  border: 1px solid var(--border-primary);
  overflow: hidden;
}

.summary-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px 24px;
  border-bottom: 1px solid var(--border-primary);
}

.summary-title {
  display: flex;
  align-items: center;
  gap: 12px;
}

.title-text {
  font-size: 18px;
  font-weight: 600;
  color: var(--text-primary);
}

.title-date {
  font-size: 14px;
  color: var(--text-tertiary);
}

.summary-grid {
  display: grid;
  grid-template-columns: repeat(5, 1fr);
  gap: 1px;
  background: var(--border-primary);
}

.summary-item {
  background: var(--bg-tertiary);
  padding: 24px;
  text-align: center;
}

.summary-label {
  font-size: 13px;
  color: var(--text-tertiary);
  margin-bottom: 8px;
}

.summary-value {
  font-size: 24px;
  font-weight: 700;
  color: var(--text-primary);
}

.summary-value.revenue {
  color: var(--el-color-success);
}

.summary-value.refund {
  color: var(--el-color-danger);
}

.summary-value.recharge {
  color: var(--el-color-primary);
}

.summary-value.consume {
  color: var(--el-color-warning);
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

.toolbar-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
}

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}

/* 金额文本 */
.amount-text {
  font-weight: 600;
  color: var(--text-primary);
}

.amount-text.danger {
  color: var(--el-color-danger);
}

.amount-text.success {
  color: var(--el-color-success);
}

/* 净营收为负数时的颜色 */
.summary-value.net-negative {
  color: var(--el-color-danger);
}

/* 退款率颜色 */
.ratio-danger {
  color: var(--el-color-danger);
  font-weight: 600;
}

.ratio-warning {
  color: var(--el-color-warning);
}

/* 退款大于营收异常提示条 */
.refund-exceed-tip {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 24px;
  background: var(--el-color-danger-light-9);
  color: var(--el-color-danger);
  font-size: 13px;
  border-top: 1px solid var(--el-color-danger-light-7);
}

/* 行高亮：退款大于营收 */
:deep(.el-table__row.row-refund-exceed > td.el-table__cell) {
  background-color: var(--el-color-danger-light-9) !important;
}

/* 确认弹窗内"查看完整明细"链接 */
.view-detail-link {
  text-align: center;
}

/* 详情弹窗 */
.detail-loading {
  min-height: 200px;
}

.detail-skeleton {
  padding: 0 12px;
}

.skeleton-section {
  margin-top: 20px;
}

.detail-content {
  max-height: 60vh;
  overflow-y: auto;
}

.detail-section {
  margin-bottom: 20px;
}

.detail-section:last-child {
  margin-bottom: 0;
}

.section-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
  padding: 8px 12px;
  background: var(--el-fill-color-light);
  border-left: 3px solid var(--el-color-primary);
  border-radius: var(--el-border-radius-base);
  margin-bottom: 12px;
}

.section-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 12px 24px;
  padding: 0 12px;
}

.detail-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 13px;
  line-height: 1.8;
}

.detail-item-full {
  grid-column: 1 / -1;
}

.detail-label {
  color: var(--text-tertiary);
}

.detail-value {
  color: var(--text-primary);
  font-weight: 500;
}

.detail-value.danger {
  color: var(--el-color-danger);
}

.detail-value.success {
  color: var(--el-color-success);
}

/* 防漏单警告列表 */
.warning-list {
  padding: 12px 16px;
  background: var(--el-fill-color-light);
  border-radius: var(--el-border-radius-base);
}

.warning-item {
  font-size: 13px;
  color: var(--el-color-warning);
  line-height: 1.8;
}

.confirm-tip {
  font-size: 13px;
  color: var(--text-tertiary);
}
</style>
