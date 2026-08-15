<template>
  <div class="store-setting">
    <div class="card setting-card" v-loading="loading">
      <div class="page-header">
        <h3 class="page-title">门店设置</h3>
        <p class="page-desc">跨店权益配置，修改后立即生效</p>
      </div>

      <el-form label-width="160px" class="setting-form">
        <el-form-item label="跨店核销">
          <div class="switch-row">
            <el-switch
              v-model="form.allowCrossStoreVerify"
              :disabled="saving"
              :loading="saving"
            />
            <div class="switch-meta">
              <span class="switch-label">
                {{ form.allowCrossStoreVerify ? '已开启' : '已关闭' }}
              </span>
              <span class="switch-hint">
                开启后允许在其他门店核销疗程卡；关闭后仅限发卡门店核销
              </span>
            </div>
          </div>
        </el-form-item>

        <el-form-item label="生日提醒接收角色">
          <div class="switch-row">
            <el-select
              v-model="form.birthdayReminderRoleIds"
              multiple
              filterable
              collapse-tags
              collapse-tags-tooltip
              placeholder="选择接收客户生日提醒站内信的角色（不选则不发送）"
              :disabled="saving || roleLoading"
              :loading="roleLoading"
              style="width: 360px"
            >
              <el-option
                v-for="r in roleOptions"
                :key="r.id"
                :label="r.name"
                :value="String(r.id)"
              />
            </el-select>
            <div class="switch-meta">
              <span class="switch-hint">
                多选；所选角色的用户将在客户生日时收到站内信提醒。空列表表示不发送
              </span>
            </div>
          </div>
        </el-form-item>

        <el-form-item>
          <el-button
            type="primary"
            :loading="saving"
            :disabled="loading || !dirty"
            @click="handleSave"
          >
            保存设置
          </el-button>
          <el-button :disabled="saving" @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import {
  getStoreTenantSetting,
  updateStoreTenantSetting
} from '@/api/store'
import { getAllRoles } from '@/api/system'
import type { Role } from '@/api/system/types'
import type { StoreTenantSettingUpdate } from '@/api/store/types'

defineOptions({ name: 'StoreSetting' })

const loading = ref(false)
const saving = ref(false)
// 角色选项加载状态：仅影响下拉框 loading 动画，加载失败不阻断主设置
const roleLoading = ref(false)
const roleOptions = ref<Role[]>([])

const form = reactive({
  allowCrossStoreVerify: true,
  birthdayReminderRoleIds: [] as string[]
})

// 原始快照，用于判断是否有改动
const snapshot = reactive({
  allowCrossStoreVerify: true,
  birthdayReminderRoleIds: [] as string[]
})

// 数组用 JSON.stringify 比对，保存后回填顺序与 snapshot 一致，无需额外排序
const dirty = computed(
  () =>
    form.allowCrossStoreVerify !== snapshot.allowCrossStoreVerify ||
    JSON.stringify(form.birthdayReminderRoleIds) !==
      JSON.stringify(snapshot.birthdayReminderRoleIds)
)

async function loadData() {
  loading.value = true
  try {
    const data = await getStoreTenantSetting()
    form.allowCrossStoreVerify = data.allowCrossStoreVerify
    form.birthdayReminderRoleIds = [...(data.birthdayReminderRoleIds ?? [])]
    snapshot.allowCrossStoreVerify = data.allowCrossStoreVerify
    snapshot.birthdayReminderRoleIds = [...(data.birthdayReminderRoleIds ?? [])]
  } catch (err) {
    ElMessage.error((err as { message?: string }).message || '加载门店设置失败')
  } finally {
    loading.value = false
  }
}

// 加载当前租户全部角色作为多选选项（角色为租户级，无需传 tenantId）
async function loadRoles() {
  roleLoading.value = true
  try {
    const roles = await getAllRoles()
    roleOptions.value = roles || []
  } catch (err) {
    // 角色加载失败不阻断页面，仅提示；用户仍可保存其他设置
    ElMessage.error((err as { message?: string }).message || '加载角色列表失败')
  } finally {
    roleLoading.value = false
  }
}

async function handleSave() {
  if (!dirty.value) return
  saving.value = true
  try {
    const payload: StoreTenantSettingUpdate = {
      allowCrossStoreVerify: form.allowCrossStoreVerify,
      birthdayReminderRoleIds: [...form.birthdayReminderRoleIds]
    }
    const data = await updateStoreTenantSetting(payload)
    snapshot.allowCrossStoreVerify = data.allowCrossStoreVerify
    snapshot.birthdayReminderRoleIds = [...(data.birthdayReminderRoleIds ?? [])]
    form.allowCrossStoreVerify = data.allowCrossStoreVerify
    form.birthdayReminderRoleIds = [...(data.birthdayReminderRoleIds ?? [])]
    ElMessage.success('保存成功')
  } catch (err) {
    ElMessage.error((err as { message?: string }).message || '保存失败')
  } finally {
    saving.value = false
  }
}

function handleReset() {
  form.allowCrossStoreVerify = snapshot.allowCrossStoreVerify
  form.birthdayReminderRoleIds = [...snapshot.birthdayReminderRoleIds]
}

onMounted(() => {
  loadData()
  loadRoles()
})
</script>

<style scoped>
.store-setting {
  width: 100%;
}

/* 卡片：复用全局 .card 视觉规范，补充内边距 */
.setting-card {
  padding: 24px;
}

/* 页面标题区：与全局 .card-header 装饰风格保持一致 */
.page-header {
  margin-bottom: 24px;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--border-primary);
  position: relative;
}

.page-header::after {
  content: '';
  position: absolute;
  bottom: -1px;
  left: 0;
  width: 40px;
  height: 2px;
  background: var(--primary);
}

.page-title {
  margin: 0 0 8px;
  font-size: 18px;
  font-weight: 600;
  color: var(--text-primary);
}

.page-desc {
  margin: 0;
  font-size: 13px;
  color: var(--text-secondary);
}

.setting-form {
  max-width: 640px;
}

.switch-row {
  display: flex;
  align-items: flex-start;
  gap: 12px;
}

.switch-meta {
  display: flex;
  flex-direction: column;
  gap: 4px;
  line-height: 1.5;
}

.switch-label {
  font-size: 14px;
  font-weight: 500;
  color: var(--text-primary);
}

.switch-hint {
  font-size: 12px;
  color: var(--text-tertiary);
}
</style>
