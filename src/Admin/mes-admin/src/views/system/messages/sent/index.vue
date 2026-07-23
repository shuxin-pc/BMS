<template>
  <div class="message-sent">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <el-form :inline="true" :model="searchForm" class="search-form">
          <el-form-item label="归属租户" v-if="isSuperAdmin">
            <el-select v-model="searchForm.tenantId" placeholder="请选择租户" clearable filterable style="width: 150px">
              <el-option label="全部" value="" />
              <el-option
                v-for="t in searchTenantOptions"
                :key="t.id"
                :label="t.name"
                :value="t.id"
              />
            </el-select>
          </el-form-item>
          <el-form-item label="消息分类">
            <el-select v-model="searchForm.category" placeholder="请选择分类" clearable style="width: 110px">
              <el-option label="全部" value="" />
              <el-option label="系统消息" :value="MessageCategory.System" />
              <el-option label="业务通知" :value="MessageCategory.Business" />
              <el-option label="公告信息" :value="MessageCategory.Announcement" />
            </el-select>
          </el-form-item>
          <el-form-item label="来源">
            <el-select v-model="searchForm.sourceType" placeholder="请选择" clearable style="width: 100px">
              <el-option label="全部" value="" />
              <el-option label="自动" :value="MessageSourceType.Auto" />
              <el-option label="手动" :value="MessageSourceType.Manual" />
            </el-select>
          </el-form-item>
          <el-form-item label="状态">
            <el-select v-model="searchForm.isRecalled" placeholder="请选择" clearable style="width: 100px">
              <el-option label="全部" value="" />
              <el-option label="正常" :value="false" />
              <el-option label="已撤回" :value="true" />
            </el-select>
          </el-form-item>
          <el-form-item label="发送时间">
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
          <el-form-item class="search-buttons">
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

    <!-- 操作栏 -->
    <div class="table-toolbar">
      <div class="toolbar-left">
        <el-button type="primary" @click="handleOpenSend" v-if="hasPermission('system:message:sent:send')">
          <el-icon><Promotion /></el-icon>
          发送消息
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
        <el-table-column prop="title" label="标题" min-width="200" show-overflow-tooltip />
        <el-table-column prop="categoryName" label="分类" width="100">
          <template #default="{ row }">
            <el-tag :type="getCategoryTagType(row.category)" size="small" effect="plain">
              {{ row.categoryName }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="sourceTypeName" label="来源" width="80">
          <template #default="{ row }">
            <el-tag :type="row.sourceType === MessageSourceType.Auto ? 'info' : 'success'" size="small">
              {{ row.sourceTypeName }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="sourceSubsystemName" label="来源子系统" width="120" show-overflow-tooltip />
        <el-table-column prop="targetTypeName" label="目标类型" width="100" />
        <el-table-column prop="targetDesc" label="目标描述" min-width="150" show-overflow-tooltip />
        <el-table-column prop="tenantName" label="归属租户" width="140" show-overflow-tooltip />
        <el-table-column prop="senderName" label="发送人" width="100" show-overflow-tooltip />
        <el-table-column prop="isRecalled" label="状态" width="80">
          <template #default="{ row }">
            <el-tag :type="row.isRecalled ? 'danger' : 'success'" size="small" effect="dark">
              {{ row.isRecalled ? '已撤回' : '正常' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="createdTime" label="发送时间" width="170">
          <template #default="{ row }">
            {{ formatDateTime(row.createdTime) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="120" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="handleViewDetail(row)">查看</el-button>
            <el-button
              link
              type="danger"
              v-if="!row.isRecalled && hasPermission('system:message:sent:recall')"
              @click="handleRecall(row)"
            >
              撤回
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <div class="pagination-container">
        <el-pagination
          v-model:current-page="pagination.pageIndex"
          v-model:page-size="pagination.pageSize"
          :total="pagination.total"
          :page-sizes="[10, 20, 50, 100]"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="loadData"
          @current-change="loadData"
        />
      </div>
    </div>

    <!-- 发送消息弹窗 -->
    <el-dialog v-model="sendVisible" title="发送消息" width="640px" @closed="resetSendForm">
      <el-form
        ref="sendFormRef"
        :model="sendForm"
        :rules="sendRules"
        label-width="90px"
      >
        <el-form-item label="标题" prop="title">
          <el-input v-model="sendForm.title" placeholder="请输入消息标题" maxlength="100" show-word-limit />
        </el-form-item>
        <el-form-item label="分类" prop="category">
          <el-radio-group v-model="sendForm.category">
            <el-radio :value="MessageCategory.System">系统消息</el-radio>
            <el-radio :value="MessageCategory.Business">业务通知</el-radio>
            <el-radio :value="MessageCategory.Announcement">公告信息</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="内容" prop="content">
          <el-input
            v-model="sendForm.content"
            type="textarea"
            :rows="5"
            placeholder="请输入消息内容"
            maxlength="1000"
            show-word-limit
          />
        </el-form-item>
        <el-form-item label="目标类型" prop="targetType">
          <el-radio-group v-model="sendForm.targetType" @change="handleTargetTypeChange">
            <el-radio :value="MessageTargetType.User">指定用户</el-radio>
            <el-radio :value="MessageTargetType.Role">按角色</el-radio>
            <el-radio :value="MessageTargetType.Organization">按组织</el-radio>
            <el-radio :value="MessageTargetType.Tenant" v-if="isSuperAdmin">按租户</el-radio>
            <el-radio :value="MessageTargetType.All">全体用户</el-radio>
          </el-radio-group>
        </el-form-item>

        <!--
          目标租户（统一选择器，置于目标类型之下、目标用户/角色/组织之上）：
          - 平台管理员：所有目标类型共用，作为过滤范围（User/Role/Organization）或目标本身（Tenant/All）
          - 租户管理员：不显示，默认本租户
        -->
        <el-form-item
          label="目标租户"
          v-if="isSuperAdmin"
          prop="targetTenantIds"
        >
          <div style="display: flex; gap: 8px; width: 100%">
            <el-select
              v-model="sendForm.targetTenantIds"
              multiple
              filterable
              placeholder="请选择目标租户"
              style="flex: 1"
            >
              <el-option
                v-for="t in tenantOptions"
                :key="t.id"
                :label="t.name"
                :value="t.id"
              />
            </el-select>
            <el-button @click="selectAllTenants">全部租户</el-button>
            <el-button @click="clearTargetTenants">清空</el-button>
          </div>
        </el-form-item>

        <el-form-item
          label="目标用户"
          v-if="sendForm.targetType === MessageTargetType.User"
          prop="targetIds"
        >
          <el-select
            v-model="sendForm.targetIds"
            multiple
            filterable
            :loading="targetLoading"
            :placeholder="targetOptionPlaceholder"
            style="width: 100%"
          >
            <el-option
              v-for="u in userOptions"
              :key="u.id"
              :label="formatOptionLabel(u.tenantName, `${u.realName || u.userName}（${u.userName}）`)"
              :value="u.id"
            />
          </el-select>
        </el-form-item>

        <el-form-item
          label="目标角色"
          v-if="sendForm.targetType === MessageTargetType.Role"
          prop="targetIds"
        >
          <el-select v-model="sendForm.targetIds" multiple filterable :placeholder="targetOptionPlaceholder" style="width: 100%">
            <el-option
              v-for="r in roleOptions"
              :key="r.id"
              :label="formatOptionLabel(r.tenantName, r.name)"
              :value="r.id"
            />
          </el-select>
        </el-form-item>

        <el-form-item
          label="目标组织"
          v-if="sendForm.targetType === MessageTargetType.Organization"
          prop="targetIds"
        >
          <!--
            组织为树形结构，使用 el-tree-select 支持选中任意层级；
            check-strictly 让父子节点独立勾选，下级组织用户的展开由后端按"组织及下级"语义处理
          -->
          <el-tree-select
            v-model="sendForm.targetIds"
            :data="orgOptions"
            :props="{ label: 'name', children: 'children' }"
            node-key="id"
            multiple
            check-strictly
            filterable
            clearable
            collapse-tags
            collapse-tags-tooltip
            :placeholder="targetOptionPlaceholder"
            style="width: 100%"
          />
        </el-form-item>

        <el-form-item label="跳转链接" prop="targetUrl">
          <el-input v-model="sendForm.targetUrl" placeholder="可选，点击消息后跳转的路由路径，如 /system/users" />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="sendVisible = false">取消</el-button>
        <el-button type="primary" :loading="sending" @click="handleSend">发送</el-button>
      </template>
    </el-dialog>

    <!-- 消息详情弹窗 -->
    <el-dialog v-model="detailVisible" width="640px">
      <div v-if="currentMessage" class="message-detail">
        <h3 class="detail-title">{{ currentMessage.title }}</h3>
        <div class="detail-meta">
          <el-tag :type="getCategoryTagType(currentMessage.category)" size="small" effect="plain">
            {{ currentMessage.categoryName }}
          </el-tag>
          <span class="detail-meta-item">来源：{{ currentMessage.sourceTypeName }}</span>
          <span class="detail-meta-item" v-if="currentMessage.sourceSubsystemName">子系统：{{ currentMessage.sourceSubsystemName }}</span>
          <span class="detail-meta-item">目标：{{ currentMessage.targetTypeName }}</span>
          <span class="detail-meta-item">发送人：{{ currentMessage.senderName }}</span>
          <span class="detail-meta-item">时间：{{ formatDateTime(currentMessage.createdTime) }}</span>
          <span class="detail-meta-item">状态：{{ currentMessage.isRecalled ? '已撤回' : '正常' }}</span>
        </div>
        <el-divider />
        <div class="detail-content">{{ currentMessage.content }}</div>

        <!-- 已读/未读统计（撤回消息不展示，也不调用统计接口） -->
        <template v-if="!currentMessage.isRecalled">
          <el-divider />
          <div class="read-stats">
            <div v-if="readStatsLoading" class="stats-loading">统计加载中...</div>
            <div v-else-if="readStatsError" class="stats-error">{{ readStatsError }}</div>
            <template v-else-if="readStats">
              <div class="stats-cards">
                <div class="stat-card stat-card--success">
                  <span class="stat-value">{{ readStats.readCount }}</span>
                  <span class="stat-label">已读</span>
                </div>
                <div class="stat-card stat-card--danger">
                  <span class="stat-value">{{ readStats.unreadCount }}</span>
                  <span class="stat-label">未读</span>
                </div>
                <div class="stat-card stat-card--primary">
                  <span class="stat-value">{{ readStats.totalCount }}</span>
                  <span class="stat-label">总计</span>
                </div>
                <div class="stat-card stat-card--warning">
                  <span class="stat-value">{{ readRateText }}</span>
                  <span class="stat-label">已读率</span>
                </div>
              </div>
              <div class="unread-section">
                <div class="unread-title">未读人员</div>
                <div v-if="readStats.unreadList.length === 0" class="unread-empty">暂无未读人员</div>
                <div v-else class="unread-list">
                  <el-tag
                    v-for="u in readStats.unreadList"
                    :key="u.userId"
                    size="small"
                    type="info"
                  >
                    {{ u.realName }}
                  </el-tag>
                </div>
                <div v-if="readStats.unreadCount > readStats.unreadList.length" class="unread-more">
                  仅显示前 {{ readStats.unreadList.length }} 人，共 {{ readStats.unreadCount }} 人未读
                </div>
              </div>
            </template>
          </div>
        </template>
      </div>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed, watch } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Promotion } from '@element-plus/icons-vue'
import { getSentList, sendMessage, recallMessage, getReadStats } from '@/api/message'
import {
  MessageCategory,
  MessageTargetType,
  MessageSourceType,
  type MessageSentItem,
  type MessageSentQuery,
  type MessageSendDto,
  type MessageReadStats
} from '@/api/message/types'
import { useUserStore } from '@/stores/user'
import { getUserList, getAllRoles, getOrganizations, getTenants } from '@/api/system'

const userStore = useUserStore()
const isSuperAdmin = computed(() => userStore.isSuperAdmin)
const hasPermission = (permissionCode: string) => userStore.hasPermission(permissionCode)

const tableLoading = ref(false)
const tableData = ref<MessageSentItem[]>([])
const sendVisible = ref(false)
const detailVisible = ref(false)
const currentMessage = ref<MessageSentItem | null>(null)
const sending = ref(false)
const targetLoading = ref(false)

// 详情弹窗已读统计
const readStats = ref<MessageReadStats | null>(null)
const readStatsLoading = ref(false)
const readStatsError = ref('')

const sendFormRef = ref<FormInstance>()

const searchForm = reactive({
  tenantId: '' as number | '',
  category: '' as number | '',
  sourceType: '' as number | '',
  isRecalled: '' as boolean | '',
  dateRange: [] as string[]
})

const pagination = reactive({
  pageIndex: 1,
  pageSize: 10,
  total: 0
})

// 目标选项数据（带租户归属，用于平台管理员多租户合并展示）
const userOptions = ref<Array<any & { tenantId: number; tenantName: string }>>([])
const roleOptions = ref<Array<any & { tenantId: number; tenantName: string }>>([])
const orgOptions = ref<Array<any & { tenantId: number; tenantName: string }>>([])
const tenantOptions = ref<any[]>([])
// 搜索区租户筛选下拉选项（仅超级管理员用，与发送弹窗的 tenantOptions 分离）
const searchTenantOptions = ref<any[]>([])

const sendForm = reactive({
  title: '',
  content: '',
  category: MessageCategory.System as number,
  targetType: MessageTargetType.User as number,
  targetIds: [] as number[],
  targetTenantIds: [] as number[],
  targetUrl: ''
})

/**
 * 目标选项下拉的占位提示
 */
const targetOptionPlaceholder = computed(() => {
  if (isSuperAdmin.value && sendForm.targetTenantIds.length === 0) {
    return '请先选择目标租户'
  }
  switch (sendForm.targetType) {
    case MessageTargetType.User: return '请选择用户'
    case MessageTargetType.Role: return '请选择角色'
    case MessageTargetType.Organization: return '请选择组织'
    default: return '请选择'
  }
})

const sendRules: FormRules = {
  title: [{ required: true, message: '请输入标题', trigger: 'blur' }],
  content: [{ required: true, message: '请输入内容', trigger: 'blur' }],
  category: [{ required: true, message: '请选择分类', trigger: 'change' }],
  targetType: [{ required: true, message: '请选择目标类型', trigger: 'change' }],
  targetIds: [{
    validator: (_rule: any, value: number[], callback: any) => {
      // User/Role/Organization 需要选目标；Tenant/All 不需要（targetTenantIds 即目标）
      if ([MessageTargetType.User, MessageTargetType.Role, MessageTargetType.Organization].includes(sendForm.targetType)
          && (!value || value.length === 0)) {
        callback(new Error('请选择目标'))
      } else {
        callback()
      }
    },
    trigger: 'change'
  }],
  targetTenantIds: [{
    validator: (_rule: any, value: number[], callback: any) => {
      // 平台管理员所有目标类型都需要选目标租户
      if (isSuperAdmin.value && (!value || value.length === 0)) {
        callback(new Error('请选择目标租户'))
      } else {
        callback()
      }
    },
    trigger: 'change'
  }]
}

onMounted(() => {
  loadSearchTenants()
  loadData()
})

/**
 * 加载搜索区租户下拉选项（仅超级管理员）
 */
async function loadSearchTenants() {
  if (!isSuperAdmin.value) return
  try {
    const result = await getTenants({ pageIndex: 1, pageSize: 200 })
    searchTenantOptions.value = (result as any)?.list || []
  } catch (error) {
    // 加载失败不阻塞列表查询
  }
}

/**
 * 构建查询参数
 */
function buildQuery(): MessageSentQuery {
  const query: MessageSentQuery = {
    pageIndex: pagination.pageIndex,
    pageSize: pagination.pageSize
  }
  if (isSuperAdmin.value && searchForm.tenantId !== '') {
    query.tenantId = searchForm.tenantId as number
  }
  if (searchForm.category !== '') {
    query.category = searchForm.category as number
  }
  if (searchForm.sourceType !== '') {
    query.sourceType = searchForm.sourceType as number
  }
  if (searchForm.isRecalled !== '') {
    query.isRecalled = searchForm.isRecalled as boolean
  }
  if (searchForm.dateRange && searchForm.dateRange.length === 2) {
    query.startTime = `${searchForm.dateRange[0]} 00:00:00`
    query.endTime = `${searchForm.dateRange[1]} 23:59:59`
  }
  return query
}

/**
 * 加载发送记录
 */
async function loadData() {
  tableLoading.value = true
  try {
    const result = await getSentList(buildQuery())
    tableData.value = result.list || []
    pagination.total = result.total || 0
  } catch (error: any) {
    ElMessage.error(error.message || '加载失败')
  } finally {
    tableLoading.value = false
  }
}

function handleSearch() {
  pagination.pageIndex = 1
  loadData()
}

function handleReset() {
  searchForm.tenantId = ''
  searchForm.category = ''
  searchForm.sourceType = ''
  searchForm.isRecalled = ''
  searchForm.dateRange = []
  pagination.pageIndex = 1
  loadData()
}

/**
 * 打开发送弹窗：平台管理员预加载租户列表，并按当前目标类型加载选项
 */
async function handleOpenSend() {
  sendVisible.value = true
  try {
    if (isSuperAdmin.value) {
      const tenants = await getTenants({ pageIndex: 1, pageSize: 200 })
      tenantOptions.value = (tenants as any)?.list || []
    }
    // 按当前目标类型加载选项（租户管理员直接加载本租户）
    await loadTargetOptions()
  } catch (error) {
    // 选项加载失败不阻塞弹窗
  }
}

/**
 * 当前目标类型对应的选项列表
 * 注意：组织为树形结构，需扁平化后用于校验已选 ID 是否仍有效
 */
function currentOptions(): Array<{ id: number; tenantId: number; tenantName: string }> {
  if (sendForm.targetType === MessageTargetType.User) return userOptions.value
  if (sendForm.targetType === MessageTargetType.Role) return roleOptions.value
  if (sendForm.targetType === MessageTargetType.Organization) {
    const flat: Array<{ id: number; tenantId: number; tenantName: string }> = []
    const walk = (nodes: any[]) => {
      nodes.forEach(n => {
        flat.push(n)
        if (n.children?.length) walk(n.children)
      })
    }
    walk(orgOptions.value)
    return flat
  }
  return []
}

/**
 * 按选中目标租户加载用户/角色/组织选项
 * - 平台管理员：对每个选中租户并行调用 API，合并结果，选项带租户名前缀
 * - 租户管理员：用当前租户 ID 加载，选项不加前缀
 * 加载完成后过滤已选，清除被移除租户下的项（避免提交无效 ID）
 */
async function loadTargetOptions() {
  // Tenant/All 类型无需加载用户/角色/组织
  if (sendForm.targetType === MessageTargetType.Tenant
      || sendForm.targetType === MessageTargetType.All) {
    userOptions.value = []
    roleOptions.value = []
    orgOptions.value = []
    return
  }

  // 确定要加载的租户列表
  let tenants: Array<{ id: number; name: string }>
  if (isSuperAdmin.value) {
    if (sendForm.targetTenantIds.length === 0) {
      userOptions.value = []
      roleOptions.value = []
      orgOptions.value = []
      return
    }
    tenants = tenantOptions.value
      .filter(t => sendForm.targetTenantIds.includes(t.id))
      .map(t => ({ id: t.id, name: t.name }))
  } else {
    // 租户管理员：仅本租户，名称留空（选项不加前缀）
    const tid = userStore.currentTenantId as number
    tenants = [{ id: tid, name: '' }]
  }

  targetLoading.value = true
  try {
    if (sendForm.targetType === MessageTargetType.User) {
      const results = await Promise.all(
        tenants.map(t => getUserList(t.id).then(users => (users || []).map(u => ({ ...u, tenantId: t.id, tenantName: t.name }))))
      )
      userOptions.value = results.flat()
    } else if (sendForm.targetType === MessageTargetType.Role) {
      const results = await Promise.all(
        tenants.map(t => getAllRoles(t.id).then(roles => (roles || []).map(r => ({ ...r, tenantId: t.id, tenantName: t.name }))))
      )
      roleOptions.value = results.flat()
    } else if (sendForm.targetType === MessageTargetType.Organization) {
      // 平台管理员场景：每个租户的组织树作为 forest 的一棵树合并展示，
      // 根节点 name 拼接租户名前缀，子节点保持原样（树形结构已表达归属）
      const results = await Promise.all(
        tenants.map(t => getOrganizations({ tenantId: t.id }).then(orgs => (orgs || []).map(o => ({
          ...o,
          name: isSuperAdmin.value && t.name ? `${t.name} - ${o.name}` : o.name,
          tenantId: t.id,
          tenantName: t.name
        }))))
      )
      orgOptions.value = results.flat()
    }

    // 清除被移除租户下的已选项（避免提交无效 ID）
    const validIds = new Set(currentOptions().map(o => o.id))
    sendForm.targetIds = sendForm.targetIds.filter(id => validIds.has(id))
  } catch (error) {
    // 加载失败保留空选项
  } finally {
    targetLoading.value = false
  }
}

/**
 * 选项 label 格式化：平台管理员加租户名前缀，租户管理员不加
 */
function formatOptionLabel(tenantName: string, name: string): string {
  if (isSuperAdmin.value && tenantName) {
    return `${tenantName} - ${name}`
  }
  return name
}

/**
 * 清空目标租户选择
 */
function clearTargetTenants() {
  sendForm.targetTenantIds = []
}

/**
 * 目标租户变化时：重新加载选项并清除被移除租户下的已选
 */
watch(() => sendForm.targetTenantIds, () => {
  if (!sendVisible.value) return
  loadTargetOptions()
})

/**
 * 目标类型变化时清空已选目标并重新加载选项（租户选择保留，避免重复选择）
 */
function handleTargetTypeChange() {
  sendForm.targetIds = []
  loadTargetOptions()
}

const PLATFORM_TENANT_ID = 1

/**
 * 选择全部业务租户（排除平台租户，平台租户无业务用户）
 */
function selectAllTenants() {
  sendForm.targetTenantIds = tenantOptions.value
    .filter(t => t.id !== PLATFORM_TENANT_ID)
    .map(t => t.id)
}

/**
 * 发送消息
 */
async function handleSend() {
  if (!sendFormRef.value) return
  await sendFormRef.value.validate(async (valid) => {
    if (!valid) return
    sending.value = true
    try {
      const dto: MessageSendDto = {
        title: sendForm.title,
        content: sendForm.content,
        category: sendForm.category,
        targetType: sendForm.targetType,
        // Tenant: 目标就是租户列表；All: 无目标 ID；User/Role/Organization: 目标 ID 列表
        targetIds: sendForm.targetType === MessageTargetType.Tenant
          ? sendForm.targetTenantIds
          : sendForm.targetType === MessageTargetType.All
            ? []
            : sendForm.targetIds,
        // 平台管理员且非 Tenant 时，targetTenantIds 作为跨租户范围（All 时决定全员范围）
        targetTenantIds: isSuperAdmin.value && sendForm.targetType !== MessageTargetType.Tenant
          ? sendForm.targetTenantIds
          : undefined,
        targetUrl: sendForm.targetUrl || undefined
      }
      await sendMessage(dto)
      ElMessage.success('发送成功')
      sendVisible.value = false
      loadData()
    } catch (error: any) {
      ElMessage.error(error.message || '发送失败')
    } finally {
      sending.value = false
    }
  })
}

/**
 * 重置发送表单
 */
function resetSendForm() {
  sendForm.title = ''
  sendForm.content = ''
  sendForm.category = MessageCategory.System
  sendForm.targetType = MessageTargetType.User
  sendForm.targetIds = []
  sendForm.targetTenantIds = []
  sendForm.targetUrl = ''
  userOptions.value = []
  roleOptions.value = []
  orgOptions.value = []
  sendFormRef.value?.clearValidate()
}

/**
 * 查看详情
 * 撤回消息不调用统计接口、不展示统计区；正常消息并行加载已读统计
 */
async function handleViewDetail(row: MessageSentItem) {
  currentMessage.value = row
  // 重置统计状态
  readStats.value = null
  readStatsError.value = ''
  readStatsLoading.value = false
  detailVisible.value = true

  if (row.isRecalled) return

  readStatsLoading.value = true
  try {
    readStats.value = await getReadStats(row.id)
  } catch (error: any) {
    readStatsError.value = error?.message || '统计加载失败'
  } finally {
    readStatsLoading.value = false
  }
}

/**
 * 已读率文案：分母为 0 时显示"--"避免除零
 */
const readRateText = computed(() => {
  const stats = readStats.value
  if (!stats || stats.totalCount === 0) return '--'
  return `${Math.round((stats.readCount / stats.totalCount) * 100)}%`
})

/**
 * 撤回消息
 */
async function handleRecall(row: MessageSentItem) {
  try {
    await ElMessageBox.confirm(
      '撤回后所有收件人将无法再看到该消息，确定撤回吗？',
      '撤回确认',
      {
        confirmButtonText: '确定撤回',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )
    await recallMessage(row.id)
    ElMessage.success('已撤回')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel' && error?.message) {
      ElMessage.error(error.message)
    }
  }
}

/**
 * 获取分类标签样式
 */
function getCategoryTagType(category: number): 'info' | 'warning' | 'success' {
  switch (category) {
    case MessageCategory.System: return 'info'
    case MessageCategory.Business: return 'warning'
    case MessageCategory.Announcement: return 'success'
    default: return 'info'
  }
}

/**
 * 格式化日期时间
 */
function formatDateTime(time: string): string {
  if (!time) return ''
  const date = new Date(time)
  const pad = (n: number) => n.toString().padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}`
}
</script>

<style scoped>
.message-sent {
  width: 100%;
}

/* 搜索区域 */
.search-form {
  padding: 16px 20px;
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
}

.search-form :deep(.el-form-item) {
  margin-right: 16px;
  margin-bottom: 0;
}

.search-form :deep(.el-form-item__label) {
  color: var(--text-secondary);
  font-weight: 500;
}

.search-buttons {
  margin-left: auto;
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

/* 卡片 - 科技风 */
.card {
  background: var(--bg-tertiary);
  border-radius: var(--radius-lg);
  border: 1px solid var(--border-primary);
  position: relative;
  overflow: hidden;
}

.mb-20 {
  margin-bottom: 20px;
}

.card::before {
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

.card:hover::before {
  opacity: 1;
}

/* 表格 - 科技风 */
:deep(.el-table) {
  border-radius: var(--radius-lg);
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
  font-size: 12px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  border-bottom: 1px solid var(--border-primary) !important;
}

:deep(.el-table td.el-table__cell) {
  background-color: var(--bg-tertiary) !important;
  border-bottom: 1px solid var(--border-primary) !important;
}

:deep(.el-table__row:hover > td.el-table__cell) {
  background-color: var(--bg-hover) !important;
}

/* 按钮基础样式 */
:deep(.el-button) {
  transition: all 0.3s ease;
}

:deep(.el-button + .el-button) {
  margin-left: 8px;
}

/* 分页 - 科技风 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}

.message-detail {
  padding: 0 10px;
}

.detail-title {
  margin: 0 0 16px 0;
  font-size: 18px;
  font-weight: 600;
  color: #1f2937;
}

.detail-meta {
  display: flex;
  align-items: center;
  gap: 16px;
  font-size: 13px;
  color: var(--text-tertiary);
  flex-wrap: wrap;
}

.detail-meta-item {
  color: var(--text-tertiary);
}

.detail-content {
  font-size: 14px;
  line-height: 1.8;
  color: #303133;
  white-space: pre-wrap;
  min-height: 100px;
}

/* 已读统计区 */
.read-stats {
  margin-top: 4px;
}

.stats-loading,
.stats-error {
  text-align: center;
  padding: 24px 0;
  font-size: 13px;
  color: var(--text-tertiary);
}

.stats-error {
  color: var(--el-color-danger);
}

.stats-cards {
  display: flex;
  gap: 12px;
  margin-bottom: 20px;
}

.stat-card {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 16px 8px;
  border-radius: var(--radius-lg);
  border: 1px solid transparent;
}

/* 浅色语义背景，与白色弹窗协调 */
.stat-card--success {
  background: #f0f9eb;
  border-color: #e1f3d8;
}
.stat-card--success .stat-value { color: #67c23a; }

.stat-card--danger {
  background: #fef0f0;
  border-color: #fde2e2;
}
.stat-card--danger .stat-value { color: #f56c6c; }

.stat-card--primary {
  background: #ecf5ff;
  border-color: #d9ecff;
}
.stat-card--primary .stat-value { color: #409eff; }

.stat-card--warning {
  background: #fdf6ec;
  border-color: #faecd8;
}
.stat-card--warning .stat-value { color: #e6a23c; }

.stat-value {
  font-size: 24px;
  font-weight: 600;
  line-height: 1.2;
}

.stat-label {
  margin-top: 6px;
  font-size: 12px;
  color: #606266;
}

.unread-section {
  margin-top: 8px;
}

.unread-title {
  font-size: 13px;
  font-weight: 600;
  color: var(--text-secondary);
  margin-bottom: 10px;
}

.unread-empty {
  font-size: 13px;
  color: var(--text-tertiary);
  padding: 8px 0;
}

.unread-list {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  max-height: 240px;
  overflow-y: auto;
  padding: 4px;
}

.unread-more {
  margin-top: 12px;
  font-size: 12px;
  color: var(--text-tertiary);
  text-align: center;
}
</style>
