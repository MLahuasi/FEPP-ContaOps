# Agent Instructions

## Repository State

- The solution, projects, tests, and Docker Compose configuration exist. Do not claim commands pass unless they have been run in the current checkout.
- The repository has no separate lint or formatter command; inspect project configuration before inventing verification commands.

## Source Of Truth

- Use `README.md` for the intended stack and local startup flow.
- Use `sdd/create-install.md` for solution creation, pinned package versions, Docker/PostgreSQL setup, User Secrets, migrations, and startup commands.
- Use `sdd/webapi-architecture.md` for layer responsibilities and dependency direction.
- Use `sdd/global-balance-sheet.md` for the account-plan data model and import invariants.
- `sdd/steps-balance-sheet.md` only lists the steps intended to be carried out; it is not itself implementation or planning guidance.
- Apply `sdd/dotnet-naming-and-code-documentation.md`; this project requires XML documentation for private members too, not only compiler-visible public members.

## Architecture

- The intended projects are `Domain`, `Application`, `Infrastructure`, and `Api` under `src/`.
- Dependency direction is inward: Domain has no project or web/data dependencies; Application references Domain and defines ports; Infrastructure implements those ports; Api references Application and Infrastructure and is the composition root.
- Keep HTTP DTOs and validation in Api, use-case inputs and orchestration in Application, business invariants in Domain, and persistence/file adapters in Infrastructure. Do not expose domain or persistence entities directly from HTTP responses.

## Local Setup

- Required tools are .NET SDK 10, Git, Docker with Compose, and access to FEPP's `Plan_Cuentas` directory.
- Create an unversioned root `.env` containing `POSTGRES_PASSWORD`, `PGADMIN_DEFAULT_EMAIL`, and `PGADMIN_DEFAULT_PASSWORD` before `docker compose up -d database pgadmin`.
- OpenCode's project `opencode.json` expects `CONTEXT7_API_KEY` from the process environment; `.env` files are not loaded automatically by .NET or OpenCode.
- On Windows, configure it with `[Environment]::SetEnvironmentVariable('CONTEXT7_API_KEY', 'YOUR_TOKEN', 'User')`, then restart the terminal and OpenCode. Never put the token in tracked files.
- Configure Api User Secrets for `ConnectionStrings:Default`, `Jwt:SigningKey`, `BootstrapAdmin:Password`, `Email:Smtp:Password`, and `FileImport:RootPath`. Never put credentials, JWT keys, SMTP passwords, or the real `Plan_Cuentas` path in tracked files.
- .NET environment-variable names use double underscores for configuration nesting, for example `ConnectionStrings__Default`, `Jwt__SigningKey`, and `FileImport__RootPath`.
- Invitation configuration uses `UserInvitation__TokenLifetimeHours` and `UserInvitation__ActivationBaseUrl`; the latter must be HTTPS.
- SMTP configuration uses only `Email__Smtp__Host`, `Port`, `SecurityMode`, `Username`, `Password`, `FromAddress`, `FromName`, and `TimeoutSeconds`. Supported modes are `StartTls` on port 587 and `TlsOnConnect` on port 465.
- Gmail SMTP requires a Google app password with two-step verification; a Google API key is not an SMTP credential. Do not use `MAILER_*`, `MAIL_*`, or `MAIL_TEMPLATE_EXTENSION` as current configuration.
- SMTP tests must use an in-process server and never connect to real Gmail. MailKit must remain isolated in Infrastructure; Domain and Application must not reference it.
- Never log or expose SMTP passwords, activation tokens, security stamps, complete activation links, email bodies, or recipient data.
- `Email__Templates__RootPath` is optional; the distributed `templates.json`, HTML, and TXT catalog must be validated at startup. `DataProtection__KeyRingPath` is optional locally but must be persistent in Docker/deployments.
- When the API runs on the host, PostgreSQL uses `Host=localhost`; containers use the service hostname `database`. A containerized API must use the mounted internal `FileImport:RootPath`.
- Once the solution exists, the documented order is: `dotnet restore FEPP-ContaOps.slnx`, `dotnet build FEPP-ContaOps.slnx`, `dotnet ef database update --project src/Fepp.ContaOps.Infrastructure --startup-project src/Fepp.ContaOps.Api`, then `dotnet run --project src/Fepp.ContaOps.Api`.

## Import Constraints

- Process each file in its own transaction. On failure, roll back both new rows and prior-account deactivation for that file; retain historical inactive rows.
- Require active regional/file associations and active parents from the current import; reject duplicate active account codes within a regional plan.
- A failed file must not stop later files. Accumulate errors and report file, regional, line/row, account code when available, and reason.
- Do not hard-code the import directory; read `FileImport:RootPath` from secure configuration.

## Workflow

- Use the SDD files as context before planning or implementing work. Consult `sdd/steps-balance-sheet.md` only to see the intended sequence of steps.
- The local `spec` skills are available under `.agents/skills/`. Their implementation workflow also expects `specs/.spec-config.yml`, which is not present in this checkout; do not assume that configuration exists.
