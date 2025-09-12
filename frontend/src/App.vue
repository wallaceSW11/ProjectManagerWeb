<script setup>

</script>

<template>
  <v-app>
    <v-app-bar>
      <v-app-bar-title>
          Project Manager Web
      </v-app-bar-title>

      <div>
        <v-btn class="text-none">
          <v-icon>mdi-git</v-icon>
          Clonar
        </v-btn>
        <v-btn class="text-none" :to="{ name: 'pastas'}">
          <v-icon>mdi-folder</v-icon>
          Pastas
        </v-btn>
        <v-btn class="text-none" :to="{ name: 'repositorios'}">
          <v-icon>mdi-folder</v-icon>
          Repositórios
        </v-btn>
        <v-btn class="text-none" :to="{ name: 'configuracao'}">
          <v-icon>mdi-cog</v-icon>
          Configurações
        </v-btn>
      </div>

    </v-app-bar>

    <v-main>
      <router-view />
    </v-main>

  </v-app>
</template>

<style>
/* Add global styles here if needed */
</style>
