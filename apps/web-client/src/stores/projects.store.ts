import { defineStore } from 'pinia'
import { ref } from 'vue'
import { projectsApi } from '@/api'
import type { ProjectListItem, CreateProjectRequest } from '@/types/api'

export const useProjectsStore = defineStore('projects', () => {
  const projects = ref<ProjectListItem[]>([])
  const loading = ref(false)

  async function fetchProjects(): Promise<void> {
    loading.value = true
    try {
      const response = await projectsApi.list()
      projects.value = response.items ?? []
    } finally {
      loading.value = false
    }
  }

  async function createProject(data: CreateProjectRequest): Promise<string> {
    const response = await projectsApi.create(data)
    await fetchProjects()
    return response.projectId!
  }

  async function deleteProject(id: string): Promise<void> {
    await projectsApi.delete(id)
    projects.value = projects.value.filter((p) => p.projectId !== id)
  }

  return { projects, loading, fetchProjects, createProject, deleteProject }
})
