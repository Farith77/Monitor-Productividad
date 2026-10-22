import axios from 'axios';

// Instancia base de Axios apuntando a tu backend ASP.NET Core
const api = axios.create({
  baseURL: 'http://localhost:5259/api',
  headers: {
    'Content-Type': 'application/json'
  }
});

export default api;