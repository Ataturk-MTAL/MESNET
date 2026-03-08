<template>
  <q-page padding>
    <div class="text-h5 text-weight-bold q-mb-lg">İşletme Dağıtımı</div>

    <!-- Filtreler -->
    <div class="row q-col-gutter-md q-mb-lg items-end">
      <!-- Alan seçimi: yöneticiler seçebilir; alan şefi kendi alanını chip olarak görür -->
      <div v-if="!authStore.isDepartmentHead" class="col-12 col-sm-3">
        <q-select
          v-model="branchFilter"
          :options="branchOpts.options.value"
          :loading="branchOpts.loading.value"
          label="Alan"
          filled
          use-input
          input-debounce="0"
          emit-value
          map-options
          option-label="label"
          option-value="value"
          clearable
          @filter="branchOpts.filter"
          @update:model-value="onBranchChange"
        >
          <template #prepend>
            <q-icon name="school" />
          </template>
          <template #no-option>
            <q-item>
              <q-item-section class="text-grey">Sonuç bulunamadı</q-item-section>
            </q-item>
          </template>
        </q-select>
      </div>
      <div v-else class="col-12 col-sm-3">
        <q-field label="Alan" filled stack-label>
          <template #control>
            <div class="self-center full-width no-outline">
              <q-icon name="school" color="blue-7" class="q-mr-sm" />
              {{ branchLabel }}
            </div>
          </template>
        </q-field>
      </div>

      <div class="col-12 col-sm-3">
        <q-select
          v-model="selectedTeacherId"
          :options="filteredTeacherOpts"
          :loading="teacherOpts.loading.value"
          label="Öğretmen"
          filled
          use-input
          input-debounce="0"
          emit-value
          map-options
          option-label="label"
          option-value="value"
          clearable
          @filter="onTeacherFilter"
          @update:model-value="onTeacherChange"
        >
          <template #prepend>
            <q-icon name="person" />
          </template>
          <template #option="scope">
            <q-item v-bind="scope.itemProps">
              <q-item-section>
                <q-item-label>{{ scope.opt.label }}</q-item-label>
                <q-item-label
                  v-if="!authStore.isDepartmentHead && branchFilter && scope.opt.branchCode !== branchFilter"
                  caption
                  class="text-orange-8"
                >
                  Farklı alan: {{ scope.opt.branchCode }}
                </q-item-label>
              </q-item-section>
              <q-item-section
                v-if="!authStore.isDepartmentHead && branchFilter && scope.opt.branchCode !== branchFilter"
                side
              >
                <q-badge color="orange" label="Farklı alan" />
              </q-item-section>
            </q-item>
          </template>
          <template #no-option>
            <q-item>
              <q-item-section class="text-grey">Sonuç bulunamadı</q-item-section>
            </q-item>
          </template>
        </q-select>
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
          <q-badge v-if="pendingChanges.length > 0" color="red" floating>
            {{ pendingChanges.length }}
          </q-badge>
        </q-btn>
      </div>
    </div>

    <!-- Bilgi Mesajı -->
    <q-banner
      v-if="!branchFilter"
      rounded
      class="bg-blue-1 text-blue-9 q-mb-md"
    >
      <template #avatar>
        <q-icon name="info" color="blue-7" />
      </template>
      İşletme dağıtımı yapmak için önce bir alan seçin.
    </q-banner>

    <q-banner
      v-if="scheduleConfigMissing"
      rounded
      class="bg-orange-1 text-orange-9 q-mb-md"
    >
      <template #avatar>
        <q-icon name="warning" color="orange-7" />
      </template>
      Kurum için günlük ders sayısı ayarlanmamış. Lütfen önce Kurum sayfasından ders programı ayarını yapın.
    </q-banner>

    <!-- Read-only Uyarı -->
    <q-banner
      v-if="periodStore.isReadOnly"
      rounded
      class="bg-grey-2 text-grey-8 q-mb-md"
    >
      <template #avatar>
        <q-icon name="lock" color="grey-6" />
      </template>
      Kapalı dönem — yalnızca görüntüleme modu.
    </q-banner>

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
        <q-tab name="hours" icon="schedule" label="Takdir Edilen Saat" />
        <q-tab name="assignment" icon="drag_indicator" label="İşletme Dağıtımı" />
        <q-tab name="teachers" icon="people" label="Öğretmen Özeti" />
        <q-tab name="map" icon="map" label="Harita" />
      </q-tabs>

      <q-separator class="q-mb-md" />

      <q-tab-panels v-model="activeTab" animated>
        <!-- ── Tab: Takdir Edilen Saat ── -->
        <q-tab-panel name="hours" class="q-pa-none">
          <!-- Kart 1: Alan Yapılandırması -->
          <q-card flat bordered class="q-mb-md">
            <q-card-section>
              <div class="text-subtitle1 text-weight-medium q-mb-sm">
                Alan Ders Yükü Yapılandırması
              </div>
              <div class="text-caption text-grey-7 q-mb-md">
                Norm Kadro Yönetmeliği Madde 22'ye göre grup sayısı ve şeflik saatleri ile toplam ders yükü havuzu hesaplanır.
              </div>

              <q-inner-loading :showing="workloadLoading" />

              <div class="row q-col-gutter-md q-mb-md items-end">
                <div class="col-12 col-sm-3">
                  <q-select
                    v-model="wlEducationType"
                    :options="EDUCATION_TYPES"
                    label="Eğitim Tipi"
                    filled
                    dense
                    emit-value
                    map-options
                    :disable="periodStore.isReadOnly"
                    @update:model-value="loadWorkloadConfig"
                  />
                </div>
                <div class="col-auto">
                  <q-btn
                    flat
                    dense
                    color="orange-8"
                    icon="sync"
                    label="Öğrenci Sayılarını Güncelle"
                    :loading="syncingCounts"
                    :disable="periodStore.isReadOnly"
                    @click="syncStudentCounts"
                  >
                    <q-tooltip>Enrollment kayıtlarından öğrenci sayılarını yeniden hesapla</q-tooltip>
                  </q-btn>
                </div>
              </div>

              <!-- Şeflik -->
              <div class="text-body2 text-weight-medium q-mb-sm">Şeflik</div>
              <div class="row q-col-gutter-md q-mb-md">
                <div class="col-6 col-sm-3">
                  <q-input
                    v-model.number="wlDeptHeadCount"
                    type="number"
                    label="Alan Şefi Sayısı"
                    filled
                    dense
                    :min="0"
                    :max="1"
                    :disable="periodStore.isReadOnly"
                  />
                </div>
                <div class="col-6 col-sm-3">
                  <q-input
                    v-model.number="wlDeptHeadHours"
                    type="number"
                    label="Alan Şefi Saati"
                    filled
                    dense
                    :min="0"
                    :disable="periodStore.isReadOnly"
                  />
                </div>
                <div class="col-6 col-sm-3">
                  <q-input
                    v-model.number="wlWorkshopHeadCount"
                    type="number"
                    label="Atölye Şefi Sayısı"
                    filled
                    dense
                    :min="0"
                    :disable="periodStore.isReadOnly"
                  />
                </div>
                <div class="col-6 col-sm-3">
                  <q-input
                    v-model.number="wlWorkshopHeadHours"
                    type="number"
                    label="Atölye Şefi Saati"
                    filled
                    dense
                    :min="0"
                    :disable="periodStore.isReadOnly"
                  />
                </div>
              </div>
              <div class="text-body2 q-mb-md">
                Şeflik Toplamı: <strong class="text-purple-8">{{ wlSupervisorTotal }}</strong> saat
                <span class="text-caption text-grey-7">
                  ({{ wlDeptHeadCount }} × {{ wlDeptHeadHours }} + {{ wlWorkshopHeadCount }} × {{ wlWorkshopHeadHours }})
                </span>
              </div>

              <!-- Sınıf Bazlı Ders Yükü -->
              <div class="text-body2 text-weight-medium q-mb-sm">Sınıf Bazlı Ders Yükü</div>
              <q-markup-table flat bordered separator="cell" class="q-mb-md">
                <thead>
                  <tr class="bg-grey-2">
                    <th class="text-center" style="width: 80px">Sınıf</th>
                    <th class="text-center" style="width: 130px">Öğrenci Sayısı</th>
                    <th class="text-center" style="width: 130px">Haftalık Ders</th>
                    <th class="text-center" style="width: 80px">Grup</th>
                    <th class="text-center" style="width: 100px">Alt Toplam</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="(cl, idx) in wlClassLevels" :key="cl.classYear">
                    <td class="text-center text-weight-medium">{{ cl.classYear }}. Sınıf</td>
                    <td class="text-center text-weight-medium">
                      {{ cl.studentCount }}
                    </td>
                    <td class="text-center">
                      <q-input
                        v-model.number="wlClassLevels[idx].weeklyLessonHours"
                        type="number"
                        dense
                        outlined
                        :min="0"
                        style="max-width: 100px; margin: 0 auto"
                        :disable="periodStore.isReadOnly"
                      />
                    </td>
                    <td class="text-center text-weight-medium text-blue-8">
                      {{ estimateGroupCount(wlEducationType, cl.classYear, cl.studentCount) }}
                    </td>
                    <td class="text-center text-weight-medium">
                      {{ cl.weeklyLessonHours * estimateGroupCount(wlEducationType, cl.classYear, cl.studentCount) }}
                    </td>
                  </tr>
                </tbody>
              </q-markup-table>

              <!-- Toplamlar + Kaydet -->
              <div class="row items-center">
                <div class="text-body2">
                  Ders Yükü: <strong class="text-blue-8">{{ wlTeachingTotal }}</strong>
                  &nbsp;+&nbsp; Şeflik: <strong class="text-purple-8">{{ wlSupervisorTotal }}</strong>
                  &nbsp;=&nbsp; <strong class="text-teal-8 text-h6">HAVUZ: {{ wlPoolTotal }} saat</strong>
                </div>
                <q-space />
                <q-btn
                  color="teal"
                  icon="save"
                  label="Yapılandırmayı Kaydet"
                  :loading="workloadSaving"
                  :disable="periodStore.isReadOnly"
                  @click="saveWorkloadConfig"
                />
              </div>
            </q-card-section>
          </q-card>

          <!-- Kart 2: İşletme Saatleri -->
          <q-card flat bordered>
            <q-card-section>
              <div class="text-subtitle1 text-weight-medium q-mb-md">
                İşletme Takdir Edilen Saatler
              </div>

              <!-- Uyarı Banner'ları -->
              <q-banner v-if="hoursPoolOverLimit" rounded class="bg-red-1 text-red-9 q-mb-md">
                <template #avatar><q-icon name="error" color="red-7" /></template>
                Toplam takdir edilen saat ({{ hoursTotalAssigned }}) ders yükü havuzunu ({{ workloadConfig?.totalWorkloadPool ?? '—' }}) aşıyor!
              </q-banner>
              <q-banner v-if="hoursOverLimit" rounded class="bg-red-1 text-red-9 q-mb-md">
                <template #avatar><q-icon name="error" color="red-7" /></template>
                Toplam takdir edilen saat ({{ hoursTotalAssigned }}) toplam verilebilir saati ({{ hoursTotalAvailable }}) aşıyor!
              </q-banner>
              <q-banner v-else-if="hoursNearLimit" rounded class="bg-orange-1 text-orange-9 q-mb-md">
                <template #avatar><q-icon name="warning" color="orange-7" /></template>
                Toplam takdir edilen saat verilebilir saate yaklaşıyor: {{ hoursTotalAssigned }} / {{ hoursTotalAvailable }}
              </q-banner>

              <q-markup-table flat bordered separator="cell" class="q-mb-md">
                <thead>
                  <tr class="bg-grey-2">
                    <th class="text-left">İşletme</th>
                    <th class="text-center" style="width: 100px">Mesafe</th>
                    <th class="text-center" style="width: 100px">Öğrenci</th>
                    <th class="text-center" style="width: 120px">Verilebilir Maks.</th>
                    <th class="text-center" style="width: 140px">Takdir Edilen</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="biz in assignments" :key="biz.businessId">
                    <td class="text-left">{{ biz.businessName }}</td>
                    <td class="text-center text-caption">
                      {{ biz.distanceToSchoolKm != null ? `${biz.distanceToSchoolKm.toFixed(1)} km` : '—' }}
                    </td>
                    <td class="text-center">{{ biz.activeStudentCount }}</td>
                    <td class="text-center text-weight-medium text-green-8">{{ biz.maxCoordinationHours }}</td>
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
                  Σ Maks: <strong class="text-green-8">{{ hoursTotalAvailable }}</strong>
                  &nbsp;|&nbsp; Σ Takdir: <strong :class="hoursOverLimit ? 'text-red-8' : 'text-blue-8'">{{ hoursTotalAssigned }}</strong>
                  <template v-if="workloadConfig">
                    &nbsp;|&nbsp; Havuz: <strong class="text-teal-8">{{ workloadConfig.totalWorkloadPool }}</strong>
                  </template>
                  &nbsp;|&nbsp; Kalan: <strong class="text-orange-8">{{ hoursRemaining }}</strong>
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
                  <q-badge v-if="changedHoursCount > 0" color="red" floating>{{ changedHoursCount }}</q-badge>
                </q-btn>
              </div>
            </q-card-section>
          </q-card>
        </q-tab-panel>

        <!-- ── Tab: İşletme Dağıtımı ── -->
        <q-tab-panel name="assignment" class="q-pa-none">
          <!-- Ana İçerik: 2 Sütun Layout -->
          <div class="row q-col-gutter-md">
            <!-- Sol Panel: Atanmamış İşletmeler -->
            <div class="col-12 col-md-3">
              <q-card flat bordered class="sticky-panel">
                <q-card-section class="q-pb-none">
                  <div class="text-subtitle1 text-weight-medium q-mb-sm">
                    Atanmamış İşletmeler
                    <q-badge color="orange-7" class="q-ml-sm">{{ unassignedBusinesses.length }}</q-badge>
                  </div>
                  <q-input
                    v-model="businessSearch"
                    dense
                    filled
                    placeholder="İşletme ara..."
                    clearable
                    class="q-mb-sm"
                  >
                    <template #prepend>
                      <q-icon name="search" size="xs" />
                    </template>
                  </q-input>
                </q-card-section>

                <q-card-section class="q-pt-sm business-list-container">
                  <div v-if="loading" class="text-center q-pa-md">
                    <q-spinner color="primary" size="2em" />
                  </div>

                  <div
                    v-else-if="filteredUnassigned.length === 0"
                    class="text-center q-pa-md text-grey-6"
                  >
                    <q-icon name="check_circle" size="2em" class="q-mb-sm" />
                    <div class="text-caption">Tüm işletmeler atanmış</div>
                  </div>

                  <div v-else class="business-card-list">
                    <div
                      v-for="biz in filteredUnassigned"
                      :key="biz.businessId"
                      class="business-card"
                      :class="{ 'business-card--disabled': periodStore.isReadOnly }"
                      :draggable="!periodStore.isReadOnly"
                      @dragstart="onBusinessDragStart($event, biz)"
                    >
                      <div class="row items-center no-wrap">
                        <q-icon name="business" size="18px" color="orange-7" class="q-mr-sm" />
                        <div class="col">
                          <div class="text-body2 text-weight-medium ellipsis">{{ biz.businessName }}</div>
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
                        <q-badge color="blue-7" :label="`${biz.activeStudentCount} öğrenci`" dense />
                      </div>
                    </div>
                  </div>
                </q-card-section>
              </q-card>
            </div>

            <!-- Sağ Panel: Grid + Özet -->
            <div class="col-12 col-md-9">
              <!-- Özet Kartları -->
              <div class="row q-col-gutter-sm q-mb-md">
                <div class="col-6 col-sm-3">
                  <q-card flat bordered>
                    <q-card-section class="text-center q-pa-sm">
                      <div class="text-caption text-grey-7">Verilebilir Saat</div>
                      <div class="text-h5 text-green-8">{{ summary.totalAvailableHours }}</div>
                    </q-card-section>
                  </q-card>
                </div>
                <div class="col-6 col-sm-3">
                  <q-card flat bordered>
                    <q-card-section class="text-center q-pa-sm">
                      <div class="text-caption text-grey-7">Dağıtılan Saat</div>
                      <div class="text-h5" :class="isOverLimit ? 'text-red-8' : 'text-blue-8'">
                        {{ summary.totalAssignedHours }}
                      </div>
                    </q-card-section>
                  </q-card>
                </div>
                <div class="col-6 col-sm-3">
                  <q-card flat bordered>
                    <q-card-section class="text-center q-pa-sm">
                      <div class="text-caption text-grey-7">Kalan Saat</div>
                      <div class="text-h5 text-orange-8">{{ summary.remainingHours }}</div>
                    </q-card-section>
                  </q-card>
                </div>
                <div class="col-6 col-sm-3">
                  <q-card flat bordered>
                    <q-card-section class="text-center q-pa-sm">
                      <div class="text-caption text-grey-7">Atanmış / Toplam</div>
                      <div class="text-h5 text-purple-8">
                        {{ summary.assignedBusinessCount }} / {{ summary.assignedBusinessCount + summary.unassignedBusinessCount }}
                      </div>
                    </q-card-section>
                  </q-card>
                </div>
              </div>

              <!-- Öğretmen Ders Programı Grid -->
              <q-card v-if="selectedTeacherId && periodCount > 0" flat bordered class="q-mb-md">
                <q-card-section>
                  <div class="text-subtitle1 text-weight-medium q-mb-sm">
                    {{ selectedTeacherName }} — Ders Programı
                  </div>

                  <div v-if="scheduleLoading" class="text-center q-pa-lg">
                    <q-spinner color="primary" size="2em" />
                    <div class="text-caption text-grey-6 q-mt-sm">Program yükleniyor...</div>
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
              <q-banner
                v-else-if="!selectedTeacherId"
                rounded
                class="bg-blue-1 text-blue-9 q-mb-md"
              >
                <template #avatar>
                  <q-icon name="person_search" color="blue-7" />
                </template>
                Grid üzerinde atama yapmak için bir öğretmen seçin. Soldan işletme kartını sürükleyip grid üzerindeki boş saate bırakın.
              </q-banner>

              <!-- Atanmış İşletmeler Listesi (seçili öğretmen için) -->
              <q-card v-if="selectedTeacherId && assignedToTeacher.length > 0" flat bordered class="q-mb-md">
                <q-card-section>
                  <div class="text-subtitle1 text-weight-medium q-mb-sm">
                    Atanmış İşletmeler
                    <q-badge color="blue-7" class="q-ml-sm">{{ assignedToTeacher.length }}</q-badge>
                  </div>
                  <q-list dense separator>
                    <q-item v-for="biz in assignedToTeacher" :key="biz.businessId">
                      <q-item-section avatar>
                        <q-icon name="business" color="blue-7" />
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
                      </q-item-section>
                    </q-item>
                  </q-list>
                </q-card-section>
              </q-card>

              <!-- Öğretmen Özet Tablosu -->
              <q-card flat bordered>
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
                          <q-td class="text-center">{{ totalTeacherBusinessCount }}</q-td>
                          <q-td class="text-center">{{ summary.totalAssignedHours }}</q-td>
                        </q-tr>
                      </template>
                    </q-table>
                  </q-card-section>
                </q-expansion-item>
              </q-card>
            </div>
          </div>

          <!-- Limit Aşıldı Uyarı -->
          <q-banner
            v-if="isOverLimit"
            rounded
            class="bg-red-1 text-red-9 q-mt-md"
          >
            <template #avatar>
              <q-icon name="error" color="red-7" />
            </template>
            Toplam dağıtılan saat ({{ summary.totalAssignedHours }}) toplam verilebilir saati ({{ summary.totalAvailableHours }}) aşıyor.
          </q-banner>
        </q-tab-panel>

        <!-- ── Tab 2: Öğretmen Özeti ── -->
        <q-tab-panel name="teachers" class="q-pa-none">
          <div v-if="teacherOverviewLoading" class="text-center q-pa-xl">
            <q-spinner color="primary" size="3em" />
            <div class="text-caption text-grey-6 q-mt-sm">Öğretmen verileri yükleniyor...</div>
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
                  <FreeSlotChip :free="row.freeSlotsByDay['Monday']" :total="totalSlotsByDay(row, 'Monday')" />
                </q-td>
              </template>
              <template #body-cell-tuesday="{ row }">
                <q-td class="text-center">
                  <FreeSlotChip :free="row.freeSlotsByDay['Tuesday']" :total="totalSlotsByDay(row, 'Tuesday')" />
                </q-td>
              </template>
              <template #body-cell-wednesday="{ row }">
                <q-td class="text-center">
                  <FreeSlotChip :free="row.freeSlotsByDay['Wednesday']" :total="totalSlotsByDay(row, 'Wednesday')" />
                </q-td>
              </template>
              <template #body-cell-thursday="{ row }">
                <q-td class="text-center">
                  <FreeSlotChip :free="row.freeSlotsByDay['Thursday']" :total="totalSlotsByDay(row, 'Thursday')" />
                </q-td>
              </template>
              <template #body-cell-friday="{ row }">
                <q-td class="text-center">
                  <FreeSlotChip :free="row.freeSlotsByDay['Friday']" :total="totalSlotsByDay(row, 'Friday')" />
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
                  <q-icon name="people" size="2em" class="q-mb-sm" />
                  <div>Öğretmen verisi bulunamadı. Önce alan seçin ve veri yükleyin.</div>
                </div>
              </template>
            </q-table>
          </div>
        </q-tab-panel>

        <!-- ── Tab 3: Harita ── -->
        <q-tab-panel name="map" class="q-pa-none">
          <!-- Harita araç çubuğu -->
          <div class="row items-center q-gutter-sm q-mb-md">
            <q-input
              v-model.number="clusterEps"
              type="number"
              label="Yarıçap (m)"
              filled
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
              filled
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
            <q-separator vertical inset class="q-mx-sm" />
            <q-btn
              color="teal"
              icon="route"
              label="Mesafe Hesapla"
              :loading="recalculating"
              :disable="periodStore.isReadOnly"
              @click="recalculateDistances"
            />
          </div>

          <div v-if="clusterLoading" class="text-center q-pa-xl">
            <q-spinner color="primary" size="3em" />
            <div class="text-caption text-grey-6 q-mt-sm">İşletme kümeleri yükleniyor...</div>
          </div>

          <div v-else-if="clusterError" class="text-center q-pa-xl text-grey-6">
            <q-icon name="warning" size="3em" color="orange-6" class="q-mb-sm" />
            <div>Kümeleme verisi yüklenemedi.</div>
            <div class="text-caption q-mt-sm">PostGIS eklentisi henüz etkin olmayabilir. Sistem yöneticisi ile iletişime geçin.</div>
          </div>

          <div v-else-if="clusterData.length === 0" class="text-center q-pa-xl text-grey-6">
            <q-icon name="location_off" size="3em" class="q-mb-sm" />
            <div>Konum verisi olan işletme bulunamadı.</div>
            <div class="text-caption q-mt-sm">İşletmelere koordinat atandıktan sonra harita burada gösterilecek.</div>
          </div>

          <div v-else>
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
              height="600px"
            />
          </div>
        </q-tab-panel>
      </q-tab-panels>
    </div>

    <!-- Kaydedilmemiş Değişiklik Onay Dialogu -->
    <q-dialog v-model="showDiscardDialog" persistent>
      <q-card style="min-width: 350px">
        <q-card-section>
          <div class="text-h6">Kaydedilmemiş Değişiklikler</div>
        </q-card-section>
        <q-card-section>
          {{ pendingChanges.length }} adet kaydedilmemiş değişiklik var. Öğretmen değiştirirseniz bu değişiklikler kaybolacak.
        </q-card-section>
        <q-card-actions align="right">
          <q-btn flat label="İptal" color="grey-7" @click="showDiscardDialog = false" />
          <q-btn flat label="Değişiklikleri At" color="red" @click="confirmDiscard" />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, defineComponent, h } from 'vue'
import {
  coordinationApi,
  type BusinessAssignmentDto,
  type CoordinationSummaryDto,
  type DailyScheduleDto,
  type TeacherSummaryRowDto,
  type TeacherWorkloadSummaryDto,
  type BusinessClusterDto,
  type BranchWorkloadConfigDto,
} from 'src/api/coordination'
import { enrollmentApi } from 'src/api/enrollment'
import { institutionApi } from 'src/api/institution'
import { useTeacherOptions, useBranchOptions } from 'src/composables/useEntityOptions'
import { useNotify } from 'src/composables/useNotify'
import { useAuthStore } from 'stores/auth'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import AssignmentGrid from 'components/AssignmentGrid.vue'
import BusinessClusterMap from 'components/BusinessClusterMap.vue'

const notify = useNotify()
const authStore = useAuthStore()
const periodStore = useAcademicPeriodStore()
const teacherOpts = useTeacherOptions()
const branchOpts = useBranchOptions()

// ── Tab ──
const activeTab = ref('assignment')

// ── State ──
const branchFilter = ref<string | null>(null)
const selectedTeacherId = ref<string | null>(null)
const businessSearch = ref('')
const loading = ref(false)
const saving = ref(false)
const recalculating = ref(false)
const scheduleLoading = ref(false)
const periodCount = ref(0)
const scheduleConfigMissing = ref(false)

const showDiscardDialog = ref(false)
let pendingTeacherId: string | null = null

const assignments = ref<BusinessAssignmentDto[]>([])
const rawSchedule = ref<DailyScheduleDto[]>([])

const summary = ref<CoordinationSummaryDto>({
  totalAvailableHours: 0,
  totalAssignedHours: 0,
  remainingHours: 0,
  assignedBusinessCount: 0,
  unassignedBusinessCount: 0,
  teacherWorkloads: [],
})

// ── Teacher Overview State ──
const teacherOverviewRows = ref<TeacherSummaryRowDto[]>([])
const teacherOverviewLoading = ref(false)

// ── Cluster Map State ──
const clusterData = ref<BusinessClusterDto[]>([])
const clusterLoading = ref(false)
const clusterError = ref(false)
const clusterEps = ref(1000) // yarıçap metre
const clusterMinPoints = ref(3)

// ── Takdir Edilen Saat Düzenleme ──
const hoursSaving = ref(false)
const editedHours = ref<Record<string, number>>({}) // businessId → editedAssignedHours

function initEditedHours() {
  const map: Record<string, number> = {}
  for (const a of assignments.value) {
    map[a.businessId] = a.assignedHours > 0 ? a.assignedHours : a.maxCoordinationHours
  }
  editedHours.value = map
}

const hoursTotalAvailable = computed(() =>
  assignments.value.reduce((sum, a) => sum + a.maxCoordinationHours, 0),
)
const hoursTotalAssigned = computed(() =>
  Object.values(editedHours.value).reduce((sum, h) => sum + h, 0),
)
const hoursRemaining = computed(() => hoursTotalAvailable.value - hoursTotalAssigned.value)
const hoursOverLimit = computed(() => hoursTotalAssigned.value > hoursTotalAvailable.value)
const hoursNearLimit = computed(() =>
  !hoursOverLimit.value && hoursTotalAssigned.value > hoursTotalAvailable.value * 0.9,
)

const changedHoursCount = computed(() => {
  let count = 0
  for (const a of assignments.value) {
    const current = a.assignedHours > 0 ? a.assignedHours : a.maxCoordinationHours
    if (editedHours.value[a.businessId] !== current) count++
  }
  return count
})

async function saveHours() {
  hoursSaving.value = true
  let successCount = 0
  const errors: string[] = []

  for (const a of assignments.value) {
    const current = a.assignedHours > 0 ? a.assignedHours : a.maxCoordinationHours
    const edited = editedHours.value[a.businessId]
    if (edited === undefined || edited === current) continue

    try {
      await coordinationApi.updateAssignedHours(a.businessId, { assignedHours: edited })
      successCount++
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : 'Bilinmeyen hata'
      errors.push(`${a.businessName}: ${msg}`)
    }
  }

  hoursSaving.value = false

  if (successCount > 0) {
    notify.success(`${successCount} işletmenin takdir edilen saati güncellendi.`)
    await loadData()
    initEditedHours()
  }
  if (errors.length > 0) {
    notify.warning(`Hatalar: ${errors.join(', ')}`)
  }
}

// ── Branch Workload Config ──
const workloadConfig = ref<BranchWorkloadConfigDto | null>(null)
const workloadLoading = ref(false)
const workloadSaving = ref(false)
const syncingCounts = ref(false)

// Editable form state
const wlEducationType = ref('Formal')
const wlDeptHeadCount = ref(1)
const wlWorkshopHeadCount = ref(0)
const wlDeptHeadHours = ref(10)
const wlWorkshopHeadHours = ref(6)
const wlClassLevels = ref<{ classYear: number; weeklyLessonHours: number; studentCount: number }[]>([
  { classYear: 10, weeklyLessonHours: 8, studentCount: 0 },
  { classYear: 11, weeklyLessonHours: 8, studentCount: 0 },
  { classYear: 12, weeklyLessonHours: 8, studentCount: 0 },
])

const EDUCATION_TYPES = [
  { label: 'Örgün', value: 'Formal' },
  { label: 'MESEM', value: 'Mesem' },
]

async function loadWorkloadConfig() {
  if (!branchFilter.value || !periodStore.selectedPeriodId) return
  workloadLoading.value = true
  try {
    const res = await coordinationApi.getBranchWorkloadConfig(
      branchFilter.value,
      periodStore.selectedPeriodId,
      wlEducationType.value,
    )
    const data = res.data
    if (data && data.id) {
      workloadConfig.value = data
      wlEducationType.value = data.educationType
      wlDeptHeadCount.value = data.departmentHeadCount
      wlWorkshopHeadCount.value = data.workshopHeadCount
      wlDeptHeadHours.value = data.departmentHeadHours
      wlWorkshopHeadHours.value = data.workshopHeadHours
      wlClassLevels.value = data.classLevels.map(cl => ({
        classYear: cl.classYear,
        weeklyLessonHours: cl.weeklyLessonHours,
        studentCount: cl.studentCount,
      }))

      // Tüm sınıf sayıları 0 ise BranchStudentCountView henüz doldurulmamış — otomatik senkronize et
      const allZero = wlClassLevels.value.every(cl => cl.studentCount === 0)
      if (allZero && !syncingCounts.value) {
        await doAutoSync()
      }
    } else {
      workloadConfig.value = null
    }
  } catch {
    workloadConfig.value = null
  } finally {
    workloadLoading.value = false
  }
}

async function saveWorkloadConfig() {
  if (!branchFilter.value || !periodStore.selectedPeriodId) return
  workloadSaving.value = true
  try {
    await coordinationApi.upsertBranchWorkloadConfig(branchFilter.value, {
      academicPeriodId: periodStore.selectedPeriodId,
      educationType: wlEducationType.value,
      departmentHeadCount: wlDeptHeadCount.value,
      workshopHeadCount: wlWorkshopHeadCount.value,
      departmentHeadHours: wlDeptHeadHours.value,
      workshopHeadHours: wlWorkshopHeadHours.value,
      classLevels: wlClassLevels.value.map(cl => ({
        classYear: cl.classYear,
        weeklyLessonHours: cl.weeklyLessonHours,
      })),
    })
    notify.success('Alan ders yükü yapılandırması kaydedildi.')
    await loadWorkloadConfig()
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : 'Bilinmeyen hata'
    notify.error(`Kaydetme hatası: ${msg}`)
  } finally {
    workloadSaving.value = false
  }
}

async function doAutoSync() {
  const instId = authStore.user?.institutionId
  if (!instId || !periodStore.selectedPeriodId) return
  syncingCounts.value = true
  try {
    await enrollmentApi.syncStudentCounts(instId, periodStore.selectedPeriodId)
    // Event async işlenecek, kısa bekleme sonrası yeniden yükle
    await new Promise(resolve => setTimeout(resolve, 1500))
    const res = await coordinationApi.getBranchWorkloadConfig(
      branchFilter.value!,
      periodStore.selectedPeriodId,
      wlEducationType.value,
    )
    const data = res.data
    if (data && data.id) {
      wlClassLevels.value = data.classLevels.map(cl => ({
        classYear: cl.classYear,
        weeklyLessonHours: cl.weeklyLessonHours,
        studentCount: cl.studentCount,
      }))
      workloadConfig.value = data
    }
  } catch {
    // Sessizce başarısız — kullanıcı manuel butonla deneyebilir
  } finally {
    syncingCounts.value = false
  }
}

async function syncStudentCounts() {
  const instId = authStore.user?.institutionId
  if (!instId || !periodStore.selectedPeriodId) return
  syncingCounts.value = true
  try {
    await enrollmentApi.syncStudentCounts(instId, periodStore.selectedPeriodId)
    notify.success('Öğrenci sayıları senkronize edildi. Veriler güncelleniyor...')
    // Event async işlenecek, kısa bekleme sonrası yeniden yükle
    setTimeout(() => loadWorkloadConfig(), 1500)
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : 'Bilinmeyen hata'
    notify.error(`Senkronizasyon hatası: ${msg}`)
  } finally {
    syncingCounts.value = false
  }
}

// Computed values for local preview (before save)
const wlSupervisorTotal = computed(() =>
  (wlDeptHeadCount.value * wlDeptHeadHours.value) + (wlWorkshopHeadCount.value * wlWorkshopHeadHours.value),
)

const wlTeachingTotal = computed(() =>
  wlClassLevels.value.reduce((sum, cl) => {
    const groups = estimateGroupCount(wlEducationType.value, cl.classYear, cl.studentCount)
    return sum + cl.weeklyLessonHours * groups
  }, 0),
)

const wlPoolTotal = computed(() => wlSupervisorTotal.value + wlTeachingTotal.value)

// Frontend grup hesaplama (Madde 22 mirror)
function estimateGroupCount(educationType: string, classYear: number, studentCount: number): number {
  if (studentCount <= 0) return 0
  if (educationType === 'Mesem') {
    if (studentCount < 10) return 0
    if (studentCount < 41) return 1
    if (studentCount < 81) return 2
    if (studentCount < 121) return 3
    if (studentCount < 161) return 4
    if (studentCount < 201) return 5
    if (studentCount < 241) return 6
    if (studentCount < 281) return 7
    if (studentCount < 321) return 8
    if (studentCount < 361) return 9
    if (studentCount < 401) return 10
    if (studentCount < 441) return 11
    return 12
  }
  // Formal
  if (classYear === 9) {
    if (studentCount < 10) return 0
    if (studentCount < 21) return 1
    if (studentCount < 31) return 2
    return 3
  }
  if (classYear >= 10 && classYear <= 12) {
    if (studentCount < 8) return 0
    if (studentCount < 17) return 1
    if (studentCount < 25) return 2
    if (studentCount < 33) return 3
    return 4
  }
  return 0
}

// Havuz kısıtı: hoursTotalAssigned vs wlPoolTotal
const hoursPoolOverLimit = computed(() => {
  if (!workloadConfig.value) return false
  return hoursTotalAssigned.value > workloadConfig.value.totalWorkloadPool
})

// ── Pending Changes ──
interface PendingChange {
  type: 'assign' | 'unassign'
  businessId: string
  businessName: string
  day: string
  periodNumber: number
}

const pendingChanges = ref<PendingChange[]>([])

// ── Teacher filter ──
const teacherFilterNeedle = ref('')

const filteredTeacherOpts = computed(() => {
  const needle = teacherFilterNeedle.value.toLowerCase()
  let opts = [...teacherOpts.allOptions.value]
  if (needle) {
    opts = opts.filter((o) => o.label.toLowerCase().includes(needle))
  }
  // Cross-branch sıralama: kendi alan öğretmenleri üstte
  if (branchFilter.value && !authStore.isDepartmentHead) {
    opts.sort((a, b) => {
      const aOwn = (a as { branchCode?: string }).branchCode === branchFilter.value ? 0 : 1
      const bOwn = (b as { branchCode?: string }).branchCode === branchFilter.value ? 0 : 1
      return aOwn !== bOwn ? aOwn - bOwn : a.label.localeCompare(b.label, 'tr')
    })
  }
  return opts
})

function onTeacherFilter(val: string, update: (fn: () => void) => void) {
  update(() => {
    teacherFilterNeedle.value = val
  })
}

// ── Computed: İşletme Ad Haritası ──
const businessNameMap = computed(() => {
  const map: Record<string, string> = {}
  for (const a of assignments.value) {
    map[a.businessId] = a.businessName
  }
  return map
})

// ── Computed: Atanmamış / Kısmi Atanmış İşletmeler ──
const unassignedBusinesses = computed(() => {
  const result: BusinessAssignmentDto[] = []

  for (const biz of assignments.value) {
    const targetHours = biz.assignedHours > 0 ? biz.assignedHours : biz.maxCoordinationHours
    const backendSlots = biz.assignedSlots?.length ?? 0

    // Pending değişiklikleri hesapla
    const pendingAssigns = pendingChanges.value.filter(
      (c) => c.businessId === biz.businessId && c.type === 'assign',
    ).length
    const pendingUnassigns = pendingChanges.value.filter(
      (c) => c.businessId === biz.businessId && c.type === 'unassign',
    ).length
    const effectiveSlots = backendSlots + pendingAssigns - pendingUnassigns

    // Hedef saate ulaşmamış → sol panelde göster (sürüklenebilir)
    if (effectiveSlots < targetHours) {
      result.push(biz)
    }
  }

  return result
})

const filteredUnassigned = computed(() => {
  if (!businessSearch.value) return unassignedBusinesses.value
  const needle = businessSearch.value.toLocaleLowerCase('tr')
  return unassignedBusinesses.value.filter(
    (b) =>
      b.businessName.toLocaleLowerCase('tr').includes(needle) ||
      (b.district?.toLocaleLowerCase('tr').includes(needle) ?? false),
  )
})

// ── Computed: Seçili öğretmene atanmış işletmeler ──
const assignedToTeacher = computed(() => {
  if (!selectedTeacherId.value) return []

  const pendingUnassignIds = new Set(
    pendingChanges.value.filter((c) => c.type === 'unassign').map((c) => c.businessId),
  )

  // Backend'den gelen + pending assign - pending unassign
  const base = assignments.value.filter(
    (a) =>
      a.assignedTeacherId === selectedTeacherId.value && !pendingUnassignIds.has(a.businessId),
  )

  // Pending assign'ları da ekle (henüz backend'e gitmemiş)
  for (const pc of pendingChanges.value) {
    if (pc.type === 'assign' && !base.find((b) => b.businessId === pc.businessId)) {
      const original = assignments.value.find((a) => a.businessId === pc.businessId)
      if (original) {
        base.push({
          ...original,
          assignedTeacherId: selectedTeacherId.value,
          assignedDay: pc.day,
          assignedPeriodNumber: pc.periodNumber,
        })
      }
    }
  }

  return base
})

// ── Computed: Effective Schedule (raw + pending changes) ──
const effectiveSchedule = computed((): DailyScheduleDto[] => {
  // Deep clone
  const schedule: DailyScheduleDto[] = JSON.parse(JSON.stringify(rawSchedule.value))

  for (const change of pendingChanges.value) {
    const daySchedule = schedule.find((d) => d.day === change.day)
    if (!daySchedule) continue
    const slot = daySchedule.periods.find((p) => p.periodNumber === change.periodNumber)
    if (!slot) continue

    if (change.type === 'assign') {
      slot.assignedBusinessId = change.businessId
    } else if (change.type === 'unassign') {
      slot.assignedBusinessId = null
    }
  }

  return schedule
})

// ── Computed: Summary ──
const isOverLimit = computed(
  () => summary.value.totalAssignedHours > summary.value.totalAvailableHours,
)

const totalTeacherBusinessCount = computed(() =>
  summary.value.teacherWorkloads.reduce((sum: number, tw: TeacherWorkloadSummaryDto) => sum + tw.businessCount, 0),
)

const selectedTeacherName = computed(() => {
  if (!selectedTeacherId.value) return ''
  return teacherOpts.allOptions.value.find((o) => o.value === selectedTeacherId.value)?.label ?? ''
})

const branchLabel = computed(() => {
  if (!branchFilter.value) return ''
  return (
    branchOpts.allOptions?.value.find((o: { value: string; label: string }) => o.value === branchFilter.value)?.label ??
    branchFilter.value
  )
})

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

// ── Teacher Overview Columns ──
const teacherOverviewColumns = [
  { name: 'teacherName', label: 'Öğretmen', field: 'teacherId', align: 'left' as const, sortable: false },
  { name: 'businessCount', label: 'İşletme', field: 'businessCount', align: 'center' as const, sortable: true },
  { name: 'assignedHours', label: 'Saat', field: 'assignedHours', align: 'center' as const, sortable: true },
  { name: 'monday', label: 'Pzt', field: 'freeSlotsByDay', align: 'center' as const, sortable: false },
  { name: 'tuesday', label: 'Sal', field: 'freeSlotsByDay', align: 'center' as const, sortable: false },
  { name: 'wednesday', label: 'Çar', field: 'freeSlotsByDay', align: 'center' as const, sortable: false },
  { name: 'thursday', label: 'Per', field: 'freeSlotsByDay', align: 'center' as const, sortable: false },
  { name: 'friday', label: 'Cum', field: 'freeSlotsByDay', align: 'center' as const, sortable: false },
  { name: 'workload', label: 'Yük', field: 'assignedHours', align: 'center' as const, sortable: false },
]

function teacherName(teacherId: string): string {
  return teacherOpts.allOptions.value.find((o) => o.value === teacherId)?.label ?? teacherId
}

// Öğretmen overview için toplam serbest slot sayısı (atanmış dahil)
function totalSlotsByDay(row: TeacherSummaryRowDto, day: string): number {
  // TeacherSummaryRowDto'da totalSlotsByDay yok, sadece freeSlotsByDay var
  // Boş slot sayısı olarak göster
  return row.freeSlotsByDay[day] ?? 0
}

// ── Inline Components ──

// FreeSlotChip: boş slot sayısını renkli chip olarak gösterir
const FreeSlotChip = defineComponent({
  props: { free: { type: Number, default: 0 }, total: { type: Number, default: 0 } },
  setup(props) {
    return () => {
      const free = props.free ?? 0
      const color = free === 0 ? 'red-2' : free <= 1 ? 'orange-2' : 'green-2'
      const textColor = free === 0 ? 'red-9' : free <= 1 ? 'orange-9' : 'green-9'
      return h(
        'span',
        {
          class: [`bg-${color}`, `text-${textColor}`, 'q-px-xs', 'rounded-borders', 'text-caption', 'text-weight-medium'],
          style: 'min-width: 24px; display: inline-block; text-align: center',
        },
        free > 0 ? String(free) : '—',
      )
    }
  },
})

// WorkloadIndicator: atanmış saat renk göstergesi
const WorkloadIndicator = defineComponent({
  props: { assignedHours: { type: Number, default: 0 }, availableHours: { type: Number, default: 0 } },
  setup(props) {
    return () => {
      const hours = props.assignedHours
      const color = hours === 0 ? 'grey-4' : hours <= 4 ? 'green-7' : hours <= 8 ? 'orange-7' : 'red-7'
      return h(
        'span',
        {
          class: [`text-${color}`, 'text-weight-bold', 'text-caption'],
        },
        `${hours}s`,
      )
    }
  },
})

// ── Cluster Map ──

const CLUSTER_COLORS = [
  '#e53935', '#8e24aa', '#1e88e5', '#00897b', '#43a047',
  '#f4511e', '#6d4c41', '#546e7a', '#c0ca33', '#00acc1',
  '#5e35b1', '#d81b60', '#039be5', '#00e676', '#ffb300',
  '#fb8c00', '#f06292', '#4db6ac', '#9575cd', '#64b5f6',
]

function clusterColor(clusterId: number | null): string {
  if (clusterId === null) return '#9e9e9e'
  return CLUSTER_COLORS[clusterId % CLUSTER_COLORS.length] ?? '#9e9e9e'
}

const clusterCounts = computed(() => {
  const counts: Record<string, number> = {}
  for (const b of clusterData.value) {
    const key = b.clusterId === null ? 'null' : String(b.clusterId)
    counts[key] = (counts[key] ?? 0) + 1
  }
  // Sort: numbered clusters first, then 'null'
  const sorted: Record<string, number> = {}
  Object.keys(counts)
    .sort((a, b) => {
      if (a === 'null') return 1
      if (b === 'null') return -1
      return Number(a) - Number(b)
    })
    .forEach((k) => {
      sorted[k] = counts[k]!
    })
  return sorted
})

// ── Slot Progress Helper ──

function slotProgress(biz: BusinessAssignmentDto): { current: number; target: number } {
  const target = biz.assignedHours > 0 ? biz.assignedHours : biz.maxCoordinationHours
  const backendSlots = biz.assignedSlots?.length ?? 0
  const pendingAssigns = pendingChanges.value.filter(
    (c) => c.businessId === biz.businessId && c.type === 'assign',
  ).length
  const pendingUnassigns = pendingChanges.value.filter(
    (c) => c.businessId === biz.businessId && c.type === 'unassign',
  ).length
  return { current: backendSlots + pendingAssigns - pendingUnassigns, target }
}

// ── DnD Event Handlers ──

function onBusinessDragStart(event: DragEvent, biz: BusinessAssignmentDto) {
  if (!event.dataTransfer) return
  event.dataTransfer.setData('application/business-id', biz.businessId)
  event.dataTransfer.effectAllowed = 'move'

  // Drag preview style
  const target = event.target as HTMLElement
  target.classList.add('business-card--dragging')
  setTimeout(() => target.classList.remove('business-card--dragging'), 0)
}

function onBusinessDropped(payload: { businessId: string; day: string; periodNumber: number }) {
  const biz = assignments.value.find((a) => a.businessId === payload.businessId)
  if (!biz) return

  // Aynı slot'a tekrar bırakılmışsa — ignore
  const existing = pendingChanges.value.find(
    (c) =>
      c.businessId === payload.businessId &&
      c.type === 'assign' &&
      c.day === payload.day &&
      c.periodNumber === payload.periodNumber,
  )
  if (existing) return

  // Hedef saat: takdir edilen saat > 0 ise onu kullan, yoksa verilebilir saat
  const targetHours = biz.assignedHours > 0 ? biz.assignedHours : biz.maxCoordinationHours

  // Mevcut atanmış slot sayısı (backend + pending)
  const backendSlots = biz.assignedSlots?.length ?? 0
  const pendingAssigns = pendingChanges.value.filter(
    (c) => c.businessId === biz.businessId && c.type === 'assign',
  ).length
  const pendingUnassigns = pendingChanges.value.filter(
    (c) => c.businessId === biz.businessId && c.type === 'unassign',
  ).length
  const currentSlots = backendSlots + pendingAssigns - pendingUnassigns

  if (currentSlots >= targetHours) {
    notify.warning(`${biz.businessName}: Tüm saatler atanmış (${currentSlots}/${targetHours}).`)
    return
  }

  pendingChanges.value.push({
    type: 'assign',
    businessId: payload.businessId,
    businessName: biz.businessName,
    day: payload.day,
    periodNumber: payload.periodNumber,
  })
}

function onBusinessRemoved(payload: { businessId: string; day: string; periodNumber: number }) {
  // Pending assign ise → sadece pending'i sil
  const pendingAssignIdx = pendingChanges.value.findIndex(
    (c) =>
      c.businessId === payload.businessId &&
      c.type === 'assign' &&
      c.day === payload.day &&
      c.periodNumber === payload.periodNumber,
  )

  if (pendingAssignIdx >= 0) {
    pendingChanges.value.splice(pendingAssignIdx, 1)
    return
  }

  // Backend'den gelen atama → unassign pending ekle
  const biz = assignments.value.find((a) => a.businessId === payload.businessId)
  if (!biz) return

  pendingChanges.value.push({
    type: 'unassign',
    businessId: payload.businessId,
    businessName: biz.businessName,
    day: payload.day,
    periodNumber: payload.periodNumber,
  })
}

function removeAssignment(biz: BusinessAssignmentDto) {
  // Multi-slot: tüm slot'ları kaldır
  if (biz.assignedSlots?.length > 0) {
    for (const slot of biz.assignedSlots) {
      onBusinessRemoved({
        businessId: biz.businessId,
        day: slot.day,
        periodNumber: slot.periodNumber,
      })
    }
  } else if (biz.assignedDay) {
    // Geriye uyumluluk: eski tek slot
    onBusinessRemoved({
      businessId: biz.businessId,
      day: biz.assignedDay,
      periodNumber: biz.assignedPeriodNumber ?? 0,
    })
  }
}

// ── Teacher Change ──

function onBranchChange() {
  selectedTeacherId.value = null
  rawSchedule.value = []
  pendingChanges.value = []
  const instId = authStore.user?.institutionId ?? undefined
  // DepartmentHead: sadece kendi alanı; Yöneticiler: tüm öğretmenler (cross-branch)
  if (authStore.isDepartmentHead) {
    void teacherOpts.reload({ institutionId: instId, branchCode: branchFilter.value ?? undefined })
  } else {
    void teacherOpts.reload({ institutionId: instId })
  }
  void loadData()
  if (activeTab.value === 'hours') void loadWorkloadConfig()
}

function onTeacherChange(teacherId: string | null) {
  if (pendingChanges.value.length > 0 && teacherId !== selectedTeacherId.value) {
    pendingTeacherId = teacherId
    showDiscardDialog.value = true
    return
  }
  doTeacherChange(teacherId)
}

function confirmDiscard() {
  showDiscardDialog.value = false
  pendingChanges.value = []
  doTeacherChange(pendingTeacherId)
  pendingTeacherId = null
}

function doTeacherChange(teacherId: string | null) {
  selectedTeacherId.value = teacherId
  rawSchedule.value = []
  if (teacherId) {
    loadTeacherSchedule(teacherId)
  }
}

function selectTeacher(teacherId: string) {
  if (pendingChanges.value.length > 0) {
    pendingTeacherId = teacherId
    showDiscardDialog.value = true
    return
  }
  doTeacherChange(teacherId)
}

// ── API: Load Data ──

async function loadScheduleConfig() {
  const instId = authStore.user?.institutionId
  if (!instId) return

  try {
    const { data } = await institutionApi.getScheduleConfig(instId)
    if (data.configured && data.dailyPeriodCount) {
      periodCount.value = data.dailyPeriodCount
      scheduleConfigMissing.value = false
    } else {
      periodCount.value = 0
      scheduleConfigMissing.value = true
    }
  } catch {
    periodCount.value = 0
    scheduleConfigMissing.value = true
  }
}

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
    initEditedHours()
  } catch (e) {
    notify.apiError(e, 'İşletme listesi yüklenirken hata oluştu.')
  } finally {
    loading.value = false
  }
}

async function loadTeacherSchedule(teacherId: string) {
  if (!periodStore.selectedPeriodId) return

  scheduleLoading.value = true
  try {
    const { data } = await coordinationApi.getCurrentSchedule(
      teacherId,
      periodStore.selectedPeriodId,
      periodStore.selectedSemester,
    )
    rawSchedule.value = data.weeklySchedule
  } catch {
    // Program henüz yok — boş grid göster
    rawSchedule.value = createEmptySchedule()
  } finally {
    scheduleLoading.value = false
  }
}

function createEmptySchedule(): DailyScheduleDto[] {
  const days = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday']
  return days.map((day) => ({
    day,
    periods: Array.from({ length: periodCount.value }, (_, i) => ({
      periodNumber: i + 1,
      status: 'Free',
      courseName: null,
      assignedBusinessId: null,
    })),
  }))
}

async function loadTeacherOverview() {
  if (!periodStore.selectedPeriodId) return

  teacherOverviewLoading.value = true
  try {
    const { data } = await coordinationApi.getAllTeachersOverview(
      periodStore.selectedPeriodId,
      periodStore.selectedSemester,
      branchFilter.value ?? undefined,
    )
    teacherOverviewRows.value = data
  } catch (e) {
    notify.apiError(e, 'Öğretmen özeti yüklenirken hata oluştu.')
  } finally {
    teacherOverviewLoading.value = false
  }
}

async function loadClusters() {
  clusterLoading.value = true
  clusterError.value = false
  try {
    const { data } = await coordinationApi.getBusinessClusters(
      clusterEps.value,
      clusterMinPoints.value,
    )
    clusterData.value = data
  } catch {
    clusterError.value = true
    clusterData.value = []
  } finally {
    clusterLoading.value = false
  }
}

// ── API: Save ──

async function saveAll() {
  if (pendingChanges.value.length === 0 || !selectedTeacherId.value) return

  saving.value = true
  let successCount = 0
  const total = pendingChanges.value.length
  const errors: string[] = []

  for (const change of [...pendingChanges.value]) {
    try {
      if (change.type === 'assign') {
        const biz = assignments.value.find((a) => a.businessId === change.businessId)
        const hours = biz?.assignedHours || biz?.maxCoordinationHours || 0
        await coordinationApi.assignBusiness({
          businessId: change.businessId,
          teacherId: selectedTeacherId.value,
          teacherName: selectedTeacherName.value,
          assignedHours: hours,
          assignedDay: change.day,
          periodNumber: change.periodNumber,
          assignedBy: authStore.user?.fullName ?? '',
        })
      } else {
        await coordinationApi.unassignBusinessSlot(
          change.businessId, change.day, change.periodNumber,
        )
      }
      successCount++
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : 'Bilinmeyen hata'
      errors.push(`${change.businessName}: ${msg}`)
    }
  }

  saving.value = false
  pendingChanges.value = []

  if (successCount === total) {
    notify.success(`${successCount} işlem başarıyla kaydedildi.`)
  } else {
    notify.warning(`${successCount}/${total} işlem kaydedildi. Hatalar: ${errors.join(', ')}`)
  }

  // Verileri yenile
  await Promise.all([
    loadData(),
    selectedTeacherId.value ? loadTeacherSchedule(selectedTeacherId.value) : Promise.resolve(),
  ])
}

async function recalculateDistances() {
  recalculating.value = true
  try {
    await coordinationApi.recalculateDistances()
    notify.success('Mesafeler yeniden hesaplandı.')
    await loadData()
  } catch (e) {
    notify.apiError(e, 'Mesafe hesaplama sırasında hata oluştu.')
  } finally {
    recalculating.value = false
  }
}

// ── Tab değişimi → lazy load ──
watch(activeTab, (tab) => {
  if (tab === 'hours') {
    initEditedHours()
    void loadWorkloadConfig()
  }
  if (tab === 'teachers' && teacherOverviewRows.value.length === 0) {
    void loadTeacherOverview()
  }
  if (tab === 'map' && clusterData.value.length === 0 && !clusterError.value) {
    void loadClusters()
  }
})

// ── Dönem değişikliği ──
watch(
  () => [periodStore.selectedPeriodId, periodStore.selectedSemester],
  () => {
    if (selectedTeacherId.value) {
      pendingChanges.value = []
      loadTeacherSchedule(selectedTeacherId.value)
    }
    // Yüklü tabları yenile
    if (activeTab.value === 'teachers') void loadTeacherOverview()
    if (activeTab.value === 'map') void loadClusters()
  },
)

// ── Init ──
onMounted(async () => {
  const instId = authStore.user?.institutionId ?? undefined

  // DepartmentHead → kendi branşını otomatik seç, sadece o branşın öğretmenlerini yükle
  if (authStore.isDepartmentHead && authStore.user?.branchCode) {
    branchFilter.value = authStore.user.branchCode
    await Promise.all([
      teacherOpts.reload({ institutionId: instId, branchCode: authStore.user.branchCode }),
      branchOpts.load(),
      loadScheduleConfig(),
    ])
    await loadData()
  } else {
    // Yöneticiler → tüm öğretmenleri yükle, alan seçimini bekle
    await Promise.all([
      teacherOpts.load({ institutionId: instId }),
      branchOpts.load(),
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
