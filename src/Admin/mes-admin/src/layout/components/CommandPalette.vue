<template>
  <Teleport to="body">
    <transition name="palette-fade">
      <div v-if="visible" class="command-palette-overlay" @mousedown.self="close">
        <div class="command-palette">
          <!-- 输入区 -->
          <div class="palette-input-row">
            <el-icon :size="18"><Search /></el-icon>
            <input
              ref="inputRef"
              v-model="keyword"
              placeholder="搜索功能、顾客、订单、商品…"
              @keydown="onInputKeydown"
            />
            <span class="palette-kbd">ESC</span>
          </div>

          <!-- 结果区 -->
          <div ref="listRef" class="palette-body">
            <!-- 空关键字：最近搜索 -->
            <template v-if="!trimmedKeyword">
              <div v-if="historyList.length > 0" class="palette-section-title">最近搜索</div>
              <div
                v-for="(h, i) in historyList"
                :key="h"
                class="palette-item"
                :class="{ active: i === historyActiveIndex }"
                @mouseenter="historyActiveIndex = i"
                @click="applyHistory(h)"
              >
                <div class="palette-item-main">
                  <span class="palette-item-title">
                    <el-icon class="palette-item-icon"><Clock /></el-icon>{{ h }}
                  </span>
                </div>
                <el-icon class="palette-item-remove" @click.stop="removeHistory(h)"><Close /></el-icon>
              </div>
              <div v-if="historyList.length === 0" class="palette-empty">
                输入关键字搜索功能菜单与业务数据
              </div>
            </template>

            <!-- 有关键字：搜索结果 -->
            <template v-else>
              <div v-if="loading" class="palette-empty">搜索中…</div>
              <div v-else-if="flatItems.length === 0" class="palette-empty">
                未找到与「{{ trimmedKeyword }}」相关的结果
              </div>
              <template v-else>
                <template v-for="(item, index) in flatItems" :key="`${item.group}-${index}`">
                  <div
                    v-if="index === 0 || flatItems[index - 1].group !== item.group"
                    class="palette-section-title"
                  >{{ item.group }}</div>
                  <div
                    class="palette-item"
                    :class="{ active: index === activeIndex }"
                    :data-item-index="index"
                    @mouseenter="activeIndex = index"
                    @click="select(item)"
                  >
                    <div class="palette-item-main">
                      <span class="palette-item-title">
                        <template v-for="(seg, si) in splitHighlight(item.title)" :key="si">
                          <mark v-if="seg.hit">{{ seg.text }}</mark>
                          <template v-else>{{ seg.text }}</template>
                        </template>
                      </span>
                      <span v-if="item.subtitle" class="palette-item-subtitle">
                        <template v-for="(seg, si) in splitHighlight(item.subtitle)" :key="si">
                          <mark v-if="seg.hit">{{ seg.text }}</mark>
                          <template v-else>{{ seg.text }}</template>
                        </template>
                      </span>
                    </div>
                    <span class="palette-item-group">{{ item.group }}</span>
                  </div>
                </template>
              </template>
            </template>
          </div>

          <!-- 底部按键提示 -->
          <div class="palette-footer">
            <span><kbd>↑</kbd><kbd>↓</kbd> 切换</span>
            <span><kbd>Enter</kbd> 打开</span>
            <span><kbd>Esc</kbd> 关闭</span>
          </div>
        </div>
      </div>
    </transition>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { Search, Clock, Close } from '@element-plus/icons-vue'
import { getActiveProviders, resolveSearchTarget } from '@/utils/global-search/providers'
import type { SearchEntry } from '@/utils/global-search/types'
import { useUserStore } from '@/stores/user'

const router = useRouter()

/** 每次搜索单源返回条数上限（与后端默认一致） */
const SEARCH_LIMIT = 5
/** 最近搜索本地存储键与容量 */
const HISTORY_KEY = 'global-search-history'
const HISTORY_MAX = 8

const visible = ref(false)
const keyword = ref('')
const inputRef = ref<HTMLInputElement | null>(null)
const listRef = ref<HTMLElement | null>(null)
const loading = ref(false)
const flatItems = ref<SearchEntry[]>([])
const activeIndex = ref(0)
const historyList = ref<string[]>([])
const historyActiveIndex = ref(0)

const trimmedKeyword = computed(() => keyword.value.trim())

// ============ 最近搜索（localStorage） ============

function loadHistory(): string[] {
  try {
    const parsed = JSON.parse(localStorage.getItem(HISTORY_KEY) || '[]')
    return Array.isArray(parsed) ? parsed.filter(x => typeof x === 'string') : []
  } catch {
    return []
  }
}

function saveHistory(list: string[]) {
  try {
    localStorage.setItem(HISTORY_KEY, JSON.stringify(list.slice(0, HISTORY_MAX)))
  } catch {
    // 存储失败不影响主流程
  }
}

function pushHistory(kw: string) {
  const list = [kw, ...loadHistory().filter(x => x !== kw)]
  saveHistory(list)
  historyList.value = list.slice(0, HISTORY_MAX)
}

function removeHistory(kw: string) {
  const list = loadHistory().filter(x => x !== kw)
  saveHistory(list)
  historyList.value = list
}

/** 回填历史关键字并立即搜索（跳过 300ms 防抖） */
let immediateNext = false
function applyHistory(h: string) {
  immediateNext = true
  keyword.value = h
}

// ============ 搜索执行（防抖 + 竞态保护 + 单源失败静默） ============

let searchSeq = 0
let debounceTimer: number | null = null

function scheduleSearch() {
  if (debounceTimer !== null) window.clearTimeout(debounceTimer)
  debounceTimer = window.setTimeout(() => {
    debounceTimer = null
    doSearch()
  }, 300)
}

async function doSearch() {
  const kw = trimmedKeyword.value
  if (!kw) {
    flatItems.value = []
    return
  }
  const seq = ++searchSeq
  loading.value = true
  const providers = getActiveProviders()
  // Promise.allSettled：单个搜索源失败（网络/权限）不影响其余源展示
  const results = await Promise.allSettled(providers.map(p => p.search(kw, SEARCH_LIMIT)))
  // 竞态保护：仅采纳最后一次搜索的结果
  if (seq !== searchSeq) return
  const items = results.flatMap(r => (r.status === 'fulfilled' ? r.value : []))
  items.sort((a, b) => (b.weight ?? 0) - (a.weight ?? 0))
  flatItems.value = items
  activeIndex.value = 0
  loading.value = false
}

watch(keyword, () => {
  if (debounceTimer !== null) {
    window.clearTimeout(debounceTimer)
    debounceTimer = null
  }
  activeIndex.value = 0
  historyActiveIndex.value = 0
  if (!trimmedKeyword.value) {
    searchSeq++ // 使进行中的搜索结果失效
    loading.value = false
    flatItems.value = []
    return
  }
  if (immediateNext) {
    immediateNext = false
    doSearch()
  } else {
    scheduleSearch()
  }
})

// 结果更新后滚动条回到顶部
watch(flatItems, () => {
  nextTick(() => {
    if (listRef.value) listRef.value.scrollTop = 0
  })
})

// ============ 键盘交互 ============

function onInputKeydown(e: KeyboardEvent) {
  if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
    e.preventDefault()
    const delta = e.key === 'ArrowDown' ? 1 : -1
    if (!trimmedKeyword.value) {
      // 历史模式上下导航
      if (historyList.value.length === 0) return
      historyActiveIndex.value = (historyActiveIndex.value + delta + historyList.value.length) % historyList.value.length
    } else if (flatItems.value.length > 0) {
      activeIndex.value = (activeIndex.value + delta + flatItems.value.length) % flatItems.value.length
    }
    return
  }
  if (e.key === 'Enter') {
    e.preventDefault()
    if (!trimmedKeyword.value) {
      if (historyList.value.length > 0) applyHistory(historyList.value[historyActiveIndex.value])
      return
    }
    const item = flatItems.value[activeIndex.value]
    if (item) select(item)
  }
}

// 高亮时滚动到可视区
watch(activeIndex, idx => {
  nextTick(() => {
    listRef.value?.querySelector(`[data-item-index="${idx}"]`)?.scrollIntoView({ block: 'nearest' })
  })
})

// ============ 打开/关闭/跳转 ============

async function open() {
  historyList.value = loadHistory()
  historyActiveIndex.value = 0
  visible.value = true
  await nextTick()
  inputRef.value?.focus()
}

function close() {
  visible.value = false
}

/**
 * 选中条目：记录最近搜索 → 解析跳转目标 → 跨子系统先切换再跳转
 * 跳转携带 keyword 查询参数，目标列表页读取后预填搜索框
 */
async function select(entry: SearchEntry) {
  const kw = trimmedKeyword.value
  close()
  if (kw) pushHistory(kw)
  const target = resolveSearchTarget(entry)
  if (!target) return
  const userStore = useUserStore()
  if (String(userStore.currentSubsystemId) !== target.subsystemId) {
    await userStore.switchSubsystem(target.subsystemId)
  }
  router.push({ path: target.path, query: kw ? { keyword: kw } : undefined })
}

// ============ 全局快捷键（Ctrl+K / Esc） ============

function onGlobalKeydown(e: KeyboardEvent) {
  if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
    e.preventDefault()
    open()
    return
  }
  if (visible.value && e.key === 'Escape') {
    e.preventDefault()
    close()
  }
}

onMounted(() => window.addEventListener('keydown', onGlobalKeydown))
onUnmounted(() => window.removeEventListener('keydown', onGlobalKeydown))

// ============ 关键字高亮（分段渲染，避免 v-html 注入风险） ============

interface HighlightSegment {
  text: string
  hit: boolean
}

function splitHighlight(text?: string): HighlightSegment[] {
  if (!text) return []
  const kw = trimmedKeyword.value
  const idx = kw ? text.toLowerCase().indexOf(kw.toLowerCase()) : -1
  if (!kw || idx < 0) return [{ text, hit: false }]
  return [
    { text: text.slice(0, idx), hit: false },
    { text: text.slice(idx, idx + kw.length), hit: true },
    { text: text.slice(idx + kw.length), hit: false }
  ]
}

defineExpose({ open })
</script>

<style scoped>
/* 遮罩层 */
.command-palette-overlay {
  position: fixed;
  inset: 0;
  z-index: 3000;
  background: rgba(0, 0, 0, 0.6);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: flex-start;
  justify-content: center;
  padding-top: 12vh;
}

/* 面板主体 - 发光暗色风格 */
.command-palette {
  width: 640px;
  max-width: calc(100vw - 40px);
  background: var(--bg-tertiary);
  border: 1px solid var(--border-glow);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-lg), var(--shadow-glow-primary);
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

/* 输入区 */
.palette-input-row {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 14px 18px;
  border-bottom: 1px solid var(--border-primary);
  color: var(--primary);
}

.palette-input-row input {
  flex: 1;
  background: transparent;
  border: none;
  outline: none;
  color: var(--text-primary);
  font-size: 16px;
  caret-color: var(--primary);
}

.palette-input-row input::placeholder {
  color: var(--text-tertiary);
}

.palette-kbd {
  font-size: 11px;
  color: var(--text-tertiary);
  border: 1px solid var(--border-primary);
  border-radius: 4px;
  padding: 2px 6px;
}

/* 结果区 */
.palette-body {
  max-height: 420px;
  overflow-y: auto;
  padding: 8px 0;
}

.palette-section-title {
  padding: 8px 18px 4px;
  font-size: 12px;
  color: var(--text-tertiary);
  letter-spacing: 0.5px;
}

.palette-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 10px 18px;
  cursor: pointer;
  border-left: 2px solid transparent;
  transition: background 0.15s ease;
}

.palette-item.active {
  background: var(--bg-hover);
  border-left-color: var(--primary);
  box-shadow: inset 0 0 20px rgba(6, 212, 228, 0.05);
}

.palette-item-main {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.palette-item-title {
  color: var(--text-primary);
  font-size: 14px;
  display: flex;
  align-items: center;
}

.palette-item-icon {
  margin-right: 8px;
  color: var(--text-tertiary);
  flex-shrink: 0;
}

.palette-item-subtitle {
  color: var(--text-tertiary);
  font-size: 12px;
}

.palette-item mark {
  background: transparent;
  color: var(--primary);
  font-weight: 600;
}

.palette-item-group {
  flex-shrink: 0;
  font-size: 11px;
  color: var(--primary);
  background: var(--bg-glow);
  border: 1px solid var(--border-glow);
  border-radius: 4px;
  padding: 1px 8px;
}

.palette-item-remove {
  color: var(--text-tertiary);
  opacity: 0;
  transition: opacity 0.15s ease;
  flex-shrink: 0;
}

.palette-item:hover .palette-item-remove {
  opacity: 1;
}

.palette-item-remove:hover {
  color: var(--danger);
}

.palette-empty {
  padding: 40px 18px;
  text-align: center;
  color: var(--text-tertiary);
  font-size: 13px;
}

/* 底部按键提示 */
.palette-footer {
  display: flex;
  gap: 16px;
  padding: 10px 18px;
  border-top: 1px solid var(--border-primary);
  color: var(--text-tertiary);
  font-size: 12px;
}

.palette-footer kbd {
  border: 1px solid var(--border-primary);
  border-radius: 4px;
  padding: 1px 5px;
  font-size: 11px;
  color: var(--text-secondary);
  background: var(--bg-secondary);
  margin-right: 2px;
}

/* 进出场动画 */
.palette-fade-enter-active,
.palette-fade-leave-active {
  transition: opacity 0.15s ease;
}

.palette-fade-enter-active .command-palette,
.palette-fade-leave-active .command-palette {
  transition: transform 0.15s ease;
}

.palette-fade-enter-from,
.palette-fade-leave-to {
  opacity: 0;
}

.palette-fade-enter-from .command-palette,
.palette-fade-leave-to .command-palette {
  transform: translateY(-8px) scale(0.98);
}
</style>
