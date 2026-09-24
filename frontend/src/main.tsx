import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter, Route, Routes } from 'react-router-dom'

import LoginPage from './pages/LoginPage.tsx'
import ProjectsPage from './pages/ProjectsPage.tsx'
import ItemTypesPage from './pages/ItemTypesPage.tsx'
import UsersPage from './pages/UsersPage.tsx'

import './index.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<App />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/projects" element={<ProjectsPage />} />
        <Route path="/admin/item-types" element={<ItemTypesPage />} />
        <Route path="/admin/users" element={<UsersPage />} />
      </Routes>
    </BrowserRouter>
  </StrictMode>
)
