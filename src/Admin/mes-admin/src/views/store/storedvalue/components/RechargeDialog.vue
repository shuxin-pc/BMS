<template>
  <el-dialog
    :model-value="modelValue"
    title="储值充值"
    width="500px"
    :close-on-click-modal="false"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
      <el-form-item label="客户">
        <span class="customer-name">{{ customerName }}</span>
      </el-form-item>
      <el-form-item label="当前余额">
        <span class="balance-text">¥{{ formatPrice(currentBalance) }}</span>
      </el-form-item>
      <el-form-item label="充值金额" prop="amount">
        <el-input-number
          v-model="form.amount"
          :min="0.01"
          :precision="2"
          :step="100"
          controls-position="right"
          style="width: 100%"
        />
      </el-form-item>
      <el-form-item label="赠送金额">
        <span v-if="giftPreviewLoading" class="text-muted">计算中...</span>
        <span v-else class="bonus-text">¥{{ formatPrice(giftAmount) }}</span>
        <div class="form-tip">由储值规则按充值档位自动计算，不可修改</div>
      </el-form-item>
      <el-form-item label="支付方式" prop="payMethod">
        <el-radio-group v-model="form.payMethod">
          <el-radio :value="1">现金</el-radio>
          <el-radio :value="2">支付宝</el-radio>
          <el-radio :value="3">微信</el-radio>
          <el-radio :value="4">银行卡</el-radio>
        </el-radio-group>
      </el-form-item>
      <el-form-item label="充值后余额">
        <span class="price-text">¥{{ formatPrice(currentBalance + form.amount + giftAmount) }}</span>
      </el-form-item>
      <el-form-item label="备注" prop="remark">
        <el-input v-model="form.remark" type="textarea" :rows="2" placeholder="请输入备注" />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="emit('update:modelValue', false)">取消</el-button>
      <el-button type="primary" :loading="submitting" @click="handleSubmit">确认充值</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { rechargeAccount, previewRechargeGift } from '@/api/member'

const props = defineProps<{
  modelValue: boolean
  /** 充值客户ID */
  customerId: number
  /** 客户名称，仅用于展示 */
  customerName?: string
  /** 充值前余额，用于展示充值后余额 */
  currentBalance: number
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  /** 充值成功，携带本次实收金额与赠送金额，供调用方就地更新余额 */
  success: [payload: { amount: number; giftAmount: number }]
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
    { required: true, message: '充值金额不能为空', trigger: 'blur' },
    { type: 'number', min: 0.01, message: '充值金额必须大于0', trigger: 'blur' }
  ],
  payMethod: [{ required: true, message: '请选择支付方式', trigger: 'change' }]
}

// 赠送金额由后端试算接口返回，与实际入账口径一致，前端只展示不参与提交
const giftAmount = ref(0)
const giftPreviewLoading = ref(false)
let giftPreviewTimer: ReturnType<typeof setTimeout> | undefined

/** 防抖试算赠送金额，避免输入过程中频繁请求 */
const refreshGiftPreview = () => {
  if (giftPreviewTimer) clearTimeout(giftPreviewTimer)
  const amount = form.amount
  if (!amount || amount <= 0) {
    giftAmount.value = 0
    giftPreviewLoading.value = false
    return
  }
  giftPreviewLoading.value = true
  giftPreviewTimer = setTimeout(async () => {
    try {
      const res = await previewRechargeGift(amount)
      // 输入已变化时丢弃过期结果
      if (form.amount !== amount) return
      giftAmount.value = res.giftAmount
    } catch {
      giftAmount.value = 0
    } finally {
      if (form.amount === amount) giftPreviewLoading.value = false
    }
  }, 300)
}

watch(() => form.amount, refreshGiftPreview)

// 每次打开重置为默认值，避免上一次的金额/备注残留
watch(
  () => props.modelValue,
  (visible) => {
    if (!visible) return
    form.amount = 100
    form.payMethod = 1
    form.remark = ''
    refreshGiftPreview()
  }
)

const handleSubmit = async () => {
  if (!formRef.value) return
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  submitting.value = true
  try {
    await rechargeAccount({
      customerId: props.customerId,
      amount: form.amount,
      payMethod: form.payMethod,
      remark: form.remark || undefined
    })
    ElMessage.success('充值成功')
    emit('update:modelValue', false)
    emit('success', { amount: form.amount, giftAmount: giftAmount.value })
  } catch (error) {
    ElMessage.error((error as Error).message || '充值失败')
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

.text-muted {
  color: var(--text-tertiary);
}

.form-tip {
  font-size: 12px;
  color: var(--text-tertiary);
  line-height: 1.5;
}
</style>
