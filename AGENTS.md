# Agent Instructions

## Repository State

- This checkout is currently a documentation scaffold: there is no `.slnx`, `src/`, test project, CI configuration, or `compose.yaml` yet.
- Do not claim that build, test, migration, or Docker commands pass until the corresponding files exist and the commands have been run.
- No repository-local test, lint, formatter, or typecheck command is defined yet; inspect new project configuration before inventing verification commands.

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
- Configure Api User Secrets for `ConnectionStrings:Default`, `Jwt:SigningKey`, and `FileImport:RootPath`. Never put credentials, JWT keys, or the real `Plan_Cuentas` path in tracked files.
- .NET environment-variable names use double underscores for configuration nesting, for example `ConnectionStrings__Default`, `Jwt__SigningKey`, and `FileImport__RootPath`.
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
