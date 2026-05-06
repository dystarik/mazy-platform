<template>
  <div class="settings-card">
    <div class="settings-card__header">
      <span class="settings-card__title">{{ title }}</span>
      <span class="settings-card__desc">{{ description }}</span>
    </div>
    <div class="settings-card__body settings-card__body--form">
      <div v-if="mode === 'change'" class="field">
        <label>Текущий пароль</label>
        <Password
          v-model="currentPasswordModel"
          placeholder="••••••••"
          autocomplete="current-password"
          :feedback="false"
          toggle-mask
          fluid
        />
      </div>
      <div class="field">
        <label>Новый пароль</label>
        <Password
          v-model="newPasswordModel"
          placeholder="••••••••"
          autocomplete="new-password"
          :feedback="false"
          toggle-mask
          fluid
        />
      </div>
      <p v-if="success" class="settings-success">{{ successText }}</p>
      <FormErrorList :messages="errors" />
      <Button :label="loading ? 'Сохраняю...' : submitLabel" :loading="loading" @click="$emit('submit')" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import Button from 'primevue/button'
import Password from 'primevue/password'
import FormErrorList from '@/components/ui/FormErrorList.vue'

const props = defineProps<{
  mode: 'change' | 'set'
  currentPassword?: string
  newPassword: string
  errors: string[]
  loading: boolean
  success: boolean
}>()

const emit = defineEmits<{
  'update:currentPassword': [value: string]
  'update:newPassword': [value: string]
  submit: []
}>()

const currentPasswordModel = computed({
  get: () => props.currentPassword ?? '',
  set: (value: string) => emit('update:currentPassword', value),
})

const newPasswordModel = computed({
  get: () => props.newPassword,
  set: (value: string) => emit('update:newPassword', value),
})

const title = computed(() => props.mode === 'change' ? 'Смена пароля' : 'Установить пароль')
const description = computed(() =>
  props.mode === 'change'
    ? 'Введите текущий пароль и придумайте новый'
    : 'Для аккаунтов без пароля, если вы входили только через внешний провайдер',
)
const successText = computed(() => props.mode === 'change' ? 'Пароль успешно изменён' : 'Пароль успешно установлен')
const submitLabel = computed(() => props.mode === 'change' ? 'Сохранить' : 'Установить пароль')
</script>
