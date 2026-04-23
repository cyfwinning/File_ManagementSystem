<template>
  <div class="exam-detail" v-if="exam">
    <el-page-header @back="$router.push('/exams')">
      <template #content>{{ exam.title }}</template>
      <template #extra>
        <el-tag v-if="!submitted">剩余 {{ formatTime(remainingSeconds) }}</el-tag>
      </template>
    </el-page-header>

    <el-card style="margin-top:20px" v-if="!submitted">
      <div v-for="(q, idx) in exam.questions" :key="q.id" class="question-item">
        <h4>{{ idx + 1 }}. {{ q.questionText }} ({{ q.score }}分)</h4>
        <el-tag size="small" type="info" style="margin-bottom:8px">
          {{ { SingleChoice: '单选', MultipleChoice: '多选', FillBlank: '填空' }[q.questionType as string] || q.questionType }}
        </el-tag>
        <div v-if="q.questionType !== 'FillBlank' && q.options">
          <el-radio-group v-model="answers[q.id]" v-if="q.questionType === 'SingleChoice'">
            <el-radio v-for="opt in parseOptions(q.options)" :key="opt" :value="opt" style="display:block;margin:8px 0">{{ opt }}</el-radio>
          </el-radio-group>
          <el-checkbox-group v-model="multiAnswers[q.id]" v-else>
            <el-checkbox v-for="opt in parseOptions(q.options)" :key="opt" :value="opt" style="display:block;margin:8px 0">{{ opt }}</el-checkbox>
          </el-checkbox-group>
        </div>
        <el-input v-else v-model="answers[q.id]" placeholder="请输入答案" style="max-width:400px" />
      </div>
      <el-divider />
      <el-button type="primary" size="large" @click="handleSubmit" :loading="submitting">提交答卷</el-button>
    </el-card>

    <!-- Result -->
    <el-card style="margin-top:20px" v-if="submitted && result">
      <el-result :icon="result.isPassed ? 'success' : 'error'" :title="result.isPassed ? '恭喜通过!' : '未通过'">
        <template #sub-title>
          <p>得分: {{ result.userScore }} / {{ result.totalScore }}</p>
          <p>用时: {{ Math.round(result.timeSpentSeconds / 60) }}分钟</p>
        </template>
        <template #extra>
          <el-button type="primary" @click="$router.push('/exams')">返回考试列表</el-button>
        </template>
      </el-result>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { getExamDetail, submitExam } from '../api'

const route = useRoute()
const examId = Number(route.params.id)

const exam = ref<any>(null)
const answers = ref<Record<number, string>>({})
const multiAnswers = ref<Record<number, string[]>>({})
const submitted = ref(false)
const submitting = ref(false)
const result = ref<any>(null)
const remainingSeconds = ref(0)
let timer: ReturnType<typeof setInterval> | null = null
let startTime = Date.now()

function formatTime(s: number) {
  const m = Math.floor(s / 60)
  const sec = s % 60
  return `${m}:${sec.toString().padStart(2, '0')}`
}

function parseOptions(options: string): string[] {
  try { return JSON.parse(options) } catch { return [] }
}

async function loadExam() {
  const res: any = await getExamDetail(examId)
  if (res.success) {
    exam.value = res.data
    remainingSeconds.value = exam.value.timeLimitMinutes * 60
    startTime = Date.now()
    timer = setInterval(() => {
      remainingSeconds.value--
      if (remainingSeconds.value <= 0) { handleSubmit() }
    }, 1000)
  }
}

async function handleSubmit() {
  await ElMessageBox.confirm('确定提交答卷？', '提示')
  submitting.value = true
  if (timer) clearInterval(timer)

  // Merge multi-choice answers
  const finalAnswers: Record<number, string> = { ...answers.value }
  for (const [qId, arr] of Object.entries(multiAnswers.value)) {
    finalAnswers[Number(qId)] = arr.join(',')
  }

  const timeSpent = Math.round((Date.now() - startTime) / 1000)
  try {
    const res: any = await submitExam({ ExamId: examId, Answers: finalAnswers }, timeSpent)
    if (res.success) { result.value = res.data; submitted.value = true; ElMessage.success('提交成功') }
  } finally { submitting.value = false }
}

onMounted(loadExam)
onUnmounted(() => { if (timer) clearInterval(timer) })
</script>

<style scoped>
.question-item { padding: 16px 0; border-bottom: 1px solid #f0f0f0; }
.question-item h4 { margin-bottom: 8px; font-size: 15px; }
</style>
