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

 
function App() {
  return (
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/form" element={<Form />} />
          <Route path="/login" element={<Login />} />
          <Route path="/candidatos" element={<Candidatos />} />
          <Route path="/payhistory" element={<PayHistoryList />} />
          <Route path="/dephistory" element={<DepartmentHistoryList />} />
          <Route path="/profile/:id?" element={<Profile />} />
          <Route path="/funcionarios" element={<Funcionarios />} />
          <Route path="/gestao-pagamentos" element={<Pagamentos />} />
          <Route path="/gestao-movimentos" element={<Movimentos />} />
        </Routes>
  );
}

export default App;