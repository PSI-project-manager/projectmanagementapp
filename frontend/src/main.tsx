import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter, Route, Routes } from 'react-router-dom'

import HomePage from './pages/HomePage.tsx'
import LoginPage from './pages/LoginPage.tsx'
import ProjectsPage from './pages/ProjectsPage.tsx'
import ItemTypesPage from './pages/ItemTypesPage.tsx'
import UsersPage from './pages/UsersPage.tsx'
import ItemsPage from './pages/ItemsPage.tsx'
import PermissionDeniedPage from './pages/PermissionDeniedPage.tsx'


import './index.css'
import ProtectedRoute from './features/protectedRoutes/ProtectedRoute.tsx'
import RequireRole from './features/protectedRoutes/RequireRole.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route element={<ProtectedRoute />}>
          <Route path="/" element={<HomePage />} />

          <Route path="/projects" element={<ProjectsPage />} />
          <Route path="/items/new" element={<ItemsPage />} />

          <Route path="/admin" element={<RequireRole roles={['Admin']} />}>
            <Route path="item-types" element={<ItemTypesPage />} />
            <Route path="users" element={<UsersPage />} />
          </Route>
          <Route path="/forbidden" element={<PermissionDeniedPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  </StrictMode>
)
