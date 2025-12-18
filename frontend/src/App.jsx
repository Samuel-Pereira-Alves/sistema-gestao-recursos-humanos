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
import AuthGuard from "./components/AuthGuard/RequireAuth";

 
function App() {
  return (
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/form" element={<Form />} />
          <Route path="/login" element={<Login />} />
          <Route path="/candidatos" element={<AuthGuard><Candidatos /></AuthGuard>} />
          <Route path="/payhistory" element={<AuthGuard><PayHistoryList /></AuthGuard>} />
          <Route path="/dephistory" element={<AuthGuard><DepartmentHistoryList /></AuthGuard>} />
          <Route path="/profile/:id?" element={<AuthGuard><Profile /></AuthGuard>} />
          <Route path="/funcionarios" element={<AuthGuard><Funcionarios /></AuthGuard>} />
          <Route path="/gestao-pagamentos" element={<AuthGuard><Pagamentos /></AuthGuard>} />
          <Route path="/gestao-movimentos" element={<AuthGuard><Movimentos /></AuthGuard>} />
        </Routes>
  );
}

export default App;