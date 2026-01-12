import React from "react";
import { Navigate, useParams } from "react-router-dom";

export default function RequireAuthWithOwner({ allowedRoles = [], children }) {
  const { id } = useParams();
  const token = localStorage.getItem("authToken");
  const role = localStorage.getItem("role");
  const userId = localStorage.getItem("businessEntityId");

  // If not authenticated, go to login
  if (!token) {
    return <Navigate to="/login" replace />;
  }

  // If role not allowed at all, forbid
  if (allowedRoles.length > 0 && !allowedRoles.includes(role)) {
    return <Navigate to="/forbidden" replace />;
  }

  // Owner-or-admin rule: if there's a route param id
  // - admin: always allowed
  // - employee: only allowed if id === userId
  if (id && role !== "admin") {
    if (userId == null || id !== String(userId)) {
      return <Navigate to="/forbidden" replace />;
    }
  }

  return children;
}
