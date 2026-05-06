import { ref, type Ref } from 'vue'

interface SettingsNavItem {
  id: string
  label: string
  icon: string
}

export function useSettingsScrollSpy(
  contentRef: Ref<HTMLElement | null>,
  navItems: SettingsNavItem[],
) {
  const activeSection = ref(navItems[0]?.id ?? '')
  let scrollContainer: HTMLElement | null = null

  function getScrollContainer(): HTMLElement | null {
    return contentRef.value?.closest('.content') as HTMLElement | null
  }

  function scrollTo(id: string): void {
    const el = document.getElementById(id)
    const container = getScrollContainer()
    if (el && container) {
      const containerRect = container.getBoundingClientRect()
      const targetRect = el.getBoundingClientRect()
      const top = container.scrollTop + targetRect.top - containerRect.top - 24
      container.scrollTo({ top, behavior: 'smooth' })
    }
    activeSection.value = id
  }

  function onScroll(): void {
    const container = getScrollContainer()
    if (!container) return
    const scrollTop = container.scrollTop
    for (const item of [...navItems].reverse()) {
      const el = document.getElementById(item.id)
      if (el && el.offsetTop - 40 <= scrollTop) {
        activeSection.value = item.id
        return
      }
    }
    activeSection.value = navItems[0]?.id ?? ''
  }

  function mount(): void {
    scrollContainer = getScrollContainer()
    scrollContainer?.addEventListener('scroll', onScroll, { passive: true })
  }

  function unmount(): void {
    scrollContainer?.removeEventListener('scroll', onScroll)
    scrollContainer = null
  }

  return {
    activeSection,
    scrollTo,
    onScroll,
    mount,
    unmount,
  }
}
