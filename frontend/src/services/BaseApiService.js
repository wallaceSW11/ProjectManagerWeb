class BaseApiService {
  constructor() {
    this.baseUrl = this.getBaseUrl();
  }

  getBaseUrl() {
    const isDevelopment = import.meta.env.DEV;
    return isDevelopment ? "http://localhost:2024/api" : "/api";
  }

  async get(endpoint) {
    try {
      const response = await fetch(`${this.baseUrl}/${endpoint}`);
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      return await response.json();
    } catch (error) {
      console.error("Error fetching data:", error);
      throw error;
    }
  }

  // Você pode adicionar outros métodos HTTP aqui (post, put, delete, etc.)
}

export default BaseApiService;
