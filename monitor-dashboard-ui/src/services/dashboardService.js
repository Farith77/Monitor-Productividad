import api from './api';

export const dashboardService = {
  getResumen: async (macAddress, fecha) => {
    const response = await api.get(`/dashboard/resumen`, { params: { macAddress, fecha } });
    return response.data;
  },
  getDistribucion: async (macAddress, fecha) => {
    const response = await api.get(`/dashboard/distribucion`, { params: { macAddress, fecha } });
    return response.data;
  },
  getDistribucionVentanas: async (macAddress, fecha) => {
    const response = await api.get(`/dashboard/distribucion-ventanas`, { params: { macAddress, fecha } });
    return response.data;
  },
  getLineaTiempo: async (macAddress, fecha) => {
    const response = await api.get(`/dashboard/linea-tiempo`, { params: { macAddress, fecha } });
    return response.data;
  },
  getTendenciaCambios: async (macAddress, fecha) => {
    const response = await api.get(`/dashboard/tendencia-cambios`, { params: { macAddress, fecha } });
    return response.data;
  },
  getRegistros: async (macAddress, fecha) => {
    const response = await api.get(`/dashboard/registros`, { params: { macAddress, fecha } });
    return response.data;
  },
  getDispositivos: async () => {
    const response = await api.get(`/dispositivos`);
    return response.data;
  },
  getBackgroundStats: async (macAddress, fecha) => {
    const response = await api.get(`/dashboard/background-stats`, { params: { macAddress, fecha } });
    return response.data;
  },
};