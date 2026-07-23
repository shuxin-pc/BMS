<template>
  <div class="no-permission-container">
    <el-empty description="暂无可访问的子系统/页面，请联系管理员分配">
      <template #image>
        <el-icon :size="80" color="var(--el-text-color-secondary)">
          <Lock />
        </el-icon>
      </template>
    </el-empty>
  </div>
</template>

<script setup lang="ts">
import { Lock } from '@element-plus/icons-vue'
</script>

<style scoped>
.no-permission-container {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  width: 100%;
}
</style>
