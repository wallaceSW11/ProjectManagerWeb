<template>
  <v-container>
    <v-row>
      <v-col>
        <v-card>
          <v-card-title>New Project</v-card-title>
          <v-card-text>
            <v-form>
              <v-text-field
                v-model="projectName"
                label="Project Name"
                required
              ></v-text-field>
              <v-textarea
                v-model="projectDescription"
                label="Description"
                rows="3"
              ></v-textarea>
              <v-card-actions>
                <v-spacer></v-spacer>
                <v-btn color="primary">
                  Save Project
                </v-btn>
              </v-card-actions>
            </v-form>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
import { ref } from 'vue'

const projectName = ref('')
const projectDescription = ref('')
</script>
