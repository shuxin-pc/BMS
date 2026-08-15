<template>
  <div class="poster-page">
    <div class="designer-container">
      <!-- ========== 左侧模板选择 ========== -->
      <div class="template-panel">
        <div class="panel-header">
          <span class="panel-title">模板选择</span>
          <button class="draft-entry-btn" @click="openDraftsDialog">草稿</button>
        </div>
        <div class="template-tabs">
          <div
            v-for="tab in templateTabs"
            :key="tab.id"
            class="template-tab"
            :class="{ active: activeTemplateTab === tab.id }"
            @click="activeTemplateTab = tab.id"
          >
            {{ tab.name }}
          </div>
        </div>
        <div class="template-list">
          <div
            v-for="tpl in filteredTemplates"
            :key="tpl.id"
            class="template-card"
            :class="{ selected: selectedTemplateId === tpl.id }"
            @click="selectTemplate(tpl)"
          >
            <div class="template-preview" :style="{ background: tpl.gradient, color: tpl.previewTextColor }">
              {{ tpl.previewText }}
            </div>
            <div class="template-name">{{ tpl.name }}</div>
          </div>
        </div>
      </div>

      <!-- ========== 中间编辑预览区 ========== -->
      <div class="editor-panel">
        <div class="editor-toolbar">
          <div class="toolbar-group">
            <div
              v-for="size in sizeOptions"
              :key="size.id"
              class="size-btn"
              :class="{ active: selectedSize === size.id }"
              @click="handleSizeChange(size.id)"
            >
              {{ size.name }}
            </div>
          </div>
          <div class="toolbar-group">
            <button class="tool-btn" @click="handlePreviewZoom">🔍 放大查看</button>
          </div>
        </div>

        <div class="preview-area" ref="previewAreaRef">
          <div class="poster-stage" :class="{ capturing: isCapturing }" :style="posterStageStyle">
            <div
              ref="posterCanvasRef"
              class="poster-canvas"
              :class="{ capturing: isCapturing }"
              :style="posterCanvasStyle"
              @click="handlePreviewZoom"
            >
              <!-- 背景图片层 -->
              <img
                v-if="posterData.backgroundImage"
                :src="posterData.backgroundImage.url"
                class="poster-bg-img"
                crossorigin="anonymous"
              />
              <div class="poster-content" :style="{ color: posterData.textColor }">
                <span class="poster-badge" v-if="posterData.badge">{{ posterData.badge }}</span>
                <h1 class="poster-title" :style="{ fontSize: posterData.titleFontSize + 'px' }">
                  {{ posterData.title }}
                </h1>
                <p class="poster-subtitle" v-if="posterData.subtitle">{{ posterData.subtitle }}</p>
                <p class="poster-description" v-if="posterData.description">{{ posterData.description }}</p>
                <p class="poster-effective" v-if="posterData.effectiveDate">有效期至 {{ posterData.effectiveDate }}</p>

                <div class="poster-highlight" v-if="posterData.price || posterData.originalPrice">
                  <div class="poster-price" v-if="posterData.price">
                    <span class="unit">¥</span>{{ posterData.price.toFixed(0) }}<span class="unit">.{{ (posterData.price % 1).toFixed(2).slice(2) }}</span>
                  </div>
                  <div class="poster-original" v-if="posterData.originalPrice">
                    原价 ¥{{ formatPrice(posterData.originalPrice) }}
                  </div>
                </div>

                <div class="poster-items" v-if="posterItems.length > 0">
                  <div class="poster-item" v-for="(item, idx) in posterItems" :key="idx">
                    {{ item }}
                  </div>
                </div>

                <div class="poster-footer">
                  <div>
                    <div class="poster-store" v-if="posterData.storeName">{{ posterData.storeName }}</div>
                    <div class="poster-address" v-if="posterData.storeAddress">{{ posterData.storeAddress }}</div>
                    <div class="poster-contact" v-if="posterData.contact">{{ posterData.contact }}</div>
                  </div>
                  <div class="poster-qr" v-if="posterData.showQr">
                    <img v-if="posterData.qrcodeImage" :src="posterData.qrcodeImage.url" class="qr-img" />
                    <span v-else>▣</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
          <div class="preview-hint" v-if="isRefreshing">刷新中...</div>
        </div>
      </div>

      <!-- ========== 右侧素材/属性面板 ========== -->
      <div class="props-panel">
        <!-- 属性配置 -->
        <div class="props-card" :class="{ collapsed: collapsedPanels.content }">
          <div class="panel-header collapsible" @click="togglePanel('content')">
            <span class="panel-title">内容编辑</span>
            <span class="collapse-arrow" :class="{ collapsed: collapsedPanels.content }">▾</span>
          </div>
          <div class="props-body" v-show="!collapsedPanels.content">
            <div class="prop-group">
              <div class="prop-label">活动标签</div>
              <el-input v-model="posterData.badge" placeholder="如：限时活动" />
            </div>
            <div class="prop-group">
              <div class="prop-label">主标题</div>
              <el-input v-model="posterData.title" placeholder="如：年中大促 美丽焕新" />
            </div>
            <div class="prop-group">
              <div class="prop-label">副标题</div>
              <el-input v-model="posterData.subtitle" placeholder="如：全场项目 5 折起" />
            </div>
            <div class="prop-group">
              <div class="prop-label">促销价格</div>
              <el-input-number
                v-model="posterData.price"
                :min="0"
                :precision="2"
                controls-position="right"
                style="width: 100%"
              />
            </div>
            <div class="prop-group">
              <div class="prop-label">原价</div>
              <el-input-number
                v-model="posterData.originalPrice"
                :min="0"
                :precision="2"
                controls-position="right"
                style="width: 100%"
              />
            </div>
            <div class="prop-group">
              <div class="prop-label">项目列表（每行一项）</div>
              <el-input
                v-model="posterData.itemsText"
                type="textarea"
                :rows="4"
                placeholder="每行输入一个项目名称"
              />
            </div>
            <div class="prop-group">
              <div class="prop-label">活动描述</div>
              <el-input
                v-model="posterData.description"
                type="textarea"
                :rows="3"
                placeholder="详细描述本次活动内容、规则、注意事项等"
              />
            </div>
            <div class="prop-group">
              <div class="prop-label">生效日期</div>
              <el-date-picker
                v-model="posterData.effectiveDate"
                type="date"
                value-format="YYYY-MM-DD"
                placeholder="选择活动生效日期"
                style="width: 100%"
              />
            </div>
            <div class="prop-group">
              <div class="prop-label">门店名称</div>
              <el-input v-model="posterData.storeName" placeholder="请输入门店名称" />
            </div>
            <div class="prop-group">
              <div class="prop-label">门店地址</div>
              <el-input v-model="posterData.storeAddress" placeholder="请输入门店详细地址" />
            </div>
            <div class="prop-group">
              <div class="prop-label">联系电话</div>
              <el-input v-model="posterData.contact" placeholder="请输入联系电话" />
            </div>
            <div class="prop-group">
              <div class="prop-label">背景图片</div>
              <div class="image-field" @click="openImagePicker('background')">
                <img v-if="posterData.backgroundImage" :src="posterData.backgroundImage.thumbnail" class="field-preview" />
                <span v-else class="field-placeholder">点击选择背景图</span>
              </div>
              <button v-if="posterData.backgroundImage" class="clear-btn" @click.stop="posterData.backgroundImage = null">清除</button>
            </div>
            <div class="prop-group">
              <div class="prop-label">二维码图片</div>
              <div class="image-field" @click="openImagePicker('qrcode')">
                <img v-if="posterData.qrcodeImage" :src="posterData.qrcodeImage.thumbnail" class="field-preview" />
                <span v-else class="field-placeholder">点击选择二维码</span>
              </div>
              <button v-if="posterData.qrcodeImage" class="clear-btn" @click.stop="posterData.qrcodeImage = null">清除</button>
            </div>
            <div class="prop-group">
              <el-checkbox v-model="posterData.showQr">显示二维码</el-checkbox>
            </div>
          </div>
        </div>

        <!-- 样式配置 -->
        <div class="props-card props-card-auto" :class="{ collapsed: collapsedPanels.style }">
          <div class="panel-header collapsible" @click="togglePanel('style')">
            <span class="panel-title">样式配置</span>
            <span class="collapse-arrow" :class="{ collapsed: collapsedPanels.style }">▾</span>
          </div>
          <div class="props-body" v-show="!collapsedPanels.style">
            <div class="prop-group">
              <div class="prop-label">背景色（独立选择）</div>
              <el-color-picker v-model="posterData.backgroundColor" show-alpha />
              <div class="color-picker" style="margin-top: 8px;">
                <div
                  v-for="(color, idx) in colorOptions"
                  :key="idx"
                  class="color-swatch"
                  :class="{ selected: selectedColorIndex === idx }"
                  :style="{ background: color }"
                  @click="handleColorChange(idx)"
                ></div>
              </div>
            </div>
            <div class="prop-group">
              <div class="prop-label">文字颜色</div>
              <el-color-picker v-model="posterData.textColor" />
            </div>
            <div class="prop-group">
              <div class="prop-label">
                标题字号
                <span class="font-size-display">{{ posterData.titleFontSize }}px</span>
              </div>
              <div class="font-size-control">
                <button class="font-size-btn" @click="changeFontSize(-2)">−</button>
                <span class="font-size-value">{{ posterData.titleFontSize }}</span>
                <button class="font-size-btn" @click="changeFontSize(2)">+</button>
              </div>
            </div>
          </div>
        </div>

        <!-- 素材库 -->
        <div class="props-card props-card-auto" :class="{ collapsed: collapsedPanels.material }">
          <div class="panel-header collapsible" @click="togglePanel('material')">
            <span class="panel-title">素材库</span>
            <span class="collapse-arrow" :class="{ collapsed: collapsedPanels.material }">▾</span>
          </div>
          <div class="props-body" v-show="!collapsedPanels.material">
            <div class="material-section">
              <div class="prop-label">预设背景</div>
              <div class="material-grid">
                <div
                  v-for="asset in presetAssets"
                  :key="asset.id"
                  class="material-item"
                  :class="{ active: posterData.backgroundImage?.id === asset.id }"
                  :title="asset.name"
                  @click="applyMaterial(asset)"
                >
                  <img :src="asset.thumbnail" class="material-thumb" />
                </div>
              </div>
            </div>
            <div class="material-section">
              <div class="prop-label">我的图片</div>
              <div class="material-grid" v-if="recentUploadedImages.length > 0">
                <div
                  v-for="asset in recentUploadedImages"
                  :key="asset.id"
                  class="material-item"
                  :class="{ active: posterData.backgroundImage?.id === asset.id }"
                  :title="asset.name"
                  @click="applyMaterial(asset)"
                >
                  <button class="asset-delete-btn" title="删除这张图片" @click.stop="deleteUploadedImage(asset.id)">×</button>
                  <img :src="asset.thumbnail" class="material-thumb" />
                </div>
              </div>
              <div class="empty-tip" v-else>暂无上传图片，可在「内容编辑 → 背景图片」中上传</div>
            </div>
          </div>
        </div>

        <!-- 底部操作 -->
        <div class="action-bar">
          <button class="btn" @click="handleSaveDraft">保存草稿</button>
          <button class="btn btn-primary" @click="handleExport">导出图片</button>
        </div>
      </div>
    </div>

    <!-- ========== 图片选择器弹窗 ========== -->
    <el-dialog v-model="imagePickerVisible" title="选择图片" width="640px" append-to-body>
      <el-tabs v-model="activeImageTab">
        <el-tab-pane label="预设素材" name="preset">
          <div class="asset-grid" v-if="presetAssetsForField.length > 0">
            <div
              v-for="asset in presetAssetsForField"
              :key="asset.id"
              class="asset-item"
              @click="selectImage(asset)"
            >
              <img :src="asset.thumbnail" class="asset-thumb" />
              <span class="asset-name">{{ asset.name }}</span>
            </div>
          </div>
          <div class="empty-tip" v-else>该字段暂无预设素材，请通过"本地上传"上传图片</div>
        </el-tab-pane>
        <el-tab-pane label="本地上传" name="upload">
          <div class="upload-area">
            <el-upload
              :show-file-list="false"
              :before-upload="handleBeforeUpload"
              accept="image/jpeg,image/png,image/webp"
            >
              <button class="upload-btn">📁 从本地上传</button>
            </el-upload>
            <div class="upload-tip">支持 JPG / PNG / WebP，单张 ≤5MB，宽度 ≥800px；超出 50 张或存储空间不足时自动清理最早上传的图片</div>
          </div>
        </el-tab-pane>
        <el-tab-pane label="我的图片" name="mine">
          <div class="asset-grid" v-if="uploadedImages.length > 0">
            <div
              v-for="asset in uploadedImages"
              :key="asset.id"
              class="asset-item"
              @click="selectImage(asset)"
            >
              <button class="asset-delete-btn" title="删除这张图片" @click.stop="deleteUploadedImage(asset.id)">×</button>
              <img :src="asset.thumbnail" class="asset-thumb" />
              <span class="asset-name">{{ asset.name }}</span>
            </div>
          </div>
          <div class="empty-tip" v-else>暂无上传图片</div>
        </el-tab-pane>
      </el-tabs>
    </el-dialog>

    <!-- ========== 放大预览弹窗 ========== -->
    <el-dialog v-model="zoomDialogVisible" title="预览放大（100%）" width="85%" append-to-body>
      <div class="zoom-container">
        <div v-if="zoomLoading" class="zoom-loading">正在生成高清预览...</div>
        <img v-if="zoomImageUrl" :src="zoomImageUrl" class="zoom-img" />
      </div>
    </el-dialog>

    <!-- ========== 草稿列表弹窗 ========== -->
    <el-dialog v-model="draftsDialogVisible" title="我的草稿" width="500px" append-to-body>
      <div class="draft-list" v-if="drafts.length > 0">
        <div v-for="draft in drafts" :key="draft.id" class="draft-item">
          <div class="draft-info">
            <div class="draft-name">{{ draft.name }}</div>
            <div class="draft-time">{{ formatTime(draft.createdAt) }}</div>
          </div>
          <div class="draft-actions">
            <button class="draft-btn restore" @click="restoreDraft(draft)">恢复</button>
            <button class="draft-btn delete" @click="deleteDraft(draft.id)">删除</button>
          </div>
        </div>
      </div>
      <div class="empty-tip" v-else>暂无草稿</div>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted, onUnmounted, nextTick } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import html2canvas from 'html2canvas'

// ==================== 类型定义 ====================
interface ImageAsset {
  id: string
  name: string
  url: string
  source: 'preset' | 'upload'
  category: 'background' | 'decoration' | 'qrcode'
  thumbnail: string
  uploadTime?: number
}

interface PosterData {
  badge: string
  title: string
  subtitle: string
  price: number
  originalPrice: number
  itemsText: string
  description: string
  effectiveDate: string | null
  storeName: string
  storeAddress: string
  contact: string
  showQr: boolean
  titleFontSize: number
  colorIndex: number
  textColor: string
  backgroundColor: string
  backgroundImage: ImageAsset | null
  qrcodeImage: ImageAsset | null
}

interface Template {
  id: number
  name: string
  previewText: string
  gradient: string
  previewTextColor: string
  category: string
  defaultData: Partial<PosterData>
}

interface PosterDraft {
  id: string
  name: string
  templateId: number
  posterData: PosterData
  createdAt: number
}

// ==================== SVG 素材生成工具 ====================
/** 将 SVG 字符串转换为 data URL */
const makeSvgUrl = (svg: string): string =>
  `data:image/svg+xml,${encodeURIComponent(svg)}`

/** 生成精美背景图 SVG */
const createBackgroundSvg = (type: string): string => {
  const svgs: Record<string, string> = {
    warm: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 400 600">
      <defs><radialGradient id="g" cx="30%" cy="20%" r="90%">
        <stop offset="0%" stop-color="#FFE4B5"/><stop offset="40%" stop-color="#FF8C69"/><stop offset="100%" stop-color="#C0392B"/>
      </radialGradient></defs>
      <rect width="400" height="600" fill="url(#g)"/>
      <circle cx="80" cy="100" r="60" fill="#FFD700" opacity="0.15"/>
      <circle cx="320" cy="200" r="80" fill="#FFA500" opacity="0.1"/>
      <circle cx="200" cy="500" r="100" fill="#FF6347" opacity="0.1"/>
    </svg>`,
    ocean: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 400 600">
      <defs><linearGradient id="g" x1="0%" y1="0%" x2="0%" y2="100%">
        <stop offset="0%" stop-color="#48CAE4"/><stop offset="50%" stop-color="#0096C7"/><stop offset="100%" stop-color="#023E8A"/>
      </linearGradient></defs>
      <rect width="400" height="600" fill="url(#g)"/>
      <path d="M0,400 Q100,380 200,400 T400,400 L400,600 L0,600 Z" fill="#00B4D8" opacity="0.3"/>
      <path d="M0,450 Q100,430 200,450 T400,450 L400,600 L0,600 Z" fill="#48CAE4" opacity="0.2"/>
    </svg>`,
    forest: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 400 600">
      <defs><linearGradient id="g" x1="0%" y1="0%" x2="100%" y2="100%">
        <stop offset="0%" stop-color="#D8F3DC"/><stop offset="50%" stop-color="#74C69D"/><stop offset="100%" stop-color="#1B4332"/>
      </linearGradient></defs>
      <rect width="400" height="600" fill="url(#g)"/>
      <circle cx="350" cy="80" r="50" fill="#B7E4C7" opacity="0.3"/>
      <circle cx="50" cy="500" r="70" fill="#52B788" opacity="0.2"/>
      <path d="M0,550 Q100,520 200,540 T400,530 L400,600 L0,600 Z" fill="#40916C" opacity="0.3"/>
    </svg>`,
    starry: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 400 600">
      <defs><linearGradient id="g" x1="0%" y1="0%" x2="0%" y2="100%">
        <stop offset="0%" stop-color="#240046"/><stop offset="50%" stop-color="#3C096C"/><stop offset="100%" stop-color="#10002B"/>
      </linearGradient></defs>
      <rect width="400" height="600" fill="url(#g)"/>
      <circle cx="50" cy="80" r="2" fill="#fff"/><circle cx="150" cy="120" r="1.5" fill="#fff"/>
      <circle cx="280" cy="60" r="2" fill="#fff"/><circle cx="350" cy="150" r="1" fill="#fff"/>
      <circle cx="100" cy="200" r="1.5" fill="#FFD700"/><circle cx="320" cy="250" r="2" fill="#fff"/>
      <circle cx="200" cy="180" r="1" fill="#fff"/><circle cx="60" cy="300" r="1.5" fill="#fff"/>
      <circle cx="180" cy="350" r="1" fill="#FFD700"/><circle cx="300" cy="400" r="1.5" fill="#fff"/>
      <circle cx="80" cy="450" r="1" fill="#fff"/><circle cx="250" cy="500" r="2" fill="#FFD700"/>
    </svg>`,
    minimal: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 400 600">
      <rect width="400" height="600" fill="#F5F5DC"/>
      <line x1="0" y1="150" x2="400" y2="150" stroke="#D4C5A0" stroke-width="1" opacity="0.5"/>
      <line x1="0" y1="450" x2="400" y2="450" stroke="#D4C5A0" stroke-width="1" opacity="0.5"/>
      <circle cx="200" cy="300" r="100" fill="none" stroke="#D4C5A0" stroke-width="1" opacity="0.3"/>
      <circle cx="200" cy="300" r="60" fill="none" stroke="#D4C5A0" stroke-width="1" opacity="0.2"/>
    </svg>`,
    gold: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 400 600">
      <defs><linearGradient id="g" x1="0%" y1="0%" x2="100%" y2="100%">
        <stop offset="0%" stop-color="#D4AF37"/><stop offset="50%" stop-color="#FFD700"/><stop offset="100%" stop-color="#B8860B"/>
      </linearGradient></defs>
      <rect width="400" height="600" fill="url(#g)"/>
      <polygon points="0,0 100,50 50,150 0,100" fill="#FFF8DC" opacity="0.1"/>
      <polygon points="400,600 300,550 350,450 400,500" fill="#8B6914" opacity="0.2"/>
      <polygon points="200,0 250,50 200,100 150,50" fill="#FFF8DC" opacity="0.08"/>
    </svg>`
  }
  return svgs[type] || svgs.warm
}

// ==================== 预设素材库 ====================
const presetAssets: ImageAsset[] = [
  // 背景图（6张）
  { id: 'bg-warm', name: '暖光渐变', url: makeSvgUrl(createBackgroundSvg('warm')), source: 'preset', category: 'background', thumbnail: makeSvgUrl(createBackgroundSvg('warm')) },
  { id: 'bg-ocean', name: '海洋蓝调', url: makeSvgUrl(createBackgroundSvg('ocean')), source: 'preset', category: 'background', thumbnail: makeSvgUrl(createBackgroundSvg('ocean')) },
  { id: 'bg-forest', name: '森林绿意', url: makeSvgUrl(createBackgroundSvg('forest')), source: 'preset', category: 'background', thumbnail: makeSvgUrl(createBackgroundSvg('forest')) },
  { id: 'bg-starry', name: '星空夜景', url: makeSvgUrl(createBackgroundSvg('starry')), source: 'preset', category: 'background', thumbnail: makeSvgUrl(createBackgroundSvg('starry')) },
  { id: 'bg-minimal', name: '极简米白', url: makeSvgUrl(createBackgroundSvg('minimal')), source: 'preset', category: 'background', thumbnail: makeSvgUrl(createBackgroundSvg('minimal')) },
  { id: 'bg-gold', name: '金箔质感', url: makeSvgUrl(createBackgroundSvg('gold')), source: 'preset', category: 'background', thumbnail: makeSvgUrl(createBackgroundSvg('gold')) }
]

// ==================== 模板数据（8个，按需求 T1-T8）====================
const templateTabs = [
  { id: 'all', name: '全部' },
  { id: 'promo', name: '促销' },
  { id: 'festival', name: '节日' },
  { id: 'member', name: '会员' },
  { id: 'storedvalue', name: '储值' }
]

const activeTemplateTab = ref('all')

const templates: Template[] = [
  {
    // T1 简约风：白色背景，大字标题，折扣活动
    id: 1,
    name: '简约风',
    previewText: '折扣\n活动',
    gradient: 'linear-gradient(135deg, #f5f5f5, #e0e0e0)',
    previewTextColor: '#333',
    category: 'promo',
    defaultData: {
      badge: '限时折扣',
      title: '本周特惠 8折起',
      subtitle: '精选项目 限时享折扣',
      price: 298,
      originalPrice: 398,
      itemsText: '深层补水面部护理\n水光针注射疗程',
      colorIndex: 0,
      textColor: '#333333'
    }
  },
  {
    // T2 复古风：深色背景，金色边框，店庆/节日
    id: 2,
    name: '复古风',
    previewText: '店庆\n感恩',
    gradient: 'linear-gradient(135deg, #4a3728, #2c1810)',
    previewTextColor: '#D4AF37',
    category: 'festival',
    defaultData: {
      badge: '店庆特惠',
      title: '周年盛典 感恩回馈',
      subtitle: '浓情佳节 · 美丽相伴',
      price: 520,
      originalPrice: 888,
      itemsText: '节日限定套餐\n到店礼赠\n会员尊享折上折',
      colorIndex: 1,
      textColor: '#D4AF37'
    }
  },
  {
    // T3 清新风：浅绿/浅蓝渐变，新店开业
    id: 3,
    name: '清新风',
    previewText: '新店\n开业',
    gradient: 'linear-gradient(135deg, #d8f3dc, #b7e4c7)',
    previewTextColor: '#1B4332',
    category: 'festival',
    defaultData: {
      badge: '新店开业',
      title: '盛大开业 惊喜不断',
      subtitle: '开业大酬宾 · 全场体验价',
      price: 99,
      originalPrice: 298,
      itemsText: '新客专享体验套餐\n到店即送精美礼品',
      colorIndex: 2,
      textColor: '#1B4332'
    }
  },
  {
    // T4 奢华风：黑金配色，高端服务
    id: 4,
    name: '奢华风',
    previewText: '高端\n定制',
    gradient: 'linear-gradient(135deg, #1a1a1a, #0d0d0d)',
    previewTextColor: '#FFD700',
    category: 'promo',
    defaultData: {
      badge: '尊享定制',
      title: '高端定制 尊贵体验',
      subtitle: '专属服务 · 奢享品质',
      price: 1888,
      originalPrice: 2888,
      itemsText: 'VIP专属护理\n进口仪器疗程\n一对一专家服务',
      colorIndex: 3,
      textColor: '#FFD700'
    }
  },
  {
    // T5 促销风：红白对比，限时特价
    id: 5,
    name: '促销风',
    previewText: '限时\n特价',
    gradient: 'linear-gradient(135deg, #ff6b6b, #ee5a24)',
    previewTextColor: '#fff',
    category: 'promo',
    defaultData: {
      badge: '限时特价',
      title: '今日特价 限时抢购',
      subtitle: '每日一款 · 抢完即止',
      price: 99,
      originalPrice: 398,
      itemsText: '氨基酸洁面乳\n每日限量50份',
      colorIndex: 4,
      textColor: '#ffffff'
    }
  },
  {
    // T6 会员卡风：卡片样式，会员招募
    id: 6,
    name: '会员卡风',
    previewText: '会员\n招募',
    gradient: 'linear-gradient(135deg, #a78bfa, #8b5cf6)',
    previewTextColor: '#fff',
    category: 'member',
    defaultData: {
      badge: '会员专享',
      title: '加入会员 尊享特权',
      subtitle: '积分双倍 · 生日礼遇 · 专属折扣',
      price: 0,
      originalPrice: 0,
      itemsText: '积分兑换好礼\n会员专属折扣\n生日双倍积分',
      colorIndex: 5,
      textColor: '#ffffff'
    }
  },
  {
    // T7 体验卡风：券类样式，体验活动
    id: 7,
    name: '体验卡风',
    previewText: '体验\n专享',
    gradient: 'linear-gradient(135deg, #fbbf24, #f59e0b)',
    previewTextColor: '#fff',
    category: 'promo',
    defaultData: {
      badge: '体验专享',
      title: '新客体验 超值首发',
      subtitle: '首次到店 · 体验价仅需',
      price: 58,
      originalPrice: 198,
      itemsText: '面部深层清洁\n肩颈舒缓按摩',
      colorIndex: 4,
      textColor: '#ffffff'
    }
  },
  {
    // T8 充值活动风：金额醒目，储值推广
    id: 8,
    name: '充值活动风',
    previewText: '储值\n有礼',
    gradient: 'linear-gradient(135deg, #5b9bff, #3b82f6)',
    previewTextColor: '#FFD700',
    category: 'storedvalue',
    defaultData: {
      badge: '储值有礼',
      title: '充值返现 储值有礼',
      subtitle: '多充多送 · 永久有效',
      price: 3000,
      originalPrice: 0,
      itemsText: '充3000送500\n充5000送1000\n充10000送3000',
      colorIndex: 5,
      textColor: '#FFD700'
    }
  }
]

const filteredTemplates = computed(() => {
  if (activeTemplateTab.value === 'all') return templates
  return templates.filter(t => t.category === activeTemplateTab.value)
})

const selectedTemplateId = ref(1)

// ==================== 海报数据 ====================
const posterCanvasRef = ref<HTMLElement | null>(null)
const previewAreaRef = ref<HTMLElement | null>(null)

const posterData = reactive<PosterData>({
  badge: '限时折扣',
  title: '本周特惠 8折起',
  subtitle: '精选项目 限时享折扣',
  price: 298,
  originalPrice: 398,
  itemsText: '深层补水面部护理\n水光针注射疗程',
  description: '新客专享 · 限时折扣 · 数量有限',
  effectiveDate: null,
  storeName: '美丽天使·总店',
  storeAddress: '北京市朝阳区某某路 88 号',
  contact: '3800-8000',
  showQr: true,
  titleFontSize: 32,
  colorIndex: 0,
  textColor: '#333333',
  backgroundColor: '',
  backgroundImage: null,
  qrcodeImage: null
})

// 项目列表（从文本拆分）
const posterItems = computed(() => {
  return posterData.itemsText
    .split('\n')
    .map(s => s.trim())
    .filter(s => s.length > 0)
})

// ==================== 颜色选项 ====================
const colorOptions = [
  'linear-gradient(135deg, #f5f5f5, #e0e0e0)',
  'linear-gradient(135deg, #4a3728, #2c1810)',
  'linear-gradient(135deg, #d8f3dc, #b7e4c7)',
  'linear-gradient(135deg, #1a1a1a, #0d0d0d)',
  'linear-gradient(135deg, #ff6b6b, #ee5a24)',
  'linear-gradient(135deg, #a78bfa, #8b5cf6)'
]

const selectedColorIndex = ref(0)

const posterCanvasStyle = computed(() => {
  // 画布始终保持设计稿真实像素，仅用 transform 缩放显示，保证导出清晰度
  const layout = {
    width: posterSize.value.width + 'px',
    height: posterSize.value.height + 'px',
    transform: `scale(${effectiveScale.value})`
  }
  if (posterData.backgroundImage) {
    return {
      background: `url(${posterData.backgroundImage.url}) center/cover no-repeat`,
      ...layout
    }
  }
  // 优先使用用户独立选择的背景色（非空且非默认时）
  if (posterData.backgroundColor) {
    return {
      background: posterData.backgroundColor,
      ...layout
    }
  }
  // 兜底使用模板渐变预设
  return {
    background: colorOptions[selectedColorIndex.value],
    ...layout
  }
})

const handleColorChange = (idx: number) => {
  selectedColorIndex.value = idx
  posterData.colorIndex = idx
}

// ==================== 模板选择 ====================
const selectTemplate = (tpl: Template) => {
  selectedTemplateId.value = tpl.id
  const data = tpl.defaultData
  if (data.badge !== undefined) posterData.badge = data.badge
  if (data.title !== undefined) posterData.title = data.title
  if (data.subtitle !== undefined) posterData.subtitle = data.subtitle
  if (data.price !== undefined) posterData.price = data.price
  if (data.originalPrice !== undefined) posterData.originalPrice = data.originalPrice
  if (data.itemsText !== undefined) posterData.itemsText = data.itemsText
  if (data.colorIndex !== undefined) {
    posterData.colorIndex = data.colorIndex
    selectedColorIndex.value = data.colorIndex
  }
  if (data.textColor !== undefined) posterData.textColor = data.textColor
  ElMessage.success(`已选择「${tpl.name}」`)
}

// ==================== 尺寸选择 ====================
const sizeOptions = [
  { id: 'a4', name: 'A4', width: 595, height: 842 },
  { id: 'a5', name: 'A5', width: 420, height: 595 },
  { id: 'moments', name: '朋友圈', width: 375, height: 667 }
]

const selectedSize = ref('moments')
const posterSize = ref({ width: 375, height: 667 })

const handleSizeChange = (sizeId: string) => {
  selectedSize.value = sizeId
  const size = sizeOptions.find(s => s.id === sizeId)
  if (size) {
    posterSize.value = { width: size.width, height: size.height }
  }
}

// ==================== 预览缩放 ====================
/**
 * 预览缩放比，让不同尺寸的画布在预览区内等比适应显示。
 * 不放大（上限 100%），避免小尺寸海报被拉伸失真。
 */
const previewScale = ref(1)

/** 截图期间需要按 100% 渲染，否则 html2canvas 会把缩放后的尺寸当成真实尺寸 */
const isCapturing = ref(false)

const effectiveScale = computed(() => (isCapturing.value ? 1 : previewScale.value))

/** 缩放后画布的占位尺寸。transform 不影响布局盒子，需由外层撑出真实占位，否则会误出滚动条 */
const posterStageStyle = computed(() => ({
  width: posterSize.value.width * effectiveScale.value + 'px',
  height: posterSize.value.height * effectiveScale.value + 'px'
}))

/** 按预览区可用空间重算缩放比 */
const updatePreviewScale = () => {
  const area = previewAreaRef.value
  if (!area) return
  const style = getComputedStyle(area)
  const availableWidth =
    area.clientWidth - parseFloat(style.paddingLeft) - parseFloat(style.paddingRight)
  const availableHeight =
    area.clientHeight - parseFloat(style.paddingTop) - parseFloat(style.paddingBottom)
  if (availableWidth <= 0 || availableHeight <= 0) return
  const { width, height } = posterSize.value
  previewScale.value = Math.min(availableWidth / width, availableHeight / height, 1)
}

/**
 * 以 100% 缩放执行截图操作，结束后恢复预览缩放。
 * html2canvas 会读取根元素的 transform，带缩放截图会得到偏小或被裁切的结果。
 */
const withFullScale = async function <T>(capture: () => Promise<T>): Promise<T> {
  isCapturing.value = true
  await nextTick()
  try {
    return await capture()
  } finally {
    isCapturing.value = false
  }
}

let previewResizeObserver: ResizeObserver | null = null

watch(posterSize, updatePreviewScale)

// ==================== 字号控制 ====================
const changeFontSize = (delta: number) => {
  const newSize = posterData.titleFontSize + delta
  if (newSize >= 16 && newSize <= 60) {
    posterData.titleFontSize = newSize
  }
}

// ==================== 右侧面板折叠 ====================
type PropsPanelKey = 'content' | 'style' | 'material'

/** 右侧各卡片的折叠状态，true 表示已折叠 */
const collapsedPanels = reactive<Record<PropsPanelKey, boolean>>({
  content: false,
  style: true,
  material: true
})

const togglePanel = (key: PropsPanelKey) => {
  collapsedPanels[key] = !collapsedPanels[key]
}

// ==================== 防抖工具 ====================
/** 简单防抖函数 */
function debounce<T extends (...args: any[]) => void>(fn: T, delay: number): T {
  let timer: ReturnType<typeof setTimeout> | null = null
  return ((...args: any[]) => {
    if (timer) clearTimeout(timer)
    timer = setTimeout(() => fn(...args), delay)
  }) as T
}

// ==================== 防抖预览刷新 ====================
const isRefreshing = ref(false)

/** 防抖100ms刷新预览（标记刷新状态供UI显示） */
const debouncedRefresh = debounce(() => {
  isRefreshing.value = false
}, 100)

// 监听海报数据变化，防抖100ms刷新预览
watch(
  () => ({ ...posterData }),
  () => {
    isRefreshing.value = true
    debouncedRefresh()
  },
  { deep: true }
)

// ==================== 放大查看 ====================
const zoomDialogVisible = ref(false)
const zoomImageUrl = ref('')
const zoomLoading = ref(false)

/** 点击预览图放大到100%查看，用html2canvas生成Canvas */
const handlePreviewZoom = async () => {
  if (!posterCanvasRef.value) return
  zoomDialogVisible.value = true
  zoomLoading.value = true
  zoomImageUrl.value = ''
  try {
    const canvas = await withFullScale(() =>
      html2canvas(posterCanvasRef.value!, {
        scale: 2,
        useCORS: true,
        backgroundColor: null,
        logging: false
      })
    )
    zoomImageUrl.value = canvas.toDataURL('image/png')
  } catch (error) {
    console.error('生成预览失败:', error)
    ElMessage.error('预览生成失败')
  } finally {
    zoomLoading.value = false
  }
}

// ==================== 图片选择器 ====================
const imagePickerVisible = ref(false)
const activeImageTab = ref('preset')
const activeImageField = ref<'background' | 'qrcode' | null>(null)
const uploadedImages = ref<ImageAsset[]>([])

const UPLOADED_IMAGES_KEY = 'poster_uploaded_images'
const MAX_UPLOAD_COUNT = 50

/** 根据当前选择字段过滤预设素材（仅背景图有预设素材，二维码等需用户上传） */
const presetAssetsForField = computed(() => {
  if (activeImageField.value !== 'background') return []
  return presetAssets.filter(a => a.category === 'background')
})

/** 打开图片选择器 */
const openImagePicker = (field: 'background' | 'qrcode') => {
  activeImageField.value = field
  // 二维码已无预设占位素材，默认进入本地上传，引导用户上传真实二维码
  activeImageTab.value = field === 'qrcode' ? 'upload' : 'preset'
  imagePickerVisible.value = true
}

/** 选择图片 */
const selectImage = (asset: ImageAsset) => {
  if (activeImageField.value === 'background') {
    posterData.backgroundImage = asset
  } else if (activeImageField.value === 'qrcode') {
    posterData.qrcodeImage = asset
    posterData.showQr = true
  }
  imagePickerVisible.value = false
  ElMessage.success(`已选择「${asset.name}」`)
}

/** 素材库快速应用背景图：直接设置，不经过选择器弹窗 */
const applyMaterial = (asset: ImageAsset) => {
  posterData.backgroundImage = asset
}

/** 我的图片按上传时间倒序（最新上传在前，便于快速复用） */
const recentUploadedImages = computed(() => [...uploadedImages.value].reverse())

/** 删除已上传图片（素材库「我的图片」与选择器弹窗「我的图片」共用入口） */
const deleteUploadedImage = (id: string) => {
  uploadedImages.value = uploadedImages.value.filter(img => img.id !== id)
  saveUploadedImages()
  ElMessage.success('图片已删除')
}

/** 淘汰最早上传的图片（LRU），用于数量/配额超限时自动腾出存储空间 */
const evictOldestImage = () => {
  if (uploadedImages.value.length === 0) return
  let oldestIndex = 0
  uploadedImages.value.forEach((img, i) => {
    if ((img.uploadTime ?? 0) < (uploadedImages.value[oldestIndex].uploadTime ?? 0)) {
      oldestIndex = i
    }
  })
  uploadedImages.value.splice(oldestIndex, 1)
}

/** 上传前校验与处理：校验大小/格式/分辨率，转base64存localStorage */
const handleBeforeUpload = async (file: File): Promise<boolean> => {
  // 数量达到上限时自动淘汰最早上传的图片，为新图腾出位置
  let evictedForCount = 0
  while (uploadedImages.value.length >= MAX_UPLOAD_COUNT) {
    evictOldestImage()
    evictedForCount++
  }
  if (evictedForCount > 0) {
    ElMessage.warning(`图片数量已达上限，已自动清理 ${evictedForCount} 张最早上传的图片`)
  }
  // 校验大小
  if (file.size > 5 * 1024 * 1024) {
    ElMessage.error('文件大小不能超过5MB')
    return false
  }
  // 校验格式
  const validTypes = ['image/jpeg', 'image/png', 'image/webp']
  if (!validTypes.includes(file.type)) {
    ElMessage.error('仅支持JPG、PNG、WebP格式')
    return false
  }
  // 校验分辨率
  const img = new Image()
  const url = URL.createObjectURL(file)
  img.src = url
  await new Promise<void>(resolve => {
    img.onload = () => resolve()
    img.onerror = () => resolve()
  })
  if (img.width < 800) {
    ElMessage.error('图片宽度不能小于800px')
    URL.revokeObjectURL(url)
    return false
  }
  URL.revokeObjectURL(url)

  // 转base64
  const reader = new FileReader()
  reader.onload = () => {
    const dataUrl = reader.result as string
    const asset: ImageAsset = {
      id: `upload_${Date.now()}_${Math.random().toString(36).slice(2, 8)}`,
      name: file.name.length > 12 ? file.name.slice(0, 10) + '...' : file.name,
      url: dataUrl,
      source: 'upload',
      category: activeImageField.value === 'background' ? 'background' : 'qrcode',
      thumbnail: dataUrl,
      uploadTime: Date.now()
    }
    uploadedImages.value.push(asset)
    saveUploadedImages()
    ElMessage.success('上传成功')
  }
  reader.readAsDataURL(file)
  return false // 阻止el-upload自动上传
}

/** 从localStorage加载已上传图片 */
const loadUploadedImages = () => {
  try {
    const raw = localStorage.getItem(UPLOADED_IMAGES_KEY)
    if (raw) {
      uploadedImages.value = JSON.parse(raw)
    }
  } catch (e) {
    console.error('加载上传图片失败:', e)
  }
}

/** 保存已上传图片到localStorage；配额不足时自动淘汰最早上传的图片直至写入成功 */
const saveUploadedImages = () => {
  let evictedForStorage = 0
  while (true) {
    try {
      localStorage.setItem(UPLOADED_IMAGES_KEY, JSON.stringify(uploadedImages.value))
      if (evictedForStorage > 0) {
        ElMessage.warning(`存储空间不足，已自动清理 ${evictedForStorage} 张最早上传的图片`)
      }
      return
    } catch (e) {
      if (uploadedImages.value.length === 0) {
        console.error('保存上传图片失败:', e)
        ElMessage.error('存储空间不足，无法保存图片')
        return
      }
      evictOldestImage()
      evictedForStorage++
    }
  }
}

// ==================== 草稿功能 ====================
const DRAFTS_KEY = 'poster_drafts'
const MAX_DRAFT_COUNT = 5
const AUTO_SAVE_INTERVAL = 60000 // 60秒（兜底保存；主动保存在停止编辑后由防抖触发）

const drafts = ref<PosterDraft[]>([])
const draftsDialogVisible = ref(false)
let autoSaveTimer: ReturnType<typeof setInterval> | null = null

/** 加载草稿列表 */
const loadDrafts = () => {
  try {
    const raw = localStorage.getItem(DRAFTS_KEY)
    if (raw) {
      drafts.value = JSON.parse(raw)
    }
  } catch (e) {
    console.error('加载草稿失败:', e)
  }
}

/** 保存草稿列表到localStorage */
const saveDraftsToStorage = () => {
  try {
    localStorage.setItem(DRAFTS_KEY, JSON.stringify(drafts.value))
    return true
  } catch (e) {
    console.error('保存草稿失败:', e)
    ElMessage.error('存储空间不足，草稿保存失败')
    return false
  }
}

/** 保存当前编辑状态为草稿（最多5个，超出提示导出） */
const handleSaveDraft = () => {
  // 检查草稿数量
  if (drafts.value.length >= MAX_DRAFT_COUNT) {
    ElMessageBox.confirm(
      `草稿数量已达上限${MAX_DRAFT_COUNT}个，继续保存将覆盖最早的草稿。是否继续？`,
      '草稿数量提示',
      { confirmButtonText: '继续保存', cancelButtonText: '取消', type: 'warning' }
    ).then(() => {
      // 移除最早的草稿
      drafts.value.shift()
      doSaveDraft()
    }).catch(() => {})
    return
  }
  doSaveDraft()
}

/** 执行保存草稿 */
const doSaveDraft = () => {
  const now = Date.now()
  const date = new Date(now)
  const pad = (n: number) => n.toString().padStart(2, '0')
  const draft: PosterDraft = {
    id: `draft_${now}_${Math.random().toString(36).slice(2, 8)}`,
    name: `草稿 ${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}`,
    templateId: selectedTemplateId.value,
    posterData: JSON.parse(JSON.stringify(posterData)),
    createdAt: now
  }
  drafts.value.unshift(draft)
  if (saveDraftsToStorage()) {
    ElMessage.success('草稿保存成功')
  }
}

/** 自动保存草稿（静默，不提示）；仅在内容变化时写入localStorage，避免无谓写入触发「重新加载站点」提示 */
const autoSaveDraft = () => {
  // 只在已有草稿时静默更新最近一条，避免无限增加草稿
  if (drafts.value.length === 0) return
  const latest = drafts.value[0]
  const newPosterData = JSON.parse(JSON.stringify(posterData))
  // 内容与最新草稿一致时直接返回，不写localStorage
  const posterDataChanged =
    !latest.posterData ||
    JSON.stringify(latest.posterData) !== JSON.stringify(newPosterData)
  if (!posterDataChanged && latest.templateId === selectedTemplateId.value) {
    return
  }
  latest.templateId = selectedTemplateId.value
  latest.posterData = newPosterData
  latest.createdAt = Date.now()
  saveDraftsToStorage()
}

/** 自动保存防抖：停止编辑5秒后静默保存，避免编辑过程中频繁写localStorage */
const debouncedAutoSaveDraft = debounce(() => {
  autoSaveDraft()
}, 5000)

// 编辑内容变化后触发防抖自动保存（深监听海报数据与模板切换）
watch(posterData, () => debouncedAutoSaveDraft(), { deep: true })
watch(selectedTemplateId, () => debouncedAutoSaveDraft())

/** 打开草稿列表弹窗 */
const openDraftsDialog = () => {
  loadDrafts()
  draftsDialogVisible.value = true
}

/** 恢复草稿 */
const restoreDraft = (draft: PosterDraft) => {
  const data = draft.posterData
  // 逐字段恢复，避免直接替换reactive对象
  posterData.badge = data.badge
  posterData.title = data.title
  posterData.subtitle = data.subtitle
  posterData.price = data.price
  posterData.originalPrice = data.originalPrice
  posterData.itemsText = data.itemsText
  posterData.description = data.description ?? ''
  posterData.effectiveDate = data.effectiveDate ?? null
  posterData.storeName = data.storeName
  posterData.storeAddress = data.storeAddress ?? ''
  posterData.contact = data.contact
  posterData.showQr = data.showQr
  posterData.titleFontSize = data.titleFontSize
  posterData.colorIndex = data.colorIndex
  posterData.textColor = data.textColor
  posterData.backgroundColor = data.backgroundColor ?? ''
  posterData.backgroundImage = data.backgroundImage
  posterData.qrcodeImage = data.qrcodeImage
  selectedTemplateId.value = draft.templateId
  selectedColorIndex.value = data.colorIndex
  draftsDialogVisible.value = false
  ElMessage.success(`已恢复「${draft.name}」`)
}

/** 删除草稿 */
const deleteDraft = (id: string) => {
  ElMessageBox.confirm('确定删除此草稿？', '删除确认', {
    confirmButtonText: '删除',
    cancelButtonText: '取消',
    type: 'warning'
  }).then(() => {
    drafts.value = drafts.value.filter(d => d.id !== id)
    saveDraftsToStorage()
    ElMessage.success('草稿已删除')
  }).catch(() => {})
}

/** 格式化时间戳 */
const formatTime = (ts: number): string => {
  const d = new Date(ts)
  const pad = (n: number) => n.toString().padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`
}

// ==================== 导出图片 ====================
const handleExport = async () => {
  if (!posterCanvasRef.value) {
    ElMessage.error('预览区域未就绪')
    return
  }
  try {
    ElMessage.info('正在生成图片，请稍候...')
    const canvas = await withFullScale(() =>
      html2canvas(posterCanvasRef.value!, {
        scale: 2,
        useCORS: true,
        backgroundColor: null,
        logging: false
      })
    )
    canvas.toBlob((blob) => {
      if (!blob) {
        ElMessage.error('图片生成失败')
        return
      }
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      const sizeName = sizeOptions.find(s => s.id === selectedSize.value)?.name || '海报'
      link.download = `海报_${sizeName}_${Date.now()}.png`
      document.body.appendChild(link)
      link.click()
      document.body.removeChild(link)
      URL.revokeObjectURL(url)
      ElMessage.success('图片导出成功')
    }, 'image/png')
  } catch (error) {
    console.error('导出失败:', error)
    ElMessage.error('图片导出失败，请重试')
  }
}

// ==================== 工具方法 ====================
const formatPrice = (price: number) => {
  return price.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ',')
}

// ==================== 退出提示 ====================
/** 页面卸载前提示保存 */
const handleBeforeUnload = (e: BeforeUnloadEvent) => {
  e.preventDefault()
  e.returnValue = '编辑内容可能未保存，确定离开吗？'
}

// ==================== 生命周期 ====================
onMounted(() => {
  loadUploadedImages()
  loadDrafts()
  // 启动60秒兜底自动保存（主动保存在停止编辑后由防抖触发）
  autoSaveTimer = setInterval(autoSaveDraft, AUTO_SAVE_INTERVAL)
  // 监听页面卸载
  window.addEventListener('beforeunload', handleBeforeUnload)
  // 预览区尺寸变化时重算缩放比（窗口缩放、右侧面板折叠等）
  updatePreviewScale()
  if (previewAreaRef.value) {
    previewResizeObserver = new ResizeObserver(updatePreviewScale)
    previewResizeObserver.observe(previewAreaRef.value)
  }
})

onUnmounted(() => {
  if (autoSaveTimer) {
    clearInterval(autoSaveTimer)
    autoSaveTimer = null
  }
  window.removeEventListener('beforeunload', handleBeforeUnload)
  previewResizeObserver?.disconnect()
  previewResizeObserver = null
})
</script>

<style scoped>
.poster-page {
  width: 100%;
  height: 100%;
}

.designer-container {
  display: flex;
  gap: 12px;
  height: calc(100vh - 120px);
  padding: 0 0 12px 0;
}

/* ========== 左侧模板选择 ========== */
.template-panel {
  width: 260px;
  background: var(--bg-tertiary);
  border: 1px solid var(--border-primary);
  border-radius: var(--radius-lg);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  flex-shrink: 0;
  position: relative;
}

.panel-header {
  padding: 16px 20px;
  border-bottom: 1px solid var(--border-primary);
  position: relative;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.panel-header::after {
  content: '';
  position: absolute;
  bottom: 0;
  left: 20px;
  width: 40px;
  height: 2px;
  background: var(--primary);
}

.panel-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
  display: flex;
  align-items: center;
  gap: 8px;
}

.panel-title::before {
  content: '';
  width: 4px;
  height: 14px;
  background: var(--primary);
  border-radius: 2px;
}

.draft-entry-btn {
  background: var(--bg-secondary);
  border: 1px solid var(--border-primary);
  border-radius: var(--radius-sm);
  color: var(--text-secondary);
  padding: 4px 10px;
  font-size: 12px;
  cursor: pointer;
  transition: all 0.3s;
}

.draft-entry-btn:hover {
  border-color: var(--primary);
  color: var(--primary);
}

.template-tabs {
  display: flex;
  gap: 2px;
  padding: 10px 16px;
  border-bottom: 1px solid var(--border-primary);
}

.template-tab {
  flex: 1;
  padding: 6px;
  font-size: 12px;
  color: var(--text-tertiary);
  cursor: pointer;
  text-align: center;
  border-radius: var(--radius-sm);
  transition: all 0.3s;
}

.template-tab:hover {
  color: var(--text-primary);
}

.template-tab.active {
  background: var(--primary);
  color: var(--bg-primary);
  font-weight: 600;
}

.template-list {
  flex: 1;
  overflow-y: auto;
  padding: 12px;
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 10px;
  align-content: start;
}

.template-card {
  border: 2px solid var(--border-primary);
  border-radius: var(--radius-md);
  overflow: hidden;
  cursor: pointer;
  transition: all 0.3s;
  position: relative;
}

.template-card:hover {
  border-color: var(--primary);
  transform: translateY(-2px);
}

.template-card.selected {
  border-color: var(--primary);
  box-shadow: 0 0 20px rgba(6, 212, 228, 0.3);
}

.template-card.selected::after {
  content: '✓';
  position: absolute;
  top: 4px;
  right: 4px;
  width: 20px;
  height: 20px;
  border-radius: 50%;
  background: var(--primary);
  color: var(--bg-primary);
  font-size: 12px;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
}

.template-preview {
  height: 110px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 11px;
  text-align: center;
  padding: 8px;
  line-height: 1.3;
  white-space: pre-line;
}

.template-name {
  padding: 6px 8px;
  font-size: 11px;
  color: var(--text-secondary);
  text-align: center;
  background: var(--bg-secondary);
}

/* ========== 中间编辑预览区 ========== */
.editor-panel {
  flex: 1;
  background: var(--bg-tertiary);
  border: 1px solid var(--border-primary);
  border-radius: var(--radius-lg);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  position: relative;
}

.editor-toolbar {
  padding: 12px 20px;
  border-bottom: 1px solid var(--border-primary);
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
}

.toolbar-group {
  display: flex;
  gap: 8px;
}

.tool-btn {
  background: var(--bg-secondary);
  border: 1px solid var(--border-primary);
  border-radius: var(--radius-md);
  color: var(--text-secondary);
  padding: 6px 12px;
  font-size: 12px;
  cursor: pointer;
  transition: all 0.3s;
  display: flex;
  align-items: center;
  gap: 4px;
}

.tool-btn:hover {
  border-color: var(--primary);
  color: var(--primary);
}

.size-selector {
  display: flex;
  gap: 4px;
  background: var(--bg-secondary);
  padding: 3px;
  border-radius: var(--radius-md);
  border: 1px solid var(--border-primary);
}

.size-btn {
  padding: 4px 10px;
  font-size: 12px;
  color: var(--text-tertiary);
  cursor: pointer;
  border-radius: var(--radius-sm);
  transition: all 0.3s;
  background: var(--bg-secondary);
  border: 1px solid var(--border-primary);
}

.size-btn:hover {
  color: var(--text-primary);
}

.size-btn.active {
  background: var(--primary);
  color: var(--bg-primary);
  font-weight: 600;
}

.preview-area {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 30px;
  overflow: auto;
  background: repeating-conic-gradient(var(--bg-secondary) 0% 25%, transparent 0% 50%) 50% / 20px 20px;
  position: relative;
}

/* 缩放占位层：撑出缩放后的实际视觉尺寸，让预览区居中与滚动判断正确 */
.poster-stage {
  flex: none;
  transition: width 0.3s, height 0.3s;
}

.poster-canvas {
  border-radius: var(--radius-md);
  box-shadow: 0 8px 40px rgba(0, 0, 0, 0.5), 0 0 60px rgba(255, 107, 107, 0.2);
  position: relative;
  overflow: hidden;
  transform-origin: top left;
  transition: width 0.3s, height 0.3s, background 0.3s, transform 0.3s;
  cursor: zoom-in;
}

/* 截图时必须立刻回到 100%，过渡中的中间值会让 html2canvas 取到错误尺寸 */
.poster-stage.capturing,
.poster-canvas.capturing {
  transition: none;
}

.poster-bg-img {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.poster-content {
  position: absolute;
  inset: 0;
  padding: 30px;
  display: flex;
  flex-direction: column;
}

.poster-badge {
  display: inline-block;
  background: rgba(255, 255, 255, 0.2);
  backdrop-filter: blur(10px);
  padding: 4px 12px;
  border-radius: 20px;
  font-size: 11px;
  font-weight: 500;
  width: fit-content;
  border: 1px solid rgba(255, 255, 255, 0.3);
}

.poster-title {
  font-weight: 700;
  margin-top: 20px;
  line-height: 1.2;
  text-shadow: 0 2px 10px rgba(0, 0, 0, 0.2);
}

.poster-subtitle {
  font-size: 14px;
  margin-top: 8px;
  opacity: 0.9;
}

.poster-description {
  font-size: 12px;
  margin-top: 8px;
  line-height: 1.5;
  opacity: 0.85;
  max-width: 80%;
}

.poster-effective {
  font-size: 12px;
  margin-top: 4px;
  opacity: 0.85;
  display: inline-block;
  padding: 2px 8px;
  background: rgba(255, 255, 255, 0.2);
  border-radius: 4px;
}

.poster-highlight {
  background: rgba(255, 255, 255, 0.15);
  backdrop-filter: blur(10px);
  border-radius: var(--radius-md);
  padding: 16px;
  margin-top: 20px;
  border: 1px solid rgba(255, 255, 255, 0.2);
}

.poster-price {
  font-size: 36px;
  font-weight: 700;
  font-family: 'JetBrains Mono', monospace;
}

.poster-price .unit {
  font-size: 16px;
  font-weight: 400;
}

.poster-original {
  font-size: 13px;
  text-decoration: line-through;
  opacity: 0.7;
  margin-top: 4px;
}

.poster-items {
  margin-top: 16px;
  flex: 1;
}

.poster-item {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
  padding: 4px 0;
  opacity: 0.95;
}

.poster-item::before {
  content: '✦';
  font-size: 12px;
}

.poster-footer {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  margin-top: 16px;
}

.poster-store {
  font-size: 14px;
  font-weight: 600;
}

.poster-address {
  font-size: 10px;
  opacity: 0.8;
  margin-top: 2px;
}

.poster-contact {
  font-size: 11px;
  opacity: 0.85;
  margin-top: 2px;
}

.poster-qr {
  width: 60px;
  height: 60px;
  background: #fff;
  border-radius: var(--radius-sm);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 28px;
  color: #333;
  flex-shrink: 0;
  overflow: hidden;
}

.poster-qr .qr-img {
  width: 100%;
  height: 100%;
  object-fit: contain;
}

.preview-hint {
  position: absolute;
  bottom: 16px;
  right: 16px;
  background: rgba(0, 0, 0, 0.6);
  color: #fff;
  padding: 4px 12px;
  border-radius: var(--radius-sm);
  font-size: 12px;
  pointer-events: none;
}

/* ========== 右侧素材/属性面板 ========== */
.props-panel {
  width: 300px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  overflow: hidden;
}

.props-card {
  background: var(--bg-tertiary);
  border: 1px solid var(--border-primary);
  border-radius: var(--radius-lg);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  position: relative;
  flex: 1;
  min-height: 0;
}

/* 内容高度自适应的卡片（样式配置、素材库），空间不足时可收缩并内部滚动 */
.props-card-auto {
  flex: 0 1 auto;
}

/* 折叠态：仅保留标题栏高度 */
.props-card.collapsed {
  flex: 0 0 auto;
}

/* 可折叠标题栏 */
.panel-header.collapsible {
  cursor: pointer;
  user-select: none;
}

.panel-header.collapsible:hover .panel-title {
  color: var(--primary);
}

.collapse-arrow {
  font-size: 12px;
  color: var(--text-secondary);
  transition: transform 0.3s;
}

.collapse-arrow.collapsed {
  transform: rotate(-90deg);
}

.props-body {
  flex: 1;
  overflow-y: auto;
  padding: 16px;
  min-height: 0;
}

/* 属性表单 */
.prop-group {
  margin-bottom: 16px;
}

.prop-label {
  font-size: 12px;
  color: var(--text-secondary);
  margin-bottom: 6px;
  font-weight: 500;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.font-size-display {
  color: var(--primary);
  font-family: 'JetBrains Mono', monospace;
}

/* 图片字段 */
.image-field {
  width: 100%;
  height: 80px;
  background: var(--bg-secondary);
  border: 1px dashed var(--border-secondary);
  border-radius: var(--radius-md);
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.3s;
  overflow: hidden;
}

.image-field:hover {
  border-color: var(--primary);
}

.field-preview {
  max-width: 100%;
  max-height: 100%;
  object-fit: contain;
}

.field-placeholder {
  font-size: 12px;
  color: var(--text-tertiary);
}

.clear-btn {
  margin-top: 6px;
  background: none;
  border: none;
  color: var(--text-tertiary);
  font-size: 12px;
  cursor: pointer;
  padding: 0;
}

.clear-btn:hover {
  color: var(--primary);
}

/* 颜色选择器 */
.color-picker {
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
}

.color-swatch {
  width: 28px;
  height: 28px;
  border-radius: var(--radius-sm);
  cursor: pointer;
  border: 2px solid transparent;
  transition: all 0.3s;
}

.color-swatch:hover {
  transform: scale(1.1);
}

.color-swatch.selected {
  border-color: var(--primary);
  box-shadow: 0 0 20px rgba(6, 212, 228, 0.3);
}

/* 字号控制 */
.font-size-control {
  display: flex;
  gap: 4px;
  align-items: center;
}

.font-size-btn {
  width: 28px;
  height: 28px;
  background: var(--bg-secondary);
  border: 1px solid var(--border-primary);
  border-radius: var(--radius-sm);
  color: var(--text-secondary);
  cursor: pointer;
  font-size: 14px;
}

.font-size-btn:hover {
  color: var(--primary);
  border-color: var(--primary);
}

.font-size-value {
  flex: 1;
  text-align: center;
  font-size: 13px;
  color: var(--text-primary);
  font-family: 'JetBrains Mono', monospace;
}

/* 素材库 */
.material-grid {
  display: grid;
  grid-template-columns: 1fr 1fr 1fr;
  gap: 8px;
}

.material-item {
  aspect-ratio: 1;
  border-radius: var(--radius-sm);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.3s;
  border: 2px solid transparent;
  overflow: hidden;
}

.material-item:hover {
  border-color: var(--primary);
  transform: scale(1.05);
}

.material-thumb {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

/* 素材库区块间距 */
.material-section + .material-section {
  margin-top: 16px;
}

/* 当前应用的背景图高亮 */
.material-item.active {
  border-color: var(--primary);
  box-shadow: 0 0 0 2px var(--primary);
}

/* 底部操作栏 */
.action-bar {
  padding: 12px;
  border-top: 1px solid var(--border-primary);
  display: flex;
  gap: 8px;
  background: var(--bg-tertiary);
  border-radius: var(--radius-lg);
}

.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  padding: 10px 16px;
  border-radius: var(--radius-md);
  font-size: 13px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.3s;
  border: 1px solid var(--border-primary);
  background: var(--bg-secondary);
  color: var(--text-secondary);
  flex: 1;
}

.btn:hover {
  border-color: var(--primary);
  color: var(--primary);
}

.btn-primary {
  background: var(--primary);
  border-color: var(--primary);
  color: var(--bg-primary);
  font-weight: 600;
}

.btn-primary:hover {
  background: var(--primary-hover);
  border-color: var(--primary-hover);
  color: var(--bg-primary);
  box-shadow: 0 0 20px rgba(6, 212, 228, 0.3);
}

/* ========== 图片选择器弹窗 ========== */
.asset-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 12px;
  padding: 8px 0;
  max-height: 400px;
  overflow-y: auto;
}

.asset-item {
  border: 1px solid var(--border-primary);
  border-radius: var(--radius-md);
  padding: 8px;
  cursor: pointer;
  transition: all 0.3s;
  text-align: center;
}

.asset-item:hover {
  border-color: var(--primary);
  transform: translateY(-2px);
}

.asset-thumb {
  width: 100%;
  aspect-ratio: 1;
  object-fit: cover;
  border-radius: var(--radius-sm);
  margin-bottom: 6px;
}

.asset-name {
  font-size: 12px;
  color: var(--text-secondary);
  display: block;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* 已上传图片删除按钮（素材库「我的图片」与选择器弹窗「我的图片」共用） */
.material-item,
.asset-item {
  position: relative;
}

.asset-delete-btn {
  position: absolute;
  top: 4px;
  right: 4px;
  width: 18px;
  height: 18px;
  border: none;
  border-radius: 50%;
  background: rgba(0, 0, 0, 0.55);
  color: #fff;
  font-size: 13px;
  line-height: 1;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  opacity: 0;
  transition: opacity 0.2s;
  z-index: 2;
}

.material-item:hover .asset-delete-btn,
.asset-item:hover .asset-delete-btn {
  opacity: 1;
}

.upload-area {
  text-align: center;
  padding: 40px 20px;
}

.upload-btn {
  background: var(--primary);
  color: var(--bg-primary);
  border: none;
  border-radius: var(--radius-md);
  padding: 10px 24px;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.3s;
}

.upload-btn:hover {
  background: var(--primary-hover);
}

.upload-tip {
  margin-top: 12px;
  font-size: 12px;
  color: var(--text-tertiary);
}

.empty-tip {
  text-align: center;
  padding: 40px;
  color: var(--text-tertiary);
  font-size: 13px;
}

/* ========== 放大预览弹窗 ========== */
.zoom-container {
  text-align: center;
  max-height: 70vh;
  overflow: auto;
}

.zoom-loading {
  padding: 40px;
  color: var(--text-tertiary);
  font-size: 14px;
}

.zoom-img {
  max-width: 100%;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
}

/* ========== 草稿列表弹窗 ========== */
.draft-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
  max-height: 400px;
  overflow-y: auto;
}

.draft-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 16px;
  /* 弹窗为浅色浮层风格，草稿列表用浅色样式与白色弹窗背景协调 */
  background: #f9fafb;
  border: 1px solid #e5e7eb;
  border-radius: var(--radius-md);
}

.draft-info {
  flex: 1;
}

.draft-name {
  font-size: 13px;
  font-weight: 500;
  color: #1f2937;
}

.draft-time {
  font-size: 11px;
  color: #9ca3af;
  margin-top: 4px;
}

.draft-actions {
  display: flex;
  gap: 6px;
}

.draft-btn {
  padding: 4px 12px;
  font-size: 12px;
  border-radius: var(--radius-sm);
  cursor: pointer;
  /* 浅色弹窗内按钮配色 */
  border: 1px solid #d1d5db;
  background: #f3f4f6;
  color: #4b5563;
  transition: all 0.3s;
}

.draft-btn.restore:hover {
  border-color: var(--primary);
  color: var(--primary);
}

.draft-btn.delete:hover {
  border-color: #ef4444;
  color: #ef4444;
}

/* Element Plus 暗色主题适配 */
:deep(.el-input__wrapper),
:deep(.el-textarea__inner),
:deep(.el-input-number__wrapper) {
  background-color: var(--bg-secondary) !important;
  box-shadow: 0 0 0 1px var(--border-primary) inset !important;
}

:deep(.el-input__inner),
:deep(.el-textarea__inner) {
  color: var(--text-primary) !important;
}

:deep(.el-input__inner::placeholder),
:deep(.el-textarea__inner::placeholder) {
  color: var(--text-tertiary) !important;
}

:deep(.el-input__wrapper:hover),
:deep(.el-textarea__inner:hover) {
  box-shadow: 0 0 0 1px var(--border-secondary) inset !important;
}

:deep(.el-input__wrapper.is-focus),
:deep(.el-textarea__inner:focus) {
  box-shadow: 0 0 0 1px var(--primary) inset !important;
}

/* 滚动条 */
.template-list::-webkit-scrollbar,
.props-body::-webkit-scrollbar,
.preview-area::-webkit-scrollbar,
.asset-grid::-webkit-scrollbar,
.draft-list::-webkit-scrollbar {
  width: 6px;
  height: 6px;
}

.template-list::-webkit-scrollbar-track,
.props-body::-webkit-scrollbar-track,
.preview-area::-webkit-scrollbar-track,
.asset-grid::-webkit-scrollbar-track,
.draft-list::-webkit-scrollbar-track {
  background: var(--bg-secondary);
  border-radius: 3px;
}

.template-list::-webkit-scrollbar-thumb,
.props-body::-webkit-scrollbar-thumb,
.preview-area::-webkit-scrollbar-thumb,
.asset-grid::-webkit-scrollbar-thumb,
.draft-list::-webkit-scrollbar-thumb {
  background: var(--border-secondary);
  border-radius: 3px;
}
</style>
