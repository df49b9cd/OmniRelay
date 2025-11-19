## CI (GitHub Actions)
- Workflow `.github/workflows/ci.yml` runs on push/pr to main. Steps: checkout; cache NuGet; `docker build --target ci -f Dockerfile.ci -t omnirelay-ci .`; copy artifacts (test-results, coverage) from container; publish test reports via dorny/test-reporter; upload coverage to Codecov; upload artifacts.
- Docker-based build ensures parity; all tests run inside image to avoid host drift. Concurrency cancels duplicate runs per ref.

## Dockerfile.ci usage
- Multi-stage Alpine SDK image (`DOTNET_VERSION` ARG, default 10.0). Installs test deps (icu, libmsquic, python3, etc.) and sets `OMNIRELAY_ENABLE_HTTP3_TESTS=true`.
- Stages: `restore` (dotnet restore OmniRelay.slnx), `build` (Release, no restore), `ci` (runs all test projects via `TEST_PROJECTS` ARG; collects TRX and XPlat Code Coverage to `/repo/artifacts`).
- Run locally for CI parity:
  - `docker build --target ci -f Dockerfile.ci -t omnirelay-ci .`
  - Extract results: `cid=$(docker create omnirelay-ci)`; `docker cp $cid:/repo/artifacts ./artifacts && docker rm $cid`
  - Override SDK/test set: `docker build --build-arg DOTNET_VERSION=10.0.100 --build-arg "TEST_PROJECTS=tests/OmniRelay.Core.UnitTests/OmniRelay.Core.UnitTests.csproj" --target ci -f Dockerfile.ci -t omnirelay-ci .`

## Publish workflow
- `.github/workflows/publish-packages.yml` triggers on tags `v*` or manual dispatch. Uses setup-dotnet (preview), restore, build+test with coverage (artifacts uploaded), then packs selected projects with `OmniRelayVersion/PackageVersion` set from tag or input. Uploads packages as artifacts, creates GitHub Release, and pushes to GitHub Packages + NuGet.org (secrets required).

## Notes
- Coverage files aggregated from container or test runs; Codecov uploads are non-blocking.
- Keep global.json/.props in sync with Dockerfile.ci to avoid restore drift.
