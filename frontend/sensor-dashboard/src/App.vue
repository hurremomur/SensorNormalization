<template>
  <v-app>
    <!-- Ust bar (Platform360 tarzi koyu lacivert) -->
    <v-app-bar app color="#0d1b3e" dark flat>
      <v-toolbar-title class="font-weight-bold">
        Sensor Normalizasyon Paneli
      </v-toolbar-title>
      <v-spacer></v-spacer>
      <v-btn icon @click="fetchLatest" :loading="loading">
        <v-icon>mdi-refresh</v-icon>
      </v-btn>
    </v-app-bar>

    <v-main style="background-color: #f4f6fb;">
      <v-container>
        <h2 class="mb-4 mt-2">Son Olcum Degerleri</h2>

        <!-- Hata mesaji -->
        <v-alert v-if="error" type="error" dense>
          {{ error }}
        </v-alert>

        <!-- Son deger kartlari -->
        <v-row>
          <v-col
            v-for="reading in latest"
            :key="reading.sensorType"
            cols="12" sm="4"
          >
            <v-card elevation="2" class="pa-4">
              <div class="text-subtitle-1 grey--text">
                {{ reading.sensorType }}
              </div>
              <div class="text-h4 font-weight-bold my-2">
                {{ reading.value }} {{ reading.unit }}
              </div>
              <div class="text-caption grey--text">
                {{ reading.sensorId }} &bull; {{ formatTime(reading.time) }}
              </div>
            </v-card>
          </v-col>
        </v-row>

        <!-- Veri yoksa -->
        <v-alert v-if="!loading && latest.length === 0 && !error" type="info" dense class="mt-4">
          Henuz veri yok. Consumer ve Simulator calisiyor mu?
        </v-alert>
      </v-container>
    </v-main>
  </v-app>
</template>

<script>
import axios from "axios";

const API_BASE = "http://localhost:5160/api/sensor-readings";

export default {
  name: "App",
  data() {
    return {
      latest: [],
      loading: false,
      error: null
    };
  },
  mounted() {
    this.fetchLatest();
  },
  methods: {
    async fetchLatest() {
      this.loading = true;
      this.error = null;
      try {
        const response = await axios.get(API_BASE + "/latest");
        this.latest = response.data;
      } catch (e) {
        this.error = "API''ye baglanilamadi: " + e.message;
      } finally {
        this.loading = false;
      }
    },
    formatTime(iso) {
      if (!iso) return "";
      return new Date(iso).toLocaleString("tr-TR");
    }
  }
};
</script>

<style>
</style>
