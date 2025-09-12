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

  async post(endpoint, body) {
    try {
      const response = await fetch(`${this.baseUrl}/${endpoint}`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(body),
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      // Verifica se tem conteúdo antes de tentar fazer o parse
      const text = await response.text();
      return text ? JSON.parse(text) : null;
    } catch (error) {
      console.error("Error fetching data:", error);
      throw error;
    }
  }

  // Você pode adicionar outros métodos HTTP aqui (post, put, delete, etc.)
}

export default BaseApiService;
