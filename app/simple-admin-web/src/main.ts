import { createApp } from 'vue'
import { createPinia } from 'pinia'
import Antd from 'ant-design-vue'
import App from './App.vue'
import router from './router'
import { setAuthToken } from './api'
import 'ant-design-vue/dist/reset.css'
import './styles/index.css'

const app = createApp(App)

const pinia = createPinia()
app.use(pinia)
app.use(router)
app.use(Antd)

// 恢复 token
const savedToken = localStorage.getItem('token')
if (savedToken) {
  setAuthToken(savedToken)
}

app.mount('#app')
