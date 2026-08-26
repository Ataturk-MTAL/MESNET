<template>
  <q-page padding>
    <PageHeader title="İşletme Dağıtımı" />

    <!-- Klavye akışının durum duyurusu (#88). Görsel olarak gizli, ekran okuyucuya açık. -->
    <div
      class="sr-only"
      role="status"
      aria-live="polite"
    >
      {{ kbAnnouncement }}
    </div>

    <!-- Filtreler -->
    <div class="row q-col-gutter-md q-mb-lg items-end">
      <div class="col-12 col-sm-3">
        <!-- Yazma bağlamı (#126): atama/saat değişikliği yapılan sayfa — yetkisiz alan listelenmez -->
        <BranchSelector
          v-model="branchFilter"
          write-context
          @update:model-value="onBranchChange"
        />
      </div>

      <div class="col-12 col-sm-3">
        <TeacherSelector
          v-model="selectedTeacherId"
          :branch-code="branchFilter"
          :show-cross-branch="authStore.canManageAllBranches && !!branchFilter"
          @update:model-value="onTeacherChange"
        />
      </div>
      <div class="col-12 col-sm-auto q-gutter-sm">
        <q-btn
          unelevated
          color="primary"
          icon="save"
          label="Kaydet"
          :loading="saving"
          :disable="pendingChanges.length === 0 || periodStore.isReadOnly"
          @click="saveAll"
        >
          <q-badge
            v-if="pendingChanges.length > 0"
            color="negative"
            floating
            class="tabular-nums"
          >
            {{ pendingChanges.length }}
          </q-badge>
        </q-btn>
      </div>
    </div>

    <!-- Bilgi Mesajı -->
    <AppNotice
      v-if="!branchFilter"
      type="info"
      message="İşletme dağıtımı yapmak için önce bir alan seçin."
      class="q-mb-md"
    />

    <AppNotice
      v-if="scheduleConfigMissing"
      type="warning"
      message="Kurum için günlük ders sayısı ayarlanmamış. Lütfen önce Kurum sayfasından ders programı ayarını yapın."
      class="q-mb-md"
    />

    <!-- Havuz hesaplanmamışken dağıtım bir üst sınırla kıyaslanamaz (#111):
      "aşım" demek yanlış, sessiz kalmak da yanlış — eksik yapılandırma uyarısı. -->
    <AppNotice
      v-if="branchFilter && !loading && poolUndefined"
      type="warning"
      class="q-mb-md"
    >
      <div class="row items-center q-col-gutter-sm">
        <div class="col">
          {{ WORKLOAD_POOL_MISSING_MESSAGE }}
          <span v-if="summary.totalAssignedHours > 0">
            Şu an dağıtılan toplam: <strong>{{ summary.totalAssignedHours }}</strong> saat.
          </span>
        </div>
        <div class="col-auto">
          <q-btn
            flat
            dense
            no-caps
            color="warning"
            icon-right="arrow_forward"
            label="Ders Yükü Havuzuna Git"
            :to="{ name: 'WorkloadConfig' }"
          />
        </div>
      </div>
    </AppNotice>

    <!-- Read-only Uyarı -->
    <AppNotice
      v-if="periodStore.isReadOnly"
      type="readonly"
      message="Kapalı dönem — yalnızca görüntüleme modu."
      class="q-mb-md"
    />

    <!-- Tab Yapısı -->
    <div v-if="branchFilter">
      <q-tabs
        v-model="activeTab"
        align="left"
        class="text-primary q-mb-md"
        active-color="primary"
        indicator-color="primary"
        outside-arrows
        mobile-arrows
      >
        <q-tab
          name="assignment"
          icon="drag_indicator"
          label="İşletme Dağıtımı"
        />
        <q-tab
          name="teachers"
          icon="people"
          label="Öğretmen Özeti"
        />
      </q-tabs>

      <q-separator class="q-mb-md" />

      <q-tab-panels
        v-model="activeTab"
        animated
      >
        <!-- ── Tab: İşletme Dağıtımı ── -->
        <q-tab-panel
          name="assignment"
          class="q-pa-none"
        >
          <!-- Ana İçerik: 2 Sütun Layout -->
          <div class="row q-col-gutter-md">
            <!-- Sol Panel: Atanmamış İşletmeler -->
            <div class="col-12 col-md-3">
              <q-card
                flat
                bordered
                class="sticky-panel"
              >
                <q-card-section class="q-pb-none">
                  <div class="text-subtitle1 text-weight-medium q-mb-sm">
                    Atanmamış İşletmeler
                    <q-badge
                      color="warning"
                      class="q-ml-sm"
                    >
                      {{ unassignedBusinesses.length }}
                    </q-badge>
                  </div>
                  <SearchInput
                    v-model="businessSearch"
                    placeholder="İşletme ara..."
                    icon-size="xs"
                    class="q-mb-sm"
                  />
                </q-card-section>

                <q-card-section class="q-pt-sm business-list-container">
                  <DataState
                    :loading="loading"
                    :empty="filteredUnassigned.length === 0"
                    empty-icon="check_circle"
                    empty-text="Tüm işletmeler atanmış"
                    padding="q-pa-md"
                  >
                    <div class="business-card-list">
                      <!-- Fare için sürükle-bırak, klavye için seç-taşı akışı (#88):
                        Enter/Space seçer, ızgarada boş hücrede Enter bırakır, Escape iptal. -->
                      <div
                        v-for="biz in filteredUnassigned"
                        :key="biz.businessId"
                        class="business-card"
                        :class="{
                          'business-card--disabled': periodStore.isReadOnly,
                          'business-card--selected': kbSelected?.businessId === biz.businessId
                            && kbSelected?.fromDay === undefined,
                        }"
                        role="button"
                        tabindex="0"
                        :aria-disabled="periodStore.isReadOnly"
                        :aria-pressed="kbSelected?.businessId === biz.businessId && kbSelected?.fromDay === undefined"
                        :aria-label="`${biz.businessName}. Ders programına atamak için Enter'a basın.`"
                        :draggable="!periodStore.isReadOnly"
                        @dragstart="onBusinessDragStart($event, biz)"
                        @keydown.enter.prevent="onCardKey(biz)"
                        @keydown.space.prevent="onCardKey(biz)"
                        @keydown.esc.prevent="kbCancel()"
                      >
                        <div class="row items-center no-wrap">
                          <q-icon
                            name="business"
                            size="18px"
                            color="warning"
                            class="q-mr-sm"
                          />
                          <div class="col">
                            <div class="text-body2 text-weight-medium ellipsis">
                              {{ biz.businessName }}
                            </div>
                            <!-- Zemin beyaz DEĞİL: .business-card = color-mix(in srgb, var(--q-warning) 10%, #fff)
                                 = #f5f0e6. ÖLÇÜLDÜ o zeminde — grey-6 2,36:1, grey-7 4,06:1 (ikisi de eşiğin
                                 altında), text-warning-strong (#7e5800) 5,63:1; hover zemininde (#ede4d1) 5,06:1.
                                 Renkli yüzeyde ikincil metin o tondan türetilir, griye düşürülmez. -->
                            <div class="text-caption text-warning-strong">
                              {{ biz.district ?? '—' }}
                              <span v-if="biz.distanceToSchoolKm != null"> · {{ biz.distanceToSchoolKm.toFixed(1) }} km</span>
                            </div>
                          </div>
                        </div>
                        <div class="row q-mt-xs q-gutter-xs">
                          <q-badge
                            :color="slotProgress(biz).current > 0 ? 'warning' : 'positive'"
                            :label="slotProgressLabel(biz)"
                            dense
                          />
                          <q-badge
                            color="info"
                            :label="`${biz.activeStudentCount} öğrenci`"
                            dense
                          />
                          <!-- Fahri ziyaret: slot işgal eder, ek ders saatine sayılmaz (#115) -->
                          <q-badge
                            v-if="biz.isHonoraryVisit"
                            color="neutral-soft"
                            text-color="neutral-strong"
                            :label="HONORARY_LABEL"
                            dense
                          >
                            <q-tooltip>{{ HONORARY_HINT }}</q-tooltip>
                          </q-badge>
                        </div>
                      </div>
                    </div>
                  </DataState>
                </q-card-section>
              </q-card>
            </div>

            <!-- Sağ Panel: Grid + Özet -->
            <div class="col-12 col-md-9">
              <!-- Özet Kartları -->
              <div class="row q-col-gutter-sm q-mb-md">
                <div class="col-6 col-sm-3">
                  <q-card
                    flat
                    bordered
                  >
                    <q-card-section class="text-center q-pa-sm">
                      <div class="text-caption text-grey-7">
                        Ders Yükü Havuzu
                      </div>
                      <div
                        class="text-h5 stat-value"
                        :class="workloadPoolToneClass(summary.totalWorkloadPool)"
                      >
                        {{ workloadPoolLabel(summary.totalWorkloadPool) }}
                      </div>
                    </q-card-section>
                  </q-card>
                </div>
                <div class="col-6 col-sm-3">
                  <q-card
                    flat
                    bordered
                  >
                    <q-card-section class="text-center q-pa-sm">
                      <div class="text-caption text-grey-7">
                        Dağıtılan Saat
                      </div>
                      <div
                        class="text-h5 stat-value"
                        :class="assignedHoursToneClass(summary.totalWorkloadPool, summary.totalAssignedHours)"
                      >
                        {{ summary.totalAssignedHours }}
                      </div>
                      <!-- Fahri ziyaretler havuza girmez; sayıyı sessiz bırakmak "eksik
                        dağıtım" izlenimi verirdi (#115). -->
                      <div
                        v-if="summary.honoraryBusinessCount > 0"
                        class="text-caption text-neutral-strong"
                      >
                        + {{ summary.honoraryBusinessCount }} fahri (havuz dışı)
                        <q-tooltip>{{ HONORARY_HINT }}</q-tooltip>
                      </div>
                    </q-card-section>
                  </q-card>
                </div>
                <div class="col-6 col-sm-3">
                  <q-card
                    flat
                    bordered
                  >
                    <q-card-section class="text-center q-pa-sm">
                      <div class="text-caption text-grey-7">
                        Kalan Saat
                      </div>
                      <div
                        class="text-h5 stat-value"
                        :class="remainingHoursToneClass(summary.totalWorkloadPool, summary.remainingHours)"
                      >
                        {{ remainingHoursLabel(summary.totalWorkloadPool, summary.remainingHours) }}
                      </div>
                    </q-card-section>
                  </q-card>
                </div>
                <div class="col-6 col-sm-3">
                  <q-card
                    flat
                    bordered
                  >
                    <q-card-section class="text-center q-pa-sm">
                      <div class="text-caption text-grey-7">
                        Atanmış / Toplam
                      </div>
                      <div class="text-h5 stat-value text-secondary-strong">
                        {{ summary.assignedBusinessCount }} / {{ summary.assignedBusinessCount + summary.unassignedBusinessCount }}
                      </div>
                    </q-card-section>
                  </q-card>
                </div>
              </div>

              <!-- Öğretmen Ders Programı Grid -->
              <q-card
                v-if="selectedTeacherId && periodCount > 0"
                flat
                bordered
                class="q-mb-md"
              >
                <q-card-section>
                  <div class="text-subtitle1 text-weight-medium q-mb-sm">
                    {{ selectedTeacherName }} — Ders Programı
                  </div>

                  <div
                    v-if="scheduleLoading"
                    class="text-center q-pa-lg"
                  >
                    <q-spinner
                      color="primary"
                      size="2em"
                    />
                    <div class="text-caption text-grey-7 q-mt-sm">
                      Program yükleniyor...
                    </div>
                  </div>

                  <AssignmentGrid
                    v-else
                    :schedule="effectiveSchedule"
                    :period-count="periodCount"
                    :disabled="periodStore.isReadOnly"
                    :business-name-map="businessNameMap"
                    :selected="kbSelected"
                    @business-dropped="onKeyboardAwareDrop"
                    @business-removed="onBusinessRemoved"
                    @business-selected="kbSelect"
                    @selection-cancelled="kbCancel"
                  />
                </q-card-section>
              </q-card>

              <!-- Öğretmen seçilmemiş -->
              <AppNotice
                v-else-if="!selectedTeacherId"
                type="info"
                icon="person_search"
                message="Grid üzerinde atama yapmak için bir öğretmen seçin. Soldan işletme kartını sürükleyip grid üzerindeki boş saate bırakın."
                class="q-mb-md"
              />

              <!-- Atanmış İşletmeler Listesi (seçili öğretmen için) -->
              <q-card
                v-if="selectedTeacherId && assignedToTeacher.length > 0"
                flat
                bordered
                class="q-mb-md"
              >
                <q-card-section>
                  <div class="text-subtitle1 text-weight-medium q-mb-sm">
                    Atanmış İşletmeler
                    <q-badge
                      color="info"
                      class="q-ml-sm"
                    >
                      {{ assignedToTeacher.length }}
                    </q-badge>
                  </div>
                  <q-list
                    dense
                    separator
                  >
                    <q-item
                      v-for="biz in assignedToTeacher"
                      :key="biz.businessId"
                    >
                      <q-item-section avatar>
                        <q-icon
                          name="business"
                          color="info"
                        />
                      </q-item-section>
                      <q-item-section>
                        <q-item-label>
                          {{ biz.businessName }}
                          <q-badge
                            v-if="biz.isHonoraryVisit"
                            color="neutral-soft"
                            text-color="neutral-strong"
                            class="q-ml-xs"
                            :label="HONORARY_LABEL"
                          >
                            <q-tooltip>{{ HONORARY_HINT }}</q-tooltip>
                          </q-badge>
                        </q-item-label>
                        <q-item-label caption>
                          {{ dayLabel(biz.assignedDay) }}
                          <span v-if="biz.assignedPeriodNumber"> · {{ biz.assignedPeriodNumber }}. saat</span>
                          <!-- Fahri satırda "0 saat" yazmak yanıltıcı: ziyaret var, ücret yok -->
                          <span v-if="biz.isHonoraryVisit"> · ek ders saatine sayılmaz</span>
                          <span v-else> · {{ biz.assignedHours }} saat</span>
                        </q-item-label>
                      </q-item-section>
                      <q-item-section side>
                        <div class="row no-wrap q-gutter-xs">
                          <q-btn
                            flat
                            round
                            dense
                            icon="history"
                            color="grey-7"
                            size="sm"
                            aria-label="Atama geçmişini göster"
                            @click="showHistory(biz.businessId, biz.businessName, biz.branchCode, periodStore.selectedPeriodId ?? '')"
                          >
                            <q-tooltip>Atama geçmişi</q-tooltip>
                          </q-btn>
                          <q-btn
                            v-if="!periodStore.isReadOnly"
                            flat
                            round
                            dense
                            icon="close"
                            color="negative"
                            size="sm"
                            aria-label="Atamayı kaldır"
                            @click="removeAssignment(biz)"
                          >
                            <q-tooltip>Atamayı kaldır</q-tooltip>
                          </q-btn>
                        </div>
                      </q-item-section>
                    </q-item>
                  </q-list>
                </q-card-section>
              </q-card>

              <!-- Öğretmen Özet Tablosu -->
              <q-card
                flat
                bordered
              >
                <q-expansion-item
                  icon="people"
                  label="Öğretmen Özeti"
                  header-class="text-subtitle1 text-weight-medium"
                  default-opened
                >
                  <q-card-section class="q-pt-none">
                    <q-table
                      :rows="summary.teacherWorkloads"
                      :columns="teacherSummaryColumns"
                      row-key="teacherId"
                      flat
                      dense
                      hide-pagination
                      :rows-per-page-options="[0]"
                    >
                      <template #body-cell-teacherName="{ row }">
                        <q-td>
                          <q-btn
                            flat
                            dense
                            no-caps
                            color="primary"
                            class="q-px-none"
                            :label="row.teacherName"
                            :aria-label="`${row.teacherName} öğretmenini seç`"
                            @click="selectTeacher(row.teacherId)"
                          />
                        </q-td>
                      </template>
                      <template #body-cell-honoraryVisitCount="{ row }">
                        <q-td class="text-center">
                          <span
                            v-if="row.honoraryVisitCount > 0"
                            class="text-neutral-strong"
                          >
                            {{ row.honoraryVisitCount }}
                            <q-tooltip>{{ HONORARY_HINT }}</q-tooltip>
                          </span>
                          <span
                            v-else
                            class="text-grey-7"
                          >—</span>
                        </q-td>
                      </template>
                      <template #bottom-row>
                        <q-tr class="text-weight-bold bg-grey-2">
                          <q-td>TOPLAM</q-td>
                          <q-td class="text-center">
                            {{ totalTeacherBusinessCount }}
                          </q-td>
                          <q-td class="text-center">
                            {{ summary.totalAssignedHours }}
                          </q-td>
                          <q-td class="text-center">
                            {{ summary.honoraryBusinessCount }}
                          </q-td>
                        </q-tr>
                      </template>
                    </q-table>
                  </q-card-section>
                </q-expansion-item>
              </q-card>
            </div>
          </div>

          <!-- Limit Aşıldı Uyarı -->
          <AppNotice
            v-if="isOverLimit"
            type="error"
            class="q-mt-md"
          >
            Toplam dağıtılan saat ({{ summary.totalAssignedHours }}) ders yükü havuzunu ({{ summary.totalWorkloadPool }}) aşıyor.
          </AppNotice>
        </q-tab-panel>

        <!-- ── Tab 2: Öğretmen Özeti ── -->
        <q-tab-panel
          name="teachers"
          class="q-pa-none"
        >
          <DataState
            :loading="teacherOverviewLoading"
            loading-text="Öğretmen verileri yükleniyor..."
            spinner-size="3em"
            padding="q-pa-xl"
          >
            <q-table
              :rows="teacherOverviewRows"
              :columns="teacherOverviewColumns"
              row-key="teacherId"
              flat
              bordered
              dense
              :rows-per-page-options="[0]"
              hide-pagination
              class="q-mb-md"
            >
              <template #body-cell-teacherName="{ row }">
                <q-td>
                  <div class="row items-center q-gutter-xs">
                    <span class="text-weight-medium">{{ teacherName(row.teacherId) }}</span>
                    <q-icon
                      v-if="!row.scheduleExists"
                      name="warning"
                      color="warning"
                      size="16px"
                    >
                      <q-tooltip>Ders programı girilmemiş</q-tooltip>
                    </q-icon>
                  </div>
                </q-td>
              </template>

              <template #body-cell-honorary="{ row }">
                <q-td class="text-center">
                  <q-badge
                    v-if="row.honoraryVisitCount > 0"
                    color="neutral-soft"
                    text-color="neutral-strong"
                    :label="`${row.honoraryVisitCount} fahri`"
                  >
                    <q-tooltip>
                      {{ HONORARY_HINT }}. Ders programında {{ row.honorarySlotCount }} slot işgal ediyor.
                    </q-tooltip>
                  </q-badge>
                  <span
                    v-else
                    class="text-grey-7"
                  >—</span>
                </q-td>
              </template>

              <template #body-cell-monday="{ row }">
                <q-td class="text-center">
                  <FreeSlotChip
                    :free="row.freeSlotsByDay['Monday'] ?? 0"
                    :assigned="row.assignedSlotsByDay?.['Monday'] ?? 0"
                  />
                </q-td>
              </template>
              <template #body-cell-tuesday="{ row }">
                <q-td class="text-center">
                  <FreeSlotChip
                    :free="row.freeSlotsByDay['Tuesday'] ?? 0"
                    :assigned="row.assignedSlotsByDay?.['Tuesday'] ?? 0"
                  />
                </q-td>
              </template>
              <template #body-cell-wednesday="{ row }">
                <q-td class="text-center">
                  <FreeSlotChip
                    :free="row.freeSlotsByDay['Wednesday'] ?? 0"
                    :assigned="row.assignedSlotsByDay?.['Wednesday'] ?? 0"
                  />
                </q-td>
              </template>
              <template #body-cell-thursday="{ row }">
                <q-td class="text-center">
                  <FreeSlotChip
                    :free="row.freeSlotsByDay['Thursday'] ?? 0"
                    :assigned="row.assignedSlotsByDay?.['Thursday'] ?? 0"
                  />
                </q-td>
              </template>
              <template #body-cell-friday="{ row }">
                <q-td class="text-center">
                  <FreeSlotChip
                    :free="row.freeSlotsByDay['Friday'] ?? 0"
                    :assigned="row.assignedSlotsByDay?.['Friday'] ?? 0"
                  />
                </q-td>
              </template>

              <template #body-cell-workload="{ row }">
                <q-td class="text-center">
                  <WorkloadIndicator
                    :assigned-hours="row.assignedHours"
                    :available-hours="row.businessCount > 0 ? row.assignedHours : 0"
                  />
                </q-td>
              </template>

              <template #no-data>
                <div class="full-width text-center q-pa-md text-grey-7">
                  <q-icon
                    name="people"
                    size="2em"
                    class="q-mb-sm"
                  />
                  <div>Öğretmen verisi bulunamadı. Önce alan seçin ve veri yükleyin.</div>
                </div>
              </template>
            </q-table>
          </DataState>
        </q-tab-panel>
      </q-tab-panels>
    </div>

    <!-- Kaydedilmemiş Değişiklik Onay Dialogu -->
    <FormDialog
      v-model="showDiscardDialog"
      title="Kaydedilmemiş Değişiklikler"
      icon="warning"
      color="warning"
      save-label="Değişiklikleri At"
      save-color="negative"
      @save="confirmDiscard"
    >
      <div>
        {{ pendingChanges.length }} adet kaydedilmemiş değişiklik var. Öğretmen değiştirirseniz bu değişiklikler kaybolacak.
      </div>
    </FormDialog>

    <!-- Atama Geçmişi Dialogu -->
    <DetailDialog
      v-model="historyDialog"
      title="Atama Geçmişi"
      position="right"
      full-height
      card-style="min-width: 420px; max-width: 500px"
    >
      <q-card-section class="text-subtitle2 text-grey-7 q-pt-none">
        {{ historyBusinessName }}
      </q-card-section>

      <q-separator />

      <q-card-section
        class="scroll"
        style="max-height: calc(100vh - 140px)"
      >
        <DataState
          :loading="historyLoading"
          :empty="historyEntries.length === 0"
          padding="q-pa-lg"
        >
          <template #empty>
            Henüz geçmiş kaydı bulunmuyor.
          </template>

          <q-timeline
            color="primary"
            layout="dense"
          >
            <q-timeline-entry
              v-for="(entry, idx) in historyEntries"
              :key="idx"
              :icon="historyIcon(entry.action)"
              :color="historyColor(entry.action)"
            >
              <template #subtitle>
                {{ formatDate(entry.timestamp) }} — {{ entry.performedByName ?? 'Bilinmiyor' }}
              </template>
              <div class="text-body2">
                {{ entry.details }}
              </div>
              <div
                v-if="entry.assignedHours"
                class="text-caption text-grey-7"
              >
                Takdir edilen saat: {{ entry.assignedHours }}
              </div>
            </q-timeline-entry>
          </q-timeline>
        </DataState>
      </q-card-section>
    </DetailDialog>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import {
  coordinationApi,
  type BusinessAssignmentDto,
  type CoordinationSummaryDto,
  type TeacherWorkloadSummaryDto,
} from 'src/api/coordination'
import { useNotify } from 'src/composables/useNotify'
import { useWorkloadConfig } from 'src/composables/useWorkloadConfig'
import { useAssignedHours } from 'src/composables/useAssignedHours'
import { useAssignmentDnD } from 'src/composables/useAssignmentDnD'
import { useTeacherOverview, teacherOverviewColumns } from 'src/composables/useTeacherOverview'
import { useTeacherOptions } from 'src/composables/useEntityOptions'
import { useAssignmentHistory } from 'src/composables/useAssignmentHistory'
import { useScheduleConfig } from 'src/composables/useScheduleConfig'
import { useTeacherScheduleLoader } from 'src/composables/useTeacherScheduleLoader'
import { useTeacherChangeFlow } from 'src/composables/useTeacherChangeFlow'
import { useAuthStore } from 'stores/auth'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import {
  WORKLOAD_POOL_MISSING_MESSAGE,
  isWorkloadPoolUndefined,
  assignedHoursToneClass,
  remainingHoursLabel,
  remainingHoursToneClass,
  workloadPoolLabel,
  workloadPoolToneClass,
} from 'src/utils/workloadPool'
import { HONORARY_LABEL, HONORARY_HINT, isHonorary } from 'src/utils/coordinationHours'
import AssignmentGrid from 'components/AssignmentGrid.vue'
import { useKeyboardAssignment } from 'src/composables/useKeyboardAssignment'
import BranchSelector from 'components/BranchSelector.vue'
import TeacherSelector from 'components/TeacherSelector.vue'
import FreeSlotChip from 'components/FreeSlotChip.vue'
import WorkloadIndicator from 'components/WorkloadIndicator.vue'
import AppNotice from 'components/AppNotice.vue'
import DataState from 'components/DataState.vue'
import DetailDialog from 'components/DetailDialog.vue'
import FormDialog from 'components/FormDialog.vue'
import PageHeader from 'components/PageHeader.vue'
import SearchInput from 'components/SearchInput.vue'

const notify = useNotify()
const authStore = useAuthStore()
const periodStore = useAcademicPeriodStore()

// ── Tab ──
const activeTab = ref('assignment')

// ── Core State ──
const branchFilter = ref<string | null>(null)
const selectedTeacherId = ref<string | null>(null)
const businessSearch = ref('')
const loading = ref(false)

const assignments = ref<BusinessAssignmentDto[]>([])

const summary = ref<CoordinationSummaryDto>({
  totalWorkloadPool: 0,
  totalAssignedHours: 0,
  remainingHours: 0,
  totalMaxHours: 0,
  assignedBusinessCount: 0,
  unassignedBusinessCount: 0,
  honoraryBusinessCount: 0,
  teacherWorkloads: [],
})

// ── Sayfa düzeyinde teacherOpts: sadece isim çözümleme için ──
// TeacherSelector bileşeni kendi instance'ını yönetir; buradaki instance
// selectedTeacherName ve teacherOverview composable'ı için gereklidir.
const teacherOpts = useTeacherOptions()

// ── Computed: Teacher helpers ──
const selectedTeacherName = computed(() => {
  if (!selectedTeacherId.value) return ''
  return teacherOpts.allOptions.value.find((o) => o.value === selectedTeacherId.value)?.label ?? ''
})

const isOverLimit = computed(
  () => summary.value.totalWorkloadPool > 0 && summary.value.totalAssignedHours > summary.value.totalWorkloadPool,
)

/**
 * Havuz hesaplanmamış (#111). `isOverLimit` burada bilinçli olarak false kalır —
 * bilinmeyen havuza göre "aşıyor" denemez — ama özet kutuları ve uyarı bandı bu
 * bayrağa bakarak sessiz kalmak yerine eksik yapılandırmayı söyler.
 */
const poolUndefined = computed(() => isWorkloadPoolUndefined(summary.value.totalWorkloadPool))

const totalTeacherBusinessCount = computed(() =>
  summary.value.teacherWorkloads.reduce((sum: number, tw: TeacherWorkloadSummaryDto) => sum + tw.businessCount, 0),
)

const businessNameMap = computed(() => {
  const map: Record<string, string> = {}
  for (const a of assignments.value) {
    map[a.businessId] = a.businessName
  }
  return map
})

// ── Day labels ──
const dayLabels: Record<string, string> = {
  Monday: 'Pazartesi',
  Tuesday: 'Salı',
  Wednesday: 'Çarşamba',
  Thursday: 'Perşembe',
  Friday: 'Cuma',
}

function dayLabel(day: string | null): string {
  return day ? (dayLabels[day] ?? day) : '—'
}

/**
 * Kart üzerindeki ilerleme etiketi. Fahri satırda birim "saat" değil "ziyaret" —
 * fahri ziyaret slot işgal eder ama ek ders saati üretmez (#115).
 */
function slotProgressLabel(biz: BusinessAssignmentDto): string {
  const { current, target } = slotProgress(biz)
  return isHonorary(biz) ? `${current}/${target} ziyaret` : `${current}/${target} saat`
}

// ── Teacher Summary Columns ──
const teacherSummaryColumns = [
  { name: 'teacherName', label: 'Öğretmen', field: 'teacherName', align: 'left' as const, sortable: true },
  { name: 'businessCount', label: 'İşletme Sayısı', field: 'businessCount', align: 'center' as const, sortable: true },
  { name: 'assignedHours', label: 'Atanan Saat', field: 'assignedHours', align: 'center' as const, sortable: true },
  // Fahri ziyaret slot işgal eder ama "Atanan Saat"e girmez — ayrı sütun olmazsa
  // öğretmen yükü olduğundan az görünür (#115).
  { name: 'honoraryVisitCount', label: 'Fahri', field: 'honoraryVisitCount', align: 'center' as const, sortable: true },
]

// ── API: Load Data ──

async function loadData() {
  if (!branchFilter.value) return
  loading.value = true
  try {
    const [assignRes, summaryRes] = await Promise.all([
      coordinationApi.listAssignments({
        branchCode: branchFilter.value,
        academicPeriodId: periodStore.selectedPeriodId ?? undefined,
      }),
      coordinationApi.getCoordinationSummary({
        branchCode: branchFilter.value,
        academicPeriodId: periodStore.selectedPeriodId ?? undefined,
      }),
    ])
    assignments.value = assignRes.data ?? []
    summary.value = summaryRes.data ?? summary.value
    hours.initEditedHours()
  } catch (e) {
    notify.apiError(e, 'İşletme listesi yüklenirken hata oluştu.')
  } finally {
    loading.value = false
  }
}

// ── Composables ──

const institutionId = computed(() => authStore.user?.institutionId ?? undefined)
const periodId = computed(() => periodStore.selectedPeriodId)
const semester = computed(() => periodStore.selectedSemester)

const { periodCount, scheduleConfigMissing, loadScheduleConfig, createEmptySchedule } =
  useScheduleConfig({ authStore })

// Öğretmen-programı yükleme orkestrasyonu (rawSchedule + scheduleLoading state'ini sahiplenir)
const { scheduleLoading, rawSchedule, loadTeacherSchedule } = useTeacherScheduleLoader({
  periodId,
  semester,
  createEmptySchedule,
})

const workload = useWorkloadConfig({ branchFilter, periodId, institutionId, notify })
const hours = useAssignedHours({
  assignments,
  workloadConfig: workload.workloadConfig,
  academicPeriodId: periodId,
  branchCode: branchFilter,
  notify,
  loadData,
})
const dnd = useAssignmentDnD({
  assignments,
  rawSchedule,
  selectedTeacherId,
  selectedTeacherName,
  academicPeriodId: periodId,
  notify,
  loadData,
  loadTeacherSchedule,
})
const teacherOverview = useTeacherOverview({
  periodId,
  semester,
  branchFilter,
  teacherOpts,
  notify,
})

// Re-export composable values used by template
const { loadWorkloadConfig } = workload
const {
  pendingChanges, saving, effectiveSchedule,
  unassignedBusinesses, assignedToTeacher,
  slotProgress, onBusinessDragStart, onBusinessDropped,
  onBusinessRemoved, removeAssignment, saveAll,
} = dnd

const {
  teacherOverviewRows, teacherOverviewLoading, teacherName, loadTeacherOverview,
} = teacherOverview

// ── Klavye erişilebilir atama (#88) ──
// Sürükle-bırak yalnız fareyle çalışıyordu; aynı işi Enter/Space + Tab ile yapılabilir kılar.
const {
  selected: kbSelected,
  announcement: kbAnnouncement,
  toggle: kbToggle,
  select: kbSelect,
  cancel: kbCancel,
  completeDrop: kbCompleteDrop,
} = useKeyboardAssignment()

function onCardKey(biz: BusinessAssignmentDto) {
  if (periodStore.isReadOnly) return
  kbToggle({ businessId: biz.businessId, businessName: biz.businessName })
}

/**
 * Izgaradan gelen bırakma. Fare yolunda seçim zaten yok; klavye yolunda seçimi temizler ve
 * sonucu duyurur. Atama mantığı tek yerde (onBusinessDropped) kalır.
 */
function onKeyboardAwareDrop(payload: { businessId: string; day: string; periodNumber: number }) {
  onBusinessDropped(payload)
  if (kbSelected.value) kbCompleteDrop(payload.day, payload.periodNumber)
}

const {
  historyDialog, historyLoading, historyBusinessName, historyEntries,
  showHistory, historyIcon, historyColor, formatDate,
} = useAssignmentHistory({ notify })

const {
  showDiscardDialog,
  onTeacherChange, confirmDiscard, selectTeacher,
} = useTeacherChangeFlow({
  selectedTeacherId,
  rawSchedule,
  pendingChanges,
  clearPending: dnd.clearPending,
  loadTeacherSchedule,
})

// ── Filtered unassigned (UI search) ──
const filteredUnassigned = computed(() => {
  if (!businessSearch.value) return unassignedBusinesses.value
  const needle = businessSearch.value.toLocaleLowerCase('tr')
  return unassignedBusinesses.value.filter(
    (b) =>
      b.businessName.toLocaleLowerCase('tr').includes(needle) ||
      (b.district?.toLocaleLowerCase('tr').includes(needle) ?? false),
  )
})

// ── Teacher Change ──

function onBranchChange() {
  selectedTeacherId.value = null
  rawSchedule.value = []
  dnd.clearPending()
  // TeacherSelector ve sayfa düzeyindeki teacherOpts tüm öğretmenleri tutar;
  // branch değişiminde client-side filtreleme yeterli, yeniden yüklemeye gerek yok.
  loadData().catch(() => {})
  loadWorkloadConfig().catch(() => {})
}

// ── Tab değişimi → lazy load ──
watch(activeTab, (tab) => {
  if (tab === 'teachers' && teacherOverviewRows.value.length === 0) {
    loadTeacherOverview().catch(() => {})
  }
})

// ── Dönem değişikliği ──
watch(
  () => [periodStore.selectedPeriodId, periodStore.selectedSemester],
  () => {
    if (selectedTeacherId.value) {
      dnd.clearPending()
      loadTeacherSchedule(selectedTeacherId.value)
    }
    if (activeTab.value === 'teachers') loadTeacherOverview().catch(() => {})
  },
)

// ── Init ──
// BranchSelector ve TeacherSelector kendi onMounted'larında yüklenirler.
// Burada sadece schedule config + sayfa düzeyindeki teacherOpts (isim çözümleme) yüklenir.
onMounted(async () => {
  const instId = authStore.user?.institutionId ?? undefined

  // Kapsam tek alansa otomatik seç (#126) — karar rol adına değil, yazma kapsamına bakar.
  const scopedBranch = authStore.writableBranchCodes?.length === 1
    ? authStore.writableBranchCodes[0]
    : null

  if (scopedBranch) {
    branchFilter.value = scopedBranch
    await Promise.all([
      teacherOpts.reload({ institutionId: instId, branchCode: scopedBranch }),
      loadScheduleConfig(),
    ])
    await loadData()
  } else {
    await Promise.all([
      teacherOpts.load({ institutionId: instId }),
      loadScheduleConfig(),
    ])
  }
})
</script>

<style scoped>
/* Ekran okuyucuya açık, görsel olarak gizli — aria-live duyuruları için (#88).
   display:none veya visibility:hidden KULLANILMAZ; ikisi de içeriği erişilebilirlik
   ağacından çıkarır ve duyuru hiç okunmaz. */
.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border: 0;
}

/* Klavyeyle seçili kart — sürükle-bırakta fareyle "tutulmuş" hâlin karşılığı */
.business-card--selected {
  outline: 2px solid var(--q-primary);
  outline-offset: 1px;
}

.sticky-panel {
  position: sticky;
  top: 60px;
  max-height: calc(100vh - 80px);
  display: flex;
  flex-direction: column;
}

.business-list-container {
  overflow-y: auto;
  flex: 1;
  min-height: 0;
}

.business-card-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.business-card {
  /* Ham amber yerine tema türevi warning tonu (#104). */
  background: #f5f0e6;
  background: color-mix(in srgb, var(--q-warning) 10%, #fff);
  border: 1px solid #dccba6;
  border-color: color-mix(in srgb, var(--q-warning) 35%, #fff);
  border-radius: 8px;
  padding: 10px 12px;
  cursor: grab;
  /* Hover'da değişen iki özellik — transition: all değil. */
  transition-property: background-color, border-color;
  transition-duration: 0.2s;
  transition-timing-function: ease-out;
  user-select: none;
}

.business-card:hover {
  /* Gölge/kaldırma yok (Tonal Derinlik Kuralı): derinlik zemin tonuyla kurulur.
    "Tutulabilir" sinyali cursor: grab / :active grabbing'den gelir. */
  background: #ede4d1;
  background: color-mix(in srgb, var(--q-warning) 18%, #fff);
  border-color: #c7ae73;
  border-color: color-mix(in srgb, var(--q-warning) 55%, #fff);
}

.business-card:active {
  cursor: grabbing;
}

.business-card--dragging {
  opacity: 0.5;
}

.business-card--disabled {
  cursor: not-allowed;
  opacity: 0.6;
}
</style>
