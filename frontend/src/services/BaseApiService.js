import axios from "axios";

class BaseApiService {
  constructor() {
    this.baseUrl = this.getBaseUrl();

    this.api = axios.create({
      baseURL: this.baseUrl,
      headers: {
        "Content-Type": "application/json",
      },
    });
  }

  getBaseUrl() {
    const isDevelopment = import.meta.env.DEV;
    return isDevelopment ? "http://localhost:2024/api" : "/api";
  }

  async get(endpoint, config = {}) {
    try {
      const response = await this.api.get(endpoint, config);
      return response.data;
    } catch (error) {
      console.error("GET request error:", error);
      throw error;
    }
  }

  async post(endpoint, body, config = {}) {
    try {
      const response = await this.api.post(endpoint, body, config);
      return response.data;
    } catch (error) {
      console.error("POST request error:", error);
      throw error;
    }
  }

  async put(endpoint, body, config = {}) {
    try {
      const response = await this.api.put(endpoint, body, config);
      return response.data;
    } catch (error) {
      console.error("PUT request error:", error);
      throw error;
    }
  }

  async delete(endpoint, config = {}) {
    try {
      const response = await this.api.delete(endpoint, config);
      return response.data;
    } catch (error) {
      console.error("DELETE request error:", error);
      throw error;
    }
  }
}

export default BaseApiService;
