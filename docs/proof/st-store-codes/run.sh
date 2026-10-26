#!/usr/bin/env bash
# Intent: reproduce ST dispatch 1's focused store, CLI, and dispatcher evidence.
set -euo pipefail
export PATH="$(printf '%s' "$PATH" | tr ':' '\n' | grep -v 'recount-' | paste -sd: -)"
cd "$(dirname "$0")/../../.."
dotnet build CFDWorkbench.slnx -c Debug -nologo -v q
CFD_TEST_ONLY=PrefStore_OneHeldHandle tests/CfdWorkbench.Core.Tests/bin/Debug/net10.0/CfdWorkbench.Core.Tests.exe --harness
CFD_TEST_ONLY=Library_UnadmittedHost,Library_FolderRefusal,PrefStore_OwnerProbeThrows,WindowsStore_ParentIsFile tests/CfdWorkbench.Core.Tests/bin/Debug/net10.0/CfdWorkbench.Core.Tests.exe --harness
CFD_TEST_ONLY=Labels_MissingFile tests/CfdWorkbench.Analysis.Tests/bin/Debug/net10.0/CfdWorkbench.Analysis.Tests.exe --harness
CFD_TEST_ONLY=Compare_OldBuild tests/CfdWorkbench.Analysis.Tests/bin/Debug/net10.0/CfdWorkbench.Analysis.Tests.exe --harness
CFD_TEST_ONLY=PrefStore_ForeignOwnedFolder tests/CfdWorkbench.Core.Tests/bin/Debug/net10.0/CfdWorkbench.Core.Tests.exe --harness
CFD_TEST_ONLY=PrefStore_ExistingInheritedFolder tests/CfdWorkbench.Core.Tests/bin/Debug/net10.0/CfdWorkbench.Core.Tests.exe --harness
CFD_TEST_ONLY=WindowsStore_NewCodes tests/CfdWorkbench.Core.Tests/bin/Debug/net10.0/CfdWorkbench.Core.Tests.exe --harness
CFD_TEST_ONLY=WindowsStore_ReparseErrorCodes tests/CfdWorkbench.Core.Tests/bin/Debug/net10.0/CfdWorkbench.Core.Tests.exe --harness
CFD_TEST_ONLY=Store_Operation_ tests/CfdWorkbench.Core.Tests/bin/Debug/net10.0/CfdWorkbench.Core.Tests.exe --harness
CFD_TEST_ONLY=StoreCodes_ tests/CfdWorkbench.Desktop.Tests/bin/Debug/net10.0/CfdWorkbench.Desktop.Tests.exe --harness --controller-shell
tests/CfdWorkbench.Cli.Tests/bin/Debug/net10.0/CfdWorkbench.Cli.Tests.exe --harness
