# CP5 – Clínica API

API RESTful em **ASP.NET Core 8 (Controllers)** para gestão de **pacientes** e **consultas** de uma clínica.
Projeto do CP5 de *Advanced Business Development with .NET* (FIAP – 2026).

> 🎥 **Vídeo de demonstração:** https://youtu.be/AkDoCDo4OYQ
>
> 👥 **Integrantes:**
> - Maria Gabriela Landim Severo – RM565146
> - Samara Porto Souza – RM559072
>
> 📦 **Repositório:** https://github.com/gabrielalandim/CP5-Clinica

---

## 1. Descrição do projeto

| Entidade | O que faz |
|---|---|
| **Paciente** | Cadastro com nome, e-mail e CEP. O endereço (logradouro, cidade, UF) é preenchido automaticamente pelo serviço externo **ViaCEP**. |
| **Consulta** | Agendamento por paciente, especialidade e horário, com bloqueio de horário duplicado e cancelamento. |
| **Usuário** | Cadastro e login com **JWT**. Perfis `admin` e `user`. |

Relacionamento: `Paciente 1 ── N Consulta` (FK com exclusão em cascata).

---

## 2. Arquitetura (Clean Architecture)

```
Clinica.Domain          → Entidades, erros customizados (DomainException + filhas), RepositoryResult,
                          interfaces (IRepository, IEnderecoService, ITokenService), PasswordHasher
Clinica.Application     → UseCases, DTOs, Mappers, OperationResult, ErrorMapper, PageResult, HATEOAS
Clinica.Infrastructure  → EF Core + Oracle, Repositories, ViaCepService (HttpClient + resiliência + cache),
                          JwtTokenService, DbInitializer
Clinica.IoC             → Bootstrap: registro de dependências (DI), resiliência do HttpClient
Clinica.API             → Controllers, GlobalExceptionHandler, Program.cs (Swagger, JWT, compressão)
Clinica.Tests           → Testes de unidade (Moq) e de integração (WebApplicationFactory + banco Oracle real)
```

**Regra de dependência (sempre para dentro):**

```
API ──► IoC ──► Infrastructure ──► Domain
 │        └───► Application ─────► Domain
 └──────────────► Application
```

- `Domain` não depende de ninguém.
- `Application` depende só do `Domain` (não conhece EF, Oracle, HttpClient nem JWT).
- `Infrastructure` implementa as interfaces do `Domain` (repositórios, ViaCEP, token).
- `API` só conversa com os UseCases através das interfaces da `Application`.

### Fluxo de uma requisição

```
HTTP ► Controller ► IUseCase (Application) ► IRepository / IEnderecoService (Domain)
                                                   ▲ implementados na Infrastructure
     ◄ ToActionResult ◄ OperationResult<T> ◄───────┘
```

---

## 3. Requisitos do CP5 – onde cada um está

| Requisito | Implementação |
|---|---|
| **Arquitetura** | 4 camadas + IoC + Tests (seção 2). JWT movido para a Infrastructure (`ITokenService`), deixando a Application sem dependências técnicas. |
| **Otimização** | `AsNoTracking` nas leituras · paginação no banco (`Skip/Take` → `OFFSET/FETCH`) · `ExisteAsync` (`SELECT 1 … FETCH FIRST 1 ROWS ONLY` em vez de contar/varrer) · índices (e-mail único em usuário e paciente; `DataHora + Especialidade` em consulta) · **cache em memória** dos CEPs (12h) · `pageSize` limitado a 50 · compressão **Brotli/Gzip** · `HttpClient` reaproveitado via `IHttpClientFactory` · tudo `async`. |
| **Operation Result** | `Application/Common/OperationResult.cs` – todo UseCase devolve `OperationResult<T>` (`IsSuccess`, `Value`, `Error`, `ErrorCode`, `StatusCode`). O controller converte com `ToActionResult`. |
| **Erros customizados – LANÇAMENTO** | `PacienteRepository` **lança** `NotFoundException` / `ConflictException` (herdam de `DomainException`, com `Code`). O UseCase captura e converte via `ErrorMapper` → 404/409. |
| **Erros customizados – RETORNO** | `ConsultaRepository` e `UserRepository` **devolvem** `RepositoryResult<T>` com `RepositoryError(Code, Message)`, sem lançar. O UseCase mapeia o código → 404/409. |
| **Tratamento global** | `API/Middlewares/GlobalExceptionHandler.cs` (`IExceptionHandler`): qualquer exception que escape vira JSON padronizado, preservando o status dos erros customizados e devolvendo 500 genérico para o resto. |
| **Serviço externo** | `Infrastructure/Services/ViaCepService.cs` – `HttpClient` tipado + `AddStandardResilienceHandler` configurado no `Bootstrap`: **retry** (3x, backoff exponencial), **timeout** por tentativa (5s) e total (20s), **circuit breaker** (50% de falha em 30s abre por 15s). CEP inexistente → `NotFoundException` (404); serviço fora do ar → `ExternalServiceException` (503). |
| **PageResult** | `Application/Common/PageResult.cs`: `items`, `page`, `pageSize`, `totalItems`, `totalPages`, `hasNext`, `hasPrevious`, `links`. |
| **HATEOAS** | `LinkDto (href, rel, method)` em cada item (`self`, `update_paciente`, `delete_paciente`, `consultas_do_paciente`, `paciente`, `cancelar_consulta` – só aparece se a consulta estiver agendada) e na página (`self`, `first`, `prev`, `next`, `last`, `create_*`). |
| **Autenticação / Autorização** | JWT Bearer (HMAC-SHA256, expira em 8h, valida assinatura e validade). `[Authorize]` nos controllers de negócio; `[Authorize(Roles = "admin")]` no `DELETE /api/pacientes/{id}`. 401/403 respondem em JSON. Senhas com hash **PBKDF2** (100 mil iterações + salt). |

### Formato padrão de erro

```json
{
  "status": 404,
  "code": "PACIENTE_NAO_ENCONTRADO",
  "error": "Paciente 999 não encontrado.",
  "path": "/api/pacientes/999"
}
```

Códigos usados: `PACIENTE_NAO_ENCONTRADO`, `CONSULTA_NAO_ENCONTRADA`, `HORARIO_INDISPONIVEL`, `CONSULTA_JA_CANCELADA`, `EMAIL_JA_CADASTRADO`, `CEP_INVALIDO`, `CEP_NAO_ENCONTRADO`, `SERVICO_EXTERNO_INDISPONIVEL`, `DATA_INVALIDA`, `CREDENCIAIS_INVALIDAS`, `NAO_AUTENTICADO`, `ACESSO_NEGADO`.

---

## 4. Endpoints

| Método | Rota | Acesso | Respostas |
|---|---|---|---|
| POST | `/api/auth` | público (login → JWT) | 200, 401 |
| POST | `/api/user` | público (cria usuário `user`) | 201, 400, 409 |
| GET | `/api/pacientes?page=1&pageSize=10` | autenticado | 200 |
| GET | `/api/pacientes/{id}` | autenticado | 200, 404 |
| POST | `/api/pacientes` | autenticado (consulta o ViaCEP) | 201, 400, 404, 409, 503 |
| PUT | `/api/pacientes/{id}` | autenticado | 200, 404, 409 |
| DELETE | `/api/pacientes/{id}` | **somente `admin`** | 204, 403, 404 |
| GET | `/api/consultas?page=1&pageSize=10&pacienteId=` | autenticado | 200 |
| GET | `/api/consultas/{id}` | autenticado | 200, 404 |
| POST | `/api/consultas` | autenticado | 201, 400, 404, 409 |
| DELETE | `/api/consultas/{id}` | autenticado (cancela) | 200, 404, 409 |

Usuários criados automaticamente na primeira execução:

| E-mail | Senha | Perfil |
|---|---|---|
| `admin@clinica.com` | `Admin@123` | admin |
| `user@clinica.com` | `User@123` | user |

---

## 5. Como rodar o projeto

**Pré-requisitos:** .NET 8 SDK e acesso ao banco **Oracle** da FIAP.

1. Configure a connection string (veja a seção 7) em `Clinica.API/appsettings.Development.json` ou por variável de ambiente.
2. Rode a API:

   ```bash
   dotnet restore
   dotnet run --project Clinica.API
   ```

   Na primeira execução as tabelas (`tb_clinica_usuario`, `tb_clinica_paciente`, `tb_clinica_consulta`) e os usuários iniciais são criados automaticamente.
3. Abra **http://localhost:5000/swagger**.
4. `POST /api/auth` com `admin@clinica.com` / `Admin@123` → copie o `jwt` → botão **Authorize** → cole o token.

### Exemplos de corpo

```json
// POST /api/pacientes
{ "nome": "Ana Souza", "email": "ana@teste.com", "cep": "01001-000" }

// POST /api/consultas
{ "pacienteId": 1, "dataHora": "2027-03-10T14:30:00", "especialidade": "Cardiologia", "valor": 250 }
```

---

## 6. Como executar os testes

```bash
dotnet test
```

| Tipo | Pasta | O que cobre |
|---|---|---|
| **Unidade** | `Clinica.Tests/Unit` | UseCases com Moq (Operation Result, erros lançados e devolvidos), `ErrorMapper`, `PageResult` e links HATEOAS, `ViaCepService` com `HttpMessageHandler` falso (sucesso, CEP inexistente, 400, 500, JSON inválido, cache), `JwtTokenService`, hash de senha. |
| **Integração / Funcionais** | `Clinica.Tests/Integration` | Sobem a API inteira com `WebApplicationFactory` e usam **o mesmo banco Oracle da aplicação** (sem SQLite/InMemory). Cobrem 401 (sem token / token inválido), 403, 404, 409, 503, CRUD de pacientes, fluxo agendar → cancelar → cancelar de novo, paginação e links HATEOAS. |

- As classes de integração compartilham **uma única instância da API** (`ICollectionFixture`) e rodam em sequência, evitando corrida na criação das tabelas.
- Só o ViaCEP é substituído por um *fake* nos testes de integração (para não depender da internet). CEPs especiais do fake: `00000000` → 404, `99999999` → 503.
- Os testes geram e-mails e horários aleatórios, então podem ser executados várias vezes no mesmo banco.

---

## 7. Configurações necessárias

| Chave | Onde | Descrição |
|---|---|---|
| `ConnectionStrings:Oracle` | `appsettings*.json` ou variável `ConnectionStrings__Oracle` | Connection string do Oracle (usada pela API **e** pelos testes). |
| `Secretkey` | `appsettings.json` | Chave de assinatura do JWT (mínimo 32 caracteres). |
| `ExternalServices:ViaCepUrl` | `appsettings.json` | URL base do ViaCEP (padrão `https://viacep.com.br/ws/`). |

Exemplo de connection string:

```json
{
  "ConnectionStrings": {
    "Oracle": "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=oracle.fiap.com.br)(PORT=1521))(CONNECT_DATA=(SID=ORCL)));User Id=SEU_RM;Password=SUA_SENHA;"
  }
}
```

Variável de ambiente (PowerShell): `$env:ConnectionStrings__Oracle="...User Id=SEU_RM;Password=SUA_SENHA;"`

> **Compatibilidade Oracle 19c:** o `Oracle.EntityFrameworkCore` 8.23 gera, por padrão, SQL do Oracle 23ai. O `AnyAsync` vira `CASE WHEN EXISTS(...) THEN TRUE ELSE FALSE END`, que no 19c da FIAP dá `ORA-00904: "FALSE"`. Por isso os repositórios usam `ExisteAsync` (`Infrastructure/Data/QueryableExtensions.cs`), que é compatível e mais leve.

**Serviço externo:** ViaCEP é público e não precisa de chave. Exige acesso à internet ao rodar a API (os testes não dependem dele).

---

## 8. Tecnologias

.NET 8 · ASP.NET Core (Controllers) · Entity Framework Core 8 · Oracle.EntityFrameworkCore · Microsoft.Extensions.Http.Resilience (Polly) · JWT Bearer · Swashbuckle (Swagger) · Serilog · xUnit · Moq · Microsoft.AspNetCore.Mvc.Testing
