<template>
  <div v-loading="loading" class="beauty-profile-panel">
    <!-- 一客户仅一份美容档案，因此为详情卡片形态而非列表 -->
    <template v-if="profile">
      <div class="panel-toolbar">
        <el-button link type="primary" size="small" @click="handleEdit" v-if="hasPermission('store:customer:archive:beauty:edit')">
          <el-icon><Edit /></el-icon>
          编辑
        </el-button>
        <el-button link type="danger" size="small" @click="handleDelete" v-if="hasPermission('store:customer:archive:beauty:delete')">
          <el-icon><Delete /></el-icon>
          删除
        </el-button>
      </div>

      <div class="info-grid">
        <div class="info-item">
          <span class="info-label">肤质类型</span>
          <div class="info-value">
            <el-tag v-if="profile.skinType" :type="getSkinTypeTagType(profile.skinType)" size="small">
              {{ getSkinTypeText(profile.skinType) }}
            </el-tag>
            <span v-else class="text-muted">-</span>
          </div>
        </div>
        <div class="info-item">
          <span class="info-label">敏感程度</span>
          <div class="info-value">
            <el-tag
              v-if="profile.sensitivity"
              :type="getSensitivityTagType(profile.sensitivity)"
              size="small"
            >
              {{ getSensitivityText(profile.sensitivity) }}
            </el-tag>
            <span v-else class="text-muted">-</span>
          </div>
        </div>
        <div class="info-item">
          <span class="info-label">发质情况</span>
          <div class="info-value">{{ profile.hairType || '-' }}</div>
        </div>
        <div class="info-item">
          <span class="info-label">更新时间</span>
          <div class="info-value">{{ formatDateTime(profile.updatedAt || profile.createdAt) }}</div>
        </div>
        <div class="info-item info-item-full">
          <span class="info-label">过敏史 / 禁忌</span>
          <div class="info-value info-value-text">{{ profile.allergyHistory || '-' }}</div>
        </div>
        <div class="info-item info-item-full">
          <span class="info-label">备注</span>
          <div class="info-value info-value-text">{{ profile.remark || '-' }}</div>
        </div>
      </div>
    </template>

    <el-empty v-else description="该客户尚无美容档案">
      <el-button type="primary" @click="handleAdd" v-if="hasPermission('store:customer:archive:beauty:add')">
        <el-icon><Plus /></el-icon>
        新建档案
      </el-button>
    </el-empty>

    <!-- 新建/编辑弹窗 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="560px" @closed="handleDialogClosed">
      <el-form ref="formRef" :model="profileForm" label-width="100px">
        <el-form-item label="客户">
          <span>{{ props.customerName }}</span>
        </el-form-item>

        <el-form-item label="肤质类型">
          <el-select v-model="profileForm.skinType" placeholder="请选择" clearable style="width: 100%">
            <el-option label="干性" value="dry" />
            <el-option label="油性" value="oily" />
            <el-option label="中性" value="normal" />
            <el-option label="混合性" value="combination" />
            <el-option label="敏感性" value="sensitive" />
          </el-select>
        </el-form-item>

        <el-form-item label="敏感程度">
          <el-select v-model="profileForm.sensitivity" placeholder="请选择" clearable style="width: 100%">
            <el-option label="低" value="low" />
            <el-option label="中" value="medium" />
            <el-option label="高" value="high" />
          </el-select>
        </el-form-item>

        <el-form-item label="发质情况">
          <el-input v-model="profileForm.hairType" placeholder="请输入发质情况" maxlength="100" />
        </el-form-item>

        <el-form-item label="过敏史">
          <el-input
            v-model="profileForm.allergyHistory"
            type="textarea"
            :rows="3"
            placeholder="请输入过敏史和禁忌成分"
            maxlength="500"
            show-word-limit
          />
        </el-form-item>

        <el-form-item label="备注">
          <el-input
            v-model="profileForm.remark"
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
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance } from 'element-plus'
import { Plus, Edit, Delete } from '@element-plus/icons-vue'
import { useUserStore } from '@/stores/user'
import { formatDateTime } from '@/utils/date'
import {
  getBeautyProfiles,
  createBeautyProfile,
  updateBeautyProfile,
  deleteBeautyProfile
} from '@/api/customer-profile'
import type {
  CustomerBeautyProfile,
  BeautyProfileSave,
  SkinType,
  SensitivityLevel
} from '@/api/customer-profile/types'

const props = defineProps<{
  /** 当前客户ID，由服务档案容器页提供 */
  customerId: number
  /** 当前客户姓名，仅用于弹窗内只读展示 */
  customerName: string
}>()

const userStore = useUserStore()

const hasPermission = (permissionCode: string) => userStore.hasPermission(permissionCode)

const loading = ref(false)
const profile = ref<CustomerBeautyProfile | null>(null)

// 肤质类型文本
const getSkinTypeText = (type: SkinType): string => {
  const map: Record<string, string> = {
    dry: '干性',
    oily: '油性',
    combination: '混合性',
    sensitive: '敏感性',
    normal: '中性'
  }
  return map[type] || type
}

// 肤质类型标签颜色
const getSkinTypeTagType = (type: SkinType): '' | 'success' | 'info' | 'warning' | 'danger' => {
  const map: Record<string, '' | 'success' | 'info' | 'warning' | 'danger'> = {
    dry: 'warning',
    oily: 'info',
    combination: '',
    sensitive: 'danger',
    normal: 'success'
  }
  return map[type] || 'info'
}

// 敏感程度文本
const getSensitivityText = (level: SensitivityLevel): string => {
  const map: Record<string, string> = { low: '低', medium: '中', high: '高' }
  return map[level] || level
}

// 敏感程度标签颜色
const getSensitivityTagType = (level: SensitivityLevel): '' | 'success' | 'info' | 'warning' | 'danger' => {
  const map: Record<string, '' | 'success' | 'info' | 'warning' | 'danger'> = {
    low: 'success',
    medium: 'warning',
    high: 'danger'
  }
  return map[level] || 'info'
}


// 加载当前客户的美容档案（一客户一份，取首条即可）
const loadData = async () => {
  loading.value = true
  try {
    const res = await getBeautyProfiles({
      customerId: props.customerId,
      pageIndex: 1,
      pageSize: 1
    })
    profile.value = res.list[0] || null
  } catch {
    ElMessage.error('加载数据失败')
  } finally {
    loading.value = false
  }
}

// 客户切换时重载；首屏由 onMounted 负责，故不加 immediate
watch(
  () => props.customerId,
  () => {
    loadData()
  }
)

// ==================== 弹窗 ====================

const dialogVisible = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()
const isEdit = ref(false)

const dialogTitle = computed(() => (isEdit.value ? '编辑肤质档案' : '新建肤质档案'))

// 编辑时携带 id，新建时 id 为 0
const profileForm = reactive<Omit<BeautyProfileSave, 'customerId'> & { id: number }>({
  id: 0,
  skinType: undefined,
  sensitivity: undefined,
  hairType: '',
  allergyHistory: '',
  remark: ''
})

const handleAdd = () => {
  isEdit.value = false
  dialogVisible.value = true
}

const handleEdit = () => {
  if (!profile.value) return
  isEdit.value = true
  profileForm.id = profile.value.id
  profileForm.skinType = profile.value.skinType
  profileForm.sensitivity = profile.value.sensitivity
  profileForm.hairType = profile.value.hairType || ''
  profileForm.allergyHistory = profile.value.allergyHistory || ''
  profileForm.remark = profile.value.remark || ''
  dialogVisible.value = true
}

const handleDelete = async () => {
  if (!profile.value) return
  try {
    await ElMessageBox.confirm(
      `确定要删除客户 "${props.customerName}" 的肤质档案吗？`,
      '警告',
      { type: 'warning', confirmButtonText: '确定删除', cancelButtonText: '取消' }
    )
    await deleteBeautyProfile(profile.value.id)
    ElMessage.success('删除成功')
    loadData()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error((error as Error).message || '删除失败')
    }
  }
}

// 弹窗关闭后重置
const handleDialogClosed = () => {
  formRef.value?.resetFields()
  isEdit.value = false
  profileForm.id = 0
  profileForm.skinType = undefined
  profileForm.sensitivity = undefined
  profileForm.hairType = ''
  profileForm.allergyHistory = ''
  profileForm.remark = ''
}

const handleSubmit = async () => {
  submitLoading.value = true
  try {
    const payload: BeautyProfileSave = {
      customerId: props.customerId,
      skinType: profileForm.skinType,
      sensitivity: profileForm.sensitivity,
      hairType: profileForm.hairType || undefined,
      allergyHistory: profileForm.allergyHistory || undefined,
      remark: profileForm.remark || undefined
    }
    if (isEdit.value) {
      await updateBeautyProfile({ ...payload, id: profileForm.id })
      ElMessage.success('更新成功')
    } else {
      await createBeautyProfile(payload)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    loadData()
  } catch (error) {
    ElMessage.error((error as Error).message || '保存失败')
  } finally {
    submitLoading.value = false
  }
}

onMounted(() => {
  loadData()
})
</script>

<style scoped>
.beauty-profile-panel {
  width: 100%;
  padding-bottom: 8px;
}

.panel-toolbar {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  margin-bottom: 12px;
}

.text-muted {
  color: var(--text-tertiary);
}

/* 档案铺在暗色卡片上，Element Plus 描述列表为浅色风格且内容文字色不可读，
   故改用与系统内其他暗色详情卡一致的信息网格 */
.info-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.info-item {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 6px;
  padding: 12px 16px;
  background: var(--bg-elevated);
  border: 1px solid var(--border-primary);
  border-radius: var(--radius-md);
}

.info-item-full {
  grid-column: 1 / -1;
}

.info-label {
  font-size: 12px;
  color: var(--text-tertiary);
  letter-spacing: 0.5px;
}

.info-value {
  font-size: 14px;
  color: var(--text-primary);
  line-height: 1.6;
}

/* 过敏史、备注为长文本，需保留换行且允许折行 */
.info-value-text {
  white-space: pre-wrap;
  word-break: break-word;
}
</style>
