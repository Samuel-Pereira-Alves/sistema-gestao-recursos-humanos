// src/pages/HomePage.jsx
import React from "react";
import Navbar from "./Navbar";

function HomePage() {
  return (
    <div>
      <Navbar />
      <main className="container" style={{ marginTop: "80px" }}>
        <h2 className="mt-4">Bem-vindo ao Sistema de Gestão de RH</h2>
        <p className="lead">
          Escolhe uma opção no menu acima para começar.
        </p>
      </main>
    </div>
  );
}

export default HomePage;