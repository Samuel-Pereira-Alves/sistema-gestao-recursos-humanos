import React from "react";
import './App.css';
import { Routes, Route } from "react-router-dom";
import Home from "./pages/Home/Home";
import Form from "./pages/Form/FormPage";
import Login from "./pages/Login/Login";
import Candidatos from "./pages/Candidatos/Candidatos";
import PayHistoryList from "./pages/PayHistory/PayHistory";
import DepartmentHistoryList from "./pages/DepHistory/DepHistory";
import Profile from "./pages/Profile/Profile";
import Funcionarios from "./pages/Funcionarios/Funcionarios";
import Movimentos from "./pages/Movimentos/Movimentos";
import Pagamentos from "./pages/Pagamentos/Pagamentos";
import ForbiddenPage from "./pages/Forbidden/Forbidden";
import AuthGuard from "./components/AuthGuard/RequireAuth";

function App() {
  return (
    <Routes>
      {/* Público */}
      <Route path="/" element={<Home />} />
      <Route path="/form" element={<Form />} />
      <Route path="/login" element={<Login />} />
      <Route path="/forbidden" element={<ForbiddenPage />} />

      {/* Autenticados */}
      <Route
        path="/payhistory"
        element={
          <AuthGuard allowedRoles={['employee', 'admin']}>
            <PayHistoryList />
          </AuthGuard>
        }
      />
      <Route
        path="/dephistory"
        element={
          <AuthGuard allowedRoles={['employee', 'admin']}>
            <DepartmentHistoryList />
          </AuthGuard>
        }
      />
      <Route
        path="/profile/:id?"
        element={
          <AuthGuard allowedRoles={['admin']}>
            <Profile />
          </AuthGuard>
        }
      />
      <Route
        path="/profile/"
        element={
          <AuthGuard allowedRoles={['employee', 'admin']}>
            <Profile />
          </AuthGuard>
        }
      />

      {/* Somente admin */}
      <Route
        path="/candidatos"
        element={
          <AuthGuard allowedRoles={['admin']}>
            <Candidatos />
          </AuthGuard>
        }
      />
      <Route
        path="/funcionarios"
        element={
          <AuthGuard allowedRoles={['admin']}>
            <Funcionarios />
          </AuthGuard>
        }
      />
      <Route
        path="/gestao-pagamentos"
        element={
          <AuthGuard allowedRoles={['admin']}>
            <Pagamentos />
          </AuthGuard>
        }
      />
      <Route
        path="/gestao-movimentos"
        element={
          <AuthGuard allowedRoles={['admin']}>
            <Movimentos />
          </AuthGuard>
        }
      />
    </Routes>
  );
}

export default App;