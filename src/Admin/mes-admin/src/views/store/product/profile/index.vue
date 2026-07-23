<template>
  <div class="product-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="商品名称">
            <el-input
              v-model="searchForm.name"
              placeholder="请输入商品名称"
              clearable
              style="width: 180px"
            />
          </el-form-item>
          <el-form-item label="商品编码">
            <el-input
              v-model="searchForm.code"
              placeholder="请输入商品编码"
              clearable
              style="width: 150px"
            />
          </el-form-item>
          <el-form-item label="状态">
            <el-select v-model="searchForm.status" placeholder="全部" clearable style="width: 120px">
              <el-option label="上架" :value="1" />
              <el-option label="下架" :value="2" />
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
          新增商品
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
        <el-table-column label="商品信息" min-width="220">
          <template #default="{ row }">
            <div class="product-info">
              <div class="product-thumb">
                <img v-if="row.imageUrl" :src="row.imageUrl" :alt="row.name" />
                <el-icon v-else><Picture /></el-icon>
              </div>
              <div class="product-detail">
                <div class="product-name">{{ row.name }}</div>
                <div class="product-code">{{ row.code }}</div>
              </div>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="categoryName" label="分类" width="120" />
        <el-table-column prop="spec" label="规格" width="120" show-overflow-tooltip />
        <el-table-column prop="unit" label="单位" width="80" />
        <el-table-column label="售价" width="100" align="right">
          <template #default="{ row }">
            <span class="price-text">¥{{ formatPrice(row.price) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="成本价" width="100" align="right">
          <template #default="{ row }">
            <span class="cost-text">¥{{ formatPrice(row.costPrice) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="lowStockThreshold" label="低库存阈值" width="110" align="center">
          <template #default="{ row }">
            <span v-if="row.lowStockThreshold !== null && row.lowStockThreshold !== undefined">{{ row.lowStockThreshold }}</span>
            <span v-else>-</span>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="90">
          <template #default="{ row }">
            <el-tag :type="row.status === 1 ? 'success' : 'info'" size="small" effect="dark">
              {{ row.status === 1 ? '上架' : '下架' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleEdit(row)">
              <el-icon><Edit /></el-icon>
              编辑
            </el-button>
            <el-button link type="danger" size="small" @click="handleDelete(row)">
              <el-icon><Delete /></el-icon>
              删除
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
      :title="isEdit ? '编辑商品' : '新增商品'"
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
            <el-form-item label="商品名称" prop="name">
              <el-input v-model="formData.name" placeholder="请输入商品名称" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="商品编码" prop="code">
              <el-input v-model="formData.code" placeholder="请输入商品编码" :disabled="isEdit" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="商品分类" prop="categoryId">
              <el-tree-select
                v-model="formData.categoryId"
                :data="categoryTree"
                :props="{ label: 'name', children: 'children' }"
                node-key="id"
                placeholder="请选择分类"
                check-strictly
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="商品类型" prop="type">
              <el-select v-model="formData.type" placeholder="请选择类型" style="width: 100%">
                <el-option label="实物商品" :value="1" />
                <el-option label="服务项目" :value="2" />
                <el-option label="耗材" :value="3" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="规格" prop="spec">
              <el-input v-model="formData.spec" placeholder="如 500ml / 红色/L" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="单位" prop="unit">
              <el-input v-model="formData.unit" placeholder="如 瓶 / 件 / 次" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="品牌" prop="brand">
              <el-input v-model="formData.brand" placeholder="请输入品牌" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="供应商" prop="supplierId">
              <el-select
                v-model="formData.supplierId"
                placeholder="请选择供应商"
                filterable
                clearable
                style="width: 100%"
              >
                <el-option
                  v-for="item in supplierOptions"
                  :key="item.id"
                  :label="item.name"
                  :value="item.id"
                />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="售价" prop="price">
              <el-input-number
                v-model="formData.price"
                :min="0"
                :precision="2"
                :step="1"
                controls-position="right"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="成本价" prop="costPrice">
              <el-input-number
                v-model="formData.costPrice"
                :min="0"
                :precision="2"
                :step="1"
                controls-position="right"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="6">
            <el-form-item label="低库存阈值" prop="lowStockThreshold">
              <el-input-number
                v-model="formData.lowStockThreshold"
                :min="0"
                :step="1"
                controls-position="right"
                style="width: 100%"
                placeholder="留空不预警"
              />
            </el-form-item>
          </el-col>
          <el-col :span="6">
            <el-form-item label="效期预警天数" prop="expiryAlertDays">
              <el-input-number
                v-model="formData.expiryAlertDays"
                :min="1"
                :step="1"
                controls-position="right"
                style="width: 100%"
                placeholder="留空不预警"
              />
            </el-form-item>
          </el-col>
          <el-col :span="6">
            <el-form-item label="积压阈值" prop="overstockThreshold">
              <el-input-number
                v-model="formData.overstockThreshold"
                :min="0"
                :step="1"
                controls-position="right"
                style="width: 100%"
                placeholder="留空不预警"
              />
            </el-form-item>
          </el-col>
          <el-col :span="6">
            <el-form-item label="状态" prop="status">
              <el-radio-group v-model="formData.status">
                <el-radio :value="1">上架</el-radio>
                <el-radio :value="2">下架</el-radio>
              </el-radio-group>
            </el-form-item>
          </el-col>
        </el-row>

        <!-- 服务项目子表字段（type=2）-->
        <template v-if="formData.type === 2">
          <el-divider content-position="left">服务项目信息</el-divider>
          <el-row :gutter="16">
            <el-col :span="12">
              <el-form-item label="服务时长(分钟)" prop="duration">
                <el-input-number
                  v-model="formData.duration"
                  :min="0"
                  :step="15"
                  controls-position="right"
                  style="width: 100%"
                />
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item label="所需房间/床位" prop="requiredRoomType">
                <el-select v-model="formData.requiredRoomType" placeholder="不限" clearable style="width: 100%">
                  <el-option label="房间" :value="1" />
                  <el-option label="床位" :value="2" />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="16">
            <el-col :span="12">
              <el-form-item label="所需仪器" prop="equipmentIds">
                <el-select
                  v-model="formData.equipmentIds"
                  multiple
                  filterable
                  placeholder="请选择所需仪器"
                  style="width: 100%"
                >
                  <el-option
                    v-for="item in equipmentOptions"
                    :key="item.id"
                    :label="item.name"
                    :value="item.id"
                  />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item label="适用技师技能" prop="applicableSkills">
                <el-input v-model="formData.applicableSkills" placeholder="如：面部护理、身体按摩" />
              </el-form-item>
            </el-col>
          </el-row>
        </template>

        <el-form-item label="商品描述" prop="description">
          <el-input v-model="formData.description" type="textarea" :rows="3" placeholder="请输入商品描述" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          确定
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus, Delete, Edit, Picture } from '@element-plus/icons-vue'
import {
  getProducts,
  createProduct,
  updateProduct,
  deleteProduct,
  deleteProducts,
  getCategoryTree
} from '@/api/product'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { Product, ProductCategory, ProductStatus } from '@/api/product/types'
import { getSuppliers } from '@/api/supplier'
import type { Supplier } from '@/api/supplier/types'
import { getAllEquipments } from '@/api/equipment'
import type { Equipment } from '@/api/equipment/types'

const systemConfigStore = useSystemConfigStore()

// 分类树（仅用于表单下拉选择）
const categoryTree = ref<ProductCategory[]>([])

// 供应商列表（仅用于表单下拉选择）
const supplierOptions = ref<Supplier[]>([])

// 设备列表（仅用于服务项目所需仪器多选）
const equipmentOptions = ref<Equipment[]>([])

// 加载分类树
const loadCategoryTree = async () => {
  try {
    categoryTree.value = await getCategoryTree()
  } catch (error) {
    categoryTree.value = []
  }
}

// 加载供应商列表
const loadSuppliers = async () => {
  try {
    const res = await getSuppliers({ pageIndex: 1, pageSize: 200 })
    supplierOptions.value = res.list
  } catch (error) {
    supplierOptions.value = []
  }
}

// 加载设备列表
const loadEquipments = async () => {
  try {
    equipmentOptions.value = await getAllEquipments()
  } catch (error) {
    equipmentOptions.value = []
  }
}

// 搜索表单
const searchForm = reactive({
  name: '',
  code: '',
  status: undefined as ProductStatus | undefined
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<Product[]>([])
const selectedRows = ref<Product[]>([])

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
    const res = await getProducts({
      name: searchForm.name || undefined,
      code: searchForm.code || undefined,
      status: searchForm.status,
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
  searchForm.code = ''
  searchForm.status = undefined
  handleSearch()
}

// 弹窗
const dialogVisible = ref(false)
const isEdit = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()

const formData = reactive({
  id: 0,
  name: '',
  code: '',
  categoryId: undefined as number | undefined,
  type: 1 as number,
  spec: '',
  unit: '',
  brand: '',
  supplierId: undefined as number | undefined,
  price: 0,
  costPrice: 0,
  lowStockThreshold: undefined as number | undefined,
  expiryAlertDays: undefined as number | undefined,
  overstockThreshold: undefined as number | undefined,
  imageUrl: '',
  status: 1 as number,
  description: '',
  // 服务项目子表字段（type=2）
  duration: undefined as number | undefined,
  requiredRoomType: undefined as number | undefined,
  equipmentIds: [] as number[],
  applicableSkills: ''
})

const formRules: FormRules = {
  name: [
    { required: true, message: '商品名称不能为空', trigger: 'blur' },
    { max: 100, message: '商品名称最多100个字符', trigger: 'blur' }
  ],
  code: [
    { required: true, message: '商品编码不能为空', trigger: 'blur' },
    { min: 2, max: 50, message: '商品编码长度为2-50个字符', trigger: 'blur' },
    { pattern: /^[a-zA-Z0-9_]+$/, message: '商品编码只能包含字母、数字、下划线', trigger: 'blur' }
  ],
  categoryId: [
    { required: true, message: '请选择商品分类', trigger: 'change' }
  ],
  type: [
    { required: true, message: '请选择商品类型', trigger: 'change' }
  ],
  price: [
    { required: true, message: '售价不能为空', trigger: 'blur' },
    { type: 'number', min: 0, message: '售价必须大于等于0', trigger: 'blur' }
  ],
  status: [
    { required: true, message: '请选择状态', trigger: 'change' }
  ]
}

// 重置表单
const resetFormData = () => {
  formData.id = 0
  formData.name = ''
  formData.code = ''
  formData.categoryId = undefined
  formData.type = 1
  formData.spec = ''
  formData.unit = ''
  formData.brand = ''
  formData.supplierId = undefined
  formData.price = 0
  formData.costPrice = 0
  formData.lowStockThreshold = undefined
  formData.expiryAlertDays = undefined
  formData.overstockThreshold = undefined
  formData.imageUrl = ''
  formData.status = 1
  formData.description = ''
  // 重置子表字段
  formData.duration = undefined
  formData.requiredRoomType = undefined
  formData.equipmentIds = []
  formData.applicableSkills = ''
}

// 新增
const handleAdd = () => {
  isEdit.value = false
  resetFormData()
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: Product) => {
  isEdit.value = true
  formData.id = row.id
  formData.name = row.name
  formData.code = row.code
  formData.categoryId = row.categoryId
  formData.type = row.type
  formData.spec = row.spec || ''
  formData.unit = row.unit || ''
  formData.brand = row.brand || ''
  formData.supplierId = row.supplierId ?? undefined
  formData.price = row.price
  formData.costPrice = row.costPrice || 0
  formData.lowStockThreshold = row.lowStockThreshold ?? undefined
  formData.expiryAlertDays = row.expiryAlertDays ?? undefined
  formData.overstockThreshold = row.overstockThreshold ?? undefined
  formData.imageUrl = row.imageUrl || ''
  formData.status = row.status
  formData.description = row.description || ''
  // 加载子表字段
  formData.duration = row.duration
  formData.requiredRoomType = row.requiredRoomType
  formData.equipmentIds = row.equipmentIds ? [...row.equipmentIds] : []
  formData.applicableSkills = row.applicableSkills || ''
  dialogVisible.value = true
}

// 删除
const handleDelete = async (row: Product) => {
  try {
    await ElMessageBox.confirm(`确定要删除商品 "${row.name}" 吗？此操作不可恢复！`, '警告', {
      type: 'warning',
      confirmButtonText: '确定删除',
      cancelButtonText: '取消'
    })
    await deleteProduct(row.id)
    ElMessage.success('删除成功')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

// 批量删除
const handleBatchDelete = async () => {
  if (selectedRows.value.length === 0) return
  try {
    await ElMessageBox.confirm(`确定要删除选中的 ${selectedRows.value.length} 个商品吗？此操作不可恢复！`, '警告', {
      type: 'warning',
      confirmButtonText: '确定删除',
      cancelButtonText: '取消'
    })
    const ids = selectedRows.value.map(row => row.id)
    await deleteProducts(ids)
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
        const payload: any = {
          name: formData.name,
          code: formData.code,
          categoryId: formData.categoryId!,
          type: formData.type,
          spec: formData.spec || undefined,
          unit: formData.unit || undefined,
          brand: formData.brand || undefined,
          supplierId: formData.supplierId ?? undefined,
          price: formData.price,
          costPrice: formData.costPrice || undefined,
          lowStockThreshold: formData.lowStockThreshold ?? undefined,
          expiryAlertDays: formData.expiryAlertDays ?? undefined,
          overstockThreshold: formData.overstockThreshold ?? undefined,
          imageUrl: formData.imageUrl || undefined,
          status: formData.status,
          description: formData.description || undefined
        }
        // 根据商品类型包含对应子表字段
        if (formData.type === 2) {
          payload.duration = formData.duration
          payload.requiredRoomType = formData.requiredRoomType
          payload.equipmentIds = formData.equipmentIds
          payload.applicableSkills = formData.applicableSkills || undefined
        }
        if (isEdit.value) {
          await updateProduct({ ...payload, id: formData.id })
          ElMessage.success('更新成功')
        } else {
          await createProduct(payload)
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
const handleSelectionChange = (rows: Product[]) => {
  selectedRows.value = rows
}

// 格式化价格
const formatPrice = (price: number | undefined) => {
  if (price === null || price === undefined) return '0.00'
  return price.toFixed(2)
}

onMounted(async () => {
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  pagination.pageSize = systemConfigStore.defaultPageSize
  loadCategoryTree()
  loadSuppliers()
  loadEquipments()
  loadData()
})
</script>

<style scoped>
.product-management {
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

/* 商品信息 */
.product-info {
  display: flex;
  align-items: center;
  gap: 12px;
}

.product-thumb {
  width: 40px;
  height: 40px;
  border-radius: 6px;
  background: var(--bg-hover);
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  flex-shrink: 0;
}

.product-thumb img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.product-thumb .el-icon {
  color: var(--text-tertiary);
  font-size: 18px;
}

.product-detail {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.product-name {
  font-weight: 500;
  color: var(--text-primary);
}

.product-code {
  font-size: 12px;
  color: var(--text-tertiary);
}

.price-text {
  color: var(--primary);
  font-weight: 600;
}

.cost-text {
  color: var(--text-tertiary);
}

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}
</style>
