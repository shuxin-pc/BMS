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
              :placeholder="searchPlaceholder"
            />
          </div>

          <!-- 结果区 -->
          <div ref="listRef" class="palette-body">
            <!-- 空关键字：最近搜索 -->
            <template v-if="!trimmedKeyword">
              <div v-if="historyList.length > 0" class="palette-section-title">最近搜索</div>
              <div
                v-for="h in historyList"
                :key="h"
                class="palette-item"
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
const historyList = ref<string[]>([])

const trimmedKeyword = computed(() => keyword.value.trim())

// 输入框提示文案按当前用户可用的搜索源动态拼接（未接入门店系统的租户不显示门店词汇）
const searchPlaceholder = computed(() => {
  const labels = getActiveProviders().map(p => p.label)
  return `搜索${labels.join('、')}…`
})

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
  loading.value = false
}

watch(keyword, () => {
  if (debounceTimer !== null) {
    window.clearTimeout(debounceTimer)
    debounceTimer = null
  }
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

// ============ 打开/关闭/跳转 ============

async function open() {
  // 每次打开清空上次关键字（watch 联动清空结果列表与防抖计时）
  keyword.value = ''
  historyList.value = loadHistory()
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

// ============ 全局快捷键（Ctrl+K） ============

function onGlobalKeydown(e: KeyboardEvent) {
  if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
    e.preventDefault()
    open()
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

.palette-item:hover {
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
