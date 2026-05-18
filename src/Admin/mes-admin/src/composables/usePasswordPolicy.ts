/**
 * 密码策略 Composable
 * 用于动态获取系统密码策略配置，并提供密码验证功能
 */
import { ref } from 'vue'
import { getSystemConfigsByGroup } from '@/api/system'

// 密码策略配置映射
const POLICY_LABELS: Record<string, string> = {
  PasswordRequireUppercase: '大写字母(A-Z)',
  PasswordRequireLowercase: '小写字母(a-z)',
  PasswordRequireNumber: '数字(0-9)',
  PasswordRequireSpecialChar: '特殊字符(!@#$等)',
}

export interface PasswordPolicy {
  minLength: number | null
  maxLength: number | null
  requireUppercase: boolean
  requireLowercase: boolean
  requireNumber: boolean
  requireSpecialChar: boolean
}

export interface PasswordPolicyResult {
  /** 密码要求提示文字，如 "密码长度8-32位，必须包含大写字母(A-Z)、小写字母(a-z)、数字(0-9)、特殊字符(!@#$等)" */
  policyText: string
  /** 原始策略对象 */
  policy: PasswordPolicy
  /** 验证密码是否符合策略 */
  validatePassword: (password: string) => { valid: boolean; errors: string[] }
}

/**
 * 使用密码策略
 * @returns 密码策略相关状态和方法
 */
export function usePasswordPolicy() {
  // 密码策略配置
  const policy = ref<PasswordPolicy>({
    minLength: null,
    maxLength: null,
    requireUppercase: false,
    requireLowercase: false,
    requireNumber: false,
    requireSpecialChar: false,
  })

  // 加载状态
  const loading = ref(false)

  /**
   * 加载密码策略配置
   */
  async function loadPolicy() {
    loading.value = true
    try {
      const configs = await getSystemConfigsByGroup('Security')

      // 重置策略
      policy.value = {
        minLength: null,
        maxLength: null,
        requireUppercase: false,
        requireLowercase: false,
        requireNumber: false,
        requireSpecialChar: false,
      }

      // 解析配置
      for (const config of configs) {
        const key = config.configKey
        const value = config.configValue

        switch (key) {
          case 'PasswordMinLength':
            policy.value.minLength = parseInt(value, 10) || null
            break
          case 'PasswordMaxLength':
            policy.value.maxLength = parseInt(value, 10) || null
            break
          case 'PasswordRequireUppercase':
            policy.value.requireUppercase = value === 'true'
            break
          case 'PasswordRequireLowercase':
            policy.value.requireLowercase = value === 'true'
            break
          case 'PasswordRequireNumber':
            policy.value.requireNumber = value === 'true'
            break
          case 'PasswordRequireSpecialChar':
            policy.value.requireSpecialChar = value === 'true'
            break
        }
      }
    } catch (error) {
      console.error('加载密码策略失败:', error)
    } finally {
      loading.value = false
    }
  }

  /**
   * 生成密码要求提示文字
   */
  function getPolicyText(): string {
    const parts: string[] = []
    const p = policy.value

    // 长度要求
    if (p.minLength !== null || p.maxLength !== null) {
      if (p.minLength !== null && p.maxLength !== null) {
        parts.push(`长度${p.minLength}-${p.maxLength}位`)
      } else if (p.minLength !== null) {
        parts.push(`至少${p.minLength}位`)
      } else if (p.maxLength !== null) {
        parts.push(`最多${p.maxLength}位`)
      }
    }

    // 必含字符要求
    if (p.requireUppercase) {
      parts.push(POLICY_LABELS.PasswordRequireUppercase)
    }
    if (p.requireLowercase) {
      parts.push(POLICY_LABELS.PasswordRequireLowercase)
    }
    if (p.requireNumber) {
      parts.push(POLICY_LABELS.PasswordRequireNumber)
    }
    if (p.requireSpecialChar) {
      parts.push(POLICY_LABELS.PasswordRequireSpecialChar)
    }

    if (parts.length === 0) {
      return ''
    }

    return '密码' + parts.join('、')
  }

  /**
   * 验证密码是否符合策略
   * @param password 密码字符串
   * @returns 验证结果 { valid: boolean, errors: string[] }
   */
  function validatePassword(password: string): { valid: boolean; errors: string[] } {
    const errors: string[] = []
    const p = policy.value

    // 检查长度
    if (p.minLength !== null && password.length < p.minLength) {
      errors.push(`密码长度不能少于${p.minLength}位`)
    }
    if (p.maxLength !== null && password.length > p.maxLength) {
      errors.push(`密码长度不能超过${p.maxLength}位`)
    }

    // 检查大写字母
    if (p.requireUppercase && !/[A-Z]/.test(password)) {
      errors.push('必须包含大写字母')
    }

    // 检查小写字母
    if (p.requireLowercase && !/[a-z]/.test(password)) {
      errors.push('必须包含小写字母')
    }

    // 检查数字
    if (p.requireNumber && !/[0-9]/.test(password)) {
      errors.push('必须包含数字')
    }

    // 检查特殊字符
    if (p.requireSpecialChar && !/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?`~]/.test(password)) {
      errors.push('必须包含特殊字符')
    }

    return {
      valid: errors.length === 0,
      errors,
    }
  }

  /**
   * 获取 Element Plus 表单验证规则
   * @returns 适用于 el-form-item 的验证规则
   */
  function getPasswordRules() {
    return {
      validator: (_rule: any, value: string, callback: (error?: Error) => void) => {
        if (!value) {
          callback(new Error('请输入新密码'))
          return
        }

        const { valid, errors } = validatePassword(value)
        if (!valid) {
          callback(new Error(errors[0]))
        } else {
          callback()
        }
      },
      trigger: 'blur' as const,
    }
  }

  /**
   * 获取策略结果对象（包含提示文字和验证方法）
   */
  function getPolicyResult(): PasswordPolicyResult {
    return {
      policyText: getPolicyText(),
      policy: policy.value,
      validatePassword,
    }
  }

  return {
    policy,
    loading,
    loadPolicy,
    getPolicyText,
    validatePassword,
    getPasswordRules,
    getPolicyResult,
  }
}
