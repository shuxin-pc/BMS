<template>
  <div class="not-found-container">
    <el-empty description="页面未找到">
      <template #image>
        <el-icon :size="80" color="var(--el-text-color-secondary)">
          <WarningFilled />
        </el-icon>
      </template>
      <el-button type="primary" @click="goHome">返回首页</el-button>
    </el-empty>
  </div>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'
import { WarningFilled } from '@element-plus/icons-vue'

const router = useRouter()

const goHome = () => {
  router.push('/')
}
</script>

<style scoped>
.not-found-container {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100vh;
  width: 100%;
}
</style>
