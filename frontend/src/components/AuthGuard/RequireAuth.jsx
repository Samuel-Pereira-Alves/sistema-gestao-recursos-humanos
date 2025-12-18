import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';

export default function RequireAuth({ children }) {
  const token = localStorage.getItem('authToken'); 

  if (!token) {
    return <Navigate to="/login" replace />;
  }
  return children;
}