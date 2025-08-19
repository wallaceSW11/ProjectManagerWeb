<template>
  <v-container>
    <v-row>
      <v-col>
        <v-icon>mdi-pencil</v-icon>
        <h1>Projects</h1>
        <v-table>
          <thead>
            <tr>
              <th>Name</th>
              <th>Description</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>Sample Project</td>
              <td>A sample project description</td>
              <td>
                <v-btn icon size="small" color="primary">
                  <v-icon>mdi-pencil</v-icon>
                </v-btn>
                <v-btn icon size="small" color="error" class="ml-2">
                  <v-icon>mdi-delete</v-icon>
                </v-btn>
              </td>
            </tr>
          </tbody>
        </v-table>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
// Projects list component logic will go here
</script>
