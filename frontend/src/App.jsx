import React from "react";
import './App.css';
import { BrowserRouter as Router, Routes, Route } from "react-router-dom";
import HomePage from './HomePage';
import FormPage from './FormPage';
import Login from './Login';
import DashboardRH from "./DashboardRH";
import Candidatos from "./Candidatos";
import PaymentsList from "./PayHistory";
import DepartmentHistoryList from "./DepHistory";
 
function App() {
  return (

      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/form" element={<FormPage />} />
        <Route path="/login" element={<Login />} />
        <Route path="/rh" element={<DashboardRH />} />
        <Route path="/candidatos" element={<Candidatos />} />
        <Route path="/payhistory" element={<PaymentsList />} />
        <Route path="/dephistory" element={<DepartmentHistoryList />} />
      </Routes>

  );
}
 
export default App;