<script setup>
// App.vue no longer needs to import HelloWorld as it's now in HomeView
</script>

<template>
  <v-app>
    <v-app-bar>
      <v-app-bar-title>Project Manager Web</v-app-bar-title>
      <v-spacer></v-spacer>
      <v-btn
        :to="{ name: 'new-project' }"
        color="primary"
        prepend-icon="mdi-plus"
      >
        New Project
      </v-btn>
    </v-app-bar>

    <v-main>
      <router-view v-slot="{ Component }">
        <v-fade-transition mode="out-in">
          <component :is="Component" />
        </v-fade-transition>
      </router-view>
    </v-main>
  </v-app>
</template>

<style scoped>
.logo {
  height: 6em;
  padding: 1.5em;
  will-change: filter;
  transition: filter 300ms;
}
.logo:hover {
  filter: drop-shadow(0 0 2em #646cffaa);
}
.logo.vue:hover {
  filter: drop-shadow(0 0 2em #42b883aa);
}
</style>
