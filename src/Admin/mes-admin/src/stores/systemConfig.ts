// 系统配置 Store
import { defineStore } from 'pinia'
import { getSystemConfigDict } from '@/api/system'

export interface SystemConfig {
  id: number
  configKey: string
  configValue: string
  configGroup: string
  description: string
  isPublic: boolean
  isEditable: boolean
  isSystem: boolean
  sort: number
  tenantId: number
  tenantCode: string
  createdTime: string
}

export const useSystemConfigStore = defineStore('systemConfig', {
  state: () => ({
    // System 分组配置
    systemConfigs: {} as Record<string, string>,
    // 是否已加载
    loaded: false
  }),

  getters: {
    // 获取配置值，支持默认值
    getConfig: (state) => (key: string, defaultValue: string = ''): string => {
      return state.systemConfigs[key] ?? defaultValue
    },
    // 系统名称
    systemName(state): string {
      return state.systemConfigs['SystemName'] ?? 'BMS后台管理系统'
    },
    // 系统英文名
    systemEnglishName(state): string {
      return state.systemConfigs['SystemEnglishName'] ?? 'Backend Management System'
    },
    // 系统简称
    systemShortName(state): string {
      return state.systemConfigs['SystemShortName'] ?? 'BMS'
    },
    // 系统版本
    systemVersion(state): string {
      return state.systemConfigs['SystemVersion'] ?? '2.0'
    },
    // 登录页 Logo
    loginLogo(state): string {
      return state.systemConfigs['LoginLogo'] ?? ''
    },
    // 框架左上角 Logo
    systemLogo(state): string {
      return state.systemConfigs['SystemLogo'] ?? ''
    },
    // 系统版权信息
    systemCopyright(state): string {
      return state.systemConfigs['SystemCopyright'] ?? '©2026 BMS保留所有权利'
    },
    // 默认分页大小
    defaultPageSize(state): number {
      const size = state.systemConfigs['DefaultPageSize'] ?? '20'
      return parseInt(size) || 20
    },
    // 分页选项列表
    defaultPageSizes(state): number[] {
      const sizes = state.systemConfigs['DefaultPageSizes'] ?? '10,15,20,50'
      return sizes.split(',').map(s => parseInt(s.trim()) || 10)
    }
  },

  actions: {
    /**
     * 重置 store 状态（用户登出时调用）
     */
    resetState() {
      this.systemConfigs = {}
      this.loaded = false
    },

    /**
     * 加载系统配置（从配置应用API获取，按租户优先级返回）
     */
    async loadSystemConfigs() {
      const configMap = await getSystemConfigDict()
      this.systemConfigs = configMap
      this.loaded = true
      return configMap
    }
  }
})
