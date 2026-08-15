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
          <el-form-item label="商品分类">
            <el-tree-select
              v-model="searchForm.categoryId"
              :data="categoryTree"
              :props="{ label: 'name', children: 'children' }"
              node-key="id"
              placeholder="全部"
              check-strictly
              clearable
              style="width: 180px"
            />
          </el-form-item>
          <el-form-item label="商品类型">
            <el-select v-model="searchForm.type" placeholder="全部" clearable style="width: 120px">
              <el-option label="实物商品" :value="1" />
              <el-option label="服务项目" :value="2" />
              <el-option label="耗材" :value="3" />
              <el-option label="样品" :value="4" />
              <el-option label="赠品" :value="5" />
            </el-select>
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
        <el-table-column prop="name" label="商品名称" min-width="160" show-overflow-tooltip />
        <el-table-column prop="code" label="商品编码" width="140" show-overflow-tooltip />
        <el-table-column label="商品类型" width="100">
          <template #default="{ row }">
            {{ getProductTypeName(row.type) }}
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
        <!-- 选择商品主档（新增时必选，编辑时只读） -->
        <el-form-item label="商品主档" prop="masterId">
          <el-select
            v-model="formData.masterId"
            placeholder="请选择商品主档"
            filterable
            :disabled="isEdit"
            style="width: 100%"
            @change="handleMasterChange"
          >
            <el-option
              v-for="item in masterOptions"
              :key="item.id"
              :label="`${item.code} - ${item.name}`"
              :value="item.id"
            />
          </el-select>
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="商品名称">
              <el-input v-model="formData.name" disabled />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="商品编码">
              <el-input v-model="formData.code" disabled />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="商品分类">
              <el-tree-select
                v-model="formData.categoryId"
                :data="categoryTree"
                :props="{ label: 'name', children: 'children' }"
                node-key="id"
                disabled
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="商品类型">
              <el-select v-model="formData.type" disabled style="width: 100%">
                <el-option label="实物商品" :value="1" />
                <el-option label="服务项目" :value="2" />
                <el-option label="耗材" :value="3" />
                <el-option label="样品" :value="4" />
                <el-option label="赠品" :value="5" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="规格">
              <el-input v-model="formData.spec" disabled />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="单位">
              <el-input v-model="formData.unit" disabled />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="品牌">
              <el-input v-model="formData.brand" disabled />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="默认供应商">
              <div class="supplier-display">
                <span class="supplier-name" :class="{ 'is-empty': !formData.defaultSupplierName }">{{ formData.defaultSupplierName || '未设置' }}</span>
                <el-button
                  type="primary"
                  link
                  size="small"
                  @click="handleManageSuppliers"
                >
                  <el-icon><Setting /></el-icon>
                  管理供应商
                </el-button>
              </div>
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
                :controls="false"
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
                :controls="false"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="低库存阈值" prop="lowStockThreshold">
              <el-input-number
                v-model="formData.lowStockThreshold"
                :min="0"
                :step="1"
                :controls="false"
                style="width: 100%"
                placeholder="留空不预警"
                :disabled="formData.type === 2"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="积压阈值" prop="overstockThreshold">
              <el-input-number
                v-model="formData.overstockThreshold"
                :min="0"
                :step="1"
                :controls="false"
                style="width: 100%"
                placeholder="留空不预警"
                :disabled="formData.type === 2"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="效期预警天数" prop="expiryAlertDays">
              <el-input-number
                v-model="formData.expiryAlertDays"
                :min="1"
                :step="1"
                :controls="false"
                style="width: 100%"
                placeholder="留空不预警"
                :disabled="formData.type === 2"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="状态" prop="status">
              <el-radio-group v-model="formData.status">
                <el-radio :value="1">上架</el-radio>
                <el-radio :value="2">下架</el-radio>
              </el-radio-group>
            </el-form-item>
          </el-col>
        </el-row>

        <el-form-item label="备注" prop="remark">
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

    <!-- 管理供应商弹窗 -->
    <el-dialog
      v-model="supplierDialogVisible"
      :title="`管理供应商 - ${formData.name}`"
      width="800px"
      :close-on-click-modal="false"
    >
      <el-alert
        title="本弹窗内的修改即时保存，无需点击确定按钮"
        type="info"
        :closable="false"
        show-icon
        style="margin-bottom: 16px"
      />
      <!-- 添加供应商 -->
      <div class="add-supplier-bar">
        <el-select
          v-model="addSupplierId"
          placeholder="选择未绑定的供应商"
          filterable
          clearable
          style="width: 320px"
        >
          <el-option
            v-for="item in unboundSupplierOptions"
            :key="item.id"
            :label="item.scope === 1 ? `${item.name}（门店通用）` : item.name"
            :value="item.id"
          />
        </el-select>
        <el-button
          type="primary"
          :disabled="!addSupplierId"
          @click="handleAddSupplier"
        >
          添加供应商
        </el-button>
      </div>
      <!-- 已绑定供应商列表 -->
      <el-table
        v-loading="supplierLoading"
        :data="productSupplierList"
        class="supplier-table"
        style="width: 100%; margin-top: 12px"
      >
        <el-table-column prop="supplierName" label="供应商名称" min-width="160" show-overflow-tooltip />
        <el-table-column label="是否默认" width="100" align="center">
          <template #default="{ row }">
            <el-tag v-if="row.isDefault" type="success" size="small" effect="dark">默认</el-tag>
            <span v-else>-</span>
          </template>
        </el-table-column>
        <el-table-column label="参考价" width="140" align="right">
          <template #default="{ row }">
            <el-input-number
              v-model="row.referencePrice"
              :min="0"
              :precision="2"
              :step="1"
              :controls="false"
              size="small"
              style="width: 120px"
              @change="(val: number | undefined) => handleUpdateReferencePrice(row, val)"
            />
          </template>
        </el-table-column>
        <el-table-column label="供货周期(天)" width="140" align="right">
          <template #default="{ row }">
            <el-input-number
              v-model="row.leadTimeDays"
              :min="0"
              :step="1"
              :precision="0"
              :controls="false"
              size="small"
              style="width: 120px"
              @change="(val: number | undefined) => handleUpdateLeadTime(row, val)"
            />
          </template>
        </el-table-column>
        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
            <el-button
              v-if="!row.isDefault"
              link
              type="primary"
              size="small"
              @click="handleSetDefault(row)"
            >
              设为默认
            </el-button>
            <el-button
              link
              type="danger"
              size="small"
              @click="handleUnbind(row)"
            >
              解除关联
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus, Delete, Edit, Setting } from '@element-plus/icons-vue'
import {
  getProducts,
  createProduct,
  updateProduct,
  deleteProduct,
  deleteProducts,
  getCategoryTree,
  getSuppliersByProduct
} from '@/api/product'
import { getProductMasterOptions, getProductMaster, type ProductMasterOption } from '@/api/product/master'
import {
  getSuppliers,
  bindProducts,
  unbindProduct,
  setDefaultSupplier,
  updateProductRelation
} from '@/api/supplier'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { Product, ProductCategory, ProductCreate, ProductStatus, ProductType } from '@/api/product/types'
import type { Supplier } from '@/api/supplier/types'
import type { ProductSupplier } from '@/api/supplier/types'

const systemConfigStore = useSystemConfigStore()

// 分类树（仅用于表单下拉选择）
const categoryTree = ref<ProductCategory[]>([])

// 供应商列表（用于管理供应商弹窗的"添加供应商"下拉选项）
const supplierOptions = ref<Supplier[]>([])

// 商品主档选项列表（用于新增时选择 Master）
const masterOptions = ref<ProductMasterOption[]>([])

// 加载分类树
const loadCategoryTree = async () => {
  try {
    categoryTree.value = await getCategoryTree()
  } catch {
    categoryTree.value = []
  }
}

// 加载供应商列表
const loadSuppliers = async () => {
  try {
    const res = await getSuppliers({ pageIndex: 1, pageSize: 200 })
    supplierOptions.value = res.list
  } catch {
    supplierOptions.value = []
  }
}

// 加载商品主档选项
const loadMasterOptions = async () => {
  try {
    masterOptions.value = await getProductMasterOptions()
  } catch {
    masterOptions.value = []
  }
}

// 搜索表单
const searchForm = reactive({
  name: '',
  code: '',
  categoryId: undefined as number | undefined,
  type: undefined as ProductType | undefined,
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
      categoryId: searchForm.categoryId,
      type: searchForm.type,
      status: searchForm.status,
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
  searchForm.name = ''
  searchForm.code = ''
  searchForm.categoryId = undefined
  searchForm.type = undefined
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
  masterId: undefined as number | undefined,
  // Master 字段（只读展示，选择 Master 后自动填充）
  name: '',
  code: '',
  categoryId: undefined as number | undefined,
  type: 1 as number,
  spec: '',
  unit: '',
  brand: '',
  // Store 字段（可编辑）
  defaultSupplierId: undefined as number | undefined,
  defaultSupplierName: '' as string,
  price: 0,
  costPrice: 0,
  lowStockThreshold: undefined as number | undefined,
  expiryAlertDays: undefined as number | undefined,
  overstockThreshold: undefined as number | undefined,
  imageUrl: '',
  status: 1 as number,
  remark: ''
})

const formRules: FormRules = {
  masterId: [
    { required: true, message: '请选择商品主档', trigger: 'change' }
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
  formData.masterId = undefined
  formData.name = ''
  formData.code = ''
  formData.categoryId = undefined
  formData.type = 1
  formData.spec = ''
  formData.unit = ''
  formData.brand = ''
  formData.defaultSupplierId = undefined
  formData.defaultSupplierName = ''
  formData.price = 0
  formData.costPrice = 0
  formData.lowStockThreshold = undefined
  formData.expiryAlertDays = undefined
  formData.overstockThreshold = undefined
  formData.imageUrl = ''
  formData.status = 1
  formData.remark = ''
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
  formData.masterId = row.masterId
  // Master 字段只读展示
  formData.name = row.name
  formData.code = row.code
  formData.categoryId = row.categoryId
  formData.type = row.type
  formData.spec = row.spec || ''
  formData.unit = row.unit || ''
  formData.brand = row.brand || ''
  // Store 字段可编辑
  formData.defaultSupplierId = row.defaultSupplierId ?? undefined
  formData.defaultSupplierName = row.defaultSupplierName || ''
  formData.price = row.price
  formData.costPrice = row.costPrice || 0
  formData.lowStockThreshold = row.lowStockThreshold ?? undefined
  formData.expiryAlertDays = row.expiryAlertDays ?? undefined
  formData.overstockThreshold = row.overstockThreshold ?? undefined
  formData.imageUrl = row.imageUrl || ''
  formData.status = row.status
  formData.remark = row.remark || ''
  dialogVisible.value = true
}

// 选择商品主档后自动填充 Master 字段（只读展示）
const handleMasterChange = async (masterId: number) => {
  const master = masterOptions.value.find(m => m.id === masterId)
  if (master) {
    formData.name = master.name
    formData.code = master.code
    formData.type = master.type
    formData.unit = master.unit || ''
  }
  // 获取详情填充其他 Master 字段（specification/brand/categoryId）
  try {
    const detail = await getProductMaster(masterId)
    formData.spec = detail.specification || ''
    formData.brand = detail.brand || ''
    formData.categoryId = detail.categoryId
  } catch {
    // 忽略，Master 字段保持空
  }
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
  } catch (error) {
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
  } catch (error) {
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
        const payload: ProductCreate = {
          masterId: formData.masterId as number,
          price: formData.price,
          costPrice: formData.costPrice || undefined,
          lowStockThreshold: formData.lowStockThreshold ?? undefined,
          expiryAlertDays: formData.expiryAlertDays ?? undefined,
          overstockThreshold: formData.overstockThreshold ?? undefined,
          status: formData.status as ProductStatus,
          remark: formData.remark || undefined
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
      } catch (error) {
        ElMessage.error((error as Error).message || '操作失败')
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

// ========== 管理供应商弹窗 ==========
const supplierDialogVisible = ref(false)
const supplierLoading = ref(false)
const productSupplierList = ref<ProductSupplier[]>([])
const addSupplierId = ref<number | undefined>(undefined)

// 未绑定的供应商选项（已绑定的供应商不在选项中）
const unboundSupplierOptions = computed(() => {
  const boundIds = productSupplierList.value.map(ps => ps.supplierId)
  return supplierOptions.value.filter(s => !boundIds.includes(s.id))
})

// 打开管理供应商弹窗
const handleManageSuppliers = async () => {
  if (!formData.id) {
    ElMessage.warning('请先保存商品再管理供应商')
    return
  }
  supplierDialogVisible.value = true
  addSupplierId.value = undefined
  await loadProductSuppliers()
}

// 加载品项已绑定的供应商列表
const loadProductSuppliers = async () => {
  if (!formData.id) return
  supplierLoading.value = true
  try {
    const list = await getSuppliersByProduct(formData.id)
    // 供货周期 0 表示即时供货，前端展示为空避免显示默认值 0
    list.forEach(ps => {
      if (ps.leadTimeDays === 0) ps.leadTimeDays = undefined
    })
    productSupplierList.value = list
    // 同步默认供应商展示
    const def = list.find(ps => ps.isDefault)
    formData.defaultSupplierId = def?.supplierId
    formData.defaultSupplierName = def?.supplierName || ''
  } catch (error) {
    ElMessage.error((error as Error).message || '加载供应商失败')
  } finally {
    supplierLoading.value = false
  }
}

// 添加供应商
const handleAddSupplier = async () => {
  if (!addSupplierId.value) return
  try {
    await bindProducts({ supplierId: addSupplierId.value, productIds: [formData.id] })
    ElMessage.success('添加成功')
    addSupplierId.value = undefined
    await loadProductSuppliers()
    loadData()
  } catch (error) {
    ElMessage.error((error as Error).message || '添加失败')
  }
}

// 设为默认供应商
const handleSetDefault = async (row: ProductSupplier) => {
  try {
    await setDefaultSupplier({
      productId: formData.id,
      supplierId: row.supplierId,
      referencePrice: row.referencePrice,
      leadTimeDays: row.leadTimeDays
    })
    ElMessage.success('已设为默认')
    await loadProductSuppliers()
    loadData()
  } catch (error) {
    ElMessage.error((error as Error).message || '设置失败')
  }
}

// 更新参考价（即时保存）
const handleUpdateReferencePrice = async (row: ProductSupplier, val: number | undefined) => {
  // 输入为空或非法时不更新，恢复后端值，避免无意义请求与误导性的成功提示
  if (val == null || Number.isNaN(val)) {
    await loadProductSuppliers()
    return
  }
  try {
    await updateProductRelation({
      productId: formData.id,
      supplierId: row.supplierId,
      referencePrice: val,
      leadTimeDays: row.leadTimeDays
    })
    ElMessage.success('参考价已更新')
  } catch (error) {
    ElMessage.error((error as Error).message || '更新失败')
    await loadProductSuppliers()
  }
}

// 更新供货周期（即时保存）
const handleUpdateLeadTime = async (row: ProductSupplier, val: number | undefined) => {
  // 供货周期为空时按默认 0（即时供货）处理
  const leadTimeDays = val ?? 0
  try {
    await updateProductRelation({
      productId: formData.id,
      supplierId: row.supplierId,
      referencePrice: row.referencePrice,
      leadTimeDays
    })
    ElMessage.success('供货周期已更新')
  } catch (error) {
    ElMessage.error((error as Error).message || '更新失败')
    await loadProductSuppliers()
  }
}

// 解除关联
const handleUnbind = async (row: ProductSupplier) => {
  try {
    await ElMessageBox.confirm(
      `确定解除与供应商 "${row.supplierName}" 的关联吗？`,
      '警告',
      { type: 'warning', confirmButtonText: '确定', cancelButtonText: '取消' }
    )
    await unbindProduct(formData.id, row.supplierId)
    ElMessage.success('解除关联成功')
    await loadProductSuppliers()
    loadData()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error((error as Error).message || '解除关联失败')
    }
  }
}

// 格式化价格
const formatPrice = (price: number | undefined) => {
  if (price === null || price === undefined) return '0.00'
  return price.toFixed(2)
}

// 商品类型名称映射
const getProductTypeName = (type: number) => {
  const map: Record<number, string> = {
    1: '实物商品',
    2: '服务项目',
    3: '耗材',
    4: '样品',
    5: '赠品'
  }
  return map[type] || '-'
}

onMounted(async () => {
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  pagination.pageSize = systemConfigStore.defaultPageSize
  loadCategoryTree()
  loadSuppliers()
  loadMasterOptions()
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
  padding: 20px 24px 20px;
}

.search-form-inline {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

/* 覆盖 el-form inline 模式下 form-item 默认的 margin-right，避免与 gap 叠加导致按钮换行 */
.search-form-inline :deep(.el-form-item) {
  margin-right: 0;
  margin-bottom: 0;
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

.price-text {
  color: var(--primary);
  font-weight: 600;
}

.cost-text {
  color: var(--text-tertiary);
}

/* 服务时长输入框 + 后缀 */
.duration-input-group {
  display: flex;
  align-items: center;
  gap: 8px;
}

.input-suffix {
  color: var(--text-tertiary);
  font-size: 14px;
  flex-shrink: 0;
}

/* 默认供应商展示 + 管理按钮 */
.supplier-display {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  width: 100%;
}

.supplier-display .supplier-name {
  color: #1f2937;
  font-size: 14px;
}

.supplier-display .supplier-name.is-empty {
  color: #9ca3af;
}

/* 管理供应商弹窗 - 添加供应商栏 */
.add-supplier-bar {
  display: flex;
  align-items: center;
  gap: 12px;
}

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}

/* 隐藏 el-input-number 在 controls=false 时内部 input 的原生数字调节箭头 */
:deep(.el-input-number.is-without-controls .el-input__inner::-webkit-inner-spin-button),
:deep(.el-input-number.is-without-controls .el-input__inner::-webkit-outer-spin-button) {
  -webkit-appearance: none;
  margin: 0;
}

:deep(.el-input-number.is-without-controls .el-input__inner) {
  -moz-appearance: textfield;
}
</style>
