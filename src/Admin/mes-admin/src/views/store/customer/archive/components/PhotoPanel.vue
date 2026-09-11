<template>
  <div class="photo-panel">
    <!-- 搜索区域 -->
    <div class="search-form">
      <el-form :inline="true" :model="searchForm" class="search-form-inline">
        <el-form-item label="服务项目">
          <el-input
            v-model="searchForm.serviceItem"
            placeholder="请输入服务项目"
            clearable
            style="width: 160px"
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

    <!-- 操作栏 -->
    <div class="table-toolbar">
      <div class="toolbar-left">
        <el-button type="primary" @click="handleAdd" v-if="hasPermission('store:customer:archive:photo:add')">
          <el-icon><Plus /></el-icon>
          上传照片
        </el-button>
      </div>
      <div class="toolbar-right">
        <el-button circle @click="loadData">
          <el-icon><Refresh /></el-icon>
        </el-button>
      </div>
    </div>

    <!-- 照片配对展示 -->
    <div v-loading="tableLoading" class="photo-grid">
      <div v-if="photoPairs.length === 0 && !tableLoading" class="empty-state">
        <el-empty description="暂无对比照片数据" />
      </div>
      <div v-for="(pair, index) in photoPairs" :key="index" class="photo-pair-card">
        <div class="pair-header">
          <div class="pair-info">
            <span class="pair-service">{{ pair.serviceItem || '未指定服务项目' }}</span>
            <span v-if="pair.orderNo" class="pair-field">订单号：{{ pair.orderNo }}</span>
            <span class="pair-field">拍照日期：{{ pair.photoDate }}</span>
          </div>
          <div class="pair-actions">
            <el-button link type="primary" @click="handleEdit(pair)" v-if="hasPermission('store:customer:archive:photo:edit')">
              <el-icon><Edit /></el-icon>
              编辑
            </el-button>
            <el-button link type="danger" @click="handleDelete(pair)" v-if="hasPermission('store:customer:archive:photo:delete')">
              <el-icon><Delete /></el-icon>
              删除
            </el-button>
          </div>
        </div>
        <div class="pair-photos">
          <div class="photo-item">
            <div class="photo-item-header">
              <span class="photo-label before">服务前</span>
              <el-button
                v-if="(pair.before?.items.length || 0) > PREVIEW_LIMIT"
                link
                type="primary"
                @click="handleViewAll(pair, '服务前')"
              >
                查看全部 {{ pair.before!.items.length }} 张
              </el-button>
            </div>
            <div v-if="pair.before && pair.before.items.length > 0" class="photo-list">
              <el-image
                v-for="(item, i) in pair.before.items.slice(0, PREVIEW_LIMIT)"
                :key="item.id"
                :src="item.photoUrl"
                :preview-src-list="getPreviewList(pair)"
                :initial-index="i"
                fit="cover"
                class="photo-image"
                preview-teleported
              >
                <template #error>
                  <div class="photo-error">
                    <el-icon><Picture /></el-icon>
                    <span>加载失败</span>
                  </div>
                </template>
                <template #placeholder>
                  <div class="photo-placeholder">
                    <el-icon class="is-loading"><Loading /></el-icon>
                  </div>
                </template>
              </el-image>
            </div>
            <div v-else class="photo-empty">
              <el-icon><Picture /></el-icon>
              <span>暂无照片</span>
            </div>
            <div v-if="pair.before?.remark" class="photo-remark">{{ pair.before.remark }}</div>
          </div>
          <div class="photo-arrow">
            <el-icon><Right /></el-icon>
          </div>
          <div class="photo-item">
            <div class="photo-item-header">
              <span class="photo-label after">服务后</span>
              <el-button
                v-if="(pair.after?.items.length || 0) > PREVIEW_LIMIT"
                link
                type="primary"
                @click="handleViewAll(pair, '服务后')"
              >
                查看全部 {{ pair.after!.items.length }} 张
              </el-button>
            </div>
            <div v-if="pair.after && pair.after.items.length > 0" class="photo-list">
              <el-image
                v-for="(item, i) in pair.after.items.slice(0, PREVIEW_LIMIT)"
                :key="item.id"
                :src="item.photoUrl"
                :preview-src-list="getPreviewList(pair)"
                :initial-index="(pair.before?.items.length || 0) + i"
                fit="cover"
                class="photo-image"
                preview-teleported
              >
                <template #error>
                  <div class="photo-error">
                    <el-icon><Picture /></el-icon>
                    <span>加载失败</span>
                  </div>
                </template>
                <template #placeholder>
                  <div class="photo-placeholder">
                    <el-icon class="is-loading"><Loading /></el-icon>
                  </div>
                </template>
              </el-image>
            </div>
            <div v-else class="photo-empty">
              <el-icon><Picture /></el-icon>
              <span>暂无照片</span>
            </div>
            <div v-if="pair.after?.remark" class="photo-remark">{{ pair.after.remark }}</div>
          </div>
        </div>
      </div>
    </div>

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

    <!-- 上传/编辑照片弹窗 -->
    <PhotoUploadDialog
      v-model:visible="dialogVisible"
      :customer-id="props.customerId"
      :customer-name="props.customerName"
      :before-record="editingPair?.before"
      :after-record="editingPair?.after"
      @success="loadData"
    />

    <!-- 查看全部照片弹窗 -->
    <el-dialog v-model="viewAllVisible" :title="viewAllTitle" width="760px">
      <div class="all-photo-grid">
        <el-image
          v-for="(item, i) in viewAllItems"
          :key="item.id"
          :src="item.photoUrl"
          :preview-src-list="viewAllItems.map(x => x.photoUrl)"
          :initial-index="i"
          fit="cover"
          class="all-photo-image"
          preview-teleported
        >
          <template #error>
            <div class="photo-error">
              <el-icon><Picture /></el-icon>
              <span>加载失败</span>
            </div>
          </template>
        </el-image>
      </div>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, watch, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Plus, Right, Picture, Loading, Edit, Delete } from '@element-plus/icons-vue'
import { useSystemConfigStore } from '@/stores/systemConfig'
import { useUserStore } from '@/stores/user'
import { getComparisonPhotos, batchDeleteComparisonPhotos } from '@/api/customer-profile'
import type { ServiceComparisonPhoto, ServiceComparisonPhotoItem } from '@/api/customer-profile/types'
import PhotoUploadDialog from './PhotoUploadDialog.vue'

const props = defineProps<{
  /** 当前客户ID，由服务档案容器页提供 */
  customerId: number
  /** 当前客户姓名，仅用于弹窗内只读展示 */
  customerName: string
}>()

// 配对展示用的结构
interface PhotoPairDisplay {
  serviceItem?: string
  orderNo?: string
  photoDate: string
  before?: ServiceComparisonPhoto
  after?: ServiceComparisonPhoto
}

const systemConfigStore = useSystemConfigStore()

const userStore = useUserStore()

const hasPermission = (permissionCode: string) => userStore.hasPermission(permissionCode)

// 卡片内每种类型最多平铺展示的照片数，超出部分通过"查看全部"弹窗查看
const PREVIEW_LIMIT = 2

// 搜索表单
const searchForm = reactive({
  serviceItem: ''
})

const tableLoading = ref(false)
const photoPairs = ref<PhotoPairDisplay[]>([])

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

// 将照片列表按分组键（订单+服务项目+日期）配对，与编辑弹窗的分组字段保持一致
// 客户已固定，但保留 customerId 入键以防接口返回越界数据
const buildPhotoPairs = (photos: ServiceComparisonPhoto[]): PhotoPairDisplay[] => {
  const pairMap = new Map<string, PhotoPairDisplay>()

  for (const photo of photos) {
    const key = [
      photo.customerId,
      photo.orderId ?? '',
      photo.productId ?? '',
      photo.serviceItem ?? '',
      photo.photoDate
    ].join('-')
    if (!pairMap.has(key)) {
      pairMap.set(key, {
        serviceItem: photo.serviceItem,
        orderNo: photo.orderNo,
        photoDate: photo.photoDate,
        before: undefined,
        after: undefined
      })
    }
    const pair = pairMap.get(key)!
    if (photo.photoType === 1) {
      pair.before = photo
    } else if (photo.photoType === 2) {
      pair.after = photo
    }
  }

  return Array.from(pairMap.values()).sort((a, b) => b.photoDate.localeCompare(a.photoDate))
}

// 获取整组预览列表（服务前在前、服务后在后，与卡片内 initial-index 计算一致）
const getPreviewList = (pair: PhotoPairDisplay): string[] => [
  ...(pair.before?.items || []).map(i => i.photoUrl),
  ...(pair.after?.items || []).map(i => i.photoUrl)
]

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getComparisonPhotos({
      customerId: props.customerId,
      serviceItem: searchForm.serviceItem || undefined,
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    photoPairs.value = buildPhotoPairs(res.list)
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
  searchForm.serviceItem = ''
  handleSearch()
}

// 客户切换时重载并归位分页；首屏由 onMounted 负责，故不加 immediate
watch(
  () => props.customerId,
  () => {
    pagination.pageIndex = 1
    loadData()
  }
)

const dialogVisible = ref(false)
// 正在编辑的照片组，为空表示新增
const editingPair = ref<PhotoPairDisplay | null>(null)

// 查看全部照片弹窗
const viewAllVisible = ref(false)
const viewAllTitle = ref('')
const viewAllItems = ref<ServiceComparisonPhotoItem[]>([])

const handleViewAll = (pair: PhotoPairDisplay, label: '服务前' | '服务后') => {
  const record = label === '服务前' ? pair.before : pair.after
  if (!record) return
  viewAllItems.value = record.items
  viewAllTitle.value = `${pair.serviceItem || '未指定服务项目'} · ${label}（${record.items.length} 张）`
  viewAllVisible.value = true
}

const handleAdd = () => {
  editingPair.value = null
  dialogVisible.value = true
}

const handleEdit = (pair: PhotoPairDisplay) => {
  editingPair.value = pair
  dialogVisible.value = true
}

// 整组删除：服务前/服务后属于同一次服务，单独保留一侧没有对比意义
const handleDelete = async (pair: PhotoPairDisplay) => {
  const ids = [pair.before?.id, pair.after?.id].filter((id): id is number => !!id)
  if (ids.length === 0) return
  try {
    await ElMessageBox.confirm('确定删除该组对比照片吗？服务前与服务后将一并删除', '提示', {
      type: 'warning'
    })
  } catch {
    return
  }
  try {
    await batchDeleteComparisonPhotos(ids)
    ElMessage.success('删除成功')
    loadData()
  } catch (error) {
    ElMessage.error((error as Error).message || '删除失败')
  }
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
.photo-panel {
  width: 100%;
}

.search-form {
  padding-bottom: 4px;
}

.search-form-inline {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.table-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
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

.empty-state {
  padding: 60px 0;
}

/* 照片配对卡片 */
.photo-pair-card {
  padding: 20px 24px;
  margin-bottom: 20px;
  background: var(--bg-secondary);
  border-radius: var(--radius-lg);
  border: 1px solid var(--border-primary);
}

.pair-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 16px;
}

.pair-info {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 16px;
  min-width: 0;
}

.pair-service {
  font-size: 15px;
  font-weight: 600;
  color: var(--text-primary);
}

.pair-field {
  font-size: 12px;
  color: var(--text-tertiary);
}

.pair-actions {
  display: flex;
  gap: 4px;
  flex-shrink: 0;
}

.pair-photos {
  display: flex;
  align-items: stretch;
  gap: 20px;
}

.photo-item {
  flex: 1;
  min-width: 0;
}

.photo-item-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  margin-bottom: 8px;
}

.photo-label {
  font-size: 13px;
  font-weight: 600;
  padding: 2px 12px;
  border-radius: var(--radius-sm);
  display: inline-block;
}

.photo-label.before {
  background: var(--bg-secondary);
  color: var(--text-tertiary);
}

.photo-label.after {
  background: rgba(6, 212, 228, 0.15);
  color: var(--primary);
}

/* 同一类型最多平铺 2 张且不换行：单张铺满半栏，两张各占一半 */
.photo-list {
  display: flex;
  gap: 8px;
}

.photo-image {
  flex: 1 1 calc(50% - 4px);
  min-width: 0;
  height: 200px;
  border-radius: var(--radius-md);
  border: 1px solid var(--border-primary);
  cursor: pointer;
  display: block;
}

.photo-error,
.photo-empty,
.photo-placeholder {
  width: 100%;
  height: 200px;
  border-radius: var(--radius-md);
  border: 1px dashed var(--border-primary);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  color: var(--text-tertiary);
  font-size: 13px;
}

.photo-error .el-icon,
.photo-empty .el-icon {
  font-size: 32px;
}

.photo-remark {
  margin-top: 8px;
  font-size: 12px;
  color: var(--text-tertiary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.photo-arrow {
  display: flex;
  align-items: center;
  font-size: 24px;
  color: var(--primary);
  flex-shrink: 0;
}

.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 0 4px;
  border-top: 1px solid var(--border-primary);
}

/* 查看全部弹窗内的照片网格 */
.all-photo-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
  gap: 12px;
}

.all-photo-image {
  width: 100%;
  height: 180px;
  border-radius: var(--radius-md);
  border: 1px solid var(--border-primary);
  cursor: pointer;
  display: block;
}
</style>
