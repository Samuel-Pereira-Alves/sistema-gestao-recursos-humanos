# Sistema de Gestão de Recursos Humanos

Aplicação web para gerir recursos humanos, com autenticação, perfis e gestão de dados.
Inclui frontend em React e backend em .NET com ligação a base de dados.
Swagger disponível no backend para visualizar e testar os endpoints.
Permite login e controlo de acesso por roles (admin, employee).
Funcionalidades: gestão de funcionários, histórico de pagamentos, movimentos, etc.

## Como instalar e correr
1. Clonar o projeto ou download do zip
2. Configurar connection string da base de dados
3. Configurar token JWT no appsettings.json

"Jwt": {
  "Key": "CHAVE_SECRETA",
  "Issuer": "ISSUER",
  "Audience": "AUDIENCE"
}

4. Instalar dependências

Frontend:
cd frontend
npm install
npm run dev

Backend:

cd backend
dotnet restore
dotnet run