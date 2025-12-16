import React from "react";
import './App.css';
import { BrowserRouter as Router, Routes, Route, BrowserRouter } from "react-router-dom";
import HomePage from './HomePage';
import FormPage from './FormPage';
import Login from './Login';
import Candidatos from "./Candidatos";
import PaymentsList from "./PayHistory";
import DepartmentHistoryList from "./DepHistory";
import EmployeeProfile from "./EmployeeProfile";
import Funcionarios from "./Funcionarios";
import GestaoMovimentacoes from "./GestaoMovimentos";
import GestaoPagamentos from "./GestaoPagamentos";

 
function App() {
  return (
     
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/form" element={<FormPage />} />
          <Route path="/login" element={<Login />} />
          <Route path="/candidatos" element={<Candidatos />} />
          <Route path="/payhistory" element={<PaymentsList />} />
          <Route path="/dephistory" element={<DepartmentHistoryList />} />
          <Route path="/profile/:id?" element={<EmployeeProfile />} />
          <Route path="/funcionarios" element={<Funcionarios />} />
          <Route path="/gestao-pagamentos" element={<GestaoPagamentos />} />
          <Route path="/gestao-movimentos" element={<GestaoMovimentacoes />} />
        </Routes>
      

  );
}

export default App;