<template>
  <div class="product-master-management">
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
          新增主档
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
        <el-table-column prop="code" label="商品编码" width="140" show-overflow-tooltip />
        <el-table-column prop="name" label="商品名称" min-width="160" show-overflow-tooltip />
        <el-table-column label="商品类型" width="100">
          <template #default="{ row }">
            {{ getProductTypeName(row.type) }}
          </template>
        </el-table-column>
        <el-table-column prop="categoryName" label="分类" width="120" />
        <el-table-column prop="specification" label="规格" width="120" show-overflow-tooltip />
        <el-table-column prop="unit" label="单位" width="80" />
        <el-table-column prop="brand" label="品牌" width="120" show-overflow-tooltip />
        <el-table-column label="可销售" width="90" align="center">
          <template #default="{ row }">
            <el-tag :type="row.isSalable ? 'success' : 'info'" size="small" effect="dark">
              {{ row.isSalable ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="280" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleEdit(row)">
              <el-icon><Edit /></el-icon>
              编辑
            </el-button>
            <el-button link type="success" size="small" @click="handleConfigStore(row)">
              <el-icon><Setting /></el-icon>
              编辑门店档案
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

    <!-- 新增/编辑主档弹窗 -->
    <el-dialog
      v-model="dialogVisible"
      :title="isEdit ? '编辑商品主档' : '新增商品主档'"
      width="640px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="100px"
      >
        <el-alert
          v-if="isEdit"
          title="Master 字段修改全门店同步生效，所有门店档案的对应字段都会同步变更"
          type="warning"
          :closable="false"
          show-icon
          style="margin-bottom: 16px"
        />
        <el-row :gutter="16">
        <el-col :span="12">
            <el-form-item label="商品名称" prop="name">
              <el-input v-model="formData.name" placeholder="请输入商品名称" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="商品编码" prop="code">
              <el-input v-model="formData.code" placeholder="编码唯一" :disabled="isEdit" />
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
                <el-option label="样品" :value="4" />
                <el-option label="赠品" :value="5" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="规格" prop="specification">
              <el-input v-model="formData.specification" placeholder="如 500ml / 红色/L" />
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
        </el-row>

        <!-- 服务项目子表字段（type=2）-->
        <template v-if="formData.type === 2">
          <el-divider content-position="left">服务项目信息</el-divider>
          <el-row :gutter="16">
            <el-col :span="12">
              <el-form-item label="服务时长" prop="duration">
                <div class="duration-input-group">
                  <el-input-number
                    v-model="formData.duration"
                    :min="0"
                    :step="15"
                    :controls="false"
                    style="width: 140px"
                  />
                  <span class="input-suffix">分钟</span>
                </div>
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item label="服务位" prop="requiredRoomType">
                <el-select v-model="formData.requiredRoomType" placeholder="不限" clearable style="width: 140px">
                  <el-option label="房间" :value="1" />
                  <el-option label="床位" :value="2" />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="16">
            <el-col :span="24">
              <el-form-item label="所需仪器" prop="equipmentTypeIds">
                <el-select
                  v-model="formData.equipmentTypeIds"
                  multiple
                  filterable
                  placeholder="请选择所需仪器"
                  style="width: 100%"
                >
                  <el-option
                    v-for="item in equipmentTypeOptions"
                    :key="item.id"
                    :label="item.name"
                    :value="item.id"
                  />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="16">
            <el-col :span="24">
              <el-form-item label="适用技能" prop="skillCategoryIds">
                <el-tree-select
                  v-model="formData.skillCategoryIds"
                  :data="skillCategoryTree"
                  :props="{ label: 'name', children: 'children' }"
                  node-key="id"
                  multiple
                  filterable
                  clearable
                  check-strictly
                  placeholder="请选择适用技能分类（选父级自动匹配其所有子级）"
                  style="width: 100%"
                />
              </el-form-item>
            </el-col>
          </el-row>
          <el-form-item label="可服务技师">
            <template v-if="isEdit">
              <div v-loading="availableTechniciansLoading" class="available-technicians">
                <el-tag
                  v-for="t in availableTechnicians"
                  :key="t.id"
                  size="small"
                  effect="plain"
                  type="success"
                  class="skill-tag"
                >
                  {{ t.name }}{{ t.phone ? `（${t.phone}）` : '' }}
                </el-tag>
                <span v-if="!availableTechniciansLoading && availableTechnicians.length === 0" class="text-muted">
                  暂无匹配技师
                </span>
              </div>
            </template>
            <span v-else class="text-muted">保存后展示可服务技师</span>
          </el-form-item>
        </template>

        <el-form-item label="备注" prop="remark">
          <el-input v-model="formData.remark" type="textarea" :rows="3" placeholder="主档级备注" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          确定
        </el-button>
      </template>
    </el-dialog>

    <!-- 编辑门店档案弹窗A -->
    <el-dialog
      v-model="storeConfigDialogVisible"
      :title="`编辑门店档案 - ${currentMaster?.name || ''}`"
      width="600px"
      :close-on-click-modal="false"
    >
      <el-alert
        title="将以下 Store 字段值应用到选中门店：已有档案 -> 覆盖；无档案 -> 自动创建"
        type="info"
        :closable="false"
        show-icon
        style="margin-bottom: 16px"
      />
      <el-form
        ref="storeConfigFormRef"
        :model="storeConfigForm"
        :rules="storeConfigRules"
        label-width="120px"
      >
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="售价" prop="price">
              <el-input-number
                v-model="storeConfigForm.price"
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
                v-model="storeConfigForm.costPrice"
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
                v-model="storeConfigForm.lowStockThreshold"
                :min="0"
                :step="1"
                :controls="false"
                style="width: 100%"
                placeholder="留空不预警"
                :disabled="currentMaster?.type === 2"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="积压阈值" prop="overstockThreshold">
              <el-input-number
                v-model="storeConfigForm.overstockThreshold"
                :min="0"
                :step="1"
                :controls="false"
                style="width: 100%"
                placeholder="留空不预警"
                :disabled="currentMaster?.type === 2"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="效期预警天数" prop="expiryAlertDays">
              <el-input-number
                v-model="storeConfigForm.expiryAlertDays"
                :min="1"
                :step="1"
                :controls="false"
                style="width: 100%"
                placeholder="留空不预警"
                :disabled="currentMaster?.type === 2"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="上架状态" prop="status">
              <el-radio-group v-model="storeConfigForm.status">
                <el-radio :value="1">上架</el-radio>
                <el-radio :value="2">下架</el-radio>
              </el-radio-group>
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="备注" prop="remark">
          <el-input v-model="storeConfigForm.remark" type="textarea" :rows="2" placeholder="分店级备注" />
        </el-form-item>
        <el-form-item label="应用门店">
          <div class="store-selector-bar">
            <span class="selected-count">已选 {{ selectedStoreIds.length }} 家门店</span>
            <el-button type="primary" plain size="small" @click="openStoreSelector">
              <el-icon><Select /></el-icon>
              选择门店
            </el-button>
          </div>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="storeConfigDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="storeConfigSubmitLoading" @click="handleStoreConfigSubmit">
          保存
        </el-button>
      </template>
    </el-dialog>

    <!-- 门店选择器弹窗B -->
    <el-dialog
      v-model="storeSelectorVisible"
      title="选择门店"
      width="600px"
      :close-on-click-modal="false"
      append-to-body
    >
      <div class="store-selector-toolbar">
        <el-checkbox v-model="storeSelectAll" @change="handleStoreSelectAll">全选</el-checkbox>
        <span class="selected-count">{{ selectedStoreIds.length }} / {{ storeOptions.length }} 家</span>
      </div>
      <el-table
        :data="storeOptions"
        max-height="400"
        style="width: 100%"
        @selection-change="handleStoreSelectionChange"
        ref="storeTableRef"
      >
        <el-table-column type="selection" width="50" />
        <el-table-column prop="code" label="门店编码" width="120" />
        <el-table-column prop="name" label="门店名称" min-width="160" show-overflow-tooltip />
        <el-table-column prop="shortName" label="简称" width="120" />
      </el-table>
      <template #footer>
        <el-button @click="storeSelectorVisible = false">取消</el-button>
        <el-button type="primary" @click="handleStoreSelectorConfirm">确认选择</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, nextTick } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus, Delete, Edit, Setting, Select } from '@element-plus/icons-vue'
import {
  getProductMasters,
  createProductMaster,
  updateProductMaster,
  deleteProductMaster,
  batchConfigStoreFields,
  getStoreConfigPreview,
  type ProductMaster,
  type ProductMasterCreate,
  type ProductStoreBatchConfig
} from '@/api/product/master'
import { getCategoryTree } from '@/api/product'
import { getAuthorizedStores } from '@/api/store'
import { getEquipmentTypeOptions } from '@/api/equipment-type'
import { getSkillCategoryTree } from '@/api/skill'
import { getTechniciansAvailableByService } from '@/api/staff'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { ProductCategory, ProductType } from '@/api/product/types'
import type { EquipmentType } from '@/api/equipment-type'
import type { Store } from '@/api/store/types'
import type { SkillCategory } from '@/api/skill'
import type { Technician } from '@/api/staff/types'

const systemConfigStore = useSystemConfigStore()

// 分类树（租户级共享）
const categoryTree = ref<ProductCategory[]>([])

// 门店列表（用于门店选择器弹窗B）
const storeOptions = ref<Store[]>([])

// 设备类型列表（仅用于服务项目所需仪器多选，租户级共享）
const equipmentTypeOptions = ref<EquipmentType[]>([])

// 技能分类树（服务项目适用技能多选）
const skillCategoryTree = ref<SkillCategory[]>([])

// 加载分类树
const loadCategoryTree = async () => {
  try {
    categoryTree.value = await getCategoryTree()
  } catch {
    categoryTree.value = []
  }
}

// 加载技能分类树
const loadSkillCategoryTree = async () => {
  try {
    skillCategoryTree.value = await getSkillCategoryTree({})
  } catch {
    skillCategoryTree.value = []
  }
}

// 加载门店列表
const loadStores = async () => {
  try {
    storeOptions.value = await getAuthorizedStores()
  } catch {
    storeOptions.value = []
  }
}

// 加载设备类型列表
const loadEquipmentTypes = async () => {
  try {
    equipmentTypeOptions.value = await getEquipmentTypeOptions()
  } catch {
    equipmentTypeOptions.value = []
  }
}

// 搜索表单
const searchForm = reactive({
  name: '',
  code: '',
  categoryId: undefined as number | undefined,
  type: undefined as ProductType | undefined
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<ProductMaster[]>([])

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
    const res = await getProductMasters({
      name: searchForm.name || undefined,
      code: searchForm.code || undefined,
      categoryId: searchForm.categoryId,
      type: searchForm.type,
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
  handleSearch()
}

// ========== 主档 CRUD 弹窗 ==========
const dialogVisible = ref(false)
const isEdit = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()

const formData = reactive({
  id: 0,
  code: '',
  name: '',
  categoryId: undefined as number | undefined,
  type: 1 as number,
  specification: '',
  unit: '',
  brand: '',
  remark: '',
  // 服务项目子表字段（type=2）
  duration: undefined as number | undefined,
  requiredRoomType: undefined as number | undefined,
  equipmentTypeIds: [] as number[],
  skillCategoryIds: [] as number[]
})

const formRules: FormRules = {
  code: [
    { required: true, message: '商品编码不能为空', trigger: 'blur' },
    { min: 2, max: 50, message: '商品编码长度为2-50个字符', trigger: 'blur' },
    { pattern: /^[a-zA-Z0-9_]+$/, message: '商品编码只能包含字母、数字、下划线', trigger: 'blur' }
  ],
  name: [
    { required: true, message: '商品名称不能为空', trigger: 'blur' },
    { max: 100, message: '商品名称最多100个字符', trigger: 'blur' }
  ],
  categoryId: [
    { required: true, message: '请选择商品分类', trigger: 'change' }
  ],
  type: [
    { required: true, message: '请选择商品类型', trigger: 'change' }
  ]
}

// 重置表单
const resetFormData = () => {
  formData.id = 0
  formData.code = ''
  formData.name = ''
  formData.categoryId = undefined
  formData.type = 1
  formData.specification = ''
  formData.unit = ''
  formData.brand = ''
  formData.remark = ''
  formData.duration = undefined
  formData.requiredRoomType = undefined
  formData.equipmentTypeIds = []
  formData.skillCategoryIds = []
}

// 新增
const handleAdd = () => {
  isEdit.value = false
  resetFormData()
  dialogVisible.value = true
}

// 可服务技师列表（服务项目编辑时展示技能匹配结果，双向展示用）
const availableTechnicians = ref<Technician[]>([])
const availableTechniciansLoading = ref(false)

// 加载指定主档对应的可服务技师（后端按 masterId 反查 ServiceProduct 后技能匹配）
const loadAvailableTechnicians = async (masterId: number) => {
  availableTechniciansLoading.value = true
  try {
    availableTechnicians.value = await getTechniciansAvailableByService(undefined, masterId)
  } catch {
    availableTechnicians.value = []
  } finally {
    availableTechniciansLoading.value = false
  }
}

// 编辑
const handleEdit = (row: ProductMaster) => {
  isEdit.value = true
  formData.id = row.id
  formData.code = row.code
  formData.name = row.name
  formData.categoryId = row.categoryId
  formData.type = row.type
  formData.specification = row.specification || ''
  formData.unit = row.unit || ''
  formData.brand = row.brand || ''
  formData.remark = row.remark || ''
  formData.duration = row.duration
  formData.requiredRoomType = row.requiredRoomType
  formData.equipmentTypeIds = row.equipmentTypeIds ? [...row.equipmentTypeIds] : []
  formData.skillCategoryIds = row.skillCategoryIds ? [...row.skillCategoryIds] : []
  // 服务项目展示可服务技师（按技能匹配）；其他类型清空
  if (row.type === 2) {
    loadAvailableTechnicians(row.id)
  } else {
    availableTechnicians.value = []
  }
  dialogVisible.value = true
}

// 删除
const handleDelete = async (row: ProductMaster) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除商品主档 "${row.name}" 吗？将同时软删除所有关联门店档案。`,
      '警告',
      { type: 'warning', confirmButtonText: '确定删除', cancelButtonText: '取消' }
    )
    await deleteProductMaster(row.id)
    ElMessage.success('删除成功')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error(error.message || '删除失败')
    }
  }
}

// 提交主档表单
const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (valid) {
      submitLoading.value = true
      try {
        const payload: ProductMasterCreate = {
          code: formData.code,
          name: formData.name,
          type: formData.type as ProductType,
          categoryId: formData.categoryId!,
          specification: formData.specification || undefined,
          unit: formData.unit || undefined,
          brand: formData.brand || undefined,
          remark: formData.remark || undefined
        }
        // 服务项目子表字段
        if (formData.type === 2) {
          payload.duration = formData.duration
          payload.requiredRoomType = formData.requiredRoomType as any
          payload.equipmentTypeIds = formData.equipmentTypeIds
          payload.skillCategoryIds = formData.skillCategoryIds
        }
        if (isEdit.value) {
          await updateProductMaster({ ...payload, id: formData.id })
          ElMessage.success('更新成功（全门店同步生效）')
        } else {
          await createProductMaster(payload)
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

// ========== 编辑门店档案弹窗A + 门店选择器弹窗B ==========
const storeConfigDialogVisible = ref(false)
const storeConfigSubmitLoading = ref(false)
const storeConfigFormRef = ref<FormInstance>()
const currentMaster = ref<ProductMaster | null>(null)

const storeConfigForm = reactive({
  price: 0,
  costPrice: undefined as number | undefined,
  lowStockThreshold: undefined as number | undefined,
  expiryAlertDays: undefined as number | undefined,
  overstockThreshold: undefined as number | undefined,
  status: 1 as 1 | 2,
  remark: ''
})

const storeConfigRules: FormRules = {
  price: [
    { required: true, message: '售价不能为空', trigger: 'blur' },
    { type: 'number', min: 0, message: '售价必须大于等于0', trigger: 'blur' }
  ]
}

// 选中的门店ID列表（Store.id 是 string，需转为 number 传给后端）
const selectedStoreIds = ref<string[]>([])

// 门店选择器弹窗B
const storeSelectorVisible = ref(false)
const storeSelectAll = ref(false)
const storeTableRef = ref()

// 打开编辑门店档案弹窗A
const handleConfigStore = (row: ProductMaster) => {
  currentMaster.value = row
  // 重置 Store 字段
  storeConfigForm.price = 0
  storeConfigForm.costPrice = undefined
  storeConfigForm.lowStockThreshold = undefined
  storeConfigForm.expiryAlertDays = undefined
  storeConfigForm.overstockThreshold = undefined
  storeConfigForm.status = 1
  storeConfigForm.remark = ''
  // 重置选中门店
  selectedStoreIds.value = []
  storeConfigDialogVisible.value = true
}

// 打开门店选择器弹窗B
const openStoreSelector = async () => {
  storeSelectorVisible.value = true
  await nextTick()
  // 恢复已选中状态
  if (storeTableRef.value) {
    storeOptions.value.forEach(store => {
      if (selectedStoreIds.value.includes(store.id)) {
        storeTableRef.value.toggleRowSelection(store, true)
      }
    })
  }
}

// 门店选择变化（等确认时再统一读取选中行，此处无需处理）
const handleStoreSelectionChange = () => {
  // 占位：保持事件绑定，实际选中行在 handleStoreSelectorConfirm 中通过 getSelectionRows 读取
}

// 全选
const handleStoreSelectAll = () => {
  if (storeTableRef.value) {
    storeTableRef.value.toggleAllSelection()
  }
}

// 确认门店选择
const handleStoreSelectorConfirm = () => {
  // 从表格获取选中行
  const selected = storeTableRef.value?.getSelectionRows() as Store[] | undefined
  if (!selected || selected.length === 0) {
    selectedStoreIds.value = []
  } else {
    selectedStoreIds.value = selected.map(s => s.id)
  }
  storeSelectorVisible.value = false
}

// 提交门店档案配置
const handleStoreConfigSubmit = async () => {
  if (!currentMaster.value) return
  if (selectedStoreIds.value.length === 0) {
    ElMessage.warning('请先选择至少一家门店')
    return
  }
  if (!storeConfigFormRef.value) return
  await storeConfigFormRef.value.validate(async (valid) => {
    if (valid) {
      // 二次确认（对应设计文档 7.3 节）：动态查询哪些门店已有档案将被覆盖、哪些将新建
      // storeIds 为雪花ID字符串，直接传递避免 Number() 转换丢失精度
      let preview
      try {
        preview = await getStoreConfigPreview(currentMaster.value!.id, selectedStoreIds.value)
      } catch (error: any) {
        ElMessage.error(error.message || '获取门店档案预览失败')
        return
      }

      // 根据预览结果分组门店名称
      const existingIdSet = new Set(preview.existingStoreIds)
      const selectedStores = storeOptions.value.filter(s => selectedStoreIds.value.includes(s.id))
      const existingNames = selectedStores.filter(s => existingIdSet.has(s.id)).map(s => s.name)
      const newNames = selectedStores.filter(s => !existingIdSet.has(s.id)).map(s => s.name)

      // 动态拼装确认文案
      const lines: string[] = [`将应用到以下 ${selectedStoreIds.value.length} 家门店：`]
      if (existingNames.length > 0) {
        lines.push(`【覆盖】已有档案（${existingNames.length} 家）：${existingNames.join('、')}`)
      }
      if (newNames.length > 0) {
        lines.push(`【新建】无档案（${newNames.length} 家）：${newNames.join('、')}`)
      }

      try {
        await ElMessageBox.confirm(
          lines.join('\n'),
          '二次确认',
          { type: 'warning', confirmButtonText: '确认应用', cancelButtonText: '取消' }
        )
      } catch {
        return
      }

      storeConfigSubmitLoading.value = true
      try {
        const payload: Omit<ProductStoreBatchConfig, 'masterId'> = {
          storeIds: selectedStoreIds.value,
          price: storeConfigForm.price,
          costPrice: storeConfigForm.costPrice ?? undefined,
          lowStockThreshold: storeConfigForm.lowStockThreshold ?? undefined,
          expiryAlertDays: storeConfigForm.expiryAlertDays ?? undefined,
          overstockThreshold: storeConfigForm.overstockThreshold ?? undefined,
          status: storeConfigForm.status,
          remark: storeConfigForm.remark || undefined
        }
        await batchConfigStoreFields(currentMaster.value!.id, payload)
        ElMessage.success('门店档案配置已应用')
        storeConfigDialogVisible.value = false
      } catch (error: any) {
        ElMessage.error(error.message || '操作失败')
      } finally {
        storeConfigSubmitLoading.value = false
      }
    }
  })
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
  loadStores()
  loadEquipmentTypes()
  loadSkillCategoryTree()
  loadData()
})
</script>

<style scoped>
.product-master-management {
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

/* 可服务技师标签列表 */
.available-technicians {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  width: 100%;
  min-height: 32px;
}

.available-technicians .skill-tag {
  margin: 0;
}

/* 空状态占位 */
.text-muted {
  color: var(--text-tertiary);
  line-height: 32px;
}

/* 门店选择器 */
.store-selector-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  width: 100%;
}

.selected-count {
  color: var(--text-tertiary);
  font-size: 14px;
}

.store-selector-toolbar {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-bottom: 12px;
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
