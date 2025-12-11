import './App.css'
import { Routes, Route } from "react-router-dom";
import HomePage from './HomePage'
import FormPage from './FormPage';

function App() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/form" element={<FormPage />} />
    </Routes>

  )
}

export default App
