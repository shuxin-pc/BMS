<template>
  <el-dialog
    :model-value="modelValue"
    title="储值退款"
    width="500px"
    :close-on-click-modal="false"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
      <el-form-item label="客户">
        <span class="customer-name">{{ customerName }}</span>
      </el-form-item>
      <el-form-item label="当前实收余额">
        <span class="balance-text">¥{{ formatPrice(realBalance) }}</span>
      </el-form-item>
      <el-form-item label="当前赠送余额">
        <span class="bonus-text">¥{{ formatPrice(giftBalance) }}</span>
      </el-form-item>
      <el-form-item label="退款金额" prop="amount">
        <el-input-number
          v-model="form.amount"
          :min="0.01"
          :max="realBalance"
          :precision="2"
          :step="100"
          controls-position="right"
          style="width: 100%"
        />
        <div class="form-tip">可退上限 = 剩余实收余额 ¥{{ formatPrice(realBalance) }}，赠送余额一律不退</div>
      </el-form-item>
      <el-form-item label="支付方式" prop="payMethod">
        <el-radio-group v-model="form.payMethod">
          <el-radio :value="1">现金</el-radio>
          <el-radio :value="2">支付宝</el-radio>
          <el-radio :value="3">微信</el-radio>
          <el-radio :value="4">银行卡</el-radio>
        </el-radio-group>
      </el-form-item>
      <el-form-item label="退款后余额">
        <span class="price-text">¥{{ formatPrice(Math.max(0, realBalance - form.amount) + giftBalance) }}</span>
      </el-form-item>
      <el-form-item label="退款原因" prop="remark">
        <el-input
          v-model="form.remark"
          type="textarea"
          :rows="2"
          :maxlength="200"
          show-word-limit
          placeholder="请填写退款原因（必填，便于审计追溯）"
        />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="emit('update:modelValue', false)">取消</el-button>
      <el-button type="danger" :loading="submitting" @click="handleSubmit">确认退款</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { refundStoredValue } from '@/api/member'

const props = defineProps<{
  modelValue: boolean
  /** 退款客户ID */
  customerId: number
  /** 客户名称，仅用于展示 */
  customerName?: string
  /** 当前实收余额（退款金额上限，文档 G7：只退实收、赠送不退） */
  realBalance: number
  /** 当前赠送余额 */
  giftBalance: number
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  /** 退款成功，调用方需刷新列表 */
  success: []
}>()

const formRef = ref<FormInstance>()
const submitting = ref(false)

const form = reactive({
  amount: 100,
  payMethod: 1,
  remark: ''
})

const rules: FormRules = {
  amount: [
    { required: true, message: '退款金额不能为空', trigger: 'blur' },
    { type: 'number', min: 0.01, message: '退款金额必须大于0', trigger: 'blur' },
    {
      validator: (_rule, value: number, callback) => {
        if (value > props.realBalance) callback(new Error('退款金额不能超过剩余实收余额（赠送余额一律不退）'))
        else callback()
      },
      trigger: 'blur'
    }
  ],
  payMethod: [{ required: true, message: '请选择支付方式', trigger: 'change' }],
  remark: [
    { required: true, message: '请填写退款原因', trigger: 'blur' },
    { max: 200, message: '退款原因不能超过200字', trigger: 'blur' }
  ]
}

// 每次打开重置为默认值，避免上一次的金额/原因残留
watch(
  () => props.modelValue,
  (visible) => {
    if (!visible) return
    form.amount = Math.min(100, props.realBalance || 100)
    form.payMethod = 1
    form.remark = ''
    // 重置校验状态，避免上次打开时的错误提示残留
    formRef.value?.clearValidate()
  }
)

const handleSubmit = async () => {
  if (!formRef.value) return
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  submitting.value = true
  try {
    await refundStoredValue({
      customerId: props.customerId,
      amount: form.amount,
      payMethod: form.payMethod,
      remark: form.remark.trim()
    })
    ElMessage.success('退款成功')
    emit('update:modelValue', false)
    emit('success')
  } catch (error) {
    ElMessage.error((error as Error).message || '退款失败')
  } finally {
    submitting.value = false
  }
}

/** 格式化金额 */
const formatPrice = (price: number | undefined) => {
  if (price === null || price === undefined) return '0.00'
  return price.toFixed(2)
}
</script>

<style scoped>
/* 弹窗为浅色浮层，此处需用深色文字，不能复用深色主题的 --text-primary */
.customer-name {
  font-weight: 500;
  color: #1f2937;
}

.balance-text,
.price-text {
  color: var(--primary);
  font-weight: 600;
}

.bonus-text {
  color: var(--el-color-success);
}

.form-tip {
  font-size: 12px;
  color: var(--text-tertiary);
  line-height: 1.5;
}
</style>
