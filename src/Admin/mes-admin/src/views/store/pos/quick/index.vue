<template>
  <div class="pos-page">
    <div class="pos-container">
      <!-- ========== 左侧商品选择区 ========== -->
      <div class="product-section">
        <div class="product-header">
          <el-input
            v-model="searchKeyword"
            placeholder="搜索商品名称 / 扫码识别..."
            clearable
            :prefix-icon="Search"
            class="search-input"
            @keyup.enter="handleSearch"
          />
          <el-button class="scan-btn" @click="handleScan">
            <el-icon><Aim /></el-icon>
            扫码
          </el-button>
        </div>

        <!-- 商品/赠品 切换 Tab（赠品是 Product 表 type=5 的记录，POS 可选赠品随订单一起提交，后端通过 Product.Type=5 识别并走 DeductSampleGiftOutAsync 扣减） -->
        <div class="mode-tabs">
          <div
            class="mode-tab"
            :class="{ active: productMode === 'product' }"
            @click="switchProductMode('product')"
          >商品</div>
          <div
            class="mode-tab"
            :class="{ active: productMode === 'gift' }"
            @click="switchProductMode('gift')"
          >赠品</div>
        </div>

        <!-- 分类Tab（仅商品模式显示；赠品不按分类组织） -->
        <div v-if="productMode === 'product'" class="category-tabs">
          <div
            v-for="cat in categories"
            :key="cat.id"
            class="cat-tab"
            :class="{ active: activeCategory === cat.id }"
            @click="handleCategoryChange(cat.id)"
          >
            {{ cat.name }}
          </div>
        </div>

        <!-- 商品网格（商品模式显示可销售商品；赠品模式显示 Type=5 赠品） -->
        <div class="product-grid">
          <div
            v-for="product in filteredProducts"
            :key="product.id"
            class="product-card"
            :class="{ 'gift-card': productMode === 'gift' }"
            @click="addToCart(product)"
          >
            <div class="product-img" :class="product.colorClass">
              {{ product.name.charAt(0) }}
            </div>
            <div class="product-name">{{ product.name }}</div>
            <div class="product-spec">{{ product.spec }}</div>
            <!-- 赠品免费，显示"赠品"标签；商品显示价格 -->
            <div v-if="productMode === 'gift'" class="product-price gift-price-text">赠品</div>
            <div v-else class="product-price">¥{{ formatPrice(product.price) }}</div>
            <div class="product-stock">{{ product.stockText }}</div>
          </div>
          <div v-if="filteredProducts.length === 0" class="empty-products">
            <el-icon><Box /></el-icon>
            <p>{{ productMode === 'gift' ? '暂无赠品' : '暂无商品' }}</p>
          </div>
        </div>
      </div>

      <!-- ========== 右侧订单区 ========== -->
      <div class="order-section">
        <div class="order-header">
          <div class="order-title">当前订单</div>
          <div class="order-meta">
            <span>单号: {{ orderNo }}</span>
            <span>{{ currentTime }}</span>
          </div>
        </div>

        <!-- 会员选择 -->
        <div class="member-bar">
          <el-input
            v-model="memberKeyword"
            placeholder="输入会员手机号 / 卡号"
            class="member-input"
            @keyup.enter="handleMemberSearch"
          />
          <el-button class="member-btn" @click="handleMemberSearch">选择会员</el-button>
        </div>

        <!-- 会员信息 -->
        <div v-if="selectedMember" class="member-info">
          <div class="member-avatar">{{ selectedMember.name.charAt(0) }}</div>
          <div class="member-detail">
            <div class="member-name">
              {{ selectedMember.name }}
              <span class="member-tag">{{ selectedMember.levelName }}</span>
            </div>
            <div class="member-balance">储值余额: ¥{{ formatPrice(selectedMember.balance) }}</div>
          </div>
          <el-icon class="member-remove" @click="clearMember"><Close /></el-icon>
        </div>

        <!-- 购物车列表 -->
        <div class="cart-list">
          <div v-if="cart.length === 0" class="empty-cart">
            <el-icon class="icon"><ShoppingCart /></el-icon>
            <p>购物车为空，点击左侧商品添加</p>
          </div>
          <div v-for="item in cart" :key="item.id" class="cart-item" :class="{ 'gift-item': item.productType === 5 }">
            <div class="cart-info">
              <div class="cart-name">
                {{ item.name }}
                <el-tag v-if="item.productType === 5" size="small" type="warning" effect="plain" class="gift-tag">赠品</el-tag>
              </div>
              <div class="cart-spec">
                {{ item.spec }}
                <span v-if="item.technicianName" class="cart-tech">技师: {{ item.technicianName }}</span>
                <!-- 赠品（type=5）与实物商品（type=1）一样需要批次效期选择，后端按效期 DeductSampleGiftOutAsync 扣减 -->
                <span v-if="(item.productType === 1 || item.productType === 5) && item.expirationDates?.length" class="cart-expiry">效期: {{ item.expirationDates.map(d => d ? d.slice(5, 10) : '无效期').join(', ') }}</span>
                <span v-else-if="item.productType === 1" class="cart-expiry">效期: 自动推荐</span>
                <!-- 赠品必须显式选择效期，不允许"自动推荐"（避免误扣非预期批次） -->
                <span v-else-if="item.productType === 5" class="cart-expiry expiry-missing">效期未选</span>
                <el-button v-if="item.productType === 2" link size="small" @click="openTechDialog(item)">选技师</el-button>
              </div>
            </div>
            <div class="qty-control">
              <button class="qty-btn" @click="changeQty(item, -1)">−</button>
              <input
                class="qty-input"
                v-model.number="item.quantity"
                @change="onQtyInput(item)"
              />
              <button class="qty-btn" @click="changeQty(item, 1)">+</button>
            </div>
            <div class="cart-price">¥{{ formatPrice(item.price * item.quantity) }}</div>
            <span class="cart-remove" @click="removeFromCart(item)">×</span>
          </div>
        </div>

        <!-- 底部结算栏 -->
        <div class="checkout-bar">
          <div class="total-row">
            <span class="total-label">商品件数</span>
            <span class="total-value">{{ totalQuantity }} 件</span>
          </div>
          <div class="total-row">
            <span class="total-label">商品总额</span>
            <span class="total-value">¥{{ formatPrice(subtotal) }}</span>
          </div>
          <div class="total-row" v-if="discountAmount > 0">
            <span class="total-label">会员折扣 ({{ selectedMember?.discountRate || 10 }}折)</span>
            <span class="total-value discount-text">−¥{{ formatPrice(discountAmount) }}</span>
          </div>
          <div class="grand-total">
            <span class="label">应付金额</span>
            <span class="amount">¥{{ formatPrice(payableAmount) }}</span>
          </div>
          <el-button
            class="checkout-btn"
            :disabled="cart.length === 0 || hasInvalidGiftInCart"
            @click="openPayDialog"
          >
            结算收银
          </el-button>
          <div class="quick-actions">
            <button class="quick-btn" @click="handleHoldOrder">挂单</button>
            <button class="quick-btn" @click="handleResumeOrder">取单</button>
            <button class="quick-btn" @click="handleVerifyCard">疗程卡核销</button>
            <button class="quick-btn" @click="handleAppointmentToOrder">预约转单</button>
            <button class="quick-btn" @click="handleClearCart">清空</button>
          </div>
        </div>
      </div>
    </div>

    <!-- 收银结算弹窗 -->
    <el-dialog
      v-model="payDialogVisible"
      title="结算收银"
      width="520px"
      :close-on-click-modal="false"
      class="pay-dialog"
    >
      <div class="pay-amount">
        <div class="label">应收金额</div>
        <div class="value">¥{{ formatPrice(payableAmount) }}</div>
      </div>
      <div class="pay-methods">
        <div
          v-for="method in payMethods"
          :key="method.id"
          class="pay-method"
          :class="{ selected: selectedPayMethod === method.id }"
          @click="onSelectPayMethod(method.id)"
        >
          <div class="icon" :style="{ color: method.color }">
            <el-icon><component :is="method.icon" /></el-icon>
          </div>
          <div class="name">{{ method.name }}</div>
        </div>
      </div>

      <!-- 组合支付 3 栏联动输入 -->
      <div v-if="isCombinedPay" class="combined-pay-section">
        <div class="combined-row">
          <div class="combined-label">类别1 现金/支付宝/微信/银行卡</div>
          <el-select v-model="cashPayMethod" placeholder="选择支付方式" size="small" style="width: 140px">
            <el-option :value="1" label="现金" />
            <el-option :value="2" label="支付宝" />
            <el-option :value="3" label="微信" />
            <el-option :value="4" label="银行卡" />
          </el-select>
          <el-input-number
            v-model="cashAmount"
            :min="0"
            :precision="2"
            :step="1"
            size="small"
            style="flex: 1"
            @change="onCashAmountChange"
          />
        </div>
        <div class="combined-row">
          <div class="combined-label">
            类别2 储值余额
            <span v-if="selectedMember" class="combined-hint">（余额 ¥{{ formatPrice(selectedMember.balance) }}）</span>
          </div>
          <el-input-number
            v-model="storedValueAmount"
            :min="0"
            :max="selectedMember?.balance || 0"
            :precision="2"
            :step="1"
            size="small"
            style="flex: 1"
            @change="onStoredValueChange"
          />
        </div>
        <div class="combined-row">
          <div class="combined-label">
            类别3 积分抵扣
            <span v-if="selectedMember" class="combined-hint">
              （可用 {{ selectedMember.totalPoints }} 积分，最多抵 ¥{{ formatPrice(maxPointsDeduct) }}）
            </span>
          </div>
          <el-input-number
            v-model="pointsAmount"
            :min="0"
            :max="maxPointsDeduct"
            :precision="2"
            :step="1"
            size="small"
            style="flex: 1"
            @change="onPointsChange"
          />
        </div>
        <div class="combined-summary">
          合计：¥{{ formatPrice(combinedTotal) }}
          <span v-if="Math.abs(combinedTotal - payableAmount) > 0.01" class="combined-warn">
            （与应收 ¥{{ formatPrice(payableAmount) }} 不等）
          </span>
        </div>
      </div>

      <div class="backfill-section">
        <el-checkbox v-model="isBackfill">补录历史订单</el-checkbox>
        <el-date-picker
          v-if="isBackfill"
          v-model="backfillDate"
          type="datetime"
          placeholder="选择补录时间"
          format="YYYY-MM-DD HH:mm"
          value-format="YYYY-MM-DDTHH:mm:ss"
          style="width: 100%; margin-top: 8px"
        />
      </div>
      <template #footer>
        <el-button @click="payDialogVisible = false">取消</el-button>
        <el-button
          type="primary"
          :loading="paying"
          :disabled="isCombinedPay && !canConfirmCombinedPay"
          @click="handleConfirmPay"
        >确认收款</el-button>
      </template>
    </el-dialog>

    <!-- 技师选择弹窗 -->
    <el-dialog v-model="techDialogVisible" title="选择技师" width="400px" :close-on-click-modal="false">
      <div class="tech-list">
        <div
          v-for="tech in technicianList"
          :key="tech.id"
          class="tech-item"
          :class="{ selected: selectedTechId === tech.id }"
          @click="selectedTechId = tech.id"
        >
          <div class="tech-name">
            {{ tech.name }}
            <el-tag size="small" :type="tech.source === 2 ? 'success' : 'info'">
              {{ tech.source === 2 ? '平台' : '商家' }}
            </el-tag>
          </div>
          <div class="tech-tags">{{ (tech.skillCategoryNames && tech.skillCategoryNames.length > 0) ? tech.skillCategoryNames.join('、') : '通用技师' }}</div>
        </div>
        <div v-if="technicianList.length === 0" class="empty-tech">暂无可用技师</div>
      </div>
      <template #footer>
        <el-button @click="techDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="confirmSelectTech">确认</el-button>
      </template>
    </el-dialog>

    <!-- 疗程卡核销弹窗 -->
    <el-dialog v-model="verifyDialogVisible" title="疗程卡核销" width="600px" :close-on-click-modal="false">
      <div class="verify-section">
        <div class="verify-step">
          <div class="step-label">选择会员（输入手机号搜索）</div>
          <div class="verify-member-bar">
            <el-input v-model="verifyPhone" placeholder="会员手机号" @keyup.enter="loadCustomerCards" />
            <el-button type="primary" @click="loadCustomerCards">查询疗程卡</el-button>
          </div>
        </div>
        <div v-if="cardList.length > 0" class="verify-step">
          <div class="step-label">选择疗程卡</div>
          <el-select v-model="selectedCardSaleId" placeholder="选择有效疗程卡" style="width: 100%" @change="onCardSelected">
            <el-option
              v-for="card in cardList"
              :key="card.id"
              :label="`卡ID:${card.id} 剩余${card.remainingTimes}次 到期:${card.expiryDate.slice(0,10)}`"
              :value="card.id"
            />
          </el-select>
        </div>
        <div v-if="selectedCard && selectedCard.items.length > 0" class="verify-step">
          <div class="step-label-row">
            <span class="step-label">核销项目</span>
            <el-button link type="primary" :disabled="verifyItems.length >= selectedCard.items.length" @click="addVerifyItem">
              + 添加项目
            </el-button>
          </div>
          <div class="verify-items-table">
            <div class="verify-items-header">
              <div class="col-product">服务项目</div>
              <div class="col-times">次数</div>
              <div class="col-amount">金额</div>
              <div class="col-action">操作</div>
            </div>
            <div v-for="(row, idx) in verifyItems" :key="idx" class="verify-items-row">
              <div class="col-product">
                <el-select v-model="row.productId" placeholder="选择项目" style="width: 100%" :disabled="selectedCard!.items.length === 1">
                  <el-option
                    v-for="item in selectedCard!.items"
                    :key="item.productId"
                    :label="`项目ID:${item.productId} 折算价:¥${item.allocatedUnitPrice}`"
                    :value="item.productId"
                    :disabled="isProductSelected(item.productId, idx)"
                  />
                </el-select>
              </div>
              <div class="col-times">
                <el-input-number v-model="row.verifyTimes" :min="1" :max="maxTimesForRow(idx)" size="small" style="width: 100%" />
              </div>
              <div class="col-amount">¥{{ formatPrice(rowSubAmount(idx)) }}</div>
              <div class="col-action">
                <el-button link size="small" type="danger" :disabled="verifyItems.length === 1" @click="removeVerifyItem(idx)">删除</el-button>
              </div>
            </div>
            <div class="verify-items-footer">
              <span>合计：<b>¥{{ formatPrice(totalAmount) }}</b></span>
              <span>共 <b>{{ totalTimes }}</b> 次</span>
            </div>
          </div>
        </div>
      </div>
      <template #footer>
        <el-button @click="verifyDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="verifying" :disabled="!canConfirmVerify" @click="confirmVerifyCard">确认核销</el-button>
      </template>
    </el-dialog>

    <!-- 预约转订单弹窗 -->
    <el-dialog v-model="appointmentDialogVisible" title="预约转单" width="600px" :close-on-click-modal="false">
      <div class="appointment-list">
        <div
          v-for="appt in todayAppointments"
          :key="appt.id"
          class="appointment-item"
          :class="{ selected: selectedAppointmentId === appt.id }"
          @click="selectedAppointmentId = appt.id"
        >
          <div class="appt-info">
            <div class="appt-customer">{{ appt.customerName || ('客户ID:' + appt.customerId) }}</div>
            <div class="appt-detail">{{ appt.appointmentDate }} {{ appt.appointmentTime }}</div>
          </div>
          <div class="appt-product">{{ appt.productName || '未指定项目' }}</div>
        </div>
        <div v-if="todayAppointments.length === 0" class="empty-tech">今日无待到店预约</div>
      </div>
      <template #footer>
        <el-button @click="appointmentDialogVisible = false">取消</el-button>
        <el-button type="primary" :disabled="!selectedAppointmentId" @click="confirmAppointmentToOrder">转入购物车</el-button>
      </template>
    </el-dialog>

    <!-- 效期选择弹窗（实物商品/赠品共用：赠品也走与商品相同的 FEFO+FIFO 批次扣减逻辑） -->
    <el-dialog v-model="expiryDialogVisible" :title="expiryDialogTitle" width="520px" :close-on-click-modal="false" class="expiry-dialog">
      <div v-if="pendingProduct" class="expiry-content">
        <!-- 商品信息 + 数量输入 -->
        <div class="expiry-product-bar">
          <span class="expiry-product-name">
            {{ pendingProduct.name }}
            <el-tag v-if="pendingProduct.productType === 5" size="small" type="warning" effect="plain">赠品</el-tag>
          </span>
          <div class="expiry-qty-control">
            <span>数量：</span>
            <button class="qty-btn" @click="expiryQuantity = Math.max(1, expiryQuantity - 1)">−</button>
            <input class="expiry-qty-input" v-model.number="expiryQuantity" type="number" min="1" />
            <button class="qty-btn" @click="expiryQuantity++">+</button>
          </div>
        </div>

        <!-- 效期选项列表 -->
        <div class="expiry-options-list">
          <div
            v-for="option in expiryOptions"
            :key="option.expirationDate ?? 'no-expiry'"
            class="expiry-option"
            :class="{ selected: selectedExpirationDates.includes(option.expirationDate) }"
            @click="toggleExpirySelection(option.expirationDate)"
          >
            <div class="expiry-option-date">
              {{ option.isNoExpiry ? '无效期限制' : option.expirationDate!.slice(0, 10) }}
              <el-tag v-if="option.isRecommended" size="small" type="success">推荐</el-tag>
              <el-tag v-if="!option.isNoExpiry && option.remainingDays! < 0" size="small" type="danger">已过期</el-tag>
              <el-tag v-else-if="!option.isNoExpiry && option.remainingDays! <= 30" size="small" type="warning">临期</el-tag>
            </div>
            <div class="expiry-option-info">
              <span>库存: {{ option.totalQuantity }} 件</span>
              <span v-if="option.isNoExpiry">剩余: 无效期</span>
              <span v-else>剩余: {{ option.remainingDays }} 天</span>
            </div>
          </div>
        </div>

        <!-- 已选效期列表（按扣减顺序） -->
        <div v-if="!useAutoRecommend && selectedExpirationDates.length > 0" class="selected-expiry-section">
          <div class="selected-title">已选（按扣减顺序）：</div>
          <div v-for="(date, idx) in selectedExpirationDates" :key="date ?? 'no-expiry'" class="selected-item">
            <span>{{ idx + 1 }}. {{ date ? date.slice(0, 10) : '无效期限制' }}</span>
            <el-button link size="small" type="danger" @click="removeExpirySelection(idx)">移除</el-button>
          </div>
          <div class="selected-summary">
            已选总和: {{ selectedExpiryTotal }} 件 / 需要 {{ expiryQuantity }} 件
            <el-tag v-if="selectedExpiryTotal < expiryQuantity" size="small" type="danger">不足</el-tag>
            <el-tag v-else size="small" type="success">充足</el-tag>
          </div>
        </div>

        <!-- 系统自动推荐选项（赠品场景下隐藏：赠品必须显式选效期，防止误扣非预期批次） -->
        <div v-if="pendingProduct.productType !== 5" class="auto-recommend-section">
          <el-checkbox v-model="useAutoRecommend" @change="selectedExpirationDates = []">系统自动推荐（按近效期优先扣减）</el-checkbox>
        </div>
        <div v-else class="auto-recommend-section">
          <el-alert
            title="赠品必须显式选择效期批次"
            type="info"
            :closable="false"
            show-icon
          />
        </div>
      </div>
      <template #footer>
        <el-button @click="expiryDialogVisible = false">取消</el-button>
        <el-button type="primary" :disabled="!canConfirmExpiry" @click="confirmExpirySelection">确认</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Aim, Box, Close, ShoppingCart, Wallet, ChatDotRound, CreditCard, Money, Coin } from '@element-plus/icons-vue'
import { getProducts, getCategoryTree } from '@/api/product'
import { getCustomers, getCustomerLevels, getPointsRule, type CustomerLevel } from '@/api/customer'
import { getMemberAccounts } from '@/api/member'
import { createOrder, type OrderCreate } from '@/api/order'
import { getTechnicians, type Technician } from '@/api/technician'
import { getTreatmentCardSales, verifyTreatmentCard, type TreatmentCardSale, type TreatmentCardVerifyItemInput } from '@/api/treatment-card'
import { getAppointments, type Appointment } from '@/api/appointment'
import { getProductExpiryOptions, type ProductExpiryOption } from '@/api/inventory'

// ==================== 商品数据 ====================
interface POSProduct {
  id: number
  name: string
  code: string
  spec: string
  price: number
  stockText: string
  categoryId: number
  productType: number
  colorClass: string
}

// 分类列表（从 API 加载）
const categories = ref<Array<{ id: number; name: string }>>([{ id: 0, name: '全部' }])
const activeCategory = ref(0)
const searchKeyword = ref('')
const allProducts = ref<POSProduct[]>([])
// 赠品列表（Product 表 type=5 的记录，POS 允许随订单一起提交，后端 DeductSampleGiftOutAsync 按效期扣减批次库存）
const allGifts = ref<POSProduct[]>([])
// 商品/赠品 切换模式：'product'=商品列表，'gift'=赠品列表
const productMode = ref<'product' | 'gift'>('product')

// 加载商品分类
const loadCategories = async () => {
  try {
    const tree = await getCategoryTree()
    categories.value = [{ id: 0, name: '全部' }, ...tree.map(c => ({ id: c.id, name: c.name }))]
  } catch (e: any) {
    ElMessage.error('加载分类失败: ' + e.message)
  }
}

// 加载商品列表（POS 仅展示可销售品项，排除样品/赠品 Type=4/5；后端 OrderAppService 亦校验 IsSalable 兜底）
const loadProducts = async () => {
  try {
    const res = await getProducts({ status: 1, pageIndex: 1, pageSize: 1000 })
    const colorClasses = ['cyan', 'green', 'blue', 'orange']
    allProducts.value = res.list
      .filter(p => p.type !== 4 && p.type !== 5)
      .map((p, idx) => ({
        id: p.id,
        name: p.name,
        code: p.code,
        spec: p.spec || '',
        price: p.price,
        stockText: p.type === 1 ? '实物商品' : '服务项目',
        categoryId: p.categoryId,
        productType: p.type,
        colorClass: colorClasses[idx % colorClasses.length]
      }))
  } catch (e: any) {
    ElMessage.error('加载商品失败: ' + e.message)
  }
}

// 加载赠品列表（Type=5 商品，POS 作为下单时的可选赠品来源，与商品共用 Product API）
const loadGifts = async () => {
  try {
    const res = await getProducts({ status: 1, type: 5, pageIndex: 1, pageSize: 1000 })
    const colorClasses = ['orange', 'green', 'cyan', 'blue']
    allGifts.value = res.list
      .filter(p => p.type === 5)
      .map((p, idx) => ({
        id: p.id,
        name: p.name,
        code: p.code,
        spec: p.spec || '',
        // 赠品对客户免费，POS 展示不显示价格；加入购物车时强制置 0
        price: 0,
        stockText: p.unit ? `单位: ${p.unit}` : '赠品',
        categoryId: p.categoryId,
        productType: p.type,
        colorClass: colorClasses[idx % colorClasses.length]
      }))
  } catch (e: any) {
    ElMessage.error('加载赠品失败: ' + e.message)
  }
}

// 切换商品/赠品 模式
const switchProductMode = (mode: 'product' | 'gift') => {
  productMode.value = mode
  // 切换时清空搜索关键字，避免另一个列表的过滤条件残留
  searchKeyword.value = ''
}

// 过滤后的商品/赠品列表（根据当前模式返回不同数据源）
const filteredProducts = computed(() => {
  const source = productMode.value === 'gift' ? allGifts.value : allProducts.value
  let result = source
  if (productMode.value === 'product' && activeCategory.value !== 0) {
    result = result.filter(p => p.categoryId === activeCategory.value)
  }
  if (searchKeyword.value.trim()) {
    const keyword = searchKeyword.value.trim().toLowerCase()
    result = result.filter(p =>
      p.name.toLowerCase().includes(keyword) ||
      p.spec.toLowerCase().includes(keyword)
    )
  }
  return result
})

const handleSearch = () => {
  // 搜索通过 computed 自动触发
}

const handleScan = () => {
  ElMessage.info('扫码功能待对接硬件设备')
}

const handleCategoryChange = (catId: number) => {
  activeCategory.value = catId
}

// ==================== 客户等级缓存 ====================
const customerLevels = ref<CustomerLevel[]>([])

const loadCustomerLevels = async () => {
  try {
    customerLevels.value = await getCustomerLevels()
  } catch (e: any) {
    // 静默失败，不影响主流程
    console.warn('加载客户等级失败:', e.message)
  }
}

// 根据等级ID获取折扣率（POS 内部用 0-10 整数表示，9 = 9折）
const getDiscountRateByLevelId = (levelId?: number): number => {
  if (!levelId) return 10
  const level = customerLevels.value.find(l => l.id === levelId)
  if (!level) return 10
  // CustomerLevel.discountRate 用小数表示（0.9 = 9折），转换为整数
  return Math.round(level.discountRate * 10)
}

// ==================== 会员 ====================
interface Member {
  id: number
  name: string
  phone: string
  levelName: string
  balance: number
  discountRate: number  // 0-10 整数（9 = 9折）
  totalPoints: number
}

const memberKeyword = ref('')
const selectedMember = ref<Member | null>(null)

const handleMemberSearch = async () => {
  const keyword = memberKeyword.value.trim()
  if (!keyword) {
    ElMessage.warning('请输入会员手机号或卡号')
    return
  }
  try {
    const res = await getCustomers({ phone: keyword, pageIndex: 1, pageSize: 10 })
    if (res.list.length === 0) {
      ElMessage.warning('未找到匹配的会员')
      return
    }
    const customer = res.list[0]
    // 查询储值账户余额
    let balance = 0
    try {
      const accounts = await getMemberAccounts({ customerId: customer.id, pageIndex: 1, pageSize: 1 })
      if (accounts.list.length > 0) {
        balance = accounts.list[0].balance
      }
    } catch (e) {
      // 储值账户查询失败不阻塞
    }
    selectedMember.value = {
      id: customer.id,
      name: customer.name,
      phone: customer.phone,
      levelName: customer.levelName || '普通会员',
      balance,
      discountRate: getDiscountRateByLevelId(customer.levelId),
      totalPoints: customer.totalPoints
    }
    memberKeyword.value = ''
    ElMessage.success('会员查询成功')
  } catch (e: any) {
    ElMessage.error('会员查询失败: ' + e.message)
  }
}

const clearMember = () => {
  selectedMember.value = null
}

// ==================== 购物车 ====================
interface CartItem {
  id: number
  name: string
  code: string
  spec: string
  price: number
  quantity: number
  productType: number  // 1=实物商品，2=服务项目
  technicianId?: number
  technicianName?: string
  technicianSource?: 1 | 2  // 1=商家技师，2=平台技师
  /** 房间/床位ID（服务订单占用房间资源，预约转单时由 Appointment.RoomId 复制） */
  roomId?: number
  /** 房间/床位名称（前端展示用） */
  roomName?: string
  /** 设备ID（服务订单占用设备资源，预约转单时由 Appointment.EquipmentId 复制） */
  equipmentId?: number
  /** 设备名称（前端展示用） */
  equipmentName?: string
  /** 源预约ID（预约转订单时记录，后端 OrderAppService 据此复制技师/房间/设备到 OrderItem） */
  sourceAppointmentId?: number
  expirationDates?: (string | null)[]  // 店员选择的效期列表（按扣减顺序），undefined 表示系统自动推荐；null 元素表示"无效期限制"批次
}

const cart = ref<CartItem[]>([])

const addToCart = (product: POSProduct) => {
  const existing = cart.value.find(item => item.id === product.id)
  if (existing) {
    existing.quantity++
    return
  }

  // 实物商品（type=1）和赠品（type=5）都需要先选择效期批次
  // 赠品走与实物商品相同的 FEFO+FIFO 批次扣减逻辑，后端 DeductSampleGiftOutAsync 按效期扣减
  if (product.productType === 1 || product.productType === 5) {
    openExpiryDialog(product)
    return
  }

  // 服务项目直接加入购物车
  cart.value.push({
    id: product.id,
    name: product.name,
    code: product.code,
    spec: product.spec,
    price: product.price,
    quantity: 1,
    productType: product.productType
  })
}

const changeQty = (item: CartItem, delta: number) => {
  item.quantity += delta
  if (item.quantity <= 0) {
    removeFromCart(item)
  }
}

const onQtyInput = (item: CartItem) => {
  if (!item.quantity || item.quantity < 1) {
    item.quantity = 1
  }
}

const removeFromCart = (item: CartItem) => {
  const index = cart.value.findIndex(i => i.id === item.id)
  if (index > -1) {
    cart.value.splice(index, 1)
  }
}

const handleClearCart = () => {
  if (cart.value.length === 0) return
  ElMessageBox.confirm('确定要清空购物车吗？', '提示', {
    type: 'warning',
    confirmButtonText: '确定',
    cancelButtonText: '取消'
  }).then(() => {
    cart.value = []
    ElMessage.success('已清空')
  }).catch(() => {})
}

const handleHoldOrder = () => {
  if (cart.value.length === 0) {
    ElMessage.warning('购物车为空')
    return
  }
  // TODO: 后端挂单接口就绪后对接
  ElMessage.info('挂单功能待后端接口支持')
}

const handleResumeOrder = () => {
  // TODO: 后端取单接口就绪后对接
  ElMessage.info('取单功能待后端接口支持')
}

// ==================== 效期选择弹窗 ====================
const expiryDialogVisible = ref(false)
const pendingProduct = ref<POSProduct | null>(null)
const expiryOptions = ref<ProductExpiryOption[]>([])
// null 表示"无效期限制"批次，与 ProductExpiryOption.expirationDate 对齐
const selectedExpirationDates = ref<(string | null)[]>([])
const useAutoRecommend = ref(false)
const expiryQuantity = ref(1)

// 弹窗标题：根据待选商品类型区分（赠品场景提示更明确）
const expiryDialogTitle = computed(() => {
  if (pendingProduct.value?.productType === 5) return '赠品效期选择'
  return '效期选择'
})

// 已选效期总库存
const selectedExpiryTotal = computed(() => {
  return selectedExpirationDates.value.reduce((sum, date) => {
    // null 表示"无效期限制"批次，匹配 isNoExpiry 标识；非 null 按 ISO 日期前缀匹配
    const option = date === null
      ? expiryOptions.value.find(o => o.isNoExpiry === true)
      : expiryOptions.value.find(o => !o.isNoExpiry && o.expirationDate!.startsWith(date.slice(0, 10)))
    return sum + (option?.totalQuantity || 0)
  }, 0)
})

// 是否可以确认效期选择
const canConfirmExpiry = computed(() => {
  if (!pendingProduct.value) return false
  if (expiryQuantity.value < 1) return false
  // 赠品必须显式选效期，不允许"系统自动推荐"（防止误扣非预期批次）
  const isGift = pendingProduct.value.productType === 5
  if (isGift) {
    if (selectedExpirationDates.value.length === 0) return false
    return selectedExpiryTotal.value >= expiryQuantity.value
  }
  if (useAutoRecommend.value) return true
  if (selectedExpirationDates.value.length === 0) return false
  return selectedExpiryTotal.value >= expiryQuantity.value
})

const openExpiryDialog = async (product: POSProduct) => {
  pendingProduct.value = product
  selectedExpirationDates.value = []
  useAutoRecommend.value = false
  expiryQuantity.value = 1
  expiryOptions.value = []

  try {
    const options = await getProductExpiryOptions(product.id)
    expiryOptions.value = options
    if (options.length === 0) {
      ElMessage.warning(`商品 ${product.name} 暂无可用效期库存`)
      pendingProduct.value = null
      return
    }
    // 默认选中推荐项
    const recommended = options.find(o => o.isRecommended)
    if (recommended) {
      selectedExpirationDates.value = [recommended.expirationDate]
    }
  } catch (e: any) {
    ElMessage.error('加载效期信息失败: ' + e.message)
    pendingProduct.value = null
    return
  }

  expiryDialogVisible.value = true
}

const toggleExpirySelection = (date: string | null) => {
  useAutoRecommend.value = false
  // 严格相等比较，避免 null 与 "" 误匹配
  const idx = selectedExpirationDates.value.findIndex(d => d === date)
  if (idx > -1) {
    selectedExpirationDates.value.splice(idx, 1)
  } else {
    selectedExpirationDates.value.push(date)
  }
}

const removeExpirySelection = (idx: number) => {
  selectedExpirationDates.value.splice(idx, 1)
}

const confirmExpirySelection = () => {
  if (!pendingProduct.value) return
  if (!canConfirmExpiry.value) return

  const isGift = pendingProduct.value.productType === 5
  // 赠品必须显式选效期（不允许自动推荐），避免后端扣减非预期批次
  const expirationDates = isGift
    ? [...selectedExpirationDates.value]
    : (useAutoRecommend.value ? undefined : [...selectedExpirationDates.value])

  cart.value.push({
    id: pendingProduct.value.id,
    name: pendingProduct.value.name,
    code: pendingProduct.value.code,
    spec: pendingProduct.value.spec,
    // 赠品对客户免费，前端强制 price=0；提交订单时 discountedAmount 自然也是 0
    price: isGift ? 0 : pendingProduct.value.price,
    quantity: expiryQuantity.value,
    productType: pendingProduct.value.productType,
    expirationDates: expirationDates
  })

  expiryDialogVisible.value = false
  pendingProduct.value = null
}

// ==================== 合计计算 ====================
const totalQuantity = computed(() => {
  return cart.value.reduce((sum, item) => sum + item.quantity, 0)
})

// 购物车中是否存在无效赠品行（赠品必须选效期才允许提交）
// 用于禁用"结算收银"按钮：赠品未选效期时阻止提交，避免后端 DeductSampleGiftOutAsync 找不到批次
const hasInvalidGiftInCart = computed(() => {
  return cart.value.some(item =>
    item.productType === 5 &&
    (!item.expirationDates || item.expirationDates.length === 0)
  )
})

const subtotal = computed(() => {
  return cart.value.reduce((sum, item) => sum + item.price * item.quantity, 0)
})

const discountAmount = computed(() => {
  if (!selectedMember.value) return 0
  const rate = selectedMember.value.discountRate / 10
  return subtotal.value * (1 - rate)
})

const payableAmount = computed(() => {
  return subtotal.value - discountAmount.value
})

// ==================== 订单信息 ====================
const orderNo = ref('')
const currentTime = ref('')

const generateOrderNo = () => {
  const now = new Date()
  const date = `${now.getFullYear()}${String(now.getMonth() + 1).padStart(2, '0')}${String(now.getDate()).padStart(2, '0')}`
  const random = String(Math.floor(Math.random() * 900) + 100)
  orderNo.value = `${date}-${random}`
}

const updateTime = () => {
  const now = new Date()
  currentTime.value = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`
}

let timeTimer: ReturnType<typeof setInterval> | null = null

// ==================== 支付弹窗 ====================
const payDialogVisible = ref(false)
const selectedPayMethod = ref('balance')
const paying = ref(false)
const isBackfill = ref(false)
const backfillDate = ref<string>('')

// 支付方式映射：前端 ID -> 后端 PayMethod 编号
const PAY_METHOD_MAP: Record<string, number> = {
  balance: 5,    // 储值卡
  wechat: 3,     // 微信
  alipay: 2,     // 支付宝
  cash: 1,       // 现金
  bank: 4,       // 银行卡
  points: 6,     // 积分抵扣
  combined: 7    // 组合支付
}

const payMethods = [
  { id: 'balance', name: '会员储值', icon: Wallet, color: '#06d4e4' },
  { id: 'wechat', name: '微信支付', icon: ChatDotRound, color: '#10b981' },
  { id: 'alipay', name: '支付宝', icon: CreditCard, color: '#5b9bff' },
  { id: 'cash', name: '现金支付', icon: Money, color: '#fbbf24' },
  { id: 'bank', name: '银行卡', icon: CreditCard, color: '#8b5cf6' },
  { id: 'points', name: '积分抵扣', icon: Coin, color: '#f97316' },
  { id: 'combined', name: '组合支付', icon: Wallet, color: '#ec4899' }
]

// ==================== 组合支付状态 ====================
const isCombinedPay = ref(false)
const cashAmount = ref(0)
const cashPayMethod = ref<1 | 2 | 3 | 4>(1)
const storedValueAmount = ref(0)
const pointsAmount = ref(0)
// 积分规则（用于组合支付类别3积分抵扣计算）
const comboPointsRule = ref<{ pointsToYuan: number } | null>(null)

// 组合支付合计
const combinedTotal = computed(() => {
  return Math.round((cashAmount.value + storedValueAmount.value + pointsAmount.value) * 100) / 100
})

// 积分最大可抵扣金额 = min(会员积分 × 抵扣率, 应收金额)
const maxPointsDeduct = computed(() => {
  if (!selectedMember.value || !comboPointsRule.value) return 0
  const rate = comboPointsRule.value.pointsToYuan || 0
  if (rate <= 0) return 0
  const byPoints = selectedMember.value.totalPoints * rate
  return Math.round(Math.min(byPoints, payableAmount.value) * 100) / 100
})

// 组合支付是否可提交：三栏总和 = 应收金额，且至少一项>0
const canConfirmCombinedPay = computed(() => {
  if (!isCombinedPay.value) return true
  if (combinedTotal.value <= 0) return false
  return Math.abs(combinedTotal.value - payableAmount.value) < 0.01
})

// 切换支付方式
const onSelectPayMethod = (id: string) => {
  selectedPayMethod.value = id
  isCombinedPay.value = (id === 'combined')
  if (isCombinedPay.value) {
    // 进入组合支付模式时初始化类别1为应收金额，其余为0
    cashAmount.value = Math.round(payableAmount.value * 100) / 100
    cashPayMethod.value = 1
    storedValueAmount.value = 0
    pointsAmount.value = 0
  }
}

// 类别1金额变化：自动计算类别2（储值补足），不足部分计算类别3（积分）
const onCashAmountChange = () => {
  if (!isCombinedPay.value) return
  const remaining = Math.round((payableAmount.value - cashAmount.value) * 100) / 100
  if (remaining <= 0) {
    storedValueAmount.value = 0
    pointsAmount.value = 0
    // 类别1超过应收时截断
    if (cashAmount.value > payableAmount.value) {
      cashAmount.value = Math.round(payableAmount.value * 100) / 100
    }
    return
  }
  // 类别2：储值余额补足（不足取实际余额）
  const svAvailable = selectedMember.value?.balance || 0
  const sv = Math.round(Math.min(svAvailable, remaining) * 100) / 100
  storedValueAmount.value = sv
  // 类别3：差额转入积分
  const afterSv = Math.round((remaining - sv) * 100) / 100
  pointsAmount.value = Math.min(afterSv, maxPointsDeduct.value)
}

// 类别2储值变化：差额转入积分
const onStoredValueChange = () => {
  if (!isCombinedPay.value) return
  const svAvailable = selectedMember.value?.balance || 0
  if (storedValueAmount.value > svAvailable) {
    storedValueAmount.value = Math.round(svAvailable * 100) / 100
  }
  const remaining = Math.round((payableAmount.value - cashAmount.value - storedValueAmount.value) * 100) / 100
  if (remaining <= 0) {
    pointsAmount.value = 0
    return
  }
  pointsAmount.value = Math.min(remaining, maxPointsDeduct.value)
}

// 类别3积分变化：仅校验上限，不联动其他栏（店员手动调整）
const onPointsChange = () => {
  if (!isCombinedPay.value) return
  if (pointsAmount.value > maxPointsDeduct.value) {
    pointsAmount.value = maxPointsDeduct.value
  }
}

const openPayDialog = async () => {
  if (cart.value.length === 0) return
  // 重置组合支付状态
  isCombinedPay.value = false
  cashAmount.value = 0
  cashPayMethod.value = 1
  storedValueAmount.value = 0
  pointsAmount.value = 0
  // 加载积分规则（用于组合支付积分抵扣计算）
  if (!comboPointsRule.value) {
    try {
      const rule = await getPointsRule()
      comboPointsRule.value = { pointsToYuan: rule.pointsToYuan }
    } catch (e) {
      // 积分规则加载失败不阻塞，组合支付积分栏将不可用
      comboPointsRule.value = { pointsToYuan: 0 }
    }
  }
  payDialogVisible.value = true
}

const handleConfirmPay = async () => {
  // 组合支付校验
  if (isCombinedPay.value) {
    if (!selectedMember.value) {
      ElMessage.warning('组合支付需先选择会员（储值/积分扣减依赖会员账户）')
      return
    }
    if (!canConfirmCombinedPay.value) {
      ElMessage.warning(`三栏合计 ¥${formatPrice(combinedTotal.value)} 与应收金额不符`)
      return
    }
    if (cashAmount.value > 0 && !cashPayMethod.value) {
      ElMessage.warning('类别1金额>0时请选择具体支付方式')
      return
    }
    if (storedValueAmount.value > selectedMember.value.balance) {
      ElMessage.warning('储值余额不足')
      return
    }
    if (pointsAmount.value > maxPointsDeduct.value) {
      ElMessage.warning('积分抵扣金额超过可用上限')
      return
    }
  }
  // 储值支付校验余额
  if (selectedPayMethod.value === 'balance') {
    if (!selectedMember.value) {
      ElMessage.warning('储值支付需先选择会员')
      return
    }
    if (selectedMember.value.balance < payableAmount.value) {
      ElMessage.warning('会员储值余额不足')
      return
    }
  }
  // 积分抵扣校验积分
  if (selectedPayMethod.value === 'points') {
    if (!selectedMember.value) {
      ElMessage.warning('积分抵扣需先选择会员')
      return
    }
    if (selectedMember.value.totalPoints <= 0) {
      ElMessage.warning('会员积分不足')
      return
    }
  }

  const payMethodName = payMethods.find(m => m.id === selectedPayMethod.value)?.name
  try {
    await ElMessageBox.confirm(
      `确认通过「${payMethodName}」收款 ¥${formatPrice(payableAmount.value)}？`,
      '确认收款',
      { type: 'info', confirmButtonText: '确认', cancelButtonText: '取消' }
    )
  } catch (e) {
    return  // 用户取消
  }

  paying.value = true

  // 构建订单 payload（在 try 外声明，以便 catch 块中 409 重试时访问）
  const hasService = cart.value.some(i => i.productType === 2)
  const orderType = hasService ? 2 : 1
  const memberRate = selectedMember.value?.discountRate
    ? selectedMember.value.discountRate / 10
    : 1

  const now = new Date()
  const orderTime = isBackfill.value && backfillDate.value
    ? backfillDate.value
    : now.toISOString()

  // 组合支付字段：仅 PayMethod=7 时填充
  const isCombined = isCombinedPay.value
  const payload: OrderCreate = {
    orderNo: orderNo.value,
    customerId: selectedMember.value?.id,
    orderType: orderType as 1 | 2,
    status: 2,  // 已完成（POS 收银即完成）
    backfillStatus: isBackfill.value ? 1 : 0,
    productAmount: subtotal.value,
    discountAmount: discountAmount.value,
    paidAmount: payableAmount.value,
    payMethod: (isCombined ? 7 : PAY_METHOD_MAP[selectedPayMethod.value]) as 1 | 2 | 3 | 4 | 5 | 6 | 7,
    cashAmount: isCombined ? cashAmount.value : undefined,
    cashPayMethod: isCombined && cashAmount.value > 0 ? cashPayMethod.value : undefined,
    storedValueAmount: isCombined ? storedValueAmount.value : undefined,
    pointsAmount: isCombined ? pointsAmount.value : undefined,
    orderTime,
    completeTime: orderTime,
    items: cart.value.map(item => ({
      productId: item.id,
      productName: item.name,
      productCode: item.code,
      technicianId: item.technicianId,
      technicianSource: item.technicianSource,
      roomId: item.roomId,
      equipmentId: item.equipmentId,
      quantity: item.quantity,
      price: item.price,
      discountRate: memberRate,
      // 赠品 price=0，乘以任何折扣率仍为 0，自然满足"赠品行 DiscountedAmount 自动为 0"
      discountedAmount: item.price * item.quantity * memberRate,
      // 实物商品（type=1）和赠品（type=5）均需传效期列表；服务项目（type=2）不需要
      // 赠品走与零售商品相同的批次扣减逻辑（FEFO+FIFO），后端 DeductSampleGiftOutAsync 按效期扣减
      expirationDates: (item.productType === 1 || item.productType === 5) ? item.expirationDates : undefined,
      allowAutoFillBeyondSelection: false
    })),
    // 预约转订单：取第一个有 sourceAppointmentId 的明细对应的预约ID（预约一次只生成一个订单）
    sourceAppointmentId: cart.value.find(i => i.sourceAppointmentId)?.sourceAppointmentId
  }

  try {
    await createOrder(payload)
    ElMessage.success('收款成功')
    payDialogVisible.value = false
    // 重置订单
    cart.value = []
    selectedMember.value = null
    isBackfill.value = false
    backfillDate.value = ''
    // 重置组合支付状态
    isCombinedPay.value = false
    selectedPayMethod.value = 'balance'
    cashAmount.value = 0
    cashPayMethod.value = 1
    storedValueAmount.value = 0
    pointsAmount.value = 0
    generateOrderNo()
  } catch (e: any) {
    // 409: 选中效期库存不足（并发冲突），弹窗让店员决定是否自动从近效期补足
    if (e.code === 409 && e.message?.startsWith('INSUFFICIENT_EXPIRY_STOCK')) {
      await handleInsufficientExpiryStock(e.message, payload)
      return
    }
    ElMessage.error('下单失败: ' + e.message)
  } finally {
    paying.value = false
  }
}

// ==================== 效期不足 409 处理 ====================
const handleInsufficientExpiryStock = async (errorMessage: string, originalPayload: OrderCreate) => {
  // 解析错误消息: INSUFFICIENT_EXPIRY_STOCK|{productId}|{productName}|{shortfall}|{expirationDate1:qty1;expirationDate2:qty2;...}
  const parts = errorMessage.split('|')
  if (parts.length < 5) {
    ElMessage.error('效期库存不足，请重新选择效期')
    return
  }

  const productName = parts[2]
  const shortfall = parts[3]
  const optionsStr = parts[4]

  try {
    await ElMessageBox.confirm(
      `商品「${productName}」选择的效期库存不足（缺 ${shortfall} 件），是否自动从近效期补足？`,
      '效期库存不足',
      {
        type: 'warning',
        confirmButtonText: '自动补足',
        cancelButtonText: '重新选择效期'
      }
    )
    // 用户确认：重试，设置 allowAutoFillBeyondSelection = true
    originalPayload.items.forEach(it => {
      it.allowAutoFillBeyondSelection = true
    })
    paying.value = true
    try {
      await createOrder(originalPayload)
      ElMessage.success('收款成功')
      payDialogVisible.value = false
      cart.value = []
      selectedMember.value = null
      isBackfill.value = false
      backfillDate.value = ''
      // 重置组合支付状态
      isCombinedPay.value = false
      selectedPayMethod.value = 'balance'
      cashAmount.value = 0
      cashPayMethod.value = 1
      storedValueAmount.value = 0
      pointsAmount.value = 0
      generateOrderNo()
    } catch (retryErr: any) {
      ElMessage.error('下单失败: ' + retryErr.message)
    }
  } catch {
    // 用户取消：提示重新选择效期
    ElMessage.info('请重新选择效期')
  }
}

// ==================== 技师选择弹窗 ====================
const techDialogVisible = ref(false)
const technicianList = ref<Technician[]>([])
const selectedTechId = ref<number | undefined>(undefined)
const currentCartItem = ref<CartItem | null>(null)

const openTechDialog = async (item: CartItem) => {
  currentCartItem.value = item
  selectedTechId.value = item.technicianId
  try {
    const res = await getTechnicians({ status: 1, pageIndex: 1, pageSize: 1000 })
    technicianList.value = res.list
  } catch (e: any) {
    ElMessage.error('加载技师失败: ' + e.message)
    return
  }
  techDialogVisible.value = true
}

const confirmSelectTech = () => {
  if (!selectedTechId.value) {
    ElMessage.warning('请选择技师')
    return
  }
  const tech = technicianList.value.find(t => t.id === selectedTechId.value)
  if (currentCartItem.value && tech) {
    currentCartItem.value.technicianId = tech.id
    currentCartItem.value.technicianName = tech.name
    currentCartItem.value.technicianSource = tech.source
  }
  techDialogVisible.value = false
}

// ==================== 疗程卡核销弹窗 ====================
const verifyDialogVisible = ref(false)
const verifyPhone = ref('')
const cardList = ref<TreatmentCardSale[]>([])
const selectedCardSaleId = ref<number | undefined>(undefined)
const verifyItems = ref<TreatmentCardVerifyItemInput[]>([])
const verifying = ref(false)

const selectedCard = computed(() => {
  if (!selectedCardSaleId.value) return undefined
  return cardList.value.find(c => c.id === selectedCardSaleId.value)
})

// 默认添加一行（使用疗程卡第一个未选项目）
const addVerifyItem = () => {
  if (!selectedCard.value) return
  const used = new Set(verifyItems.value.map(i => i.productId).filter(p => p != null) as number[])
  const next = selectedCard.value.items.find(i => !used.has(i.productId))
  verifyItems.value.push({ productId: next?.productId, verifyTimes: 1 })
}

// 删除一行
const removeVerifyItem = (idx: number) => {
  verifyItems.value.splice(idx, 1)
}

// 检查某项目是否已被其他行选中（用于禁用下拉选项）
const isProductSelected = (productId: number, currentIdx: number): boolean => {
  return verifyItems.value.some((item, idx) => idx !== currentIdx && item.productId === productId)
}

// 单项金额 = 折算单价 × 次数（仅前端预估，最终以服务端为准）
const rowSubAmount = (idx: number): number => {
  const row = verifyItems.value[idx]
  if (!row || !selectedCard.value) return 0
  const saleItem = selectedCard.value.items.find(i => i.productId === row.productId)
  if (!saleItem) return 0
  return Number((Number(saleItem.allocatedUnitPrice) * (row.verifyTimes || 0)).toFixed(2))
}

const totalAmount = computed(() => {
  return Number(verifyItems.value.reduce((sum, _, idx) => sum + rowSubAmount(idx), 0).toFixed(2))
})

const totalTimes = computed(() => {
  return verifyItems.value.reduce((sum, row) => sum + (row.verifyTimes || 0), 0)
})

// 单行最大次数 = 剩余次数 - 其他行已占用次数
const maxTimesForRow = (idx: number): number => {
  if (!selectedCard.value) return 1
  const otherTimes = verifyItems.value.reduce((sum, row, i) => i === idx ? sum : sum + (row.verifyTimes || 0), 0)
  return Math.max(1, selectedCard.value.remainingTimes - otherTimes)
}

// 校验：所有行都已选项目 + 总次数不超过剩余次数
const canConfirmVerify = computed(() => {
  if (!selectedCardSaleId.value) return false
  if (verifyItems.value.length === 0) return false
  if (verifyItems.value.some(r => r.productId == null || r.verifyTimes < 1)) return false
  if (totalTimes.value > (selectedCard.value?.remainingTimes || 0)) return false
  return true
})

const handleVerifyCard = () => {
  verifyDialogVisible.value = true
  cardList.value = []
  selectedCardSaleId.value = undefined
  verifyItems.value = []
  verifyPhone.value = ''
}

const loadCustomerCards = async () => {
  const phone = verifyPhone.value.trim()
  if (!phone) {
    ElMessage.warning('请输入会员手机号')
    return
  }
  try {
    // 先按手机号查客户
    const custRes = await getCustomers({ phone, pageIndex: 1, pageSize: 1 })
    if (custRes.list.length === 0) {
      ElMessage.warning('未找到匹配的会员')
      return
    }
    const customerId = custRes.list[0].id
    // 查询该客户的有效疗程卡（status=1）
    const cardRes = await getTreatmentCardSales({ customerId, status: 1, pageSize: 1000 })
    if (cardRes.list.length === 0) {
      ElMessage.warning('该会员无有效疗程卡')
      return
    }
    cardList.value = cardRes.list
    ElMessage.success(`找到 ${cardRes.list.length} 张有效疗程卡`)
  } catch (e: any) {
    ElMessage.error('查询疗程卡失败: ' + e.message)
  }
}

const onCardSelected = () => {
  // 切换疗程卡时重置核销项目（默认添加一行）
  verifyItems.value = []
  addVerifyItem()
}

const confirmVerifyCard = async () => {
  const cardSaleId = selectedCardSaleId.value
  if (!cardSaleId) {
    ElMessage.warning('请选择疗程卡')
    return
  }
  if (!canConfirmVerify.value) {
    ElMessage.warning('核销项目不合法（项目未选、次数小于 1 或总次数超限）')
    return
  }
  verifying.value = true
  try {
    await verifyTreatmentCard({ cardSaleId, items: verifyItems.value })
    ElMessage.success('核销成功')
    verifyDialogVisible.value = false
    // 刷新疗程卡列表
    await loadCustomerCards()
  } catch (e: any) {
    ElMessage.error('核销失败: ' + e.message)
  } finally {
    verifying.value = false
  }
}

// ==================== 预约转订单弹窗 ====================
const appointmentDialogVisible = ref(false)
const todayAppointments = ref<Appointment[]>([])
const selectedAppointmentId = ref<number | undefined>(undefined)

const handleAppointmentToOrder = async () => {
  const today = new Date().toISOString().slice(0, 10)
  try {
    // 查询今日已预约状态（status=2）的预约
    const res = await getAppointments({
      appointmentDateStart: today,
      appointmentDateEnd: today,
      status: 2,
      pageIndex: 1,
      pageSize: 1000
    })
    todayAppointments.value = res.list
    appointmentDialogVisible.value = true
  } catch (e: any) {
    ElMessage.error('加载今日预约失败: ' + e.message)
  }
}

const confirmAppointmentToOrder = () => {
  const appt = todayAppointments.value.find(a => a.id === selectedAppointmentId.value)
  if (!appt) {
    ElMessage.warning('请选择预约')
    return
  }
  // 通过 productId 直接查找商品并加入购物车（替代原按 serviceItem 名称匹配）
  if (appt.productId) {
    const matched = allProducts.value.find(p => p.id === appt.productId)
    if (matched) {
      addToCart(matched)
      // 预约转订单：继承预约的技师/房间/设备到购物车，提交时再透传给 OrderItem
      // 后端 OrderAppService.CreateAsync 也会通过 sourceAppointmentId 复制（双保险）
      const cartItem = cart.value.find(i => i.id === matched.id)
      if (cartItem) {
        if (appt.technicianId) {
          cartItem.technicianId = appt.technicianId
          const tech = technicianList.value.find(t => t.id === appt.technicianId)
          if (tech) {
            cartItem.technicianName = tech.name
            cartItem.technicianSource = tech.source
          }
        }
        if (appt.roomId) {
          cartItem.roomId = appt.roomId
        }
        if (appt.equipmentId) {
          cartItem.equipmentId = appt.equipmentId
        }
        cartItem.sourceAppointmentId = appt.id
      }
      ElMessage.success(`已添加商品：${matched.name}`)
    } else {
      ElMessage.info(`未找到商品（ID: ${appt.productId}），请手动添加`)
    }
  } else {
    ElMessage.info('该预约未指定服务项目，请手动添加商品')
  }
  appointmentDialogVisible.value = false
}

// ==================== 工具方法 ====================
const formatPrice = (price: number) => {
  return price.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ',')
}

onMounted(async () => {
  generateOrderNo()
  updateTime()
  timeTimer = setInterval(updateTime, 60000)
  await Promise.all([
    loadCategories(),
    loadProducts(),
    loadGifts(),
    loadCustomerLevels()
  ])
})

onUnmounted(() => {
  if (timeTimer) clearInterval(timeTimer)
})
</script>

<style scoped>
.pos-page {
  width: 100%;
  height: 100%;
}

.pos-container {
  display: flex;
  gap: 16px;
  height: calc(100vh - 120px);
}

/* ========== 左侧商品选择区 ========== */
.product-section {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.product-header {
  display: flex;
  gap: 12px;
  margin-bottom: 12px;
}

.search-input {
  flex: 1;
}

.scan-btn {
  background: var(--bg-tertiary);
  border: 1px solid var(--border-primary);
  color: var(--primary);
}

.scan-btn:hover {
  border-color: var(--primary);
  box-shadow: 0 0 20px rgba(6, 212, 228, 0.3);
}

/* 分类Tab */
.category-tabs {
  display: flex;
  gap: 4px;
  background: var(--bg-tertiary);
  border: 1px solid var(--border-primary);
  border-radius: var(--radius-md);
  padding: 4px;
  margin-bottom: 12px;
  overflow-x: auto;
}

.cat-tab {
  padding: 6px 16px;
  font-size: 13px;
  color: var(--text-tertiary);
  cursor: pointer;
  border-radius: var(--radius-sm);
  transition: all 0.3s;
  white-space: nowrap;
}

.cat-tab:hover {
  color: var(--text-primary);
}

.cat-tab.active {
  background: var(--primary);
  color: var(--bg-primary);
  font-weight: 600;
}

/* 商品/赠品 切换 Tab */
.mode-tabs {
  display: flex;
  gap: 4px;
  background: var(--bg-tertiary);
  border: 1px solid var(--border-primary);
  border-radius: var(--radius-md);
  padding: 4px;
  margin-bottom: 12px;
}

.mode-tab {
  flex: 1;
  padding: 6px 16px;
  font-size: 13px;
  color: var(--text-tertiary);
  cursor: pointer;
  border-radius: var(--radius-sm);
  transition: all 0.3s;
  text-align: center;
  font-weight: 500;
}

.mode-tab:hover {
  color: var(--text-primary);
}

.mode-tab.active {
  background: var(--primary);
  color: var(--bg-primary);
  font-weight: 600;
}

/* 赠品卡片标识 */
.gift-card {
  border-color: rgba(251, 191, 36, 0.4);
}

.gift-card:hover {
  border-color: #fbbf24;
}

.gift-price-text {
  color: #f59e0b !important;
  font-size: 13px !important;
  font-weight: 600 !important;
}

/* 购物车赠品行 */
.cart-item.gift-item {
  background: rgba(251, 191, 36, 0.05);
}

.cart-item.gift-item:hover {
  background: rgba(251, 191, 36, 0.1);
}

.gift-tag {
  margin-left: 6px;
}

.expiry-missing {
  color: var(--danger) !important;
  font-weight: 500;
}

/* 商品网格 */
.product-grid {
  flex: 1;
  overflow-y: auto;
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
  gap: 12px;
  align-content: start;
  padding-right: 4px;
}

.product-card {
  background: var(--bg-tertiary);
  border: 1px solid var(--border-primary);
  border-radius: var(--radius-md);
  padding: 12px;
  cursor: pointer;
  transition: all 0.3s;
  display: flex;
  flex-direction: column;
  gap: 8px;
  position: relative;
  overflow: hidden;
}

.product-card::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 2px;
  background: linear-gradient(90deg, transparent, var(--primary), transparent);
  opacity: 0;
  transition: opacity 0.3s;
}

.product-card:hover {
  border-color: var(--primary);
  transform: translateY(-2px);
  box-shadow: var(--shadow-md);
}

.product-card:hover::before {
  opacity: 1;
}

.product-img {
  height: 80px;
  border-radius: var(--radius-sm);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 32px;
  color: var(--bg-primary);
  font-weight: 700;
}

.product-img.cyan {
  background: var(--gradient-primary);
}

.product-img.green {
  background: var(--gradient-green);
}

.product-img.blue {
  background: linear-gradient(135deg, #5b9bff, #3b82f6);
}

.product-img.orange {
  background: linear-gradient(135deg, #fbbf24, #f59e0b);
}

.product-name {
  font-size: 13px;
  color: var(--text-primary);
  font-weight: 500;
  line-height: 1.3;
}

.product-spec {
  font-size: 11px;
  color: var(--text-tertiary);
}

.product-price {
  font-size: 16px;
  color: var(--primary);
  font-weight: 700;
  font-family: 'JetBrains Mono', monospace;
}

.product-stock {
  font-size: 11px;
  color: var(--text-tertiary);
}

.empty-products {
  grid-column: 1 / -1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px 0;
  color: var(--text-tertiary);
  gap: 12px;
}

.empty-products .el-icon {
  font-size: 48px;
  opacity: 0.3;
}

/* ========== 右侧订单区 ========== */
.order-section {
  width: 380px;
  background: var(--bg-tertiary);
  border: 1px solid var(--border-primary);
  border-radius: var(--radius-lg);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  position: relative;
  flex-shrink: 0;
}

.order-section::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 2px;
  background: linear-gradient(90deg, transparent, var(--primary), transparent);
  opacity: 0;
  transition: opacity 0.3s;
}

.order-section:hover::before {
  opacity: 1;
}

.order-header {
  padding: 16px 20px;
  border-bottom: 1px solid var(--border-primary);
  position: relative;
}

.order-header::after {
  content: '';
  position: absolute;
  bottom: 0;
  left: 20px;
  width: 40px;
  height: 2px;
  background: var(--primary);
}

.order-title {
  font-size: 15px;
  font-weight: 600;
  color: var(--text-primary);
  display: flex;
  align-items: center;
  gap: 8px;
}

.order-title::before {
  content: '';
  width: 4px;
  height: 16px;
  background: var(--primary);
  border-radius: 2px;
}

.order-meta {
  font-size: 12px;
  color: var(--text-tertiary);
  margin-top: 6px;
  display: flex;
  gap: 12px;
}

/* 会员选择 */
.member-bar {
  padding: 12px 20px;
  border-bottom: 1px solid var(--border-primary);
  display: flex;
  align-items: center;
  gap: 10px;
}

.member-input {
  flex: 1;
}

.member-btn {
  background: var(--bg-secondary);
  border: 1px solid var(--border-primary);
  color: var(--primary);
}

.member-btn:hover {
  border-color: var(--primary);
}

.member-info {
  padding: 10px 20px;
  background: rgba(6, 212, 228, 0.05);
  border-bottom: 1px solid var(--border-primary);
  display: flex;
  align-items: center;
  gap: 10px;
}

.member-avatar {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  background: var(--gradient-primary);
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--bg-primary);
  font-weight: 600;
  font-size: 13px;
}

.member-detail {
  flex: 1;
}

.member-name {
  font-size: 13px;
  color: var(--text-primary);
  font-weight: 500;
}

.member-tag {
  font-size: 11px;
  color: var(--primary);
}

.member-balance {
  font-size: 12px;
  color: var(--success);
  font-family: 'JetBrains Mono', monospace;
}

.member-remove {
  cursor: pointer;
  color: var(--text-tertiary);
  transition: color 0.3s;
}

.member-remove:hover {
  color: var(--danger);
}

/* 购物车列表 */
.cart-list {
  flex: 1;
  overflow-y: auto;
  padding: 8px 0;
}

.cart-item {
  padding: 10px 20px;
  display: flex;
  align-items: center;
  gap: 10px;
  border-bottom: 1px solid var(--border-primary);
  transition: background 0.3s;
}

.cart-item:hover {
  background: var(--bg-hover);
}

.cart-item:last-child {
  border-bottom: none;
}

.cart-info {
  flex: 1;
  min-width: 0;
}

.cart-name {
  font-size: 13px;
  color: var(--text-primary);
  margin-bottom: 4px;
}

.cart-spec {
  font-size: 11px;
  color: var(--text-tertiary);
}

.cart-price {
  font-size: 13px;
  color: var(--primary);
  font-weight: 600;
  font-family: 'JetBrains Mono', monospace;
  min-width: 70px;
  text-align: right;
}

.qty-control {
  display: flex;
  align-items: center;
  gap: 0;
  border: 1px solid var(--border-primary);
  border-radius: var(--radius-sm);
  overflow: hidden;
}

.qty-btn {
  width: 24px;
  height: 24px;
  background: var(--bg-secondary);
  border: none;
  color: var(--text-secondary);
  cursor: pointer;
  font-size: 14px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.qty-btn:hover {
  background: var(--bg-hover);
  color: var(--primary);
}

.qty-input {
  width: 36px;
  height: 24px;
  background: var(--bg-tertiary);
  border: none;
  color: var(--text-primary);
  text-align: center;
  font-size: 13px;
  outline: none;
  font-family: 'JetBrains Mono', monospace;
}

.cart-remove {
  color: var(--text-tertiary);
  cursor: pointer;
  font-size: 18px;
  padding: 4px;
  transition: color 0.3s;
}

.cart-remove:hover {
  color: var(--danger);
}

/* 空状态 */
.empty-cart {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  color: var(--text-tertiary);
  gap: 12px;
  padding: 40px 20px;
}

.empty-cart .icon {
  font-size: 48px;
  opacity: 0.3;
}

.empty-cart p {
  font-size: 14px;
}

/* 底部结算栏 */
.checkout-bar {
  padding: 16px 20px;
  border-top: 1px solid var(--border-primary);
  background: var(--bg-primary);
}

.total-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
  font-size: 13px;
}

.total-label {
  color: var(--text-tertiary);
}

.total-value {
  color: var(--text-primary);
  font-family: 'JetBrains Mono', monospace;
}

.discount-text {
  color: var(--success);
}

.grand-total {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
  padding: 12px 0;
  margin-bottom: 12px;
  border-top: 1px dashed var(--border-primary);
}

.grand-total .label {
  font-size: 14px;
  color: var(--text-secondary);
  font-weight: 500;
}

.grand-total .amount {
  font-size: 28px;
  color: var(--primary);
  font-weight: 700;
  font-family: 'JetBrains Mono', monospace;
  text-shadow: 0 0 10px rgba(6, 212, 228, 0.4);
}

.checkout-btn {
  width: 100%;
  padding: 14px;
  background: var(--gradient-primary);
  border: none;
  border-radius: var(--radius-md);
  color: var(--bg-primary);
  font-size: 16px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.3s;
  box-shadow: 0 0 20px rgba(6, 212, 228, 0.3);
}

.checkout-btn:hover:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: 0 0 30px rgba(6, 212, 228, 0.5);
}

.checkout-btn:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.quick-actions {
  display: flex;
  gap: 8px;
  margin-top: 10px;
}

.quick-btn {
  flex: 1;
  padding: 8px;
  background: var(--bg-tertiary);
  border: 1px solid var(--border-primary);
  border-radius: var(--radius-md);
  color: var(--text-secondary);
  font-size: 12px;
  cursor: pointer;
  transition: all 0.3s;
}

.quick-btn:hover {
  border-color: var(--primary);
  color: var(--primary);
}

/* 支付弹窗 */
.pay-amount {
  text-align: center;
  margin-bottom: 24px;
}

.pay-amount .label {
  font-size: 14px;
  color: var(--text-tertiary);
  margin-bottom: 8px;
}

.pay-amount .value {
  font-size: 36px;
  font-weight: 700;
  color: var(--primary);
  font-family: 'JetBrains Mono', monospace;
}

.pay-methods {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
  margin-bottom: 20px;
}

.pay-method {
  padding: 16px;
  border: 2px solid var(--border-primary);
  border-radius: var(--radius-md);
  cursor: pointer;
  transition: all 0.3s;
  text-align: center;
}

.pay-method:hover {
  border-color: var(--border-secondary);
}

.pay-method.selected {
  border-color: var(--primary);
  background: rgba(6, 212, 228, 0.05);
}

.pay-method .icon {
  font-size: 28px;
  margin-bottom: 6px;
}

.pay-method .icon .el-icon {
  font-size: 28px;
}

.pay-method .name {
  font-size: 13px;
  color: var(--text-primary);
  font-weight: 500;
}

/* 滚动条样式 */
.product-grid::-webkit-scrollbar,
.cart-list::-webkit-scrollbar,
.category-tabs::-webkit-scrollbar {
  width: 6px;
  height: 6px;
}

.product-grid::-webkit-scrollbar-track,
.cart-list::-webkit-scrollbar-track,
.category-tabs::-webkit-scrollbar-track {
  background: var(--bg-secondary);
  border-radius: 3px;
}

.product-grid::-webkit-scrollbar-thumb,
.cart-list::-webkit-scrollbar-thumb,
.category-tabs::-webkit-scrollbar-thumb {
  background: var(--border-secondary);
  border-radius: 3px;
}

/* 购物车效期标签 */
.cart-expiry {
  color: var(--primary);
  font-size: 11px;
}

/* 效期选择弹窗 */
.expiry-content {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.expiry-product-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px;
  background: var(--bg-tertiary);
  border-radius: var(--radius-md);
}

.expiry-product-name {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
}

.expiry-qty-control {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  color: var(--text-secondary);
}

.expiry-qty-control .qty-btn {
  width: 28px;
  height: 28px;
  background: var(--bg-secondary);
  border: 1px solid var(--border-primary);
  border-radius: var(--radius-sm);
  color: var(--text-secondary);
  cursor: pointer;
  font-size: 14px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.expiry-qty-control .qty-btn:hover {
  border-color: var(--primary);
  color: var(--primary);
}

.expiry-qty-input {
  width: 48px;
  height: 28px;
  background: var(--bg-tertiary);
  border: 1px solid var(--border-primary);
  border-radius: var(--radius-sm);
  color: var(--text-primary);
  text-align: center;
  font-size: 13px;
  outline: none;
  font-family: 'JetBrains Mono', monospace;
}

.expiry-options-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  max-height: 240px;
  overflow-y: auto;
}

.expiry-option {
  padding: 12px;
  background: var(--bg-tertiary);
  border: 2px solid var(--border-primary);
  border-radius: var(--radius-md);
  cursor: pointer;
  transition: all 0.2s;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.expiry-option:hover {
  border-color: var(--border-secondary);
}

.expiry-option.selected {
  border-color: var(--primary);
  background: rgba(6, 212, 228, 0.08);
}

.expiry-option-date {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 14px;
  font-weight: 500;
  color: var(--text-primary);
  font-family: 'JetBrains Mono', monospace;
}

.expiry-option-info {
  display: flex;
  gap: 12px;
  font-size: 12px;
  color: var(--text-tertiary);
}

.selected-expiry-section {
  padding: 12px;
  background: var(--bg-tertiary);
  border-radius: var(--radius-md);
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.selected-title {
  font-size: 12px;
  color: var(--text-secondary);
  font-weight: 600;
}

.selected-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 13px;
  color: var(--text-primary);
  font-family: 'JetBrains Mono', monospace;
  padding: 4px 0;
}

.selected-summary {
  font-size: 12px;
  color: var(--text-tertiary);
  display: flex;
  align-items: center;
  gap: 6px;
  margin-top: 4px;
  padding-top: 8px;
  border-top: 1px dashed var(--border-primary);
}

.auto-recommend-section {
  padding: 12px;
  background: rgba(6, 212, 228, 0.05);
  border-radius: var(--radius-md);
  border: 1px dashed var(--border-secondary);
}

/* 疗程卡核销弹窗 */
.verify-section {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.verify-step {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.verify-step .step-label {
  font-size: 13px;
  color: var(--text-secondary);
}

.verify-step .step-label-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.verify-member-bar {
  display: flex;
  gap: 8px;
}

.verify-items-table {
  border: 1px solid var(--border-primary);
  border-radius: var(--radius-md);
  overflow: hidden;
}

.verify-items-header,
.verify-items-row {
  display: grid;
  grid-template-columns: 1.6fr 0.8fr 0.9fr 0.6fr;
  gap: 8px;
  padding: 8px 12px;
  align-items: center;
}

.verify-items-header {
  background: var(--bg-tertiary);
  font-size: 12px;
  color: var(--text-secondary);
}

.verify-items-row {
  border-top: 1px solid var(--border-primary);
}

.verify-items-row .col-amount {
  font-weight: 600;
  color: var(--primary);
}

.verify-items-footer {
  display: flex;
  justify-content: space-between;
  padding: 10px 12px;
  background: var(--bg-tertiary);
  border-top: 1px solid var(--border-primary);
  font-size: 13px;
  color: var(--text-secondary);
}

.verify-items-footer b {
  color: var(--primary);
  font-size: 15px;
}

/* ========== 组合支付 3 栏联动 ========== */
.combined-pay-section {
  margin-top: 12px;
  padding: 12px;
  background: var(--bg-tertiary, #f5f7fa);
  border-radius: 8px;
  border: 1px dashed var(--border-primary, #dcdfe6);
}

.combined-row {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 10px;
}

.combined-row:last-of-type {
  margin-bottom: 0;
}

.combined-label {
  width: 220px;
  font-size: 12px;
  color: var(--text-secondary, #909399);
  flex-shrink: 0;
}

.combined-hint {
  display: inline-block;
  margin-left: 4px;
  color: var(--primary, #409eff);
  font-size: 11px;
}

.combined-summary {
  margin-top: 8px;
  padding-top: 8px;
  border-top: 1px solid var(--border-primary, #dcdfe6);
  font-size: 13px;
  font-weight: 600;
  color: var(--text-primary, #303133);
}

.combined-warn {
  margin-left: 8px;
  color: var(--el-color-danger, #f56c6c);
  font-weight: normal;
  font-size: 12px;
}
</style>
