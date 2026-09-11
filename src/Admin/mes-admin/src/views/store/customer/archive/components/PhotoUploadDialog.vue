<template>
  <el-dialog
    :model-value="visible"
    :title="isEdit ? '编辑对比照片' : '上传对比照片'"
    width="760px"
    @update:model-value="handleVisibleChange"
    @closed="handleDialogClosed"
  >
    <el-form ref="formRef" :model="photoForm" :rules="formRules" label-width="90px">
      <el-form-item label="客户">
        <span>{{ props.customerName }}</span>
      </el-form-item>

      <el-form-item label="关联订单">
        <el-select
          v-model="photoForm.orderId"
          placeholder="选填，可关联服务/项目卡核销订单"
          filterable
          clearable
          :loading="orderOptionsLoading"
          style="width: 100%"
        >
          <el-option
            v-for="order in orderOptions"
            :key="order.id"
            :label="`${order.orderNo}（${order.orderType === 2 ? '服务' : '项目卡核销'} · ${order.orderTime ? order.orderTime.substring(0, 10) : ''}）`"
            :value="order.id"
          />
        </el-select>
      </el-form-item>

      <el-form-item label="服务项目" prop="productId">
        <el-select
          v-model="photoForm.productId"
          placeholder="请选择服务项目"
          filterable
          clearable
          style="width: 100%"
        >
          <el-option v-for="p in serviceProductOptions" :key="p.id" :label="p.name" :value="p.id" />
        </el-select>
      </el-form-item>

      <el-form-item label="拍照日期" prop="photoDate">
        <el-date-picker
          v-model="photoForm.photoDate"
          type="date"
          placeholder="请选择拍照日期"
          value-format="YYYY-MM-DD"
          :disabled-date="disableFutureDate"
          style="width: 100%"
        />
      </el-form-item>
    </el-form>

    <!-- 服务前/服务后并列录入：一次提交即可保证两条记录的分组字段一致 -->
    <div class="photo-sections">
      <div v-for="section in sections" :key="section.type" class="photo-section">
        <div class="section-header">
          <span class="section-title" :class="section.type === 1 ? 'before' : 'after'">
            {{ section.title }}
          </span>
          <span class="section-count">
            {{ section.state.items.length }}/{{ MAX_PHOTOS_PER_TYPE }} 张
          </span>
        </div>

        <div class="thumb-list">
          <div v-for="(item, index) in section.state.items" :key="item.key" class="thumb-wrapper">
            <el-image
              :src="item.photoUrl"
              fit="cover"
              class="thumb-img"
              :preview-src-list="section.state.items.map(i => i.photoUrl)"
              :initial-index="index"
              preview-teleported
            />
            <el-icon class="thumb-remove" @click="handleRemovePhoto(section.state, index)">
              <Close />
            </el-icon>
          </div>
          <el-upload
            v-if="section.state.items.length < MAX_PHOTOS_PER_TYPE"
            accept="image/jpeg,image/png,image/webp"
            multiple
            :show-file-list="false"
            :before-upload="(file: UploadRawFile) => handleBeforeUpload(file, section.state)"
          >
            <div class="thumb-add">
              <el-icon><Plus /></el-icon>
              <span>上传</span>
            </div>
          </el-upload>
        </div>

        <div class="url-input-wrapper">
          <el-input
            v-model="section.state.urlInput"
            placeholder="或输入图片URL"
            :disabled="section.state.items.length >= MAX_PHOTOS_PER_TYPE"
            @keyup.enter="handleAddUrl(section.state)"
          >
            <template #append>
              <el-button
                :disabled="section.state.items.length >= MAX_PHOTOS_PER_TYPE"
                @click="handleAddUrl(section.state)"
              >
                添加
              </el-button>
            </template>
          </el-input>
        </div>

        <el-input
          v-model="section.state.remark"
          type="textarea"
          :rows="2"
          :placeholder="`${section.title}备注`"
          maxlength="200"
          show-word-limit
        />
      </div>
    </div>
    <div class="form-hint">
      支持 jpg/png/webp 本地图片或图片URL；服务前/服务后各最多 {{ MAX_PHOTOS_PER_TYPE }} 张；清空某一侧的全部照片会删除该侧记录
    </div>

    <template #footer>
      <el-button @click="handleVisibleChange(false)">取消</el-button>
      <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
        {{ isEdit ? '保存' : '上传' }}
      </el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted, onUnmounted } from 'vue'
import { ElMessage, type FormInstance, type FormRules, type UploadRawFile } from 'element-plus'
import { Plus, Close } from '@element-plus/icons-vue'
import {
  createComparisonPhoto,
  updateComparisonPhoto,
  deleteComparisonPhoto
} from '@/api/customer-profile'
import { uploadImage } from '@/api/shared/fileUpload'
import { formatDate } from '@/utils/date'
import { getOrders } from '@/api/order'
import { getProducts } from '@/api/product'
import type {
  ComparisonPhotoCreate,
  PhotoType,
  ServiceComparisonPhoto
} from '@/api/customer-profile/types'
import type { Order } from '@/api/order/types'
import type { Product } from '@/api/product/types'

const props = defineProps<{
  /** 弹窗显隐，由父级通过 v-model:visible 控制 */
  visible: boolean
  /** 当前客户ID，由照片面板提供 */
  customerId: number
  /** 当前客户姓名，仅用于弹窗内只读展示 */
  customerName: string
  /** 编辑时传入该组的"服务前"记录，为空表示该类型尚未录入 */
  beforeRecord?: ServiceComparisonPhoto | null
  /** 编辑时传入该组的"服务后"记录，为空表示该类型尚未录入 */
  afterRecord?: ServiceComparisonPhoto | null
}>()

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void
  /** 保存成功，通知父级刷新列表 */
  (e: 'success'): void
}>()

/** 弹窗内的照片项：已有照片带 id（提交时只回传 id），新增照片带 objectKey */
interface PhotoItemState {
  /** 列表渲染用的稳定 key，新增照片无 id 故不能用 id 作 key */
  key: string
  id?: number
  /** 提交给后端的照片来源：上传返回的 objectKey 或外部直链，已有照片无需该值 */
  objectKey?: string
  /** 缩略图展示地址：已有照片为后端签发的 URL，新上传的为本地 blob 预览 */
  photoUrl: string
}

/** 单个照片类型（服务前/服务后）的编辑状态 */
interface PhotoSectionState {
  /** 已有记录ID，为空表示该类型尚未录入过 */
  recordId?: number
  items: PhotoItemState[]
  /** 已选中但仍在读取的本地图片数，用于多选时提前占位以免超出上限 */
  pending: number
  remark: string
  urlInput: string
}

const handleVisibleChange = (val: boolean) => {
  emit('update:visible', val)
}

const submitLoading = ref(false)
const formRef = ref<FormInstance>()

// 客户可选订单（仅服务/项目卡核销类型，orderType=2/3）
const orderOptions = ref<Order[]>([])
const orderOptionsLoading = ref(false)
// 可选服务项目（仅服务类上架商品）
const serviceProductOptions = ref<Product[]>([])

const photoForm = reactive({
  orderId: undefined as number | undefined,
  productId: undefined as number | undefined,
  photoDate: formatDate(new Date())
})

const beforeSection = reactive<PhotoSectionState>({
  recordId: undefined,
  items: [],
  pending: 0,
  remark: '',
  urlInput: ''
})
const afterSection = reactive<PhotoSectionState>({
  recordId: undefined,
  items: [],
  pending: 0,
  remark: '',
  urlInput: ''
})

const sections = [
  { type: 1 as PhotoType, title: '服务前', state: beforeSection },
  { type: 2 as PhotoType, title: '服务后', state: afterSection }
]

const isEdit = computed(() => !!(props.beforeRecord || props.afterRecord))

/** 服务前/服务后各自允许的最大照片数，与后端校验保持一致 */
const MAX_PHOTOS_PER_TYPE = 4

const formRules: FormRules = {
  productId: [{ required: true, message: '请选择服务项目', trigger: 'change' }],
  photoDate: [{ required: true, message: '请选择拍照日期', trigger: 'change' }]
}

// 对比照片只能登记已拍摄的照片，故禁止选择当天之后的日期
const disableFutureDate = (date: Date) => date.getTime() > Date.now()

let photoItemSeq = 0
const nextItemKey = () => `photo-${++photoItemSeq}`

/** 客户服务照片的业务类型，需与后端 ObjectStorage:AllowedBizTypes 一致 */
const PHOTO_BIZ_TYPE = 'customer-photo'

// 本次弹窗生命周期内创建的 blob 预览地址，组件卸载时统一释放，否则图片会一直驻留内存
const previewUrls: string[] = []

// el-upload 上传前置钩子：阻止自动上传，改为调用统一上传接口换取 objectKey
// 多选时该钩子会对每个文件同步触发，而上传是异步完成的，
// 故用 pending 提前占位，否则一次选中超量文件时全部都能通过上限判断
const handleBeforeUpload = async (file: UploadRawFile, state: PhotoSectionState) => {
  if (!file.type.startsWith('image/')) {
    ElMessage.error('请上传图片文件')
    return false
  }
  if (state.items.length + state.pending >= MAX_PHOTOS_PER_TYPE) {
    ElMessage.warning(`每种类型最多上传 ${MAX_PHOTOS_PER_TYPE} 张照片`)
    return false
  }
  state.pending++
  try {
    const { objectKey } = await uploadImage(file, PHOTO_BIZ_TYPE)
    // 预览用本地 blob 而非再向后端换预签名地址，省一次往返
    const previewUrl = URL.createObjectURL(file)
    previewUrls.push(previewUrl)
    state.items.push({ key: nextItemKey(), objectKey, photoUrl: previewUrl })
  } catch (error) {
    ElMessage.error((error as Error).message || `图片 ${file.name} 上传失败`)
  } finally {
    state.pending--
  }
  return false
}

const handleAddUrl = (state: PhotoSectionState) => {
  const url = state.urlInput.trim()
  if (!url) return
  // 后端会再校验一次协议白名单，此处仅为即时提示，不作为安全边界
  if (!/^https?:\/\//i.test(url)) {
    ElMessage.warning('图片链接需以 http:// 或 https:// 开头')
    return
  }
  if (state.items.length + state.pending >= MAX_PHOTOS_PER_TYPE) {
    ElMessage.warning(`每种类型最多上传 ${MAX_PHOTOS_PER_TYPE} 张照片`)
    return
  }
  // 外链直接入库，展示时后端原样返回，故两个字段取同一个值
  state.items.push({ key: nextItemKey(), objectKey: url, photoUrl: url })
  state.urlInput = ''
}

const handleRemovePhoto = (state: PhotoSectionState, index: number) => {
  state.items.splice(index, 1)
}

// 加载客户对应的服务/项目卡核销订单
const loadOrderOptions = async (customerId: number | undefined) => {
  if (!customerId) {
    orderOptions.value = []
    return
  }
  orderOptionsLoading.value = true
  try {
    const res = await getOrders({ customerId, pageIndex: 1, pageSize: 9999 })
    // 前端过滤仅显示 orderType=2(服务) 或 3(项目卡核销)
    orderOptions.value = res.list.filter(o => o.orderType === 2 || o.orderType === 3)
  } catch {
    ElMessage.error('加载订单列表失败')
    orderOptions.value = []
  } finally {
    orderOptionsLoading.value = false
  }
}

// 加载服务项目列表（仅商品类型为"服务项目"的上架商品）
const loadServiceProducts = async () => {
  try {
    const res = await getProducts({ type: 2, status: 1, pageIndex: 1, pageSize: 200 })
    serviceProductOptions.value = res.list
  } catch {
    serviceProductOptions.value = []
  }
}

// 用已有记录回填某一类型的编辑状态
const fillSection = (state: PhotoSectionState, record?: ServiceComparisonPhoto | null) => {
  state.recordId = record?.id
  state.items = (record?.items || []).map(i => ({
    key: nextItemKey(),
    id: i.id,
    photoUrl: i.photoUrl
  }))
  state.remark = record?.remark || ''
  state.urlInput = ''
  state.pending = 0
}

// 打开弹窗时回填：编辑取已有记录的分组字段，新增用默认值
const initForm = () => {
  const base = props.beforeRecord || props.afterRecord
  photoForm.orderId = base?.orderId
  photoForm.productId = base?.productId
  photoForm.photoDate = base?.photoDate || formatDate(new Date())
  fillSection(beforeSection, props.beforeRecord)
  fillSection(afterSection, props.afterRecord)
}

// 客户切换时重载订单选项并清空已选订单
watch(
  () => props.customerId,
  id => {
    photoForm.orderId = undefined
    loadOrderOptions(id)
  }
)

watch(
  () => props.visible,
  async val => {
    if (!val) return
    // 首次打开时懒加载服务项目，避免面板挂载即发请求
    if (serviceProductOptions.value.length === 0) {
      await loadServiceProducts()
    }
    initForm()
  }
)

// 弹窗关闭后重置（订单选项由客户决定，不随弹窗清空）
const handleDialogClosed = () => {
  formRef.value?.clearValidate()
  photoForm.orderId = undefined
  photoForm.productId = undefined
  photoForm.photoDate = formatDate(new Date())
  fillSection(beforeSection, null)
  fillSection(afterSection, null)
}

// 组装提交载荷：已有照片只回传 id，新增照片回传 objectKey
const buildPayload = (state: PhotoSectionState, photoType: PhotoType): ComparisonPhotoCreate => ({
  customerId: props.customerId,
  orderId: photoForm.orderId,
  productId: photoForm.productId,
  photoDate: photoForm.photoDate,
  photoType,
  items: state.items.map(i => (i.id ? { id: i.id } : { objectKey: i.objectKey })),
  remark: state.remark || undefined
})

const handleSubmit = async () => {
  if (!formRef.value) return
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  if (beforeSection.items.length === 0 && afterSection.items.length === 0) {
    ElMessage.warning('请至少上传一张照片')
    return
  }

  submitLoading.value = true
  try {
    // 有照片则"已有则改、没有则增"；照片被清空则删除该类型记录
    for (const section of sections) {
      const { state, type } = section
      if (state.items.length > 0) {
        if (state.recordId) {
          await updateComparisonPhoto({ id: state.recordId, ...buildPayload(state, type) })
        } else {
          await createComparisonPhoto(buildPayload(state, type))
        }
      } else if (state.recordId) {
        await deleteComparisonPhoto(state.recordId)
      }
    }
    ElMessage.success(isEdit.value ? '保存成功' : '照片上传成功')
    handleVisibleChange(false)
    emit('success')
  } catch (error) {
    ElMessage.error((error as Error).message || '保存失败')
  } finally {
    submitLoading.value = false
  }
}

onMounted(() => {
  // 关联订单下拉原由"选择客户"触发加载，客户恒定后改为挂载时加载
  loadOrderOptions(props.customerId)
})

onUnmounted(() => {
  previewUrls.forEach(url => URL.revokeObjectURL(url))
})
</script>

<style scoped>
.form-hint {
  font-size: 12px;
  color: var(--text-tertiary);
  margin-top: 8px;
  line-height: 1.5;
}

.photo-sections {
  display: flex;
  gap: 16px;
}

.photo-section {
  flex: 1;
  min-width: 0;
  padding: 12px;
  border: 1px solid var(--border-primary);
  border-radius: var(--radius-md);
}

.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 10px;
}

.section-title {
  font-size: 13px;
  font-weight: 600;
  padding: 2px 12px;
  border-radius: var(--radius-sm);
}

.section-title.before {
  /* 服务前用暖橙系，与服务后的主题青形成冷暖对比，且保证文字与底色对比清晰 */
  background: rgba(251, 191, 36, 0.15);
  color: var(--warning);
}

.section-title.after {
  background: rgba(6, 212, 228, 0.15);
  color: var(--primary);
}

.section-count {
  font-size: 12px;
  color: var(--text-tertiary);
}

.thumb-list {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 10px;
}

.thumb-wrapper {
  position: relative;
  width: 72px;
  height: 72px;
}

.thumb-img {
  width: 72px;
  height: 72px;
  border-radius: var(--radius-sm);
  border: 1px solid var(--border-primary);
  display: block;
}

.thumb-remove {
  position: absolute;
  top: -6px;
  right: -6px;
  padding: 2px;
  font-size: 12px;
  color: #fff;
  background: var(--danger, #f56c6c);
  border-radius: 50%;
  cursor: pointer;
}

.thumb-add {
  width: 72px;
  height: 72px;
  border: 1px dashed var(--border-primary);
  border-radius: var(--radius-sm);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 2px;
  font-size: 12px;
  color: var(--text-tertiary);
  cursor: pointer;
}

.thumb-add:hover {
  border-color: var(--primary);
  color: var(--primary);
}

.url-input-wrapper {
  margin-bottom: 10px;
}
</style>
