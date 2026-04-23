<template>
  <div class="doc-detail" v-if="doc">
    <el-page-header @back="$router.back()">
      <template #content>{{ doc.title }}</template>
      <template #extra>
        <el-space>
          <el-button @click="handleFavorite"><el-icon><Star /></el-icon>{{ isFavorited ? '取消收藏' : '收藏' }}</el-button>
          <el-button type="primary" @click="$router.push(`/documents/${doc.id}/edit`)"><el-icon><Edit /></el-icon>编辑</el-button>
          <el-dropdown trigger="click">
            <el-button><el-icon><MoreFilled /></el-icon></el-button>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click="handlePin">{{ doc.isPinned ? '取消置顶' : '置顶' }}</el-dropdown-item>
                <el-dropdown-item divided @click="handleDelete" style="color:#f56c6c">删除文档</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </el-space>
      </template>
    </el-page-header>

    <el-row :gutter="20" style="margin-top:20px">
      <el-col :span="18">
        <!-- Document Content -->
        <el-card>
          <div class="doc-meta">
            <el-tag size="small">{{ doc.spaceName }}</el-tag>
            <el-tag size="small" type="info">{{ doc.categoryName }}</el-tag>
            <span>作者: {{ doc.authorName }}</span>
            <span>版本: v{{ doc.currentVersion }}</span>
            <span>阅读: {{ doc.viewCount }}</span>
            <span>创建: {{ new Date(doc.createdAt).toLocaleString() }}</span>
          </div>
          <el-divider />
          <div class="doc-content" v-html="doc.content || '<p style=color:#999>暂无内容</p>'"></div>
        </el-card>

        <!-- Comments -->
        <el-card style="margin-top:20px">
          <template #header><span>评论 ({{ comments.length }})</span></template>
          <div v-for="c in comments" :key="c.id" class="comment-item">
            <div class="comment-header">
              <el-avatar :size="28" :style="{ backgroundColor: '#409eff' }">{{ c.userName?.charAt(0) || '?' }}</el-avatar>
              <strong>{{ c.userName }}</strong>
              <span class="comment-time">{{ new Date(c.createdAt).toLocaleString() }}</span>
            </div>
            <p class="comment-body">{{ c.content }}</p>
            <div v-if="c.replies?.length" class="comment-replies">
              <div v-for="r in c.replies" :key="r.id" class="reply-item">
                <strong>{{ r.userName }}</strong>: {{ r.content }}
              </div>
            </div>
          </div>
          <el-divider />
          <el-input v-model="newComment" type="textarea" rows="3" placeholder="发表评论..." />
          <el-button type="primary" style="margin-top:8px" @click="handleAddComment" :disabled="!newComment.trim()">发表</el-button>
        </el-card>
      </el-col>

      <el-col :span="6">
        <!-- Attachments -->
        <el-card>
          <template #header>
            <div style="display:flex;justify-content:space-between;align-items:center">
              <span>附件</span>
              <el-upload :action="''" :before-upload="handleUpload" :show-file-list="false">
                <el-button size="small" type="primary" text><el-icon><Upload /></el-icon></el-button>
              </el-upload>
            </div>
          </template>
          <div v-for="a in attachments" :key="a.id" class="attach-item">
            <el-icon><Document /></el-icon>
            <div class="attach-info">
              <span class="attach-name">{{ a.originalFileName }}</span>
              <span class="attach-size">{{ formatSize(a.fileSize) }}</span>
            </div>
            <el-button-group size="small">
              <el-button type="primary" text @click="openPreview(a)"><el-icon><View /></el-icon></el-button>
              <el-button type="success" text @click="openDownload(a)"><el-icon><Download /></el-icon></el-button>
            </el-button-group>
          </div>
          <el-empty v-if="!attachments.length" description="暂无附件" :image-size="60" />
        </el-card>

        <!-- Version History -->
        <el-card style="margin-top:20px">
          <template #header><span>版本历史</span></template>
          <el-timeline>
            <el-timeline-item v-for="v in versions" :key="v.id" :timestamp="new Date(v.createdAt).toLocaleString()" placement="top">
              <span>v{{ v.versionNumber }} - {{ v.changeNote || '无备注' }}</span>
            </el-timeline-item>
          </el-timeline>
          <el-empty v-if="!versions.length" description="暂无版本" :image-size="60" />
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { getDocument, getDocumentComments, getDocumentVersions, getAttachments, addComment, toggleFavorite, togglePin, deleteDocument, uploadAttachment, updateLearningProgress, getAttachmentPreviewUrl, getAttachmentDownloadUrl } from '../api'
import { useAuthStore } from '../stores/auth'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const docId = Number(route.params.id)

const doc = ref<any>(null)
const comments = ref<any[]>([])
const versions = ref<any[]>([])
const attachments = ref<any[]>([])
const newComment = ref('')
const isFavorited = ref(false)

// Learning progress reporting
let progressTimer: ReturnType<typeof setInterval> | null = null
let readTime = 0

function startProgressTracking() {
  progressTimer = setInterval(async () => {
    readTime += 10
    const scrollPercent = Math.min(100, Math.round((window.scrollY / (document.body.scrollHeight - window.innerHeight)) * 100) || 10)
    try {
      await updateLearningProgress({ DocumentId: docId, ProgressPercent: scrollPercent, AdditionalSeconds: 10 })
    } catch {}
  }, 10000)
}

async function loadAll() {
  const [docRes, commRes, verRes, attRes] = await Promise.all([
    getDocument(docId), getDocumentComments(docId), getDocumentVersions(docId), getAttachments(docId)
  ]) as any[]
  if (docRes.success) doc.value = docRes.data
  if (commRes.success) comments.value = commRes.data
  if (verRes.success) versions.value = verRes.data
  if (attRes.success) attachments.value = attRes.data
}

async function handleAddComment() {
  const res: any = await addComment({ DocumentId: docId, Content: newComment.value })
  if (res.success) { newComment.value = ''; await loadAll(); ElMessage.success('评论成功') }
}

async function handleFavorite() {
  const res: any = await toggleFavorite(docId)
  if (res.success) { isFavorited.value = !isFavorited.value; ElMessage.success(res.message) }
}

async function handlePin() {
  const res: any = await togglePin(docId)
  if (res.success) { doc.value.isPinned = !doc.value.isPinned; ElMessage.success(res.message) }
}

async function handleDelete() {
  await ElMessageBox.confirm('确定删除此文档？', '警告', { type: 'warning' })
  const res: any = await deleteDocument(docId)
  if (res.success) { ElMessage.success('已删除'); router.back() }
}

async function handleUpload(file: File) {
  const formData = new FormData()
  formData.append('file', file)
  const res: any = await uploadAttachment(docId, formData)
  if (res.success) { ElMessage.success('上传成功'); await loadAll() }
  return false
}

function formatSize(bytes: number) {
  if (bytes < 1024) return bytes + 'B'
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + 'KB'
  return (bytes / 1024 / 1024).toFixed(1) + 'MB'
}

function openPreview(a: any) {
  const ext = (a.fileExtension || '').toLowerCase()
  const url = getAttachmentPreviewUrl(a.id) + `?token=${auth.token}`
  if (['.pdf', '.png', '.jpg', '.jpeg', '.gif', '.bmp', '.webp'].includes(ext)) {
    window.open(url, '_blank')
  } else if (['.mp4', '.mov', '.avi', '.webm'].includes(ext)) {
    window.open(url, '_blank')
  } else if (['.mp3', '.m4a', '.wav', '.ogg'].includes(ext)) {
    window.open(url, '_blank')
  } else if (['.doc', '.docx', '.xls', '.xlsx', '.ppt', '.pptx'].includes(ext)) {
    const fullUrl = window.location.origin + getAttachmentPreviewUrl(a.id) + `?token=${auth.token}`
    window.open(`https://view.officeapps.live.com/op/embed.aspx?src=${encodeURIComponent(fullUrl)}`, '_blank')
  } else {
    ElMessage.info('该文件类型暂不支持在线预览，请下载查看')
  }
}

function openDownload(a: any) {
  const url = getAttachmentDownloadUrl(a.id) + `?token=${auth.token}`
  const link = document.createElement('a')
  link.href = url
  link.download = a.originalFileName
  link.target = '_blank'
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
}

onMounted(() => { loadAll(); startProgressTracking() })
onUnmounted(() => { if (progressTimer) clearInterval(progressTimer) })
</script>

<style scoped>
.doc-meta { display: flex; align-items: center; gap: 12px; font-size: 13px; color: #909399; flex-wrap: wrap; }
.doc-content { line-height: 1.8; font-size: 15px; min-height: 200px; }
.doc-content :deep(h1), .doc-content :deep(h2), .doc-content :deep(h3) { margin: 16px 0 8px; }
.doc-content :deep(p) { margin: 8px 0; }
.doc-content :deep(pre) { background: #f6f8fa; padding: 12px; border-radius: 6px; overflow-x: auto; }
.comment-item { padding: 12px 0; border-bottom: 1px solid #f0f0f0; }
.comment-header { display: flex; align-items: center; gap: 8px; margin-bottom: 6px; }
.comment-time { font-size: 12px; color: #c0c4cc; }
.comment-body { font-size: 14px; color: #606266; margin-left: 36px; }
.comment-replies { margin-left: 36px; margin-top: 8px; padding: 8px; background: #fafafa; border-radius: 4px; }
.reply-item { font-size: 13px; color: #606266; margin: 4px 0; }
.attach-item { display: flex; align-items: center; gap: 8px; padding: 8px 0; border-bottom: 1px solid #f5f5f5; cursor: pointer; }
.attach-info { flex: 1; }
.attach-name { font-size: 13px; display: block; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.attach-size { font-size: 11px; color: #c0c4cc; }
</style>
