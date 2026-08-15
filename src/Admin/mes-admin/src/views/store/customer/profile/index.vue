<template>
  <div class="customer-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="客户姓名">
            <el-input
              v-model="searchForm.name"
              placeholder="请输入客户姓名"
              clearable
              style="width: 180px"
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
          <el-form-item label="客户等级">
            <el-select v-model="searchForm.levelId" placeholder="全部" clearable style="width: 140px">
              <el-option
                v-for="level in customerLevels"
                :key="level.id"
                :label="level.name"
                :value="level.id"
              />
            </el-select>
          </el-form-item>
          <el-form-item label="性别">
            <el-select v-model="searchForm.gender" placeholder="全部" clearable style="width: 120px">
              <el-option label="男" :value="1" />
              <el-option label="女" :value="2" />
            </el-select>
          </el-form-item>
          <el-form-item label="标签">
            <el-select v-model="searchForm.tagId" placeholder="全部" clearable style="width: 140px">
              <el-option
                v-for="tag in allTags"
                :key="tag.id"
                :label="tag.name"
                :value="tag.id"
              />
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
        <el-button type="primary" @click="handleAdd()">
          <el-icon><Plus /></el-icon>
          新增客户
        </el-button>
        <el-button
          type="danger"
          :disabled="selectedRows.length === 0"
          @click="handleBatchDelete"
        >
          <el-icon><Delete /></el-icon>
          批量删除
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
        @selection-change="handleSelectionChange"
        style="width: 100%"
      >
        <el-table-column type="selection" width="50" />
        <el-table-column label="客户姓名" min-width="110">
          <template #default="{ row }">
            <div class="customer-name">
              <div class="customer-avatar" :class="row.gender === 2 ? 'female' : 'male'">
                {{ row.name.charAt(0) }}
              </div>
              <span class="name-text">{{ row.name }}</span>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="phone" label="手机号" width="130" />
        <el-table-column prop="gender" label="性别" width="70">
          <template #default="{ row }">
            {{ genderText(row.gender) }}
          </template>
        </el-table-column>
        <el-table-column prop="levelName" label="等级" width="90">
          <template #default="{ row }">
            <el-tag v-if="row.levelName" type="warning" size="small" effect="dark">
              {{ row.levelName }}
            </el-tag>
            <span v-else class="text-tertiary">普通客户</span>
          </template>
        </el-table-column>
        <el-table-column label="标签" min-width="150">
          <template #default="{ row }">
            <template v-if="row.tags && row.tags.length > 0">
              <el-tag
                v-for="tag in row.tags"
                :key="tag.id"
                :type="(tag.color as any) || 'primary'"
                size="small"
                effect="light"
                style="margin-right: 4px; margin-bottom: 2px;"
              >
                {{ tag.name }}
              </el-tag>
            </template>
            <span v-else class="text-tertiary">-</span>
          </template>
        </el-table-column>
        <el-table-column prop="totalPoints" label="积分" width="90" align="right">
          <template #default="{ row }">
            <span class="points-text">{{ row.totalPoints }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="balance" label="余额" width="110" align="right">
          <template #default="{ row }">
            <span class="balance-text">¥{{ formatPrice(row.balance) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="totalConsume" label="累计消费" width="110" align="right">
          <template #default="{ row }">
            ¥{{ formatPrice(row.totalConsume) }}
          </template>
        </el-table-column>
        <el-table-column prop="lastConsumeTime" label="最后消费" width="150">
          <template #default="{ row }">
            {{ formatDateTime(row.lastConsumeTime) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="280" fixed="right">
          <template #default="{ row }">
            <el-button link type="info" size="small" @click="handleViewDetail(row)">
              <el-icon><View /></el-icon>
              详情
            </el-button>
            <el-button link type="primary" size="small" @click="handleEdit(row)">
              <el-icon><Edit /></el-icon>
              编辑
            </el-button>
            <el-button link type="danger" size="small" @click="handleDelete(row)">
              <el-icon><Delete /></el-icon>
              删除
            </el-button>
            <el-button link type="danger" size="small" @click="handlePermanentDelete(row)">
              <el-icon><WarnTriangleFilled /></el-icon>
              永久删除
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

    <!-- 新增/编辑弹窗 -->
    <el-dialog
      v-model="dialogVisible"
      :title="isEdit ? '编辑客户' : '新增客户'"
      width="600px"
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
            <el-form-item label="客户姓名" prop="name">
              <el-input v-model="formData.name" placeholder="请输入客户姓名" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="手机号" prop="phone">
              <el-input v-model="formData.phone" placeholder="请输入手机号" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="性别" prop="gender">
              <el-radio-group v-model="formData.gender">
                <el-radio :value="1">男</el-radio>
                <el-radio :value="2">女</el-radio>
              </el-radio-group>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="生日" prop="birthday">
              <el-date-picker
                v-model="formData.birthday"
                type="date"
                placeholder="请选择生日"
                format="YYYY-MM-DD"
                value-format="YYYY-MM-DD"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="客户等级" prop="levelId">
          <el-select v-model="formData.levelId" placeholder="请选择客户等级" clearable style="width: 100%">
            <el-option
              v-for="level in customerLevels"
              :key="level.id"
              :label="formatLevelLabel(level)"
              :value="level.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="客户标签" prop="selectedTags">
          <el-select
            v-model="formData.selectedTags"
            multiple
            filterable
            allow-create
            default-first-option
            placeholder="选择已有标签或输入新标签"
            style="width: 100%"
          >
            <el-option
              v-for="tag in allTags"
              :key="tag.id"
              :label="tag.name"
              :value="tag.id"
            />
          </el-select>
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item prop="authorizationStatus">
              <template #label>
                <span>授权状态</span>
                <el-tooltip placement="top" effect="light">
                  <template #content>
                    <div style="max-width: 300px; line-height: 1.7; color: #303133;">
                      <div style="font-weight: 600; margin-bottom: 4px;">客户个人信息授权（依据《个人信息保护法》）</div>
                      <div>· 未授权：尚未取得客户签字授权</div>
                      <div>· 已授权：客户已在会员卡申请表/服务协议/消费单据中签字确认</div>
                      <div>· 已撤回：客户已撤回授权</div>
                      <div style="margin-top: 6px; font-size: 12px; color: #606266;">由门店人员根据线下签署文件勾选确认</div>
                    </div>
                  </template>
                  <el-icon class="auth-tip-icon"><QuestionFilled /></el-icon>
                </el-tooltip>
              </template>
              <el-select v-model="formData.authorizationStatus" placeholder="请选择授权状态" style="width: 100%" @change="handleAuthorizationStatusChange">
                <el-option label="未授权" :value="0" />
                <el-option label="已授权" :value="1" />
                <el-option label="已撤回" :value="2" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="授权时间" prop="authorizationTime">
              <el-date-picker
                v-model="formData.authorizationTime"
                type="datetime"
                placeholder="请选择授权时间"
                format="YYYY-MM-DD HH:mm"
                value-format="YYYY-MM-DDTHH:mm:ss"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="地址" prop="address">
          <el-input v-model="formData.address" placeholder="请输入地址" />
        </el-form-item>
        <el-form-item label="备注" prop="remark">
          <el-input v-model="formData.remark" type="textarea" :rows="3" placeholder="请输入备注信息" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          确定
        </el-button>
      </template>
    </el-dialog>

    <!-- 客户详情弹窗 -->
    <el-dialog v-model="detailVisible" title="客户详情" width="640px">
      <el-descriptions v-if="detailData" :column="2" border class="detail-desc">
        <el-descriptions-item label="客户姓名">
          {{ detailData.name }}
        </el-descriptions-item>
        <el-descriptions-item label="手机号">
          {{ detailData.phone }}
        </el-descriptions-item>
        <el-descriptions-item label="性别">
          {{ genderText(detailData.gender) }}
        </el-descriptions-item>
        <el-descriptions-item label="生日">
          {{ formatDate(detailData.birthday) }}
        </el-descriptions-item>
        <el-descriptions-item label="客户等级">
          <el-tag v-if="detailData.levelName" type="warning" size="small" effect="dark">
            {{ detailData.levelName }}
          </el-tag>
          <span v-else class="text-tertiary">普通客户</span>
        </el-descriptions-item>
        <el-descriptions-item label="授权状态">
          <el-tag :type="authorizationStatusType(detailData.authorizationStatus)" size="small">
            {{ authorizationStatusText(detailData.authorizationStatus) }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="标签" :span="2">
          <template v-if="detailData.tags && detailData.tags.length > 0">
            <el-tag
              v-for="tag in detailData.tags"
              :key="tag.id"
              :type="(tag.color as any) || 'primary'"
              size="small"
              effect="light"
              style="margin-right: 4px;"
            >
              {{ tag.name }}
            </el-tag>
          </template>
          <span v-else class="text-tertiary">-</span>
        </el-descriptions-item>
        <el-descriptions-item label="积分">
          <span class="points-text">{{ detailData.totalPoints }}</span>
          <el-button link type="primary" size="small" class="detail-inline-btn" @click="handleOpenPointsAdjust">
            手动调整
          </el-button>
        </el-descriptions-item>
        <el-descriptions-item label="余额">
          <span class="balance-text">¥{{ formatPrice(detailData.balance) }}</span>
          <el-button link type="primary" size="small" class="detail-inline-btn" @click="rechargeVisible = true">
            充值
          </el-button>
        </el-descriptions-item>
        <el-descriptions-item label="累计消费">
          ¥{{ formatPrice(detailData.totalConsume) }}
        </el-descriptions-item>
        <el-descriptions-item label="最后消费">
          {{ formatDateTime(detailData.lastConsumeTime) }}
        </el-descriptions-item>
        <el-descriptions-item label="授权时间">
          {{ formatDateTime(detailData.authorizationTime) }}
        </el-descriptions-item>
        <el-descriptions-item label="创建时间">
          {{ formatDateTime(detailData.createdAt) }}
        </el-descriptions-item>
        <el-descriptions-item label="地址" :span="2">
          {{ detailData.address || '-' }}
        </el-descriptions-item>
        <el-descriptions-item label="备注" :span="2">
          {{ detailData.remark || '-' }}
        </el-descriptions-item>
      </el-descriptions>

      <!-- 消费统计（近6个月） -->
      <div v-if="detailData" class="detail-stat-section">
        <div class="detail-stat-title">消费统计（近6个月）</div>
        <el-descriptions
          v-loading="statLoading"
          :column="2"
          border
          class="stat-desc"
          v-if="statData"
        >
          <el-descriptions-item>
            <template #label>
              <span>消费频次</span>
              <el-tooltip placement="top" effect="light">
                <template #content>
                  <div style="max-width: 300px; line-height: 1.7; color: #303133;">
                    <div>近 6 个月内已完成订单数（不含退款/取消）</div>
                    <div style="margin-top: 4px; font-size: 12px; color: #606266;">跨门店统计：包含本连锁其他门店的消费</div>
                  </div>
                </template>
                <el-icon class="stat-tip-icon"><QuestionFilled /></el-icon>
              </el-tooltip>
            </template>
            {{ statData.orderCount }} 次
          </el-descriptions-item>
          <el-descriptions-item>
            <template #label>
              <span>累计消费</span>
              <el-tooltip placement="top" effect="light">
                <template #content>
                  <div style="max-width: 300px; line-height: 1.7; color: #303133;">
                    <div>近 6 个月内已完成订单的实付金额合计</div>
                    <div style="margin-top: 4px; font-size: 12px; color: #606266;">跨门店统计：包含本连锁其他门店的消费</div>
                  </div>
                </template>
                <el-icon class="stat-tip-icon"><QuestionFilled /></el-icon>
              </el-tooltip>
            </template>
            ¥{{ formatPrice(statData.totalConsumption) }}
          </el-descriptions-item>
          <el-descriptions-item>
            <template #label>
              <span>客单价</span>
              <el-tooltip placement="top" effect="light">
                <template #content>
                  <div style="max-width: 300px; line-height: 1.7; color: #303133;">
                    <div>累计消费 ÷ 消费频次</div>
                    <div style="margin-top: 4px; font-size: 12px; color: #606266;">无订单时显示 0</div>
                  </div>
                </template>
                <el-icon class="stat-tip-icon"><QuestionFilled /></el-icon>
              </el-tooltip>
            </template>
            ¥{{ formatPrice(statData.averageOrderValue) }}
          </el-descriptions-item>
          <el-descriptions-item>
            <template #label>
              <span>最近消费</span>
              <el-tooltip placement="top" effect="light">
                <template #content>
                  <div style="max-width: 300px; line-height: 1.7; color: #303133;">
                    <div>近 6 个月内最近一次已完成订单的时间</div>
                    <div style="margin-top: 4px; font-size: 12px; color: #606266;">超过 6 个月的不统计</div>
                  </div>
                </template>
                <el-icon class="stat-tip-icon"><QuestionFilled /></el-icon>
              </el-tooltip>
            </template>
            {{ statData.lastConsumeTime ? formatDate(statData.lastConsumeTime) : '无' }}
          </el-descriptions-item>
        </el-descriptions>
        <el-empty v-else-if="!statLoading" description="暂无消费统计数据" />

        <!-- 消费偏好 -->
        <div v-if="statData" class="stat-preferences">
          <div class="stat-section-title">
            <span>消费偏好</span>
            <el-tooltip placement="top" effect="light">
              <template #content>
                <div style="max-width: 300px; line-height: 1.7; color: #303133;">
                  <div>按订单类型分组的金额占比：</div>
                  <div>· 零售单 -> 实物商品</div>
                  <div>· 服务单 -> 服务项目</div>
                  <div>· 疗程卡核销 -> 疗程卡</div>
                  <div style="margin-top: 4px; font-size: 12px; color: #606266;">仅统计近 6 个月已完成订单</div>
                </div>
              </template>
              <el-icon class="stat-tip-icon"><QuestionFilled /></el-icon>
            </el-tooltip>
          </div>
          <template v-if="statData.preferences.length > 0">
            <div v-for="item in statData.preferences" :key="item.productType" class="pref-item">
              <div class="pref-header">
                <span class="pref-name">{{ item.productTypeName }}</span>
                <span class="pref-amount">¥{{ formatPrice(item.amount) }}（{{ (item.percentage * 100).toFixed(1) }}%）</span>
              </div>
              <el-progress
                :percentage="Math.round(item.percentage * 100)"
                :show-text="false"
                :stroke-width="8"
                :color="getPrefColor(item.productType)"
              />
            </div>
          </template>
          <p v-else class="stat-empty-text">暂无消费偏好数据</p>
        </div>
      </div>
    </el-dialog>

    <!-- 手动调整积分弹窗 -->
    <el-dialog
      v-model="pointsAdjustVisible"
      title="手动调整积分"
      width="480px"
      :close-on-click-modal="false"
      append-to-body
    >
      <el-form
        ref="pointsAdjustFormRef"
        :model="pointsAdjustForm"
        :rules="pointsAdjustRules"
        label-width="100px"
      >
        <el-form-item label="客户">
          <span>{{ detailData?.name }}（{{ detailData?.phone }}）</span>
        </el-form-item>
        <el-form-item label="当前积分">
          <span class="points-text">{{ detailData?.totalPoints }}</span>
        </el-form-item>
        <el-form-item label="变动积分" prop="points">
          <el-input-number
            v-model="pointsAdjustForm.points"
            :step="1"
            controls-position="right"
            style="width: 200px"
          />
          <div class="form-tip">正数增加，负数扣减（扣减后积分不能为负）</div>
        </el-form-item>
        <el-form-item label="调整后积分">
          <span class="points-text">{{ (detailData?.totalPoints ?? 0) + pointsAdjustForm.points }}</span>
        </el-form-item>
        <el-form-item label="原因备注" prop="remark">
          <el-input
            v-model="pointsAdjustForm.remark"
            type="textarea"
            :rows="3"
            placeholder="请输入调整原因"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="pointsAdjustVisible = false">取消</el-button>
        <el-button type="primary" :loading="pointsAdjustLoading" @click="handleSubmitPointsAdjust">
          确定
        </el-button>
      </template>
    </el-dialog>

    <!-- 永久删除确认弹窗 -->
    <el-dialog
      v-model="permanentDeleteVisible"
      title="永久删除客户档案"
      width="500px"
      :close-on-click-modal="false"
      append-to-body
    >
      <el-alert type="error" :closable="false" show-icon style="margin-bottom: 16px;">
        <template #title>
          此操作将<strong>物理删除</strong>客户及所有关联个人信息（美容档案、体型数据、服务对比照片、消费偏好、积分流水、消费记录、疗程卡销售、储值账户等），订单数据将脱敏保留。<strong>此操作不可恢复！</strong>
        </template>
      </el-alert>

      <el-form
        ref="permanentDeleteFormRef"
        :model="permanentDeleteForm"
        :rules="permanentDeleteRules"
        label-width="110px"
      >
        <el-form-item label="客户">
          <span>{{ permanentDeleteTarget?.name }}（{{ permanentDeleteTarget?.phone }}）</span>
        </el-form-item>
        <el-form-item label="确认码" prop="confirmCode">
          <el-input
            v-model="permanentDeleteForm.confirmCode"
            placeholder="请输入客户手机号后4位"
            maxlength="4"
            style="width: 200px"
          />
          <div class="form-tip">为防止误操作，需输入客户手机号后4位确认</div>
        </el-form-item>
        <el-form-item label="删除原因" prop="reason">
          <el-input
            v-model="permanentDeleteForm.reason"
            type="textarea"
            :rows="3"
            placeholder="请输入删除原因（将写入审计日志，永久保留）"
            maxlength="200"
            show-word-limit
          />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="permanentDeleteVisible = false">取消</el-button>
        <el-button type="danger" :loading="permanentDeleteLoading" @click="handleSubmitPermanentDelete">
          确认永久删除
        </el-button>
      </template>
    </el-dialog>

    <!-- 储值充值弹窗（与储值账户页共用同一组件） -->
    <RechargeDialog
      v-if="detailData"
      v-model="rechargeVisible"
      :customer-id="detailData.id"
      :customer-name="detailData.name"
      :current-balance="detailData.balance"
      @success="handleRechargeSuccess"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus, Delete, Edit, QuestionFilled, View, WarnTriangleFilled } from '@element-plus/icons-vue'
import {
  getCustomers,
  getCustomer,
  createCustomer,
  updateCustomer,
  deleteCustomer,
  deleteCustomers,
  permanentlyDeleteCustomer,
  getCustomerLevels,
  getAllCustomerTags,
  getCustomerConsumptionStat,
  createPointsLog
} from '@/api/customer'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { Customer, CustomerLevel, CustomerTag, AuthorizationStatus, CustomerConsumptionStat, CustomerPermanentDeleteDto } from '@/api/customer/types'
import RechargeDialog from '@/views/store/storedvalue/components/RechargeDialog.vue'

const systemConfigStore = useSystemConfigStore()

const route = useRoute()

// 客户等级列表
const customerLevels = ref<CustomerLevel[]>([])

// 全量客户标签列表（供弹窗下拉选择）
const allTags = ref<CustomerTag[]>([])

// 搜索表单
const searchForm = reactive({
  name: '',
  phone: '',
  levelId: undefined as number | undefined,
  tagId: undefined as number | undefined,
  gender: undefined as number | undefined
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<Customer[]>([])
const selectedRows = ref<Customer[]>([])

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

// 弹窗
const dialogVisible = ref(false)
const isEdit = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()

const formData = reactive({
  id: 0,
  name: '',
  phone: '',
  gender: undefined as number | undefined,
  birthday: '',
  levelId: undefined as number | undefined,
  // 选中的标签：string 类型统一存储已有标签 ID（后端 long 序列化为 string）和新建标签名称
  selectedTags: [] as string[],
  authorizationStatus: 0 as AuthorizationStatus,
  authorizationTime: '',
  address: '',
  remark: ''
})

const formRules: FormRules = {
  name: [
    { required: true, message: '客户姓名不能为空', trigger: 'blur' },
    { max: 50, message: '客户姓名最多50个字符', trigger: 'blur' }
  ],
  phone: [
    { required: true, message: '手机号不能为空', trigger: 'blur' },
    { pattern: /^1[3-9]\d{9}$/, message: '手机号格式不正确', trigger: 'blur' }
  ]
}

// 加载客户等级列表
const loadCustomerLevels = async () => {
  try {
    customerLevels.value = await getCustomerLevels()
  } catch (error) {
    customerLevels.value = []
  }
}

/** 格式化客户等级下拉选项标签：折扣率 >= 1 时不附加折扣说明，与客户等级列表显示保持一致 */
const formatLevelLabel = (level: CustomerLevel) => {
  if (level.discountRate >= 1) return level.name
  return `${level.name}（${(level.discountRate * 10).toFixed(1)}折）`
}

// 授权状态变更：选择已授权时若授权时间为空则默认填充当前时间
const handleAuthorizationStatusChange = (value: AuthorizationStatus) => {
  if (value === 1 && !formData.authorizationTime) {
    const now = new Date()
    const pad = (n: number) => n.toString().padStart(2, '0')
    formData.authorizationTime = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}T${pad(now.getHours())}:${pad(now.getMinutes())}:${pad(now.getSeconds())}`
  }
}

// 加载全量客户标签列表
const loadAllTags = async () => {
  try {
    allTags.value = await getAllCustomerTags()
  } catch (error) {
    allTags.value = []
  }
}

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getCustomers({
      name: searchForm.name || undefined,
      phone: searchForm.phone || undefined,
      levelId: searchForm.levelId,
      tagId: searchForm.tagId,
      gender: searchForm.gender,
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
  searchForm.name = ''
  searchForm.phone = ''
  searchForm.levelId = undefined
  searchForm.tagId = undefined
  searchForm.gender = undefined
  handleSearch()
}

// 重置表单数据
const resetFormData = () => {
  formData.id = 0
  formData.name = ''
  formData.phone = ''
  formData.gender = undefined
  formData.birthday = ''
  formData.levelId = undefined
  formData.selectedTags = []
  formData.authorizationStatus = 0
  formData.authorizationTime = ''
  formData.address = ''
  formData.remark = ''
}

// 新增
const handleAdd = () => {
  isEdit.value = false
  resetFormData()
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: Customer) => {
  isEdit.value = true
  formData.id = row.id
  formData.name = row.name
  formData.phone = row.phone
  formData.gender = row.gender
  formData.birthday = row.birthday || ''
  formData.levelId = row.levelId
  formData.selectedTags = row.tags ? row.tags.map(t => t.id) : []
  formData.authorizationStatus = row.authorizationStatus ?? 0
  formData.authorizationTime = row.authorizationTime || ''
  formData.address = row.address || ''
  formData.remark = row.remark || ''
  dialogVisible.value = true
}

// 删除
const handleDelete = async (row: Customer) => {
  try {
    await ElMessageBox.confirm(`确定要删除客户 "${row.name}" 吗？此操作不可恢复！`, '警告', {
      type: 'warning',
      confirmButtonText: '确定删除',
      cancelButtonText: '取消'
    })
    await deleteCustomer(row.id)
    ElMessage.success('删除成功')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

// ==================== 永久删除（物理删除） ====================
const permanentDeleteVisible = ref(false)
const permanentDeleteLoading = ref(false)
const permanentDeleteFormRef = ref<FormInstance>()
const permanentDeleteTarget = ref<Customer | null>(null)
const permanentDeleteForm = reactive<CustomerPermanentDeleteDto>({
  confirmCode: '',
  reason: ''
})
const permanentDeleteRules: FormRules = {
  confirmCode: [
    { required: true, message: '请输入客户手机号后4位', trigger: 'blur' },
    { len: 4, message: '确认码必须为4位', trigger: 'blur' }
  ],
  reason: [{ required: true, message: '请输入删除原因', trigger: 'blur' }]
}

// 打开永久删除弹窗
const handlePermanentDelete = (row: Customer) => {
  permanentDeleteTarget.value = row
  permanentDeleteForm.confirmCode = ''
  permanentDeleteForm.reason = ''
  permanentDeleteVisible.value = true
}

// 提交永久删除
const handleSubmitPermanentDelete = async () => {
  if (!permanentDeleteFormRef.value || !permanentDeleteTarget.value) return
  await permanentDeleteFormRef.value.validate(async (valid) => {
    if (!valid) return
    // 前端二次确认：校验确认码是否匹配客户手机号后4位，避免无效请求到后端
    const phoneLast4 = permanentDeleteTarget.value!.phone.slice(-4)
    if (permanentDeleteForm.confirmCode !== phoneLast4) {
      ElMessage.error('确认码不匹配，请输入客户手机号后4位')
      return
    }
    permanentDeleteLoading.value = true
    try {
      await permanentlyDeleteCustomer(permanentDeleteTarget.value!.id, {
        confirmCode: permanentDeleteForm.confirmCode,
        reason: permanentDeleteForm.reason
      })
      ElMessage.success('客户档案已永久删除')
      permanentDeleteVisible.value = false
      loadData()
    } catch (error: any) {
      ElMessage.error(error.message || '永久删除失败')
    } finally {
      permanentDeleteLoading.value = false
    }
  })
}

// ==================== 消费统计 ====================
const statLoading = ref(false)
const statData = ref<CustomerConsumptionStat | null>(null)

const getPrefColor = (productType: number) => {
  const colors: Record<number, string> = { 1: '#409eff', 2: '#67c23a', 4: '#e6a23c' }
  return colors[productType] || '#909399'
}

// ==================== 客户详情 ====================
const detailVisible = ref(false)
const detailData = ref<Customer | null>(null)

// 打开详情弹窗并加载消费统计
const handleViewDetail = async (row: Customer) => {
  detailData.value = row
  detailVisible.value = true
  statLoading.value = true
  statData.value = null
  try {
    statData.value = await getCustomerConsumptionStat(row.id)
  } catch (error: any) {
    ElMessage.error(error.message || '加载消费统计失败')
  } finally {
    statLoading.value = false
  }
}

/** 通过客户ID打开详情弹窗（站内信跳转等场景） */
const openCustomerDetailById = async (customerId: number) => {
  try {
    const customer = await getCustomer(customerId)
    await handleViewDetail(customer)
  } catch (error: any) {
    ElMessage.error(error.message || '加载客户详情失败')
  }
}

// ==================== 储值充值 ====================
const rechargeVisible = ref(false)

/**
 * 充值成功后就地更新详情弹窗余额并刷新列表
 * 余额为实收 + 赠送之和，与后端入账口径一致
 */
const handleRechargeSuccess = (payload: { amount: number; giftAmount: number }) => {
  if (detailData.value) {
    detailData.value.balance += payload.amount + payload.giftAmount
  }
  loadData()
}

// ==================== 手动调整积分 ====================
const pointsAdjustVisible = ref(false)
const pointsAdjustLoading = ref(false)
const pointsAdjustFormRef = ref<FormInstance>()
const pointsAdjustForm = reactive({
  points: 0,
  remark: ''
})
const pointsAdjustRules: FormRules = {
  points: [
    { required: true, message: '变动积分不能为空', trigger: 'blur' },
    {
      validator: (_rule: any, value: number, callback: any) => {
        if (value === 0) return callback(new Error('变动积分不能为0'))
        if (detailData.value && detailData.value.totalPoints + value < 0)
          return callback(new Error('扣减后积分不能为负'))
        callback()
      },
      trigger: 'blur'
    }
  ],
  remark: [
    { required: true, message: '调整原因不能为空', trigger: 'blur' }
  ]
}

// 打开手动调整弹窗
const handleOpenPointsAdjust = () => {
  pointsAdjustForm.points = 0
  pointsAdjustForm.remark = ''
  pointsAdjustVisible.value = true
}

// 提交手动调整
const handleSubmitPointsAdjust = async () => {
  if (!pointsAdjustFormRef.value || !detailData.value) return
  await pointsAdjustFormRef.value.validate(async (valid) => {
    if (!valid) return
    pointsAdjustLoading.value = true
    try {
      const currentPoints = detailData.value!.totalPoints
      await createPointsLog({
        customerId: detailData.value!.id,
        type: 8,
        points: pointsAdjustForm.points,
        beforePoints: currentPoints,
        afterPoints: currentPoints + pointsAdjustForm.points,
        remark: pointsAdjustForm.remark
      })
      ElMessage.success('积分调整成功')
      pointsAdjustVisible.value = false
      // 更新详情弹窗中的积分显示
      detailData.value!.totalPoints += pointsAdjustForm.points
      // 刷新表格数据
      loadData()
    } catch (error: any) {
      ElMessage.error(error.message || '积分调整失败')
    } finally {
      pointsAdjustLoading.value = false
    }
  })
}

// 授权状态文本
const authorizationStatusText = (status: AuthorizationStatus) => {
  const map: Record<AuthorizationStatus, string> = { 0: '未授权', 1: '已授权', 2: '已撤回' }
  return map[status] || '未授权'
}

// 授权状态标签类型
const authorizationStatusType = (status: AuthorizationStatus) => {
  const map: Record<AuthorizationStatus, string> = { 0: 'info', 1: 'success', 2: 'danger' }
  return map[status] || 'info'
}

// 批量删除
const handleBatchDelete = async () => {
  if (selectedRows.value.length === 0) return
  try {
    await ElMessageBox.confirm(`确定要删除选中的 ${selectedRows.value.length} 个客户吗？此操作不可恢复！`, '警告', {
      type: 'warning',
      confirmButtonText: '确定删除',
      cancelButtonText: '取消'
    })
    const ids = selectedRows.value.map(row => row.id)
    await deleteCustomers(ids)
    ElMessage.success('批量删除成功')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

// 提交表单
const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (valid) {
      submitLoading.value = true
      try {
        // 分离选中标签：通过 allTags 集合区分已有标签 ID 和新建标签名
        // 后端 long 序列化为 string，不能用 typeof 区分（ID 和标签名都是 string）
        // String(t.id) 统一转为 string，避免 CustomerTag.id 声明为 number 的类型不一致
        const existingTagIds = new Set(allTags.value.map(t => String(t.id)))
        const tagIds = formData.selectedTags.filter(t => existingTagIds.has(t))
        const newTagNames = formData.selectedTags.filter(t => !existingTagIds.has(t))
        const payload = {
          name: formData.name,
          phone: formData.phone,
          gender: formData.gender ?? 0,
          birthday: formData.birthday || undefined,
          levelId: formData.levelId || undefined,
          tagIds: tagIds.length > 0 ? tagIds : undefined,
          newTagNames: newTagNames.length > 0 ? newTagNames : undefined,
          authorizationStatus: formData.authorizationStatus,
          authorizationTime: formData.authorizationTime || undefined,
          address: formData.address || undefined,
          remark: formData.remark || undefined
        }
        if (isEdit.value) {
          await updateCustomer({ ...payload, id: formData.id })
          ElMessage.success('更新成功')
        } else {
          await createCustomer(payload)
          ElMessage.success('创建成功')
        }
        dialogVisible.value = false
        loadData()
      } catch (error: any) {
        ElMessage.error(error.message || '操作失败')
      } finally {
        submitLoading.value = false
      }
    }
  })
}

// 选择行
const handleSelectionChange = (rows: Customer[]) => {
  selectedRows.value = rows
}

// 性别文本
const genderText = (gender: number) => {
  const map: Record<number, string> = { 0: '未知', 1: '男', 2: '女' }
  return map[gender] || '未知'
}

// 格式化价格
const formatPrice = (price: number) => {
  if (price === null || price === undefined) return '0.00'
  return price.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ',')
}

// 格式化日期（仅日期）
const formatDate = (dateStr?: string) => {
  if (!dateStr) return '-'
  const date = new Date(dateStr)
  return date.toLocaleDateString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  })
}

// 格式化日期时间
const formatDateTime = (dateStr?: string) => {
  if (!dateStr) return '-'
  const date = new Date(dateStr)
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  })
}

onMounted(async () => {
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  pagination.pageSize = systemConfigStore.defaultPageSize
  loadCustomerLevels()
  loadAllTags()
  await loadData()

  // 支持通过 URL 参数 customerId 直接打开客户详情弹窗（站内信跳转场景）
  const customerIdParam = route.query.customerId
  if (customerIdParam) {
    await openCustomerDetailById(Number(customerIdParam))
  }
})
</script>

<style scoped>
.customer-management {
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

.search-form-inline :deep(.el-form-item) {
  margin-right: 0;
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

/* 客户姓名显示 */
.customer-name {
  display: flex;
  align-items: center;
  gap: 10px;
}

.customer-avatar {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 13px;
  font-weight: 600;
  flex-shrink: 0;
}

.customer-avatar.male {
  background: linear-gradient(135deg, #5b9bff, #3b82f6);
  color: #fff;
}

.customer-avatar.female {
  background: linear-gradient(135deg, #f97373, #ec4899);
  color: #fff;
}

.name-text {
  font-weight: 500;
  color: var(--text-primary);
}

.text-tertiary {
  color: var(--text-tertiary);
}

.points-text {
  color: var(--warning);
  font-weight: 600;
  font-family: 'JetBrains Mono', monospace;
}

.balance-text {
  color: var(--success);
  font-weight: 600;
  font-family: 'JetBrains Mono', monospace;
}

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}

/* 详情弹窗 - 消费统计区块 */
.detail-stat-section {
  margin-top: 20px;
}

.detail-stat-title {
  font-size: 15px;
  font-weight: 600;
  color: #1f2937;
  margin-bottom: 12px;
  padding-bottom: 8px;
  border-bottom: 1px solid #e5e7eb;
}

.stat-desc :deep(.el-descriptions__label) {
  width: 100px;
}

.stat-desc :deep(.el-descriptions__label .stat-tip-icon) {
  margin-left: 4px;
  vertical-align: middle;
  position: relative;
  top: -8px;
}

.stat-preferences {
  margin-top: 20px;
}

.stat-section-title {
  font-size: 14px;
  font-weight: 600;
  color: #1f2937;
  margin-bottom: 12px;
}

.stat-empty-text {
  color: #9ca3af;
  font-size: 13px;
  text-align: center;
  padding: 8px 0;
  margin: 0;
}

.pref-item {
  margin-bottom: 12px;
}

.pref-header {
  display: flex;
  justify-content: space-between;
  margin-bottom: 4px;
  font-size: 13px;
}

.pref-name {
  color: #1f2937;
}

.pref-amount {
  color: var(--text-tertiary);
}

.auth-tip-icon {
  margin-left: 4px;
  color: var(--text-tertiary);
  cursor: help;
  vertical-align: middle;
}

.stat-tip-icon {
  margin-left: 4px;
  color: var(--text-tertiary);
  cursor: help;
  vertical-align: middle;
}

/* 客户详情弹窗 */
.detail-desc :deep(.el-descriptions__label) {
  width: 100px;
}

.detail-inline-btn {
  margin-left: 8px;
}

.form-tip {
  font-size: 12px;
  color: var(--text-tertiary);
  line-height: 1.5;
  margin-top: 4px;
}
</style>
