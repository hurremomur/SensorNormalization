```vue
<template>
  <v-app>
    <v-app-bar app color="#0d1b3e" dark flat>
      <v-toolbar-title class="font-weight-bold">
        Sensor Normalizasyon Paneli
      </v-toolbar-title>

      <v-spacer></v-spacer>

      <v-chip v-if="autoRefresh" color="green" small class="mr-3" dark>
        <v-icon left small>mdi-circle</v-icon>
        CANLI
      </v-chip>

      <v-btn text small @click="toggleAutoRefresh" class="mr-2">
        <v-icon left small>
          {{ autoRefresh ? "mdi-pause" : "mdi-play" }}
        </v-icon>
        {{ autoRefresh ? "Duraklat" : "Devam" }}
      </v-btn>

      <v-btn icon @click="refreshAll" :loading="loading">
        <v-icon>mdi-refresh</v-icon>
      </v-btn>
    </v-app-bar>

    <v-main style="background-color: #eef1f6;">

      <!-- ALARM TICKER -->
      <div v-if="alarms.length > 0" class="alarm-ticker">
        <v-icon small dark left>mdi-alert</v-icon>

        <div class="ticker-track">
          <span
            v-for="(a, i) in alarms"
            :key="i"
            class="ticker-item"
          >
            {{ a.sensorId }} ({{ a.sensorType }}) ANOMALI:
            {{ a.value }} {{ a.unit }} - {{ formatTime(a.time) }}
            &nbsp;&bull;&nbsp;
          </span>

          <!-- Kesintisiz kayma için tekrar -->
          <span
            v-for="(a, i) in alarms"
            :key="'r' + i"
            class="ticker-item"
          >
            {{ a.sensorId }} ({{ a.sensorType }}) ANOMALI:
            {{ a.value }} {{ a.unit }} - {{ formatTime(a.time) }}
            &nbsp;&bull;&nbsp;
          </span>
        </div>
      </div>

      <div v-else class="alarm-ticker-ok">
        <v-icon small dark left>mdi-check-circle</v-icon>
        Tum sensorler normal araliklarda - aktif alarm yok
      </div>

      <v-container fluid class="pa-6">

        <!-- CANLI IZLEME -->
        <div class="d-flex align-center mb-1">
          <h2>Canli Izleme</h2>

          <v-spacer></v-spacer>

          <span
            class="text-caption grey--text"
            v-if="lastUpdated"
          >
            Son guncelleme:
            {{ formatTime(lastUpdated) }}
            &bull;
            {{ REFRESH_SEC }} sn'de bir
          </span>
        </div>

        <p class="text-caption grey--text mb-4">
          Farkli formatlardaki (JSON/XML/CSV) veriler tek standarda
          (birim + UTC) normalize edilir.
        </p>

        <v-alert v-if="error" type="error" dense>
          {{ error }}
        </v-alert>

        <!-- DINAMIK SENSOR KARTLARI -->
        <v-row>
          <v-col
            v-for="reading in latest"
            :key="reading.sensorId || reading.sensorType"
            cols="12"
            sm="6"
            md="3"
          >
            <v-card
              elevation="3"
              class="sensor-card"
              :class="{
                'anomaly-card': reading.isAnomaly,
                'selected-card':
                  selectedType === reading.sensorType.toLowerCase()
              }"
              @click="selectType(reading.sensorType)"
            >

              <!-- Kart basligi -->
              <div
                class="pa-4"
                :style="{
                  backgroundColor: cardColor(reading.sensorType),
                  color: 'white'
                }"
              >
                <div class="d-flex align-center">

                  <v-icon left dark>
                    {{ cardIcon(reading.sensorType) }}
                  </v-icon>

                  <span class="text-subtitle-1 font-weight-medium">
                    {{ reading.sensorType }}
                  </span>

                  <v-spacer></v-spacer>

                  <v-chip
                    x-small
                    color="white"
                    :text-color="cardColor(reading.sensorType)"
                  >
                    {{ reading.sourceFormat }}
                  </v-chip>

                </div>

                <div class="text-h4 font-weight-bold mt-3">
                  {{ reading.value }}
                </div>

                <div class="text-subtitle-2">
                  {{ reading.unit }}
                </div>

                <div class="text-caption mt-1">
                  {{ reading.sensorId }}
                </div>
              </div>

              <!-- Durum -->
              <div
                v-if="reading.isAnomaly"
                class="status-bar anomaly"
              >
                <v-icon x-small dark left>
                  mdi-alert
                </v-icon>
                ANOMALI
              </div>

              <div
                v-else
                class="status-bar normal"
              >
                <v-icon x-small left color="green">
                  mdi-check-circle
                </v-icon>
                Normal
              </div>

              <!-- Zaman -->
              <div class="pa-2 text-caption grey--text text-center">
                {{ formatTime(reading.time) }} (UTC)
              </div>

              <!-- Ham veri / normalizasyon -->
              <v-expansion-panels flat tile>
                <v-expansion-panel>

                  <v-expansion-panel-header
                    class="text-caption py-1"
                    style="min-height:32px;"
                  >
                    Ham veri / normalizasyon detayi
                  </v-expansion-panel-header>

                  <v-expansion-panel-content>

                    <div class="text-caption grey--text mb-1">
                      HAM ({{ reading.sourceFormat }})
                    </div>

                    <div class="raw-box">
                      {{ reading.rawPayload || "(yok)" }}
                    </div>

                    <div class="text-center my-1">
                      <v-icon color="grey" x-small>
                        mdi-arrow-down-bold
                      </v-icon>
                    </div>

                    <div>
                      <v-chip
                        v-for="(t, i) in transforms(reading.sensorType)"
                        :key="i"
                        x-small
                        label
                        outlined
                        class="ma-1"
                        color="deep-purple"
                      >
                        {{ t }}
                      </v-chip>

                      <!-- Yeni sensörlerde -->
                      <span
                        v-if="transforms(reading.sensorType).length === 0"
                        class="text-caption grey--text"
                      >
                        Standart normalize edilmis veri
                      </span>
                    </div>

                    <div class="norm-box mt-2">
                      <strong>
                        {{ reading.value }} {{ reading.unit }}
                      </strong>
                      &bull; UTC
                    </div>

                  </v-expansion-panel-content>

                </v-expansion-panel>
              </v-expansion-panels>

            </v-card>
          </v-col>
        </v-row>

        <!-- SENSOR YOKSA -->
        <v-alert
          v-if="!loading && latest.length === 0 && !error"
          type="info"
          outlined
          class="mt-4"
        >
          Henuz sensor verisi bulunamadi.
        </v-alert>

        <!-- GECMIS -->
        <v-divider class="my-6"></v-divider>

        <div class="d-flex align-center mb-3">

          <h2>
            Gecmis - {{ selectedTypeLabel }}
          </h2>

          <v-spacer></v-spacer>

          <!--
            BURASI ARTIK HARDCODE DEGIL.
            API'den gelen sensor tipleri otomatik olusur.
          -->
          <v-btn-toggle
            v-if="sensorTypes.length > 0"
            v-model="selectedType"
            dense
            mandatory
            @change="onTypeChange"
          >
            <v-btn
              v-for="sensor in sensorTypes"
              :key="sensor"
              small
              :value="sensor.toLowerCase()"
            >
              {{ sensor }}
            </v-btn>
          </v-btn-toggle>

        </div>

        <v-card elevation="3">

          <v-simple-table>
            <template v-slot:default>

              <thead>
                <tr>
                  <th>Zaman (UTC)</th>
                  <th>Deger</th>
                  <th>Birim</th>
                  <th>Format</th>
                  <th>Sensor</th>
                  <th>Durum</th>
                </tr>
              </thead>

              <tbody>

                <tr
                  v-for="(row, i) in history"
                  :key="i"
                  :class="{ 'anomaly-row': row.isAnomaly }"
                >
                  <td>
                    {{ formatTime(row.time) }}
                  </td>

                  <td class="font-weight-bold">
                    {{ row.value }}
                  </td>

                  <td>
                    {{ row.unit }}
                  </td>

                  <td>
                    <v-chip x-small>
                      {{ row.sourceFormat }}
                    </v-chip>
                  </td>

                  <td>
                    {{ row.sensorId }}
                  </td>

                  <td>

                    <v-chip
                      v-if="row.isAnomaly"
                      x-small
                      color="red"
                      dark
                    >
                      <v-icon x-small left>
                        mdi-alert
                      </v-icon>
                      Anomali
                    </v-chip>

                    <v-chip
                      v-else
                      x-small
                      color="green"
                      outlined
                    >
                      Normal
                    </v-chip>

                  </td>
                </tr>

                <tr v-if="history.length === 0">
                  <td
                    colspan="6"
                    class="text-center grey--text py-4"
                  >
                    Kayit yok
                  </td>
                </tr>

              </tbody>

            </template>
          </v-simple-table>

          <!-- PAGINATION -->
          <div class="d-flex align-center pa-3">

            <span class="text-caption grey--text">
              Toplam {{ totalCount }} kayit
              &bull;
              Sayfa {{ pageIndex + 1 }} / {{ totalPages }}
            </span>

            <v-spacer></v-spacer>

            <v-btn
              small
              text
              :disabled="pageIndex === 0"
              @click="prevPage"
            >
              <v-icon left small>
                mdi-chevron-left
              </v-icon>
              Onceki
            </v-btn>

            <v-btn
              small
              text
              :disabled="pageIndex + 1 >= totalPages"
              @click="nextPage"
            >
              Sonraki
              <v-icon right small>
                mdi-chevron-right
              </v-icon>
            </v-btn>

          </div>

        </v-card>

      </v-container>
    </v-main>
  </v-app>
</template>

<script>
import api from "./services/api";

const REFRESH_MS = 5000;
const PAGE_SIZE = 10;

export default {
  name: "App",

  data() {
    return {
      latest: [],
      loading: false,
      error: null,
      lastUpdated: null,

      autoRefresh: true,
      timer: null,

      REFRESH_SEC: REFRESH_MS / 1000,

      // Ilk deger gecici.
      // API'den sensorler geldikten sonra otomatik guncellenecek.
      selectedType: null,

      history: [],
      pageIndex: 0,
      pageSize: PAGE_SIZE,
      totalCount: 0
    };
  },

  computed: {

    totalPages() {
      return Math.max(
        1,
        Math.ceil(this.totalCount / this.pageSize)
      );
    },

    // Son degeri anomali olan sensorler
    alarms() {
      return this.latest.filter(
        reading => reading.isAnomaly
      );
    },

    /*
     * API'den gelen sensorType'lari otomatik olarak bulur.
     *
     * Ornegin API:
     *
     * Temperature
     * Humidity
     * Pressure
     * Light
     * Sound
     *
     * donduruyorsa burasi otomatik olarak bu listeyi olusturur.
     *
     * Yarin CO2 gelirse:
     *
     * Temperature
     * Humidity
     * Pressure
     * Light
     * Sound
     * CO2
     *
     * olur.
     *
     * Buraya CO2 eklemek gerekmiyor.
     */
    sensorTypes() {
      const types = this.latest
        .map(reading => reading.sensorType)
        .filter(type => type);

      return [...new Set(types)];
    },

    selectedTypeLabel() {
      if (!this.selectedType) {
        return "Sensor";
      }

      const selected = this.sensorTypes.find(
        sensor =>
          sensor.toLowerCase() ===
          this.selectedType.toLowerCase()
      );

      return selected || this.selectedType;
    }
  },

  mounted() {
    this.refreshAll();
    this.startAutoRefresh();
  },

  beforeDestroy() {
    this.stopAutoRefresh();
  },

  methods: {

    /*
     * Son sensor degerlerini REST API'den alir.
     *
     * GET:
     * /api/sensor-readings/latest
     */
    async fetchLatest() {
      this.loading = true;
      this.error = null;

      try {
        const response = await api.get(
          "/sensor-readings/latest"
        );

        const newLatest = Array.isArray(response.data)
          ? response.data
          : [];

        this.latest = newLatest;

        /*
         * Eger daha once secili sensor yoksa
         * ilk gelen sensoru otomatik sec.
         */
        if (
          !this.selectedType &&
          newLatest.length > 0
        ) {
          this.selectedType =
            newLatest[0].sensorType.toLowerCase();

          this.pageIndex = 0;

          // Ilk sensor belirlendikten sonra
          // onun gecmisini getir.
          await this.fetchHistory();
        }

        /*
         * Secili sensor artik API'den gelmiyorsa
         * mevcut sensorlerden ilkini sec.
         *
         * Boylece sistemde sensor degisikligi oldugunda
         * eski secim yuzunden ekran bozulmaz.
         */
        if (
          this.selectedType &&
          newLatest.length > 0 &&
          !this.sensorTypes.some(
            type =>
              type.toLowerCase() ===
              this.selectedType.toLowerCase()
          )
        ) {
          this.selectedType =
            newLatest[0].sensorType.toLowerCase();

          this.pageIndex = 0;

          await this.fetchHistory();
        }

        this.lastUpdated =
          new Date().toISOString();

      } catch (e) {
        this.error =
          "API'ye baglanilamadi: " +
          (e.response?.data?.message || e.message);
      } finally {
        this.loading = false;
      }
    },

    /*
     * Secili sensorun gecmisini REST API'den getirir.
     *
     * Ornek:
     * /api/sensor-readings/temperature/history
     */
    async fetchHistory() {

      if (!this.selectedType) {
        this.history = [];
        this.totalCount = 0;
        return;
      }

      try {

        const response = await api.get(
          "/sensor-readings/" +
          encodeURIComponent(this.selectedType) +
          "/history",
          {
            params: {
              pageIndex: this.pageIndex,
              pageSize: this.pageSize
            }
          }
        );

        this.history =
          response.data.items || [];

        this.totalCount =
          response.data.totalCount || 0;

      } catch (e) {

        console.error(
          "History API hatasi:",
          e
        );

        this.history = [];
        this.totalCount = 0;
      }
    },

    /*
     * Hem latest hem history yenilenir.
     */
    async refreshAll() {
      await this.fetchLatest();

      /*
       * fetchLatest zaten ilk sensoru buldugunda
       * history'yi getiriyor.
       *
       * Secili sensor zaten varsa burada
       * history'yi de guncelliyoruz.
       */
      if (this.selectedType) {
        await this.fetchHistory();
      }
    },

    /*
     * Bir sensor kartina tiklandiginda
     * o sensorun gecmisini goster.
     *
     * Yeni sensor olmasi fark etmez.
     */
    selectType(type) {
      this.selectedType =
        type.toLowerCase();

      this.pageIndex = 0;

      this.fetchHistory();
    },

    /*
     * Gecmis sensor secimi degistiginde.
     */
    onTypeChange() {
      this.pageIndex = 0;
      this.fetchHistory();
    },

    prevPage() {
      if (this.pageIndex > 0) {
        this.pageIndex--;
        this.fetchHistory();
      }
    },

    nextPage() {
      if (
        this.pageIndex + 1 <
        this.totalPages
      ) {
        this.pageIndex++;
        this.fetchHistory();
      }
    },

    /*
     * 5 saniyede bir API'yi kontrol eder.
     *
     * Sayfa yenilenmez.
     * Browser refresh olmaz.
     * Sadece Vue state'i guncellenir.
     */
    startAutoRefresh() {

      this.stopAutoRefresh();

      this.timer = setInterval(async () => {

        if (!this.autoRefresh) {
          return;
        }

        await this.fetchLatest();

        /*
         * Ilk sayfadaysak gecmis tablosunu da
         * otomatik guncelle.
         *
         * Kullanici 2., 3. vb. sayfadaysa
         * her 5 saniyede sayfa pozisyonunu bozmayalim.
         */
        if (this.pageIndex === 0) {
          await this.fetchHistory();
        }

      }, REFRESH_MS);
    },

    stopAutoRefresh() {

      if (this.timer) {
        clearInterval(this.timer);
        this.timer = null;
      }
    },

    toggleAutoRefresh() {

      this.autoRefresh =
        !this.autoRefresh;

      if (this.autoRefresh) {
        this.startAutoRefresh();
      } else {
        this.stopAutoRefresh();
      }
    },

    formatTime(iso) {
      return iso
        ? new Date(iso).toLocaleString("tr-TR", { timeZone: "UTC" })
        : "";
    },

    /*
     * Normalizasyon adimlari.
     *
     * Bunlar su an mevcut sensorlerin
     * gercek donusumlerini gostermek icin tutuluyor.
     *
     * Yeni bir sensor geldiginde burada kod yazmak
     * zorunlu degil. Yeni sensor genel olarak
     * "Standart normalize edilmis veri" olarak gosterilir.
     */
    transforms(type) {

      if (type === "Temperature") {
        return [
          "JSON->model",
          "F->C",
          "Unix->UTC"
        ];
      }

      if (type === "Humidity") {
        return [
          "XML->model",
          "yuzde",
          "+03:00->UTC"
        ];
      }

      if (type === "Pressure") {
        return [
          "CSV->model",
          "mbar->hPa",
          "UTC"
        ];
      }

      if (type === "Light") {
        return [
          "JSON->model",
          "lux",
          "Unix->UTC"
        ];
      }

      /*
       * Sound, CO2 veya ileride gelecek baska
       * sensorler burada hata olusturmaz.
       */
      return [];
    },

    /*
     * Bilinen sensorlerin renklerini koruyoruz.
     *
     * Yeni sensor icin otomatik renk uretiliyor.
     * Dolayisiyla CO2 icin tekrar kod yazmak gerekmiyor.
     */
    cardColor(type) {

      const known = {
        Temperature: "#e53935",
        Humidity: "#1e88e5",
        Pressure: "#43a047",
        Light: "#f9a825"
      };

      if (known[type]) {
        return known[type];
      }

      /*
       * Yeni sensor tipi icin sensor adindan
       * tutarli bir renk uretilir.
       */
      let hash = 0;

      for (let i = 0; i < type.length; i++) {
        hash =
          type.charCodeAt(i) +
          ((hash << 5) - hash);
      }

      const hue =
        Math.abs(hash) % 360;

      return `hsl(${hue}, 55%, 45%)`;
    },

    /*
     * Bilinen sensor ikonlari korunuyor.
     *
     * Yeni sensor icin genel sensor ikonu.
     */
    cardIcon(type) {

      const known = {
        Temperature: "mdi-thermometer",
        Humidity: "mdi-water-percent",
        Pressure: "mdi-gauge",
        Light: "mdi-lightbulb-on",
        Sound: "mdi-volume-high"
      };

      return (
        known[type] ||
        "mdi-access-point"
      );
    }
  }
};
</script>

<style>
.sensor-card {
  cursor: pointer;
  transition:
    transform 0.15s,
    box-shadow 0.15s;
}

.sensor-card:hover {
  transform: translateY(-3px);
}

.selected-card {
  outline: 3px solid #0d1b3e;
}

.anomaly-card {
  outline: 3px solid #d32f2f;
}

.status-bar {
  padding: 4px 8px;
  font-size: 12px;
  font-weight: bold;
}

.status-bar.anomaly {
  background: #d32f2f;
  color: white;
  text-align: center;
}

.status-bar.normal {
  background: #e8f5e9;
  color: #2e7d32;
}

.raw-box {
  font-family: monospace;
  font-size: 11px;
  background: #f5f5f5;
  border: 1px solid #e0e0e0;
  border-radius: 4px;
  padding: 6px;
  white-space: pre-wrap;
  word-break: break-all;
  max-height: 70px;
  overflow-y: auto;
}

.norm-box {
  font-size: 12px;
  background: #e8f5e9;
  border: 1px solid #c8e6c9;
  border-radius: 4px;
  padding: 6px;
}

.anomaly-row {
  background-color: #ffebee !important;
}

/* Alarm ticker */
.alarm-ticker {
  background: #b71c1c;
  color: white;
  display: flex;
  align-items: center;
  padding: 6px 12px;
  overflow: hidden;
  white-space: nowrap;
  font-weight: bold;
  font-size: 13px;
}

.alarm-ticker-ok {
  background: #2e7d32;
  color: white;
  padding: 6px 12px;
  font-size: 13px;
  display: flex;
  align-items: center;
}

.ticker-track {
  display: inline-flex;
  animation: ticker 20s linear infinite;
}

.ticker-item {
  padding-right: 20px;
}

@keyframes ticker {
  0% {
    transform: translateX(0);
  }

  100% {
    transform: translateX(-50%);
  }
}
</style>
```
