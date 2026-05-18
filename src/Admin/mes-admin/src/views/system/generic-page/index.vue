<template>
  <div class="generic-page-container">
    <el-card class="page-card">
      <template #header>
        <div class="page-header">
          <el-icon class="header-icon"><component :is="routeIcon" /></el-icon>
          <h1 class="page-title">{{ routeTitle }}</h1>
        </div>
      </template>

      <div class="page-content">
        <el-empty description="页面功能开发中..." />
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()

const routeTitle = computed(() => route.meta?.title || '页面')
const routeIcon = computed(() => route.meta?.icon || 'Document')
</script>

<style scoped>
.generic-page-container {
  padding: 24px;
  min-height: 100vh;
  background: var(--bg-secondary);
}

.page-card {
  background: var(--bg-tertiary);
  border: 1px solid var(--border-primary);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-lg);
}

.page-header {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 20px 24px;
  border-bottom: 1px solid var(--border-primary);
}

.header-icon {
  font-size: 28px;
  color: var(--primary);
}

.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: var(--text-primary);
}

.page-content {
  padding: 40px 24px;
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 400px;
}
</style>
