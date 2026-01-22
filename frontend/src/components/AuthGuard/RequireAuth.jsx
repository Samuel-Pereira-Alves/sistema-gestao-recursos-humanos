import React from "react";
import { Navigate, useParams } from "react-router-dom";

export default function RequireAuthWithOwner({ allowedRoles = [], children }) {
  const { id } = useParams();
  const token = localStorage.getItem("authToken");
  const role = localStorage.getItem("role");
  const userId = localStorage.getItem("businessEntityId");

  if (!token) {
    return <Navigate to="/login" replace />;
  }

  if (allowedRoles.length > 0 && !allowedRoles.includes(role)) {
    return <Navigate to="/forbidden" replace />;
  }

  if (id && role !== "admin") {
    if (userId == null || id !== String(userId)) {
      return <Navigate to="/forbidden" replace />;
    }
  }

  return children;
}
