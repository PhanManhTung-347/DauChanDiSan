import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import '@fontsource/be-vietnam-pro/400.css'
import '@fontsource/be-vietnam-pro/500.css'
import '@fontsource/be-vietnam-pro/600.css'
import '@fontsource/newsreader/500.css'
import '@fontsource/newsreader/500-italic.css'
import './index.css'
import App from './App'

document.documentElement.dataset.heritageTheme = localStorage.getItem('heritage-theme') === 'map' ? 'map' : 'drum'

createRoot(document.getElementById('root')!).render(<StrictMode><App /></StrictMode>)
