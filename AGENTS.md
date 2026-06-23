# AGENTS.md

## Cursor Cloud specific instructions

VPureLux is a single **ABP Framework (v10.4.1) layered monolith ERP** built on **.NET 10** + ASP.NET Core MVC/Razor Pages (LeptonX theme). See `README.md` for the canonical solution structure, build, cert, and DbMigrator steps. The notes below only capture non-obvious, environment-specific caveats.

### Critical: ABP commercial credentials are required to restore packages
This solution depends on ABP **commercial** packages (NuGet `Volo.*.Pro`, `Volo.Saas`, `Volo.Chat`, `Volo.CmsKit.Pro`, `...LeptonX`; npm `@volo/*`). These are served from ABP's **private** feeds, not public nuget.org / npmjs.org. Without authentication, `dotnet restore`/`dotnet build` fail with `NU1101: Unable to find package Volo.*` and `abp install-libs` fails to fetch `@volo/*`.

- Authenticate once with `abp login "$ABP_USERNAME" --password "$ABP_PASSWORD"`. Use `--password`, NOT the `-p` shorthand: with ABP Studio CLI 3.x the `-p` form falls through to an interactive browser login that hangs in a headless VM.
- The ABP Studio CLI login does **not** auto-add the commercial NuGet source. Add it manually (once; it persists in `~/.nuget/NuGet/NuGet.Config`): fetch your org `apiKey` from `https://abp.io/api/license/api-key` using the CLI bearer token in `~/.abp/cli/access-token.bin`, then `dotnet nuget add source "https://nuget.abp.io/<apiKey>/v3/index.json" -n "ABP Commercial NuGet Source"`. Without this, `dotnet restore` fails with `NU1101: Unable to find package Volo.*`.
- The `@volo/*` npm packages resolve from the public npm registry, but `abp install-libs` runs Yarn 1.x which aborts on an engine check (`select2` wants node >=24). Run `yarn config set ignore-engines true` once (persists in `~/.yarnrc`) so install-libs succeeds on node 22.
- The runtime ABP license code is already committed in each `appsettings.secrets.json` (`AbpLicenseCode`); that license is for runtime only and does NOT grant package-feed access.
- After login + source setup: `dotnet restore VPureLux.slnx` then `abp install-libs` (run from `src/VPureLux.Web` and `src/VPureLux.Web.Public`). Restore + install-libs are also wired into the Cursor Cloud update script.

### Toolchain (baked into the VM snapshot)
- .NET 10 SDK at `/usr/local/dotnet` (symlinked to `/usr/local/bin/dotnet`, so `dotnet` is on PATH).
- ABP CLI installed globally as `abp` (Volo.Abp.Studio.Cli). It requires `DOTNET_ROOT=/usr/local/dotnet`; `PATH`/`DOTNET_ROOT` are set in `~/.bashrc`.
- Node v22 + Yarn 1.x are preinstalled (README mentions Node 18/20; v22 is what's available here).

### Infrastructure services (Docker)
The Docker daemon is NOT started automatically. Start it with `sudo dockerd > /tmp/dockerd.log 2>&1 &` (configured for `fuse-overlayfs` + iptables-legacy). Note: the installed Docker is v29, which defaults to the containerd snapshotter and breaks `fuse-overlayfs`; `/etc/docker/daemon.json` therefore sets `"features": { "containerd-snapshotter": false }`. Then the app needs:
- **Redis**: `sudo docker run -d --name redis -p 6379:6379 redis:alpine` (required for distributed locking on every run, even in Development).
- **SQL Server**: `sudo docker run -d --name sql-server -p 1434:1433 -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=myPassw0rd mcr.microsoft.com/azure-sql-edge:1.0.7`.
- Do NOT use `docker compose up` from `etc/docker-compose/` to bring up the whole stack — that also tries to build the web/web-public/db-migrator images (slow, and needs ABP creds). Start only `redis` + `sql-server` and run the apps with `dotnet run`.

### Database connection
`appsettings.json` `ConnectionStrings:Default` points to a **remote** SQL Server (`103.172.236.78` / DB `VPL`), which is reachable from this VM. For isolated local dev, override to the local container, e.g. `ConnectionStrings__Default="Data Source=localhost,1434;Initial Catalog=VPureLux;User Id=sa;Password=myPassw0rd;TrustServerCertificate=True;MultipleActiveResultSets=true"`.

### Signing certificate
`openiddict.pfx` (password `b6b8be5a-c72c-4ee6-9377-06ce8d0541a1`) must exist in `src/VPureLux.Web` and `src/VPureLux.Web.Public`. Generate with `dotnet dev-certs https -ep openiddict.pfx -p b6b8be5a-c72c-4ee6-9377-06ce8d0541a1`. These files are untracked/local-only — do not commit them.

### Run / migrate / test (after restore succeeds)
- Seed DB (run once, and after new migrations): build first (`dotnet build src/VPureLux.DbMigrator -c Release`) then run the built DLL directly: `dotnet src/VPureLux.DbMigrator/bin/Release/net10.0/VPureLux.DbMigrator.dll`. Running it via `dotnet run --project src/VPureLux.DbMigrator` can exit with code 214 without seeding (a `dotnet run` host quirk for this console app); the DLL form logs `Successfully completed all database migrations` and exits 0. Seeds 103 tables + admin user and customer groups (RETAIL/DEALER/etc.). No sample customer is seeded by code today; create a UAT customer explicitly before Sales testing.
- Admin app + auth server: `dotnet run --project src/VPureLux.Web` → `https://localhost:44325` (default seeded admin is `admin` / `1q2w3E*`).
- Public site (optional): `dotnet run --project src/VPureLux.Web.Public` → `https://localhost:44304` (depends on the Web auth server).
- Tests: `dotnet test VPureLux.slnx` (test projects use EF Core SQLite in-memory + in-memory distributed lock, so they don't need the SQL Server / Redis containers).
