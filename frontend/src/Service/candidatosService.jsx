import axios from "axios";
const API_BASE = "http://localhost:5136/api";

export async function getCandidatos(token, { pageNumber, pageSize, search }) {
  const url = new URL(`${API_BASE}/v1/jobcandidate`);
  const params = new URLSearchParams();

  params.set("pageNumber", String(pageNumber));
  params.set("pageSize", String(pageSize));

  if (search && search.trim()) {
    params.set("search", search.trim());
  }

  url.search = params.toString();

  const res = await fetch(url.toString(), {
    headers: {
      Accept: "application/json",
      Authorization: `Bearer ${token}`,
    },
  });

  if (!res.ok) throw new Error(`Erro ao carregar candidatos (HTTP ${res.status})`);

  const header = res.headers.get("X-Pagination");
  const metaFromHeader = header ? JSON.parse(header) : null;
  const body = await res.json();

  return {
    items: body.items ?? [],
    meta: {
      totalCount: body.totalCount ?? metaFromHeader?.TotalCount ?? 0,
      pageNumber: body.pageNumber ?? metaFromHeader?.PageNumber ?? pageNumber,
      pageSize: body.pageSize ?? metaFromHeader?.PageSize ?? pageSize,
      totalPages: body.totalPages ?? metaFromHeader?.TotalPages ?? 1,
      hasPrevious: body.hasPrevious ?? metaFromHeader?.HasPrevious ?? pageNumber > 1,
      hasNext: body.hasNext ?? metaFromHeader?.HasNext ?? false,
    },
  };
}

export async function openPdf(id) {
  try {
    const token = localStorage.getItem("authToken");

    const response = await fetch(
      `${API_BASE}/v1/jobcandidate/${id}/cv`,
      {
        headers: { Authorization: `Bearer ${token}` },
      }
    );

    if (!response.ok) throw new Error("Erro ao baixar o arquivo");

    const blob = await response.blob();
    const blobUrl = URL.createObjectURL(blob);

    const a = document.createElement("a");
    a.href = blobUrl;
    a.download = `CV_${id}.pdf`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);

    URL.revokeObjectURL(blobUrl);
  } catch (error) {
    console.error("Erro ao baixar o CV:", error);
    alert("Falha ao baixar o CV.");
  }
}

function getToken() {
  const token = localStorage.getItem("authToken");
  if (!token) throw new Error("Token ausente. Faça login novamente.");
  return token;
}

export async function sendFeedbackEmail(email, condition) {
  const frase =
    condition === false
      ? "Infelizmente, a sua candidatura não foi aprovada nesta fase do processo. Agradecemos o seu interesse e o tempo dedicado à candidatura. Continuaremos a considerar o seu perfil para futuras oportunidades compatíveis."
      : "Parabéns! A sua candidatura foi aprovada nesta fase do processo. Em breve entraremos em contacto para lhe fornecer mais detalhes sobre os próximos passos. Obrigado pelo seu interesse e confiança.";

  try {
    await axios.post(
      `${API_BASE}/email/send`,
      {
        To: email,
        subject: "Feedback Candidatura",
        text: frase,
      },
      {
        headers: { "Content-Type": "application/json" },
      }
    );
  } catch (e) {
    if (e.response) {
      throw new Error(
        `Erro API ao enviar email (${e.response.status}): ${JSON.stringify(
          e.response.data
        )}`
      );
    } else {
      throw new Error(`Erro rede/CORS ao enviar email: ${e.message}`);
    }
  }
}

export async function approveCandidate(id) {
  const token = getToken();

  const res = await fetch(`${API_BASE}/v1/employee/approve/${id}`, {
    method: "POST",
    headers: {
      Accept: "application/json",
      Authorization: `Bearer ${token}`,
    },
  });

  if (!res.ok) {
    let details = "";
    try {
      const ct = res.headers.get("content-type") || "";
      if (ct.includes("application/json")) {
        const j = await res.json();
        details = j?.message ? ` Detalhes: ${j.message}` : "";
      } else {
        const t = await res.text();
        details = t ? ` Detalhes: ${t}` : "";
      }
    } catch { }
    throw new Error(`Falha ao aprovar candidato (HTTP ${res.status}).${details}`);
  }

}

export async function deleteCandidate(id) {
  const token = getToken();

  const res = await fetch(`${API_BASE}/v1/jobcandidate/${id}`, {
    method: "DELETE",
    headers: {
      Accept: "application/json",
      Authorization: `Bearer ${token}`,
    },
  });

  if (!res.ok) {
    let details = "";
    try {
      const ct = res.headers.get("content-type") || "";
      if (ct.includes("application/json")) {
        const j = await res.json();
        details = j?.message ? ` Detalhes: ${j.message}` : "";
      } else {
        const t = await res.text();
        details = t ? ` Detalhes: ${t}` : "";
      }
    } catch { }
    throw new Error(`Falha ao eliminar (HTTP ${res.status}).${details}`);
  }
}

export async function sendEmailFeedback(condition, email) {

  var frase = condition === false
    ? "Infelizmente, a sua candidatura não foi aprovada nesta fase do processo. Agradecemos o seu interesse e o tempo dedicado à candidatura. Continuaremos a considerar o seu perfil para futuras oportunidades compatíveis."
    : "Parabéns! A sua candidatura foi aprovada nesta fase do processo. Em breve entraremos em contacto para lhe fornecer mais detalhes sobre os próximos passos. Obrigado pelo seu interesse e confiança.";

  console.log(email)
  try {

    await axios.post(`${API_BASE}/email/send`, {
      to: email,
      subject: 'Feedback Candidatura',
      text: frase
    }, {
      headers: { "Content-Type": "application/json" }
    }
    );
  } catch (e) {
    if (e.response) {
      console.error('Erro API:', e.response.data);
    } else {
      console.error('Erro rede/CORS:', e.message);
    }
  }
}