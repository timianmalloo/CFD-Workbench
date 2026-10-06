# GRP half 1 - red-first receipt (round oct06, track trk-grp-core)

Ring: fast. Command: CFD_TEST_ONLY=GroupGesture,ApplyGroupValue tools/run-suite.sh dotnet tests/CfdWorkbench.Core.Tests/bin/Release/net10.0/CfdWorkbench.Core.Tests.dll

## Red on the old Core (HEAD~1 AuthoringSession.cs, new tests)

The suite does not compile: the group API does not exist.

    tests/CfdWorkbench.Core.Tests/GroupGestureTests.cs(132,23): error CS1061: 'AuthoringSession' does not contain a definition for 'BeginGroupGe
    tests/CfdWorkbench.Core.Tests/GroupGestureTests.cs(134,27): error CS1061: 'AuthoringSession' does not contain a definition for 'UpdateGroupG
    tests/CfdWorkbench.Core.Tests/GroupGestureTests.cs(143,40): error CS1061: 'AuthoringSession' does not contain a definition for 'BeginGroupGe
    tests/CfdWorkbench.Core.Tests/GroupGestureTests.cs(163,27): error CS1061: 'AuthoringSession' does not contain a definition for 'BeginGroupGe

## Planted mutants (each reverted after its run)

    ### mutant A: clamp per member instead of rigid
    Build succeeded.
    FAIL GroupGesture_ChannelDomain_HoldsGroupNotMember InvalidOperationException: expected 0.8799999000000001; actual 0.8999999000000001
    RESULT failures=1
    SUBSET CFD_TEST_ONLY=GroupGesture_ChannelDomain ran=1 skipped=719
    PASS lines: 0
    ### mutant B: a handle seeded twice
    Build succeeded.
    FAIL GroupGesture_HandleNeverSeededTwice InvalidOperationException: expected 0.003; actual 0.006000030000000017
    RESULT failures=1
    SUBSET CFD_TEST_ONLY=GroupGesture_HandleNeverSeededTwice,GroupGesture_AnchorBringsHandles ran=2 skipped=718
    PASS lines: 1

## Green on the new Core: every design section 9 Core name

    PASS GroupGesture_OneMember_EqualsSinglePointGesture
    PASS GroupGesture_Translates_AllMembers_OneDraft
    PASS GroupGesture_IncludesTipVertex_HoldsWholeGroupAtMinimum
    PASS GroupGesture_IncludesRootEnd_HoldsWholeGroupAtRootMax
    PASS GroupGesture_RootBelow250_NeverClamps
    PASS GroupGesture_NeighbourSpacing_HoldsWholeGroupRigid
    PASS GroupGesture_ChannelDomain_HoldsGroupNotMember
    PASS GroupGesture_FixedMember_RefusedNamingLock
    PASS GroupGesture_ValueOnlyMember_HoldsSpanForGroup
    PASS GroupGesture_RootSeeded_WhenPointOneSelected
    PASS GroupGesture_AnchorBringsHandles
    PASS GroupGesture_HandleWithoutAnchor_Refused
    PASS GroupGesture_HandleNeverSeededTwice
    PASS GroupGesture_LegacyTipUnderMinimum_HoldsAtPressChord
    PASS GroupGesture_ReleaseNeverThrowsTipChordMin
    PASS GroupGesture_End_CarriesMembers
    PASS ApplyGroupValue_SetTo_AllEqual
    PASS ApplyGroupValue_MoveBy_AllShifted
    PASS ApplyGroupValue_AnyViolation_NothingApplied
    PASS ApplyGroupValue_MoveByPastTip_NamesMaxAmount
    RESULT failures=0
    SUBSET CFD_TEST_ONLY=GroupGesture,ApplyGroupValue ran=20 skipped=700
