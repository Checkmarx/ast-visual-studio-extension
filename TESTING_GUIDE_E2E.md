# End-to-End Testing Guide - AST Visual Studio Extension

**Document ID:** AST-144920  
**Last Updated:** 2026-04-06  
**Test Suite:** Comprehensive Realtime Scanners Validation

---

## Overview

This document provides detailed end-to-end testing procedures for all 5 realtime scanners (ASCA, Secrets, IaC, Containers, OSS) in the Checkmarx One Assist Visual Studio extension.

## Environment Setup

### Prerequisites
- Visual Studio 2022 (or 2019 if applicable)
- Checkmarx CLI installed and in PATH
- Checkmarx One Assist extension installed
- Test solution with sample files (see below)

### Create Test Solution

Create a test solution with the following file structure:

```
TestSolution/
├── python_files/
│   └── sample.py          # ASCA: Python file with potential issues
├── csharp_files/
│   ├── Program.cs         # ASCA: C# file
│   └── secrets.cs         # Secrets: File with hardcoded credentials
├── terraform_files/
│   └── main.tf            # IaC: Terraform configuration
├── docker_files/
│   ├── Dockerfile         # Containers: Dockerfile
│   └── docker-compose.yml # Containers: Docker compose
├── npm_files/
│   ├── package.json       # OSS: npm manifest
│   └── package-lock.json  # OSS: npm lock file
├── java_files/
│   └── App.java           # ASCA: Java file
└── go_files/
    └── main.go            # ASCA: Go file
```

### Sample File Contents

**secrets.cs** (for Secrets scanner):
```csharp
public class ApiConfig
{
    public const string ApiKey = "sk-1234567890abcdefghijklmnopqrstuvwxyz";
    public const string Password = "admin123!@#";
}
```

**main.tf** (for IaC scanner):
```hcl
resource "aws_s3_bucket" "example" {
  bucket = "my-bucket"
  acl    = "public-read"  # IaC: Security issue - public read access
}
```

**package.json** (for OSS scanner):
```json
{
  "name": "test-app",
  "version": "1.0.0",
  "dependencies": {
    "vulnerable-package": "1.0.0"
  }
}
```

---

## Test Scenarios

### Scenario 1: Scanner Initialization

**Objective:** Verify scanners auto-register on solution open

**Steps:**
1. Open Visual Studio
2. Open the test solution
3. Check Debug Output window for messages:
   - "CxWindowPackage: Solution event handler registered"
   - "SolutionEventHandler: Solution opened, registering realtime scanners"
   - "[ScannerName] scanner initialized" (for each enabled scanner)

**Expected Result:** ✓ All scanners register without manual window open
**Priority:** HIGH

---

### Scenario 2: ASCA File Scanning

**Objective:** Verify ASCA scans Python, C#, Java, Go, JavaScript files

**Steps:**
1. Open each file in python_files/, csharp_files/, java_files/, go_files/
2. Wait for scan to complete (check Output Pane)
3. Verify findings appear in Error List

**Test Matrix:**
| File Type | File | Expected | Pass |
|-----------|------|----------|------|
| Python | sample.py | Findings or empty | [ ] |
| C# | Program.cs | Findings or empty | [ ] |
| Java | App.java | Findings or empty | [ ] |
| Go | main.go | Findings or empty | [ ] |

**Expected Result:** ✓ Each file type scans without errors
**Priority:** HIGH

---

### Scenario 3: Secrets Scanner

**Objective:** Verify hardcoded credentials are detected

**Steps:**
1. Open secrets.cs file
2. Wait for Secrets scan to complete
3. Check Error List for findings

**Expected Result:** ✓ Hardcoded API key and password detected
**Priority:** HIGH

---

### Scenario 4: IaC Scanner

**Objective:** Verify Terraform/YAML/JSON files scan correctly

**Steps:**
1. Open main.tf file
2. Wait for IaC scan to complete
3. Check for security misconfigurations

**Expected Result:** ✓ Public S3 bucket access detected
**Priority:** HIGH

---

### Scenario 5: Containers Scanner

**Objective:** Verify Dockerfile and docker-compose scanning

**Requirements:** Docker or Podman installed

**Steps:**
1. Open Dockerfile
2. Wait for Containers scan (may skip if Docker unavailable)
3. Open docker-compose.yml
4. Verify results or skip message

**Expected Result:** ✓ Scans complete or gracefully skip if Docker unavailable
**Priority:** MEDIUM

---

### Scenario 6: OSS Scanner

**Objective:** Verify dependency vulnerability detection

**Steps:**
1. Open package.json
2. Wait for OSS scan to complete
3. Verify vulnerable-package is detected (if in vulnerability database)
4. Confirm package-lock.json was copied to temp directory

**Expected Result:** ✓ Dependencies scanned, lock file handled
**Priority:** HIGH

---

### Scenario 7: Debounce Timing

**Objective:** Verify 2000ms debounce delay works correctly

**Steps:**
1. Open a Python file
2. Make rapid edits (5+ changes within 1 second)
3. Wait 2+ seconds
4. Check Output Pane for scan message

**Expected Result:** ✓ Only ONE scan triggered after 2s delay (not multiple)
**Priority:** MEDIUM

---

### Scenario 8: File Open - Instant Scan

**Objective:** Verify instant scan (no debounce) on file open

**Steps:**
1. Close all open files
2. Open a Python file by double-clicking
3. Check Output Pane for "triggering instant scan" message
4. Verify scan completes quickly

**Expected Result:** ✓ Instant scan without waiting for 2s debounce
**Priority:** MEDIUM

---

### Scenario 9: File Close - Cleanup

**Objective:** Verify pending scans are cancelled on file close

**Steps:**
1. Open a large Python file
2. Make an edit (starts debounce)
3. Immediately close the file
4. Check that debounce timer is cancelled
5. Verify temp files are cleaned up from %TEMP%

**Expected Result:** ✓ Timer cancelled, no scan triggered for closed file
**Priority:** MEDIUM

---

### Scenario 10: Manifest File Watcher

**Objective:** Verify dependency changes trigger re-scans

**Steps:**
1. Open npm_files/ folder
2. Create a new package.json or modify existing one
3. Wait 1-2 seconds
4. Check Output Pane for "Manifest file changed" message

**Expected Result:** ✓ Manifest file change detected and logged
**Priority:** MEDIUM

---

### Scenario 11: Error List Display

**Objective:** Verify Error List shows correct columns without truncation

**Steps:**
1. Run a scan that produces findings
2. Check Error List for:
   - No "+1" indicators (full content visible)
   - Correct line numbers
   - Severity level displayed
   - Scanner type in Category column
   - "Checkmarx One Assist" in Tool column

**Expected Result:** ✓ All columns display properly, no truncation
**Priority:** HIGH

---

### Scenario 12: Timeout Handling

**Objective:** Verify scans timeout gracefully after 60 seconds

**Steps:**
1. Create a very large file (>100MB) or one that causes slow scan
2. Wait for timeout
3. Check Output Pane for timeout message
4. Verify UI is still responsive
5. Verify user can continue working

**Expected Result:** ✓ Timeout after 60s, UI remains responsive
**Priority:** HIGH

---

### Scenario 13: File Size Validation

**Objective:** Verify files over 100MB are skipped

**Steps:**
1. Create a 150MB dummy file with .py extension
2. Open the file in VS
3. Wait for scan decision
4. Check Output Pane for "exceeds 100MB limit" message

**Expected Result:** ✓ File skipped with warning, not scanned
**Priority:** MEDIUM

---

### Scenario 14: Out-of-Order Completion

**Objective:** Verify stale results don't display from out-of-order scans

**Steps:**
1. Open a Python file
2. Edit file → Scan 1 starts (5 second duration)
3. Edit file again → Scan 2 starts (1 second duration)
4. Wait for both to complete
5. Verify only Scan 2's results display

**Expected Result:** ✓ Newest scan results displayed, older results discarded
**Priority:** MEDIUM

---

### Scenario 15: Settings Integration

**Objective:** Verify scanner enable/disable from settings

**Steps:**
1. Go to Tools → Options → Checkmarx One Assist
2. Disable ASCA checkbox
3. Make edit to Python file → Verify no ASCA scan triggered
4. Re-enable ASCA checkbox
5. Make edit to Python file → Verify ASCA scan triggered

**Test Each Scanner:**
- [ ] ASCA enable/disable
- [ ] Secrets enable/disable
- [ ] IaC enable/disable
- [ ] Containers enable/disable
- [ ] OSS enable/disable

**Expected Result:** ✓ Settings changes take effect immediately
**Priority:** MEDIUM

---

### Scenario 16: Output Pane Logging

**Objective:** Verify all scan events logged to Output Pane

**Steps:**
1. Open Output Pane (View → Output)
2. Select "Checkmarx" output pane
3. Run a scan
4. Verify messages appear:
   - "Scan started: [file]"
   - "Scan completed: X issue(s) found"
   - "Manifest file changed: [file]"

**Expected Result:** ✓ All events logged with timestamps
**Priority:** MEDIUM

---

### Scenario 17: Solution Close Cleanup

**Objective:** Verify all resources cleaned up on solution close

**Steps:**
1. Run several scans to generate temp files
2. Close the solution
3. Check %TEMP% directory
4. Verify no orphaned Cx-*-realtime-scanner directories remain

**Expected Result:** ✓ All temp directories cleaned
**Priority:** MEDIUM

---

### Scenario 18: Empty File Handling

**Objective:** Verify empty files are skipped gracefully

**Steps:**
1. Create empty.py file (0 bytes)
2. Open it in VS
3. Wait for scan decision

**Expected Result:** ✓ Logged as skipped (file content empty), not scanned
**Priority:** LOW

---

### Scenario 19: Performance Baseline

**Objective:** Establish performance metrics

**Steps:**
1. Run single file scan (Python, 1000 lines)
2. Measure time from edit to result display
3. Monitor memory usage during scan
4. Check CPU utilization

**Metrics to Record:**
| Metric | Expected | Actual |
|--------|----------|--------|
| Single scan duration | < 10s | [ ] |
| Memory peak | < 500MB | [ ] |
| CPU during scan | Reasonable | [ ] |
| UI responsiveness | Not frozen | [ ] |

**Expected Result:** ✓ Performance within acceptable range
**Priority:** MEDIUM

---

### Scenario 20: Multi-Scanner Concurrent Operation

**Objective:** Verify multiple scanners work simultaneously

**Steps:**
1. Open package.json (triggers OSS)
2. While OSS scans, open main.tf (triggers IaC)
3. While both scan, edit secrets.cs (triggers Secrets)
4. Verify all three scanners output to same pane without interleaving

**Expected Result:** ✓ Concurrent scans complete without conflicts
**Priority:** MEDIUM

---

## Regression Testing

Run these tests before each release:

- [ ] All 5 scanners register on solution open
- [ ] No "+1" truncation in Error List
- [ ] Timeout works (no UI freeze)
- [ ] File size validation works (>100MB skipped)
- [ ] Results are fresh (no stale display)
- [ ] Temp files cleaned up
- [ ] Output Pane logs all events
- [ ] Settings enable/disable works
- [ ] No crashes on startup or shutdown

---

## Known Limitations

1. **Containers Scanner** - Requires Docker/Podman installed; silently skips if unavailable
2. **Large Files** - Files >100MB will be skipped (by design)
3. **Network Paths** - Slow or may timeout on network shares
4. **Encoding** - Only UTF-8 supported; other encodings may cause issues

---

## Troubleshooting

### Scanners don't initialize
- [ ] Check if authenticated in VS
- [ ] Check Output Pane for error messages
- [ ] Verify CLI is in PATH: `cx.exe --version`

### No results appear
- [ ] Check Error List (may be filtered)
- [ ] Check Output Pane for scan errors
- [ ] Verify file type is supported by scanner

### UI freezes
- [ ] Timeout may be occurring (60s)
- [ ] Check for very large files (>100MB)
- [ ] Check Output Pane for timeout message

### High memory usage
- [ ] Close large files
- [ ] Restart VS
- [ ] Check for memory leaks in Debug output

---

## Sign-Off

**Tester Name:** _________________  
**Test Date:** _________________  
**All Tests Passed:** [ ] Yes [ ] No  
**Failures/Issues:** _________________

**QA Lead Approval:** _________________  
**Date:** _________________

