<template>
  <div class="supplier-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="供应商名称">
            <el-input
              v-model="searchForm.name"
              placeholder="请输入供应商名称"
              clearable
              style="width: 180px"
            />
          </el-form-item>
          <el-form-item label="合作状态">
            <el-select v-model="searchForm.status" placeholder="全部状态" clearable style="width: 130px">
              <el-option label="合作中" :value="1" />
              <el-option label="已停止" :value="2" />
            </el-select>
          </el-form-item>
          <el-form-item label="数据范围">
            <el-select v-model="searchForm.scope" placeholder="全部范围" clearable style="width: 130px">
              <el-option label="门店通用" :value="1" />
              <el-option label="本门店" :value="2" />
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
          新增供应商
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
        <el-table-column prop="name" label="供应商名称" min-width="180" />
        <el-table-column prop="contact" label="联系人" width="100" />
        <el-table-column prop="phone" label="联系电话" width="130" />
        <el-table-column prop="totalPurchaseAmount" label="累计采购额" width="130" align="right">
          <template #default="{ row }">
            ¥{{ (row.totalPurchaseAmount || 0).toFixed(2) }}
          </template>
        </el-table-column>
        <el-table-column label="合作状态" width="100">
          <template #default="{ row }">
            <el-tag :type="row.status === 1 ? 'success' : 'info'" size="small" effect="dark">
              {{ row.status === 1 ? '合作中' : '已停止' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="数据范围" width="100">
          <template #default="{ row }">
            <el-tag v-if="row.scope === 1" type="primary" size="small">门店通用</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="220" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleViewProducts(row)">
              <el-icon><View /></el-icon>
              管理品项
            </el-button>
            <el-button v-if="canEditSupplier(row)" link type="primary" size="small" @click="handleEdit(row)">
              <el-icon><Edit /></el-icon>
              编辑
            </el-button>
            <el-button v-if="canEditSupplier(row)" link type="danger" size="small" @click="handleDelete(row)">
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
      :title="isEdit ? '编辑供应商' : '新增供应商'"
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
            <el-form-item label="供应商名称" prop="name">
              <el-input v-model="formData.name" placeholder="请输入供应商名称" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="供应商编码" prop="code">
              <el-input v-model="formData.code" placeholder="如 GYS-001" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="联系人" prop="contact">
              <el-input v-model="formData.contact" placeholder="请输入联系人" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="联系电话" prop="phone">
              <el-input v-model="formData.phone" placeholder="请输入联系电话" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="银行账户" prop="bankAccount">
          <el-input v-model="formData.bankAccount" placeholder="请输入银行账户" />
        </el-form-item>
        <el-form-item label="地址" prop="address">
          <el-input v-model="formData.address" placeholder="请输入供应商地址" />
        </el-form-item>
        <el-form-item v-if="canCreatePublic" label="数据范围" prop="scope">
          <el-radio-group v-model="formData.scope">
            <el-radio :value="2">本门店</el-radio>
            <el-radio :value="1">门店通用</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="合作状态" prop="status">
          <el-radio-group v-model="formData.status">
            <el-radio :value="1">合作中</el-radio>
            <el-radio :value="2">已停止</el-radio>
          </el-radio-group>
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

    <!-- 管理品项弹窗 -->
    <el-dialog
      v-model="productDialogVisible"
      :title="`管理品项 - ${currentSupplierName}`"
      width="900px"
      :close-on-click-modal="false"
    >
      <el-alert
        title="本弹窗内的修改即时保存，无需点击确定按钮"
        type="info"
        :closable="false"
        show-icon
        style="margin-bottom: 16px"
      />
      <!-- 添加品项 -->
      <div class="add-product-bar">
        <el-select
          v-model="addProductId"
          placeholder="选择未绑定的品项"
          filterable
          clearable
          style="width: 360px"
        >
          <el-option
            v-for="item in unboundProductOptions"
            :key="item.id"
            :label="`${item.name} (${item.code})`"
            :value="item.id"
          />
        </el-select>
        <el-button
          type="primary"
          :disabled="!addProductId"
          @click="handleAddProduct"
        >
          添加品项
        </el-button>
      </div>
      <!-- 已绑定品项列表 -->
      <el-table
        v-loading="productLoading"
        :data="supplierProductList"
        style="width: 100%; margin-top: 12px"
      >
        <el-table-column prop="productName" label="商品名称" min-width="160" show-overflow-tooltip />
        <el-table-column prop="productCode" label="商品编码" width="140" show-overflow-tooltip />
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
              @change="(val: number | undefined) => handleUpdateProductReferencePrice(row, val)"
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
              @change="(val: number | undefined) => handleUpdateProductLeadTime(row, val)"
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
              @click="handleSetDefaultProduct(row)"
            >
              设为默认
            </el-button>
            <el-button
              link
              type="danger"
              size="small"
              @click="handleUnbindProduct(row)"
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
import { Search, Refresh, Plus, Delete, Edit, View } from '@element-plus/icons-vue'
import {
  getSuppliers,
  createSupplier,
  updateSupplier,
  deleteSupplier,
  deleteSuppliers,
  getProductsBySupplier,
  bindProducts,
  unbindProduct,
  setDefaultSupplier
} from '@/api/supplier'
import { getProducts } from '@/api/product'
import type { Product } from '@/api/product/types'
import type { Supplier, ProductSupplier } from '@/api/supplier/types'
import { useSystemConfigStore } from '@/stores/systemConfig'
import { useUserStore } from '@/stores/user'

const systemConfigStore = useSystemConfigStore()
const userStore = useUserStore()

// 是否可创建公用供应商（前端按权限码控制显隐，后端不额外校验）
const canCreatePublic = computed(() => userStore.hasPermission('store:product:supplier:create:public'))
// 是否可编辑公用供应商
const canEditPublic = computed(() => userStore.hasPermission('store:product:supplier:edit:public'))
// 公用供应商的编辑/删除需要 edit:public 权限；私用供应商有 edit 权限即可
const canEditSupplier = (row: Supplier) => row.scope !== 1 || canEditPublic.value

// 搜索表单
const searchForm = reactive({
  name: '',
  status: undefined as number | undefined,
  scope: undefined as number | undefined
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<Supplier[]>([])
const selectedRows = ref<Supplier[]>([])

// 管理品项弹窗
const productDialogVisible = ref(false)
const productLoading = ref(false)
const currentSupplierName = ref('')
const currentSupplierId = ref<number>(0)
const supplierProductList = ref<ProductSupplier[]>([])
const addProductId = ref<number | undefined>(undefined)
// 用于"添加品项"下拉的候选商品列表（当前门店全部商品，弹窗打开时加载一次）
const allProductOptions = ref<Product[]>([])

// 未绑定的品项选项（已绑定的品项不在选项中）
const unboundProductOptions = computed(() => {
  const boundIds = supplierProductList.value.map(ps => ps.productId)
  return allProductOptions.value.filter(p => !boundIds.includes(p.id))
})

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
    const res = await getSuppliers({
      name: searchForm.name || undefined,
      status: searchForm.status,
      scope: searchForm.scope,
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
  searchForm.status = undefined
  searchForm.scope = undefined
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
  contact: '',
  phone: '',
  address: '',
  bankAccount: '',
  status: 1,
  scope: 2,
  remark: ''
})

const formRules: FormRules = {
  name: [
    { required: true, message: '供应商名称不能为空', trigger: 'blur' },
    { max: 100, message: '供应商名称最多100个字符', trigger: 'blur' }
  ],
  code: [
    { required: true, message: '供应商编码不能为空', trigger: 'blur' },
    { max: 50, message: '供应商编码最多50个字符', trigger: 'blur' }
  ],
  phone: [
    { pattern: /^1[3-9]\d{9}$/, message: '请输入正确的手机号码', trigger: 'blur' }
  ],
  status: [
    { required: true, message: '请选择合作状态', trigger: 'change' }
  ]
}

// 重置表单
const resetFormData = () => {
  formData.id = 0
  formData.name = ''
  formData.code = ''
  formData.contact = ''
  formData.phone = ''
  formData.address = ''
  formData.bankAccount = ''
  formData.status = 1
  formData.scope = 2
  formData.remark = ''
}

// 新增
const handleAdd = () => {
  isEdit.value = false
  resetFormData()
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: Supplier) => {
  isEdit.value = true
  formData.id = row.id
  formData.name = row.name
  formData.code = row.code
  formData.contact = row.contact || ''
  formData.phone = row.phone || ''
  formData.address = row.address || ''
  formData.bankAccount = row.bankAccount || ''
  formData.status = row.status
  formData.scope = row.scope || 2
  formData.remark = row.remark || ''
  dialogVisible.value = true
}

// 删除
const handleDelete = async (row: Supplier) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除供应商 "${row.name}" 吗？此操作不可恢复！`,
      '警告',
      {
        type: 'warning',
        confirmButtonText: '确定删除',
        cancelButtonText: '取消'
      }
    )
    await deleteSupplier(row.id)
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
    await ElMessageBox.confirm(
      `确定要删除选中的 ${selectedRows.value.length} 个供应商吗？此操作不可恢复！`,
      '警告',
      {
        type: 'warning',
        confirmButtonText: '确定删除',
        cancelButtonText: '取消'
      }
    )
    const ids = selectedRows.value.map(row => row.id)
    await deleteSuppliers(ids)
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
        const payload = {
          name: formData.name,
          code: formData.code,
          contact: formData.contact,
          phone: formData.phone,
          address: formData.address,
          bankAccount: formData.bankAccount || undefined,
          status: formData.status,
          scope: formData.scope,
          remark: formData.remark || undefined
        }
        if (isEdit.value) {
          await updateSupplier({ ...payload, id: formData.id })
          ElMessage.success('更新成功')
        } else {
          await createSupplier(payload)
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

// 打开管理品项弹窗
const handleViewProducts = async (row: Supplier) => {
  currentSupplierName.value = row.name
  currentSupplierId.value = row.id
  addProductId.value = undefined
  productDialogVisible.value = true
  // 并行加载已绑定品项与候选品项列表
  await Promise.all([
    loadSupplierProducts(),
    loadAllProductOptions()
  ])
}

// 加载供应商已绑定的品项列表（不分页）
const loadSupplierProducts = async () => {
  productLoading.value = true
  try {
    const list = await getProductsBySupplier(currentSupplierId.value)
    // 供货周期 0 表示即时供货，前端展示为空避免显示默认值 0
    list.forEach(ps => {
      if (ps.leadTimeDays === 0) ps.leadTimeDays = undefined
    })
    supplierProductList.value = list
  } catch (error) {
    ElMessage.error((error as Error).message || '加载品项失败')
  } finally {
    productLoading.value = false
  }
}

// 加载当前门店全部商品（用于"添加品项"下拉），单页拉取较大 pageSize
const loadAllProductOptions = async () => {
  try {
    const res = await getProducts({ pageIndex: 1, pageSize: 500 })
    allProductOptions.value = res.list
  } catch {
    allProductOptions.value = []
  }
}

// 添加品项到当前供应商
const handleAddProduct = async () => {
  if (!addProductId.value) return
  try {
    await bindProducts({
      supplierId: currentSupplierId.value,
      productIds: [addProductId.value]
    })
    ElMessage.success('添加成功')
    addProductId.value = undefined
    await loadSupplierProducts()
  } catch (error) {
    ElMessage.error((error as Error).message || '添加失败')
  }
}

// 设为默认供应商
const handleSetDefaultProduct = async (row: ProductSupplier) => {
  try {
    await setDefaultSupplier({
      productId: row.productId,
      supplierId: currentSupplierId.value,
      referencePrice: row.referencePrice,
      leadTimeDays: row.leadTimeDays
    })
    ElMessage.success('已设为默认')
    await loadSupplierProducts()
  } catch (error) {
    ElMessage.error((error as Error).message || '设置失败')
  }
}

// 更新参考价（即时保存，仅默认供应商生效）
const handleUpdateProductReferencePrice = async (row: ProductSupplier, val: number | undefined) => {
  if (row.isDefault) {
    try {
      await setDefaultSupplier({
        productId: row.productId,
        supplierId: currentSupplierId.value,
        referencePrice: val,
        leadTimeDays: row.leadTimeDays
      })
      ElMessage.success('参考价已更新')
    } catch (error) {
      ElMessage.error((error as Error).message || '更新失败')
      await loadSupplierProducts()
    }
  }
}

// 更新供货周期（即时保存，仅默认供应商生效）
const handleUpdateProductLeadTime = async (row: ProductSupplier, val: number | undefined) => {
  if (row.isDefault) {
    // 供货周期为空时按默认 0（即时供货）处理
    const leadTimeDays = val ?? 0
    try {
      await setDefaultSupplier({
        productId: row.productId,
        supplierId: currentSupplierId.value,
        referencePrice: row.referencePrice,
        leadTimeDays
      })
      ElMessage.success('供货周期已更新')
    } catch (error) {
      ElMessage.error((error as Error).message || '更新失败')
      await loadSupplierProducts()
    }
  }
}

// 解除品项与当前供应商的关联
const handleUnbindProduct = async (row: ProductSupplier) => {
  try {
    await ElMessageBox.confirm(
      `确定解除与品项 "${row.productName}" 的关联吗？`,
      '警告',
      { type: 'warning', confirmButtonText: '确定', cancelButtonText: '取消' }
    )
    await unbindProduct(row.productId, currentSupplierId.value)
    ElMessage.success('解除关联成功')
    await loadSupplierProducts()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error((error as Error).message || '解除关联失败')
    }
  }
}

// 选择行
const handleSelectionChange = (rows: Supplier[]) => {
  selectedRows.value = rows
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
.supplier-management {
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

/* 查看品项弹窗内表格 - 浅色风格，与弹窗整体底色一致 */
:deep(.el-dialog .el-table) {
  --el-table-bg-color: #ffffff !important;
  --el-table-text-color: #1f2937 !important;
  --el-table-border-color: #e5e7eb !important;
  --el-table-header-bg-color: #f3f4f6 !important;
  --el-table-header-text-color: #4b5563 !important;
  --el-table-row-hover-bg-color: #f3f4f6 !important;
  background-color: #ffffff !important;
}

:deep(.el-dialog .el-table th.el-table__cell) {
  background-color: #f3f4f6 !important;
  color: #4b5563 !important;
  border-bottom: 1px solid #e5e7eb !important;
}

:deep(.el-dialog .el-table td.el-table__cell) {
  background-color: #ffffff !important;
  color: #1f2937 !important;
  border-bottom: 1px solid #e5e7eb !important;
}

:deep(.el-dialog .el-table__row) {
  background-color: #ffffff !important;
}

:deep(.el-dialog .el-table__row:hover > td.el-table__cell) {
  background-color: #f3f4f6 !important;
}

:deep(.el-dialog .el-table__body-wrapper) {
  background-color: #ffffff;
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

/* 价格文本 */
.price-text {
  color: var(--primary);
  font-weight: 600;
}

/* 管理品项弹窗 - 添加品项栏 */
.add-product-bar {
  display: flex;
  align-items: center;
  gap: 12px;
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
