import axios from 'axios';

// En producción leerá la URL de Render; en local usará localhost:5259
const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5259/api',
  headers: {
    'Content-Type': 'application/json'
  }
});

export default api;