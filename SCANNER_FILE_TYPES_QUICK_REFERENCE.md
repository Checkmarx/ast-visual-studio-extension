# Scanner File Types - Quick Reference

**Purpose:** Quick lookup for which files each scanner processes

---

## ASCA Scanner ✓

**Scans ONLY these file types:**
- `.py` - Python
- `.cs` - C#
- `.java` - Java
- `.go` - Go
- `.js`, `.jsx` - JavaScript
- `.ts`, `.tsx` - TypeScript
- `.cpp`, `.c`, `.h`, `.cc`, `.cxx`, `.hh`, `.hpp` - C/C++

**Excludes:**
- Anything in `/node_modules/`, `/venv/`, `/dist/`, `/build/`
- Manifest files
- Config files

---

## Secrets Scanner ✓

**Scans:** ALL files EXCEPT the ones listed below

**Skips these file types:**
- Manifests: `package.json`, `pom.xml`, `requirements.txt`, `go.mod`, `Gemfile`, `composer.json`, `Pipfile`
- Lock files: `package-lock.json`, `yarn.lock`, `pom.xml.lock`, `go.sum`
- Project files: `.csproj`, `.vbproj`, `.fsproj`
- Config: `.checkmarxIgnored`, `.checkmarxIgnoredTempList`

**Excludes:**
- Files in `/node_modules/`, `/.git/`

---

## IaC Scanner ✓

**Scans ONLY these file types:**
- `.tf` - Terraform
- `.tfvars` - Terraform variables
- `.yaml`, `.yml` - YAML
- `.json` - JSON
- `.hcl` - HCL
- `.bicep` - Azure Bicep
- `.arm` - ARM templates
- `dockerfile` / `Dockerfile*` - Docker
- `docker-compose.yml` / `docker-compose.yaml`
- `buildspec.yml` / `buildspec.yaml`

**Excludes:**
- Non-IaC files
- Non-matching extensions

---

## Containers Scanner ✓

**Scans ONLY these file types:**
- `dockerfile` / `Dockerfile*` (Dockerfile, Dockerfile.dev, etc.)
- `docker-compose.yml` / `docker-compose.yaml`
- Helm charts (`.yaml` / `.yml` files in `/helm/` directory)

**Excludes:**
- `chart.yml`, `chart.yaml` (Helm chart metadata)
- Non-container files

**Requires:** Docker or Podman installed

---

## OSS Scanner ✓

**Scans ONLY these manifest files:**

### Dependency Manifests
- `package.json` - npm
- `pom.xml` - Maven
- `requirements.txt` - Python pip
- `go.mod` - Go
- `go.sum` - Go checksums
- `packages.config` - NuGet
- `Gemfile` - Ruby gems
- `composer.json` - PHP Composer
- `Pipfile` - Python poetry
- `setup.py` - Python setuptools
- `pubspec.yaml` - Dart/Flutter
- `Cargo.toml` - Rust
- `mix.exs` - Elixir

### Project Files
- `*.csproj` - .NET C#
- `*.vbproj` - .NET VB
- `*.fsproj` - .NET F#

---

## Summary Table

| File Type | ASCA | Secrets | IaC | Containers | OSS |
|-----------|------|---------|-----|------------|-----|
| `.cs` | ✓ | ✓ | ✗ | ✗ | ✗ |
| `.py` | ✓ | ✓ | ✗ | ✗ | ✗ |
| `.js` | ✓ | ✓ | ✗ | ✗ | ✗ |
| `.tf` | ✗ | ✓ | ✓ | ✗ | ✗ |
| `.yaml` | ✗ | ✓ | ✓ | ✓* | ✗ |
| `dockerfile` | ✗ | ✓ | ✓ | ✓ | ✗ |
| `package.json` | ✗ | ✗ | ✓ | ✗ | ✓ |
| `pom.xml` | ✗ | ✗ | ✓ | ✗ | ✓ |
| `requirements.txt` | ✗ | ✓ | ✓ | ✗ | ✓ |
| `go.mod` | ✗ | ✓ | ✓ | ✗ | ✓ |
| `.csproj` | ✗ | ✗ | ✗ | ✗ | ✓ |

*IaC scans all `.yaml`, Containers only in `/helm/` or as Helm chart

---

## Test Files to Create

To test all scanners, create:

```
TestProject/
├── test.cs              # → ASCA ✓, Secrets ✓
├── test.py              # → ASCA ✓, Secrets ✓
├── test.js              # → ASCA ✓, Secrets ✓
├── main.tf              # → IaC ✓, Secrets ✓
├── dockerfile           # → IaC ✓, Containers ✓, Secrets ✓
├── docker-compose.yml   # → IaC ✓, Containers ✓, Secrets ✓
├── package.json         # → OSS ✓, Secrets ✓
├── pom.xml              # → OSS ✓
├── requirements.txt     # → OSS ✓
└── go.mod               # → OSS ✓
```

**Expected Results:**
- `test.cs`: ASCA (0 issues), Secrets (depends on content)
- `test.py`: ASCA (0 issues), Secrets (depends on content)
- `main.tf`: IaC (depends on config)
- `dockerfile`: Containers (if Docker installed), IaC
- `package.json`: OSS (depends on dependencies)

---

## Notes

1. **File Extensions are Case-Insensitive**
   - `.CS`, `.Cs`, `.cs` all match

2. **Secrets Scanner is Broadest**
   - Scans everything except manifest/config files
   - Most likely to produce logs

3. **OSS Scanner is Most Specific**
   - Only scans exact manifest files
   - Smallest number of scans

4. **IaC and Containers Overlap**
   - Both scan `dockerfile` and `docker-compose.yml`
   - IaC also handles `.tf`, `.yaml`, `.json` configs

5. **ASCA Focused on Source Code**
   - Only language-specific files
   - Won't scan config, scripts, or manifests

