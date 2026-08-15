<template>
  <el-dialog
    v-model="dialogVisible"
    title="选择图标"
    width="600px"
    :close-on-click-modal="false"
    @close="handleClose"
  >
    <!-- 搜索和上传 -->
    <div class="icon-picker-header">
      <el-input
        v-model="searchText"
        placeholder="搜索图标..."
        clearable
        class="icon-search"
      >
        <template #prefix>
          <el-icon><Search /></el-icon>
        </template>
      </el-input>
      <el-upload
        v-if="showUpload"
        :auto-upload="false"
        :show-file-list="false"
        :accept="'.png,.jpg,.jpeg'"
        :on-change="handleFileChange"
      >
        <el-button type="primary">
          <el-icon><Upload /></el-icon>
          上传
        </el-button>
      </el-upload>
    </div>

    <!-- 图标网格 -->
    <div class="icon-grid-container">
      <div v-if="filteredIcons.length === 0" class="no-result">
        未找到匹配的图标
      </div>
      <div v-else class="icon-grid">
        <div
          v-for="icon in filteredIcons"
          :key="icon"
          :class="['icon-item', { active: selectedIcon === icon }]"
          @click="handleSelectIcon(icon)"
        >
          <el-icon size="24">
            <component :is="icon" />
          </el-icon>
          <span class="icon-name">{{ icon }}</span>
        </div>
      </div>
    </div>

    <!-- 预览区 -->
    <div class="icon-preview">
      <span class="preview-label">已选择:</span>
      <div v-if="selectedIcon" class="preview-content">
        <el-icon size="24">
          <component :is="selectedIcon" />
        </el-icon>
        <span>{{ selectedIcon }}</span>
      </div>
      <div v-else-if="uploadedImage" class="preview-content">
        <img :src="uploadedImage" alt="已上传" class="preview-image" />
        <span>自定义图片</span>
      </div>
      <span v-else class="preview-placeholder">未选择</span>
    </div>

    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button type="primary" @click="handleConfirm">确定</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { ElMessage, type UploadFile } from 'element-plus'
import { Search, Upload } from '@element-plus/icons-vue'

// 图标列表（从 Element Plus 提取常用图标）
const iconList = [
  'Plus', 'Minus', 'Close', 'Check', 'Delete', 'Edit', 'Search', 'Refresh',
  'Setting', 'User', 'Lock', 'Key', 'Bell', 'Message', 'Date', 'Clock',
  'Folder', 'Document', 'Files', 'FolderOpened', 'FolderAdd',
  'DataAnalysis', 'PieChart', 'TrendCharts', 'Histogram',
  'Monitor', 'Laptop', 'Mobile', 'Printer', 'Keyboard',
  'Connection', 'Link', 'Wifi', 'Globe', 'Cloud',
  'Tools', 'Screwdriver', 'Hammer', 'Wrench',
  'Star', 'Heart', 'Bookmark', 'Flag', 'Tag', 'Calendar',
  'Home', 'House', 'Grid', 'List', 'Menu', 'Fold', 'Expand',
  'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight',
  'Top', 'Bottom', 'Back', 'Right', 'Sort', 'SortUp', 'SortDown',
  'Upload', 'Download', 'Picture', 'Camera', 'VideoCamera',
  'OfficeBuilding', 'School', 'Hospital', 'CreditCard', 'Money',
  'Phone', 'ChatDotRound', 'ChatLineRound',
  'Pointer', 'Aim', 'Coordinate', 'Location', 'LocationInformation',
  'MagicStick', 'Brush', 'Scale', 'Bingo',
  'Sunrise', 'Sunny', 'Moon', 'Cloudy', 'PartlyCloudy', 'HeavySnow',
  'Drizzling', 'Pouring', 'Lightning', 'Sunset', 'Odometer'
]

interface Props {
  visible: boolean
  modelValue: string
  /** 是否显示上传按钮 */
  showUpload?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  visible: false,
  modelValue: '',
  showUpload: true
})

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void
  (e: 'update:modelValue', value: string): void
  (e: 'confirm', value: string): void
}>()

const dialogVisible = computed({
  get: () => props.visible,
  set: (val) => emit('update:visible', val)
})

const searchText = ref('')
const selectedIcon = ref('')
const uploadedImage = ref('')

// 初始化选中状态
watch(() => props.modelValue, (val) => {
  if (val) {
    if (val.startsWith('data:')) {
      selectedIcon.value = ''
      uploadedImage.value = val
    } else {
      selectedIcon.value = val
      uploadedImage.value = ''
    }
  }
}, { immediate: true })

// 过滤图标
const filteredIcons = computed(() => {
  if (!searchText.value) return iconList
  return iconList.filter(icon =>
    icon.toLowerCase().includes(searchText.value.toLowerCase())
  )
})

// 选择图标
const handleSelectIcon = (icon: string) => {
  selectedIcon.value = icon
  uploadedImage.value = ''
}

// 图片大小限制 50KB，Base64 编码会增加约 33% 长度
const MAX_FILE_SIZE = 50 * 1024

// 上传文件
const handleFileChange = async (file: UploadFile) => {
  if (!file.raw) return

  const isImage = file.raw.type === 'image/png' ||
                  file.raw.type === 'image/jpeg'

  if (!isImage) {
    ElMessage.error('请上传 PNG 或 JPG 格式的图片')
    return
  }

  // 检查文件大小是否超过 50KB 限制
  if (file.raw.size > MAX_FILE_SIZE) {
    ElMessage.error(`图片大小不能超过 50KB，请选择更小的图片`)
    return
  }

  // 转换为 Base64
  const reader = new FileReader()
  reader.onload = (e) => {
    uploadedImage.value = e.target?.result as string
    selectedIcon.value = ''
    ElMessage.success('图片上传成功')
  }
  reader.readAsDataURL(file.raw)
}

// 确认选择
const handleConfirm = () => {
  const value = uploadedImage.value || selectedIcon.value
  emit('update:modelValue', value)
  emit('confirm', value)
  handleClose()
}

// 关闭弹窗
const handleClose = () => {
  dialogVisible.value = false
  searchText.value = ''
}
</script>

<style scoped>
.icon-picker-header {
  display: flex;
  gap: 12px;
  margin-bottom: 16px;
}

.icon-search {
  flex: 1;
}

.icon-grid-container {
  max-height: 360px;
  overflow-y: auto;
  padding: 12px;
  background: #f9fafb;
  border-radius: 8px;
  border: 1px solid #e5e7eb;
}

.no-result {
  text-align: center;
  color: #9ca3af;
  padding: 40px 0;
}

.icon-grid {
  display: grid;
  grid-template-columns: repeat(5, 1fr);
  gap: 8px;
}

.icon-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 12px 8px;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
  border: 1px solid transparent;
}

.icon-item:hover {
  background: #f3f4f6;
  border-color: #d1d5db;
}

.icon-item:hover .icon-name {
  color: #1f2937;
}

.icon-item.active {
  background: rgba(6, 212, 228, 0.15);
  border-color: #06b6d4;
}

.icon-item.active .icon-name {
  color: #1f2937;
  font-weight: 600;
}

.icon-item .icon-name {
  margin-top: 6px;
  font-size: 11px;
  color: #6b7280;
  text-align: center;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  max-width: 100%;
}

.icon-preview {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-top: 16px;
  padding: 12px;
  background: #f9fafb;
  border-radius: 8px;
  border: 1px solid #e5e7eb;
}

.preview-label {
  color: #6b7280;
  font-size: 13px;
}

.preview-content {
  display: flex;
  align-items: center;
  gap: 8px;
  color: #1f2937;
  font-weight: 500;
}

.icon-grid-container :deep(.el-icon),
.icon-preview :deep(.el-icon) {
  color: #1f2937;
}

.preview-image {
  width: 24px;
  height: 24px;
  object-fit: contain;
}

.preview-placeholder {
  color: #9ca3af;
  font-style: italic;
}
</style>