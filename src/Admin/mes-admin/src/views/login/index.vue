<template>
  <div class="login-container">
    <!-- 背景装饰 -->
    <div class="bg-effects">
      <div class="grid-overlay"></div>
      <div class="glow-orb glow-orb-1"></div>
      <div class="glow-orb glow-orb-2"></div>
      <div class="glow-orb glow-orb-3"></div>
    </div>

    <!-- 左侧区域 -->
    <div class="login-left">
      <div class="logo-section" v-if="systemConfig.loaded">
        <div class="logo-wrapper">
          <img v-if="isBase64Image(systemConfig.loginLogo)" :src="systemConfig.loginLogo" class="logo-icon" alt="logo">
          <div v-else class="logo-icon" v-html="systemConfig.loginLogo"></div>
          <div class="logo-glow"></div>
        </div>
        <h1 class="title">
          <span class="title-main">{{ systemConfig.systemShortName }}</span>
          <span class="title-sub">{{ systemConfig.systemName }}</span>
        </h1>
        <p class="subtitle">{{ systemConfig.systemEnglishName }}</p>
      </div>

      <div class="features">
        <div class="feature-item" v-for="(feature, index) in features" :key="index" :style="{ '--delay': index * 0.1 + 's' }">
          <div class="feature-icon">
            <el-icon><component :is="feature.icon" /></el-icon>
          </div>
          <span class="feature-text">{{ feature.text }}</span>
          <div class="feature-line"></div>
        </div>
      </div>

      <!-- 底部版权 -->
      <div class="login-footer">
        <p>{{ systemConfig.systemCopyright }}</p>
      </div>
    </div>

    <!-- 右侧登录表单 -->
    <div class="login-right">
      <div class="login-form-wrapper">
        <div class="form-header">
          <h2 class="form-title">欢迎登录</h2>
          <p class="form-subtitle">请输入您的账号和密码</p>
        </div>

        <el-form ref="formRef" :model="loginForm" :rules="rules" class="login-form" @submit.native.prevent>
          <el-form-item prop="username">
            <el-input
              v-model="loginForm.username"
              placeholder="请输入用户名"
              size="large"
              :prefix-icon="UserIcon"
              clearable
              class="tech-input"
            />
          </el-form-item>

          <el-form-item prop="password">
            <el-input
              v-model="loginForm.password"
              type="password"
              placeholder="请输入密码"
              size="large"
              :prefix-icon="LockIcon"
              show-password
              clearable
              class="tech-input"
            />
          </el-form-item>

          <el-form-item>
            <el-checkbox v-model="loginForm.rememberMe" class="tech-checkbox">记住密码</el-checkbox>
          </el-form-item>

          <el-form-item>
            <el-button
              type="primary"
              size="large"
              class="login-btn"
              :loading="loading"
              @click="handleLogin"
            >
              <span v-if="!loading" class="btn-text">登 录</span>
              <span v-else class="btn-loading">登录中...</span>
              <div class="btn-glow"></div>
            </el-button>
          </el-form-item>
        </el-form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, onUnmounted, markRaw } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useUserStore } from '@/stores/user'
import { useSystemConfigStore } from '@/stores/systemConfig'
import { User, Lock, Operation, DataAnalysis, Setting } from '@element-plus/icons-vue'
import type { FormInstance, FormRules } from 'element-plus'

const router = useRouter()
const userStore = useUserStore()
const systemConfig = useSystemConfigStore()
const formRef = ref<FormInstance>()
const loading = ref(false)

// 图标组件使用 markRaw 避免响应式开销
const UserIcon = markRaw(User)
const LockIcon = markRaw(Lock)

const loginForm = reactive({
  username: '',
  password: '',
  rememberMe: false
})

// 键盘事件处理函数
const handleKeydown = (e: KeyboardEvent) => {
  if (e.key === 'Enter' && !loading.value) {
    handleLogin()
  }
}

// 页面加载时读取保存的凭证
onMounted(async () => {
  // 确保系统配置已加载（刷新后 store 状态丢失）
  if (!systemConfig.loaded) {
    await systemConfig.loadSystemConfigs()
  }

  const savedUsername = localStorage.getItem('remember_username')
  const savedPassword = localStorage.getItem('remember_password')
  const savedRemember = localStorage.getItem('remember_me')

  if (savedUsername) {
    loginForm.username = savedUsername
  }
  if (savedPassword && savedRemember === 'true') {
    loginForm.password = savedPassword
    loginForm.rememberMe = true
  }
})

// 注册键盘事件监听器
onMounted(() => {
  window.addEventListener('keydown', handleKeydown)
})

// 组件卸载时移除监听
onUnmounted(() => {
  window.removeEventListener('keydown', handleKeydown)
})

const rules = reactive<FormRules>({
  username: [
    { required: true, message: '请输入用户名', trigger: 'blur' },
    { min: 3, max: 20, message: '用户名长度在 3 到 20 个字符', trigger: 'blur' }
  ],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, max: 20, message: '密码长度在 6 到 20 个字符', trigger: 'blur' }
  ]
})

const features = [
  { icon: markRaw(Operation), text: '门店运营实时管控' },
  { icon: markRaw(DataAnalysis), text: '多维数据分析报表' },
  { icon: markRaw(Setting), text: '灵活的系统配置' },
  { icon: markRaw(Lock), text: '权限分级精细管控' }
]

// 判断是否为 base64 图片格式
const isBase64Image = (value: string): boolean => {
  return value.startsWith('data:image')
}

const handleLogin = async () => {
  if (!formRef.value) return

  await formRef.value.validate(async (valid) => {
    if (valid) {
      loading.value = true
      try {
        await userStore.login(loginForm.username, loginForm.password)

        // 重置并加载系统配置（确保新用户使用自己的配置）
        systemConfig.resetState()
        await systemConfig.loadSystemConfigs()

        // 记住密码功能
        if (loginForm.rememberMe) {
          localStorage.setItem('remember_username', loginForm.username)
          localStorage.setItem('remember_password', loginForm.password)
          localStorage.setItem('remember_me', 'true')
        } else {
          localStorage.removeItem('remember_username')
          localStorage.removeItem('remember_password')
          localStorage.setItem('remember_me', 'false')
        }

        ElMessage.success('登录成功')
        // 跳转到根路径，由路由守卫根据当前子系统的授权菜单决定首页
        // 有授权菜单时跳转到排序第1的叶子菜单；无授权菜单时跳转到 /no-permission
        await router.push('/')
      } catch (error: any) {
        ElMessage.error(error?.message || '登录失败，请检查用户名和密码')
      } finally {
        loading.value = false
      }
    }
  })
}
</script>

<style scoped>
.login-container {
  display: flex;
  width: 100%;
  height: 100vh;
  background: var(--bg-secondary);
  position: relative;
  overflow: hidden;
}

/* 背景效果 */
.bg-effects {
  position: absolute;
  inset: 0;
  pointer-events: none;
}

.grid-overlay {
  position: absolute;
  inset: 0;
  background-image:
    linear-gradient(rgba(6, 212, 228, 0.03) 1px, transparent 1px),
    linear-gradient(90deg, rgba(6, 212, 228, 0.03) 1px, transparent 1px);
  background-size: 40px 40px;
}

.glow-orb {
  position: absolute;
  border-radius: 50%;
  filter: blur(100px);
}

.glow-orb-1 {
  width: 500px;
  height: 500px;
  background: rgba(6, 212, 228, 0.12);
  top: -200px;
  left: -100px;
  animation: float 15s ease-in-out infinite;
}

.glow-orb-2 {
  width: 400px;
  height: 400px;
  background: rgba(91, 155, 255, 0.1);
  bottom: -150px;
  right: -50px;
  animation: float 20s ease-in-out infinite reverse;
}

.glow-orb-3 {
  width: 300px;
  height: 300px;
  background: rgba(16, 250, 158, 0.08);
  top: 50%;
  left: 30%;
  animation: float 18s ease-in-out infinite;
}

@keyframes float {
  0%, 100% { transform: translate(0, 0); }
  25% { transform: translate(20px, -20px); }
  50% { transform: translate(-15px, 15px); }
  75% { transform: translate(15px, 20px); }
}

/* 左侧区域 */
.login-left {
  flex: 1;
  display: flex;
  flex-direction: column;
  justify-content: center;
  padding: 0 80px;
  position: relative;
  z-index: 1;
}

.logo-section {
  margin-bottom: 60px;
}

.logo-wrapper {
  position: relative;
  width: 80px;
  height: 80px;
  margin-bottom: 24px;
}

.logo-icon {
  width: 100%;
  height: 100%;
  color: var(--primary);
  filter: drop-shadow(0 0 20px var(--primary-glow));
}

.logo-icon :deep(svg) {
  width: 100%;
  height: 100%;
}

.logo-glow {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  width: 120px;
  height: 120px;
  background: radial-gradient(circle, rgba(6, 212, 228, 0.3) 0%, transparent 70%);
  border-radius: 50%;
  animation: logo-pulse 3s ease-in-out infinite;
}

@keyframes logo-pulse {
  0%, 100% { transform: translate(-50%, -50%) scale(1); opacity: 0.5; }
  50% { transform: translate(-50%, -50%) scale(1.2); opacity: 0.8; }
}

.title {
  display: flex;
  flex-direction: column;
  gap: 4px;
  margin-bottom: 8px;
}

.title-main {
  font-size: 52px;
  font-weight: 700;
  color: var(--text-primary);
  letter-spacing: 8px;
  background: linear-gradient(135deg, var(--primary) 0%, var(--info) 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.title-sub {
  font-size: 20px;
  font-weight: 400;
  color: var(--text-tertiary);
  letter-spacing: 4px;
}

.subtitle {
  font-size: 14px;
  color: var(--text-tertiary);
  letter-spacing: 2px;
  font-weight: 300;
}

/* 特性列表 */
.features {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 24px;
}

.feature-item {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 16px 20px;
  background: var(--bg-tertiary);
  border-radius: var(--radius-md);
  border: 1px solid var(--border-primary);
  position: relative;
  overflow: hidden;
  animation: feature-appear 0.5s ease-out backwards;
  animation-delay: var(--delay, 0s);
  transition: all 0.3s;
}

@keyframes feature-appear {
  from {
    opacity: 0;
    transform: translateX(-20px);
  }
  to {
    opacity: 1;
    transform: translateX(0);
  }
}

.feature-item:hover {
  border-color: var(--primary);
  transform: translateX(4px);
}

.feature-icon {
  width: 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(6, 212, 228, 0.15);
  border-radius: var(--radius-sm);
  color: var(--primary);
  font-size: 18px;
  flex-shrink: 0;
}

.feature-text {
  font-size: 14px;
  color: var(--text-secondary);
  font-weight: 500;
}

.feature-line {
  position: absolute;
  bottom: 0;
  left: 0;
  width: 0;
  height: 2px;
  background: var(--primary);
  transition: width 0.3s;
}

.feature-item:hover .feature-line {
  width: 100%;
}

/* 底部版权 */
.login-footer {
  position: absolute;
  bottom: 30px;
  left: 80px;
}

.login-footer p {
  font-size: 12px;
  color: var(--text-disabled);
}

/* 右侧登录表单 */
.login-right {
  width: 520px;
  background: var(--bg-tertiary);
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
  z-index: 1;
  border-left: 1px solid var(--border-primary);
}

.login-right::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 3px;
  background: linear-gradient(90deg, var(--primary), var(--info), var(--success));
}

.login-form-wrapper {
  width: 380px;
}

.form-header {
  margin-bottom: 40px;
}

.form-title {
  font-size: 28px;
  font-weight: 700;
  color: var(--text-primary);
  margin-bottom: 8px;
}

.form-subtitle {
  font-size: 14px;
  color: var(--text-tertiary);
}

/* 科技风输入框 */
.login-form {
  margin-bottom: 20px;
}

:deep(.tech-input .el-input__wrapper) {
  background: var(--bg-elevated) !important;
  border: 1px solid var(--border-primary);
  border-radius: var(--radius-md);
  box-shadow: none !important;
  padding: 4px 12px;
}

:deep(.tech-input .el-input__wrapper:hover) {
  border-color: var(--border-secondary);
}

:deep(.tech-input .el-input__wrapper.is-focus) {
  border-color: var(--primary) !important;
  box-shadow: 0 0 0 3px var(--primary-glow) !important;
}

:deep(.tech-input .el-input__inner) {
  color: var(--text-primary);
}

:deep(.tech-input .el-input__inner::placeholder) {
  color: var(--text-disabled);
}

:deep(.tech-input .el-input__prefix) {
  color: var(--text-tertiary);
}

/* 科技风复选框 */
:deep(.tech-checkbox .el-checkbox__label) {
  color: var(--text-tertiary);
}

:deep(.tech-checkbox .el-checkbox__inner) {
  background: var(--bg-elevated);
  border-color: var(--border-primary);
}

:deep(.tech-checkbox .el-checkbox__input.is-checked .el-checkbox__inner) {
  background: var(--primary);
  border-color: var(--primary);
}

/* 表单选项 */
.form-options {
  display: flex;
  justify-content: space-between;
  align-items: center;
  width: 100%;
}

/* 登录按钮 */
.login-btn {
  width: 100%;
  height: 48px;
  font-size: 16px;
  font-weight: 600;
  background: linear-gradient(135deg, var(--primary) 0%, var(--primary-dim) 100%);
  border: none;
  border-radius: var(--radius-md);
  color: var(--bg-primary);
  position: relative;
  overflow: hidden;
  transition: all 0.3s;
}

.login-btn::before {
  content: '';
  position: absolute;
  top: 0;
  left: -100%;
  width: 100%;
  height: 100%;
  background: linear-gradient(90deg, transparent, rgba(255,255,255,0.2), transparent);
  transition: left 0.5s;
}

.login-btn:hover::before {
  left: 100%;
}

.login-btn:hover {
  box-shadow: var(--shadow-glow-primary);
  transform: translateY(-2px);
}

.btn-text {
  position: relative;
  z-index: 1;
}

.btn-loading {
  position: relative;
  z-index: 1;
}
</style>
