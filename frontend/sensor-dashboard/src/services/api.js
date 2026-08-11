import axios from "axios";

const api = axios.create({
  baseURL: "http://localhost:5160/api"
});

export default api;