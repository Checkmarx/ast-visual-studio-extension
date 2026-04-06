# Enhanced Scanner Logging Guide

**Date:** 2026-04-06  
**Commit:** `c953e89`  
**Purpose:** Improve visibility into all 5 realtime scanner activities

---

## Overview

Enhanced logging has been added to BaseRealtimeScannerService to show:
1. Scanner registration on startup
2. File type filtering decisions
3. Scan start and completion with timing
4. Issue counts for each scan

This helps troubleshoot why certain scanners might not be appearing in the Output pane.

---

## What You'll See in Output Pane

### Scanner Registration (on solution open)

```
✓ ASCA scanner: Monitoring enabled
✓ Secrets scanner: Monitoring enabled
✓ IaC scanner: Monitoring enabled
✓ Containers scanner: Monitoring enabled
✓ OSS scanner: Monitoring enabled
```

### Debug Output (View → Debug Output or Output Pane → Show output from: Debug)

When you edit files, you'll see:

**File Type Matching:**
```
ASCA: Starting scan on CxWrapper.cs
ASCA: Scan completed in 245ms - 0 issue(s) found

Secrets: Skipping Program.cs - file type not applicable
IaC: Skipping Requirements.txt - file type not applicable
Containers: Skipping Dockerfile - file type not applicable (no Docker installed)
OSS: Starting scan on package.json
OSS: Scan completed in 1234ms - 3 issue(s) found
```

---

## Logging Points

### 1. Scanner Registration

**When:** Solution opens or scanner initializes  
**Output Pane Message:**
```
✓ ASCA scanner: Monitoring enabled
```

**Debug Output:**
```
✓ ASCA scanner: Successfully registered for text change and document lifecycle events.
```

### 2. File Processing Starts

**When:** Text edit triggers debounce timer (2000ms)  
**Debug Output:**
```
ASCA: Starting scan on CxWrapper.cs
```

### 3. File Type Check

**When:** `ShouldScanFile()` is evaluated  
**Debug Output (if skip):**
```
IaC: Skipping package.json - file type not applicable
```

**Why Skipped:**
- **ASCA:** Only `.py`, `.cs`, `.go`, `.java`, `.js`, `.ts`, `.cpp`, `.c`, `.h`, etc.
- **Secrets:** All files EXCEPT manifests
- **IaC:** Only `.tf`, `.yaml`, `.yml`, `.json`, `.hcl`, `dockerfile`, etc.
- **Containers:** Only `dockerfile`, `docker-compose.yml`, Helm charts
- **OSS:** Only manifests (`package.json`, `pom.xml`, `requirements.txt`, `*.csproj`, etc.)

### 4. Empty Content Check

**When:** File content is empty/whitespace  
**Debug Output:**
```
ASCA: Skipping empty_file.py - content is empty
```

### 5. Scan Completion

**When:** Scan finishes (success or error)  
**Debug Output:**
```
ASCA: Scan completed in 245ms - 0 issue(s) found
Secrets: Scan completed in 156ms - 1 issue(s) found
```

---

## How to View Logs

### Output Pane (Recommended)

1. Open **View → Output**
2. Select **"Show output from: Checkmarx"** (dropdown)
3. Watch for scanner registration and scan events

**Format:**
```
[Checkmarx] [HH:MM:SS.fff] SCANNER_NAME: Message
```

### Debug Output (More Verbose)

1. Open **View → Output**
2. Select **"Show output from: Debug"** (dropdown)
3. Watch for Debug.WriteLine() calls

**Format:**
```
SCANNER_NAME: Message
```

---

## Troubleshooting

### I don't see scanner registration messages

**Possible causes:**
1. Scanners not initialized (authentication issue)
2. Output Pane not showing "Checkmarx" output
3. Extension not loaded

**Solution:**
- Check if authenticated in **Tools → Options → Checkmarx One Assist**
- Verify API key is set
- Restart Visual Studio

### A scanner is not appearing in logs

**Possible causes:**
1. File type not matching (see file type rules above)
2. File filtered out by exclusion rules (e.g., `/node_modules/`)
3. Scanner disabled in settings
4. File content is empty

**Solution:**
- Open a file that matches scanner's file type:
  - **ASCA:** Create a `.py` or `.cs` file
  - **Secrets:** Edit any `.cs` file
  - **IaC:** Create a `.tf` or `dockerfile`
  - **Containers:** Create a `dockerfile`
  - **OSS:** Create a `package.json`
- Edit the file to trigger scan
- Check Debug output for skip reasons

### Scan takes too long

**Possible causes:**
1. Large file being scanned (>100MB)
2. CLI timeout happening
3. Slow network/disk I/O

**Solution:**
- Check if file size > 100MB (will be skipped)
- Check Output Pane for timeout message
- Look at Debug output for timing: `Scan completed in Xms`

### I see "No security best practice violations found"

**What this means:**
- Scan completed successfully
- No issues found by that scanner
- This is normal behavior (not an error)

---

## Log Format Reference

### Success Log
```
[Checkmarx] [12:45:30.123] ASCA: Scan completed: 2 issue(s) found in CxWrapper.cs
```

### Skip Log
```
[Checkmarx] [12:45:31.456] IaC: Skipping Constants.cs - file type not applicable
```

### Error Log
```
[Checkmarx] [12:45:32.789] ERROR: ASCA scan failed - timeout after 60s
```

---

## Expected Behavior

### On Solution Open
```
[Checkmarx] CxWindowPackage: Solution event handler registered
[Checkmarx] SolutionEventHandler: Solution opened, registering realtime scanners
[Checkmarx] ✓ ASCA scanner: Monitoring enabled
[Checkmarx] ✓ Secrets scanner: Monitoring enabled
[Checkmarx] ✓ IaC scanner: Monitoring enabled
[Checkmarx] ✓ Containers scanner: Monitoring enabled
[Checkmarx] ✓ OSS scanner: Monitoring enabled
```

### On File Edit
```
[Checkmarx] [HH:MM:SS.fff] ASCA: Scan started: CxWrapper.cs
[Checkmarx] [HH:MM:SS.fff] ASCA: Scan completed: 0 issue(s) found
[Checkmarx] [HH:MM:SS.fff] Secrets: Scan started: CxWrapper.cs
[Checkmarx] [HH:MM:SS.fff] Secrets: Scan completed: 0 issue(s) found
```

### On Non-Matching File
```
[Checkmarx] [HH:MM:SS.fff] IaC: Skipping Constants.cs - file type not applicable
[Checkmarx] [HH:MM:SS.fff] Containers: Skipping Constants.cs - file type not applicable
```

---

## File Type Matching Rules

| Scanner | Scans These Files | Skips These Files |
|---------|------------------|------------------|
| **ASCA** | `.cs`, `.py`, `.java`, `.go`, `.js`, `.ts`, `.cpp`, `.c`, `.h` | Everything else |
| **Secrets** | All except manifests | `package.json`, `pom.xml`, `.csproj`, lock files, `go.mod`, etc. |
| **IaC** | `.tf`, `.yaml`, `.yml`, `dockerfile`, `docker-compose.yml` | Everything else |
| **Containers** | `dockerfile`, `docker-compose.yml`, Helm charts | Everything else |
| **OSS** | `package.json`, `pom.xml`, `requirements.txt`, `*.csproj`, `go.mod`, etc. | Everything else |

---

## FAQ

**Q: Why don't I see Secrets scanner logs?**  
A: Secrets scanner scans ALL file types except manifests. The file you're editing might not have secrets. Check Debug output to confirm it's being scanned.

**Q: Why is IaC scanner skipping my JSON file?**  
A: IaC only scans configuration-related JSON files (CloudFormation, etc.). Regular JSON files are skipped. Try a `.tf` file instead.

**Q: Can I disable logs?**  
A: Debug output is controlled by Debug configuration. Output Pane logs can't be disabled - they're important for troubleshooting.

**Q: Why is Containers scanner skipping my Dockerfile?**  
A: Containers scanner requires Docker or Podman to be installed. If not available, it silently skips.

---

## Git Commit

**Commit:** `c953e89`  
**Message:** "Enhance scanner logging for better visibility - show all scanner activity"  
**Files Modified:**
- `BaseRealtimeScannerService.cs` - Added 6 new logging points

