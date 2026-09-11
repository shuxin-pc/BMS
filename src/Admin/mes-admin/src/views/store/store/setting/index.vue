<template>
  <div class="store-setting">
    <div class="card setting-card" v-loading="loading">
      <div class="page-header">
        <h3 class="page-title">门店设置</h3>
        <p class="page-desc">跨店核销为租户级配置，提醒接收角色按当前门店配置</p>
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
                开启后允许在其他门店核销项目卡；关闭后仅限发卡门店核销
              </span>
            </div>
          </div>
        </el-form-item>

        <el-form-item
          v-for="rt in reminderTypes"
          :key="rt.type"
          :label="`${rt.label}接收角色`"
        >
          <div class="switch-row">
            <el-select
              v-model="form.reminderRoles[rt.type]"
              multiple
              filterable
              collapse-tags
              collapse-tags-tooltip
              :placeholder="`选择接收${rt.label}站内信的角色（不选则不发送）`"
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
                多选；所选角色的用户将收到{{ rt.label }}站内信。空列表表示不发送
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
            v-if="hasPermission('store:store:setting:save')"
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
  updateStoreTenantSetting,
  getStoreReminderSettings,
  updateStoreReminderSettings
} from '@/api/store'
import { getAllRoles } from '@/api/system'
import { useUserStore } from '@/stores/user'
import type { Role } from '@/api/system/types'
import type { StoreTenantSettingUpdate, StoreReminderSetting } from '@/api/store/types'

defineOptions({ name: 'StoreSetting' })

const userStore = useUserStore()
const hasPermission = (permissionCode: string) => userStore.hasPermission(permissionCode)

// 提醒类型清单：各类型提醒的接收角色独立配置（对应子表 StoreReminderSetting.ReminderType）
const reminderTypes = [
  { type: 'Birthday', label: '生日提醒' },
  { type: 'Appointment', label: '预约提醒' },
  { type: 'TreatmentExpiry', label: '项目卡到期' }
] as const

const loading = ref(false)
const saving = ref(false)
// 角色选项加载状态：仅影响下拉框 loading 动画，加载失败不阻断主设置
const roleLoading = ref(false)
const roleOptions = ref<Role[]>([])

const form = reactive({
  allowCrossStoreVerify: true,
  // 各提醒类型接收角色ID（key 为 ReminderType，value 为 roleIds 字符串数组）
  reminderRoles: {} as Record<string, string[]>
})

// 原始快照，用于判断是否有改动
const snapshot = reactive({
  allowCrossStoreVerify: true,
  reminderRoles: {} as Record<string, string[]>
})

// 为每个提醒类型初始化空数组，保证 reactive 可追踪动态 key
reminderTypes.forEach(rt => {
  form.reminderRoles[rt.type] = []
  snapshot.reminderRoles[rt.type] = []
})

// 数组用 JSON.stringify 比对，保存后回填顺序与 snapshot 一致，无需额外排序
const dirty = computed(
  () =>
    form.allowCrossStoreVerify !== snapshot.allowCrossStoreVerify ||
    reminderTypes.some(
      rt =>
        JSON.stringify(form.reminderRoles[rt.type] ?? []) !==
        JSON.stringify(snapshot.reminderRoles[rt.type] ?? [])
    )
)

async function loadData() {
  loading.value = true
  try {
    // 并行加载主设置与各提醒类型配置，互不依赖
    const [setting, reminderSettings] = await Promise.all([
      getStoreTenantSetting(),
      getStoreReminderSettings()
    ])
    form.allowCrossStoreVerify = setting.allowCrossStoreVerify
    snapshot.allowCrossStoreVerify = setting.allowCrossStoreVerify

    // 按类型填充角色ID：未配置的类型保持空数组
    const roleMap = new Map(reminderSettings.map(s => [s.reminderType, s.roleIds ?? []]))
    reminderTypes.forEach(rt => {
      const roles = roleMap.get(rt.type) ?? []
      form.reminderRoles[rt.type] = [...roles]
      snapshot.reminderRoles[rt.type] = [...roles]
    })
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
    // 1. 保存主设置（跨店核销开关）
    const payload: StoreTenantSettingUpdate = {
      allowCrossStoreVerify: form.allowCrossStoreVerify
    }
    await updateStoreTenantSetting(payload)

    // 2. 保存各提醒类型接收角色（整体提交）
    const reminderPayload: StoreReminderSetting[] = reminderTypes.map(rt => ({
      reminderType: rt.type,
      roleIds: [...(form.reminderRoles[rt.type] ?? [])]
    }))
    const savedReminderSettings = await updateStoreReminderSettings(reminderPayload)

    // 3. 回填快照，保证 dirty 判定回到未改动状态
    snapshot.allowCrossStoreVerify = form.allowCrossStoreVerify
    const roleMap = new Map(savedReminderSettings.map(s => [s.reminderType, s.roleIds ?? []]))
    reminderTypes.forEach(rt => {
      const roles = roleMap.get(rt.type) ?? []
      form.reminderRoles[rt.type] = [...roles]
      snapshot.reminderRoles[rt.type] = [...roles]
    })
    ElMessage.success('保存成功')
  } catch (err) {
    ElMessage.error((err as { message?: string }).message || '保存失败')
  } finally {
    saving.value = false
  }
}

function handleReset() {
  form.allowCrossStoreVerify = snapshot.allowCrossStoreVerify
  reminderTypes.forEach(rt => {
    form.reminderRoles[rt.type] = [...(snapshot.reminderRoles[rt.type] ?? [])]
  })
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
