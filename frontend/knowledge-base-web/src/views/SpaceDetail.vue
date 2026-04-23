<template>
  <div class="space-detail">
    <el-page-header @back="$router.push('/spaces')">
      <template #content>{{ space?.name || '知识空间' }}</template>
      <template #extra>
        <el-button type="primary" @click="$router.push('/documents/new?spaceId=' + spaceId)"><el-icon><Plus /></el-icon>新建文档</el-button>
      </template>
    </el-page-header>

    <el-row :gutter="20" style="margin-top:20px">
      <!-- Category Tree -->
      <el-col :span="5">
        <el-card>
          <template #header>
            <div style="display:flex;justify-content:space-between;align-items:center">
              <span>目录</span>
              <el-button size="small" type="primary" text @click="openCreateCategoryDialog(null)"><el-icon><Plus /></el-icon></el-button>
            </div>
          </template>
          <el-tree
            :data="categories"
            :props="{ label: 'name', children: 'children' }"
            node-key="id"
            highlight-current
            default-expand-all
            @node-click="handleCategoryClick"
          >
            <template #default="{ node, data }">
              <span class="tree-node">
                <span class="tree-node-left">
                  <el-icon><Folder /></el-icon>
                  <span>{{ node.label }}</span>
                  <el-tag size="small" type="info">{{ data.documentCount }}</el-tag>
                </span>
                <span class="tree-node-actions">
                  <el-button text type="primary" size="small" @click.stop="openCreateCategoryDialog(data)" title="新增子目录">
                    <el-icon><Plus /></el-icon>
                  </el-button>
                  <el-button text type="warning" size="small" @click.stop="openEditCategoryDialog(data)" title="编辑">
                    <el-icon><Edit /></el-icon>
                  </el-button>
                  <el-button text type="danger" size="small" @click.stop="handleDeleteCategory(data.id)" title="删除">
                    <el-icon><Delete /></el-icon>
                  </el-button>
                </span>
              </span>
            </template>
          </el-tree>
        </el-card>
      </el-col>

      <!-- Document Cards -->
      <el-col :span="19">
        <el-card>
          <template #header>
            <div class="list-header">
              <span>文档列表 ({{ total }})</span>
              <el-input v-model="keyword" placeholder="搜索文档..." style="width:240px" clearable @change="loadDocuments">
                <template #prefix><el-icon><Search /></el-icon></template>
              </el-input>
            </div>
          </template>

          <div class="doc-cards" v-if="documents.length">
            <div
              v-for="doc in documents"
              :key="doc.id"
              class="doc-card"
              @click="$router.push(`/documents/${doc.id}`)"
            >
              <div class="card-cover">
                <img v-if="doc.coverUrl" :src="doc.coverUrl" class="cover-img" />
                <div v-else class="cover-placeholder" :class="getDocCoverClass(doc)">
                  <el-icon :size="40"><component :is="getDocCoverIcon(doc)" /></el-icon>
                  <span class="cover-ext">{{ getDocExtLabel(doc) }}</span>
                </div>
                <el-tag v-if="doc.isPinned" type="warning" size="small" class="pin-badge">
                  <el-icon><Top /></el-icon> 置顶
                </el-tag>
              </div>
              <div class="card-body">
                <div class="card-title" :title="doc.title">{{ doc.title }}</div>
                <div class="card-meta">
                  <span v-if="doc.originalFileName" class="card-filename" :title="doc.originalFileName">
                    <el-icon><Document /></el-icon> {{ doc.originalFileName }}
                  </span>
                  <span class="card-info">
                    {{ doc.authorName || '未知' }} · <el-icon><View /></el-icon>{{ doc.viewCount }}
                  </span>
                </div>
              </div>
            </div>
          </div>

          <el-empty v-else description="暂无文档" />

          <el-pagination
            v-if="total > pageSize"
            :current-page="page"
            :page-size="pageSize"
            :total="total"
            layout="total, prev, pager, next"
            style="margin-top:16px;justify-content:flex-end"
            @current-change="(p: number) => { page = p; loadDocuments() }"
          />
        </el-card>
      </el-col>
    </el-row>

    <!-- Create Category Dialog -->
    <el-dialog v-model="showCreateCategoryDialog" :title="createCategoryParent ? `新增「${createCategoryParent.name}」的子目录` : '新建目录'" width="400">
      <el-form :model="createCategoryForm" label-width="80px">
        <el-form-item label="名称"><el-input v-model="createCategoryForm.Name" placeholder="请输入目录名称" /></el-form-item>
        <el-form-item label="父目录">
          <el-tree-select v-model="createCategoryForm.ParentId" :data="categories" :props="{ label: 'name', value: 'id', children: 'children' }" clearable placeholder="无(根目录)" check-strictly style="width:100%" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showCreateCategoryDialog = false">取消</el-button>
        <el-button type="primary" @click="handleCreateCategory">创建</el-button>
      </template>
    </el-dialog>

    <!-- Edit Category Dialog -->
    <el-dialog v-model="showEditCategoryDialog" title="编辑目录" width="400">
      <el-form :model="editCategoryForm" label-width="80px">
        <el-form-item label="名称">
          <el-input v-model="editCategoryForm.Name" placeholder="目录名称" :disabled="!editCategoryCanRename" />
          <div v-if="!editCategoryCanRename" class="rename-hint">该目录下有子目录或文件，不允许修改名称</div>
        </el-form-item>
        <el-form-item label="描述"><el-input v-model="editCategoryForm.Description" placeholder="可选" /></el-form-item>
        <el-form-item label="排序"><el-input-number v-model="editCategoryForm.SortOrder" :min="0" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showEditCategoryDialog = false">取消</el-button>
        <el-button type="primary" @click="handleUpdateCategory">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { getSpaces, getCategoriesBySpace, getDocuments, createCategory, updateCategory, deleteCategory } from '../api'

const route = useRoute()
const spaceId = Number(route.params.id)

const space = ref<any>(null)
const categories = ref<any[]>([])
const documents = ref<any[]>([])
const keyword = ref('')
const page = ref(1)
const pageSize = 20
const total = ref(0)
const selectedCategoryId = ref<number | null>(null)

// Create category
const showCreateCategoryDialog = ref(false)
const createCategoryParent = ref<any>(null)
const createCategoryForm = reactive({ Name: '', ParentId: null as number | null })

// Edit category
const showEditCategoryDialog = ref(false)
const editCategoryId = ref<number>(0)
const editCategoryCanRename = ref(true)
const editCategoryForm = reactive({ Name: '', Description: '', SortOrder: 0 })

function getDocExtLabel(doc: any): string {
  if (!doc.originalFileName) return 'DOC'
  const ext = doc.originalFileName.substring(doc.originalFileName.lastIndexOf('.')).toUpperCase()
  return ext.replace('.', '') || 'DOC'
}

function getDocCoverIcon(doc: any): string {
  if (!doc.originalFileName) return 'Document'
  const ext = doc.originalFileName.substring(doc.originalFileName.lastIndexOf('.')).toLowerCase()
  if (['.mp4', '.mov', '.avi', '.flv', '.webm'].includes(ext)) return 'VideoCamera'
  if (['.mp3', '.m4a', '.wav', '.ogg'].includes(ext)) return 'Headset'
  if (['.png', '.jpg', '.jpeg', '.gif', '.bmp', '.webp'].includes(ext)) return 'Picture'
  if (['.ppt', '.pptx'].includes(ext)) return 'DataBoard'
  if (['.xls', '.xlsx'].includes(ext)) return 'Grid'
  if (['.pdf'].includes(ext)) return 'Reading'
  return 'Document'
}

function getDocCoverClass(doc: any): string {
  if (!doc.originalFileName) return 'cover-doc'
  const ext = doc.originalFileName.substring(doc.originalFileName.lastIndexOf('.')).toLowerCase()
  if (['.mp4', '.mov', '.avi', '.flv', '.webm'].includes(ext)) return 'cover-video'
  if (['.mp3', '.m4a', '.wav', '.ogg'].includes(ext)) return 'cover-audio'
  if (['.png', '.jpg', '.jpeg', '.gif', '.bmp', '.webp'].includes(ext)) return 'cover-image'
  if (['.ppt', '.pptx'].includes(ext)) return 'cover-ppt'
  if (['.xls', '.xlsx'].includes(ext)) return 'cover-excel'
  if (['.pdf'].includes(ext)) return 'cover-pdf'
  if (['.doc', '.docx'].includes(ext)) return 'cover-word'
  return 'cover-doc'
}

async function loadSpace() {
  const res: any = await getSpaces()
  if (res.success) space.value = res.data.find((s: any) => s.id === spaceId)
}

async function loadCategories() {
  const res: any = await getCategoriesBySpace(spaceId)
  if (res.success) categories.value = res.data
}

async function loadDocuments() {
  const res: any = await getDocuments({ spaceId, categoryId: selectedCategoryId.value, keyword: keyword.value, page: page.value, pageSize })
  if (res.success) { documents.value = res.data.items; total.value = res.data.totalCount }
}

function handleCategoryClick(data: any) {
  selectedCategoryId.value = data.id
  page.value = 1
  loadDocuments()
}

// --- Create Category ---
function openCreateCategoryDialog(parent: any) {
  createCategoryParent.value = parent
  createCategoryForm.Name = ''
  createCategoryForm.ParentId = parent ? parent.id : null
  showCreateCategoryDialog.value = true
}

async function handleCreateCategory() {
  if (!createCategoryForm.Name.trim()) {
    ElMessage.warning('请输入目录名称')
    return
  }
  const res: any = await createCategory({ ...createCategoryForm, SpaceId: spaceId })
  if (res.success) {
    ElMessage.success('创建成功')
    showCreateCategoryDialog.value = false
    createCategoryForm.Name = ''
    createCategoryForm.ParentId = null
    await loadCategories()
  }
}

// --- Edit Category ---
function openEditCategoryDialog(data: any) {
  editCategoryId.value = data.id
  editCategoryForm.Name = data.name
  editCategoryForm.Description = data.description || ''
  editCategoryForm.SortOrder = data.sortOrder || 0
  // If category has children or documents, it cannot be renamed
  const hasChildren = data.children && data.children.length > 0
  const hasDocuments = data.documentCount > 0
  editCategoryCanRename.value = !hasChildren && !hasDocuments
  showEditCategoryDialog.value = true
}

async function handleUpdateCategory() {
  if (!editCategoryForm.Name.trim()) {
    ElMessage.warning('请输入目录名称')
    return
  }
  const res: any = await updateCategory(editCategoryId.value, editCategoryForm)
  if (res.success) {
    ElMessage.success('更新成功')
    showEditCategoryDialog.value = false
    await loadCategories()
  }
}

// --- Delete Category ---
async function handleDeleteCategory(id: number) {
  await ElMessageBox.confirm('确定删除此目录？', '警告', { type: 'warning' })
  const res: any = await deleteCategory(id)
  if (res.success) {
    ElMessage.success('删除成功')
    if (selectedCategoryId.value === id) {
      selectedCategoryId.value = null
      page.value = 1
      loadDocuments()
    }
    await loadCategories()
  }
}

onMounted(() => { loadSpace(); loadCategories(); loadDocuments() })
</script>

<style scoped>
.tree-node { display: flex; align-items: center; gap: 4px; font-size: 14px; flex: 1; justify-content: space-between; }
.tree-node-left { display: flex; align-items: center; gap: 4px; }
.tree-node-actions { display: flex; gap: 0; }
.list-header { display: flex; justify-content: space-between; align-items: center; }
.rename-hint { font-size: 12px; color: #e6a23c; margin-top: 4px; line-height: 1.3; }

.doc-cards {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  gap: 16px;
}

.doc-card {
  border-radius: 10px;
  overflow: hidden;
  background: #fff;
  border: 1px solid #ebeef5;
  cursor: pointer;
  transition: all 0.25s ease;
}
.doc-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);
  border-color: #409eff;
}

.card-cover {
  position: relative;
  width: 100%;
  height: 140px;
  overflow: hidden;
}

.cover-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.cover-placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 6px;
  color: #fff;
}
.cover-ext {
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 1px;
  opacity: 0.9;
}

.cover-doc   { background: linear-gradient(135deg, #409eff, #79bbff); }
.cover-word  { background: linear-gradient(135deg, #2b5797, #4a80c4); }
.cover-pdf   { background: linear-gradient(135deg, #d32f2f, #ef5350); }
.cover-excel { background: linear-gradient(135deg, #217346, #33a067); }
.cover-ppt   { background: linear-gradient(135deg, #d04423, #e8734a); }
.cover-video { background: linear-gradient(135deg, #e6a23c, #f0c060); }
.cover-audio { background: linear-gradient(135deg, #67c23a, #95d475); }
.cover-image { background: linear-gradient(135deg, #f56c6c, #f89898); }

.pin-badge {
  position: absolute;
  top: 8px;
  right: 8px;
}

.card-body {
  padding: 10px 12px 12px;
}

.card-title {
  font-size: 14px;
  font-weight: 600;
  color: #303133;
  line-height: 1.4;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  text-overflow: ellipsis;
  min-height: 39px;
}

.card-meta {
  margin-top: 6px;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.card-filename {
  font-size: 11px;
  color: #909399;
  display: flex;
  align-items: center;
  gap: 3px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.card-info {
  font-size: 11px;
  color: #c0c4cc;
  display: flex;
  align-items: center;
  gap: 3px;
}
</style>
