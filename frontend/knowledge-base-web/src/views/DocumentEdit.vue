<template>
  <div class="doc-edit">
    <el-page-header @back="$router.back()">
      <template #content>{{ isNew ? '新建文档' : '编辑文档' }}</template>
      <template #extra>
        <el-space>
          <el-radio-group v-model="form.EditMode" size="small">
            <el-radio-button :value="0">富文本</el-radio-button>
            <el-radio-button :value="1">Markdown</el-radio-button>
          </el-radio-group>
          <el-button type="primary" @click="handleSave" :loading="saving">保存</el-button>
        </el-space>
      </template>
    </el-page-header>

    <!-- Quick Upload Area (for new doc) -->
    <el-card style="margin-top:20px" v-if="isNew && !quickFile">
      <div class="quick-upload-area">
        <el-upload
          :auto-upload="false"
          :show-file-list="false"
          :on-change="onQuickFileChange"
          accept=".doc,.docx,.xls,.xlsx,.ppt,.pptx,.pdf,.mp3,.m4a,.wav,.ogg,.mp4,.mov,.avi,.flv,.webm,.png,.jpg,.jpeg,.gif"
          drag
          class="quick-upload"
        >
          <el-icon :size="48" color="#409eff"><Upload /></el-icon>
          <div class="el-upload__text">拖拽文件到此处，或 <em>点击上传文件</em></div>
          <div class="el-upload__tip">上传文件后自动识别文件名为文档标题，支持 Word、Excel、PPT、PDF、音频、视频</div>
        </el-upload>
        <el-divider>或手动填写文档信息</el-divider>
      </div>
    </el-card>

    <!-- Quick file preview badge -->
    <el-card style="margin-top:20px" v-if="quickFile">
      <div class="quick-file-badge">
        <el-icon :size="24" :color="getFileIconColor(guessFileType(quickFile.name))"><component :is="getFileIcon(guessFileType(quickFile.name))" /></el-icon>
        <div class="quick-file-info">
          <span class="quick-file-name">{{ quickFile.name }}</span>
          <span class="quick-file-size">{{ formatFileSize(quickFile.size) }}</span>
        </div>
        <el-button type="danger" link @click="removeQuickFile"><el-icon><Close /></el-icon> 移除</el-button>
      </div>
    </el-card>

    <el-card style="margin-top:20px">
      <el-form :model="form" label-width="80px">
        <el-form-item label="标题">
          <el-input v-model="form.Title" placeholder="文档标题" size="large" />
        </el-form-item>
        <el-form-item label="文件名称" v-if="form.OriginalFileName">
          <el-input v-model="form.OriginalFileName" disabled>
            <template #prefix><el-icon><Document /></el-icon></template>
          </el-input>
        </el-form-item>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="空间">
              <el-select v-model="form.SpaceId" placeholder="选择空间" @change="loadSpaceCategories" style="width:100%">
                <el-option v-for="s in spaces" :key="s.id" :label="s.name" :value="s.id" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="目录">
              <el-tree-select v-model="form.CategoryId" :data="categories" :props="{ label: 'name', value: 'id', children: 'children' }" check-strictly placeholder="选择目录" style="width:100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="标签">
          <el-input v-model="form.Tags" placeholder="多个标签用逗号分隔" />
        </el-form-item>
        <el-form-item label="摘要">
          <el-input v-model="form.Summary" type="textarea" rows="2" placeholder="文档摘要" />
        </el-form-item>
        <el-form-item label="内容">
          <el-input v-model="form.Content" type="textarea" :rows="15" placeholder="文档内容（支持 HTML / Markdown）" style="font-family:monospace" />
        </el-form-item>
        <el-form-item label="变更说明" v-if="!isNew">
          <el-input v-model="changeNote" placeholder="本次修改说明" />
        </el-form-item>
      </el-form>
    </el-card>

    <!-- File Upload Section (after doc saved) -->
    <el-card style="margin-top:20px" v-if="savedDocId">
      <template #header>
        <div class="upload-header">
          <span class="upload-title"><el-icon><Paperclip /></el-icon> 附件管理</span>
          <el-tag size="small" type="info">{{ attachments.length }} 个文件</el-tag>
        </div>
      </template>
      <el-upload
        :action="`/api/documents/${savedDocId}/attachments`"
        :headers="uploadHeaders"
        :on-success="onUploadSuccess"
        :on-error="onUploadError"
        :before-upload="beforeUpload"
        multiple
        :show-file-list="false"
        accept=".doc,.docx,.xls,.xlsx,.ppt,.pptx,.pdf,.mp3,.m4a,.wav,.ogg,.mp4,.mov,.avi,.flv,.webm,.png,.jpg,.jpeg,.gif"
        drag
        class="upload-dragger"
      >
        <el-icon :size="40" color="#c0c4cc"><Upload /></el-icon>
        <div class="el-upload__text">拖拽文件到此处，或 <em>点击上传</em></div>
        <div class="el-upload__tip">支持 Word、Excel、PPT、PDF、音频、视频文件</div>
      </el-upload>
      <div class="attachment-list" v-if="attachments.length">
        <div v-for="att in attachments" :key="att.id" class="attachment-item">
          <div class="att-info">
            <el-icon :size="20" :color="getFileIconColor(att.type)"><component :is="getFileIcon(att.type)" /></el-icon>
            <div class="att-detail">
              <span class="att-name">{{ att.originalFileName }}</span>
              <span class="att-meta">{{ formatFileSize(att.fileSize) }} · {{ att.uploaderName || '未知' }} · {{ new Date(att.createdAt).toLocaleString() }}</span>
            </div>
          </div>
          <div class="att-actions">
            <el-button type="primary" link size="small" @click="previewFile(att)"><el-icon><View /></el-icon> 预览</el-button>
            <el-button type="success" link size="small" @click="downloadFile(att)"><el-icon><Download /></el-icon> 下载</el-button>
            <el-button type="danger" link size="small" @click="handleDeleteAttachment(att)"><el-icon><Delete /></el-icon></el-button>
          </div>
        </div>
      </div>
    </el-card>

    <!-- Preview Dialog -->
    <el-dialog v-model="previewVisible" :title="previewTitle" width="80%" top="5vh" destroy-on-close>
      <div class="preview-container">
        <iframe v-if="previewType === 'pdf'" :src="previewUrl" class="preview-frame" />
        <video v-else-if="previewType === 'video'" :src="previewUrl" controls class="preview-media" />
        <audio v-else-if="previewType === 'audio'" :src="previewUrl" controls class="preview-audio" />
        <img v-else-if="previewType === 'image'" :src="previewUrl" class="preview-image" />
        <iframe v-else-if="previewType === 'office'" :src="previewUrl" class="preview-frame" />
        <div v-else class="preview-fallback">
          <el-icon :size="48" color="#c0c4cc"><Document /></el-icon>
          <p>该文件类型暂不支持在线预览，请下载后查看</p>
          <el-button type="primary" @click="downloadFile(previewAttachment!)">下载文件</el-button>
        </div>
      </div>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useAuthStore } from '../stores/auth'
import {
  getSpaces, getCategoriesBySpace, getDocument, createDocument, updateDocument,
  getAttachments, uploadAttachment, deleteAttachment, getAttachmentPreviewUrl, getAttachmentDownloadUrl
} from '../api'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const docId = route.params.id ? Number(route.params.id) : null
const isNew = computed(() => !docId || route.name === 'DocumentNew')
const savedDocId = ref(isNew.value ? 0 : docId)

const spaces = ref<any[]>([])
const categories = ref<any[]>([])
const saving = ref(false)
const changeNote = ref('')
const attachments = ref<any[]>([])
const quickFile = ref<File | null>(null)

const uploadHeaders = computed(() => ({
  Authorization: `Bearer ${auth.token}`,
  'X-Database-Type': auth.databaseType || ''
}))

const form = reactive({
  Title: '',
  OriginalFileName: '',
  Content: '',
  Summary: '',
  EditMode: 0,
  SpaceId: Number(route.query.spaceId) || 0,
  CategoryId: 0,
  Tags: ''
})

const previewVisible = ref(false)
const previewTitle = ref('')
const previewUrl = ref('')
const previewType = ref('')
const previewAttachment = ref<any>(null)

function getFileIcon(type: string) {
  switch (type) {
    case 'Document': return 'Document'
    case 'Video': return 'VideoCamera'
    case 'Audio': return 'Headset'
    case 'Image': return 'Picture'
    default: return 'Files'
  }
}

function getFileIconColor(type: string) {
  switch (type) {
    case 'Document': return '#409eff'
    case 'Video': return '#e6a23c'
    case 'Audio': return '#67c23a'
    case 'Image': return '#f56c6c'
    default: return '#909399'
  }
}

function guessFileType(fileName: string): string {
  const ext = fileName.substring(fileName.lastIndexOf('.')).toLowerCase()
  if (['.doc', '.docx', '.xls', '.xlsx', '.ppt', '.pptx', '.pdf', '.md'].includes(ext)) return 'Document'
  if (['.mp4', '.mov', '.avi', '.flv', '.webm'].includes(ext)) return 'Video'
  if (['.mp3', '.m4a', '.wav', '.ogg'].includes(ext)) return 'Audio'
  if (['.png', '.jpg', '.jpeg', '.gif', '.bmp', '.webp'].includes(ext)) return 'Image'
  return 'Other'
}

function formatFileSize(bytes: number) {
  if (bytes < 1024) return bytes + ' B'
  if (bytes < 1048576) return (bytes / 1024).toFixed(1) + ' KB'
  return (bytes / 1048576).toFixed(1) + ' MB'
}

function getPreviewType(ext: string): string {
  ext = ext.toLowerCase()
  if (ext === '.pdf') return 'pdf'
  if (['.mp4', '.mov', '.avi', '.flv', '.webm'].includes(ext)) return 'video'
  if (['.mp3', '.m4a', '.wav', '.ogg'].includes(ext)) return 'audio'
  if (['.png', '.jpg', '.jpeg', '.gif', '.bmp', '.webp'].includes(ext)) return 'image'
  if (['.doc', '.docx', '.xls', '.xlsx', '.ppt', '.pptx'].includes(ext)) return 'office'
  return 'unknown'
}

// Quick file upload handler: auto-fill title from filename
function onQuickFileChange(uploadFile: any) {
  const file = uploadFile.raw as File
  if (!file) return
  if (file.size > 200 * 1024 * 1024) {
    ElMessage.error('文件大小不能超过 200MB')
    return
  }
  quickFile.value = file
  // Extract filename without extension as title
  const name = file.name
  const dotIdx = name.lastIndexOf('.')
  const titleFromFile = dotIdx > 0 ? name.substring(0, dotIdx) : name
  if (!form.Title) {
    form.Title = titleFromFile
  }
  form.OriginalFileName = name
}

function removeQuickFile() {
  quickFile.value = null
  form.OriginalFileName = ''
}

async function loadSpaces() {
  const res: any = await getSpaces()
  if (res.success) spaces.value = res.data
}

async function loadSpaceCategories() {
  if (!form.SpaceId) return
  const res: any = await getCategoriesBySpace(form.SpaceId)
  if (res.success) categories.value = res.data
}

async function loadDocument() {
  if (!docId || isNew.value) return
  const res: any = await getDocument(docId)
  if (res.success) {
    const d = res.data
    form.Title = d.title
    form.OriginalFileName = d.originalFileName || ''
    form.Content = d.content || ''
    form.Summary = d.summary || ''
    form.EditMode = d.editMode === 'Markdown' ? 1 : 0
    form.SpaceId = d.spaceId
    form.CategoryId = d.categoryId
    form.Tags = d.tags || ''
    await loadSpaceCategories()
  }
}

async function loadAttachments() {
  if (!savedDocId.value) return
  try {
    const res: any = await getAttachments(savedDocId.value)
    if (res.success) attachments.value = res.data || []
  } catch {}
}

async function handleSave() {
  if (!form.Title.trim()) { ElMessage.warning('请输入标题'); return }
  if (!form.SpaceId) { ElMessage.warning('请选择空间'); return }
  if (!form.CategoryId) { ElMessage.warning('请选择目录'); return }

  saving.value = true
  try {
    if (isNew.value) {
      const res: any = await createDocument({
        Title: form.Title,
        OriginalFileName: form.OriginalFileName || null,
        Content: form.Content,
        Summary: form.Summary,
        EditMode: form.EditMode,
        SpaceId: form.SpaceId,
        CategoryId: form.CategoryId,
        Tags: form.Tags
      })
      if (res.success) {
        const newId = res.data.id
        savedDocId.value = newId
        ElMessage.success('文档创建成功')

        // If there was a quick file, upload it as attachment
        if (quickFile.value) {
          const fd = new FormData()
          fd.append('file', quickFile.value)
          try {
            await uploadAttachment(newId, fd)
            quickFile.value = null
          } catch {}
        }

        router.replace(`/documents/${newId}/edit`)
        await loadAttachments()
      }
    } else {
      const res: any = await updateDocument(docId!, {
        Title: form.Title,
        Content: form.Content,
        Summary: form.Summary,
        Tags: form.Tags,
        ChangeNote: changeNote.value
      })
      if (res.success) { ElMessage.success('保存成功'); router.push(`/documents/${docId}`) }
    }
  } finally { saving.value = false }
}

function beforeUpload(file: File) {
  if (file.size > 200 * 1024 * 1024) { ElMessage.error('文件大小不能超过 200MB'); return false }
  return true
}

function onUploadSuccess(response: any) {
  if (response.success) { ElMessage.success('文件上传成功'); loadAttachments() }
  else ElMessage.error(response.message || '上传失败')
}

function onUploadError() { ElMessage.error('文件上传失败') }

async function handleDeleteAttachment(att: any) {
  try {
    await ElMessageBox.confirm(`确定删除附件"${att.originalFileName}"？`, '确认删除', { type: 'warning' })
    const res: any = await deleteAttachment(att.id)
    if (res.success) { ElMessage.success('附件已删除'); loadAttachments() }
  } catch {}
}

function previewFile(att: any) {
  const type = getPreviewType(att.fileExtension)
  previewTitle.value = att.originalFileName
  previewAttachment.value = att
  previewType.value = type
  const baseUrl = getAttachmentPreviewUrl(att.id)
  const tokenParam = `?token=${auth.token}`
  if (type === 'office') {
    const fullUrl = window.location.origin + baseUrl + tokenParam
    previewUrl.value = `https://view.officeapps.live.com/op/embed.aspx?src=${encodeURIComponent(fullUrl)}`
  } else {
    previewUrl.value = baseUrl + tokenParam
  }
  previewVisible.value = true
}

function downloadFile(att: any) {
  const url = getAttachmentDownloadUrl(att.id)
  const a = document.createElement('a')
  a.href = url + `?token=${auth.token}`
  a.download = att.originalFileName
  a.target = '_blank'
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
}

onMounted(async () => {
  await loadSpaces()
  if (form.SpaceId) await loadSpaceCategories()
  await loadDocument()
  if (savedDocId.value) await loadAttachments()
})
</script>

<style scoped>
.doc-edit { padding: 0; }

.quick-upload-area { text-align: center; }
.quick-upload { width: 100%; }
.quick-upload :deep(.el-upload-dragger) {
  padding: 40px 0;
  border-radius: 12px;
  border: 2px dashed #409eff;
  background: #f0f6ff;
  transition: all 0.3s;
}
.quick-upload :deep(.el-upload-dragger:hover) {
  border-color: #337ecc;
  background: #e8f1ff;
}
.el-upload__tip { font-size: 12px; color: #909399; margin-top: 4px; }

.quick-file-badge {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 16px;
  background: linear-gradient(135deg, #ecf5ff, #f0f6ff);
  border-radius: 10px;
  border: 1px solid #d9ecff;
}
.quick-file-info { flex: 1; display: flex; flex-direction: column; }
.quick-file-name { font-size: 14px; font-weight: 600; color: #303133; }
.quick-file-size { font-size: 12px; color: #909399; }

.upload-header { display: flex; align-items: center; justify-content: space-between; }
.upload-title { display: flex; align-items: center; gap: 6px; font-size: 16px; font-weight: 600; }

.upload-dragger { width: 100%; margin-bottom: 16px; }
.upload-dragger :deep(.el-upload-dragger) { padding: 30px 0; border-radius: 8px; }

.attachment-list { display: flex; flex-direction: column; gap: 8px; }
.attachment-item {
  display: flex; align-items: center; justify-content: space-between;
  padding: 10px 14px; background: #f5f7fa; border-radius: 8px; transition: background 0.2s;
}
.attachment-item:hover { background: #ecf5ff; }
.att-info { display: flex; align-items: center; gap: 12px; flex: 1; min-width: 0; }
.att-detail { display: flex; flex-direction: column; min-width: 0; }
.att-name { font-size: 14px; color: #303133; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.att-meta { font-size: 12px; color: #909399; }
.att-actions { display: flex; align-items: center; gap: 4px; flex-shrink: 0; }

.preview-container { min-height: 400px; display: flex; justify-content: center; align-items: center; }
.preview-frame { width: 100%; height: 75vh; border: none; border-radius: 8px; }
.preview-media { max-width: 100%; max-height: 75vh; border-radius: 8px; }
.preview-audio { width: 100%; }
.preview-image { max-width: 100%; max-height: 75vh; object-fit: contain; border-radius: 8px; }
.preview-fallback { text-align: center; padding: 40px; color: #909399; }
.preview-fallback p { margin: 16px 0; }
</style>
