<template>
  <div class="no-store-container">
    <el-empty>
      <template #image>
        <el-icon :size="80" color="var(--el-text-color-secondary)">
          <Shop />
        </el-icon>
      </template>
      <template #description>
        <p class="title">暂无授权门店</p>
        <p class="desc">您尚未被分配到任何门店，请联系管理员绑定门店后再访问此功能</p>
      </template>
      <el-button type="primary" @click="handleLogout">退出登录</el-button>
    </el-empty>
  </div>
</template>

<script setup lang="ts">
import { Shop } from '@element-plus/icons-vue'
import { useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user'

const router = useRouter()
const userStore = useUserStore()

const handleLogout = async () => {
  await userStore.logout()
  router.push('/login')
}
</script>

<style scoped>
.no-store-container {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  width: 100%;
}
.title {
  font-size: 16px;
  font-weight: 600;
  color: var(--el-text-color-primary);
  margin: 0 0 8px;
}
.desc {
  font-size: 14px;
  color: var(--el-text-color-secondary);
  margin: 0 0 16px;
}
</style>
