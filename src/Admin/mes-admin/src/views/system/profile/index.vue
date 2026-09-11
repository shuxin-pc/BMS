<template>
  <div class="profile-management">
    <!-- 动态背景 -->
    <div class="profile-bg">
      <div class="bg-orb bg-orb-1"></div>
      <div class="bg-orb bg-orb-2"></div>
      <div class="bg-grid"></div>
    </div>

    <!-- 顶部用户卡片 -->
    <div class="profile-header card">
      <div class="header-content">
        <!-- 头像区域 -->
        <div class="avatar-section">
          <!-- 隐藏的文件输入框 -->
          <input
            type="file"
            accept="image/jpeg,image/png,image/gif,image/webp"
            class="avatar-upload-input"
            @change="handleAvatarChange"
          />
          <div class="avatar-ring">
            <div class="avatar-glow"></div>
            <div v-if="!avatarUrl || avatarUrl === defaultAvatarSvg" class="avatar avatar-placeholder">
              <svg viewBox="0 0 120 120" fill="none" xmlns="http://www.w3.org/2000/svg">
                <circle cx="60" cy="60" r="60" fill="url(#avatarGrad)" />
                <circle cx="60" cy="45" r="22" fill="#0f172a" />
                <path d="M30 95c0-16 13-29 30-29s30 13 30 29" fill="#0f172a" />
                <defs>
                  <linearGradient id="avatarGrad" x1="0" y1="0" x2="120" y2="120">
                    <stop offset="0%" stop-color="#06d4e4" />
                    <stop offset="100%" stop-color="#0e7490" />
                  </linearGradient>
                </defs>
              </svg>
            </div>
            <img v-else :src="avatarUrl" alt="用户头像" class="avatar" />
          </div>
          <el-button class="avatar-upload-btn" type="primary" circle @click="handleAvatarUpload">
            <el-icon><Camera /></el-icon>
          </el-button>
        </div>

        <!-- 用户信息 -->
        <div class="user-info">
          <div class="user-name-row">
            <h2 class="user-name">{{ userInfo.realName || '未设置姓名' }}</h2>
          </div>
          <p class="user-role">
            <el-icon><Medal /></el-icon>
            {{ userRoleLabel || '暂无角色' }}
          </p>
          <p class="user-username">@{{ userInfo.userName }}</p>
          <div class="user-meta">
            <span class="meta-item">
              <el-icon><OfficeBuilding /></el-icon>
              {{ userInfo.organizationName || '未设置组织' }}
            </span>
            <span class="meta-item">
              <el-icon><Calendar /></el-icon>
              注册于 {{ formatDate(userInfo.createdTime) }}
            </span>
          </div>
        </div>

        <!-- 快速操作 -->
        <div class="quick-actions">
          <el-button type="primary" @click="handleEditInfo">
            <el-icon><Edit /></el-icon>
            编辑资料
          </el-button>
          <el-button @click="handlePasswordChange">
            <el-icon><Key /></el-icon>
            修改密码
          </el-button>
        </div>
      </div>
    </div>

    <!-- 主体内容 -->
    <div class="content-wrapper">
      <!-- 左侧：详细信息 -->
      <div class="info-column">
        <!-- 个人信息卡 -->
        <div class="info-card card">
          <div class="card-header">
            <span class="card-title">
              <el-icon><UserIcon /></el-icon>
              个人信息
            </span>
          </div>
          <div class="info-grid">
            <div class="info-item" v-for="item in profileInfoList" :key="item.label">
              <div class="info-icon">
                <el-icon><component :is="item.icon" /></el-icon>
              </div>
              <div class="info-content">
                <span class="info-label">{{ item.label }}</span>
                <span class="info-value" :class="{ 'data-highlight': item.highlight }">
                  {{ item.value }}
                </span>
              </div>
            </div>
          </div>
        </div>

        <!-- 角色信息 -->
        <div class="roles-card card">
          <div class="card-header">
            <span class="card-title">
              <el-icon><Key /></el-icon>
              角色权限
            </span>
          </div>
          <div class="roles-content">
            <div class="role-item" v-for="role in userInfo.roles" :key="role.id">
              <div class="role-badge">
                <el-icon><Avatar /></el-icon>
              </div>
              <span class="role-name">{{ role.name }}</span>
            </div>
            <div class="no-roles" v-if="!userInfo.roles?.length">
              <el-icon><Warning /></el-icon>
              暂未分配角色
            </div>
          </div>
        </div>
      </div>

      <!-- 右侧：安全信息 -->
      <div class="security-column">
        <!-- 登录安全 -->
        <div class="security-card card">
          <div class="card-header">
            <span class="card-title">
              <el-icon><LockIcon /></el-icon>
              登录安全
            </span>
          </div>
          <div class="security-list">
            <div class="security-item">
              <div class="security-icon-wrapper">
                <el-icon><Clock /></el-icon>
              </div>
              <div class="security-info">
                <span class="security-label">最后登录时间</span>
                <span class="security-value data-highlight">
                  {{ formatFullDate(securityInfo.lastLoginTime) }}
                </span>
              </div>
            </div>
            <div class="security-item">
              <div class="security-icon-wrapper">
                <el-icon><Location /></el-icon>
              </div>
              <div class="security-info">
                <span class="security-label">最后登录IP</span>
                <span class="security-value data-highlight">
                  {{ securityInfo.lastLoginIp || '-' }}
                </span>
              </div>
            </div>
            <div class="security-item">
              <div class="security-icon-wrapper">
                <el-icon><Monitor /></el-icon>
              </div>
              <div class="security-info">
                <span class="security-label">登录设备</span>
                <span class="security-value">
                  {{ securityInfo.userAgent || '未知设备' }}
                </span>
              </div>
            </div>
          </div>
        </div>

        <!-- 修改密码 -->
        <div class="password-card card">
          <div class="card-header">
            <span class="card-title">
              <el-icon><Lock /></el-icon>
              修改密码
            </span>
          </div>
          <el-form
            ref="passwordFormRef"
            :model="passwordForm"
            :rules="passwordRules"
            class="password-form"
          >
            <el-form-item prop="oldPassword">
              <el-input
                v-model="passwordForm.oldPassword"
                type="password"
                placeholder="请输入原密码"
                show-password
              >
                <template #prefix>
                  <el-icon><Lock /></el-icon>
                </template>
              </el-input>
            </el-form-item>
            <el-form-item prop="newPassword">
              <div class="password-input-wrapper">
                <el-input
                  v-model="passwordForm.newPassword"
                  type="password"
                  placeholder="请输入新密码"
                  show-password
                  class="password-input"
                >
                  <template #prefix>
                    <el-icon><Key /></el-icon>
                  </template>
                </el-input>
                <el-tooltip v-if="getPolicyText()" :content="getPolicyText()" placement="top" effect="dark">
                  <el-icon class="policy-icon"><InfoFilled /></el-icon>
                </el-tooltip>
              </div>
            </el-form-item>
            <el-form-item prop="confirmPassword">
              <el-input
                v-model="passwordForm.confirmPassword"
                type="password"
                placeholder="请再次输入新密码"
                show-password
              >
                <template #prefix>
                  <el-icon><CircleCheck /></el-icon>
                </template>
              </el-input>
            </el-form-item>
            <el-button
              type="primary"
              class="submit-btn"
              :loading="passwordLoading"
              @click="handlePasswordChange"
            >
              <el-icon><Key /></el-icon>
              确认修改
            </el-button>
          </el-form>
        </div>
      </div>
    </div>

    <!-- 编辑个人信息弹窗 -->
    <el-dialog
      v-model="editDialogVisible"
      title="编辑个人信息"
      width="500px"
      :close-on-click-modal="false"
      class="edit-dialog"
    >
      <el-form
        ref="editFormRef"
        :model="editForm"
        :rules="editRules"
        label-width="80px"
      >
        <el-form-item label="姓名" prop="realName">
          <el-input v-model="editForm.realName" placeholder="请输入姓名">
            <template #prefix>
              <el-icon><UserIcon /></el-icon>
            </template>
          </el-input>
        </el-form-item>
        <el-form-item label="邮箱" prop="email">
          <el-input v-model="editForm.email" placeholder="请输入邮箱">
            <template #prefix>
              <el-icon><Message /></el-icon>
            </template>
          </el-input>
        </el-form-item>
        <el-form-item label="手机号" prop="phone">
          <el-input v-model="editForm.phone" placeholder="请输入手机号">
            <template #prefix>
              <el-icon><Phone /></el-icon>
            </template>
          </el-input>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="editDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="editLoading" @click="handleEditSubmit">
          保存修改
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, markRaw } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import {
  Camera, Edit, Key, Clock, Location, Monitor, Lock,
  User as UserIcon, Medal, OfficeBuilding, Calendar, Avatar,
  Warning, CircleCheck, Message, Phone, Lock as LockIcon,
  InfoFilled
} from '@element-plus/icons-vue'
import { getUserProfile, updateProfile, changeProfilePassword, updateAvatar, getSystemConfigsByGroup } from '@/api/system'
import type { User, Role, SystemConfig } from '@/api/system/types'
import { useUserStore } from '@/stores/user'
import { usePasswordPolicy } from '@/composables/usePasswordPolicy'
import { formatDate, formatDateTimeSeconds as formatFullDate } from '@/utils/date'

const router = useRouter()

// 密码策略
const { loadPolicy, getPolicyText, getPasswordRules } = usePasswordPolicy()

// 默认头像 SVG
const defaultAvatarSvg = ''

// 用户信息
const userInfo = ref<User>({
  id: 0,
  userName: '',
  realName: '',
  email: '',
  phone: '',
  avatar: '',
  status: 1,
  roles: [],
  createdTime: ''
})

// 安全信息
const securityInfo = reactive({
  lastLoginTime: '',
  lastLoginIp: '',
  userAgent: ''
})

// 头像URL
const avatarUrl = computed(() => {
  return userInfo.value.avatar || ''
})

// 头像最大大小配置（MB）
const maxAvatarSize = ref(2)

// 加载头像大小配置
const loadAvatarSizeConfig = async () => {
  try {
    const configs = await getSystemConfigsByGroup('Upload')
    const maxSizeConfig = configs.find((c: SystemConfig) => c.configKey === 'MaxAvatarSize')
    if (maxSizeConfig) {
      maxAvatarSize.value = parseFloat(maxSizeConfig.configValue) || 2
    }
  } catch {
    // 加载头像大小配置失败
  }
}

// 用户角色标签
const userRoleLabel = computed(() => {
  const roles = userInfo.value.roles || []
  if (roles.length === 0) return ''
  return roles.map((r: Role) => r.name).join('、')
})

// 个人信息列表 - 使用 markRaw 避免图标组件被响应式化
const profileInfoList = computed(() => [
  { label: '用户名', value: userInfo.value.userName || '-', icon: markRaw(UserIcon), highlight: true },
  { label: '姓名', value: userInfo.value.realName || '-', icon: markRaw(UserIcon) },
  { label: '邮箱', value: userInfo.value.email || '-', icon: markRaw(Message), highlight: true },
  { label: '手机号', value: userInfo.value.phone || '-', icon: markRaw(Phone), highlight: true },
  { label: '所属组织', value: userInfo.value.organizationName || '未设置', icon: markRaw(OfficeBuilding) },
  { label: '所属租户', value: userInfo.value.tenantName || '未设置', icon: markRaw(OfficeBuilding) },
  { label: '注册时间', value: formatDate(userInfo.value.createdTime), icon: markRaw(Calendar) }
])

// 编辑弹窗
const editDialogVisible = ref(false)
const editLoading = ref(false)
const editFormRef = ref<FormInstance>()

const editForm = reactive({
  realName: '',
  email: '',
  phone: ''
})

const editRules: FormRules = {
  realName: [{ required: true, message: '请输入姓名', trigger: 'blur' }],
  email: [
    { required: true, message: '请输入邮箱', trigger: 'blur' },
    { type: 'email', message: '请输入正确的邮箱格式', trigger: 'blur' }
  ],
  phone: [{ pattern: /^1[3-9]\d{9}$/, message: '请输入正确的手机号', trigger: 'blur' }]
}

// 修改密码
const passwordFormRef = ref<FormInstance>()
const passwordLoading = ref(false)

const passwordForm = reactive({
  oldPassword: '',
  newPassword: '',
  confirmPassword: ''
})

const passwordRules: FormRules = {
  oldPassword: [{ required: true, message: '请输入原密码', trigger: 'blur' }],
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    getPasswordRules()
  ],
  confirmPassword: [
    { required: true, message: '请确认密码', trigger: 'blur' },
    {
      validator: (_rule, value, callback) => {
        if (value !== passwordForm.newPassword) {
          callback(new Error('两次输入的密码不一致'))
        } else {
          callback()
        }
      },
      trigger: 'blur'
    }
  ]
}

// 加载用户信息
const loadUserProfile = async () => {
  try {
    const data = await getUserProfile()
    userInfo.value = data
    // 安全信息从用户数据中获取
    securityInfo.lastLoginTime = data.lastLoginTime || ''
    securityInfo.lastLoginIp = data.lastLoginIp || ''
    // 登录设备从浏览器获取
    securityInfo.userAgent = navigator.userAgent
  } catch (error) {
    ElMessage.error((error as Error).message || '加载用户信息失败')
  }
}

// 头像上传
const handleAvatarUpload = () => {
  // 触发隐藏的文件输入框
  const uploadInput = document.querySelector('.avatar-upload-input') as HTMLInputElement
  if (uploadInput) {
    uploadInput.click()
  }
}

// 处理头像文件选择
const handleAvatarChange = async (event: Event) => {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]
  if (!file) return

  // 验证文件类型
  const allowedTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp']
  if (!allowedTypes.includes(file.type)) {
    ElMessage.error('只能上传 JPG、PNG、GIF、WebP 格式的图片')
    return
  }

  // 验证文件大小（使用配置的 MaxAvatarSize）
  if (file.size > maxAvatarSize.value * 1024 * 1024) {
    ElMessage.error(`图片大小不能超过 ${maxAvatarSize.value}MB`)
    return
  }

  try {
    // 读取文件为 base64
    const reader = new FileReader()
    const avatarBase64 = await new Promise<string>((resolve, reject) => {
      reader.onload = (e) => resolve(e.target?.result as string)
      reader.onerror = reject
      reader.readAsDataURL(file)
    })

    // 调用后端 API 保存头像
    const result = await updateAvatar(avatarBase64)
    userInfo.value.avatar = result.avatar

    // 同步更新 store 中的用户信息
    const userStore = useUserStore()
    userStore.userInfo.avatar = result.avatar || ''

    ElMessage.success('头像上传成功')
  } catch (error) {
    ElMessage.error((error as Error).message || '头像上传失败')
  }

  // 清空 input 值，允许重复选择同一文件
  target.value = ''
}

// 编辑个人信息
const handleEditInfo = () => {
  editForm.realName = userInfo.value.realName
  editForm.email = userInfo.value.email
  editForm.phone = userInfo.value.phone
  editDialogVisible.value = true
}

// 提交编辑
const handleEditSubmit = async () => {
  if (!editFormRef.value) return
  await editFormRef.value.validate(async (valid) => {
    if (valid) {
      editLoading.value = true
      try {
        const data = {
          id: userInfo.value.id,
          userName: userInfo.value.userName,
          realName: editForm.realName,
          email: editForm.email,
          phone: editForm.phone,
          status: userInfo.value.status,
          roleIds: userInfo.value.roleIds || []
        }
        await updateProfile(data)
        ElMessage.success('个人信息更新成功')
        editDialogVisible.value = false
        // 更新本地用户信息
        userInfo.value.realName = editForm.realName
        userInfo.value.email = editForm.email
        userInfo.value.phone = editForm.phone
        // 同步更新 store 中的用户信息
        const userStore = useUserStore()
        userStore.userInfo.realName = editForm.realName
      } catch (error) {
        ElMessage.error((error as Error).message || '更新失败')
      } finally {
        editLoading.value = false
      }
    }
  })
}

// 修改密码
const handlePasswordChange = async () => {
  if (!passwordFormRef.value) return
  await passwordFormRef.value.validate(async (valid) => {
    if (valid) {
      passwordLoading.value = true
      try {
        await changeProfilePassword(passwordForm.oldPassword, passwordForm.newPassword)
        ElMessage.success('密码修改成功，请重新登录')
        // 重置密码表单
        passwordForm.oldPassword = ''
        passwordForm.newPassword = ''
        passwordForm.confirmPassword = ''
        // 跳转到登录页
        setTimeout(async () => {
          const userStore = useUserStore()
          await userStore.logout()
          router.push('/login')
        }, 1500)
      } catch (error) {
        ElMessage.error((error as Error).message || '密码修改失败')
      } finally {
        passwordLoading.value = false
      }
    }
  })
}

onMounted(() => {
  loadUserProfile()
  loadPolicy()
  loadAvatarSizeConfig()
})
</script>

<style scoped>
.profile-management {
  width: 100%;
  min-height: 100%;
  padding: 24px;
  position: relative;
  overflow: hidden;
}

/* 动态背景 */
.profile-bg {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  pointer-events: none;
  z-index: 0;
}

.bg-orb {
  position: absolute;
  border-radius: 50%;
  filter: blur(80px);
  opacity: 0.4;
  animation: float 20s infinite ease-in-out;
}

.bg-orb-1 {
  width: 400px;
  height: 400px;
  background: var(--primary);
  top: -100px;
  right: 10%;
  animation-delay: 0s;
}

.bg-orb-2 {
  width: 300px;
  height: 300px;
  background: var(--info);
  bottom: -50px;
  left: 5%;
  animation-delay: -10s;
}

@keyframes float {
  0%, 100% {
    transform: translate(0, 0) scale(1);
  }
  25% {
    transform: translate(30px, -30px) scale(1.05);
  }
  50% {
    transform: translate(-20px, 20px) scale(0.95);
  }
  75% {
    transform: translate(20px, 30px) scale(1.02);
  }
}

.bg-grid {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-image:
    linear-gradient(rgba(6, 212, 228, 0.03) 1px, transparent 1px),
    linear-gradient(90deg, rgba(6, 212, 228, 0.03) 1px, transparent 1px);
  background-size: 40px 40px;
}

/* 顶部用户卡片 */
.profile-header {
  position: relative;
  z-index: 1;
  padding: 32px;
  margin-bottom: 24px;
  background: linear-gradient(135deg, var(--bg-tertiary) 0%, rgba(6, 212, 228, 0.05) 100%);
  overflow: visible;
}

.profile-header::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 3px;
  background: linear-gradient(90deg, transparent, var(--primary), transparent);
}

.header-content {
  display: flex;
  align-items: center;
  gap: 40px;
}

/* 头像区域 */
.avatar-section {
  position: relative;
  flex-shrink: 0;
}

.avatar-ring {
  position: relative;
  width: 120px;
  height: 120px;
  border-radius: 50%;
  padding: 4px;
  background: linear-gradient(135deg, var(--primary), var(--primary-dim));
}

.avatar-glow {
  position: absolute;
  top: -10px;
  left: -10px;
  right: -10px;
  bottom: -10px;
  border-radius: 50%;
  background: conic-gradient(from 0deg, transparent, var(--primary), transparent, var(--primary), transparent);
  animation: rotate 4s linear infinite;
  opacity: 0.5;
}

@keyframes rotate {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

.avatar {
  width: 100%;
  height: 100%;
  border-radius: 50%;
  object-fit: cover;
  border: 3px solid var(--bg-tertiary);
  position: relative;
  z-index: 1;
}

.avatar-placeholder {
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--bg-tertiary);
  overflow: hidden;
}

.avatar-placeholder svg {
  width: 100%;
  height: 100%;
}

.avatar-upload-btn {
  position: absolute;
  bottom: 0;
  right: 0;
  width: 36px;
  height: 36px;
  padding: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 2;
}

.avatar-upload-input {
  display: none;
}

/* 用户信息 */
.user-info {
  flex: 1;
}

.user-name-row {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-bottom: 8px;
}

.user-name {
  font-size: 28px;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
  background: linear-gradient(135deg, var(--text-primary), var(--primary));
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  flex-shrink: 0;
}

.user-role {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 14px;
  color: var(--primary);
  margin: 0 0 4px 0;
}

.user-username {
  font-size: 14px;
  color: var(--text-tertiary);
  margin: 0 0 12px 0;
  font-family: 'JetBrains Mono', monospace;
}

.user-meta {
  display: flex;
  gap: 24px;
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  color: var(--text-secondary);
}

/* 快速操作 */
.quick-actions {
  display: flex;
  flex-direction: column;
  gap: 12px;
  flex-shrink: 0;
}

.quick-actions .el-button {
  margin: 0 !important;
}

/* 主体内容 */
.content-wrapper {
  position: relative;
  z-index: 1;
  display: grid;
  grid-template-columns: 1fr 400px;
  gap: 24px;
}

.info-column,
.security-column {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

/* 卡片通用样式 */
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px 24px;
  border-bottom: 1px solid var(--border-primary);
  position: relative;
}

.card-header::after {
  content: '';
  position: absolute;
  bottom: 0;
  left: 24px;
  width: 40px;
  height: 2px;
  background: var(--primary);
}

.card-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
}

.card-title .el-icon {
  color: var(--primary);
}

/* 个人信息卡片 */
.info-card {
  flex: 1;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 20px;
  padding: 24px;
}

.info-item {
  display: flex;
  align-items: flex-start;
  gap: 16px;
  padding: 16px;
  background: var(--bg-elevated);
  border-radius: var(--radius-md);
  border: 1px solid var(--border-primary);
  transition: all 0.3s ease;
}

.info-item:hover {
  border-color: var(--primary);
  background: var(--bg-hover);
}

.info-icon {
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--bg-tertiary);
  border-radius: var(--radius-md);
  color: var(--primary);
  font-size: 18px;
  flex-shrink: 0;
}

.info-content {
  display: flex;
  flex-direction: column;
  gap: 4px;
  min-width: 0;
}

.info-label {
  font-size: 12px;
  color: var(--text-tertiary);
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.info-value {
  font-size: 14px;
  color: var(--text-primary);
  word-break: break-all;
}

/* 角色卡片 */
.roles-card {
  flex-shrink: 0;
}

.roles-content {
  padding: 24px;
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
}

.role-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 12px 20px;
  background: linear-gradient(135deg, rgba(6, 212, 228, 0.1), rgba(6, 212, 228, 0.05));
  border: 1px solid var(--border-primary);
  border-radius: var(--radius-lg);
  transition: all 0.3s ease;
}

.role-item:hover {
  border-color: var(--primary);
  box-shadow: var(--shadow-glow-primary);
}

.role-badge {
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--primary);
  border-radius: 50%;
  color: var(--bg-primary);
}

.role-name {
  font-size: 14px;
  font-weight: 500;
  color: var(--text-primary);
}

.no-roles {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 20px;
  color: var(--text-tertiary);
  font-size: 14px;
}

/* 安全卡片 */
.security-card {
  flex-shrink: 0;
}

.security-list {
  padding: 24px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.security-item {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 16px;
  background: var(--bg-elevated);
  border-radius: var(--radius-md);
  border: 1px solid var(--border-primary);
  transition: all 0.3s ease;
}

.security-item:hover {
  border-color: var(--border-secondary);
  background: var(--bg-hover);
}

.security-icon-wrapper {
  width: 44px;
  height: 44px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, var(--primary), var(--primary-dim));
  border-radius: var(--radius-md);
  color: var(--bg-primary);
  font-size: 20px;
  flex-shrink: 0;
}

.security-info {
  display: flex;
  flex-direction: column;
  gap: 4px;
  min-width: 0;
  flex: 1;
}

.security-label {
  font-size: 12px;
  color: var(--text-tertiary);
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.security-value {
  font-size: 14px;
  color: var(--text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* 修改密码卡片 */
.password-card {
  flex-shrink: 0;
}

.password-form {
  padding: 24px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.password-input-wrapper {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
}

.password-input {
  flex: 1;
}

.policy-icon {
  color: var(--primary);
  font-size: 16px;
  cursor: pointer;
  transition: opacity 0.2s;
  flex-shrink: 0;
}

.policy-icon:hover {
  opacity: 0.7;
}

.submit-btn {
  width: 100%;
  height: 44px;
  font-size: 15px;
  font-weight: 600;
  margin-top: 8px;
}

/* 响应式 */
@media (max-width: 1400px) {
  .content-wrapper {
    grid-template-columns: 1fr 360px;
  }
}

@media (max-width: 1200px) {
  .content-wrapper {
    grid-template-columns: 1fr;
  }

  .info-grid {
    grid-template-columns: repeat(3, 1fr);
  }
}

@media (max-width: 768px) {
  .header-content {
    flex-direction: column;
    text-align: center;
  }

  .quick-actions {
    flex-direction: row;
    width: 100%;
  }

  .quick-actions .el-button {
    flex: 1;
  }

  .info-grid {
    grid-template-columns: 1fr;
  }

  .user-meta {
    flex-direction: column;
    gap: 8px;
  }
}
</style>
