<script setup>
import { onMounted, ref } from 'vue';
import { http } from '../api/http';

const branches = ref([]);
const loading = ref(true);
const errorMessage = ref('');

onMounted(async () => {
  try {
    const { data } = await http.get('/api/branches');
    branches.value = data;
  } catch {
    errorMessage.value = 'ไม่สามารถโหลดรายชื่อสาขาได้ กรุณาลองใหม่';
  } finally {
    loading.value = false;
  }
});

function go(slug) {
  window.location.href = `/${slug}`;
}
</script>

<template>
  <div class="picker">
    <v-container class="picker-container" style="max-width: 480px">
      <div class="text-center mb-5">
        <v-avatar color="primary" size="48" class="mb-3">
          <v-icon icon="mdi-hospital-box" color="white" size="30" />
        </v-avatar>
        <div class="text-h6 font-weight-medium">เลือกสาขาโรงพยาบาล</div>
        <div class="text-body-2 text-medium-emphasis">Select your hospital branch</div>
      </div>

      <div v-if="loading" class="text-center py-6">
        <v-progress-circular indeterminate color="primary" />
      </div>

      <v-alert v-else-if="errorMessage" type="error" variant="tonal" density="compact">
        {{ errorMessage }}
      </v-alert>

      <v-alert v-else-if="branches.length === 0" type="warning" variant="tonal" density="compact">
        ยังไม่มีสาขาที่ตั้งค่าไว้ในระบบ
      </v-alert>

      <v-list v-else class="branch-list" elevation="2" rounded="lg">
        <v-list-item
          v-for="branch in branches"
          :key="branch.slug"
          class="branch-item"
          @click="go(branch.slug)"
        >
          <template #prepend>
            <v-icon icon="mdi-hospital-building" color="primary" />
          </template>
          <v-list-item-title>{{ branch.displayName }}</v-list-item-title>
          <template #append>
            <v-icon icon="mdi-chevron-right" />
          </template>
        </v-list-item>
      </v-list>

      <div class="text-center mt-4">
        <a href="/super-admin" class="text-caption text-medium-emphasis">จัดการสาขาทั้งหมด (Super Admin)</a>
      </div>
    </v-container>
  </div>
</template>

<style scoped>
.picker {
  min-height: 100dvh;
  display: flex;
  align-items: center;
  background: var(--n-bg);
}

.picker-container {
  width: 100%;
}

.branch-list {
  background: var(--n-card);
  border: 1px solid var(--n-border);
}

.branch-item {
  cursor: pointer;
}
</style>
