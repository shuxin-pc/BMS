<template>
  <div class="pos-page">
    <div class="pos-container">
      <!-- ========== 左侧商品选择区 ========== -->
      <div class="product-section">
        <div class="product-header">
          <el-input
            v-model="searchKeyword"
            placeholder="搜索商品名称..."
            clearable
            :prefix-icon="Search"
            class="search-input"
            @keyup.enter="handleSearch"
          />
        </div>

        <!-- 商品/赠品/项目卡 切换 Tab（赠品是 Product 表 type=5 的记录，随订单一起提交，后端按 Type=5 识别走 DeductSampleGiftOutAsync 扣减；项目卡为已启用的项目卡配置，点击进入销售弹窗） -->
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
          <div
            class="mode-tab"
            :class="{ active: productMode === 'treatment' }"
            @click="switchProductMode('treatment')"
          >项目卡</div>
        </div>

        <!-- 分类Tab（商品/赠品模式显示；赠品同为 Product 表记录且具备商品分类属性，支持按分类筛选；项目卡无分类概念不显示） -->
        <div v-if="productMode === 'product' || productMode === 'gift'" class="category-tabs">
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

        <!-- 网格区（商品模式显示可销售商品；赠品模式显示 Type=5 赠品；项目卡模式显示已启用的项目卡配置，点击进入销售弹窗） -->
        <div class="product-grid">
          <!-- 项目卡网格 -->
          <template v-if="productMode === 'treatment'">
            <div
              v-for="card in filteredTreatmentCards"
              :key="card.id"
              class="product-card treatment-card"
              @click="openTreatmentSale(card)"
            >
              <div class="product-img" :class="card.colorClass">
                {{ card.name.charAt(0) }}
              </div>
              <div class="product-name">{{ card.name }}</div>
              <div class="product-spec">总次数 {{ card.totalTimes }} 次 · 有效期 {{ card.validityDays }} 天</div>
              <div class="product-price">¥{{ formatPrice(card.price) }}</div>
              <div class="product-stock">{{ card.itemSummary }}</div>
            </div>
            <div v-if="filteredTreatmentCards.length === 0" class="empty-products">
              <el-icon><Box /></el-icon>
              <p>暂无项目卡</p>
            </div>
          </template>
          <!-- 商品/赠品网格 -->
          <template v-else>
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
          </template>
        </div>
      </div>

      <!-- ========== 右侧订单区 ========== -->
      <div class="order-section">
        <div class="order-header">
          <div class="order-title">当前订单</div>
        </div>

        <!-- 会员选择（远程搜索候选列表，从候选中确认选择） -->
        <div class="member-bar">
          <div class="member-search">
            <span class="member-search-label">
              <el-icon><User /></el-icon>
              <span>会员</span>
            </span>
            <el-select
              v-model="selectedMemberId"
              filterable
              remote
              reserve-keyword
              clearable
              :remote-method="searchMembers"
              :loading="memberLoading"
              placeholder="输入会员手机号搜索"
              class="member-input"
              style="width: 100%"
              @change="handleMemberSelect"
              @clear="() => clearMember(true)"
            >
              <el-option
                v-for="m in memberResults"
                :key="m.id"
                :label="m.name"
                :value="m.id"
              >
                <span>{{ m.name }}（{{ m.phone }}）</span>
              </el-option>
            </el-select>
          </div>
          <!-- 未选择时展示权益提示，选择后由下方会员卡承载 -->
          <div v-if="!selectedMember" class="member-hint">
            <el-icon class="hint-icon"><Wallet /></el-icon>
            <span>选会员享专属折扣 · 储值支付 · 积分抵扣</span>
          </div>
        </div>

        <!-- 会员信息：发光会员卡（选中后展开） -->
        <div v-if="selectedMember" class="member-card">
          <div class="member-card-head">
            <div class="member-avatar">{{ selectedMember.name.charAt(0) }}</div>
            <div class="member-identity">
              <div class="member-name">
                {{ selectedMember.name }}
                <span class="member-tag">{{ selectedMember.levelName }}</span>
              </div>
              <div class="member-phone">{{ selectedMember.phone }}</div>
            </div>
            <span class="member-remove" @click="clearMember(true)">×</span>
          </div>
          <!-- 数据面板：储值/积分/折扣 -->
          <div class="member-metrics">
            <div class="member-metric">
              <div class="metric-label">储值余额</div>
              <div class="metric-value metric-balance">¥{{ formatPrice(selectedMember.balance) }}</div>
            </div>
            <div class="metric-divider"></div>
            <div class="member-metric">
              <div class="metric-label">可用积分</div>
              <div class="metric-value metric-points">{{ selectedMember.totalPoints }}</div>
            </div>
            <div class="metric-divider"></div>
            <div class="member-metric">
              <div class="metric-label">会员折扣</div>
              <div class="metric-value metric-discount">{{ selectedMember.discountRate }}折</div>
            </div>
          </div>
        </div>

        <!-- 购物车列表 -->
        <div class="cart-list">
          <div v-if="cart.length === 0" class="empty-cart">
            <el-icon class="icon"><ShoppingCart /></el-icon>
            <p>购物车为空，点击左侧商品添加</p>
          </div>
          <div v-for="item in cart" :key="item.lineId" class="cart-item" :class="{ 'gift-item': item.productType === 5 }">
            <!-- 项目卡核销行：扣卡次不收款，金额仅展示；无数量控件，删除行即移除；点击行查看核销详情/编辑 -->
            <template v-if="item.lineType === 'verify'">
              <div
                class="cart-info cart-info-clickable"
                title="点击查看核销详情"
                @click="openCartVerifyDetail(item)"
              >
                <div class="cart-name">
                  {{ item.cardName || item.name }}
                  <span class="neon-tag neon-verify">核销</span>
                </div>
              </div>
              <!-- 核销行金额仅展示不参与收款，价格加删除线弱化提示 -->
              <div class="cart-price price-strike">¥{{ formatPrice(item.price) }}</div>
              <span class="cart-remove" @click="removeFromCart(item)">×</span>
            </template>
            <!-- 项目卡开卡行：开卡费参与应付金额展示（店员按显示金额线下收款），走独立销售记账；无数量控件，删除行即移除；点击行查看开卡详情/编辑 -->
            <template v-else-if="item.lineType === 'sale'">
              <div
                class="cart-info cart-info-clickable"
                title="点击查看开卡详情"
                @click="openCartSaleDetail(item)"
              >
                <div class="cart-name">
                  {{ item.name }}
                  <span class="neon-tag neon-sale">开卡</span>
                </div>
              </div>
              <div class="cart-price">¥{{ formatPrice(item.price) }}</div>
              <span class="cart-remove" @click="removeFromCart(item)">×</span>
            </template>
            <!-- 商品/服务行（含赠品） -->
            <template v-else>
              <!-- 商品/服务行均支持点击查看详情弹窗：服务项目查看服务详情（技师/房间/设备/时间），商品/耗材/赠品查看商品详情（效期批次等），效期不再行内展示 -->
              <div
                class="cart-info cart-info-clickable"
                :title="item.productType === 2 ? '点击查看服务详情' : '点击查看商品详情'"
                @click="item.productType === 2 ? openCartServiceDetail(item) : openCartProductDetail(item)"
              >
                <div class="cart-name">
                  {{ item.name }}
                  <!-- 预约转单的服务项目显示"预约"标签，与左侧直接加购的服务商品"服务"标签区分 -->
                  <span v-if="item.productType === 2 && item.sourceAppointmentId" class="neon-tag neon-appointment">预约</span>
                  <span v-else-if="item.productType === 2" class="neon-tag neon-service">服务</span>
                  <span v-if="item.productType === 5" class="neon-tag neon-gift">赠品</span>
                </div>
              </div>
              <div class="qty-control">
                <button class="qty-btn" @click="changeQty(item, -1)">−</button>
                <input
                  class="qty-input"
                  v-model.number="item.quantity"
                  @change="onQtyInput(item)"
                  @focus="onCartQtyFocus(item)"
                />
                <button class="qty-btn" @click="changeQty(item, 1)">+</button>
              </div>
              <div class="cart-price">¥{{ formatPrice(item.price * item.quantity) }}</div>
              <span class="cart-remove" @click="removeFromCart(item)">×</span>
            </template>
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
            :disabled="cart.length === 0"
            @click="openPayDialog"
          >
            结算收银
          </el-button>
          <div class="quick-actions">
            <button class="quick-btn" @click="handleHoldOrder">挂单</button>
            <button class="quick-btn" @click="handleResumeOrder">取单</button>
            <button class="quick-btn" @click="handleVerifyCard">项目卡核销</button>
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
          :class="{ selected: isPayMethodActive(method.id), 'offline-selected': isOfflineSelected(method.id) }"
          @click="onSelectPayMethod(method.id)"
        >
          <div class="icon" :style="{ color: method.color }">
            <el-icon><component :is="method.icon" /></el-icon>
          </div>
          <div class="name">{{ method.name }}</div>
        </div>
      </div>

      <!-- 会员储值账户信息面板（选中"会员储值"时展示） -->
      <div v-if="selectedPayMethod === 'balance' && selectedMember" class="pay-info-section">
        <div class="pay-info-title">会员储值</div>
        <div class="pay-info-row">
          <span class="label">会员</span>
          <span class="value">{{ selectedMember.name }}（{{ selectedMember.levelName }}）</span>
        </div>
        <div class="pay-info-row">
          <span class="label">储值余额</span>
          <span class="value" :class="{ danger: selectedMember.balance < payableAmount }">¥{{ formatPrice(selectedMember.balance) }}</span>
        </div>
        <div class="pay-info-row">
          <span class="label">本单扣除</span>
          <span class="value">¥{{ formatPrice(payableAmount) }}</span>
        </div>
        <div class="pay-info-row">
          <span class="label">支付后剩余</span>
          <span class="value">¥{{ formatPrice(balanceAfterPay) }}</span>
        </div>
      </div>

      <!-- 积分抵扣信息面板（选中"积分抵扣"时展示） -->
      <div v-if="selectedPayMethod === 'points' && selectedMember" class="pay-info-section">
        <div class="pay-info-title">积分抵扣</div>
        <div class="pay-info-row">
          <span class="label">当前积分</span>
          <span class="value" :class="{ danger: selectedMember.totalPoints < pointsNeeded }">{{ selectedMember.totalPoints }}</span>
        </div>
        <div class="pay-info-row">
          <span class="label">本单需要积分</span>
          <span class="value">{{ pointsNeeded }}</span>
        </div>
        <div class="pay-info-row">
          <span class="label">最大抵扣积分<em v-if="comboPointsRule && comboPointsRule.maxDeductAmount > 0" class="label-note">单笔最高抵扣金额{{ comboPointsRule.maxDeductAmount }}元</em></span>
          <span class="value" :class="{ danger: maxPointsUsed < pointsNeeded }">{{ maxPointsUsed }}</span>
        </div>
        <div class="pay-info-row">
          <span class="label">剩余积分</span>
          <span class="value">{{ pointsRemain }}</span>
        </div>
      </div>

      <!-- 组合支付 3 栏联动输入（顺序：积分抵扣 / 储值余额 / 线下支付） -->
      <div v-if="isCombinedPay" class="combined-pay-section">
        <div class="combined-row">
          <div class="combined-label">
            积分抵扣
            <span class="combined-hint">本单可抵扣{{ pointsDeductHint.amount }}元，消耗{{ pointsDeductHint.points }}积分</span>
          </div>
          <el-input-number
            v-model="pointsAmount"
            :min="0"
            :max="pointsDeductMax"
            :precision="0"
            :step="1"
            class="combined-input"
            @change="onPointsChange"
          />
          <span class="combined-unit">元</span>
        </div>
        <div class="combined-row">
          <div class="combined-label">
            储值余额
            <span class="combined-hint">本单可扣除{{ formatPrice(storedValueDeductHint) }}元</span>
          </div>
          <el-input-number
            v-model="storedValueAmount"
            :min="0"
            :max="selectedMember?.balance || 0"
            :precision="2"
            :step="1"
            class="combined-input"
            @change="onStoredValueChange"
          />
        </div>
        <div class="combined-row">
          <div class="combined-label">
            线下支付
            <span v-if="offlinePayMethodName" class="combined-hint">（{{ offlinePayMethodName }}）</span>
          </div>
          <el-input-number
            v-model="cashAmount"
            :min="0"
            :precision="2"
            :step="1"
            class="combined-input"
            @change="onCashAmountChange"
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
          type="date"
          placeholder="选择补录日期"
          format="YYYY-MM-DD"
          value-format="YYYY-MM-DD"
          style="width: 100%; margin-top: 8px"
        />
      </div>
      <template #footer>
        <el-button @click="payDialogVisible = false">取消</el-button>
        <el-button
          type="primary"
          :loading="paying"
          :disabled="!canConfirmPay"
          @click="handleConfirmPay(false)"
        >确认收款</el-button>
      </template>
    </el-dialog>

    <!-- 服务内容弹窗（快速开单录入服务项目技师/房间/设备/服务时间，参考预约列表弹窗；占用标红按服务时间实时查询） -->
    <el-dialog
      v-model="serviceDialogVisible"
      title="服务内容"
      width="640px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="serviceFormRef"
        :model="serviceForm"
        :rules="serviceFormRules"
        label-width="100px"
      >
        <!-- 服务项目由点击的商品确定，禁止更改，仅展示当前商品名称 -->
        <el-form-item label="服务项目" prop="productId">
          <div class="service-product-name">{{ serviceForm.productName }}</div>
        </el-form-item>
        <el-form-item label="开始时间" prop="startTime">
          <el-date-picker
            v-model="serviceForm.startTime"
            type="datetime"
            placeholder="请选择开始时间"
            format="YYYY-MM-DD HH:mm"
            value-format="YYYY-MM-DDTHH:mm:ss"
            style="width: 100%"
            @change="handleServiceStartTimeChange"
          />
        </el-form-item>
        <el-form-item label="结束时间" prop="endTime">
          <el-date-picker
            v-model="serviceForm.endTime"
            type="datetime"
            placeholder="请选择结束时间"
            format="YYYY-MM-DD HH:mm"
            value-format="YYYY-MM-DDTHH:mm:ss"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item label="商家技师">
          <el-select
            v-model="serviceForm.merchantTechnicianId"
            filterable
            clearable
            placeholder="请选择商家技师"
            popper-class="technician-select-popper"
            style="width: 100%"
            @change="handleServiceMerchantChange"
          >
            <el-option
              v-for="t in merchantTechnicianOptions"
              :key="t.id"
              :label="t.name"
              :value="t.id"
            >
              <div class="technician-option">
                <span :class="{ 'resource-occupied': technicianOccupancyMap.get(t.id)?.isOccupied }">
                  {{ t.name }}
                  <el-tag v-if="technicianOccupancyMap.get(t.id)?.isOccupied" type="danger" size="small" effect="plain">
                    占用
                  </el-tag>
                </span>
                <span class="technician-skill">{{ formatServiceSkills(t.skillCategoryNames) }}</span>
              </div>
            </el-option>
          </el-select>
        </el-form-item>
        <el-form-item label="平台技师">
          <el-select
            v-model="serviceForm.platformTechnicianId"
            filterable
            clearable
            placeholder="请选择平台技师"
            popper-class="technician-select-popper"
            style="width: 100%"
            @change="handleServicePlatformChange"
          >
            <el-option
              v-for="t in platformTechnicianOptions"
              :key="t.id"
              :label="t.name"
              :value="t.id"
            >
              <div class="technician-option">
                <span>{{ t.name }}</span>
                <span class="technician-skill">{{ formatServiceSkills(t.skillCategoryNames) }}</span>
              </div>
            </el-option>
          </el-select>
        </el-form-item>
        <el-form-item label="房间/床位">
          <el-select
            v-model="serviceForm.roomId"
            placeholder="请选择房间/床位"
            clearable
            style="width: 100%"
            @change="handleServiceRoomChange"
          >
            <el-option
              v-for="room in roomOptions"
              :key="room.id"
              :label="room.name"
              :value="room.id"
            >
              <span :class="{ 'resource-occupied': roomOccupancyMap.get(room.id)?.isOccupied }">
                {{ room.name }}（{{ room.roomType === 1 ? '房间' : '床位' }}）
                <el-tag v-if="roomOccupancyMap.get(room.id)?.isOccupied" type="danger" size="small" effect="plain">
                  占用
                </el-tag>
              </span>
            </el-option>
          </el-select>
        </el-form-item>
        <el-form-item label="设备">
          <el-select
            v-model="serviceForm.equipmentId"
            placeholder="请选择设备"
            clearable
            filterable
            style="width: 100%"
            @change="handleServiceEquipmentChange"
          >
            <el-option
              v-for="eq in equipmentOptions"
              :key="eq.id"
              :label="eq.name"
              :value="eq.id"
            >
              <span :class="{ 'resource-occupied': equipmentOccupancyMap.get(eq.id)?.isOccupied }">
                {{ eq.name }}
                <el-tag v-if="equipmentOccupancyMap.get(eq.id)?.isOccupied" type="danger" size="small" effect="plain">
                  占用
                </el-tag>
              </span>
            </el-option>
          </el-select>
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="serviceForm.remark" type="textarea" :rows="2" placeholder="请输入备注" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="serviceDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="serviceSubmitting" @click="confirmServiceDialog">
          {{ verifyEditingRow ? '确定' : '加入购物车' }}
        </el-button>
      </template>
    </el-dialog>

    <!-- 服务详情弹窗（点击购物车服务项目行查看，参考预约列表详情形态；展示字段对齐服务内容弹窗，底部提供编辑入口） -->
    <el-dialog v-model="serviceDetailVisible" title="服务详情" width="500px">
      <el-descriptions :column="1" border label-width="88px" v-if="serviceDetail">
        <el-descriptions-item label="服务项目">{{ serviceDetail.name }}</el-descriptions-item>
        <el-descriptions-item label="技师">{{ serviceDetail.technicianName || '-' }}</el-descriptions-item>
        <el-descriptions-item label="技师来源">
          <el-tag
            v-if="serviceDetail.technicianSource"
            :type="getTechnicianSourceTagType(serviceDetail.technicianSource)"
            size="small"
            effect="plain"
          >
            {{ getTechnicianSourceText(serviceDetail.technicianSource) }}
          </el-tag>
          <span v-else>-</span>
        </el-descriptions-item>
        <el-descriptions-item label="房间">{{ serviceDetail.roomName || '-' }}</el-descriptions-item>
        <el-descriptions-item label="设备">{{ serviceDetail.equipmentName || '-' }}</el-descriptions-item>
        <!-- 服务时间含完整日期（YYYY-MM-DD HH:mm），跨天时段两端日期均可见 -->
        <el-descriptions-item label="服务时间">
          {{ formatServiceDateTime(serviceDetail.serviceStartTime) }} - {{ formatServiceDateTime(serviceDetail.serviceEndTime) }}
        </el-descriptions-item>
        <el-descriptions-item label="备注">{{ serviceDetail.remark || '-' }}</el-descriptions-item>
      </el-descriptions>
      <template #footer>
        <el-button @click="serviceDetailVisible = false">关闭</el-button>
        <el-button type="primary" @click="editServiceDetail">编辑服务内容</el-button>
      </template>
    </el-dialog>

    <!-- 批次详情弹窗（点击购物车商品/耗材/赠品行查看；以批次扣减明细为主，按扣除顺序展示各批次扣减数量/剩余库存，含编辑入口） -->
    <el-dialog v-model="productDetailVisible" title="批次详情" width="460px">
      <div v-if="productDetail" class="product-detail-body">
        <div class="product-detail-expiry">
          <div class="product-detail-expiry-header">
            批次扣减明细
            <el-tag size="small" type="info" effect="plain">
              {{ productDetail.expirationDates?.length ? '手动选择' : '系统推荐' }}
            </el-tag>
          </div>
          <template v-if="productDetail.expiryDetails?.length">
            <!-- 批次明细表格：按扣除顺序展示（批次效期 | 库存 | 扣减数量 | 剩余库存） -->
            <div class="product-detail-expiry-table">
              <div class="product-detail-expiry-th">
                <span>批次效期</span>
                <span>库存</span>
                <span>扣减数量</span>
                <span>剩余库存</span>
              </div>
              <div v-for="(d, idx) in productDetail.expiryDetails" :key="d.expirationDate ?? 'no-expiry'" class="product-detail-expiry-tr">
                <span class="product-detail-expiry-date">{{ idx + 1 }}. {{ d.expirationDate ? d.expirationDate.slice(0, 10) : '无效期限制' }}</span>
                <span class="product-detail-expiry-stock">{{ d.stock }}</span>
                <span class="product-detail-expiry-qty">-{{ d.quantity }}</span>
                <span class="product-detail-expiry-remaining">{{ d.remainingStock }}</span>
              </div>
            </div>
            <div class="product-detail-expiry-summary">合计扣减 {{ expiryDetailTotal }} 件 / 需求 {{ productDetail.quantity }} 件</div>
          </template>
          <div v-else class="product-detail-expiry-state product-detail-expiry-missing">
            暂无批次明细
          </div>
        </div>
      </div>
      <template #footer>
        <el-button @click="productDetailVisible = false">关闭</el-button>
        <el-button type="primary" @click="editProductDetail">编辑效期</el-button>
      </template>
    </el-dialog>

    <!-- 项目卡开卡弹窗（向当前选中的会员售卖已启用的项目卡配置，确认后加入购物车，结算时走 POST /treatmentCardSales 独立接口） -->
    <el-dialog v-model="saleDialogVisible" title="项目卡开卡" width="520px" :close-on-click-modal="false">
      <div v-if="saleCard" class="sale-section">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="会员">{{ selectedMember?.name || '-' }}</el-descriptions-item>
          <el-descriptions-item label="手机号">{{ selectedMember?.phone || '-' }}</el-descriptions-item>
          <el-descriptions-item label="卡名称" :span="2">{{ saleCard.name }}</el-descriptions-item>
          <el-descriptions-item label="总次数">{{ saleCard.totalTimes }} 次</el-descriptions-item>
          <el-descriptions-item label="有效期">{{ saleCard.validityDays }} 天</el-descriptions-item>
          <el-descriptions-item label="包含项目" :span="2">{{ saleCard.itemSummary }}</el-descriptions-item>
        </el-descriptions>
        <el-form label-width="70px" class="sale-form">
          <el-form-item label="售价">
            <el-input-number v-model="saleAmount" :min="0" :precision="2" :step="1" style="width: 200px" />
          </el-form-item>
          <el-form-item label="备注">
            <el-input v-model="saleRemark" type="textarea" :rows="2" placeholder="选填" />
          </el-form-item>
        </el-form>
      </div>
      <template #footer>
        <el-button @click="saleDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="confirmAddSaleToCart">加入购物车</el-button>
      </template>
    </el-dialog>

    <!-- 开卡详情弹窗（点击购物车项目卡开卡行查看；卡配置信息从当前项目卡列表反查，售价/备注取购物车行固化值，底部提供编辑入口） -->
    <el-dialog v-model="saleDetailVisible" title="开卡详情" width="500px">
      <el-descriptions v-if="saleDetail" :column="1" border label-width="88px">
        <el-descriptions-item label="会员">{{ saleDetail.line.saleCustomerName || '-' }}</el-descriptions-item>
        <el-descriptions-item label="卡名称">{{ saleDetail.line.name }}</el-descriptions-item>
        <el-descriptions-item label="总次数">{{ saleDetail.card ? `${saleDetail.card.totalTimes} 次` : '-' }}</el-descriptions-item>
        <el-descriptions-item label="有效期">{{ saleDetail.card ? `${saleDetail.card.validityDays} 天` : '-' }}</el-descriptions-item>
        <el-descriptions-item label="包含项目">{{ saleDetail.card?.itemSummary || '-' }}</el-descriptions-item>
        <el-descriptions-item label="售价">¥{{ formatPrice(saleDetail.line.saleAmount ?? 0) }}</el-descriptions-item>
        <el-descriptions-item label="备注">{{ saleDetail.line.saleRemark || '-' }}</el-descriptions-item>
      </el-descriptions>
      <template #footer>
        <el-button @click="saleDetailVisible = false">关闭</el-button>
        <el-button type="primary" @click="editSaleDetail">编辑开卡信息</el-button>
      </template>
    </el-dialog>

    <!-- 项目卡核销弹窗（左右分栏：左侧选择项目卡，右侧核销项目编辑；会员复用购物车上方选择，弹窗内不再搜索；编辑模式从购物车核销详情进入，见 editVerifyDetail） -->
    <el-dialog v-model="verifyDialogVisible" title="项目卡核销" width="920px" :close-on-click-modal="false" @close="verifyEditingLine = null">
      <!-- 顶部会员栏：核销对象固定为购物车上方已选会员 -->
      <div class="verify-member-bar">
        <el-icon class="verify-member-icon"><User /></el-icon>
        <span class="verify-member-name">{{ selectedMember?.name }}</span>
        <span class="verify-member-phone">{{ selectedMember?.phone }}</span>
        <span class="verify-member-tip">核销当前会员的有效项目卡</span>
      </div>
      <div class="verify-layout">
        <!-- 左栏：该会员有效项目卡卡片列表（点击选中，加载后默认选中第一张） -->
        <div class="verify-card-panel">
          <div class="verify-panel-title">选择项目卡（{{ cardList.length }}）</div>
          <div v-if="cardList.length === 0" class="verify-card-empty">
            <el-icon><Ticket /></el-icon>
            <p>该会员暂无有效项目卡</p>
          </div>
          <div v-else class="verify-card-list">
            <div
              v-for="card in cardList"
              :key="card.id"
              class="verify-card-item"
              :class="{ active: selectedCardSaleId === card.id }"
              @click="onCardSelect(card)"
            >
              <div class="verify-card-name">{{ getVerifyCardName(card) }}</div>
              <div class="verify-card-meta">
                <span class="verify-card-remaining">剩余 {{ card.remainingTimes }} 次</span>
                <span class="verify-card-expiry">到期 {{ formatVerifyExpiry(card.expiryDate) }}</span>
              </div>
              <div class="verify-card-sub">含 {{ card.items.length }} 个服务项目</div>
            </div>
          </div>
        </div>
        <!-- 右栏：选中项目卡的核销项目编辑区 -->
        <div class="verify-editor">
          <div class="verify-panel-title">核销项目</div>
          <div v-if="!selectedCard" class="verify-editor-placeholder">
            <el-icon><Pointer /></el-icon>
            <p>请在左侧选择一张项目卡</p>
          </div>
          <div v-else-if="selectedCard.items.length === 0" class="verify-editor-placeholder">
            <el-icon><Warning /></el-icon>
            <p>该项目卡无核销项目</p>
          </div>
          <template v-else>
            <div class="step-label-row">
              <span class="step-label">本次核销项目</span>
              <el-button link type="primary" :disabled="verifyItems.length >= availableVerifyProductCount" @click="addVerifyItem">
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
                <div class="verify-items-row-main">
                  <div class="col-product">
                    <el-select v-model="row.productId" placeholder="选择项目" style="width: 100%" :disabled="selectedCard!.items.length === 1" @change="handleVerifyRowProductChange(idx)">
                      <el-option
                        v-for="item in selectedCard!.items"
                        :key="item.productId"
                        :label="`${getVerifyProductName(item.productId)}（¥${item.allocatedUnitPrice}/次）`"
                        :value="item.productId"
                        :disabled="isProductSelected(item.productId, idx) || itemRemainingTimes(item.productId) <= 0"
                      />
                    </el-select>
                  </div>
                  <div class="col-times">
                    <el-input-number v-model="row.verifyTimes" :min="1" :max="maxTimesForRow(idx)" size="small" style="width: 100%" />
                  </div>
                  <div class="col-amount">¥{{ formatPrice(rowSubAmount(idx)) }}</div>
                  <div class="col-action">
                    <el-button link size="small" type="primary" :disabled="row.productId == null" @click="openVerifyRowEditor(row)">编辑</el-button>
                    <el-button link size="small" type="danger" :disabled="verifyItems.length === 1" @click="removeVerifyItem(idx)">删除</el-button>
                  </div>
                </div>
              </div>
              <div class="verify-items-footer">
                <span>合计：<b>¥{{ formatPrice(totalAmount) }}</b></span>
                <span>共 <b>{{ totalTimes }}</b> 次</span>
              </div>
            </div>
          </template>
        </div>
      </div>
      <template #footer>
        <el-button @click="verifyDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="verifying" :disabled="!canConfirmVerify" @click="confirmVerifyCard">加入购物车</el-button>
      </template>
    </el-dialog>

    <!-- 核销详情弹窗（点击购物车项目卡核销行查看；卡信息与项目折算金额从当前会员有效卡列表反查，反查不到回退行内固化值，底部提供编辑入口） -->
    <el-dialog v-model="verifyDetailVisible" title="核销详情" width="700px">
      <template v-if="verifyDetail">
        <el-descriptions :column="1" border label-width="88px">
          <el-descriptions-item label="会员">{{ selectedMember?.name || '-' }}</el-descriptions-item>
          <el-descriptions-item label="卡名称">{{ verifyDetail.line.cardName || verifyDetail.line.name }}</el-descriptions-item>
          <el-descriptions-item label="剩余次数">{{ verifyDetail.card?.remainingTimes ?? verifyDetail.line.remainingTimes ?? 0 }} 次</el-descriptions-item>
          <el-descriptions-item label="到期日期">{{ verifyDetail.card?.expiryDate ? formatVerifyExpiry(verifyDetail.card.expiryDate) : '-' }}</el-descriptions-item>
        </el-descriptions>
        <div class="verify-detail-items">
          <div class="verify-detail-items-title">核销项目明细</div>
          <!-- 核销项目明细表格：服务项目 | 次数 | 金额 | 服务时间（金额按反查的折算单价计算，反查不到显示 -） -->
          <div class="verify-detail-table">
            <div class="verify-detail-th">
              <span>服务项目</span>
              <span>次数</span>
              <span>金额</span>
              <span>服务时间</span>
            </div>
            <template v-for="(row, idx) in verifyDetailItems" :key="idx">
              <div class="verify-detail-tr">
                <span class="verify-detail-product">{{ row.productName }}</span>
                <span>{{ row.verifyTimes }} 次</span>
                <span class="verify-detail-amount">{{ row.amount != null ? `¥${formatPrice(row.amount)}` : '-' }}</span>
                <span class="verify-detail-time">{{ row.serviceTimeText }}</span>
              </div>
              <!-- 核销项目备注：有备注时占满整行展示（四列 grid 跨列） -->
              <div v-if="row.remark" class="verify-detail-remark">备注：{{ row.remark }}</div>
            </template>
          </div>
          <div class="verify-detail-footer">
            <span>合计：<b>¥{{ formatPrice(verifyDetail.line.price) }}</b></span>
            <span>共 <b>{{ verifyDetailTotalTimes }}</b> 次</span>
          </div>
        </div>
      </template>
      <template #footer>
        <el-button @click="verifyDetailVisible = false">关闭</el-button>
        <!-- 卡反查不到（已用完/过期）时禁用编辑入口，避免进入核销弹窗后无法回填 -->
        <el-button
          type="primary"
          :disabled="!verifyDetail?.card"
          :title="verifyDetail?.card ? '' : '该项目卡已用完或过期，无法编辑核销'"
          @click="editVerifyDetail"
        >编辑核销信息</el-button>
      </template>
    </el-dialog>

    <!-- 结算完成聚合展示弹窗：按批次号展示本次购物车结算生成的订单/核销/开卡记录 -->
    <el-dialog v-model="settlementResultVisible" title="结算完成" width="520px" :close-on-click-modal="false">
      <div v-if="settlementResult" class="settle-result">
        <template v-if="settlementResult.orders.length > 0">
          <div class="settle-title">商品/服务订单</div>
          <div v-for="o in settlementResult.orders" :key="o.orderNo" class="settle-row">
            单号 {{ o.orderNo }} · ¥{{ formatPrice(o.amount) }} · {{ o.payMethodName }}
          </div>
        </template>
        <template v-if="settlementResult.verifies.length > 0">
          <div class="settle-title">项目卡核销</div>
          <div v-for="(v, i) in settlementResult.verifies" :key="i" class="settle-row">
            {{ v.cardName }} · 核销 {{ v.times }} 次 · ¥{{ formatPrice(v.amount) }}
          </div>
        </template>
        <template v-if="settlementResult.sales.length > 0">
          <div class="settle-title">项目卡开卡</div>
          <div v-for="(s, i) in settlementResult.sales" :key="i" class="settle-row">
            {{ s.cardName }} · ¥{{ formatPrice(s.amount) }}
          </div>
        </template>
      </div>
      <template #footer>
        <el-button type="primary" @click="settlementResultVisible = false">完成</el-button>
      </template>
    </el-dialog>

    <!-- 预约转订单弹窗：规范展示符合条件的可转单预约（1已预约 / 2已到店），单选其中任一条转入购物车 -->
    <el-dialog v-model="appointmentDialogVisible" title="预约转单" width="880px" :close-on-click-modal="false">
      <el-table
        ref="appointmentTableRef"
        class="appointment-table"
        :data="todayAppointments"
        v-loading="appointmentLoading"
        height="360"
        highlight-current-row
        @current-change="handleAppointmentRowChange"
      >
        <el-table-column label="客户" width="110" show-overflow-tooltip>
          <template #default="{ row }">{{ row.customerName || ('客户ID:' + row.customerId) }}</template>
        </el-table-column>
        <el-table-column prop="customerPhone" label="手机号" width="120" show-overflow-tooltip />
        <el-table-column prop="productName" label="服务项目" min-width="80" show-overflow-tooltip />
        <el-table-column label="技师" width="110" show-overflow-tooltip>
          <template #default="{ row }">{{ row.technicianName || '-' }}</template>
        </el-table-column>
        <el-table-column label="预约时间" width="240" show-overflow-tooltip>
          <template #default="{ row }">
            {{ row.startTime ? row.startTime.slice(0, 16).replace('T', ' ') : '-' }} - {{ row.endTime ? row.endTime.slice(11, 16) : '-' }}
          </template>
        </el-table-column>
        <el-table-column label="状态" width="76">
          <template #default="{ row }">
            <el-tag :type="getStatusTagType(row.status)" size="small" effect="dark">{{ getStatusText(row.status) }}</el-tag>
          </template>
        </el-table-column>
      </el-table>
      <el-empty v-if="todayAppointments.length === 0 && !appointmentLoading" description="暂无符合条件的可转单预约" :image-size="72" />
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
            <button class="qty-btn" @click="onExpiryQtyDecrease">−</button>
            <input class="expiry-qty-input" v-model.number="expiryQuantity" type="number" min="1" step="1" @keydown="onExpiryQtyKeydown" @change="onExpiryQtyChange" @focus="expiryQtyInputFocused = true" @blur="expiryQtyInputFocused = false" />
            <button class="qty-btn" @click="onExpiryQtyIncrease">+</button>
          </div>
        </div>

        <!-- 赠品关联活动（非必填，用于活动维度归因统计，写入 InventoryLog.ActivityId） -->
        <div v-if="pendingProduct.productType === 5" class="expiry-activity-section">
          <div class="expiry-activity-label">关联活动（非必填）</div>
          <el-select
            v-model="pendingActivityId"
            placeholder="选择关联活动"
            clearable
            style="width: 100%"
          >
            <el-option
              v-for="act in activityOptions"
              :key="act.id"
              :label="act.name"
              :value="act.id"
            />
          </el-select>
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

        <!-- 系统自动推荐选项（实物商品/赠品一致：勾选后按近效期优先自动扣减；点选批次会自动切换为手选） -->
        <div class="auto-recommend-section">
          <el-checkbox v-model="useAutoRecommend" @change="selectedExpirationDates = []">系统自动推荐（按近效期优先扣减）</el-checkbox>
        </div>
      </div>
      <template #footer>
        <el-button @click="expiryDialogVisible = false">取消</el-button>
        <!-- 确认按钮不用原生 disabled（禁用元素会抑制 mousedown），改用 is-disabled 样式置灰 + mousedown 快照聚焦状态，
             从而能在"直接从输入框点击确认"时识别超限回退场景，拦截首次点击并提示再次点击 -->
        <el-button type="primary" :class="{ 'is-disabled': !canConfirmExpiry }" @mousedown="onConfirmMousedown" @click="confirmExpirySelection">确认</el-button>
      </template>
    </el-dialog>

    <!-- 服务项目耗材效期选择弹窗（列表式：一个窗口按耗材分行展示，每行独立选择效期）
         服务项目商品加购 / 项目卡核销 / 预约转单共用；需求数量 = BOM单次消耗量 × 服务数量/核销次数 -->
    <el-dialog v-model="consumableDialogVisible" title="选择耗材效期" width="680px" :close-on-click-modal="false" class="expiry-dialog" @closed="onConsumableDialogClosed">
      <div v-if="consumableRows.length" class="consumable-list">
        <div v-for="(row, idx) in consumableRows" :key="row.productId" class="consumable-block">
          <div class="consumable-header">
            <span class="consumable-name">{{ row.productName }}</span>
            <span class="consumable-need">需求 {{ row.needQuantity }} 件</span>
            <el-tag v-if="row.insufficient" size="small" type="danger">库存不足</el-tag>
          </div>
          <!-- 效期选项列表（交互与实物商品一致：点选批次切换为手选；勾选自动推荐按 FEFO 扣减） -->
          <div class="expiry-options-list">
            <div
              v-for="option in row.options"
              :key="option.expirationDate ?? 'no-expiry'"
              class="expiry-option"
              :class="{ selected: !row.useAutoRecommend && row.selectedDates.includes(option.expirationDate) }"
              @click="toggleConsumableExpiry(idx, option.expirationDate)"
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
          <div class="consumable-auto">
            <el-checkbox v-model="row.useAutoRecommend" @change="row.selectedDates = []">系统自动推荐（按近效期优先扣减）</el-checkbox>
          </div>
          <div v-if="!row.useAutoRecommend && row.selectedDates.length > 0" class="consumable-selected">
            <div class="selected-title">已选（按扣减顺序）：</div>
            <div v-for="(date, di) in row.selectedDates" :key="date ?? 'no-expiry'" class="selected-item">
              <span>{{ di + 1 }}. {{ date ? date.slice(0, 10) : '无效期限制' }}</span>
              <el-button link size="small" type="danger" @click="removeConsumableExpiry(idx, di)">移除</el-button>
            </div>
            <div class="selected-summary">
              已选总和: {{ consumableRowSelectedTotal(row) }} 件 / 需要 {{ row.needQuantity }} 件
              <el-tag v-if="consumableRowSelectedTotal(row) < row.needQuantity" size="small" type="danger">不足</el-tag>
              <el-tag v-else size="small" type="success">充足</el-tag>
            </div>
          </div>
        </div>
      </div>
      <template #footer>
        <el-button @click="cancelConsumableDialog">取消</el-button>
        <el-button type="primary" :disabled="!canConfirmConsumables" @click="confirmConsumableDialog">确认</el-button>
      </template>
    </el-dialog>

    <!-- 挂单弹窗（保存当前购物车草稿，挂单后清空购物车与会员，可随时取单恢复） -->
    <el-dialog
      v-model="holdDialogVisible"
      title="挂单"
      width="440px"
      :close-on-click-modal="false"
      class="hold-dialog"
    >
      <div class="hold-tip">
        将保存当前购物车（{{ cart.length }} 件商品）为挂单，挂单后购物车与会员会被清空，可在「取单」中随时恢复。
      </div>
      <el-input
        v-model="holdRemark"
        placeholder="备注（选填）：客户称呼 / 电话，便于取单时识别"
        maxlength="200"
        show-word-limit
        clearable
      />
      <template #footer>
        <el-button @click="holdDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="confirmHoldOrder">确认挂单</el-button>
      </template>
    </el-dialog>

    <!-- 取单弹窗（门店内共享挂单列表，取单恢复购物车与会员，可取消挂单） -->
    <el-dialog
      v-model="resumeDialogVisible"
      title="取单"
      width="1000px"
      :close-on-click-modal="false"
      class="resume-dialog"
    >
      <div class="resume-search">
        <el-input
          v-model="parkedKeyword"
          placeholder="搜索挂单号 / 备注 / 会员姓名"
          clearable
          @keyup.enter="handleResumeSearch"
          @clear="handleResumeSearch"
        >
          <template #append>
            <el-button @click="handleResumeSearch">搜索</el-button>
          </template>
        </el-input>
      </div>
      <el-table
        v-loading="parkedLoading"
        :data="parkedList"
        empty-text="暂无挂单"
      >
        <el-table-column prop="parkNo" label="挂单号" width="130" />
        <el-table-column label="备注" min-width="140" show-overflow-tooltip>
          <template #default="{ row }">
            <span>{{ row.remark || '-' }}</span>
          </template>
        </el-table-column>
        <el-table-column label="会员" width="100">
          <template #default="{ row }">
            <span>{{ row.customerName || '散客' }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="itemCount" label="行数" width="70" />
        <el-table-column label="金额" width="110">
          <template #default="{ row }">
            <span>¥{{ formatPrice(row.totalAmount) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="挂单人" width="100">
          <template #default="{ row }">
            <span>{{ row.createdByName || '-' }}</span>
          </template>
        </el-table-column>
        <el-table-column label="挂单时间" width="140">
          <template #default="{ row }">
            <span>{{ formatDateTime(row.heldTime) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="120" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="confirmResumeOrder(row)">取单</el-button>
            <el-button link type="danger" @click="handleCancelParked(row)">取消</el-button>
          </template>
        </el-table-column>
      </el-table>
      <div class="resume-pagination">
        <el-pagination
          v-model:current-page="parkedPageIndex"
          :page-size="parkedPageSize"
          :total="parkedTotal"
          layout="total, prev, pager, next"
          @current-change="handleResumePaged"
        />
      </div>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage, ElMessageBox, type TableInstance, type FormInstance, type FormRules } from 'element-plus'
import { Search, Box, ShoppingCart, Wallet, ChatDotRound, CreditCard, Money, Coin, User, Ticket, Pointer, Warning } from '@element-plus/icons-vue'
import { getProducts, getCategoryTree } from '@/api/product'
import type { Product, ProductCategory } from '@/api/product/types'
import { getCustomer, getCustomers, getCustomerLevels, getPointsRule, type Customer, type CustomerLevel, type PointsRule } from '@/api/customer'
import { getMemberAccounts } from '@/api/member'
import { type OrderCreate, type ConsumableExpiry } from '@/api/order'
import { getTechniciansAvailableByService, type Technician } from '@/api/technician'
import {
  getTreatmentCardSales,
  getTreatmentCardConfigs,
  type TreatmentCardSale,
  type TreatmentCardConfig,
  type TreatmentCardVerifyItemInput
} from '@/api/treatment-card'
import { posCheckout, type PosCheckoutCreate, type PosCheckoutResult, type PosCheckoutVerify } from '@/api/pos-checkout'
import { getAppointments, getAppointment, getRooms, type Appointment, type RoomOption } from '@/api/appointment'
import { getAllEquipments, getAvailableEquipmentsByService } from '@/api/equipment'
import type { Equipment } from '@/api/equipment/types'
import { getResourceAvailability, type ResourceAvailabilityDto } from '@/api/resource'
import { formatDateTimeToTime, toLocalDateTime } from '@/utils/time'
import { formatDateTime } from '@/utils/date'
import { roundMoney } from '@/utils/money'
import { getProductExpiryOptions, type ProductExpiryOption } from '@/api/inventory'
import { getBomList } from '@/api/bom'
import { getActivityOptions, type ActivityOption } from '@/api/activity'
import { createParkedOrder, getParkedOrders, resumeParkedOrder, cancelParkedOrder, type ParkedOrder } from '@/api/parked-order'

// ==================== 商品数据 ====================
interface POSProduct {
  id: number
  /** 商品主档ID（服务内容弹窗按此反查 ServiceProduct 过滤技师技能/设备类型） */
  masterId: number
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
// 商品/赠品/项目卡 切换模式：'product'=商品列表，'gift'=赠品列表，'treatment'=项目卡列表
const productMode = ref<'product' | 'gift' | 'treatment'>('product')

// 分类ID -> 自身+所有子孙分类ID集合（本地过滤时选中顶级分类可命中子分类商品，与后端 ProductAppService 展开逻辑对齐）
const categoryIdSets = new Map<number, Set<number>>()

// 递归收集分类节点及其所有子孙的 ID
const collectCategoryIds = (node: ProductCategory, set: Set<number>) => {
  set.add(node.id)
  node.children?.forEach(child => collectCategoryIds(child, set))
}

// 加载商品分类（Tab 展示顶级分类；同时构建展开集合供 filteredProducts 过滤）
const loadCategories = async () => {
  try {
    const tree = await getCategoryTree()
    categories.value = [{ id: 0, name: '全部' }, ...tree.map(c => ({ id: c.id, name: c.name }))]
    categoryIdSets.clear()
    tree.forEach(node => {
      const set = new Set<number>()
      collectCategoryIds(node, set)
      categoryIdSets.set(node.id, set)
    })
  } catch (e) {
    ElMessage.error('加载分类失败: ' + (e as Error).message)
  }
}

// 商品模式类型标签（POS 商品列表仅含 Type=1 实物 / 2 服务 / 3 耗材，样品赠品已过滤）
const PRODUCT_TYPE_LABELS: Record<number, string> = {
  1: '实物商品',
  2: '服务项目',
  3: '耗材'
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
        masterId: p.masterId,
        name: p.name,
        code: p.code,
        spec: p.spec || '',
        price: p.price,
        stockText: PRODUCT_TYPE_LABELS[p.type] ?? '商品',
        categoryId: p.categoryId,
        productType: p.type,
        colorClass: colorClasses[idx % colorClasses.length]
      }))
  } catch (e) {
    ElMessage.error('加载商品失败: ' + (e as Error).message)
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
        masterId: p.masterId,
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
  } catch (e) {
    ElMessage.error('加载赠品失败: ' + (e as Error).message)
  }
}

// ==================== 项目卡列表（POS 项目卡 Tab） ====================
// 项目卡展示项：在配置基础上附加前端卡片样式类与项目摘要文本
interface TreatmentCardItem extends TreatmentCardConfig {
  colorClass: string
  itemSummary: string
}

// 已启用的项目卡配置列表（来源与"项目卡配置"页面一致，后端按当前门店过滤）
const treatmentCards = ref<TreatmentCardItem[]>([])

// 加载已启用的项目卡配置（列表接口已填充项目明细 items，含 productName）
const loadTreatmentCards = async () => {
  try {
    const res = await getTreatmentCardConfigs({ isEnabled: true, pageIndex: 1, pageSize: 1000 })
    const colorClasses = ['blue', 'purple', 'orange', 'cyan']
    treatmentCards.value = res.list.map((c, idx) => ({
      ...c,
      colorClass: colorClasses[idx % colorClasses.length],
      itemSummary: c.serviceItems
        || (c.items.length > 0 ? c.items.map(i => i.productName).join('、') : '无项目')
    }))
  } catch (e) {
    ElMessage.error('加载项目卡失败: ' + (e as Error).message)
  }
}

// 过滤后的项目卡列表（仅关键字过滤；项目卡无商品分类概念，不参与分类 Tab）
const filteredTreatmentCards = computed(() => {
  const keyword = searchKeyword.value.trim().toLowerCase()
  if (!keyword) return treatmentCards.value
  return treatmentCards.value.filter(c => c.name.toLowerCase().includes(keyword))
})

// 切换商品/赠品/项目卡 模式
const switchProductMode = (mode: 'product' | 'gift' | 'treatment') => {
  productMode.value = mode
  // 切换时清空搜索关键字，避免另一个列表的过滤条件残留
  searchKeyword.value = ''
}

// 过滤后的商品/赠品列表（根据当前模式返回不同数据源；项目卡模式走 filteredTreatmentCards，不经过本函数）
const filteredProducts = computed(() => {
  const source = productMode.value === 'gift' ? allGifts.value : allProducts.value
  let result = source
  // 商品与赠品同为 Product 表记录，均具备商品分类属性，分类筛选对两种模式都生效
  if ((productMode.value === 'product' || productMode.value === 'gift') && activeCategory.value !== 0) {
    // 选中顶级分类时同时命中其所有子孙分类下的商品/赠品
    const idSet = categoryIdSets.get(activeCategory.value) ?? new Set([activeCategory.value])
    result = result.filter(p => idSet.has(p.categoryId))
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

const handleCategoryChange = (catId: number) => {
  activeCategory.value = catId
}

// ==================== 客户等级缓存 ====================
const customerLevels = ref<CustomerLevel[]>([])

const loadCustomerLevels = async () => {
  try {
    customerLevels.value = await getCustomerLevels()
  } catch (e) {
    // 静默失败，不影响主流程
    console.warn('加载客户等级失败:', (e as Error).message)
  }
}

// ==================== 活动选项（赠品关联活动用） ====================
const activityOptions = ref<ActivityOption[]>([])
const loadActivityOptions = async () => {
  try {
    activityOptions.value = await getActivityOptions()
  } catch (e) {
    // 静默失败，不影响主流程
    console.warn('加载活动选项失败:', (e as Error).message)
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

const selectedMember = ref<Member | null>(null)
// 选中的会员ID（绑定 el-select v-model，清空时为 null）
const selectedMemberId = ref<number | null>(null)
// 远程搜索候选列表
const memberResults = ref<Customer[]>([])
const memberLoading = ref(false)

/**
 * 远程搜索会员（输入关键词实时按手机号模糊查询，结果作为候选列表供选择）
 * @param keyword 手机号关键词
 */
const searchMembers = async (keyword?: string) => {
  const kw = (keyword || '').trim()
  if (!kw) {
    memberResults.value = []
    return
  }
  memberLoading.value = true
  try {
    const res = await getCustomers({ phone: kw, pageIndex: 1, pageSize: 10 })
    memberResults.value = res.list
  } catch (e) {
    memberResults.value = []
    ElMessage.error('会员查询失败: ' + (e as Error).message)
  } finally {
    memberLoading.value = false
  }
}

/**
 * 从候选列表中确认选择会员：查询储值余额并回填会员信息
 * @param customerId 选中的客户ID
 */
const handleMemberSelect = async (customerId: number) => {
  const customer = memberResults.value.find(c => c.id === customerId)
  if (!customer) return
  // 切换会员（散客→会员 或 会员A→会员B）保留购物车：购物车行价格为标准价，会员折扣在结算时按当前选中会员动态计算（见 discountAmount），无需清空重加
  let balance = 0
  try {
    const accounts = await getMemberAccounts({ customerId, pageIndex: 1, pageSize: 1 })
    if (accounts.list.length > 0) {
      balance = accounts.list[0].balance
    }
  } catch {
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
}

const clearMember = (clearCart = false) => {
  // 用户主动移除会员（会员→散客）时同时清空购物车：部分商品需会员验证才可选购，移除会员后需重新加购，避免绕过验证
  // 挂单/取单等内部调用不传参，购物车已清空或正在被挂单快照恢复，不应触发清空
  if (clearCart && cart.value.length > 0) {
    cart.value = []
    ElMessage.success('已移除会员，购物车已清空')
  }
  selectedMember.value = null
  selectedMemberId.value = null
}

// ==================== 购物车 ====================
// 购物车行类型：product=商品/服务行，verify=项目卡核销行，sale=项目卡开卡行（项目卡入购物车方案）
type CartLineType = 'product' | 'verify' | 'sale'

/** 效期批次扣减明细（按扣减顺序；手选按用户选择顺序，自动推荐按 FEFO 近效期优先，前端批次详情弹窗展示用） */
interface ExpiryBatchDetail {
  /** 批次效期（null 表示"无效期限制"批次） */
  expirationDate: string | null
  /** 该批次确认时的在库库存（扣减前） */
  stock: number
  /** 该批次扣减数量 */
  quantity: number
  /** 扣减后剩余库存（stock - quantity） */
  remainingStock: number
}

interface CartItem {
  /** 购物车行唯一标识（所有行统一生成，用于渲染 key 与删除/结算行匹配；同一商品选择不同资源会产生多行，故不能以商品ID作行标识） */
  lineId: string
  /** 业务ID（商品行为商品ID，结算时透传 productId；核销/开卡行为字符串前缀+唯一值，不参与行匹配） */
  id: number | string
  name: string
  code: string
  spec: string
  price: number
  quantity: number
  productType: number  // 1=实物商品，2=服务项目（核销/开卡行不参与商品计算，仅占位）
  /** 行类型：product=商品/服务行，verify=项目卡核销行，sale=项目卡开卡行 */
  lineType: CartLineType
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
  /** 服务开始时间（服务内容弹窗/预约转单录入，提交时透传给 OrderItemCreate.serviceStartTime，ISO 字符串，本地拼接禁用 toISOString） */
  serviceStartTime?: string
  /** 服务结束时间（开始时间 + 服务时长自动计算，ISO 字符串，可空由后端推算） */
  serviceEndTime?: string
  /** 服务明细备注（服务内容弹窗录入，提交时透传给 OrderItemCreate.remark） */
  remark?: string
  /** 源预约ID（预约转订单时记录，后端 OrderAppService 据此复制技师/房间/设备到 OrderItem） */
  sourceAppointmentId?: number
  expirationDates?: (string | null)[]  // 店员选择的效期列表（按扣减顺序），undefined 表示系统自动推荐；null 元素表示"无效期限制"批次
  /** 效期批次扣减数量明细（按扣减顺序，确认效期时按 FEFO 计算；undefined 表示自动推荐/未选批次，仅供详情弹窗展示） */
  expiryDetails?: ExpiryBatchDetail[]
  /** 关联活动ID（仅赠品 Type=5 有意义，用于活动维度归因统计，非必填） */
  activityId?: number | null
  /** 服务项目绑定耗材的效期选择（仅服务项目 Type=2 有意义；结算时透传给 OrderItemCreate.consumableExpiries） */
  consumableExpiries?: ConsumableExpiry[]

  // ---- 核销行专属字段（lineType === 'verify'）----
  /** 项目卡销售记录ID（核销结算时传给 verifyTreatmentCard） */
  cardSaleId?: number
  /** 卡名（展示用，从项目卡配置匹配） */
  cardName?: string
  /** 剩余次数（展示用） */
  remainingTimes?: number
  /** 核销项目列表（productName 仅前端展示用；提交时透传技师/房间/设备/服务时间给 verifyTreatmentCard） */
  verifyItems?: (TreatmentCardVerifyItemInput & { productName?: string })[]

  // ---- 开卡行专属字段（lineType === 'sale'）----
  /** 项目卡配置ID（开卡结算时传给 createTreatmentCardSale） */
  cardId?: number
  /** 开卡售价（线下独立收款，不进入系统支付，仅参与应付金额展示） */
  saleAmount?: number
  /** 开卡备注 */
  saleRemark?: string
  /** 开卡客户ID（随行固化，避免结算时会员已变更导致丢失） */
  saleCustomerId?: number
  /** 开卡客户姓名（展示用） */
  saleCustomerName?: string
}

const cart = ref<CartItem[]>([])

// 购物车行唯一ID生成器（会话内递增，商品ID参与可读性；行匹配一律用 lineId 而非 id）
let cartLineSeq = 0
const genCartLineId = (base: number | string): string => `p-${base}-${++cartLineSeq}`

const addToCart = async (product: POSProduct, opts?: { skipDialog?: boolean }): Promise<CartItem | undefined> => {
  // 服务项目：点击卡片需先通过服务内容弹窗录入技师/房间/设备/时间，确认后加入购物车
  // 预约转单（skipDialog）等场景直接加购，不弹窗（弹窗确认时的"同配置数量+1"见 confirmServiceDialog）
  if (product.productType === 2) {
    if (opts?.skipDialog) {
      return pushProductToCart(product)
    } else {
      openServiceDialog(product)
    }
    return undefined
  }

  // 除服务项目外，其余类型（实物 1/耗材 3/赠品 5）均需先选择效期批次
  // 赠品走与实物商品相同的 FEFO+FIFO 批次扣减逻辑，后端 DeductSampleGiftOutAsync 按效期扣减
  const existing = cart.value.find(item => item.id === product.id)
  if (existing) {
    // 再次点击商品卡片等同购物车数量+1：复用 changeQty 的库存上限校验与批次明细刷新，避免超库存加购
    // 数量已达当前所选批次库存上限、且该商品还有其他未选择批次有库存时，直接打开效期编辑弹窗供店员加选批次后继续加购（否则 changeQty 仅提示"库存不足"而无法加购）
    const refreshed = await refreshCartItemExpiry(existing)
    if (refreshed) {
      const totalStock = refreshed.options.reduce((sum, o) => sum + o.totalQuantity, 0)
      if (existing.quantity + 1 > refreshed.maxStock && refreshed.maxStock < totalStock) {
        openExpiryDialog(product, { editing: existing })
        return undefined
      }
    }
    changeQty(existing, 1)
    return undefined
  }
  openExpiryDialog(product)
  return undefined
}

/**
 * 直接加入购物车（不弹服务内容弹窗），始终新增一行并返回该行
 * 用于服务项目预约转单等已确定资源的场景；调用方设置资源后自行按配置合并相同行（见 addAppointmentToCart）
 * @param product 商品信息
 * @returns 新加入的购物车行
 */
const pushProductToCart = (product: POSProduct): CartItem => {
  const item: CartItem = {
    lineId: genCartLineId(product.id),
    id: product.id,
    name: product.name,
    code: product.code,
    spec: product.spec,
    price: product.price,
    quantity: 1,
    productType: product.productType,
    lineType: 'product'
  }
  cart.value.push(item)
  return item
}

const changeQty = async (item: CartItem, delta: number) => {
  const next = item.quantity + delta
  if (next <= 0) {
    removeFromCart(item)
    return
  }
  // 服务项目行：数量变化联动校验绑定耗材库存并重算需求（未绑定耗材/未选效期直接改数量）
  if (item.productType === 2) {
    const err = await checkServiceConsumableCapacity(item, next)
    if (err) {
      ElMessage.warning(err)
      return
    }
    item.quantity = next
    return
  }
  // 增减数量后重算批次明细，保证"批次详情"弹窗与购物车数量保持一致
  const refreshed = await refreshCartItemExpiry(item)
  if (refreshed) {
    // 增加数量校验：不能超过该商品当前可用库存上限
    if (delta > 0 && next > refreshed.maxStock) {
      ElMessage.warning(`库存不足：该商品当前最多可售 ${refreshed.maxStock} 件`)
      return
    }
    item.quantity = next
    item.expiryDetails = allocateExpiryQuantities(refreshed.orderedDates, next, refreshed.options)
  } else {
    item.quantity = next
  }
}

const onQtyInput = async (item: CartItem) => {
  if (!item.quantity || item.quantity < 1) {
    item.quantity = 1
  }
  // 服务项目行：手输数量后校验绑定耗材库存（不足回退到原值并提示；原值在聚焦时记录）
  if (item.productType === 2) {
    const baseQuantity = cartQtyEditOrigin?.lineId === item.lineId ? cartQtyEditOrigin.quantity : item.quantity
    const err = await checkServiceConsumableCapacity(item, item.quantity, baseQuantity)
    if (err) {
      ElMessage.warning(err)
      item.quantity = baseQuantity
    }
    cartQtyEditOrigin = null
    return
  }
  const refreshed = await refreshCartItemExpiry(item)
  if (refreshed) {
    // 手动输入数量不能超过该商品当前可用库存上限，超出时回退到上限
    if (item.quantity > refreshed.maxStock) {
      ElMessage.warning(`库存不足：该商品当前最多可售 ${refreshed.maxStock} 件`)
      item.quantity = refreshed.maxStock
    }
    item.expiryDetails = allocateExpiryQuantities(refreshed.orderedDates, item.quantity, refreshed.options)
  }
}

const removeFromCart = (item: CartItem) => {
  // 行匹配按 lineId（同一商品不同资源配置会存在多行，按 id 只能删到第一行）
  const index = cart.value.findIndex(i => i.lineId === item.lineId)
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

// ==================== 挂单 / 取单 ====================
// 挂单：将当前购物车（含会员）保存为门店内共享的挂单草稿，清空购物车与会员；
// 取单：从门店内挂单列表恢复购物车与会员，继续结算。挂单不预占库存/技师/房间资源，结算时由后端做最终校验。

// ---- 挂单弹窗状态 ----
const holdDialogVisible = ref(false)
const holdRemark = ref('')

// ---- 取单弹窗状态 ----
const resumeDialogVisible = ref(false)
const parkedList = ref<ParkedOrder[]>([])
const parkedTotal = ref(0)
const parkedKeyword = ref('')
const parkedPageIndex = ref(1)
const parkedPageSize = ref(10)
const parkedLoading = ref(false)

/** 加载门店内挂起状态的挂单列表（分页 + 关键字搜索） */
const loadParkedOrders = async () => {
  parkedLoading.value = true
  try {
    const res = await getParkedOrders({
      keyword: parkedKeyword.value,
      pageIndex: parkedPageIndex.value,
      pageSize: parkedPageSize.value
    })
    parkedList.value = res.list
    parkedTotal.value = res.total
  } catch (e) {
    parkedList.value = []
    parkedTotal.value = 0
    ElMessage.error('挂单列表加载失败: ' + (e as Error).message)
  } finally {
    parkedLoading.value = false
  }
}

/** 挂单：已选会员时自动携带会员信息直接挂单（不再弹窗录入备注）；散客场景保留备注弹窗 */
const handleHoldOrder = async () => {
  if (cart.value.length === 0) {
    ElMessage.warning('购物车为空')
    return
  }
  if (selectedMember.value) {
    // 已选会员：自动将会员信息填充进挂单，并将会员手机号写入备注，直接挂单
    await doHoldOrder(selectedMember.value.id, selectedMember.value.name, selectedMember.value.phone)
  } else {
    // 散客：打开备注弹窗，便于录入客户称呼/电话用于取单识别
    holdRemark.value = ''
    holdDialogVisible.value = true
  }
}

/** 执行挂单提交：携带会员信息与备注提交购物车快照，成功后清空购物车与会员 */
const doHoldOrder = async (customerId?: number, customerName?: string, customerPhone?: string) => {
  try {
    await createParkedOrder({
      customerId,
      customerName,
      // 已选会员挂单时自动将手机号写入备注，便于取单列表识别；散客场景取用户输入的备注
      remark: customerPhone || holdRemark.value.trim() || undefined,
      cartJson: JSON.stringify(cart.value)
    })
    cart.value = []
    clearMember()
    holdDialogVisible.value = false
    ElMessage.success('已挂单')
  } catch (e) {
    ElMessage.error('挂单失败: ' + (e as Error).message)
  }
}

/** 确认挂单（弹窗内确认）：提交购物车快照 */
const confirmHoldOrder = () => {
  doHoldOrder(selectedMember.value?.id, selectedMember.value?.name)
}

/** 取单：打开挂单列表弹窗并加载 */
const handleResumeOrder = () => {
  resumeDialogVisible.value = true
  parkedPageIndex.value = 1
  loadParkedOrders()
}

/** 搜索挂单（重置到第一页） */
const handleResumeSearch = () => {
  parkedPageIndex.value = 1
  loadParkedOrders()
}

/** 挂单列表翻页 */
const handleResumePaged = (page: number) => {
  parkedPageIndex.value = page
  loadParkedOrders()
}

/** 恢复会员上下文：按 customerId 重新查询最新客户信息（余额等）构造 Member */
const restoreMember = async (customerId: number) => {
  try {
    const customer = await getCustomer(customerId)
    let balance = 0
    try {
      const accounts = await getMemberAccounts({ customerId, pageIndex: 1, pageSize: 1 })
      if (accounts.list.length > 0) {
        balance = accounts.list[0].balance
      }
    } catch {
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
    selectedMemberId.value = customer.id
    // 回填候选列表，使顶部会员输入框通过 el-option 匹配回显会员姓名而非裸 ID
    memberResults.value = [customer]
  } catch {
    // 挂单关联会员不存在（已删除等）则按散客恢复，不阻塞取单
    clearMember()
    ElMessage.warning('挂单关联会员不存在，已按散客恢复')
  }
}

/** 取单：覆盖当前购物车为挂单快照，恢复会员上下文 */
const confirmResumeOrder = async (order: ParkedOrder) => {
  // 当前购物车非空时提示将被覆盖
  if (cart.value.length > 0) {
    try {
      await ElMessageBox.confirm('当前购物车非空，取单将覆盖当前购物车，是否继续？', '提示', {
        type: 'warning',
        confirmButtonText: '继续',
        cancelButtonText: '取消'
      })
    } catch {
      return
    }
  }
  try {
    const resumed = await resumeParkedOrder(order.id)
    // 恢复购物车（cartJson 为挂单时 JSON.stringify(cart) 的快照，直接反序列化）
    try {
      const parsed = JSON.parse(resumed.cartJson || '[]')
      cart.value = Array.isArray(parsed) ? (parsed as CartItem[]) : []
    } catch {
      cart.value = []
      ElMessage.warning('挂单数据解析失败，购物车已清空')
    }
    // 恢复会员（散客清空）
    if (resumed.customerId) {
      await restoreMember(resumed.customerId)
    } else {
      clearMember()
    }
    resumeDialogVisible.value = false
    ElMessage.success('已取单')
  } catch (e) {
    ElMessage.error('取单失败: ' + (e as Error).message)
  }
}

/** 取消挂单（确认后置为已取消并从列表移除） */
const handleCancelParked = async (order: ParkedOrder) => {
  try {
    await ElMessageBox.confirm(`确定取消挂单 ${order.parkNo} 吗？`, '提示', {
      type: 'warning',
      confirmButtonText: '确定',
      cancelButtonText: '取消'
    })
  } catch {
    return
  }
  try {
    await cancelParkedOrder(order.id)
    ElMessage.success('挂单已取消')
    loadParkedOrders()
  } catch (e) {
    ElMessage.error('取消失败: ' + (e as Error).message)
  }
}

// ==================== 效期选择弹窗 ====================
const expiryDialogVisible = ref(false)
const pendingProduct = ref<POSProduct | null>(null)
const expiryOptions = ref<ProductExpiryOption[]>([])
// null 表示"无效期限制"批次，与 ProductExpiryOption.expirationDate 对齐
const selectedExpirationDates = ref<(string | null)[]>([])
const useAutoRecommend = ref(false)
const expiryQuantity = ref(1)
// 弹窗内临时关联活动ID（仅赠品 Type=5 有意义，确认时写入购物车项 activityId）
const pendingActivityId = ref<number | null>(null)
// 效期弹窗编辑模式标志：记录待更新购物车行 lineId；null 表示新增模式（确认时 push 新行），非 null 时更新已有行（见 confirmExpirySelection）
let expiryEditingLineId: string | null = null

// 数量仅支持正整数：拦截小数与科学计数法按键（. e E + -），change 时兜底取整（防粘贴小数）
const onExpiryQtyKeydown = (e: KeyboardEvent) => {
  if (['.', 'e', 'E', '+', '-'].includes(e.key)) e.preventDefault()
}
// 数量输入超限被回退后置位（自动推荐/手选模式通用，用于确认时判断是否刚被自动调整）
let expiryQtyAdjusted = false
// 数量输入框是否聚焦（区分"直接从输入框点击确认"与"已失焦后点击确认"）
const expiryQtyInputFocused = ref(false)
// 点击确认按钮瞬间输入框的聚焦状态快照（确认按钮未用原生 disabled，mousedown 必然触发，可据此判断点击前输入框是否仍聚焦）
let confirmMousedownFocused = false

const onExpiryQtyChange = () => {
  if (typeof expiryQuantity.value === 'number' && !Number.isInteger(expiryQuantity.value)) {
    expiryQuantity.value = Math.floor(expiryQuantity.value)
  }
  // 数量上限：自动推荐模式=全部批次库存合计，手选模式=已选效期库存合计；输入数量超限时静默回退到上限并置"已自动调整"标志
  // 手选未选批次时上限为0，回退到下限1（确认仍由 canConfirmExpiry 拦截）
  // （是否拦截/提示由 confirmExpirySelection 结合输入框聚焦状态决定，避免失焦与点击确认时重复提示）
  const limit = useAutoRecommend.value ? expiryTotalStock.value : selectedExpiryTotal.value
  if (expiryQuantity.value > limit) {
    expiryQuantity.value = Math.max(1, limit)
    expiryQtyAdjusted = true
  } else {
    expiryQtyAdjusted = false
  }
}

// 记录点击确认按钮瞬间输入框是否仍聚焦：直接从输入框点按钮时为 true，已失焦（先点了其他位置）后为 false
const onConfirmMousedown = () => {
  confirmMousedownFocused = expiryQtyInputFocused.value
}

// 数量 -1（下限为 1）；用户主动操作视为已确认数量，清除"已自动调整"标志
const onExpiryQtyDecrease = () => {
  expiryQtyAdjusted = false
  expiryQuantity.value = Math.max(1, expiryQuantity.value - 1)
}

// 数量 +1 不能超过当前模式上限（自动推荐=全部批次库存合计；手选=已选效期库存合计，可通过加选批次提高上限）
// 用户主动点击 + 视为已确认数量，清除"已自动调整"标志
const onExpiryQtyIncrease = () => {
  expiryQtyAdjusted = false
  const limit = useAutoRecommend.value ? expiryTotalStock.value : selectedExpiryTotal.value
  if (expiryQuantity.value >= limit) {
    ElMessage.warning(limit > 0 ? `库存不足：该商品当前最多可售 ${limit} 件` : '库存不足：请先选择效期批次')
    return
  }
  expiryQuantity.value++
}

// 弹窗标题（实物商品/赠品共用，除关联活动外行为一致）
const expiryDialogTitle = computed(() => '效期选择')

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

// 全部效期批次库存合计（"系统自动推荐"模式的数量上限）
const expiryTotalStock = computed(() => {
  return expiryOptions.value.reduce((sum, o) => sum + o.totalQuantity, 0)
})

// 是否可以确认效期选择（实物商品/赠品一致：仅按"自动推荐/手选"模式区分）
const canConfirmExpiry = computed(() => {
  if (!pendingProduct.value) return false
  if (expiryQuantity.value < 1) return false
  // 自动推荐数量上限为全部批次库存合计，超限不允许确认；失焦回退到上限后按钮恢复可用
  if (useAutoRecommend.value) return expiryQuantity.value <= expiryTotalStock.value
  // 手选模式须先选效期，且数量不能超过已选效期库存合计
  if (selectedExpirationDates.value.length === 0) return false
  return selectedExpiryTotal.value >= expiryQuantity.value
})

const openExpiryDialog = async (product: POSProduct, opts?: { editing?: CartItem }) => {
  const editing = opts?.editing
  pendingProduct.value = product
  // 编辑模式回填当前行的选择；新增模式重置
  selectedExpirationDates.value = editing?.expirationDates ? [...editing.expirationDates] : []
  // 新增模式默认勾选"系统自动推荐"（实物/耗材/赠品一致）；编辑模式沿用已有行的选择（有具体批次即非自动推荐）
  useAutoRecommend.value = editing?.expirationDates ? false : true
  expiryQuantity.value = editing?.quantity ?? 1
  pendingActivityId.value = editing?.activityId ?? null
  expiryOptions.value = []
  // 编辑模式记录待更新行（confirmExpirySelection 据此更新已有行而非新增）
  expiryEditingLineId = editing?.lineId ?? null
  // 重置"输入超限被自动回退"标志与输入框聚焦状态（每次打开弹窗都是全新状态）
  expiryQtyAdjusted = false
  expiryQtyInputFocused.value = false
  confirmMousedownFocused = false

  try {
    const options = await getProductExpiryOptions(product.id)
    expiryOptions.value = options
    if (options.length === 0) {
      ElMessage.warning(`商品 ${product.name} 暂无可用效期库存`)
      pendingProduct.value = null
      return
    }
    // 新增模式且非自动推荐时默认选中推荐项（编辑模式回填用户选择，不预选）
    if (!editing && !useAutoRecommend.value) {
      const recommended = options.find(o => o.isRecommended)
      if (recommended) {
        selectedExpirationDates.value = [recommended.expirationDate]
      }
    }
  } catch (e) {
    ElMessage.error('加载效期信息失败: ' + (e as Error).message)
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

/**
 * 按扣减顺序分配每个效期批次的扣减数量（顺序优先，前一批次先扣满，与后端 FEFO 语义一致）
 * @param dates 扣减的效期序列（按扣减顺序，null 表示"无效期限制"批次）
 * @param need 需要扣减的总数量
 * @param options 效期批次选项（默认取弹窗内存数据；重新加载场景需显式传入）
 * @returns 每批次的扣减明细（仅包含扣减数量 > 0 的批次）
 */
const allocateExpiryQuantities = (dates: (string | null)[], need: number, options: ProductExpiryOption[] = expiryOptions.value): ExpiryBatchDetail[] => {
  const result: ExpiryBatchDetail[] = []
  let remaining = need
  for (const date of dates) {
    if (remaining <= 0) break
    const option = date === null
      ? options.find(o => o.isNoExpiry === true)
      : options.find(o => !o.isNoExpiry && o.expirationDate!.startsWith(date.slice(0, 10)))
    const stock = option?.totalQuantity ?? 0
    const qty = Math.min(remaining, stock)
    if (qty > 0) {
      result.push({ expirationDate: date, quantity: qty, stock, remainingStock: stock - qty })
      remaining -= qty
    }
  }
  return result
}

/**
 * 重新加载购物车行当前的可用效期库存，返回库存上限与扣减序列
 * 用于购物车增减数量后刷新"批次详情"弹窗数据，并对增加数量做上限校验
 * @param item 购物车行
 * @returns 库存上限、扣减序列、批次选项；服务行或加载失败返回 null
 */
const refreshCartItemExpiry = async (item: CartItem): Promise<{
  maxStock: number
  orderedDates: (string | null)[]
  options: ProductExpiryOption[]
} | null> => {
  // 服务项目行无效期扣减语义，直接跳过
  if (item.productType === 2) return null
  try {
    // 商品 id 实为字符串（雪花ID，LongToStringConverter 序列化），as number 仅过编译，运行时保持原值用于加载效期
    const options = await getProductExpiryOptions(item.id as number)
    if (options.length === 0) return null
    // 扣减序列：手选按用户选择顺序；自动推荐按近效期优先顺序（与后端 FEFO 一致）
    const orderedDates = item.expirationDates?.length
      ? [...item.expirationDates]
      : options.map(o => o.expirationDate)
    // 可用库存上限 = 扣减序列涉及批次的库存合计（手选仅限所选批次，自动推荐为全部批次）
    const maxStock = orderedDates.reduce((sum, date) => {
      const option = date === null
        ? options.find(o => o.isNoExpiry === true)
        : options.find(o => !o.isNoExpiry && o.expirationDate!.startsWith(date.slice(0, 10)))
      return sum + (option?.totalQuantity ?? 0)
    }, 0)
    return { maxStock, orderedDates, options }
  } catch {
    // 加载失败不阻断数量修改，仅无法校验与刷新批次明细
    return null
  }
}

const confirmExpirySelection = () => {
  if (!pendingProduct.value) return
  if (!canConfirmExpiry.value) return
  // 数量因超限被自动回退（expiryQtyAdjusted=true）时区分两种场景：
  // 场景1-直接从输入框点击确认：mousedown 时输入框仍聚焦（confirmMousedownFocused=true），回退与本次点击在同一鼠标事件序列内，
  //   按钮刚因回退"复活"，用户观感上根本看不到"按钮取消禁用"这一步，故本次不提交，提示数量已回退、请再次点击确认；
  // 场景2-输入框已失焦（用户先点了其他位置再点确认）：回退早已完成、按钮已可用，点击确认直接提交，不要求多点一次。
  // 判断依据是 mousedown 瞬间的聚焦快照而非时间差，避免正常点击因耗时超过阈值被误判为场景1
  if (expiryQtyAdjusted && confirmMousedownFocused) {
    ElMessage.warning(`库存不足：数量已自动调整为最大库存 ${expiryQuantity.value} 件，本次未提交，请再次点击确认`)
    expiryQtyAdjusted = false
    return
  }

  const isGift = pendingProduct.value.productType === 5
  // 自动推荐模式不指定批次（后端按 FEFO 扣减）；手选模式按用户选择顺序提交（实物/赠品一致）
  const expirationDates = useAutoRecommend.value ? undefined : [...selectedExpirationDates.value]
  // 批次扣减序列：手选按用户选择顺序；自动推荐按 expiryOptions 返回顺序（近效期优先、无效期末尾，与后端 FEFO 一致）
  // 无论来源均生成批次明细，供"批次详情"弹窗按扣除顺序展示扣减数量/剩余库存
  const orderedDates = useAutoRecommend.value
    ? expiryOptions.value.map(o => o.expirationDate)
    : [...selectedExpirationDates.value]
  const expiryDetails = orderedDates.length > 0
    ? allocateExpiryQuantities(orderedDates, expiryQuantity.value)
    : undefined

  if (expiryEditingLineId) {
    // 编辑模式：更新已有行（数量/效期/活动），不新增行
    const target = cart.value.find(i => i.lineId === expiryEditingLineId)
    if (target) {
      target.quantity = expiryQuantity.value
      target.expirationDates = expirationDates
      target.expiryDetails = expiryDetails
      target.activityId = isGift ? pendingActivityId.value : null
    }
  } else {
    cart.value.push({
      lineId: genCartLineId(pendingProduct.value.id),
      id: pendingProduct.value.id,
      name: pendingProduct.value.name,
      code: pendingProduct.value.code,
      spec: pendingProduct.value.spec,
      // 赠品对客户免费，前端强制 price=0；提交订单时 discountedAmount 自然也是 0
      price: isGift ? 0 : pendingProduct.value.price,
      quantity: expiryQuantity.value,
      productType: pendingProduct.value.productType,
      lineType: 'product',
      expirationDates: expirationDates,
      expiryDetails: expiryDetails,
      // 赠品（type=5）关联活动（非必填，写入 InventoryLog.ActivityId 用于活动维度归因统计）
      activityId: isGift ? pendingActivityId.value : null
    })
  }

  expiryDialogVisible.value = false
  pendingProduct.value = null
  expiryEditingLineId = null
}

// ==================== 服务项目耗材效期选择弹窗（列表式） ====================
// 服务项目商品加购 / 项目卡核销 / 预约转单共用：
// 先查服务项目 BOM（是否绑定耗材），绑定则校验耗材库存充足（任一不足阻止加购），
// 充足则打开列表式弹窗（一个窗口按耗材分行展示，每行独立选择效期，交互复用实物商品效期选项）。
// 确认后按"请求来源"分派耗材选择：服务行写入 CartItem.consumableExpiries，核销项写入 verifyItems[].consumableExpiries
interface ConsumableRow {
  /** 耗材商品ID */
  productId: number
  /** 耗材商品名称 */
  productName: string
  /** 需求来源：请求索引 -> 该请求需求数量（BOM单次消耗量 × 服务数量/核销次数） */
  sourceRequests: Map<number, number>
  /** 需求合计（各来源需求之和） */
  needQuantity: number
  /** 效期选项 */
  options: ProductExpiryOption[]
  /** 全部效期批次库存合计 */
  totalStock: number
  /** 是否库存不足（打开弹窗前已校验，仅兜底展示） */
  insufficient: boolean
  /** 是否系统自动推荐（按 FEFO 扣减）；false 为手选 */
  useAutoRecommend: boolean
  /** 手选效期（按扣减顺序，null 表示"无效期限制"批次） */
  selectedDates: (string | null)[]
}
const consumableDialogVisible = ref(false)
const consumableRows = ref<ConsumableRow[]>([])
// 打开弹窗时的请求列表（确认后按索引分派耗材选择）
let consumableRequests: { requestIndex: number; productId: number; quantity: number }[] = []
// Promise 化确认：confirmConsumableDialog 传入选择，cancelConsumableDialog 传入 null
let consumableResolve: ((value: ConsumableExpiry[][] | null) => void) | null = null

// 手选模式下行内已选效期库存合计（null 表示"无效期限制"批次，匹配 isNoExpiry 标识；非 null 按 ISO 日期前缀匹配）
const consumableRowSelectedTotal = (row: ConsumableRow): number => {
  return row.selectedDates.reduce((sum, date) => {
    const option = date === null
      ? row.options.find(o => o.isNoExpiry === true)
      : row.options.find(o => !o.isNoExpiry && o.expirationDate!.startsWith(date.slice(0, 10)))
    return sum + (option?.totalQuantity ?? 0)
  }, 0)
}

// 是否可以确认耗材效期（每行均满足：自动推荐=全部效期库存充足；手选=已选效期库存合计 ≥ 需求）
const canConfirmConsumables = computed(() => {
  if (consumableRows.value.length === 0) return false
  return consumableRows.value.every(row => {
    if (row.useAutoRecommend) return !row.insufficient && row.totalStock >= row.needQuantity
    if (row.selectedDates.length === 0) return false
    return consumableRowSelectedTotal(row) >= row.needQuantity
  })
})

const toggleConsumableExpiry = (idx: number, date: string | null) => {
  const row = consumableRows.value[idx]
  if (!row) return
  row.useAutoRecommend = false
  const i = row.selectedDates.findIndex(d => d === date)
  if (i > -1) {
    row.selectedDates.splice(i, 1)
  } else {
    row.selectedDates.push(date)
  }
}

const removeConsumableExpiry = (idx: number, dateIdx: number) => {
  consumableRows.value[idx]?.selectedDates.splice(dateIdx, 1)
}

const cancelConsumableDialog = () => {
  consumableDialogVisible.value = false
  consumableResolve?.(null)
  consumableResolve = null
  consumableRequests = []
}

// 弹窗被右上角 X / ESC 关闭（未走确认/取消按钮）时兜底 resolve(null)，避免调用方（confirmServiceDialog 等）Promise 悬挂
const onConsumableDialogClosed = () => {
  if (consumableResolve) {
    consumableResolve(null)
    consumableResolve = null
    consumableRequests = []
  }
}

const confirmConsumableDialog = () => {
  if (!canConfirmConsumables.value) return
  // 每个耗材生成一份选择（自动推荐不指定效期 → expirationDates undefined；手选按扣减顺序）
  const byProduct = new Map<number, ConsumableExpiry>()
  for (const row of consumableRows.value) {
    byProduct.set(row.productId, {
      productId: row.productId,
      productName: row.productName,
      quantity: row.needQuantity,
      expirationDates: row.useAutoRecommend ? undefined : [...row.selectedDates]
    })
  }
  // 按请求分派（同一耗材被多个请求共用时各自记入对应需求数量）
  // sourceRequests 的 key 是调用方 requestIndex（核销为 nextVerifyItems 索引，可能因 productId 为空跳过而不连续），
  // 需映射回 consumableRequests 的数组索引再写入 perRequest，避免越界
  const perRequest: ConsumableExpiry[][] = consumableRequests.map(() => [])
  for (const row of consumableRows.value) {
    for (const [reqIdx, need] of row.sourceRequests) {
      const arrIdx = consumableRequests.findIndex(r => r.requestIndex === reqIdx)
      if (arrIdx < 0) continue
      const sel = byProduct.get(row.productId)!
      perRequest[arrIdx].push({ ...sel, quantity: need })
    }
  }
  consumableDialogVisible.value = false
  consumableResolve?.(perRequest)
  consumableResolve = null
  consumableRequests = []
}

/**
 * 服务项目耗材效期选择流程（服务项目商品加购 / 项目卡核销 / 预约转单共用）
 * 步骤：逐服务项目查 BOM → 汇总各耗材需求 → 校验库存（任一耗材不足则阻止并提示）→ 打开列表式弹窗选择效期
 * @param requests 服务项目需求（requestIndex 为调用方使用的索引：服务行恒 0，核销为核销项目在列表中的索引）
 * @param previous 编辑模式回显：requestIndex -> 该请求已有的耗材效期选择（弹窗初始化时回填批次与自动推荐状态，新增模式不传）
 * @returns 按 requests 索引归属的耗材效期选择数组；无 BOM 返回 undefined；库存不足或用户取消返回 null
 */
const selectServiceConsumables = async (
  requests: { requestIndex: number; productId: number; quantity: number }[],
  previous?: Map<number, ConsumableExpiry[]>
): Promise<ConsumableExpiry[][] | undefined | null> => {
  const validRequests = requests.filter(r => r.productId && r.quantity > 0)
  if (validRequests.length === 0) return undefined

  // 1. 逐服务项目查 BOM（是否绑定耗材）
  // req.productId 是门店商品档案ID，BOM 服务端关联服务项目档案，后端 getBomList 经商品主档桥接反查
  const bomByService = new Map<number, { consumableProductId: number; consumableProductName: string; quantity: number }[]>()
  for (const req of validRequests) {
    const res = await getBomList({ productId: req.productId, pageIndex: 1, pageSize: 100 })
    if (res.list.length > 0) {
      bomByService.set(req.productId, res.list.map(b => ({
        consumableProductId: b.consumableProductId,
        consumableProductName: b.consumableProductName,
        quantity: b.quantity
      })))
    }
  }
  // 2. 所有服务项目均未绑定耗材 → 无需选择（后端按 FEFO 自动扣减或无需扣减）
  if (bomByService.size === 0) return undefined

  // 3. 汇总各耗材需求 + 加载效期选项 + 校验库存
  const rows: ConsumableRow[] = []
  const insufficient: string[] = []
  for (const req of validRequests) {
    const boms = bomByService.get(req.productId) ?? []
    for (const bom of boms) {
      let row = rows.find(r => r.productId === bom.consumableProductId)
      if (!row) {
        // 编辑模式回显：该请求已有该耗材的效期选择（手选批次则按原批次回填并关闭自动推荐，自动推荐/无选择维持默认）
        const prevCe = previous?.get(req.requestIndex)?.find(ce => ce.productId === bom.consumableProductId)
        row = {
          productId: bom.consumableProductId,
          productName: bom.consumableProductName,
          sourceRequests: new Map(),
          needQuantity: 0,
          options: [],
          totalStock: 0,
          insufficient: false,
          useAutoRecommend: !prevCe?.expirationDates?.length,
          selectedDates: prevCe?.expirationDates ? [...prevCe.expirationDates] : []
        }
        rows.push(row)
      }
      const need = bom.quantity * req.quantity
      row.sourceRequests.set(req.requestIndex, (row.sourceRequests.get(req.requestIndex) ?? 0) + need)
      row.needQuantity += need
    }
  }
  for (const row of rows) {
    const options = await getProductExpiryOptions(row.productId)
    row.options = options
    row.totalStock = options.reduce((s, o) => s + o.totalQuantity, 0)
    if (options.length === 0 || row.totalStock < row.needQuantity) {
      row.insufficient = true
      insufficient.push(`${row.productName}（需求 ${row.needQuantity} 件，当前可用 ${row.totalStock} 件）`)
    }
  }
  // 4. 任一耗材库存不足 → 阻止加购
  if (insufficient.length > 0) {
    ElMessageBox.alert(`以下服务项目绑定耗材库存不足，无法加入购物车：\n${insufficient.join('\n')}`, '耗材库存不足', {
      type: 'warning',
      confirmButtonText: '知道了'
    })
    return null
  }

  // 5. 打开列表式弹窗等待店员确认效期
  consumableRequests = validRequests
  consumableRows.value = rows
  consumableDialogVisible.value = true
  return new Promise(resolve => {
    consumableResolve = resolve
  })
}

/**
 * 服务项目行数量变化后校验绑定耗材库存并重算各耗材需求数量
 * @param item 服务项目购物车行（含 consumableExpiries）
 * @param nextQuantity 调整后的服务数量
 * @param baseQuantity 调整前数量（onQtyInput 手输时 v-model 已直接改值，需由调用方传入原值）
 * @returns 充足返回 null；不足返回错误信息（不修改状态，数量由调用方回退）
 */
const checkServiceConsumableCapacity = async (item: CartItem, nextQuantity: number, baseQuantity = item.quantity): Promise<string | null> => {
  if (item.productType !== 2 || !item.consumableExpiries?.length) return null
  // BOM 单次消耗量 = 原需求 / 原数量；按新数量重算需求并校验库存（库存只看全部可售效期合计，店员可重选效期补足）
  for (const ce of item.consumableExpiries) {
    const perUnit = baseQuantity > 0 ? ce.quantity / baseQuantity : 0
    const need = perUnit * nextQuantity
    const options = await getProductExpiryOptions(ce.productId)
    const totalStock = options.reduce((s, o) => s + o.totalQuantity, 0)
    if (totalStock < need) {
      return `耗材「${ce.productName}」库存不足：需求 ${need} 件，当前可用 ${totalStock} 件`
    }
    ce.quantity = need
  }
  return null
}

// 购物车数量输入框聚焦时记录原值（服务项目行手输数量校验失败后回退用；实物行走 maxStock 回退不需要）
let cartQtyEditOrigin: { lineId: string; quantity: number } | null = null
const onCartQtyFocus = (item: CartItem) => {
  cartQtyEditOrigin = { lineId: item.lineId, quantity: item.quantity }
}

// ==================== 合计计算 ====================
const totalQuantity = computed(() => {
  // 仅统计商品/服务行件数；核销行（扣卡次不收款）与开卡行（线下独立收款）不计入件数
  return cart.value.reduce((sum, item) => item.lineType === 'verify' ? sum : sum + item.quantity, 0)
})

const subtotal = computed(() => {
  // 商品/服务行与开卡行金额计入应付总额展示；核销行金额仅展示不参与收款（排除）
  return cart.value.reduce((sum, item) => item.lineType === 'verify' ? sum : sum + item.price * item.quantity, 0)
})

const discountAmount = computed(() => {
  if (!selectedMember.value) return 0
  const rate = selectedMember.value.discountRate / 10
  // 会员折扣仅作用于商品/服务行；核销行（扣卡次）与开卡行（线下独立收款，现状不打折）不参与折扣
  const discountBase = cart.value.reduce((sum, item) =>
    item.lineType === 'product' ? sum + item.price * item.quantity : sum, 0)
  return discountBase * (1 - rate)
})

const payableAmount = computed(() => {
  return subtotal.value - discountAmount.value
})

// ==================== 订单信息 ====================
// 订单号不再由前端生成，后端 OrderAppService 通过 OrderNoGenerator 生成正式单号（SO{yyyyMMdd}{序号}）

// ==================== 支付弹窗 ====================
const payDialogVisible = ref(false)
const selectedPayMethod = ref('wechat')
const paying = ref(false)
const isBackfill = ref(false)
const backfillDate = ref<string>('')

// ==================== 结算结果聚合展示 ====================
interface SettlementResultItem {
  /** 商品/服务订单 */
  orders: { orderNo: string; amount: number; payMethodName: string }[]
  /** 项目卡核销 */
  verifies: { cardName: string; times: number; amount: number }[]
  /** 项目卡开卡（线下收取） */
  sales: { cardName: string; amount: number }[]
}
const settlementResultVisible = ref(false)
const settlementResult = ref<{ checkoutSessionNo: string } & SettlementResultItem | null>(null)

/**
 * 生成购物车结算批次号（CS{yyyyMMddHHmmss}{3位随机}）
 * 同一批次下订单/核销/开卡三单据共用，用于跨单据聚合追溯
 * @returns 批次号
 */
const generateCheckoutSessionNo = (): string => {
  const now = new Date()
  const pad = (n: number) => String(n).padStart(2, '0')
  const ts = `${now.getFullYear()}${pad(now.getMonth() + 1)}${pad(now.getDate())}${pad(now.getHours())}${pad(now.getMinutes())}${pad(now.getSeconds())}`
  const rand = Math.floor(Math.random() * 1000).toString().padStart(3, '0')
  return `CS${ts}${rand}`
}

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
  { id: 'wechat', name: '微信支付', icon: ChatDotRound, color: '#10b981' },
  { id: 'alipay', name: '支付宝', icon: CreditCard, color: '#5b9bff' },
  { id: 'bank', name: '银行卡', icon: CreditCard, color: '#8b5cf6' },
  { id: 'cash', name: '现金支付', icon: Money, color: '#fbbf24' },
  { id: 'balance', name: '会员储值', icon: Wallet, color: '#06d4e4' },
  { id: 'points', name: '积分抵扣', icon: Coin, color: '#f97316' },
  { id: 'combined', name: '组合支付', icon: Wallet, color: '#ec4899' }
]

// ==================== 组合支付状态 ====================
const isCombinedPay = ref(false)
const cashAmount = ref(0)
const cashPayMethod = ref<1 | 2 | 3 | 4>(1)
const storedValueAmount = ref(0)
const pointsAmount = ref(0)
// 积分规则（用于组合支付类别3积分抵扣计算，完整规则含 status 与 maxDeductAmount）
const comboPointsRule = ref<PointsRule | null>(null)

// 线下支付方式：组合支付模式下由顶部 4 种线下方式（微信/支付宝/银行卡/现金）点击选择，
// 其 PayMethod 编号与 PAY_METHOD_MAP 中 wechat=3 / alipay=2 / cash=1 / bank=4 一致（即 cashPayMethod）
const OFFLINE_METHOD_IDS = ['wechat', 'alipay', 'bank', 'cash']
const OFFLINE_METHOD_NAME: Record<number, string> = { 1: '现金', 2: '支付宝', 3: '微信', 4: '银行卡' }
// 当前选中的线下支付方式名称（组合支付"线下支付"行提示展示）
const offlinePayMethodName = computed(() => OFFLINE_METHOD_NAME[cashPayMethod.value] ?? '')

// 组合支付合计
const combinedTotal = computed(() => {
  return Math.round((cashAmount.value + storedValueAmount.value + pointsAmount.value) * 100) / 100
})

// 积分最大可抵扣金额 = min(会员积分 × 抵扣率, 应收金额, 规则单笔上限)
// 与后端 DeductPointsAsync 保持一致：规则须启用（status=1）、抵扣率>0，并受 maxDeductAmount（0=不限）约束
const maxPointsDeduct = computed(() => {
  const rule = comboPointsRule.value
  if (!selectedMember.value || !rule) return 0
  if (rule.status !== 1 || (rule.deductRate || 0) <= 0) return 0
  const byPoints = selectedMember.value.totalPoints * rule.deductRate
  let limit = Math.min(byPoints, payableAmount.value)
  if (rule.maxDeductAmount > 0) limit = Math.min(limit, rule.maxDeductAmount)
  return Math.round(limit * 100) / 100
})

// 积分抵扣金额上限（仅整数元）：会员积分可抵金额与规则单笔上限的较小者，向下取整保证输入框只能输入整数元
const pointsDeductMax = computed(() => Math.floor(maxPointsDeduct.value))

// 组合支付说明：本单积分可抵金额（元）与对应消耗积分（规则不可用时均为 0）
const pointsDeductHint = computed(() => {
  const rule = comboPointsRule.value
  if (!rule || rule.status !== 1 || (rule.deductRate || 0) <= 0) return { amount: 0, points: 0 }
  return {
    amount: pointsDeductMax.value,
    points: Math.ceil(pointsDeductMax.value / rule.deductRate)
  }
})

// 组合支付说明：本单储值可扣除金额（会员当前余额与本单应付金额的较小者）
const storedValueDeductHint = computed(() => {
  if (!selectedMember.value) return 0
  return Math.round(Math.min(selectedMember.value.balance, payableAmount.value) * 100) / 100
})

// 储值支付后剩余金额（余额不足时归零，结算另有"余额不足"拦截）
const balanceAfterPay = computed(() => {
  if (!selectedMember.value) return 0
  return Math.max(0, Math.round((selectedMember.value.balance - payableAmount.value) * 100) / 100)
})

// 积分抵扣：本单应收全额所需积分（向上取整为整数积分；rate=每积分可抵金额）
// 规则须启用（status=1）且抵扣率>0，否则不可抵
const pointsNeeded = computed(() => {
  const rule = comboPointsRule.value
  if (!rule || rule.status !== 1 || (rule.deductRate || 0) <= 0) return 0
  return Math.ceil(payableAmount.value / rule.deductRate)
})

// 积分抵扣：实际最大可用积分（受会员总积分与规则单笔抵扣上限约束）
const maxPointsUsed = computed(() => {
  const rule = comboPointsRule.value
  if (!rule || rule.status !== 1 || (rule.deductRate || 0) <= 0) return 0
  // 单笔抵扣上限换算成积分上限（maxDeductAmount / deductRate，向上取整保证不超金额上限）
  let limit = Math.min(selectedMember.value?.totalPoints || 0, pointsNeeded.value)
  if (rule.maxDeductAmount > 0) {
    limit = Math.min(limit, Math.ceil(rule.maxDeductAmount / rule.deductRate))
  }
  return limit
})

// 积分抵扣后剩余积分
const pointsRemain = computed(() => {
  return Math.max(0, (selectedMember.value?.totalPoints || 0) - maxPointsUsed.value)
})

// 组合支付是否可提交：三栏总和 = 应收金额，且至少一项>0
const canConfirmCombinedPay = computed(() => {
  if (!isCombinedPay.value) return true
  if (combinedTotal.value <= 0) return false
  return Math.abs(combinedTotal.value - payableAmount.value) < 0.01
})

// 确认收款是否可提交（前端预校验，与结算时后端校验保持一致）：
// 组合支付三栏须合计=应收；积分抵扣须规则可用且最大可用积分≥本单需要积分；储值支付须余额足够
const canConfirmPay = computed(() => {
  if (isCombinedPay.value && !canConfirmCombinedPay.value) return false
  if (selectedPayMethod.value === 'balance' && selectedMember.value && selectedMember.value.balance < payableAmount.value) return false
  if (selectedPayMethod.value === 'points') {
    const rule = comboPointsRule.value
    const ruleOk = !!rule && rule.status === 1 && (rule.deductRate || 0) > 0
    // 规则不可用或积分不足以覆盖本单应收时均不可提交
    if (!ruleOk || maxPointsUsed.value < pointsNeeded.value) return false
  }
  return true
})

// 支付方式卡片最外层选中高亮（青色）：组合支付模式下 4 种线下方式不使用该效果，
// 改由 offline-selected（橙色）区分，避免与"组合支付"卡片选中混淆
const isPayMethodActive = (id: string) => {
  if (isCombinedPay.value && OFFLINE_METHOD_IDS.includes(id)) return false
  return selectedPayMethod.value === id
}

// 组合支付模式下，顶部 4 种线下方式（微信/支付宝/银行卡/现金）是否被选中为当前线下收款方式
const isOfflineSelected = (id: string) =>
  isCombinedPay.value && OFFLINE_METHOD_IDS.includes(id) && cashPayMethod.value === PAY_METHOD_MAP[id]

// 切换支付方式
const onSelectPayMethod = (id: string) => {
  // 组合支付模式下，点击顶部 4 种线下方式仅切换线下收款方式，不退出组合支付
  if (isCombinedPay.value && OFFLINE_METHOD_IDS.includes(id)) {
    cashPayMethod.value = PAY_METHOD_MAP[id] as 1 | 2 | 3 | 4
    return
  }
  // 储值/积分/组合支付依赖会员账户（扣储值余额/抵积分），未选会员时拦截提示，与项目卡开卡提示一致
  if ((id === 'balance' || id === 'points' || id === 'combined') && !selectedMember.value) {
    ElMessage.warning('请先选择会员')
    return
  }
  selectedPayMethod.value = id
  isCombinedPay.value = (id === 'combined')
  if (isCombinedPay.value) {
    // 进入组合支付模式时初始化线下支付为应收金额（默认现金），其余为0
    cashAmount.value = Math.round(payableAmount.value * 100) / 100
    cashPayMethod.value = 1
    storedValueAmount.value = 0
    pointsAmount.value = 0
  }
}

// 线下支付金额变化：储值余额补足剩余，差额转入积分（整数元），取整/上限缺口回填线下保证合计=应收
const onCashAmountChange = () => {
  if (!isCombinedPay.value) return
  // 线下支付不能超过应收，超过时截断
  if (cashAmount.value > payableAmount.value) {
    cashAmount.value = Math.round(payableAmount.value * 100) / 100
  }
  const remaining = Math.round((payableAmount.value - cashAmount.value) * 100) / 100
  if (remaining <= 0) {
    storedValueAmount.value = 0
    pointsAmount.value = 0
    return
  }
  // 储值余额补足（不足取实际余额）
  const svAvailable = selectedMember.value?.balance || 0
  const sv = Math.round(Math.min(svAvailable, remaining) * 100) / 100
  storedValueAmount.value = sv
  // 差额转入积分（仅整数元，受会员可抵金额与规则单笔上限约束）
  const afterSv = Math.round((remaining - sv) * 100) / 100
  const points = Math.min(Math.floor(afterSv), pointsDeductMax.value)
  pointsAmount.value = points
  // 积分取整（或受上限钳制）产生的缺口由线下支付兜底，保证三栏合计=应收
  const gap = Math.round((afterSv - points) * 100) / 100
  if (gap > 0) cashAmount.value = Math.round((cashAmount.value + gap) * 100) / 100
}

// 储值金额变化：积分保持不变，线下支付 = 应收 - 储值 - 积分（≥0），
// 储值超限（余额 / 应收-积分）时钳制，保证三栏合计恒等于应收
const onStoredValueChange = () => {
  if (!isCombinedPay.value) return
  // 储值不能超过会员当前余额
  const svAvailable = selectedMember.value?.balance || 0
  if (storedValueAmount.value > svAvailable) {
    storedValueAmount.value = Math.round(svAvailable * 100) / 100
  }
  // 储值不能超过"应收-积分"（否则线下支付为负）
  const maxSv = Math.round((payableAmount.value - pointsAmount.value) * 100) / 100
  if (storedValueAmount.value > maxSv) {
    storedValueAmount.value = Math.max(0, maxSv)
  }
  // 线下兜底：总金额 = 应收
  cashAmount.value = Math.max(0, Math.round((payableAmount.value - storedValueAmount.value - pointsAmount.value) * 100) / 100)
}

// 积分金额变化：储值保持不变，线下支付 = 应收 - 储值 - 积分（≥0），
// 积分超限（可抵上限 / 应收-储值）时钳制，保证三栏合计恒等于应收
const onPointsChange = () => {
  if (!isCombinedPay.value) return
  // 积分不能超过可抵上限（整数元）
  if (pointsAmount.value > pointsDeductMax.value) {
    pointsAmount.value = pointsDeductMax.value
  }
  // 积分不能超过"应收-储值"（否则线下支付为负）
  const maxPts = Math.floor(payableAmount.value - storedValueAmount.value)
  if (pointsAmount.value > maxPts) {
    pointsAmount.value = Math.max(0, maxPts)
  }
  // 线下兜底：总金额 = 应收
  cashAmount.value = Math.max(0, Math.round((payableAmount.value - storedValueAmount.value - pointsAmount.value) * 100) / 100)
}

const openPayDialog = async () => {
  if (cart.value.length === 0) return
  // 应付金额为 0（如纯核销购物车）时无需收银，跳过结算弹窗，直接进入结算流程
  if (payableAmount.value <= 0) {
    await handleConfirmPay(true)
    return
  }
  // 重置组合支付状态
  isCombinedPay.value = false
  cashAmount.value = 0
  cashPayMethod.value = 1
  storedValueAmount.value = 0
  pointsAmount.value = 0
  // 加载积分规则（完整规则：status/maxDeductAmount/deductRate，用于积分抵扣计算与展示）
  if (!comboPointsRule.value) {
    try {
      comboPointsRule.value = await getPointsRule()
    } catch {
      // 积分规则加载失败不阻塞，组合支付积分栏将不可用
      comboPointsRule.value = null
    }
  }
  payDialogVisible.value = true
}

const handleConfirmPay = async (skipConfirm = false) => {
  // 拆分购物车行：核销行（扣卡次，走 verifyTreatmentCard）、商品/服务行（走 createOrder，系统收款记录）、
  // 开卡行（线下独立收款，走 createTreatmentCardSale 独立记账，金额仅展示计入应付）
  // 类型谓词收窄 id 为 number（商品/服务行行ID恒为商品ID，核销/开卡行才是字符串ID）
  const isProductLine = (i: CartItem): i is CartItem & { id: number } => i.lineType === 'product'
  const verifyLines = cart.value.filter(i => i.lineType === 'verify')
  const saleLines = cart.value.filter(i => i.lineType === 'sale')
  const productLines = cart.value.filter(isProductLine)

  // 支付方式名称（商品/服务收款确认 + 结算结果聚合展示共用）；应付为 0 免收款时固定记现金
  const payMethodName = payableAmount.value <= 0
    ? '现金支付'
    : (payMethods.find(m => m.id === selectedPayMethod.value)?.name ?? '')

  // 仅存在商品/服务行时才需要系统收款（支付校验 + 收款确认）；
  // 应付为 0（纯核销/免单）直接结算时跳过（skipConfirm=true），无需支付
  if (productLines.length > 0 && !skipConfirm) {
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
      if (pointsAmount.value > pointsDeductMax.value) {
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

    try {
      await ElMessageBox.confirm(
        `确认通过「${payMethodName}」收款 ¥${formatPrice(payableAmount.value)}？`,
        '确认收款',
        { type: 'info', confirmButtonText: '确认', cancelButtonText: '取消' }
      )
    } catch {
      return  // 用户取消
    }
  }

  // 开卡配置预检：混合结算为整体事务，任一开卡失败将整单回滚，故先拦截配置缺失的开卡行，避免白跑结算
  const saleBuilds: { line: CartItem; items: { productId: number; quantity: number; originalPrice: number }[] }[] = []
  const saleErrors: string[] = []
  for (const line of saleLines) {
    if (!line.cardId || !line.saleCustomerId) {
      saleErrors.push(`开卡「${line.name}」失败：缺少开卡客户或卡配置`)
      continue
    }
    const card = treatmentCards.value.find(c => c.id === line.cardId)
    if (!card) {
      saleErrors.push(`开卡「${line.name}」失败：未找到项目卡配置（ID: ${line.cardId}）`)
      continue
    }
    // 项目明细取自项目卡配置（商品、次数、原价；折算单价由后端按实际售价分摊计算）
    saleBuilds.push({
      line,
      items: card.items.map(it => ({
        productId: it.productId,
        quantity: it.quantity,
        originalPrice: it.originalPrice
      }))
    })
  }
  if (saleErrors.length > 0) {
    ElMessageBox.alert(saleErrors.join('\n'), '无法结算', { type: 'warning', confirmButtonText: '知道了' })
    return
  }

  paying.value = true
  // 购物车结算批次号：订单/核销/开卡三单据共用，用于跨单据聚合追溯（见 CheckoutSessionNo 落库字段）
  const checkoutSessionNo = generateCheckoutSessionNo()

  const hasService = productLines.some(i => i.productType === 2)
  const orderType = hasService ? 2 : 1
  const memberRate = selectedMember.value?.discountRate
    ? selectedMember.value.discountRate / 10
    : 1

  const now = new Date()
  const orderTime = isBackfill.value && backfillDate.value
    ? backfillDate.value
    : toLocalDateTime(now)

  // 组合支付字段：仅 PayMethod=7 时填充
  const isCombined = isCombinedPay.value
  // 应付金额为 0（纯核销/免单）时无需真实收款，支付方式固定记现金(1)，避免沿用默认选择造成流水语义错误
  const effectivePayMethod = (payableAmount.value <= 0 ? 1 : (isCombined ? 7 : PAY_METHOD_MAP[selectedPayMethod.value])) as 1 | 2 | 3 | 4 | 5 | 6 | 7
  // 订单金额口径：仅商品/服务行（开卡费走 sales 独立记账，不进订单 → 不进日结营收，D3）
  // 注意：应付金额展示（payableAmount）含开卡费（店员按显示金额线下收款），订单 paidAmount 仅记录商品/服务
  const productAmount = roundMoney(productLines.reduce((sum, i) => sum + i.price * i.quantity, 0))
  const productPayable = roundMoney(productAmount - discountAmount.value)

  // 组装混合结算请求：核销/建单/开卡三单据由后端同一 DB 事务执行，任一失败整体回滚（无部分成功）
  const payload: PosCheckoutCreate = {
    checkoutSessionNo,
    // 商品/服务行建订单（仅存在时传；系统收款记录仅覆盖商品/服务，开卡费不进订单）
    ...(productLines.length > 0 ? {
      order: {
        customerId: selectedMember.value?.id,
        orderType: orderType as 1 | 2,
        status: 2,  // 已完成（POS 收银即完成）
        backfillStatus: isBackfill.value ? 1 : 0,
        productAmount: productAmount,
        discountAmount: roundMoney(discountAmount.value),
        paidAmount: productPayable,
        payMethod: effectivePayMethod,
        cashAmount: isCombined ? cashAmount.value : undefined,
        cashPayMethod: isCombined && cashAmount.value > 0 ? cashPayMethod.value : undefined,
        storedValueAmount: isCombined ? storedValueAmount.value : undefined,
        pointsAmount: isCombined ? pointsAmount.value : undefined,
        orderTime,
        completeTime: orderTime,
        items: productLines.map(item => ({
          productId: item.id,
          productName: item.name,
          productCode: item.code,
          technicianId: item.technicianId,
          technicianSource: item.technicianSource,
          roomId: item.roomId,
          equipmentId: item.equipmentId,
          // 服务时间透传给 OrderItemCreate：后端据此做占用检测并按服务时间日归集技师统计
          serviceStartTime: item.serviceStartTime,
          serviceEndTime: item.serviceEndTime,
          remark: item.remark,
          quantity: item.quantity,
          price: item.price,
          discountRate: memberRate,
          // 赠品 price=0，乘以任何折扣率仍为 0，自然满足"赠品行 DiscountedAmount 自动为 0"
          discountedAmount: roundMoney(item.price * item.quantity * memberRate),
          // 除服务项目（type=2）外，实物/耗材/赠品均需传效期列表（undefined 表示自动推荐，后端按 FEFO 扣减）
          // 赠品走与零售商品相同的批次扣减逻辑（FEFO+FIFO），后端 DeductSampleGiftOutAsync 按效期扣减
          expirationDates: (item.productType !== 2) ? item.expirationDates : undefined,
          // 服务项目（type=2）绑定耗材的效期选择（加购时店员已选择；未绑定耗材/未选为 undefined，后端按 FEFO 扣减）
          consumableExpiries: (item.productType === 2) ? item.consumableExpiries : undefined,
          allowAutoFillBeyondSelection: false,
          // 赠品（type=5）关联活动（非必填，写入 InventoryLog.ActivityId 用于活动维度归因统计）
          activityId: item.productType === 5 ? item.activityId : undefined
        })),
        // 预约转订单：取第一个有 sourceAppointmentId 的明细对应的预约ID（预约一次只生成一个订单）
        sourceAppointmentId: productLines.find(i => i.sourceAppointmentId)?.sourceAppointmentId,
        checkoutSessionNo
      } satisfies OrderCreate
    } : {}),
    // 核销组（D2 结算时序：先核销、后建单、再开卡；同一张卡多次加入购物车时按 cardSaleId 聚合后一次核销）
    ...(verifyLines.length > 0 ? {
      verifies: buildVerifyGroups(verifyLines)
    } : {}),
    // 开卡组独立记账（线下独立收款，不进入系统支付/日结）；saleNames 按同序索引映射结果卡名（见 finalizeCheckoutSuccess）
    ...(saleLines.length > 0 ? {
      sales: saleBuilds.map(sb => ({
        // 预检已保证 cardId/saleCustomerId 非空（! 收窄类型以满足必填字段）
        cardId: sb.line.cardId!,
        customerId: sb.line.saleCustomerId!,
        purchaseDate: toLocalDateTime(now),
        amount: sb.line.saleAmount ?? 0,
        items: sb.items,
        remark: sb.line.saleRemark,
        checkoutSessionNo,
        // POS 统一支付方式：开卡与同批次订单支付方式一致（用户线下收款方式与所选一致，否则为操作错误）
        payMethod: effectivePayMethod,
        cashAmount: isCombined ? cashAmount.value : undefined,
        cashPayMethod: isCombined && cashAmount.value > 0 ? cashPayMethod.value : undefined,
        storedValueAmount: isCombined ? storedValueAmount.value : undefined,
        pointsAmount: isCombined ? pointsAmount.value : undefined
      }))
    } : {})
  }
  const saleNames = saleBuilds.map(sb => sb.line.name)
  // 核销卡名映射（按 cardSaleId 取购物车核销行上的卡名，用于结果聚合展示）
  const verifyCardNameById = new Map<number, string>()
  verifyLines.forEach(l => {
    if (l.cardSaleId != null && l.cardName) verifyCardNameById.set(l.cardSaleId, l.cardName)
  })

  try {
    // 混合结算单次调用：后端同一 DB 事务内完成 核销+建单+开卡，任一失败整体回滚，无部分成功
    const result = await posCheckout(payload)
    finalizeCheckoutSuccess(result, checkoutSessionNo, payMethodName, verifyCardNameById, saleNames)
  } catch (e) {
    const err = e as { code?: number; message: string }
    // 409: 选中效期库存不足（并发冲突），弹窗让店员决定是否自动从近效期补足（整单重试）
    if (err.code === 409 && err.message?.startsWith('INSUFFICIENT_EXPIRY_STOCK')) {
      const retried = await handleInsufficientExpiryStock(err.message, payload)
      if (retried) finalizeCheckoutSuccess(retried, checkoutSessionNo, payMethodName, verifyCardNameById, saleNames)
    } else {
      // 整体事务已回滚，购物车保持原样供重新结算（核销扣次等消耗性操作一并撤销，可放心重试）
      ElMessageBox.alert(`结算失败：${err.message}`, '结算失败', { type: 'error', confirmButtonText: '知道了' })
    }
  } finally {
    paying.value = false
  }
}

// ==================== 效期不足 409 处理 ====================
// 返回成功结算的复合结果（用户取消或仍失败返回 null），由 handleConfirmPay 统一决定结果聚合与购物车清理，避免此处重复重置
// 混合结算整体事务下失败已整体回滚（无部分成功），故重试整个复合请求（订单项设置自动补足标志即可，核销/开卡随之重放）
const handleInsufficientExpiryStock = async (errorMessage: string, payload: PosCheckoutCreate): Promise<PosCheckoutResult | null> => {
  // 解析错误消息: INSUFFICIENT_EXPIRY_STOCK|{productId}|{productName}|{shortfall}|{expirationDate1:qty1;expirationDate2:qty2;...}
  const parts = errorMessage.split('|')
  if (parts.length < 5) {
    ElMessage.error('效期库存不足，请重新选择效期')
    return null
  }

  const productName = parts[2]
  const shortfall = parts[3]

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
    // 用户确认：整单重试，设置订单项 allowAutoFillBeyondSelection = true（仅订单行受效期约束，核销/开卡无需改动）
    payload.order?.items.forEach(it => {
      it.allowAutoFillBeyondSelection = true
    })
    return await posCheckout(payload)
  } catch (e) {
    // 用户取消（Element Plus confirm 取消时 reject 字符串）或自动补足仍失败
    if (typeof e === 'string') {
      ElMessage.info('请重新选择效期')
    } else {
      const err = e as { message?: string }
      ElMessageBox.alert(`自动补足仍失败：${err.message ?? '未知错误'}`, '结算失败', { type: 'error', confirmButtonText: '知道了' })
    }
    return null
  }
}

// 核销组构建：同一张卡多次加入购物车时按 cardSaleId 聚合后一次核销（混合结算 D2 时序：先核销、后建单、再开卡）
const buildVerifyGroups = (verifyLines: CartItem[]): PosCheckoutVerify[] => {
  const groups = new Map<number, TreatmentCardVerifyItemInput[]>()
  for (const line of verifyLines) {
    if (!line.cardSaleId) continue
    const items = groups.get(line.cardSaleId) ?? []
    line.verifyItems?.forEach(vi => {
      if (vi.productId != null) items.push({
        productId: vi.productId,
        verifyTimes: vi.verifyTimes,
        technicianId: vi.technicianId,
        technicianSource: vi.technicianSource,
        roomId: vi.roomId,
        equipmentId: vi.equipmentId,
        serviceStartTime: vi.serviceStartTime,
        serviceEndTime: vi.serviceEndTime,
        remark: vi.remark || undefined,
        // 服务项目绑定耗材的效期选择（加购时店员已选择；未绑定耗材/未选为 undefined，后端按 FEFO 扣减）
        consumableExpiries: vi.consumableExpiries
      })
    })
    groups.set(line.cardSaleId, items)
  }
  return [...groups.entries()].map(([cardSaleId, items]) => ({ cardSaleId, items }))
}

// 混合结算成功收尾：聚合展示本次交易（订单/核销/开卡），清空购物车与支付状态
const finalizeCheckoutSuccess = (
  result: PosCheckoutResult,
  checkoutSessionNo: string,
  payMethodName: string,
  verifyCardNameById: Map<number, string>,
  saleNames: string[]
) => {
  payDialogVisible.value = false
  settlementResult.value = {
    checkoutSessionNo,
    orders: result.orders.map(o => ({ orderNo: o.orderNo, amount: o.amount, payMethodName })),
    verifies: result.verifies.map(v => ({
      cardName: verifyCardNameById.get(v.cardSaleId) ?? `项目卡#${v.cardSaleId}`,
      times: v.times,
      amount: v.amount
    })),
    // 开卡结果按请求顺序返回，saleNames 同序索引映射卡名
    sales: result.sales.map((s, i) => ({ cardName: saleNames[i] ?? '项目卡', amount: s.amount }))
  }
  settlementResultVisible.value = true
  cart.value = []
  selectedMember.value = null
  selectedMemberId.value = null
  isBackfill.value = false
  backfillDate.value = ''
  isCombinedPay.value = false
  selectedPayMethod.value = 'wechat'
  cashAmount.value = 0
  cashPayMethod.value = 1
  storedValueAmount.value = 0
  pointsAmount.value = 0
}

// ==================== 服务内容弹窗（技师/房间/设备/服务时间） ====================
// 快速开单点击服务项目商品时录入服务内容，参考预约列表弹窗（去掉客户选择/预约信息）
// 开始/结束时间（年月日时分）用于按时段查询资源占用标红；支持先服务后补单的历史时间录入
const serviceDialogVisible = ref(false)
const serviceSubmitting = ref(false)
const serviceFormRef = ref<FormInstance>()

const serviceForm = reactive({
  productId: undefined as number | undefined,
  // 服务项目名称（由点击的商品确定，弹窗内只读展示，不可更改）
  productName: '',
  technicianId: undefined as number | undefined,
  technicianName: '',
  // 商家技师/平台技师分别单独选择，互斥（选择其一清空另一）
  merchantTechnicianId: undefined as number | undefined,
  platformTechnicianId: undefined as number | undefined,
  technicianSource: 1 as 1 | 2,
  roomId: undefined as number | undefined,
  roomName: '',
  equipmentId: undefined as number | undefined,
  equipmentName: '',
  // 服务时间：完整时间戳（YYYY-MM-DDTHH:mm:ss，年月日时分），由用户选择，提交时透传 serviceStartTime/serviceEndTime
  startTime: '',
  endTime: '',
  remark: ''
})

const serviceFormRules: FormRules = {
  productId: [{ required: true, message: '请选择服务项目', trigger: 'change' }],
  startTime: [{ required: true, message: '请选择开始时间', trigger: 'change' }],
  endTime: [
    { required: true, message: '请选择结束时间', trigger: 'change' },
    {
      // 结束时间必须晚于开始时间（按完整时间戳比较，天然支持跨天时段）
      validator: (_rule, value: string, callback: (error?: Error) => void) => {
        if (!value || !serviceForm.startTime) {
          callback()
          return
        }
        if (new Date(value).getTime() <= new Date(serviceForm.startTime).getTime()) {
          callback(new Error('结束时间必须晚于开始时间'))
        } else {
          callback()
        }
      },
      trigger: 'change'
    }
  ]
}

// 服务项目列表（type=2 上架商品）
const serviceProductOptions = ref<Product[]>([])
// 商家技师列表（按服务项目适用技能过滤）
const merchantTechnicianOptions = ref<Technician[]>([])
// 平台技师列表（平台技师技能无法对齐当前门店技能分类，保持全部可选）
const platformTechnicianOptions = ref<Technician[]>([])
// 房间/床位列表（按服务项目所需房间类型过滤）
const roomOptions = ref<RoomOption[]>([])
// 设备列表（按服务项目关联设备类型过滤）
const equipmentOptions = ref<Equipment[]>([])

// 资源占用状态（技师/房间/设备），按开始/结束时间时段查询占用并标红
const resourceAvailability = ref<ResourceAvailabilityDto | null>(null)

/**
 * 购物车自检占用集合：当前选择时段内被购物车已加行占用的资源 ID（服务商品行 + 核销行逐项）
 * 与落库占用（resourceAvailability）叠加共同驱动下拉标红；编辑模式排除自身行，避免自己标红自己
 */
const cartOccupiedIds = computed(() => {
  const technicianIds = new Set<number>()
  const roomIds = new Set<number>()
  const equipmentIds = new Set<number>()
  const start = serviceForm.startTime
  const end = serviceForm.endTime
  if (!start || !end) return { technicianIds, roomIds, equipmentIds }
  // 排除目标行自身（避免自己标红自己）：
  // - 服务商品编辑模式 → 排除详情打开的目标行
  // - 核销行编辑模式（从购物车核销详情进入）→ 排除购物车中该核销行
  // - 加购合并场景 → 购物车已有与当前配置完全一致的同行（确认时合并数量、不产生新占用），同样排除
  let excludeLineId = serviceEditing
    ? serviceDetail.value?.lineId
    : (verifyEditingLine ?? undefined)
  if (!excludeLineId && !serviceEditing && !verifyEditingLine && serviceForm.productId != null) {
    // 回调内访问 reactive 属性不保留 null 收窄，先捕获到局部常量
    const productId = serviceForm.productId
    const same = cart.value.find(i => isSameServiceConfig(i, {
      id: productId,
      technicianId: serviceForm.technicianId,
      roomId: serviceForm.roomId,
      equipmentId: serviceForm.equipmentId,
      serviceStartTime: serviceForm.startTime,
      serviceEndTime: serviceForm.endTime
    }))
    excludeLineId = same?.lineId
  }
  for (const line of cart.value) {
    if (line.lineId === excludeLineId) continue
    // 服务商品行（type=2 服务项目才占用资源）：整行单一资源/时段
    if (line.lineType === 'product' && line.productType === 2) {
      if (!isTimeOverlap(start, end, line.serviceStartTime, line.serviceEndTime)) continue
      if (line.technicianId) technicianIds.add(line.technicianId)
      if (line.roomId) roomIds.add(line.roomId)
      if (line.equipmentId) equipmentIds.add(line.equipmentId)
    }
    // 项目卡核销行：逐核销项比较（每项可能不同时段/资源）
    if (line.lineType === 'verify') {
      for (const vi of line.verifyItems ?? []) {
        if (!isTimeOverlap(start, end, vi.serviceStartTime, vi.serviceEndTime)) continue
        if (vi.technicianId) technicianIds.add(vi.technicianId)
        if (vi.roomId) roomIds.add(vi.roomId)
        if (vi.equipmentId) equipmentIds.add(vi.equipmentId)
      }
    }
  }
  return { technicianIds, roomIds, equipmentIds }
})

/** 技师占用 Map（id -> IsOccupied）：落库占用 + 购物车自检占用 叠加标红 */
const technicianOccupancyMap = computed(() => {
  const map = new Map<number, { isOccupied: boolean; conflictInfo?: string }>()
  if (resourceAvailability.value) {
    for (const t of resourceAvailability.value.technicians) {
      map.set(t.id, { isOccupied: t.isOccupied, conflictInfo: t.conflictInfo })
    }
  }
  for (const id of cartOccupiedIds.value.technicianIds) {
    map.set(id, { isOccupied: true, conflictInfo: '购物车已占用' })
  }
  return map
})

/** 房间占用 Map（id -> IsOccupied）：落库占用 + 购物车自检占用 叠加标红 */
const roomOccupancyMap = computed(() => {
  const map = new Map<number, { isOccupied: boolean; conflictInfo?: string }>()
  if (resourceAvailability.value) {
    for (const r of resourceAvailability.value.rooms) {
      map.set(r.id, { isOccupied: r.isOccupied, conflictInfo: r.conflictInfo })
    }
  }
  for (const id of cartOccupiedIds.value.roomIds) {
    map.set(id, { isOccupied: true, conflictInfo: '购物车已占用' })
  }
  return map
})

/** 设备占用 Map（id -> IsOccupied）：落库占用 + 购物车自检占用 叠加标红 */
const equipmentOccupancyMap = computed(() => {
  const map = new Map<number, { isOccupied: boolean; conflictInfo?: string }>()
  if (resourceAvailability.value) {
    for (const e of resourceAvailability.value.equipments) {
      map.set(e.id, { isOccupied: e.isOccupied, conflictInfo: e.conflictInfo })
    }
  }
  for (const id of cartOccupiedIds.value.equipmentIds) {
    map.set(id, { isOccupied: true, conflictInfo: '购物车已占用' })
  }
  return map
})

/**
 * 格式化当天日期（YYYY-MM-DD）
 * @returns 当天日期字符串
 */
const formatServiceToday = (): string => {
  const now = new Date()
  const y = now.getFullYear()
  const m = String(now.getMonth() + 1).padStart(2, '0')
  const d = String(now.getDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}

/**
 * 格式化技师技能展示文本
 * @param skills 技能分类名称列表
 * @returns 展示文本（无技能时提示未设置）
 */
const formatServiceSkills = (skills?: string[]): string => {
  if (!skills || skills.length === 0) return '未设置技能'
  return `技能：${skills.join('、')}`
}

/**
 * 将服务日期 + 开始时间拼接为后端可解析的 ISO 时间戳（YYYY-MM-DDTHH:mm:ss）
 * 采用本地字符串拼接而非 toISOString()，避免 UTC 时区偏移导致跨日（历史补单场景日期必须保持用户所选）
 * @param date 服务日期（YYYY-MM-DD）
 * @param time 开始时间（HH:mm 或 HH:mm:ss）
 * @returns 拼接后的时间戳；缺少任一部分返回 undefined（不传后端，由后端回退 OrderTime 推算）
 */
const buildServiceTimestamp = (date: string, time: string): string | undefined => {
  if (!date || !time) return undefined
  const t = time.length === 5 ? `${time}:00` : time
  return `${date}T${t}`
}

/**
 * 根据开始时间和服务时长计算结束时间（HH:mm，分针算法，供核销行使用）
 * 若跨天（开始时间 + 时长 ≥ 24h）则回绕到次日时段，前端仅预览，后端按项目时长权威计算
 * @param startTime 开始时间（HH:mm）
 * @param duration 服务时长（分钟）
 * @returns 结束时间；缺开始时间或时长返回空串
 */
const calcEndTime = (startTime: string, duration?: number): string => {
  if (!startTime || !duration) return ''
  const [h, m] = startTime.split(':').map(Number)
  if (isNaN(h) || isNaN(m)) return ''
  const totalMinutes = h * 60 + m + duration
  const eh = Math.floor(totalMinutes / 60) % 24
  const em = totalMinutes % 60
  return `${String(eh).padStart(2, '0')}:${String(em).padStart(2, '0')}`
}

/**
 * 根据服务开始时间与服务时长计算服务结束时间（完整 ISO，YYYY-MM-DDTHH:mm:ss）
 * 与 calcEndTime（HH:mm 预览）不同，本函数基于 Date 计算，跨天时自动进位到次日日期
 * @param startTime 服务开始时间（YYYY-MM-DDTHH:mm:ss）
 * @param duration 服务时长（分钟）
 * @returns 服务结束时间 ISO；缺开始时间或时长返回空串
 */
const calcServiceEndTime = (startTime: string, duration?: number): string => {
  if (!startTime || !duration) return ''
  const start = new Date(startTime)
  if (isNaN(start.getTime())) return ''
  const end = new Date(start.getTime() + duration * 60000)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${end.getFullYear()}-${pad(end.getMonth() + 1)}-${pad(end.getDate())}T${pad(end.getHours())}:${pad(end.getMinutes())}:00`
}

// 监听开始/结束时间变化，实时刷新资源占用情况（技师/房间/设备列表标红）
watch(
  () => [serviceForm.startTime, serviceForm.endTime],
  () => {
    if (serviceDialogVisible.value) {
      loadServiceAvailability()
    }
  }
)

/**
 * 开始时间变更处理：开始时间变更后总是按项目服务时长自动计算结束时间（覆盖此前"仅结束时间为空才计算"的限制）；
 * 结束时间仍可手动自由更改，仅在再次变更开始时间时被重新计算。
 * 用 @change 而非 watch：仅用户交互触发，编辑模式程序回填开始时间不会覆盖已保存的结束时间。
 * 计算后重校验结束时间（保留"必须晚于开始时间"验证）。
 * @param val 新的开始时间（YYYY-MM-DDTHH:mm:ss，清空时为 null）
 */
const handleServiceStartTimeChange = (val: string | null) => {
  if (!val) return
  const duration = serviceProductOptions.value.find(p => p.id === serviceForm.productId)?.duration
  serviceForm.endTime = calcServiceEndTime(val, duration)
  serviceFormRef.value?.validateField('endTime').catch(() => {})
}

/**
 * 加载当前服务时段内的资源占用情况
 * 触发时机：开始时间/结束时间变化时
 */
const loadServiceAvailability = async () => {
  // 必须选择开始时间与结束时间才计算占用区间
  if (!serviceForm.startTime || !serviceForm.endTime) {
    resourceAvailability.value = null
    return
  }
  try {
    // 编辑模式排除目标行的源预约占用：预约转单行的来源预约仍占用资源，但其占用由本次转单继承，
    // 不应标红自身（后端 OrderAppService 对源预约订单同样跳过冲突校验）
    const excludeAppointmentId = serviceEditing ? serviceDetail.value?.sourceAppointmentId : undefined
    resourceAvailability.value = await getResourceAvailability(
      serviceForm.startTime,
      serviceForm.endTime,
      undefined,
      excludeAppointmentId
    )
  } catch {
    // 静默失败：占用查询失败不影响主流程
    resourceAvailability.value = null
  }
}

/**
 * 加载服务项目列表（仅商品类型为"服务项目"的上架商品）
 */
const loadServiceProducts = async () => {
  try {
    const res = await getProducts({ type: 2, status: 1, pageIndex: 1, pageSize: 200 })
    serviceProductOptions.value = res.list
  } catch {
    serviceProductOptions.value = []
  }
}

/**
 * 加载商家技师与平台技师列表（并行请求，按服务项目适用技能过滤）
 * - 已选服务项目：按服务项目适用技能过滤（树形展开匹配，选父级自动匹配子级）
 * - 未选服务项目：返回指定来源全部启用技师
 */
const loadServiceTechnicians = async () => {
  try {
    // 服务项目技能过滤需按商品主档ID传参：后端以 masterId 反查租户内 ServiceProduct 子表
    const masterId = serviceProductOptions.value.find(p => p.id === serviceForm.productId)?.masterId
    const [merchantList, platformList] = await Promise.all([
      getTechniciansAvailableByService(undefined, masterId, 1),
      getTechniciansAvailableByService(undefined, masterId, 2)
    ])
    merchantTechnicianOptions.value = merchantList
    platformTechnicianOptions.value = platformList
  } catch {
    merchantTechnicianOptions.value = []
    platformTechnicianOptions.value = []
  }
}

/**
 * 加载房间/床位列表（按服务项目所需房间类型过滤）
 */
const loadServiceRooms = async () => {
  try {
    const product = serviceProductOptions.value.find(p => p.id === serviceForm.productId)
    const allRooms = await getRooms()
    roomOptions.value = product?.requiredRoomType
      ? allRooms.filter(r => r.roomType === product.requiredRoomType)
      : allRooms
  } catch {
    roomOptions.value = []
  }
}

/**
 * 加载设备列表（按服务项目关联设备类型过滤，不传时间仅按类型过滤）
 */
const loadServiceEquipments = async () => {
  try {
    const product = serviceProductOptions.value.find(p => p.id === serviceForm.productId)
    if (product?.masterId) {
      equipmentOptions.value = await getAvailableEquipmentsByService(undefined, product.masterId)
    } else {
      equipmentOptions.value = await getAllEquipments(1)
    }
  } catch {
    equipmentOptions.value = await getAllEquipments(1)
  }
}

/**
 * 重置服务内容表单（服务时间需由用户重新选择）
 */
const resetServiceForm = () => {
  serviceForm.productId = undefined
  serviceForm.productName = ''
  serviceForm.technicianId = undefined
  serviceForm.technicianName = ''
  serviceForm.merchantTechnicianId = undefined
  serviceForm.platformTechnicianId = undefined
  serviceForm.technicianSource = 1
  serviceForm.roomId = undefined
  serviceForm.roomName = ''
  serviceForm.equipmentId = undefined
  serviceForm.equipmentName = ''
  serviceForm.startTime = ''
  serviceForm.endTime = ''
  serviceForm.remark = ''
  resourceAvailability.value = null
}

/**
 * 选择商家技师：同步技师ID/名称/来源，并清空平台技师保证互斥
 * @param technicianId 选中的商家技师ID（清空时为 undefined）
 */
const handleServiceMerchantChange = (technicianId: number | undefined) => {
  if (technicianId !== undefined) {
    serviceForm.platformTechnicianId = undefined
    serviceForm.technicianSource = 1
    serviceForm.technicianId = technicianId
    serviceForm.technicianName = merchantTechnicianOptions.value.find(t => t.id === technicianId)?.name ?? ''
  } else {
    serviceForm.technicianId = undefined
    serviceForm.technicianName = ''
  }
}

/**
 * 选择平台技师：同步技师ID/名称/来源，并清空商家技师保证互斥
 * @param technicianId 选中的平台技师ID（清空时为 undefined）
 */
const handleServicePlatformChange = (technicianId: number | undefined) => {
  if (technicianId !== undefined) {
    serviceForm.merchantTechnicianId = undefined
    serviceForm.technicianSource = 2
    serviceForm.technicianId = technicianId
    serviceForm.technicianName = platformTechnicianOptions.value.find(t => t.id === technicianId)?.name ?? ''
  } else {
    serviceForm.technicianId = undefined
    serviceForm.technicianName = ''
  }
}

/**
 * 选择房间时同步房间名称
 * @param roomId 选中的房间ID
 */
const handleServiceRoomChange = (roomId: number) => {
  const room = roomOptions.value.find(r => r.id === roomId)
  serviceForm.roomName = room ? room.name : ''
}

/**
 * 选择设备时同步设备名称（设备下拉缺少该同步曾导致购物车设备名为空、详情弹窗设备显示 '-'）
 * @param equipmentId 选中的设备ID（clearable 清空时为 undefined）
 */
const handleServiceEquipmentChange = (equipmentId: number | undefined) => {
  const equipment = equipmentOptions.value.find(e => e.id === equipmentId)
  serviceForm.equipmentName = equipment ? equipment.name : ''
}

// ==================== 服务详情弹窗（点击购物车服务项目行查看，含编辑入口） ====================
const serviceDetailVisible = ref(false)
const serviceDetail = ref<CartItem | null>(null)

/** 技师来源文本（1=商家技师，2=平台技师，与预约列表一致） */
const getTechnicianSourceText = (source?: number): string => {
  if (source === 1) return '商家技师'
  if (source === 2) return '平台技师'
  return '-'
}

/**
 * 格式化服务时间为 "YYYY-MM-DD HH:mm"（保留完整日期，跨天时段可读）
 * @param time 服务时间（YYYY-MM-DDTHH:mm:ss）
 * @returns 格式化文本；空值返回 '-'
 */
const formatServiceDateTime = (time?: string): string => {
  if (!time) return '-'
  return time.replace('T', ' ').slice(0, 16)
}

/** 技师来源标签类型（平台技师用 success 色区分，与预约列表一致） */
const getTechnicianSourceTagType = (source?: number): '' | 'success' => {
  if (source === 1) return ''
  if (source === 2) return 'success'
  return ''
}

/** 打开购物车服务项目详情弹窗（技师/房间/设备/服务时间等只读展示） */
const openCartServiceDetail = (item: CartItem) => {
  serviceDetail.value = item
  serviceDetailVisible.value = true
}

// ==================== 商品详情弹窗（点击购物车商品/耗材/赠品行查看） ====================
const productDetailVisible = ref(false)
const productDetail = ref<CartItem | null>(null)

/** 打开购物车商品行详情弹窗（效期批次等只读展示），打开时刷新批次明细避免显示数量变更前的过期数据 */
const openCartProductDetail = async (item: CartItem) => {
  productDetail.value = item
  productDetailVisible.value = true
  const refreshed = await refreshCartItemExpiry(item)
  if (refreshed) {
    item.expiryDetails = allocateExpiryQuantities(refreshed.orderedDates, item.quantity, refreshed.options)
  }
}

/** 详情弹窗：已选效期批次扣减数量合计（显式选择批次时等于行数量） */
const expiryDetailTotal = computed(() => {
  return productDetail.value?.expiryDetails?.reduce((sum, d) => sum + d.quantity, 0) ?? 0
})

/** 编辑商品效期：关闭详情弹窗，复用效期选择弹窗修改（编辑模式更新已有行，不新增行，见 confirmExpirySelection） */
const editProductDetail = () => {
  if (!productDetail.value) return
  const item = productDetail.value
  productDetailVisible.value = false
  openExpiryDialog(
    {
      // 商品 id 实为字符串（雪花ID，LongToStringConverter 序列化），as number 仅过编译、不做 Number() 转换，运行时保持原值用于加载效期与行匹配
      id: item.id as number,
      masterId: 0,
      name: item.name,
      code: item.code,
      spec: item.spec,
      price: item.price,
      stockText: '',
      categoryId: 0,
      productType: item.productType,
      colorClass: ''
    },
    { editing: item }
  )
}

/** 编辑服务内容：关闭详情弹窗，复用服务内容弹窗修改（编辑模式不增加数量，见 confirmServiceDialog） */
const editServiceDetail = () => {
  if (!serviceDetail.value) return
  const item = serviceDetail.value
  serviceDetailVisible.value = false
  openServiceDialog(
    {
      // 项目 id 实为字符串（雪花ID，LongToStringConverter 序列化），as number 仅过编译、不做 Number() 转换，运行时保持原值用于购物车行匹配
      id: item.id as number,
      masterId: 0,
      name: item.name,
      code: item.code,
      spec: item.spec,
      price: item.price,
      stockText: '',
      categoryId: 0,
      productType: item.productType,
      colorClass: ''
    },
    { editing: true }
  )
}

/** 服务内容弹窗编辑模式标志：编辑入口进入时只更新已有行内容，不增加数量 */
let serviceEditing = false

/** 核销行编辑模式标志：核销弹窗"编辑"进入时复用本弹窗回写核销行，不触购物车（见 openVerifyRowEditor/applyVerifyRowFromServiceForm） */
let verifyEditingRow: VerifyRow | null = null

/**
 * 打开服务内容弹窗
 * 点击左侧服务项目商品时表单清空（resetServiceForm 后仅保留服务项目名称）；
 * 编辑模式（从购物车详情"编辑服务内容"进入）回填目标行服务内容
 * @param product 点击的服务项目商品
 * @param opts.editing 是否编辑模式（从服务详情弹窗进入，仅更新不增数量）
 */
const openServiceDialog = async (product: POSProduct, opts?: { editing?: boolean }) => {
  resetServiceForm()
  serviceEditing = opts?.editing ?? false
  // 非核销入口打开时清空核销编辑标志，避免上次核销编辑残留导致确认误走核销分支
  verifyEditingRow = null
  serviceForm.productId = product.id
  serviceForm.productName = product.name
  // 仅编辑模式（从购物车详情"编辑服务内容"进入）回填目标行服务内容；
  // 点击左侧服务商品加购时保持清空（resetServiceForm 已重置，不回填购物车已有行，避免残留上次编辑信息）
  const existing = serviceEditing ? serviceDetail.value : undefined
  if (existing) {
    serviceForm.technicianId = existing.technicianId
    serviceForm.technicianName = existing.technicianName || ''
    serviceForm.technicianSource = existing.technicianSource ?? 1
    if (existing.technicianId) {
      if (existing.technicianSource === 2) {
        serviceForm.platformTechnicianId = existing.technicianId
      } else {
        serviceForm.merchantTechnicianId = existing.technicianId
      }
    }
    serviceForm.roomId = existing.roomId
    serviceForm.roomName = existing.roomName || ''
    serviceForm.equipmentId = existing.equipmentId
    serviceForm.equipmentName = existing.equipmentName || ''
    serviceForm.remark = existing.remark || ''
    serviceForm.startTime = existing.serviceStartTime || ''
    serviceForm.endTime = existing.serviceEndTime || ''
  }
  serviceDialogVisible.value = true
  // 先确保服务项目列表就绪：技师/房间/设备候选均需按所选商品反查过滤条件（masterId/requiredRoomType）
  // 若与其余加载并行，首次打开时 serviceProductOptions 尚未填充、过滤条件缺失会退化为全量列表，
  // 二次打开后列表已就绪、过滤生效，造成"首次可选资源多、后续下拉变少"的竞态问题
  await loadServiceProducts()
  await Promise.all([
    loadServiceTechnicians(),
    loadServiceRooms(),
    loadServiceEquipments()
  ])
  // 显式加载落库占用：编辑模式回填的开始/结束时间已在弹窗可见前赋值，时间监听因可见性为 false 被跳过不会触发，
  // 需在资源候选就绪后主动加载一次以驱动下拉标红（预约转单行编辑时排除其源预约占用）
  await loadServiceAvailability()
}

/**
 * 服务项目购物车行资源配置是否一致
 * 同一服务项目，仅当时段/技师/房间/设备/源预约完全相同时才视为同一购物车项（合并数量），否则新增一行
 * 源预约（sourceAppointmentId）参与比较：预约转单行只与同预约转单行合并，不与左侧直接加购的服务行合并，
 * 保证预约行的"预约"标签与 sourceAppointmentId 透传不因合并而丢失
 */
const isSameServiceConfig = (
  a: CartItem,
  b: Pick<CartItem, 'id' | 'technicianId' | 'roomId' | 'equipmentId' | 'serviceStartTime' | 'serviceEndTime' | 'sourceAppointmentId'>
): boolean =>
  a.lineType === 'product' &&
  a.id === b.id &&
  a.technicianId === b.technicianId &&
  a.roomId === b.roomId &&
  a.equipmentId === b.equipmentId &&
  a.serviceStartTime === b.serviceStartTime &&
  a.serviceEndTime === b.serviceEndTime &&
  a.sourceAppointmentId === b.sourceAppointmentId

// ==================== 加购前资源冲突检测（硬拦截） ====================
// 服务商品/项目卡核销在加入购物车前对所选技师/房间/设备做占用校验：
// 1) 落库占用（订单/预约已落库，getResourceAvailability 查询）
// 2) 购物车自检（本单已加行同资源同时段，防止单内互占）
// 结算收银仅作最后兜底校验（防止加购后到结算期间被其他操作员占用），故此处查询失败不阻塞加购

/** 半开区间重叠判断：startA < endB && startB < endA（与后端占用检测区间语义一致） */
const isTimeOverlap = (aStart?: string, aEnd?: string, bStart?: string, bEnd?: string): boolean =>
  !!aStart && !!aEnd && !!bStart && !!bEnd &&
  new Date(aStart).getTime() < new Date(bEnd).getTime() &&
  new Date(bStart).getTime() < new Date(aEnd).getTime()

/**
 * 落库占用硬校验：一次查询目标时段占用，检查所选技师/房间/设备是否已被占用（订单/预约落库）
 * 任一被占用即返回冲突描述；无冲突或查询失败返回 null（查询失败不阻塞，交给结算兜底）
 * @param startTime 目标开始时间（ISO）
 * @param endTime 目标结束时间（ISO）
 * @param resources 所选资源（仅含已选部分）
 * @param excludeAppointmentId 需排除占用的预约ID（预约转单行编辑时传其源预约，该占用由转单继承、不拦截自身）
 * @returns 冲突描述或 null
 */
const checkPersistedConflict = async (
  startTime: string,
  endTime: string,
  resources: {
    technician?: { id: number; name?: string }
    room?: { id: number; name?: string }
    equipment?: { id: number; name?: string }
  },
  excludeAppointmentId?: number
): Promise<string | null> => {
  if (!startTime || !endTime) return null
  if (!resources.technician && !resources.room && !resources.equipment) return null
  try {
    const avail = await getResourceAvailability(startTime, endTime, undefined, excludeAppointmentId)
    if (resources.technician) {
      const t = avail.technicians.find(x => x.id === resources.technician!.id)
      if (t?.isOccupied) return `技师「${resources.technician.name ?? resources.technician.id}」在该时段已被占用`
    }
    if (resources.room) {
      const r = avail.rooms.find(x => x.id === resources.room!.id)
      if (r?.isOccupied) return `房间「${resources.room.name ?? resources.room.id}」在该时段已被占用`
    }
    if (resources.equipment) {
      const e = avail.equipments.find(x => x.id === resources.equipment!.id)
      if (e?.isOccupied) return `设备「${resources.equipment.name ?? resources.equipment.id}」在该时段已被占用`
    }
    return null
  } catch {
    // 占用查询失败：不阻塞加购，结算时后端兜底校验
    return null
  }
}

/**
 * 购物车自检：查找与目标资源/时段冲突的已加购物车行（服务商品行 + 项目卡核销行）
 * 同资源同时段视为冲突（硬拦截前自检，防止本单内部行互相占用）
 * @param target 目标资源与时段（未选资源传 undefined 不参与比对）
 * @param excludeLineId 排除的购物车行ID（编辑模式/同配置合并时排除目标行自身，避免自己查自己）
 * @returns 冲突描述，无冲突返回 null
 */
const findCartResourceConflict = (
  target: { technicianId?: number; roomId?: number; equipmentId?: number; startTime: string; endTime: string },
  excludeLineId?: string
): string | null => {
  for (const line of cart.value) {
    if (line.lineId === excludeLineId) continue
    // 服务商品行（type=2 服务项目才占用资源）：整行单一资源/时段
    if (line.lineType === 'product' && line.productType === 2) {
      if (!isTimeOverlap(target.startTime, target.endTime, line.serviceStartTime, line.serviceEndTime)) continue
      if (target.technicianId && line.technicianId && line.technicianId === target.technicianId) {
        return `技师「${line.technicianName ?? line.technicianId}」已被购物车「${line.name}」占用（${formatDateTimeToTime(line.serviceStartTime)}~${formatDateTimeToTime(line.serviceEndTime)}）`
      }
      if (target.roomId && line.roomId && line.roomId === target.roomId) {
        return `房间「${line.roomName ?? line.roomId}」已被购物车「${line.name}」占用`
      }
      if (target.equipmentId && line.equipmentId && line.equipmentId === target.equipmentId) {
        return `设备「${line.equipmentName ?? line.equipmentId}」已被购物车「${line.name}」占用`
      }
    }
    // 项目卡核销行：逐核销项比较（每项可能不同时段/资源）
    if (line.lineType === 'verify') {
      for (const vi of line.verifyItems ?? []) {
        if (!isTimeOverlap(target.startTime, target.endTime, vi.serviceStartTime, vi.serviceEndTime)) continue
        if (target.technicianId && vi.technicianId && vi.technicianId === target.technicianId) {
          return `技师已被购物车「${line.name}」核销项占用（${formatDateTimeToTime(vi.serviceStartTime)}~${formatDateTimeToTime(vi.serviceEndTime)}）`
        }
        if (target.roomId && vi.roomId && vi.roomId === target.roomId) {
          return `房间已被购物车「${line.name}」核销项占用（${formatDateTimeToTime(vi.serviceStartTime)}~${formatDateTimeToTime(vi.serviceEndTime)}）`
        }
        if (target.equipmentId && vi.equipmentId && vi.equipmentId === target.equipmentId) {
          return `设备已被购物车「${line.name}」核销项占用（${formatDateTimeToTime(vi.serviceStartTime)}~${formatDateTimeToTime(vi.serviceEndTime)}）`
        }
      }
    }
  }
  return null
}

/**
 * 确认服务内容并加入购物车
 * 加购模式：仅当购物车存在同商品且时段/技师/房间/设备完全一致的行才合并数量并更新内容，否则新增一行；
 * 编辑模式（服务详情弹窗进入）：按 lineId 更新目标行服务内容，不增加数量
 * 加入购物车前做资源占用硬校验（落库 + 购物车自检），冲突则阻止加购
 */
const confirmServiceDialog = async () => {
  if (!serviceFormRef.value) return
  const valid = await serviceFormRef.value.validate().catch(() => false)
  if (!valid) return
  // 核销行编辑模式（核销弹窗"编辑"进入）：复用本弹窗回写核销弹窗中的目标行，不触购物车，核销弹窗保持打开
  if (verifyEditingRow) {
    // 核销项目本质是服务商品：重新选择绑定耗材效期（与核销整体确认一致），库存不足或店员取消则中止编辑，服务弹窗保持打开
    // 回显该核销行已有的耗材效期选择（previous 按 requestIndex 0 传递）
    if (verifyEditingRow.productId != null && (verifyEditingRow.verifyTimes || 0) > 0) {
      const consumables = await selectServiceConsumables(
        [{ requestIndex: 0, productId: verifyEditingRow.productId, quantity: verifyEditingRow.verifyTimes }],
        new Map([[0, verifyEditingRow.consumableExpiries ?? []]])
      )
      if (consumables === null) return
      verifyEditingRow.consumableExpiries = consumables?.[0]
    }
    applyVerifyRowFromServiceForm(verifyEditingRow)
    serviceDialogVisible.value = false
    return
  }
  const product = serviceProductOptions.value.find(p => p.id === serviceForm.productId)
  if (!product) return

  // 构建服务数据与目标行（供冲突检查排除自身行）
  const serviceData = {
    technicianId: serviceForm.technicianId,
    technicianName: serviceForm.technicianName || undefined,
    technicianSource: serviceForm.technicianSource,
    roomId: serviceForm.roomId,
    roomName: serviceForm.roomName || undefined,
    equipmentId: serviceForm.equipmentId,
    equipmentName: serviceForm.equipmentName || undefined,
    // 服务时间透传：开始/结束时间均由用户选择（YYYY-MM-DDTHH:mm:ss），直接透传给 OrderItemCreate
    serviceStartTime: serviceForm.startTime,
    serviceEndTime: serviceForm.endTime,
    remark: serviceForm.remark || undefined
  }
  // 编辑模式更新详情弹窗打开的目标行（按 lineId）；加购模式仅匹配"同商品同资源配置"行
  const existing = serviceEditing
    ? cart.value.find(i => i.lineId === serviceDetail.value?.lineId)
    : cart.value.find(i => isSameServiceConfig(i, {
        id: product.id,
        technicianId: serviceData.technicianId,
        roomId: serviceData.roomId,
        equipmentId: serviceData.equipmentId,
        serviceStartTime: serviceData.serviceStartTime,
        serviceEndTime: serviceData.serviceEndTime
      }))

  // 编辑模式（服务项目/预约转单）：先重新选择绑定耗材效期（与新增一致），库存不足或店员取消则中止编辑
  // 效期选择结果存局部变量，待资源冲突校验与编辑标志在 try/finally 中统一处理，避免部分更新
  let editConsumables: ConsumableExpiry[] | undefined
  if (serviceEditing && existing && existing.productType === 2) {
    // 编辑模式回显行上已有的耗材效期选择（previous 按 requestIndex 0 传递）
    const consumables = await selectServiceConsumables(
      [{ requestIndex: 0, productId: product.id, quantity: existing.quantity }],
      new Map([[0, existing.consumableExpiries ?? []]])
    )
    if (consumables === null) return
    editConsumables = consumables?.[0]
  }

  // 资源占用硬校验（落库 + 购物车自检）：
  // - 自检排除目标行自身（existing.lineId），其余已加行同资源同时段冲突则阻止
  // - 合并场景（同配置行已存在且为加购合并）不产生新的资源占用，跳过落库校验（该行来源占用允许共存）
  // - 校验放在 try 之前：冲突 return 不重置编辑状态，弹窗保持打开供调整
  const targetLineId = existing?.lineId
  const cartConflict = findCartResourceConflict({
    technicianId: serviceData.technicianId,
    roomId: serviceData.roomId,
    equipmentId: serviceData.equipmentId,
    startTime: serviceData.serviceStartTime ?? '',
    endTime: serviceData.serviceEndTime ?? ''
  }, targetLineId)
  if (cartConflict) {
    ElMessage.warning(`无法加入购物车：${cartConflict}`)
    return
  }
  const isMerge = !!existing && !serviceEditing
  if (!isMerge) {
    // 编辑模式排除目标行的源预约占用（预约转单行自身来源不拦截，与下拉标红口径一致）
    const excludeAppointmentId = serviceEditing ? serviceDetail.value?.sourceAppointmentId : undefined
    const persistedConflict = await checkPersistedConflict(
      serviceData.serviceStartTime ?? '',
      serviceData.serviceEndTime ?? '',
      {
        technician: serviceData.technicianId ? { id: serviceData.technicianId, name: serviceData.technicianName } : undefined,
        room: serviceData.roomId ? { id: serviceData.roomId, name: serviceData.roomName } : undefined,
        equipment: serviceData.equipmentId ? { id: serviceData.equipmentId, name: serviceData.equipmentName } : undefined
      },
      excludeAppointmentId
    )
    if (persistedConflict) {
      ElMessage.warning(`无法加入购物车：${persistedConflict}，请调整时间或更换资源`)
      return
    }
  }

  serviceSubmitting.value = true
  try {
    if (existing) {
      // 编辑模式只更新服务内容不增加数量；加购模式同资源配置叠加数量
      if (!serviceEditing) {
        // 数量+1 前先按新数量校验绑定耗材库存并重算需求（不足回退并提示，服务弹窗保持打开供调整）
        const nextQty = existing.quantity + 1
        if (existing.consumableExpiries?.length) {
          const err = await checkServiceConsumableCapacity(existing, nextQty)
          if (err) {
            ElMessage.warning(err)
            return
          }
        }
        existing.quantity = nextQty
      } else if (existing.productType === 2) {
        // 编辑模式（服务项目/预约转单）：写回弹窗确认的耗材效期（选择已在 try 前完成，见 editConsumables）
        existing.consumableExpiries = editConsumables
      }
      Object.assign(existing, serviceData)
    } else {
      // 新增行：服务项目绑定耗材时弹窗选择效期并校验库存（任一耗材不足或店员取消则中止加购）
      const consumables = await selectServiceConsumables([
        { requestIndex: 0, productId: product.id, quantity: 1 }
      ])
      if (consumables === null) return
      cart.value.push({
        lineId: genCartLineId(product.id),
        id: product.id,
        name: product.name,
        code: product.code,
        spec: product.spec || '',
        price: product.price,
        quantity: 1,
        productType: product.type,
        lineType: 'product',
        // 绑定耗材的效期选择（无 BOM 时为 undefined，后端按 FEFO 自动扣减）
        consumableExpiries: consumables?.[0],
        ...serviceData
      })
    }
    serviceDialogVisible.value = false
  } finally {
    serviceSubmitting.value = false
    serviceEditing = false
    verifyEditingRow = null
  }
}

// ==================== 项目卡销售弹窗（开卡）与开卡详情弹窗 ====================
const saleDialogVisible = ref(false)
const saleCard = ref<TreatmentCardItem | null>(null)
const saleAmount = ref(0)
const saleRemark = ref('')
/** 开卡弹窗编辑模式标志：从购物车开卡详情"编辑开卡信息"进入时只更新目标行售价/备注，不新增行（见 confirmAddSaleToCart） */
let saleEditing = false

// ---- 开卡详情弹窗（点击购物车开卡行查看，含编辑入口）----
const saleDetailVisible = ref(false)
/** 开卡详情数据：购物车开卡行 + 按 cardId 反查到的卡配置（配置可能被停用/删除而缺失，缺失时详情中卡信息显示 '-'，编辑仍可改售价/备注） */
interface SaleDetailData {
  line: CartItem
  card?: TreatmentCardItem
}
const saleDetail = ref<SaleDetailData | null>(null)

// 打开项目卡开卡弹窗（开卡必须先选择会员，后端 CreateAsync 强依赖 CustomerId）
// 编辑模式（从购物车开卡详情进入）回填目标行售价/备注；新增模式重置为卡配置默认售价
const openTreatmentSale = (card: TreatmentCardItem, opts?: { editing?: boolean }) => {
  if (!selectedMember.value) {
    ElMessage.warning('请先选择会员')
    return
  }
  saleCard.value = card
  saleEditing = opts?.editing ?? false
  if (saleEditing && saleDetail.value) {
    saleAmount.value = saleDetail.value.line.saleAmount ?? card.price
    saleRemark.value = saleDetail.value.line.saleRemark || ''
  } else {
    saleAmount.value = card.price
    saleRemark.value = ''
  }
  saleDialogVisible.value = true
}

// 将开卡选择加入购物车（不立即开卡；结算时随订单一起走混合结算，见 handleConfirmPay）
// 开卡费参与应付金额展示（店员按显示金额线下收款），但走 createTreatmentCardSale 独立记账，不进系统支付/日结
const confirmAddSaleToCart = () => {
  if (!saleCard.value || !selectedMember.value) return
  if (!saleAmount.value || saleAmount.value <= 0) {
    ElMessage.warning('请输入正确的销售金额')
    return
  }
  // 编辑模式（从开卡详情"编辑开卡信息"进入）按 lineId 更新目标行售价/备注，不新增行
  const existing = saleEditing
    ? cart.value.find(i => i.lineId === saleDetail.value?.line?.lineId)
    : undefined
  if (existing) {
    Object.assign(existing, {
      name: saleCard.value.name,
      price: saleAmount.value,
      saleAmount: saleAmount.value,
      saleRemark: saleRemark.value || undefined
    })
    saleDialogVisible.value = false
    saleEditing = false
    ElMessage.success('开卡信息已更新')
    return
  }
  cart.value.push({
    lineId: genCartLineId(saleCard.value.id),
    id: `s-${saleCard.value.id}-${Date.now()}`,
    name: saleCard.value.name,
    code: '',
    spec: '',
    price: saleAmount.value,
    quantity: 1,
    productType: 2,
    lineType: 'sale',
    cardId: saleCard.value.id,
    saleAmount: saleAmount.value,
    saleRemark: saleRemark.value || undefined,
    saleCustomerId: selectedMember.value.id,
    saleCustomerName: selectedMember.value.name
  })
  saleDialogVisible.value = false
  saleEditing = false
  ElMessage.success('已加入购物车，结算时一并开卡')
}

/** 打开购物车开卡行详情弹窗（会员/卡信息/售价/备注只读展示，卡配置按 cardId 从当前项目卡列表反查） */
const openCartSaleDetail = (item: CartItem) => {
  const card = treatmentCards.value.find(c => c.id === item.cardId)
  saleDetail.value = { line: item, card }
  saleDetailVisible.value = true
}

/** 编辑开卡信息：关闭详情弹窗，复用开卡弹窗编辑模式修改（编辑模式更新已有行不新增，见 confirmAddSaleToCart） */
const editSaleDetail = () => {
  if (!saleDetail.value) return
  const { line, card } = saleDetail.value
  saleDetailVisible.value = false
  // 卡配置优先取详情已反查结果；反查不到（已停用/删除）时以行内字段构造兜底卡，保证售价/备注仍可编辑、cardId 不丢失
  const fallbackCard: TreatmentCardItem = {
    id: line.cardId ?? 0,
    name: line.name,
    price: line.price,
    totalTimes: 0,
    validityDays: 0,
    isEnabled: false,
    items: [],
    createdAt: '',
    colorClass: '',
    itemSummary: ''
  }
  openTreatmentSale(card ?? fallbackCard, { editing: true })
}

// ==================== 项目卡核销弹窗 ====================
const verifyDialogVisible = ref(false)
const cardList = ref<TreatmentCardSale[]>([])
const selectedCardSaleId = ref<number | undefined>(undefined)
const verifying = ref(false)

/** 核销编辑行：扩展 TreatmentCardVerifyItemInput，附加 UI 展示与编辑辅助字段 */
interface VerifyRow extends TreatmentCardVerifyItemInput {
  /** 项目名称（展示用，从项目卡配置匹配） */
  productName?: string
  /** 服务项目时长（分钟，用于计算提交时的结束时间 ISO） */
  duration?: number
  /** 服务日期（UI：YYYY-MM-DD，由服务内容弹窗录入） */
  serviceDate: string
  /** 开始时间（UI：HH:mm，由服务内容弹窗录入） */
  startTime: string
  /** 结束日期（UI：YYYY-MM-DD，可空；结束时间与开始时间跨天时使用，缺省与 serviceDate 同天） */
  endDate?: string
  /** 结束时间（UI：HH:mm，可空；服务内容弹窗手动选择保存，缺省按项目时长自动计算） */
  endTime?: string
}

const verifyItems = ref<VerifyRow[]>([])

/** 核销编辑模式标志：从购物车核销详情"编辑核销信息"进入时按 lineId 更新已有行，不新增（见 loadCustomerCards/confirmVerifyCard） */
let verifyEditingLine: string | null = null

const selectedCard = computed(() => {
  if (!selectedCardSaleId.value) return undefined
  return cardList.value.find(c => c.id === selectedCardSaleId.value)
})

/**
 * 将核销编辑行转换为提交给后端的输入类型（剥离 UI 展示字段，服务时间由 日期+开始时间(+时长) 计算）
 * @param row 核销编辑行
 * @returns 核销项目输入（对齐 TreatmentCardVerifyItemInput）
 */
const toVerifyItemInput = (row: VerifyRow): TreatmentCardVerifyItemInput => {
  const startISO = buildServiceTimestamp(row.serviceDate, row.startTime)
  // 结束时间优先用用户手动选择保存的日期+时间（支持跨天），缺省按项目时长自动计算
  const endISO = buildServiceTimestamp(
    row.endDate || row.serviceDate,
    row.endTime || calcEndTime(row.startTime, row.duration)
  )
  return {
    productId: row.productId,
    verifyTimes: row.verifyTimes,
    technicianId: row.technicianId,
    technicianSource: row.technicianSource,
    roomId: row.roomId,
    equipmentId: row.equipmentId,
    serviceStartTime: startISO,
    serviceEndTime: endISO,
    // 项目级备注透传：后端落库到核销项目明细 Remark 及关联订单明细 Remark
    remark: row.remark || undefined
  }
}

/**
 * 切换核销行服务项目：同步时长，重置已选资源（资源改由服务内容弹窗编辑录入，见 openVerifyRowEditor）
 * @param idx 行索引
 */
const handleVerifyRowProductChange = async (idx: number) => {
  const row = verifyItems.value[idx]
  if (!row) return
  // 服务项目列表可能尚未加载完成（核销弹窗独立打开场景），确保主档信息就绪后再按项目加载时长
  if (serviceProductOptions.value.length === 0) {
    await loadServiceProducts()
  }
  const product = serviceProductOptions.value.find(p => p.id === row.productId)
  row.duration = product?.duration
  // 项目变化后旧资源选择可能失效，整体重置（后续由服务内容弹窗重新录入）
  row.technicianId = undefined
  row.technicianSource = undefined
  row.roomId = undefined
  row.equipmentId = undefined
  // 项目变化后结束时间按新项目时长重新计算，清除手动保存值避免沿用旧项目时段
  row.endDate = undefined
  row.endTime = undefined
}

/**
 * 打开服务内容弹窗编辑核销行（复用服务项目商品的服务内容弹窗录入技师/房间/设备/服务时间）
 * 确认后由 applyVerifyRowFromServiceForm 回写核销弹窗中的该行（见 confirmServiceDialog）
 * @param row 核销编辑行
 */
const openVerifyRowEditor = async (row: VerifyRow) => {
  verifyEditingRow = row
  // 服务项目列表可能尚未加载完成（核销弹窗独立打开场景），确保主档信息就绪后再按项目加载候选
  if (serviceProductOptions.value.length === 0) {
    await loadServiceProducts()
  }
  resetServiceForm()
  serviceEditing = false
  serviceForm.productId = row.productId
  serviceForm.productName = row.productName || (row.productId != null ? getVerifyProductName(row.productId) : '')
  // 服务时间：核销行 日期 + 开始/结束(HH:mm) → 服务内容弹窗完整时间戳
  // 结束时间优先回填用户手动选择保存的值，缺省按项目时长自动计算（跨天时日期取结束日期）
  if (row.serviceDate && row.startTime) {
    serviceForm.startTime = `${row.serviceDate}T${row.startTime}:00`
    const endTime = row.endTime || calcEndTime(row.startTime, row.duration)
    if (endTime) {
      serviceForm.endTime = `${row.endDate || row.serviceDate}T${endTime}:00`
    }
  }
  // 技师：按来源映射回商家/平台技师选择（互斥，回填其一）
  if (row.technicianId) {
    if (row.technicianSource === 2) {
      serviceForm.platformTechnicianId = row.technicianId
    } else {
      serviceForm.merchantTechnicianId = row.technicianId
    }
  }
  serviceForm.technicianId = row.technicianId
  serviceForm.technicianSource = row.technicianSource ?? 1
  serviceForm.roomId = row.roomId
  serviceForm.equipmentId = row.equipmentId
  // 备注回填：核销行明细备注（服务内容弹窗录入）
  serviceForm.remark = row.remark || ''
  serviceDialogVisible.value = true
  await Promise.all([
    loadServiceTechnicians(),
    loadServiceRooms(),
    loadServiceEquipments()
  ])
  // 已回填服务时间时按该时段刷新资源占用标红
  if (serviceForm.startTime && serviceForm.endTime) {
    await loadServiceAvailability()
  }
}

/**
 * 服务内容弹窗确认后将表单回写核销行（仅回写资源与时间，次数仍在核销行内编辑）
 * @param row 目标核销编辑行
 */
const applyVerifyRowFromServiceForm = (row: VerifyRow) => {
  // 服务时间：完整时间戳 → 核销行 日期 + 开始时间(HH:mm)
  const start = serviceForm.startTime
  if (start) {
    const [date, time] = start.split('T')
    row.serviceDate = date
    row.startTime = time.slice(0, 5)
  }
  // 结束时间：完整时间戳 → 核销行 结束日期 + 结束时间(HH:mm)（跨天时结束日期不同）
  const end = serviceForm.endTime
  if (end) {
    const [date, time] = end.split('T')
    row.endDate = date
    row.endTime = time.slice(0, 5)
  }
  row.technicianId = serviceForm.technicianId
  row.technicianSource = serviceForm.technicianSource
  row.roomId = serviceForm.roomId
  row.equipmentId = serviceForm.equipmentId
  // 备注回写核销行（服务内容弹窗备注为项目级，核销提交时随明细透传落库）
  row.remark = serviceForm.remark || undefined
  // 时长缺失时按服务项目主档补全（提交时用于计算结束时间 ISO）
  if (!row.duration) {
    row.duration = serviceProductOptions.value.find(p => p.id === row.productId)?.duration
  }
}

// 默认添加一行（使用项目卡第一个未选且仍有剩余可核销次数的项目，避免默认选中已核销完的项目；服务日期默认当天，项目就绪后自动加载候选选项）
const addVerifyItem = async () => {
  if (!selectedCard.value) return
  const used = new Set(verifyItems.value.map(i => i.productId).filter(p => p != null) as number[])
  // 只取未使用且剩余可核销次数 > 0 的项目（remainingQuantity 由后端聚合返回，缺失时回退购买次数）；全核销完时不添加空行
  const next = selectedCard.value.items.find(i => !used.has(i.productId) && itemRemainingTimes(i.productId) > 0)
  if (next?.productId != null) {
    verifyItems.value.push({
      productId: next.productId,
      verifyTimes: 1,
      serviceDate: formatServiceToday(),
      startTime: ''
    })
    await handleVerifyRowProductChange(verifyItems.value.length - 1)
  }
}

// 删除一行
const removeVerifyItem = (idx: number) => {
  verifyItems.value.splice(idx, 1)
}

// 检查某项目是否已被其他行选中（用于禁用下拉选项）
const isProductSelected = (productId: number, currentIdx: number): boolean => {
  return verifyItems.value.some((item, idx) => idx !== currentIdx && item.productId === productId)
}

// 单项金额（B2 小数处理规则：以"分"为单位整数运算避免浮点误差；该项目最后一次核销时兜底 = 分摊总价值 - 已核销累计金额）
// 兜底仅在后端补充了 remainingQuantity 与 consumedAmount 时启用，否则回退分整数估算（仅前端预估，最终以服务端为准）
const rowSubAmount = (idx: number): number => {
  const row = verifyItems.value[idx]
  if (!row || !selectedCard.value) return 0
  const saleItem = selectedCard.value.items.find(i => i.productId === row.productId)
  if (!saleItem) return 0
  const times = row.verifyTimes || 0
  const remaining = saleItem.remainingQuantity
  const consumed = saleItem.consumedAmount
  if (remaining != null && consumed != null && remaining >= 1 && times >= remaining) {
    // 该项目最后一次核销：兜底 = 分摊总价值 - 已核销累计金额
    const totalFen = Math.round(saleItem.allocatedTotalPrice * 100)
    const consumedFen = Math.round(consumed * 100)
    return Math.max(0, totalFen - consumedFen) / 100
  }
  // 分整数运算：折算单价(分) × 次数
  const unitFen = Math.round(saleItem.allocatedUnitPrice * 100)
  return (unitFen * times) / 100
}

const totalAmount = computed(() => {
  return Number(verifyItems.value.reduce((sum, _, idx) => sum + rowSubAmount(idx), 0).toFixed(2))
})

const totalTimes = computed(() => {
  return verifyItems.value.reduce((sum, row) => sum + (row.verifyTimes || 0), 0)
})

/** 某项目在选中项目卡中的剩余可核销次数（后端销售明细 remainingQuantity 返回；字段缺失时回退购买次数） */
const itemRemainingTimes = (productId: number | undefined): number => {
  if (productId == null || !selectedCard.value) return 0
  const saleItem = selectedCard.value.items.find(i => i.productId === productId)
  return saleItem?.remainingQuantity ?? saleItem?.quantity ?? 0
}

/** 选中项目卡中仍有剩余可核销次数的项目数（用于限制核销弹窗"添加项目"行数上限，避免点选已核销完的项目） */
const availableVerifyProductCount = computed(() => {
  if (!selectedCard.value) return 0
  return selectedCard.value.items.filter(i => itemRemainingTimes(i.productId) > 0).length
})

// 单行最大次数 = min(该项目剩余可核销次数, 剩余总次数 - 其他行已占用次数)
const maxTimesForRow = (idx: number): number => {
  if (!selectedCard.value) return 1
  const row = verifyItems.value[idx]
  const otherTimes = verifyItems.value.reduce((sum, r, i) => i === idx ? sum : sum + (r.verifyTimes || 0), 0)
  return Math.max(1, Math.min(itemRemainingTimes(row?.productId), selectedCard.value.remainingTimes - otherTimes))
}

// 校验：所有行都已选项目 + 每行必录服务日期与开始时间 + 总次数不超过剩余次数 + 单一项目次数不超过其剩余可核销次数
const canConfirmVerify = computed(() => {
  if (!selectedCardSaleId.value) return false
  if (verifyItems.value.length === 0) return false
  if (verifyItems.value.some(r => r.productId == null || r.verifyTimes < 1)) return false
  if (verifyItems.value.some(r => !r.serviceDate || !r.startTime)) return false
  if (totalTimes.value > (selectedCard.value?.remainingTimes || 0)) return false
  if (verifyItems.value.some(r => r.productId != null && r.verifyTimes > itemRemainingTimes(r.productId))) return false
  return true
})

const handleVerifyCard = () => {
  // 核销复用购物车上方已选会员（与开卡流程一致），未选则拦截提示
  if (!selectedMember.value) {
    ElMessage.warning('请先选择会员')
    return
  }
  verifyEditingLine = null
  verifyDialogVisible.value = true
  cardList.value = []
  selectedCardSaleId.value = undefined
  verifyItems.value = []
  // 预加载服务项目列表：核销行需要按项目反查时长/所需房间类型/关联设备类型来加载候选选项
  loadServiceProducts()
  // 直接按已选会员查询其有效项目卡（弹窗内不再搜手机号）
  loadCustomerCards()
}

const loadCustomerCards = async () => {
  const member = selectedMember.value
  if (!member) {
    cardList.value = []
    return
  }
  try {
    // 按购物车上方已选会员查询其有效项目卡（status=1）
    const cardRes = await getTreatmentCardSales({ customerId: member.id, status: 1, pageSize: 1000 })
    cardList.value = cardRes.list
    // 编辑模式（从购物车核销详情"编辑核销信息"进入）：按购物车行固化的 cardSaleId 匹配卡并回填核销项目，不默认选中第一张卡
    if (verifyEditingLine) {
      const line = cart.value.find(i => i.lineId === verifyEditingLine)
      const target = line ? cardRes.list.find(c => c.id === line.cardSaleId) : undefined
      if (target) {
        selectedCardSaleId.value = target.id
        // target 由 line 的 cardSaleId 匹配得出，此处 line 必然存在
        verifyItems.value = (line!.verifyItems ?? []).map(toVerifyRowFromCart)
      } else {
        // 该卡已核销完/过期，编辑失去核销上下文，放弃编辑并提示
        ElMessage.warning('该项目卡已无法核销（可能已用完或过期）')
        verifyEditingLine = null
        verifyDialogVisible.value = false
        if (cardRes.list.length > 0) {
          selectedCardSaleId.value = cardRes.list[0].id
          await onCardSelected()
        }
      }
      return
    }
    // 默认选中第一张卡并预填一行核销项目
    if (cardRes.list.length > 0) {
      selectedCardSaleId.value = cardRes.list[0].id
      await onCardSelected()
    }
  } catch (e) {
    cardList.value = []
    ElMessage.error('查询项目卡失败: ' + (e as Error).message)
  }
}

/** 点击左栏项目卡卡片选中（与下拉切换一致：重置核销项目并预填一行） */
const onCardSelect = (card: TreatmentCardSale) => {
  if (selectedCardSaleId.value === card.id) return
  selectedCardSaleId.value = card.id
  onCardSelected()
}

/** 项目卡卡名（优先后端 join 返回的 cardName，其次按 cardId 从项目卡配置匹配，匹配不到回退卡销售ID） */
const getVerifyCardName = (card: TreatmentCardSale): string =>
  card.cardName || treatmentCards.value.find(c => c.id === card.cardId)?.name || `项目卡#${card.id}`

/** 核销弹窗到期日期展示（截取 YYYY-MM-DD） */
const formatVerifyExpiry = (expiryDate: string): string => expiryDate.slice(0, 10)

/** 核销项目名（优先服务项目主档商品名，其次项目卡配置 items，匹配不到回退项目ID；用于下拉展示） */
const getVerifyProductName = (productId: number): string => {
  const master = serviceProductOptions.value.find(p => p.id === productId)
  if (master?.name) return master.name
  for (const c of treatmentCards.value) {
    const it = c.items.find(i => i.productId === productId)
    if (it?.productName) return it.productName
  }
  return `项目#${productId}`
}

const onCardSelected = async () => {
  // 切换项目卡时重置核销项目（默认添加一行，等待候选选项加载完成避免选项滞后）
  verifyItems.value = []
  await addVerifyItem()
}

// 将核销选择加入购物车（不立即核销；结算时随订单一起走混合结算，见 handleConfirmPay）
// 编辑模式（从购物车核销详情"编辑核销信息"进入）按 lineId 更新已有行，不新增
// 加入购物车前做资源占用硬校验（落库 + 购物车自检），冲突则阻止加购
const confirmVerifyCard = async () => {
  const cardSaleId = selectedCardSaleId.value
  if (!cardSaleId) {
    ElMessage.warning('请选择项目卡')
    return
  }
  if (!canConfirmVerify.value) {
    ElMessage.warning('核销项目不合法（项目未选、次数小于 1、未录服务日期/开始时间或总次数超限）')
    return
  }
  const card = selectedCard.value
  if (!card) return

  // 卡名：优先后端 join 返回的 cardName，其次按 cardId 从项目卡配置列表匹配
  const cardName = card.cardName || treatmentCards.value.find(c => c.id === card.cardId)?.name || `项目卡#${cardSaleId}`
  // 项目名：后端 SaleItem 无 productName，从项目卡配置 items 匹配（仅前端展示，提交只传业务字段）
  const productNameMap = new Map<number, string>()
  treatmentCards.value.forEach(c => c.items.forEach(it => {
    if (it.productName) productNameMap.set(it.productId, it.productName)
  }))
  // 核销项目透传：技师/房间/设备/服务时间（toVerifyItemInput 内计算 ISO 时间戳），后端核销时按服务商品占用资源并归集技师统计
  const nextVerifyItems = verifyItems.value.map(row => ({
    ...toVerifyItemInput(row),
    productName: row.productId != null ? productNameMap.get(row.productId) : undefined
  }))

  // 资源占用硬校验（落库 + 购物车自检）：逐核销项检查所选资源在目标时段是否被占用
  // 自检排除自身行（verifyEditingLine，编辑模式更新已有行），仅校验与其他行/落库的冲突
  const selfLineId = verifyEditingLine ?? undefined
  for (const vi of nextVerifyItems) {
    if (!vi.serviceStartTime || !vi.serviceEndTime) continue
    if (!vi.technicianId && !vi.roomId && !vi.equipmentId) continue
    const itemName = vi.productName ?? '核销项目'
    const cartConflict = findCartResourceConflict({
      technicianId: vi.technicianId,
      roomId: vi.roomId,
      equipmentId: vi.equipmentId,
      startTime: vi.serviceStartTime,
      endTime: vi.serviceEndTime
    }, selfLineId)
    if (cartConflict) {
      ElMessage.warning(`无法加入购物车：「${itemName}」${cartConflict}`)
      return
    }
    const persistedConflict = await checkPersistedConflict(vi.serviceStartTime, vi.serviceEndTime, {
      technician: vi.technicianId ? { id: vi.technicianId } : undefined,
      room: vi.roomId ? { id: vi.roomId } : undefined,
      equipment: vi.equipmentId ? { id: vi.equipmentId } : undefined
    })
    if (persistedConflict) {
      ElMessage.warning(`无法加入购物车：「${itemName}」${persistedConflict}，请调整时间或更换资源`)
      return
    }
  }

  // 服务项目绑定耗材效期选择：核销项目本质是服务商品，汇总所有项目的耗材需求，一次列表式弹窗选择效期
  // 任一耗材库存不足则阻止加购；店员取消则中止；确认的选择按核销项目索引写回 nextVerifyItems[].consumableExpiries
  const verifyRequests: { requestIndex: number; productId: number; quantity: number }[] = []
  nextVerifyItems.forEach((vi, idx) => {
    if (vi.productId != null) verifyRequests.push({ requestIndex: idx, productId: vi.productId, quantity: vi.verifyTimes })
  })
  // 编辑模式回显：核销弹窗行已有耗材效期选择（编辑核销信息回填时保留，见 toVerifyRowFromCart），按行索引传入弹窗初始化回填；新增核销无选择则为空 Map
  const prevVerifyExpiries = new Map<number, ConsumableExpiry[]>()
  verifyItems.value.forEach((vi, idx) => {
    if (vi.consumableExpiries?.length) prevVerifyExpiries.set(idx, vi.consumableExpiries)
  })
  const consumableResult = await selectServiceConsumables(verifyRequests, prevVerifyExpiries)
  if (consumableResult === null) return
  if (consumableResult) {
    verifyRequests.forEach((req, i) => {
      nextVerifyItems[req.requestIndex].consumableExpiries = consumableResult[i]
    })
  }

  // 编辑模式（从购物车核销详情"编辑核销信息"进入）按 lineId 更新已有行，不新增
  const existing = verifyEditingLine
    ? cart.value.find(i => i.lineId === verifyEditingLine)
    : undefined
  if (existing) {
    Object.assign(existing, {
      name: cardName,
      price: totalAmount.value,
      cardName,
      remainingTimes: card.remainingTimes,
      verifyItems: nextVerifyItems
    })
    verifyDialogVisible.value = false
    verifyEditingLine = null
    ElMessage.success('核销信息已更新')
    return
  }

  cart.value.push({
    lineId: genCartLineId(cardSaleId),
    id: `v-${cardSaleId}-${Date.now()}`,
    name: cardName,
    code: '',
    spec: '',
    // 核销行金额为展示用（折算金额），扣卡次不收款，不计入应付
    price: totalAmount.value,
    quantity: 1,
    productType: 2,
    lineType: 'verify',
    cardSaleId,
    cardName,
    remainingTimes: card.remainingTimes,
    verifyItems: nextVerifyItems
  })

  verifyDialogVisible.value = false
  ElMessage.success('已加入购物车，结算时一并核销')
}

// ==================== 核销详情弹窗（点击购物车核销行查看，含编辑入口） ====================
const verifyDetailVisible = ref(false)
/** 核销详情数据：购物车核销行 + 打开时刷新当前会员有效卡列表反查的销售记录（卡可能已核销完/过期而缺失，缺失时卡信息回退行内固化值，编辑入口不可用） */
interface VerifyDetailData {
  line: CartItem
  card?: TreatmentCardSale
}
const verifyDetail = ref<VerifyDetailData | null>(null)

/** 打开购物车核销行详情弹窗：刷新当前会员有效卡列表反查该卡最新状态（剩余次数/到期日期/项目折算单价），反查不到回退行内固化值 */
const openCartVerifyDetail = async (item: CartItem) => {
  verifyDetail.value = { line: item }
  verifyDetailVisible.value = true
  await loadCustomerCards()
  verifyDetail.value = { line: item, card: cardList.value.find(c => c.id === item.cardSaleId) }
}

/** 核销详情明细行：购物车核销行固化项目 + 反查的折算单价/金额（反查不到时金额为 undefined 展示 -） */
const verifyDetailItems = computed(() => {
  const d = verifyDetail.value
  if (!d) return []
  const saleItems = d.card?.items ?? []
  return (d.line.verifyItems ?? []).map(row => {
    const saleItem = saleItems.find(i => i.productId === row.productId)
    return {
      ...row,
      productName: row.productName || (row.productId != null ? getVerifyProductName(row.productId) : ''),
      amount: saleItem ? saleItem.allocatedUnitPrice * row.verifyTimes : undefined,
      serviceTimeText: row.serviceStartTime
        ? `${formatServiceDateTime(row.serviceStartTime)} - ${formatServiceDateTime(row.serviceEndTime)}`
        : '-'
    }
  })
})

/** 核销详情合计次数 */
const verifyDetailTotalTimes = computed(() => {
  return (verifyDetail.value?.line.verifyItems ?? []).reduce((sum, r) => sum + (r.verifyTimes || 0), 0)
})

/** 编辑核销信息：关闭详情弹窗，复用核销弹窗编辑模式回填（编辑模式更新已有行不新增，见 loadCustomerCards/confirmVerifyCard） */
const editVerifyDetail = async () => {
  if (!verifyDetail.value) return
  const line = verifyDetail.value.line
  verifyDetailVisible.value = false
  verifyEditingLine = line.lineId
  verifyDialogVisible.value = true
  cardList.value = []
  selectedCardSaleId.value = undefined
  verifyItems.value = []
  // 先确保服务项目主档就绪（回填时按项目反查时长），再加载会员卡列表走编辑模式回填
  await loadServiceProducts()
  loadCustomerCards()
}

/**
 * 将购物车核销行固化的项目输入（ISO 时间戳）转换为核销弹窗编辑行（VerifyRow）
 * 服务时间由 ISO 时间戳拆分回 日期 + HH:mm；时长按服务项目主档反查（缺失可空，提交时结束时间由后端按项目时长推算）
 * @param item 购物车核销行固化的项目输入
 * @returns 核销编辑行
 */
const toVerifyRowFromCart = (item: TreatmentCardVerifyItemInput & { productName?: string }): VerifyRow => {
  let serviceDate = ''
  let startTime = ''
  const start = item.serviceStartTime
  if (start) {
    const [d, t] = start.split('T')
    serviceDate = d
    startTime = t ? t.slice(0, 5) : ''
  }
  // 结束时间：从购物车行固化的 ISO 拆分出 结束日期 + 结束时间(HH:mm)（跨天时结束日期不同）
  let endDate: string | undefined
  let endTime = ''
  const end = item.serviceEndTime
  if (end) {
    const [d, t] = end.split('T')
    endDate = d
    endTime = t ? t.slice(0, 5) : ''
  }
  const product = item.productId != null ? serviceProductOptions.value.find(p => p.id === item.productId) : undefined
  return {
    productId: item.productId,
    verifyTimes: item.verifyTimes,
    technicianId: item.technicianId,
    technicianSource: item.technicianSource,
    roomId: item.roomId,
    equipmentId: item.equipmentId,
    productName: item.productName,
    duration: product?.duration,
    serviceDate,
    startTime,
    endDate,
    endTime,
    remark: item.remark,
    // 透传已选耗材效期：编辑核销信息回填后，核销行"编辑"（openVerifyRowEditor）重选效期时能回显原选择
    consumableExpiries: item.consumableExpiries
  }
}

// ==================== 预约转订单弹窗 ====================
const route = useRoute()
const appointmentDialogVisible = ref(false)
const todayAppointments = ref<Appointment[]>([])
const appointmentLoading = ref(false)
const selectedAppointmentId = ref<number | undefined>(undefined)
const appointmentTableRef = ref<TableInstance>()

/**
 * 预约状态文本（与预约列表页保持一致）
 * @param status 预约状态：1-已预约，2-已到店，3-已完成，4-已取消，5-爽约
 * @returns 状态文本
 */
const getStatusText = (status: number): string => {
  const map: Record<number, string> = { 1: '已预约', 2: '已到店', 3: '已完成', 4: '已取消', 5: '爽约' }
  return map[status] || '未知'
}

/**
 * 预约状态标签类型（与预约列表页保持一致）
 * @param status 预约状态
 * @returns el-tag 的 type
 */
const getStatusTagType = (status: number): '' | 'success' | 'info' | 'warning' | 'danger' => {
  const map: Record<number, '' | 'success' | 'info' | 'warning' | 'danger'> = {
    1: '',
    2: 'success',
    3: 'info',
    4: 'info',
    5: 'danger'
  }
  // 用 ?? 而非 ||：空字符串（已预约默认蓝色）是合法值，不能被 falsy 判断替换成 info 灰色
  return map[status] ?? 'info'
}

/**
 * 当前选中行变化时同步选中预约（highlight-current-row 高亮行即选中数据，单选其中任一条）
 * @param currentRow 当前高亮行（setCurrentRow(null) 清空高亮时为空）
 */
const handleAppointmentRowChange = (currentRow: Appointment | null) => {
  selectedAppointmentId.value = currentRow?.id
}

/**
 * 将预约客户自动设置为购物车会员（预约转单时调用，避免店员重复搜索选择客户）
 * 仅当预约关联了客户档案（customerId 有效）时加载；散客（未关联档案，仅冗余姓名手机号）跳过，
 * 保持购物车会员现状（订单作散客结算，与快速开单散客行为一致）
 * @param appt 预约对象（含 customerId/customerName/customerPhone 冗余字段）
 */
const loadAppointmentCustomer = async (appt: Appointment): Promise<void> => {
  // 散客跳过：预约允许仅填姓名手机号不关联客户档案（customerId 为 0/空）
  if (!appt.customerId || String(appt.customerId) === '0') return
  try {
    // 注意：customerId 为雪花ID（运行时字符串），getCustomer 拼接 URL 保留精度，严禁 Number() 转换
    const customer = await getCustomer(appt.customerId as number)
    // 直接覆盖当前会员：预约转单语义即该预约客户来店消费，无需弹窗确认
    selectedMember.value = {
      id: customer.id,
      name: customer.name,
      phone: customer.phone,
      levelName: customer.levelName || '普通会员',
      balance: customer.balance,
      discountRate: getDiscountRateByLevelId(customer.levelId),
      totalPoints: customer.totalPoints
    }
    // 重置搜索框选中值（memberResults 候选列表不含该客户，回显会显示裸 ID 或与会员卡不一致）
    selectedMemberId.value = null
  } catch {
    // 客户档案加载失败不阻塞转单加购，保持购物车会员现状
  }
}

/**
 * 将指定预约的商品加入购物车，并继承预约的技师/房间/设备到购物车明细
 * sourceAppointmentId 供后端 OrderAppService.CreateAsync 复制资源（双保险）
 * @param appt 预约对象
 */
const addAppointmentToCart = async (appt: Appointment) => {
  // 通过 productId 直接查找商品并加入购物车（替代原按 serviceItem 名称匹配）
  if (!appt.productId) {
    ElMessage.info('该预约未指定服务项目，请手动添加商品')
    return
  }
  const matched = allProducts.value.find(p => p.id === appt.productId)
  if (!matched) {
    ElMessage.info(`未找到商品（ID: ${appt.productId}），请手动添加`)
    return
  }
  // 预约转订单：资源已由预约确定（技师/房间/设备/服务时间），直接加购不弹服务内容弹窗
  // pushProductToCart 始终新增一行；设置资源后再与购物车中"同商品同资源配置"的行合并（叠加数量，与弹窗加购语义一致）
  const cartItem = await addToCart(matched, { skipDialog: true })
  if (cartItem) {
    // 预约服务项目绑定耗材效期选择（与左侧直接加购服务项目一致：列表式弹窗选择 + 库存校验）
    // 任一耗材库存不足或店员取消则移除该行并中止，避免残留半成品行
    const consumables = await selectServiceConsumables([
      { requestIndex: 0, productId: matched.id, quantity: 1 }
    ])
    if (consumables === null) {
      removeFromCart(cartItem)
      return
    }
    cartItem.consumableExpiries = consumables?.[0]
    // 继承预约的技师/房间/设备到购物车，提交时再透传给 OrderItem（技师名称/来源直接取自预约记录）
    // 后端 OrderAppService.CreateAsync 也会通过 sourceAppointmentId 复制（双保险）
    if (appt.technicianId) {
      cartItem.technicianId = appt.technicianId
      cartItem.technicianName = appt.technicianName
      cartItem.technicianSource = appt.technicianSource
    }
    if (appt.roomId) {
      cartItem.roomId = appt.roomId
      // 同步复制房间名称供详情弹窗展示（仅设 id 会导致详情弹窗直接读 roomName 字段时显示 '-'）
      cartItem.roomName = appt.roomName
    }
    if (appt.equipmentId) {
      cartItem.equipmentId = appt.equipmentId
      // 同步复制设备名称供详情弹窗展示（与房间一致）
      cartItem.equipmentName = appt.equipmentName
    }
    // 继承预约的服务时间（一体格式开始时间直接使用，结束时间取预约记录的 EndTime），
    // 供后端占用检测与技师统计按服务时间日归集（与后端 sourceAppointmentId 复制逻辑双保险）
    if (appt.startTime) {
      cartItem.serviceStartTime = appt.startTime
      cartItem.serviceEndTime = appt.endTime
    }
    cartItem.sourceAppointmentId = appt.id
    // 同商品同资源配置的行合并（例如同预约重复转单叠加数量），不同配置保持独立行
    // 合并前按合并后数量校验绑定耗材库存并重算需求（不足则移除本次行并中止，避免需求缺口）
    const sameCfg = cart.value.find(i => i.lineId !== cartItem.lineId && isSameServiceConfig(i, cartItem))
    if (sameCfg) {
      const mergedQty = sameCfg.quantity + cartItem.quantity
      if (sameCfg.consumableExpiries?.length) {
        const err = await checkServiceConsumableCapacity(sameCfg, mergedQty)
        if (err) {
          removeFromCart(cartItem)
          ElMessage.warning(err)
          return
        }
      }
      sameCfg.quantity = mergedQty
      removeFromCart(cartItem)
    }
  }
  // 加购成功后自动将预约客户带到购物车（覆盖当前会员，无需店员重复搜索选择）
  await loadAppointmentCustomer(appt)
  ElMessage.success(`已添加商品：${matched.name}`)
}

/**
 * 从预约列表页跳转进入（query 携带 appointmentId）：加载指定预约并自动加入购物车
 * 支持非今日 / 已到店状态的预约转单（预约列表页"更多"下拉的"预约转订单"入口）
 * @param appointmentId 预约ID（字符串，雪花ID超 JS 安全整数，严禁 Number() 转换，否则精度丢失查不到记录）
 */
const handleAppointmentToOrderById = async (appointmentId: string) => {
  try {
    const appt = await getAppointment(appointmentId)
    await addAppointmentToCart(appt)
  } catch (e) {
    ElMessage.error('加载预约失败: ' + (e as Error).message)
  }
}

const handleAppointmentToOrder = async () => {
  // 预约转单必须选择会员（与项目卡核销按钮一致）：转单即该会员来店消费，按该会员筛选可转单预约列表
  if (!selectedMember.value) {
    ElMessage.warning('请先选择会员')
    return
  }
  try {
    appointmentLoading.value = true
    // 按当前购物车会员筛选可转单预约：状态为 1已预约 / 2已到店（终态不可转）
    const res = await getAppointments({
      statuses: [1, 2],
      customerId: selectedMember.value.id,
      pageIndex: 1,
      pageSize: 1000
    })
    todayAppointments.value = res.list
    // 打开弹窗时重置选中项与高亮行，避免上次选择的预约残留（首次打开时表格尚未挂载，ref 为空，跳过即可）
    selectedAppointmentId.value = undefined
    appointmentTableRef.value?.setCurrentRow(null)
    appointmentDialogVisible.value = true
  } catch (e) {
    ElMessage.error('加载可转单预约失败: ' + (e as Error).message)
  } finally {
    appointmentLoading.value = false
  }
}

const confirmAppointmentToOrder = async () => {
  const appt = todayAppointments.value.find(a => a.id === selectedAppointmentId.value)
  if (!appt) {
    ElMessage.warning('请选择预约')
    return
  }
  await addAppointmentToCart(appt)
  appointmentDialogVisible.value = false
}

// ==================== 工具方法 ====================
const formatPrice = (price: number) => {
  return price.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ',')
}

onMounted(async () => {
  await Promise.all([
    loadCategories(),
    loadProducts(),
    loadGifts(),
    loadTreatmentCards(),
    loadCustomerLevels(),
    loadActivityOptions()
  ])
  // 预约列表页"预约转订单"跳转进入（已在预约列表页确认）：自动将该预约的商品加入购物车
  const appointmentId = route.query.appointmentId
  if (appointmentId) {
    // 注意：雪花ID超过 JS 安全整数，直接以字符串传递，不能 Number() 转换（否则精度丢失后端查不到）
    handleAppointmentToOrderById(appointmentId as string)
  }
})
</script>

<style scoped>
.pos-page {
  width: 100%;
  height: 100%;
}

/* 服务内容弹窗：服务项目只读展示（由点击的商品确定，禁止更改） */
.service-product-name {
  line-height: 32px;
  font-weight: 500;
}

/* 资源占用标红（服务内容弹窗技师/房间/设备选择列表中的已占用条目） */
.resource-occupied {
  color: var(--el-color-danger, #f56c6c);
  font-weight: 600;
}

/* 技师下拉选项：名称 + 技能展示（选择技师时展示技能） */
.technician-option {
  display: flex;
  flex-direction: column;
  line-height: 1.4;
}

.technician-skill {
  font-size: 12px;
  color: var(--text-tertiary);
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

/* 项目卡卡片标识（沿用商品卡片布局，仅边框强调开卡语义） */
.treatment-card {
  border-color: rgba(99, 102, 241, 0.4);
}

.treatment-card:hover {
  border-color: #6366f1;
}

/* 项目卡销售弹窗 */
.sale-section :deep(.el-descriptions) {
  margin-bottom: 4px;
}

.sale-form {
  margin-top: 16px;
}

/* 购物车赠品行 */
.cart-item.gift-item {
  background: rgba(251, 191, 36, 0.05);
}

.cart-item.gift-item:hover {
  background: rgba(251, 191, 36, 0.1);
}

/* 赛博霓虹徽章：半透明深底 + 发光描边 + 辉光文字。
   用于购物车商品行的类型标识（服务/核销/开卡/赠品），
   与左侧商品区青色科技风衔接，徽章本身以霓虹四色做类型区分。 */
.neon-tag {
  flex-shrink: 0;
  display: inline-flex;
  align-items: center;
  margin-left: 6px;
  padding: 0 6px;
  height: 17px;
  border-radius: 4px;
  font-size: 10px;
  font-weight: 600;
  letter-spacing: 0.08em;
  border: 1px solid;
  background: rgba(8, 12, 22, 0.55);
  vertical-align: 1px;
}

.neon-service {
  color: #7df4ff;
  border-color: rgba(0, 240, 255, 0.45);
  background: rgba(0, 240, 255, 0.08);
  text-shadow: 0 0 8px rgba(0, 240, 255, 0.7);
  box-shadow: 0 0 8px rgba(0, 240, 255, 0.25), inset 0 0 6px rgba(0, 240, 255, 0.12);
}

.neon-appointment {
  color: #ffb86b;
  border-color: rgba(255, 184, 107, 0.45);
  background: rgba(255, 184, 107, 0.08);
  text-shadow: 0 0 8px rgba(255, 184, 107, 0.7);
  box-shadow: 0 0 8px rgba(255, 184, 107, 0.25), inset 0 0 6px rgba(255, 184, 107, 0.12);
}

.neon-verify {
  color: #6fffd0;
  border-color: rgba(52, 245, 197, 0.45);
  background: rgba(52, 245, 197, 0.08);
  text-shadow: 0 0 8px rgba(52, 245, 197, 0.7);
  box-shadow: 0 0 8px rgba(52, 245, 197, 0.25), inset 0 0 6px rgba(52, 245, 197, 0.12);
}

.neon-sale {
  color: #c4b5fd;
  border-color: rgba(167, 139, 250, 0.45);
  background: rgba(167, 139, 250, 0.08);
  text-shadow: 0 0 8px rgba(167, 139, 250, 0.7);
  box-shadow: 0 0 8px rgba(167, 139, 250, 0.25), inset 0 0 6px rgba(167, 139, 250, 0.12);
}

.neon-gift {
  color: #ff9ff0;
  border-color: rgba(255, 92, 225, 0.45);
  background: rgba(255, 92, 225, 0.08);
  text-shadow: 0 0 8px rgba(255, 92, 225, 0.7);
  box-shadow: 0 0 8px rgba(255, 92, 225, 0.25), inset 0 0 6px rgba(255, 92, 225, 0.12);
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

/* ========== 会员选择 ========== */
/* 搜索区：会员标签 + 远程搜索框 */
.member-bar {
  padding: 12px 20px 10px;
  border-bottom: 1px solid var(--border-primary);
}

.member-search {
  display: flex;
  align-items: center;
  gap: 8px;
}

/* "会员" 语义标签，提升区域辨识度 */
.member-search-label {
  flex-shrink: 0;
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 6px 10px;
  font-size: 12px;
  font-weight: 600;
  color: var(--primary);
  background: var(--bg-glow);
  border: 1px solid var(--border-glow);
  border-radius: var(--radius-md);
  white-space: nowrap;
}

.member-search-label .el-icon {
  font-size: 13px;
}

.member-input {
  flex: 1;
}

/* 未选择时的权益提示 */
.member-hint {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-top: 8px;
  font-size: 11px;
  color: var(--text-tertiary);
}

.member-hint .hint-icon {
  color: var(--primary);
  font-size: 13px;
}

/* ========== 发光会员卡（选中会员后展开） ========== */
.member-card {
  margin: 10px 12px 12px;
  position: relative;
  overflow: hidden;
  border-radius: var(--radius-lg);
  background:
    linear-gradient(135deg, rgba(6, 212, 228, 0.14) 0%, rgba(8, 145, 178, 0.05) 60%, transparent 100%),
    var(--bg-elevated);
  border: 1px solid var(--border-glow);
  box-shadow: 0 0 24px rgba(6, 212, 228, 0.18), inset 0 1px 0 rgba(255, 255, 255, 0.04);
  animation: member-card-in 0.35s cubic-bezier(0.22, 1, 0.36, 1);
}

/* 顶部渐变辉光线条 */
.member-card::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 2px;
  background: linear-gradient(90deg, transparent, var(--primary-hover), var(--primary), transparent);
}

/* 头部：头像 + 身份 + 移除 */
.member-card-head {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 16px 16px 12px;
}

/* 赛博霓虹会员头像：青→紫→粉三色渐变 + 内外双层发光光环，
   模拟霓虹灯管效果，作为购物车区域霓虹视觉的焦点锚点 */
.member-avatar {
  flex-shrink: 0;
  width: 48px;
  height: 48px;
  border-radius: 50%;
  background: linear-gradient(135deg, #00f0ff 0%, #7c6cff 55%, #ff5ce1 135%);
  border: 1px solid rgba(255, 255, 255, 0.35);
  display: flex;
  align-items: center;
  justify-content: center;
  color: #04060e;
  font-weight: 700;
  font-size: 18px;
  box-shadow:
    0 0 0 3px rgba(0, 240, 255, 0.22),
    0 0 14px rgba(0, 240, 255, 0.55),
    0 0 30px rgba(124, 108, 255, 0.4),
    inset 0 0 10px rgba(255, 255, 255, 0.25);
}

.member-identity {
  flex: 1;
  min-width: 0;
}

.member-name {
  font-size: 15px;
  color: var(--text-primary);
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 8px;
}

/* 等级徽章：金色渐变，辨识度突出 */
.member-tag {
  flex-shrink: 0;
  padding: 1px 8px;
  font-size: 11px;
  font-weight: 600;
  color: #fff;
  background: var(--gradient-orange);
  border-radius: 999px;
  box-shadow: 0 0 8px rgba(251, 191, 36, 0.35);
}

.member-phone {
  margin-top: 4px;
  font-size: 12px;
  color: var(--text-secondary);
  font-family: 'JetBrains Mono', monospace;
  letter-spacing: 0.5px;
}

.member-remove {
  flex-shrink: 0;
  align-self: flex-start;
  margin: -6px -6px 0 0;
  cursor: pointer;
  padding: 4px;
  font-size: 18px; /* 与购物车商品关闭按钮(cart-remove)字号一致 */
  color: var(--text-tertiary);
  transition: color 0.3s;
}

.member-remove:hover {
  color: var(--danger);
}

/* 数据面板：储值 / 积分 / 折扣 */
.member-metrics {
  display: flex;
  align-items: stretch;
  padding: 12px 16px;
  border-top: 1px dashed var(--border-primary);
  background: rgba(6, 212, 228, 0.03);
}

.member-metric {
  flex: 1;
  min-width: 0;
  text-align: center;
}

.metric-label {
  font-size: 11px;
  color: var(--text-tertiary);
}

.metric-value {
  margin-top: 4px;
  font-family: 'JetBrains Mono', monospace;
  font-size: 15px;
  font-weight: 600;
}

.metric-balance {
  color: var(--primary);
}

.metric-points {
  color: var(--warning);
}

.metric-discount {
  color: var(--success);
}

.metric-divider {
  width: 1px;
  margin: 2px 10px;
  background: var(--border-primary);
}

/* 会员卡入场动画 */
@keyframes member-card-in {
  from {
    opacity: 0;
    transform: translateY(-8px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
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

/* 商品/服务行：可点击查看详情弹窗（服务项目看服务详情，商品/耗材/赠品看商品详情） */
.cart-info-clickable {
  cursor: pointer;
}

.cart-info-clickable:hover .cart-name {
  color: var(--primary);
}

.cart-name {
  font-size: 13px;
  color: var(--text-primary);
  margin-bottom: 4px;
}

.cart-price {
  font-size: 13px;
  color: var(--primary);
  font-weight: 600;
  font-family: 'JetBrains Mono', monospace;
  min-width: 70px;
  text-align: right;
}

/* 核销行金额仅展示不参与收款（subtotal 排除 verify 行），价格加删除线并弱化颜色 */
.cart-price.price-strike {
  color: #8a8f98;
  font-weight: 400;
  text-decoration: line-through;
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

/* 赛博霓虹快捷按钮（挂单/取单/核销/预约转单/清空）：
   近黑半透明底 + 冷色霓虹描边，hover 时青霓虹点亮描边与文字并泛起光晕 */
.quick-btn {
  flex: 1;
  padding: 8px;
  background: rgba(13, 18, 32, 0.6);
  border: 1px solid rgba(120, 200, 255, 0.18);
  border-radius: var(--radius-sm);
  color: var(--text-secondary);
  font-size: 12px;
  letter-spacing: 0.04em;
  cursor: pointer;
  transition: all 0.3s;
}

.quick-btn:hover {
  border-color: rgba(0, 240, 255, 0.6);
  background: rgba(0, 240, 255, 0.06);
  color: #7df4ff;
  text-shadow: 0 0 8px rgba(0, 240, 255, 0.6);
  box-shadow: 0 0 14px rgba(0, 240, 255, 0.25), inset 0 0 8px rgba(0, 240, 255, 0.08);
}

/* 支付弹窗 */
.pay-amount {
  text-align: center;
  margin-bottom: 24px;
}

.pay-amount .label {
  font-size: 14px;
  /* 弹窗为白色背景，不能用深色主题的浅色文字变量（--text-tertiary 近透明），改用弹窗正文深灰 */
  color: #4b5563;
  margin-bottom: 8px;
}

.pay-amount .value {
  font-size: 36px;
  font-weight: 700;
  /* 白底上用加深的青色调（--primary-dim），保留品牌色同时保证对比度 */
  color: #0891b2;
  font-family: 'JetBrains Mono', monospace;
}

.pay-methods {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 8px;
  margin-bottom: 16px;
}

/* 支付方式卡片样式与效期选择弹窗 .expiry-option 一致（浅色浮层风格） */
.pay-method {
  padding: 10px 8px;
  background: #f5f7fa;
  border: 2px solid #e5e7eb;
  border-radius: var(--radius-md);
  cursor: pointer;
  transition: all 0.2s;
  text-align: center;
}

.pay-method:hover {
  border-color: #d1d5db;
}

.pay-method.selected {
  border-color: var(--primary);
  background: rgba(6, 212, 228, 0.15);
}

/* 组合支付模式下，线下收款方式选中效果：与最外层支付方式选中（青色）区分，采用橙色描边 + 浅橙底 */
.pay-method.offline-selected {
  border-color: #f59e0b;
  background: rgba(251, 191, 36, 0.16);
}

.pay-method .icon {
  font-size: 20px;
  margin-bottom: 4px;
}

.pay-method .icon .el-icon {
  font-size: 20px;
}

.pay-method .name {
  font-size: 12px;
  color: #1f2937;
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

/* 商品详情弹窗：效期批次明细（浅色浮层风格，适配白色弹窗） */
.product-detail-expiry {
  padding: 12px;
  background: #f5f7fa;
  border: 1px solid #e5e7eb;
  border-radius: var(--radius-md);
  margin-bottom: 14px;
}

.product-detail-expiry-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 13px;
  font-weight: 600;
  color: #1f2937;
  margin-bottom: 8px;
}

/* 批次明细表格：四列（批次效期 | 库存 | 扣减数量 | 剩余库存），display:contents 使表头/表行直接作为网格项对齐 */
.product-detail-expiry-table {
  display: grid;
  grid-template-columns: 1fr 56px 76px 76px;
  font-size: 13px;
}

.product-detail-expiry-th {
  display: contents;
}

.product-detail-expiry-th > span {
  padding: 6px 8px;
  font-size: 12px;
  color: #6b7280;
  font-weight: 600;
  border-bottom: 1px solid #d1d5db;
}

.product-detail-expiry-tr {
  display: contents;
}

.product-detail-expiry-tr > span {
  padding: 7px 8px;
  border-bottom: 1px solid #eef0f3;
}

.product-detail-expiry-date {
  color: #1f2937;
  font-family: 'JetBrains Mono', monospace;
}

.product-detail-expiry-qty {
  color: var(--primary);
  font-weight: 600;
  font-family: 'JetBrains Mono', monospace;
  text-align: center;
}

.product-detail-expiry-stock,
.product-detail-expiry-remaining {
  color: #6b7280;
  font-family: 'JetBrains Mono', monospace;
  text-align: right;
}

.product-detail-expiry-summary {
  margin-top: 6px;
  padding-top: 8px;
  border-top: 1px dashed #d1d5db;
  font-size: 12px;
  color: #6b7280;
}

.product-detail-expiry-state {
  font-size: 13px;
  color: #6b7280;
  padding: 6px 0;
}

.product-detail-expiry-missing {
  color: var(--danger) !important;
  font-weight: 500;
}

/* 效期选择弹窗（浅色浮层风格：弹窗主体为白色，内容块需用浅色卡片适配，不能复用页面深色变量 --bg-tertiary） */
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
  background: #f5f7fa;
  border-radius: var(--radius-md);
}

.expiry-product-name {
  font-size: 14px;
  font-weight: 600;
  color: #1f2937;
}

.expiry-qty-control {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  color: #4b5563;
}

.expiry-qty-control .qty-btn {
  width: 28px;
  height: 28px;
  background: #f5f7fa;
  border: 1px solid #d1d5db;
  border-radius: var(--radius-sm);
  color: #4b5563;
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
  background: #ffffff;
  border: 1px solid #d1d5db;
  border-radius: var(--radius-sm);
  color: #1f2937;
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
  background: #f5f7fa;
  border: 2px solid #e5e7eb;
  border-radius: var(--radius-md);
  cursor: pointer;
  transition: all 0.2s;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.expiry-option:hover {
  border-color: #d1d5db;
}

.expiry-option.selected {
  border-color: var(--primary);
  background: rgba(6, 212, 228, 0.15);
}

.expiry-option-date {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 14px;
  font-weight: 500;
  color: #1f2937;
  font-family: 'JetBrains Mono', monospace;
}

.expiry-option-info {
  display: flex;
  gap: 12px;
  font-size: 12px;
  color: #6b7280;
}

.selected-expiry-section {
  padding: 12px;
  background: #f5f7fa;
  border-radius: var(--radius-md);
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.selected-title {
  font-size: 12px;
  color: #4b5563;
  font-weight: 600;
}

.selected-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 13px;
  color: #1f2937;
  font-family: 'JetBrains Mono', monospace;
  padding: 4px 0;
}

.selected-summary {
  font-size: 12px;
  color: #6b7280;
  display: flex;
  align-items: center;
  gap: 6px;
  margin-top: 4px;
  padding-top: 8px;
  border-top: 1px dashed #d1d5db;
}

.auto-recommend-section {
  padding: 12px;
  background: rgba(6, 212, 228, 0.08);
  border-radius: var(--radius-md);
  border: 1px dashed #9ca3af;
}

.expiry-activity-section {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.expiry-activity-label {
  font-size: 13px;
  color: #4b5563;
}

/* 服务项目耗材效期选择弹窗（列表式：按耗材分行，复用 expiry-* 选项样式） */
.consumable-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
  max-height: 60vh;
  overflow-y: auto;
  padding-right: 4px;
}

.consumable-block {
  padding: 12px;
  background: #f8fafc;
  border: 1px solid #e5e7eb;
  border-radius: var(--radius-md);
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.consumable-header {
  display: flex;
  align-items: center;
  gap: 8px;
}

.consumable-name {
  font-size: 14px;
  font-weight: 600;
  color: #1f2937;
}

.consumable-need {
  font-size: 12px;
  color: #6b7280;
  font-family: 'JetBrains Mono', monospace;
}

.consumable-auto {
  padding: 10px;
  background: rgba(6, 212, 228, 0.08);
  border: 1px dashed #9ca3af;
  border-radius: var(--radius-sm);
}

.consumable-selected {
  padding: 10px;
  background: #f5f7fa;
  border-radius: var(--radius-sm);
  display: flex;
  flex-direction: column;
  gap: 4px;
}

/* 项目卡核销弹窗（浅色浮层风格：左右分栏，左选卡右核销；内容块适配白色弹窗，不复用页面深色变量） */
/* 顶部会员栏：核销对象固定为购物车上方已选会员 */
.verify-member-bar {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 10px 12px;
  margin-bottom: 12px;
  background: #f5f7fa;
  border: 1px solid #e5e7eb;
  border-radius: var(--radius-md);
}

.verify-member-icon {
  color: var(--primary);
}

.verify-member-name {
  font-size: 14px;
  font-weight: 600;
  color: #1f2937;
}

.verify-member-phone {
  font-size: 12px;
  color: #6b7280;
}

.verify-member-tip {
  margin-left: auto;
  font-size: 12px;
  color: #9ca3af;
}

/* 左右分栏容器 */
.verify-layout {
  display: flex;
  gap: 14px;
  min-height: 340px;
}

/* 左侧项目卡面板 */
.verify-card-panel {
  width: 280px;
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.verify-panel-title {
  font-size: 13px;
  color: #4b5563;
}

.verify-card-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  overflow-y: auto;
  max-height: 360px;
  padding-right: 2px;
}

.verify-card-item {
  border: 1px solid #e5e7eb;
  border-radius: var(--radius-md);
  padding: 10px 12px;
  cursor: pointer;
  transition: border-color 0.2s, box-shadow 0.2s;
  background: #fff;
}

.verify-card-item:hover {
  border-color: var(--primary);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.verify-card-item.active {
  border-color: var(--primary);
  background: rgba(6, 212, 228, 0.08);
  box-shadow: 0 0 0 1px var(--primary) inset;
}

.verify-card-name {
  font-size: 14px;
  font-weight: 600;
  color: #1f2937;
}

.verify-card-meta {
  display: flex;
  justify-content: space-between;
  margin-top: 6px;
  font-size: 12px;
}

.verify-card-remaining {
  color: var(--primary);
  font-weight: 600;
}

.verify-card-expiry {
  color: #6b7280;
}

.verify-card-sub {
  margin-top: 4px;
  font-size: 12px;
  color: #9ca3af;
}

.verify-card-empty,
.verify-editor-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 40px 12px;
  color: #9ca3af;
  font-size: 13px;
  border: 1px dashed #e5e7eb;
  border-radius: var(--radius-md);
}

/* 右侧核销项目编辑区 */
.verify-editor {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.step-label {
  font-size: 13px;
  color: #4b5563;
}

.step-label-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.verify-items-table {
  border: 1px solid #e5e7eb;
  border-radius: var(--radius-md);
  overflow: hidden;
}

.verify-items-header,
.verify-items-row-main {
  display: grid;
  grid-template-columns: 1.6fr 0.8fr 0.9fr 0.6fr;
  gap: 8px;
  padding: 8px 12px;
  align-items: center;
}

.verify-items-header {
  background: #f5f7fa;
  font-size: 12px;
  color: #4b5563;
}

.verify-items-row {
  border-top: 1px solid #e5e7eb;
}

.verify-items-row .col-amount {
  font-weight: 600;
  color: var(--primary);
}

.verify-items-footer {
  display: flex;
  justify-content: space-between;
  padding: 10px 12px;
  background: #f5f7fa;
  border-top: 1px solid #e5e7eb;
  font-size: 13px;
  color: #4b5563;
}

.verify-items-footer b {
  color: var(--primary);
  font-size: 15px;
}

/* 核销详情弹窗（浅色浮层风格：卡信息描述列表 + 核销项目明细表格，与核销弹窗右栏口径一致） */
.verify-detail-items {
  margin-top: 14px;
  border: 1px solid #e5e7eb;
  border-radius: var(--radius-md);
  overflow: hidden;
}

.verify-detail-items-title {
  padding: 8px 12px;
  background: #f5f7fa;
  font-size: 13px;
  font-weight: 600;
  color: #1f2937;
  border-bottom: 1px solid #e5e7eb;
}

/* 核销项目明细表格：四列（服务项目 | 次数 | 金额 | 服务时间），display:contents 使表头/表行直接作为网格项对齐；服务时间列加宽以容纳完整时段不换行 */
.verify-detail-table {
  display: grid;
  grid-template-columns: 1.2fr 0.6fr 0.7fr 1.9fr;
  font-size: 13px;
}

.verify-detail-th {
  display: contents;
}

.verify-detail-th > span {
  padding: 6px 12px;
  font-size: 12px;
  color: #6b7280;
  font-weight: 600;
  border-bottom: 1px solid #eef0f3;
}

.verify-detail-tr {
  display: contents;
}

.verify-detail-tr > span {
  padding: 7px 12px;
  border-bottom: 1px solid #eef0f3;
  color: #1f2937;
}

.verify-detail-product {
  font-weight: 500;
}

.verify-detail-amount {
  color: var(--primary);
  font-weight: 600;
  font-family: 'JetBrains Mono', monospace;
}

.verify-detail-time {
  color: #6b7280;
  font-size: 12px;
  /* 完整时段（YYYY-MM-DD HH:mm - YYYY-MM-DD HH:mm）需单行展示，禁止换行 */
  white-space: nowrap;
}

/* 核销项目备注行：跨满四列 grid 展示，浅灰底与明细区分 */
.verify-detail-remark {
  grid-column: 1 / -1;
  padding: 5px 12px;
  font-size: 12px;
  color: #6b7280;
  background: #fafbfc;
  border-bottom: 1px solid #eef0f3;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.verify-detail-footer {
  display: flex;
  justify-content: space-between;
  padding: 10px 12px;
  background: #f5f7fa;
  border-top: 1px solid #e5e7eb;
  font-size: 13px;
  color: #4b5563;
}

.verify-detail-footer b {
  color: var(--primary);
  font-size: 15px;
}

/* 结算完成聚合展示（浅色浮层风格） */
.settle-result {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.settle-title {
  font-size: 13px;
  font-weight: 600;
  color: #1f2937;
  margin-top: 8px;
  padding-bottom: 4px;
  border-bottom: 1px solid #f3f4f6;
}

.settle-row {
  font-size: 13px;
  color: #4b5563;
  padding: 2px 0;
}

/* ========== 组合支付 3 栏联动 ========== */
/* 组合支付区块样式与效期选择弹窗内容块一致（浅色浮层风格，纯浅灰圆角块） */
.combined-pay-section {
  display: flex;
  flex-direction: column;
  gap: 10px;
  margin-top: 12px;
  padding: 12px;
  background: #f5f7fa;
  border-radius: var(--radius-md);
}

.combined-row {
  display: flex;
  align-items: center;
  gap: 8px;
}

.combined-label {
  width: 220px;
  font-size: 14px;
  color: #4b5563;
  flex-shrink: 0;
}

.combined-hint {
  display: block;
  margin-left: 0;
  margin-top: 2px;
  color: #9ca3af;
  font-size: 12px;
  line-height: 1.4;
}

.combined-summary {
  margin-top: 4px;
  padding-top: 8px;
  border-top: 1px dashed #d1d5db;
  font-size: 13px;
  font-weight: 600;
  color: #1f2937;
}

.combined-warn {
  margin-left: 8px;
  color: #ef4444;
  font-weight: normal;
  font-size: 12px;
}

/* 组合支付行金额输入框：统一 flex 拉伸占满剩余空间并保证最小宽度（避免被压缩不可见），
   去除默认固定宽度，使其在浅色浮层中正常可见 */
.combined-row .el-input-number {
  flex: 1;
  min-width: 120px;
}

/* 积分抵扣金额单位"元" */
.combined-unit {
  flex-shrink: 0;
  color: #6b7280;
  font-size: 13px;
}

/* 会员储值 / 积分抵扣信息面板（选中对应支付方式时展示，浅色浮层风格，与组合支付区块一致） */
.pay-info-section {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-bottom: 16px;
  padding: 12px;
  background: #f5f7fa;
  border-radius: var(--radius-md);
}

.pay-info-title {
  font-size: 13px;
  font-weight: 600;
  color: #1f2937;
  padding-bottom: 8px;
  border-bottom: 1px dashed #d1d5db;
}

.pay-info-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 13px;
}

.pay-info-row .label {
  color: #6b7280;
}

/* 最大抵扣积分后的规则备注（如"单笔最高抵扣金额50元"），淡色小字弱化、与主标签区分 */
.pay-info-row .label .label-note {
  margin-left: 6px;
  font-size: 12px;
  font-style: normal;
  color: #9ca3af;
}

.pay-info-row .value {
  color: #1f2937;
  font-weight: 500;
  font-family: 'JetBrains Mono', monospace;
}

/* 导致确认收款禁用的不足项标红（储值余额/当前积分/最大抵扣积分 < 对应需要值） */
.pay-info-row .value.danger {
  color: #ef4444;
  font-weight: 600;
}

/* ========== 预约转单弹窗：表格选中行效果 ==========
   弹窗内表格为浅色风格（dark-theme.css 全局覆盖为白底浅灰 hover），Element Plus 默认选中高亮
   （#f5f7fa）在白底上几乎不可见。此处用主题青绿色强化选中行：淡青背景 + 首列左侧主题色强调条。
   用 tr.el-table__row.current-row 提高选择器特异性（含类型选择器），确保可覆盖全局 hover/选中样式。 */
:deep(.appointment-table tr.el-table__row.current-row > td.el-table__cell) {
  background-color: rgba(6, 212, 228, 0.12) !important;
}

:deep(.appointment-table tr.el-table__row.current-row > td.el-table__cell:first-child) {
  box-shadow: inset 3px 0 0 var(--primary) !important;
}

/* ========== 挂单/取单弹窗 ========== */
.hold-tip {
  color: #8892a6;
  font-size: 13px;
  line-height: 1.7;
  margin-bottom: 14px;
}

.resume-search {
  margin-bottom: 14px;
}

.resume-pagination {
  display: flex;
  justify-content: flex-end;
  margin-top: 14px;
}
</style>

<!-- 服务内容弹窗技师下拉选项（popper-class 定位，teleport 到 body 需非 scoped 样式）
 * 选项为两行自定义内容（名称+占用标签 / 技能），默认固定 34px 行高 + overflow:hidden 会裁切底部文字
 * 需让 item 高度自适应内容，避免占用标签撑高后底部文字显示不全 -->
<style>
.technician-select-popper .el-select-dropdown__item {
  height: auto;
  min-height: 34px;
  line-height: 1.4;
  padding-top: 6px;
  padding-bottom: 6px;
  white-space: normal;
}
</style>
