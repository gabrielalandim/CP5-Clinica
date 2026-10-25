# Roteiro do vídeo (máx. 5 min) — ensaie uma vez com cronômetro

**Antes de gravar:** API rodando (`dotnet run --project Clinica.API`), Swagger aberto em `http://localhost:5080/swagger`, terminal com o console da API visível, banco acessível. Deixe `dotnet test` pronto para rodar em outro terminal.

| Tempo | O que mostrar | O que falar |
|---|---|---|
| 0:00–0:30 | Gerenciador de Soluções com os 6 projetos e as referências | "Clean Architecture: Domain, Application, Infrastructure e API, mais IoC e Tests. A Application só conhece o Domain; o JWT, o EF e o ViaCEP ficam na Infrastructure." |
| 0:30–1:05 | Terminal: `dotnet test` → todos passando | "Testes de unidade com Moq e de integração com WebApplicationFactory, rodando no mesmo banco Oracle da aplicação, sem SQLite." |
| 1:05–1:35 | `GET /api/pacientes` **sem token** → **401** com `code: NAO_AUTENTICADO`. `POST /api/auth` com `user@clinica.com` / `User@123` → Authorize | "Sem token a API bloqueia. Com o JWT, acesso liberado." |
| 1:35–2:30 | `POST /api/pacientes` com CEP `01001-000` → **201** com endereço preenchido. Mostrar o log no console. Repetir com outro e-mail e o mesmo CEP → log "obtido do cache". Depois CEP `00000-000` → **404** | "Serviço externo ViaCEP na Infrastructure, com retry, timeout e circuit breaker. O segundo pedido veio do cache: otimização." |
| 2:30–3:10 | Repetir o mesmo e-mail → **409** `EMAIL_JA_CADASTRADO`. `GET /api/pacientes/999999` → **404** `PACIENTE_NAO_ENCONTRADO` | "Aqui o Repository LANÇA a exception customizada; o UseCase captura e devolve um Operation Result com status e código." |
| 3:10–3:50 | `POST /api/consultas` → **201** (mostrar link `cancelar_consulta`). Repetir o mesmo horário → **409** `HORARIO_INDISPONIVEL`. `pacienteId` 999999 → **404** | "Na consulta o Repository não lança: DEVOLVE o erro num RepositoryResult. São as duas abordagens pedidas." |
| 3:50–4:25 | `GET /api/pacientes?page=1&pageSize=1` → mostrar `totalItems`, `totalPages`, `hasNext` e links `next`, `last`, `create_paciente`. Abrir um item e mostrar `self`, `update`, `delete` | "PageResult com metadados e HATEOAS orientando a navegação." |
| 4:25–5:00 | `DELETE /api/pacientes/{id}` como **user** → **403** `ACESSO_NEGADO`. Login `admin@clinica.com` / `Admin@123` → Authorize → DELETE → **204** | "Autorização por perfil: acesso bloqueado para user, permitido para admin." |

**Dicas:** deixe os JSONs de exemplo copiados num bloco de notas para colar rápido. Suba como **não listado** no YouTube e teste o link numa aba anônima antes de entregar. Cole o link no `README.md` e no `entrega.txt`.
