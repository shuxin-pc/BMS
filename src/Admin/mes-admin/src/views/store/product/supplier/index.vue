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
        <el-table-column prop="address" label="地址" min-width="200" show-overflow-tooltip />
        <el-table-column prop="supplyCategory" label="供应品类" width="120" />
        <el-table-column prop="bankAccount" label="银行账户" width="180" show-overflow-tooltip>
          <template #default="{ row }">
            {{ row.bankAccount || '-' }}
          </template>
        </el-table-column>
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
        <el-table-column prop="remark" label="备注" min-width="150" show-overflow-tooltip />
        <el-table-column label="操作" width="220" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleViewProducts(row)">
              <el-icon><View /></el-icon>
              查看品项
            </el-button>
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
            <el-form-item label="联系人" prop="contact">
              <el-input v-model="formData.contact" placeholder="请输入联系人" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="联系电话" prop="phone">
              <el-input v-model="formData.phone" placeholder="请输入联系电话" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="供应品类" prop="supplyCategory">
              <el-input v-model="formData.supplyCategory" placeholder="如 洗护用品" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="地址" prop="address">
          <el-input v-model="formData.address" placeholder="请输入供应商地址" />
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

    <!-- 查看品项弹窗 -->
    <el-dialog
      v-model="productDialogVisible"
      :title="`供应商品项 - ${currentSupplierName}`"
      width="800px"
      :close-on-click-modal="false"
    >
      <el-table
        v-loading="productLoading"
        :data="productList"
        style="width: 100%"
      >
        <el-table-column prop="name" label="商品名称" min-width="160" />
        <el-table-column prop="code" label="商品编码" width="120" />
        <el-table-column prop="categoryName" label="分类" width="120" />
        <el-table-column label="售价" width="100" align="right">
          <template #default="{ row }">
            <span class="price-text">¥{{ (row.price || 0).toFixed(2) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="80">
          <template #default="{ row }">
            <el-tag :type="row.status === 1 ? 'success' : 'info'" size="small" effect="dark">
              {{ row.status === 1 ? '上架' : '下架' }}
            </el-tag>
          </template>
        </el-table-column>
      </el-table>
      <div class="pagination-container">
        <el-pagination
          v-model:current-page="productPagination.pageIndex"
          v-model:page-size="productPagination.pageSize"
          :page-sizes="systemConfigStore.defaultPageSizes"
          :total="productPagination.total"
          layout="total, prev, pager, next"
          @current-change="loadSupplierProducts"
        />
      </div>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus, Delete, Edit, View } from '@element-plus/icons-vue'
import {
  getSuppliers,
  createSupplier,
  updateSupplier,
  deleteSupplier,
  deleteSuppliers
} from '@/api/supplier'
import { getProducts } from '@/api/product'
import type { Product } from '@/api/product/types'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { Supplier } from '@/api/supplier/types'

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  name: '',
  status: undefined as number | undefined
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<Supplier[]>([])
const selectedRows = ref<Supplier[]>([])

// 查看品项
const productDialogVisible = ref(false)
const productLoading = ref(false)
const currentSupplierName = ref('')
const currentSupplierId = ref<number>(0)
const productList = ref<Product[]>([])
const productPagination = reactive({
  pageIndex: 1,
  pageSize: 10,
  total: 0
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
  contact: '',
  phone: '',
  address: '',
  supplyCategory: '',
  status: 1,
  remark: ''
})

const formRules: FormRules = {
  name: [
    { required: true, message: '供应商名称不能为空', trigger: 'blur' },
    { max: 100, message: '供应商名称最多100个字符', trigger: 'blur' }
  ],
  contact: [
    { required: true, message: '联系人不能为空', trigger: 'blur' }
  ],
  phone: [
    { required: true, message: '联系电话不能为空', trigger: 'blur' },
    { pattern: /^1[3-9]\d{9}$/, message: '请输入正确的手机号码', trigger: 'blur' }
  ],
  address: [
    { required: true, message: '地址不能为空', trigger: 'blur' }
  ],
  supplyCategory: [
    { required: true, message: '供应品类不能为空', trigger: 'blur' }
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
  formData.supplyCategory = ''
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
const handleEdit = (row: Supplier) => {
  isEdit.value = true
  formData.id = row.id
  formData.name = row.name
  formData.code = row.code
  formData.contact = row.contact || ''
  formData.phone = row.phone || ''
  formData.address = row.address || ''
  // TODO: 后端 Supplier 不返回 supplyCategory 字段
  formData.supplyCategory = ''
  formData.status = row.status
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
        const payload = {
          name: formData.name,
          code: formData.code,
          contact: formData.contact,
          phone: formData.phone,
          address: formData.address,
          status: formData.status,
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
      } catch (error: any) {
        ElMessage.error(error.message || '操作失败')
      } finally {
        submitLoading.value = false
      }
    }
  })
}

// 查看品项
const handleViewProducts = (row: Supplier) => {
  currentSupplierName.value = row.name
  currentSupplierId.value = row.id
  productPagination.pageIndex = 1
  productDialogVisible.value = true
  loadSupplierProducts()
}

// 加载供应商品项
const loadSupplierProducts = async () => {
  productLoading.value = true
  try {
    const res = await getProducts({
      supplierId: currentSupplierId.value,
      pageIndex: productPagination.pageIndex,
      pageSize: productPagination.pageSize
    })
    productList.value = res.list
    productPagination.total = res.total
  } catch (error) {
    ElMessage.error('加载品项失败')
  } finally {
    productLoading.value = false
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
</style>
