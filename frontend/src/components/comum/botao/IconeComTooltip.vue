<template>
  <v-tooltip :text="texto" :location="getLocation()">
    <template #activator="{ props }">
      <v-btn v-bind="props" icon size="small" variant="flat" @click="acao()">
        <v-icon small>{{ icone }}</v-icon>
      </v-btn>
    </template>
  </v-tooltip>
</template>

<script setup>
const props = defineProps({
  icone: {
    type: String,
    required: true,
  },
  texto: {
    type: String,
    required: true,
  },
  acao: {
    type: Function,
    required: true,
  },
  top: {
    type: Boolean,
    default: true,
  },
  bottom: {
    type: Boolean,
    default: false,
  },
  left: {
    type: Boolean,
    default: false,
  },
  right: {
    type: Boolean,
    default: false,
  },
});

const getLocation = () => {
  if (props.top) return 'top';
  if (props.bottom) return 'bottom';
  if (props.left) return 'start';
  if (props.right) return 'end';
  return 'top';
};
</script>
