import React from "react";
import { Navigate, useParams, useLocation } from "react-router-dom";

export default function RequireAuthWithOwner({ allowedRoles = [], children }) {
  const { id } = useParams();        

  const token = localStorage.getItem("authToken");
  const role = localStorage.getItem("role");
  const userId = localStorage.getItem("businessEntityId");

  if(allowedRoles.includes(role)){
    return children;
  }

  if (!token) {
    return <Navigate to="/login" />;
  }

  if (!allowedRoles || allowedRoles.length === 0) {
    return children;
  }

  if (!allowedRoles.includes(role)) {
    return <Navigate to="/forbidden" />;
  }


  return children;
}
