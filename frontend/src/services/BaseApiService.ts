import axios, { AxiosInstance, AxiosResponse } from 'axios';

class BaseApiService {
  protected api: AxiosInstance;
  protected baseUrl: string;

  constructor() {
    this.baseUrl = this.getBaseUrl();

    this.api = axios.create({
      baseURL: this.baseUrl,
      headers: {
        'Content-Type': 'application/json',
      },
    });
  }

  protected getBaseUrl(): string {
    const isDevelopment = import.meta.env.DEV;
    return isDevelopment ? 'http://localhost:2024/api' : '/api';
  }

  async get<T = any>(endpoint: string, config = {}): Promise<T> {
    try {
      const response: AxiosResponse<T> = await this.api.get(endpoint, config);
      return response.data;
    } catch (error) {
      console.error('GET request error:', error);
      throw error;
    }
  }

  async post<T = any>(endpoint: string, body: any, config = {}): Promise<T> {
    try {
      const response: AxiosResponse<T> = await this.api.post(
        endpoint,
        body,
        config
      );
      return response.data;
    } catch (error) {
      console.error('POST request error:', error);
      throw error;
    }
  }

  async put<T = any>(endpoint: string, body: any, config = {}): Promise<T> {
    try {
      const response: AxiosResponse<T> = await this.api.put(
        endpoint,
        body,
        config
      );
      return response.data;
    } catch (error) {
      console.error('PUT request error:', error);
      throw error;
    }
  }

  async delete<T = any>(endpoint: string, config = {}): Promise<T> {
    try {
      const response: AxiosResponse<T> = await this.api.delete(
        endpoint,
        config
      );
      return response.data;
    } catch (error) {
      console.error('DELETE request error:', error);
      throw error;
    }
  }
}

export default BaseApiService;
