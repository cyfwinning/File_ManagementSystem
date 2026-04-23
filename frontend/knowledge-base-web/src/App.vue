<template>
  <Watermark>
    <router-view />
  </Watermark>
</template>

<script setup lang="ts">
import Watermark from './components/Watermark.vue'
</script>

<style>
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}
body {
  font-family: 'Microsoft YaHei', 'PingFang SC', 'Helvetica Neue', Arial, sans-serif;
  background-color: #f5f7fa;
}
</style>
