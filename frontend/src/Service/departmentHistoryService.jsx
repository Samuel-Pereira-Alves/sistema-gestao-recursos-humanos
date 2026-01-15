const API_BASE = "http://localhost:5136/api";

export async function deleteDepHistory(token, beid, depId, shId, formattedDate) {
  const url = `${API_BASE}/v1/departmenthistory/${encodeURIComponent(beid)}/${encodeURIComponent(depId)}/${encodeURIComponent(shId)}/${encodeURIComponent(formattedDate)}`;
    await fetch(url, {
      method: "DELETE",
      headers: {
        Accept: "application/json",
        Authorization: `Bearer ${token}`,
      },
    });
}

export async function createDepartmentHistory(body) {
  const token = localStorage.getItem("authToken");
  if (!token) throw new Error("Token ausente. Faça login novamente.");

  await fetch(`${API_BASE}/v1/departmenthistory`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Accept: "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify(body),
  });

}

export async function patchDepartmentHistory(businessEntityID, departmentID, shiftID, startDate , body) {
  const token = localStorage.getItem("authToken");
  if (!token) throw new Error("Token ausente. Faça login novamente.");

  const url = `${API_BASE}/v1/departmenthistory/${encodeURIComponent(businessEntityID)}/${encodeURIComponent(departmentID)}/${encodeURIComponent(shiftID)}/${encodeURIComponent(startDate)}`;

 await fetch(url, {
      method: "PATCH",
      headers: {
        "Content-Type": "application/json",
        Accept: "application/json",
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(body),
    });

}
