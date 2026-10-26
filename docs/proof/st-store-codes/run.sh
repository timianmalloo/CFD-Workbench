#!/usr/bin/env bash
# Intent: reproduce ST dispatch 1's focused store, CLI, and dispatcher evidence.
set -euo pipefail
cd "$(dirname "$0")/../../.."
dotnet build CFDWorkbench.slnx -c Debug -nologo -v q
CFD_TEST_ONLY=WindowsStore_NewCodes tests/CfdWorkbench.Core.Tests/bin/Debug/net10.0/CfdWorkbench.Core.Tests.exe --harness
CFD_TEST_ONLY=WindowsStore_ReparseErrorCodes tests/CfdWorkbench.Core.Tests/bin/Debug/net10.0/CfdWorkbench.Core.Tests.exe --harness
CFD_TEST_ONLY=Store_Operation_ tests/CfdWorkbench.Core.Tests/bin/Debug/net10.0/CfdWorkbench.Core.Tests.exe --harness
CFD_TEST_ONLY=StoreCodes_ tests/CfdWorkbench.Desktop.Tests/bin/Debug/net10.0/CfdWorkbench.Desktop.Tests.exe --harness --controller-shell
tests/CfdWorkbench.Cli.Tests/bin/Debug/net10.0/CfdWorkbench.Cli.Tests.exe --harness
