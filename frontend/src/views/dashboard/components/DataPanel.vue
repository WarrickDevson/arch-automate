<script setup>
import { computed, ref, watch } from 'vue'
import SimpleLineChart from '@/views/dashboard/components/charts/SimpleLineChart.vue'
import SimpleBarChart from '@/views/dashboard/components/charts/SimpleBarChart.vue'
import StackedBarChart from '@/views/dashboard/components/charts/StackedBarChart.vue'
import { Button } from '@/components/ui/button'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Activity,
  Bug,
  Droplets,
  FlaskConical,
  ListChecks,
  MousePointerClick,
  Shield,
  Wheat,
} from 'lucide-vue-next'
import {
  getFertiliserBreakdown,
  getHarvestingData,
  getHorticulturalActivitiesData,
  getHorticulturalActivitiesManHoursData,
  getIrrigationData,
  getMonitoringSeriesData,
  getPlantProtectionData,
} from '@/composables/useDashboardAnalytics'

const props = defineProps({
  blocks: {
    type: Array,
    default: () => [],
  },
  activeTab: {
    type: String,
    default: 'Fertilising',
  },
})

const emit = defineEmits(['monitoring-point-click', 'update:active-tab'])

const tabMeta = [
  { label: 'Fertilising', icon: FlaskConical },
  { label: 'Irrigation', icon: Droplets },
  { label: 'Harvesting', icon: Wheat },
  { label: 'Monitoring', icon: Bug },
  { label: 'Plant Protection', icon: Shield },
  { label: 'Horticultural Activities', icon: ListChecks },
]
const tabs = tabMeta.map((tab) => tab.label)
const iconByTab = Object.fromEntries(tabMeta.map((tab) => [tab.label, tab.icon]))
const irrigationMode = ref('total')
const harvestMode = ref('crop')
const plantProtectionMode = ref('total')

const resolveTab = () => (tabs.includes(props.activeTab) ? props.activeTab : tabs[0])

const monitoringTree = computed(() => getMonitoringSeriesData(props.blocks))
const fertData = computed(() => getFertiliserBreakdown(props.blocks))
const irrigationData = computed(() => getIrrigationData(props.blocks, irrigationMode.value))
const harvestingData = computed(() => getHarvestingData(props.blocks, harvestMode.value))
const plantProtectionData = computed(() =>
  getPlantProtectionData(props.blocks, plantProtectionMode.value)
)
const activitiesData = computed(() => getHorticulturalActivitiesData(props.blocks))
const activitiesHours = computed(() => getHorticulturalActivitiesManHoursData(props.blocks))

const tabHasData = computed(() => ({
  Monitoring: Object.keys(monitoringTree.value).length > 0,
  Fertilising: fertData.value.macro.length > 0 || fertData.value.micro.length > 0,
  Irrigation: irrigationData.value.length > 0,
  Harvesting: harvestingData.value.length > 0,
  'Plant Protection': plantProtectionData.value.length > 0,
  'Horticultural Activities': activitiesData.value.length > 0 || activitiesHours.value.length > 0,
}))

const visibleTabs = computed(() => tabs)
const hasAnyAnalytics = computed(() => Object.values(tabHasData.value).some(Boolean))

watch(
  () => [props.activeTab, visibleTabs.value.join('|')],
  () => {
    if (!visibleTabs.value.length) return
    if (visibleTabs.value.includes(props.activeTab)) return
    emit('update:active-tab', visibleTabs.value[0])
  },
  { immediate: true }
)

const years = computed(() => {
  const set = new Set(activitiesData.value.map((item) => item.label.split('-')[0]))
  return [...set].sort()
})

const selectedYear = ref('all')
const selectedWeek = ref('all')
const selectedActivities = ref([])
const weekOptions = ['all', 'W01', 'W02', 'W03', 'W04', 'W05', 'W06', 'W07']

const activityCatalog = computed(() => [
  'Pruning',
  'Canopy Check',
  'Weeding',
  'Drain Check',
  'Training',
])

const filteredActivitiesData = computed(() => {
  return activitiesData.value.filter((item) => {
    const [year, week] = item.label.split('-')
    const yearMatch = selectedYear.value === 'all' || selectedYear.value === year
    const weekMatch = selectedWeek.value === 'all' || selectedWeek.value === week
    return yearMatch && weekMatch
  })
})

const toggleActivity = (activity) => {
  if (selectedActivities.value.includes(activity)) {
    selectedActivities.value = selectedActivities.value.filter((item) => item !== activity)
    return
  }

  selectedActivities.value = [...selectedActivities.value, activity]
}

const toggleSelectAllActivities = () => {
  if (selectedActivities.value.length === activityCatalog.value.length) {
    selectedActivities.value = []
    return
  }
  selectedActivities.value = [...activityCatalog.value]
}

const macroStacked = computed(() => {
  const grouped = new Map()
  fertData.value.macro.forEach((item) => {
    const [product, nutrient] = item.label.split(' ')
    if (!grouped.has(product)) grouped.set(product, [])
    grouped.get(product).push({ label: nutrient, value: item.value })
  })
  return [...grouped.entries()].map(([label, segments]) => ({ label, segments }))
})

const microStacked = computed(() => {
  const grouped = new Map()
  fertData.value.micro.forEach((item) => {
    const [product, nutrient] = item.label.split(' ')
    if (!grouped.has(product)) grouped.set(product, [])
    grouped.get(product).push({ label: nutrient, value: item.value })
  })
  return [...grouped.entries()].map(([label, segments]) => ({ label, segments }))
})
</script>

<template>
  <div data-export-charts-panel class="p-4">
    <div class="mb-4 flex flex-wrap items-center justify-between gap-2 border-b border-border pb-3">
      <div class="flex flex-wrap gap-2">
        <Button
          v-for="tab in visibleTabs"
          :key="tab"
          type="button"
          size="sm"
          class="gap-1.5"
          :variant="resolveTab() === tab ? 'default' : 'ghost'"
          @click="emit('update:active-tab', tab)"
        >
          <component :is="iconByTab[tab]" class="h-3.5 w-3.5" />
          {{ tab }}
        </Button>
      </div>
    </div>

    <section v-if="!hasAnyAnalytics" class="rounded-xl border border-border bg-bg/70 p-4 text-sm">
      <p class="font-semibold text-text">No analytics data for the current date range.</p>
      <p class="mt-1">Expand your date range or adjust filters to populate charts in this panel.</p>
    </section>

    <div v-else-if="resolveTab() === 'Monitoring'" class="space-y-3">
      <details
        v-for="(targetGroup, targetType) in monitoringTree"
        :key="targetType"
        class="rounded-2xl border border-border bg-bg/70 p-3"
        open
      >
        <summary class="cursor-pointer text-sm font-semibold text-text">{{ targetType }}</summary>
        <div class="mt-3 space-y-4">
          <details
            v-for="(stageGroup, targetName) in targetGroup"
            :key="targetName"
            class="rounded-xl border border-border bg-surface p-3"
            open
          >
            <summary class="cursor-pointer text-sm font-semibold text-text">
              {{ targetName }}
            </summary>
            <div class="mt-3 space-y-5">
              <div v-for="(series, stageName) in stageGroup" :key="stageName" class="space-y-2">
                <div class="flex items-center justify-between">
                  <h4 class="text-sm font-semibold text-text">{{ stageName }}</h4>
                  <span class="text-xs">{{ series.length }} points</span>
                </div>
                <SimpleLineChart :points="series" color="#0E4553" />
                <div class="flex flex-wrap gap-2">
                  <Button
                    v-for="point in series"
                    :key="`${point.x}-${point.blockName}-${point.plantingName}`"
                    type="button"
                    variant="outline"
                    size="sm"
                    class="text-xs gap-1.5"
                    @click="emit('monitoring-point-click', point)"
                  >
                    <MousePointerClick class="h-3 w-3" />
                    {{ point.x }} • {{ point.blockName }}
                  </Button>
                </div>
              </div>
            </div>
          </details>
        </div>
      </details>
    </div>

    <div v-else-if="resolveTab() === 'Fertilising'" class="grid gap-4 lg:grid-cols-2">
      <article class="space-y-2">
        <h4 class="text-sm font-semibold text-text">Macro Nutrients (kg/ha)</h4>
        <StackedBarChart :data="macroStacked" />
      </article>
      <article class="space-y-2">
        <h4 class="text-sm font-semibold text-text">Micro Nutrients (kg/ha)</h4>
        <StackedBarChart :data="microStacked" />
      </article>
    </div>

    <div v-else-if="resolveTab() === 'Irrigation'" class="space-y-3">
      <div class="flex flex-wrap gap-2">
        <button
          v-for="mode in ['total', 'block', 'planting']"
          :key="mode"
          type="button"
          class="rounded-lg border px-3 py-1.5 text-xs font-semibold uppercase"
          :class="irrigationMode === mode ? 'border-accent text-accent' : 'border-border '"
          @click="irrigationMode = mode"
        >
          {{ mode }}
        </button>
      </div>
      <SimpleBarChart :data="irrigationData" color="#0E4553" />
    </div>

    <div v-else-if="resolveTab() === 'Harvesting'" class="space-y-3">
      <div class="flex flex-wrap gap-2">
        <Button
          v-for="mode in ['crop', 'block', 'planting']"
          :key="mode"
          type="button"
          size="sm"
          :variant="harvestMode === mode ? 'default' : 'outline'"
          class="uppercase"
          @click="harvestMode = mode"
        >
          {{ mode }}
        </Button>
      </div>
      <SimpleBarChart :data="harvestingData" color="#DCE557" />
    </div>

    <div v-else-if="resolveTab() === 'Plant Protection'" class="space-y-3">
      <div class="flex flex-wrap gap-2">
        <Button
          v-for="mode in ['total', 'block', 'planting']"
          :key="mode"
          type="button"
          size="sm"
          :variant="plantProtectionMode === mode ? 'default' : 'outline'"
          class="uppercase"
          @click="plantProtectionMode = mode"
        >
          {{ mode }}
        </Button>
      </div>
      <SimpleBarChart :data="plantProtectionData" color="#A855F7" />
    </div>

    <div v-else class="space-y-4">
      <div class="grid gap-3 rounded-2xl border border-border bg-bg/70 p-3 md:grid-cols-3">
        <div class="space-y-1">
          <p class="text-xs">Year</p>
          <Select v-model="selectedYear">
            <SelectTrigger class="h-10 w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All</SelectItem>
              <SelectItem v-for="year in years" :key="year" :value="year">{{ year }}</SelectItem>
            </SelectContent>
          </Select>
        </div>

        <div class="space-y-1">
          <p class="text-xs">Week</p>
          <Select v-model="selectedWeek">
            <SelectTrigger class="h-10 w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem v-for="week in weekOptions" :key="week" :value="week">{{
                week
              }}</SelectItem>
            </SelectContent>
          </Select>
        </div>

        <div class="space-y-1">
          <p class="text-xs">Activities</p>
          <Button
            type="button"
            variant="outline"
            class="mt-0.5 h-10 w-full justify-start gap-2 text-sm"
            @click="toggleSelectAllActivities"
          >
            <Activity class="h-4 w-4" />
            {{ selectedActivities.length === activityCatalog.length ? 'Clear All' : 'Select All' }}
          </Button>
        </div>
      </div>

      <div class="flex flex-wrap gap-2">
        <Button
          v-for="activity in activityCatalog"
          :key="activity"
          type="button"
          size="sm"
          :variant="selectedActivities.includes(activity) ? 'default' : 'outline'"
          class="rounded-full"
          @click="toggleActivity(activity)"
        >
          {{ activity }}
        </Button>
      </div>

      <div class="grid gap-4 lg:grid-cols-2">
        <article>
          <h4 class="mb-2 text-sm font-semibold">Activity Count By Week</h4>
          <SimpleBarChart :data="filteredActivitiesData" color="#0E4553" />
        </article>
        <article>
          <h4 class="mb-2 text-sm font-semibold">Man-Hours By Week</h4>
          <SimpleBarChart :data="activitiesHours" color="#3B82F6" />
        </article>
      </div>
    </div>
  </div>
</template>
