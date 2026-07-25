<template>
  <q-page padding>
    <div class="text-h5 text-weight-bold q-mb-lg">
      İşletme Dağıtımı
    </div>

    <!-- Filtreler -->
    <div class="row q-col-gutter-md q-mb-lg items-end">
      <div class="col-12 col-sm-3">
        <BranchSelector
          v-model="branchFilter"
          @update:model-value="onBranchChange"
        />
      </div>

      <div class="col-12 col-sm-3">
        <TeacherSelector
          ref="teacherSelectorRef"
          v-model="selectedTeacherId"
          :branch-code="branchFilter"
          :show-cross-branch="!authStore.isDepartmentHead && !!branchFilter"
          @update:model-value="onTeacherChange"
        />
      </div>
      <div class="col-12 col-sm-auto q-gutter-sm">
        <q-btn
          color="primary"
          icon="save"
          label="Kaydet"
          :loading="saving"
          :disable="pendingChanges.length === 0 || periodStore.isReadOnly"
          @click="saveAll"
        >
          <q-badge
            v-if="pendingChanges.length > 0"
            color="red"
            floating
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
          name="hours-map"
          icon="map"
          label="İşletme Saatleri & Harita"
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
                      color="orange-7"
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
                      <!-- Sürükle-bırak yüzeyi. Klavyeyle erişilebilir alternatif ayrı issue'da. -->
                      <!-- eslint-disable-next-line vuejs-accessibility/no-static-element-interactions -->
                      <div
                        v-for="biz in filteredUnassigned"
                        :key="biz.businessId"
                        class="business-card"
                        :class="{ 'business-card--disabled': periodStore.isReadOnly }"
                        :draggable="!periodStore.isReadOnly"
                        @dragstart="onBusinessDragStart($event, biz)"
                      >
                        <div class="row items-center no-wrap">
                          <q-icon
                            name="business"
                            size="18px"
                            color="orange-7"
                            class="q-mr-sm"
                          />
                          <div class="col">
                            <div class="text-body2 text-weight-medium ellipsis">
                              {{ biz.businessName }}
                            </div>
                            <div class="text-caption text-grey-6">
                              {{ biz.district ?? '—' }}
                              <span v-if="biz.distanceToSchoolKm != null"> · {{ biz.distanceToSchoolKm.toFixed(1) }} km</span>
                            </div>
                          </div>
                        </div>
                        <div class="row q-mt-xs q-gutter-xs">
                          <q-badge
                            :color="slotProgress(biz).current > 0 ? 'orange-7' : 'green-7'"
                            :label="`${slotProgress(biz).current}/${slotProgress(biz).target} saat`"
                            dense
                          />
                          <q-badge
                            color="blue-7"
                            :label="`${biz.activeStudentCount} öğrenci`"
                            dense
                          />
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
                      <div class="text-h5 text-green-8">
                        {{ summary.totalWorkloadPool }}
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
                        class="text-h5"
                        :class="isOverLimit ? 'text-red-8' : 'text-blue-8'"
                      >
                        {{ summary.totalAssignedHours }}
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
                      <div class="text-h5 text-orange-8">
                        {{ summary.remainingHours }}
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
                      <div class="text-h5 text-purple-8">
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
                    <div class="text-caption text-grey-6 q-mt-sm">
                      Program yükleniyor...
                    </div>
                  </div>

                  <AssignmentGrid
                    v-else
                    :schedule="effectiveSchedule"
                    :period-count="periodCount"
                    :disabled="periodStore.isReadOnly"
                    :business-name-map="businessNameMap"
                    @business-dropped="onBusinessDropped"
                    @business-removed="onBusinessRemoved"
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
                      color="blue-7"
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
                          color="blue-7"
                        />
                      </q-item-section>
                      <q-item-section>
                        <q-item-label>{{ biz.businessName }}</q-item-label>
                        <q-item-label caption>
                          {{ dayLabel(biz.assignedDay) }}
                          <span v-if="biz.assignedPeriodNumber"> · {{ biz.assignedPeriodNumber }}. saat</span>
                          · {{ biz.assignedHours }} saat
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
                            @click="showHistory(biz.businessId, biz.businessName)"
                          >
                            <q-tooltip>Atama geçmişi</q-tooltip>
                          </q-btn>
                          <q-btn
                            v-if="!periodStore.isReadOnly"
                            flat
                            round
                            dense
                            icon="close"
                            color="red-5"
                            size="sm"
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
                      bordered
                      dense
                      hide-pagination
                      :rows-per-page-options="[0]"
                    >
                      <template #body-cell-teacherName="{ row }">
                        <q-td>
                          <a
                            href="#"
                            class="text-primary cursor-pointer"
                            @click.prevent="selectTeacher(row.teacherId)"
                          >
                            {{ row.teacherName }}
                          </a>
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
          <div
            v-if="teacherOverviewLoading"
            class="text-center q-pa-xl"
          >
            <q-spinner
              color="primary"
              size="3em"
            />
            <div class="text-caption text-grey-6 q-mt-sm">
              Öğretmen verileri yükleniyor...
            </div>
          </div>

          <div v-else>
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
                      color="orange-6"
                      size="16px"
                    >
                      <q-tooltip>Ders programı girilmemiş</q-tooltip>
                    </q-icon>
                  </div>
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
                <div class="full-width text-center q-pa-md text-grey-6">
                  <q-icon
                    name="people"
                    size="2em"
                    class="q-mb-sm"
                  />
                  <div>Öğretmen verisi bulunamadı. Önce alan seçin ve veri yükleyin.</div>
                </div>
              </template>
            </q-table>
          </div>
        </q-tab-panel>

        <!-- ── Tab: İşletme Saatleri & Harita ── -->
        <q-tab-panel
          name="hours-map"
          class="q-pa-none"
        >
          <!-- Harita Bölümü -->
          <q-card
            flat
            bordered
            class="q-mb-md"
          >
            <q-card-section>
              <div class="text-subtitle1 text-weight-medium q-mb-sm">
                Harita
              </div>

              <!-- Harita araç çubuğu -->
              <div class="row items-center q-gutter-sm q-mb-md">
                <q-input
                  v-model.number="clusterEps"
                  type="number"
                  label="Yarıçap (m)"
                  outlined
                  dense
                  style="width: 130px"
                  :min="100"
                  :max="10000"
                  :step="100"
                />
                <q-input
                  v-model.number="clusterMinPoints"
                  type="number"
                  label="Min. Nokta"
                  outlined
                  dense
                  style="width: 110px"
                  :min="2"
                  :max="20"
                />
                <q-btn
                  color="primary"
                  icon="refresh"
                  label="Kümele"
                  :loading="clusterLoading"
                  @click="loadClusters"
                />
                <q-separator
                  vertical
                  inset
                  class="q-mx-sm"
                />
                <q-btn
                  color="teal"
                  icon="route"
                  label="Mesafe Hesapla"
                  :loading="recalculating"
                  :disable="periodStore.isReadOnly"
                  @click="recalculateDistances"
                />
              </div>

              <DataState
                :loading="clusterLoading"
                :error="clusterError"
                :empty="clusterData.length === 0"
                loading-text="İşletme kümeleri yükleniyor..."
                padding="q-pa-xl"
                spinner-size="3em"
              >
                <template #error>
                  <q-icon
                    name="warning"
                    size="3em"
                    color="orange-6"
                    class="q-mb-sm"
                  />
                  <div>Kümeleme verisi yüklenemedi.</div>
                  <div class="text-caption q-mt-sm">
                    PostGIS eklentisi henüz etkin olmayabilir. Sistem yöneticisi ile iletişime geçin.
                  </div>
                </template>

                <template #empty>
                  <q-icon
                    name="location_off"
                    size="3em"
                    class="q-mb-sm"
                  />
                  <div>Konum verisi olan işletme bulunamadı.</div>
                  <div class="text-caption q-mt-sm">
                    İşletmelere koordinat atandıktan sonra harita burada gösterilecek.
                  </div>
                </template>

                <!-- Küme Özeti Chip'leri -->
                <div class="row q-gutter-xs q-mb-md flex-wrap">
                  <q-chip
                    v-for="(count, clusterId) in clusterCounts"
                    :key="clusterId"
                    :style="{ backgroundColor: clusterColor(Number(clusterId)), color: '#fff' }"
                    dense
                    class="text-weight-medium"
                  >
                    {{ clusterId === 'null' ? 'Tek başına' : `Küme ${clusterId}` }}: {{ count }}
                  </q-chip>
                </div>

                <BusinessClusterMap
                  :businesses="clusterData"
                  :school-location="null"
                  :assigned-hours="editedHours"
                  :editable="!periodStore.isReadOnly"
                  height="500px"
                  @update:hours="onMapHoursUpdate"
                />
              </DataState>
            </q-card-section>
          </q-card>

          <!-- Saat Tablosu Bölümü -->
          <q-card
            flat
            bordered
          >
            <q-card-section>
              <div class="text-subtitle1 text-weight-medium q-mb-md">
                İşletme Takdir Edilen Saatler
              </div>

              <!-- Uyarı Banner'ları -->
              <AppNotice
                v-if="hoursOverLimit"
                type="error"
                class="q-mb-md"
              >
                Toplam takdir edilen saat ({{ hoursTotalAssigned }}) ders yükü havuzunu ({{ hoursWorkloadPool }}) aşıyor!
              </AppNotice>
              <AppNotice
                v-else-if="hoursNearLimit"
                type="warning"
                class="q-mb-md"
              >
                Toplam takdir edilen saat havuza yaklaşıyor: {{ hoursTotalAssigned }} / {{ hoursWorkloadPool }}
              </AppNotice>

              <q-markup-table
                flat
                bordered
                separator="cell"
                class="q-mb-md"
              >
                <thead>
                  <tr class="bg-grey-2">
                    <th class="text-left">
                      İşletme
                    </th>
                    <th
                      class="text-center"
                      style="width: 100px"
                    >
                      Mesafe
                    </th>
                    <th
                      class="text-center"
                      style="width: 100px"
                    >
                      Öğrenci
                    </th>
                    <th
                      class="text-center"
                      style="width: 120px"
                    >
                      Verilebilir Maks.
                    </th>
                    <th
                      class="text-center"
                      style="width: 140px"
                    >
                      Takdir Edilen
                    </th>
                  </tr>
                </thead>
                <tbody>
                  <tr
                    v-for="biz in assignments"
                    :key="biz.businessId"
                  >
                    <td class="text-left">
                      {{ biz.businessName }}
                      <q-btn
                        flat
                        round
                        dense
                        icon="history"
                        color="grey-6"
                        size="xs"
                        class="q-ml-xs"
                        @click="showHistory(biz.businessId, biz.businessName)"
                      >
                        <q-tooltip>Atama geçmişi</q-tooltip>
                      </q-btn>
                    </td>
                    <td class="text-center text-caption">
                      {{ biz.distanceToSchoolKm != null ? `${biz.distanceToSchoolKm.toFixed(1)} km` : '—' }}
                    </td>
                    <td class="text-center">
                      {{ biz.activeStudentCount }}
                    </td>
                    <td class="text-center text-weight-medium text-green-8">
                      {{ biz.maxCoordinationHours }}
                    </td>
                    <td class="text-center">
                      <q-input
                        v-model.number="editedHours[biz.businessId]"
                        type="number"
                        dense
                        outlined
                        :min="1"
                        :max="biz.maxCoordinationHours"
                        style="max-width: 90px; margin: 0 auto"
                        :disable="periodStore.isReadOnly"
                        :rules="[v => (v > 0 && v <= biz.maxCoordinationHours) || `1-${biz.maxCoordinationHours}`]"
                      />
                    </td>
                  </tr>
                </tbody>
              </q-markup-table>

              <!-- Özet + Kaydet -->
              <div class="row items-center q-mt-md">
                <div class="text-body2">
                  Havuz: <strong class="text-green-8">{{ hoursWorkloadPool }}</strong>
                  &nbsp;|&nbsp; Σ Takdir: <strong :class="hoursOverLimit ? 'text-red-8' : 'text-blue-8'">{{ hoursTotalAssigned }}</strong>
                  &nbsp;|&nbsp; Kalan: <strong class="text-orange-8">{{ hoursRemaining }}</strong>
                  &nbsp;|&nbsp; Σ Maks: <strong class="text-grey-6">{{ hoursTotalMaxHours }}</strong>
                </div>
                <q-space />
                <q-btn
                  color="primary"
                  icon="save"
                  label="Saatleri Kaydet"
                  :loading="hoursSaving"
                  :disable="changedHoursCount === 0 || periodStore.isReadOnly"
                  @click="saveHours"
                >
                  <q-badge
                    v-if="changedHoursCount > 0"
                    color="red"
                    floating
                  >
                    {{ changedHoursCount }}
                  </q-badge>
                </q-btn>
              </div>
            </q-card-section>
          </q-card>
        </q-tab-panel>
      </q-tab-panels>
    </div>

    <!-- Kaydedilmemiş Değişiklik Onay Dialogu -->
    <q-dialog
      v-model="showDiscardDialog"
      persistent
    >
      <q-card style="min-width: 350px">
        <q-card-section>
          <div class="text-h6">
            Kaydedilmemiş Değişiklikler
          </div>
        </q-card-section>
        <q-card-section>
          {{ pendingChanges.length }} adet kaydedilmemiş değişiklik var. Öğretmen değiştirirseniz bu değişiklikler kaybolacak.
        </q-card-section>
        <q-card-actions align="right">
          <q-btn
            flat
            label="İptal"
            color="grey-7"
            @click="showDiscardDialog = false"
          />
          <q-btn
            flat
            label="Değişiklikleri At"
            color="red"
            @click="confirmDiscard"
          />
        </q-card-actions>
      </q-card>
    </q-dialog>

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
                {{ formatDate(entry.timestamp) }} — {{ entry.performedBy }}
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
import { useClusterMap } from 'src/composables/useClusterMap'
import { useTeacherOverview, teacherOverviewColumns } from 'src/composables/useTeacherOverview'
import { useTeacherOptions } from 'src/composables/useEntityOptions'
import { useAssignmentHistory } from 'src/composables/useAssignmentHistory'
import { useScheduleConfig } from 'src/composables/useScheduleConfig'
import { useTeacherScheduleLoader } from 'src/composables/useTeacherScheduleLoader'
import { useTeacherChangeFlow } from 'src/composables/useTeacherChangeFlow'
import { useAuthStore } from 'stores/auth'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import AssignmentGrid from 'components/AssignmentGrid.vue'
import BusinessClusterMap from 'components/BusinessClusterMap.vue'
import BranchSelector from 'components/BranchSelector.vue'
import TeacherSelector from 'components/TeacherSelector.vue'
import FreeSlotChip from 'components/FreeSlotChip.vue'
import WorkloadIndicator from 'components/WorkloadIndicator.vue'
import AppNotice from 'components/AppNotice.vue'
import DataState from 'components/DataState.vue'
import DetailDialog from 'components/DetailDialog.vue'
import SearchInput from 'components/SearchInput.vue'

const notify = useNotify()
const authStore = useAuthStore()
const periodStore = useAcademicPeriodStore()
const teacherSelectorRef = ref<InstanceType<typeof TeacherSelector> | null>(null)

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

// ── Teacher Summary Columns ──
const teacherSummaryColumns = [
  { name: 'teacherName', label: 'Öğretmen', field: 'teacherName', align: 'left' as const, sortable: true },
  { name: 'businessCount', label: 'İşletme Sayısı', field: 'businessCount', align: 'center' as const, sortable: true },
  { name: 'assignedHours', label: 'Atanan Saat', field: 'assignedHours', align: 'center' as const, sortable: true },
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
  notify,
  loadData,
})
const dnd = useAssignmentDnD({
  assignments,
  rawSchedule,
  selectedTeacherId,
  selectedTeacherName,
  notify,
  authStore,
  loadData,
  loadTeacherSchedule,
})
const cluster = useClusterMap({ notify, loadData, branchFilter })
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
  editedHours, hoursSaving, hoursTotalMaxHours, hoursWorkloadPool,
  hoursTotalAssigned, hoursRemaining, hoursOverLimit, hoursNearLimit,
  changedHoursCount, saveHours,
} = hours

const {
  pendingChanges, saving, effectiveSchedule,
  unassignedBusinesses, assignedToTeacher,
  slotProgress, onBusinessDragStart, onBusinessDropped,
  onBusinessRemoved, removeAssignment, saveAll,
} = dnd

const {
  clusterData, clusterLoading, clusterError,
  clusterEps, clusterMinPoints, clusterCounts,
  recalculating, clusterColor,
  loadClusters, recalculateDistances,
} = cluster

const {
  teacherOverviewRows, teacherOverviewLoading, teacherName, loadTeacherOverview,
} = teacherOverview

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

// ── Harita popup'tan saat güncelleme ──
function onMapHoursUpdate(businessId: string, hours_val: number) {
  editedHours.value[businessId] = hours_val
}

// ── Tab değişimi → lazy load ──
watch(activeTab, (tab) => {
  if (tab === 'hours-map') {
    hours.initEditedHours()
    loadWorkloadConfig().catch(() => {})
    if (clusterData.value.length === 0 && !clusterError.value) {
      loadClusters().catch(() => {})
    }
  }
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
    if (activeTab.value === 'hours-map') loadClusters().catch(() => {})
  },
)

// ── Init ──
// BranchSelector ve TeacherSelector kendi onMounted'larında yüklenirler.
// Burada sadece schedule config + sayfa düzeyindeki teacherOpts (isim çözümleme) yüklenir.
onMounted(async () => {
  const instId = authStore.user?.institutionId ?? undefined

  if (authStore.isDepartmentHead && authStore.user?.branchCode) {
    branchFilter.value = authStore.user.branchCode
    await Promise.all([
      teacherOpts.reload({ institutionId: instId, branchCode: authStore.user.branchCode }),
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
  background: #fff8e1;
  border: 1px solid #ffe082;
  border-radius: 8px;
  padding: 10px 12px;
  cursor: grab;
  transition: all 0.2s;
  user-select: none;
}

.business-card:hover {
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.12);
  border-color: #ffb74d;
  transform: translateY(-1px);
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
