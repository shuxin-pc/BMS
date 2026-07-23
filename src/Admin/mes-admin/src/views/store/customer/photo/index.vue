<template>
  <div class="comparison-photo">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="客户">
            <el-select
              v-model="searchForm.customerId"
              placeholder="全部客户"
              clearable
              filterable
              style="width: 180px"
            >
              <el-option
                v-for="item in customerOptions"
                :key="item.id"
                :label="`${item.name}（${item.phone}）`"
                :value="item.id"
              />
            </el-select>
          </el-form-item>
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
    </div>

    <!-- 操作栏 -->
    <div class="table-toolbar">
      <div class="toolbar-left">
        <span class="toolbar-hint">服务对比照片（共 {{ pagination.total }} 组）</span>
      </div>
      <div class="toolbar-right">
        <el-button type="primary" @click="handleAdd">
          <el-icon><Plus /></el-icon>
          上传照片
        </el-button>
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
      <div v-for="(pair, index) in photoPairs" :key="index" class="photo-pair-card card mb-20">
        <div class="pair-header">
          <div class="pair-info">
            <span class="pair-customer">{{ pair.customerName }}</span>
            <span class="pair-service">{{ pair.serviceItem || '未指定服务项目' }}</span>
            <span class="pair-date">{{ pair.photoDate }}</span>
          </div>
        </div>
        <div class="pair-photos">
          <div class="photo-item">
            <div class="photo-label before">服务前</div>
            <el-image
              v-if="pair.before"
              :src="pair.before.photoUrl"
              :preview-src-list="getPreviewList(pair)"
              :initial-index="0"
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
            <div class="photo-label after">服务后</div>
            <el-image
              v-if="pair.after"
              :src="pair.after.photoUrl"
              :preview-src-list="getPreviewList(pair)"
              :initial-index="pair.before ? 1 : 0"
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
    <div class="card">
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

    <!-- 上传照片弹窗 -->
    <el-dialog v-model="dialogVisible" title="上传对比照片" width="520px" @closed="handleDialogClosed">
      <el-form
        ref="formRef"
        :model="photoForm"
        :rules="formRules"
        label-width="100px"
      >
        <el-form-item label="客户" prop="customerId">
          <el-select
            v-model="photoForm.customerId"
            placeholder="请选择客户"
            filterable
            style="width: 100%"
          >
            <el-option
              v-for="item in customerOptions"
              :key="item.id"
              :label="`${item.name}（${item.phone}）`"
              :value="item.id"
            />
          </el-select>
        </el-form-item>

        <el-form-item label="服务项目">
          <el-input
            v-model="photoForm.serviceItem"
            placeholder="请输入服务项目名称"
            maxlength="100"
          />
        </el-form-item>

        <el-form-item label="拍照日期" prop="photoDate">
          <el-date-picker
            v-model="photoForm.photoDate"
            type="date"
            placeholder="请选择拍照日期"
            value-format="YYYY-MM-DD"
            style="width: 100%"
          />
        </el-form-item>

        <el-form-item label="照片类型" prop="photoType">
          <el-radio-group v-model="photoForm.photoType">
            <el-radio-button :value="1">服务前</el-radio-button>
            <el-radio-button :value="2">服务后</el-radio-button>
          </el-radio-group>
        </el-form-item>

        <el-form-item label="照片URL" prop="photoUrl">
          <el-input
            v-model="photoForm.photoUrl"
            placeholder="请输入照片URL"
          />
          <div class="form-hint">Mock 环境：可输入任意图片URL，如 https://picsum.photos/seed/test/400/400</div>
        </el-form-item>

        <el-form-item label="备注">
          <el-input
            v-model="photoForm.remark"
            type="textarea"
            :rows="2"
            placeholder="请输入备注"
            maxlength="200"
            show-word-limit
          />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          上传
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus, Right, Picture, Loading } from '@element-plus/icons-vue'
import { useSystemConfigStore } from '@/stores/systemConfig'
import { getComparisonPhotos, createComparisonPhoto, getCustomerOptions } from '@/api/customer-profile'
import type {
  ServiceComparisonPhoto,
  ComparisonPhotoCreate,
  CustomerOption,
  PhotoType
} from '@/api/customer-profile/types'

// 配对展示用的结构
interface PhotoPairDisplay {
  customerName: string
  serviceItem?: string
  photoDate: string
  before?: ServiceComparisonPhoto
  after?: ServiceComparisonPhoto
}

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  customerId: undefined as number | undefined,
  serviceItem: ''
})

// 表格数据
const tableLoading = ref(false)
const allPhotos = ref<ServiceComparisonPhoto[]>([])
const photoPairs = ref<PhotoPairDisplay[]>([])

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

const customerOptions = ref<CustomerOption[]>([])

// 将照片列表按客户+服务项目+日期配对
const buildPhotoPairs = (photos: ServiceComparisonPhoto[]): PhotoPairDisplay[] => {
  const pairMap = new Map<string, PhotoPairDisplay>()

  for (const photo of photos) {
    const key = `${photo.customerId}-${photo.serviceItem || ''}-${photo.photoDate}`
    if (!pairMap.has(key)) {
      pairMap.set(key, {
        customerName: photo.customerName || '',
        serviceItem: photo.serviceItem,
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

// 获取预览列表
const getPreviewList = (pair: PhotoPairDisplay): string[] => {
  const list: string[] = []
  if (pair.before) list.push(pair.before.photoUrl)
  if (pair.after) list.push(pair.after.photoUrl)
  return list
}

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getComparisonPhotos({
      customerId: searchForm.customerId,
      serviceItem: searchForm.serviceItem || undefined,
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    allPhotos.value = res.list
    photoPairs.value = buildPhotoPairs(res.list)
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
  searchForm.customerId = undefined
  searchForm.serviceItem = ''
  handleSearch()
}

// ==================== 上传弹窗 ====================

const dialogVisible = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()

const photoForm = reactive<ComparisonPhotoCreate>({
  customerId: 0,
  serviceItem: '',
  photoDate: new Date().toISOString().substring(0, 10),
  photoType: 1 as PhotoType,
  photoUrl: '',
  remark: ''
})

const formRules: FormRules = {
  customerId: [{ required: true, message: '请选择客户', trigger: 'change' }],
  photoDate: [{ required: true, message: '请选择拍照日期', trigger: 'change' }],
  photoType: [{ required: true, message: '请选择照片类型', trigger: 'change' }],
  photoUrl: [{ required: true, message: '请输入照片URL', trigger: 'blur' }]
}

// 上传
const handleAdd = async () => {
  if (customerOptions.value.length === 0) {
    customerOptions.value = await getCustomerOptions()
  }
  dialogVisible.value = true
}

// 弹窗关闭后重置
const handleDialogClosed = () => {
  formRef.value?.resetFields()
  photoForm.customerId = 0
  photoForm.serviceItem = ''
  photoForm.photoDate = new Date().toISOString().substring(0, 10)
  photoForm.photoType = 1
  photoForm.photoUrl = ''
  photoForm.remark = ''
}

// 提交
const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    submitLoading.value = true
    try {
      await createComparisonPhoto({
        customerId: photoForm.customerId,
        serviceItem: photoForm.serviceItem || undefined,
        photoDate: photoForm.photoDate,
        photoType: photoForm.photoType,
        photoUrl: photoForm.photoUrl,
        remark: photoForm.remark || undefined
      })
      ElMessage.success('照片上传成功')
      dialogVisible.value = false
      loadData()
    } catch (error: any) {
      ElMessage.error(error.message || '上传失败')
    } finally {
      submitLoading.value = false
    }
  })
}

onMounted(async () => {
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  pagination.pageSize = systemConfigStore.defaultPageSize
  customerOptions.value = await getCustomerOptions()
  loadData()
})
</script>

<style scoped>
.comparison-photo {
  width: 100%;
}

.card {
  background: var(--bg-tertiary);
  border-radius: var(--radius-lg);
  border: 1px solid var(--border-primary);
  overflow: hidden;
}

.mb-20 {
  margin-bottom: 20px;
}

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

.search-form {
  padding: 20px 24px 0;
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

.toolbar-hint {
  font-size: 13px;
  color: var(--text-tertiary);
}

.form-hint {
  font-size: 12px;
  color: var(--text-tertiary);
  margin-top: 4px;
  line-height: 1.5;
}

.empty-state {
  padding: 60px 0;
}

/* 照片配对卡片 */
.photo-pair-card {
  padding: 20px 24px;
}

.pair-header {
  margin-bottom: 16px;
}

.pair-info {
  display: flex;
  align-items: center;
  gap: 16px;
}

.pair-customer {
  font-size: 15px;
  font-weight: 600;
  color: var(--text-primary);
}

.pair-service {
  font-size: 13px;
  color: var(--text-secondary);
}

.pair-date {
  font-size: 12px;
  color: var(--text-tertiary);
  font-family: 'JetBrains Mono', monospace;
  margin-left: auto;
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

.photo-label {
  font-size: 13px;
  font-weight: 600;
  margin-bottom: 8px;
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

.photo-image {
  width: 100%;
  height: 240px;
  border-radius: var(--radius-md);
  border: 1px solid var(--border-primary);
  cursor: pointer;
  display: block;
}

.photo-error,
.photo-empty,
.photo-placeholder {
  width: 100%;
  height: 240px;
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
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}
</style>
