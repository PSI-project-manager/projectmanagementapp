import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter, Route, Routes } from 'react-router-dom'

import HomePage from './pages/HomePage.tsx'
import LoginPage from './pages/LoginPage.tsx'
import ProjectsPage from './pages/ProjectsPage.tsx'
import ItemTypesPage from './pages/ItemTypesPage.tsx'
import UsersPage from './pages/UsersPage.tsx'
import ItemsPage from './pages/ItemsPage.tsx'


import './index.css'
import ProtectedRoute from './features/protectedRoutes/ProtectedRoute.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <Routes>
        
        <Route path="/login" element={<LoginPage />} />
        <Route element={<ProtectedRoute />}>
          <Route path="/" element={<HomePage />} />

          <Route path="/projects" element={<ProjectsPage />} />
          <Route path="/items/new" element={<ItemsPage />} />

          <Route path="/admin/item-types" element={<ItemTypesPage />} />
          <Route path="/admin/users" element={<UsersPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  </StrictMode>
)
