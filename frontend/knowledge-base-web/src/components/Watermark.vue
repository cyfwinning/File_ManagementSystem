<template>
  <div class="watermark-container" ref="containerRef">
    <slot />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'

const containerRef = ref<HTMLElement>()
let observer: MutationObserver | null = null

function createWatermark() {
  if (!containerRef.value) return

  const existingCanvas = containerRef.value.querySelector('.kb-watermark')
  if (existingCanvas) existingCanvas.remove()

  const canvas = document.createElement('canvas')
  canvas.width = 340
  canvas.height = 220
  const ctx = canvas.getContext('2d')!
  ctx.clearRect(0, 0, canvas.width, canvas.height)
  ctx.font = '16px Microsoft YaHei'
  ctx.fillStyle = 'rgba(0, 0, 0, 0.08)'
  ctx.textAlign = 'center'
  ctx.textBaseline = 'middle'
  ctx.translate(canvas.width / 2, canvas.height / 2)
  ctx.rotate((-25 * Math.PI) / 180)
  ctx.fillText('浙江南芯半导体，内部文件', 0, 0)

  const div = document.createElement('div')
  div.className = 'kb-watermark'
  div.style.cssText = `
    position: fixed; top: 0; left: 0; width: 100vw; height: 100vh;
    background-image: url(${canvas.toDataURL('image/png')});
    background-repeat: repeat; pointer-events: none; z-index: 99999;
    user-select: none; -webkit-user-select: none;
  `
  containerRef.value.appendChild(div)

  // Prevent removal
  observer = new MutationObserver((mutations) => {
    for (const m of mutations) {
      for (const node of m.removedNodes) {
        if ((node as HTMLElement).className === 'kb-watermark') {
          createWatermark()
          return
        }
      }
    }
  })
  observer.observe(containerRef.value, { childList: true })
}

onMounted(createWatermark)
onUnmounted(() => observer?.disconnect())
</script>

<style scoped>
.watermark-container {
  position: relative;
  min-height: 100vh;
}
</style>
