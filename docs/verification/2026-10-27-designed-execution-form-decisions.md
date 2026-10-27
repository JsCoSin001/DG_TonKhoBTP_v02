# SDD ledger — plan: docs/superpowers/plans/2026-10-27-execution-detail-designed-form.md

Base: 6900707f082eb0d1732c47b25b62ffcdd3b50549.

Task 1: in progress. Baseline Debug build passed (existing warnings). New form is only InitializeComponent.
Pre-flight: Task 1 produces new form constructors and LastSaveResult consumed by Task 2 parent opener; signatures match existing query/snapshot model. Database interfaces unchanged.
Ruling: Work in the existing codex/b2-b3-execution-edit checkout — preserve user's prior choice to work in place and current unsaved-to-git Designer files — no new worktree or reset.
Ruling: lblTacDong is the error display exception to frozen Label captions — user's final explanation establishes this dynamic role — no RichTextBox needed; all other captions remain untouched.
Ruling: The reason is common to one explicit save and goes through existing Note/GhiChu while Reason keeps INFO/MISTAKE/MARKERS — supports mixed row/reel edits without changing database validation — per-event differing prose would require a separate UI in a future request.
Evidence: Designer SHA256 C85201A8EEC46EC64727F23401BF032653D511D5770ADE25AC2EC36DEEEAB49E; resx SHA256 4363CD7D5B8671C72442CE1A1BFC10D64EBD24B2D718B54BD4FCD025E4967298 before implementation.
Test harness: Windows PowerShell requires UTF-8 BOM for Vietnamese literals; initial parser failure is not RED evidence and must be fixed before proving the feature absent.

RED: ExecutionEdit.DesignedForm.Tests.ps1 -Case Binding failed with the intended assertion: Designed form must accept the existing execution query and bind actual business data.
Ruling: Put event/binding logic in Frm_ChiTietThucHienKeHoach.Execution.cs, a focused partial alongside the designer-owned constructor — avoid reworking the user's file and keep generated code unchanged — one extra csproj entry, no domain interface change.
Test harness correction: Show the off-screen form to install the actual WinForms synchronization context before async Save; invoking PowerShell delegates on thread-pool threads is a harness error, not a production transaction defect.
Ruling: The parent opener is a one-line type substitution; verify it by diff plus actual new-form integration tests, not an implementation-mirroring source-text assertion.

Task 1: functional verification green — 11 designed-form cases, existing UI/review tests. Native async driver installs WinForms SynchronizationContext inside C# so callback test delegates execute on the actual UI thread; all 11 cases pass.
Task 2: Debug and Release builds passed. All 14 ExecutionEdit scripts ran: 13 pass, 1 explicit real-database test skips without a path. Three native form images rendered and inspected. Designer/resx hashes identical to pre-edit evidence. No Database/B3/SQL modifications.
Broader suite: 9 scripts ran; 5 pass, 2 require explicit database and skip, 2 shift-time scripts fail (Ending 19/13, StartDate 20/1). Exact failure names recorded in verification document. The shift-time sources/tests are untouched; do not expand this form migration into unrelated shift validation repairs.
Ruling: Preserve the user's static sample captions too, including label1 and N01; only lblTacDong changes text — user's explicit freeze — those captions may not describe a different selected product/operation, while the actual grid/detail values remain correct.

Task 1: complete (commits 6900707..87c6101; tests: ExecutionEdit.DesignedForm.Tests.ps1 11/11; ExecutionEdit.UI.Tests.ps1 pass; ExecutionEdit.Review.Tests.ps1 7/7).
Task 2: complete (commits 6900707..87c6101; Debug/Release builds exit0; entire ExecutionEdit wildcard runner exit0, 13 scripts pass/1 skip; Preview three cases pass; broad results reported separately).
Ruling: Do not repeat passing suites solely to write helper completion lines — command outputs above already prove completion and developer instructions prohibit unjustified redundant tests — record the actual commands/results manually in this Windows ledger.
Final review: dispatched fresh gpt-6-astra/high read-only reviewer for 6900707..87c6101 per executing-plans skill. No second review planned; Important/Critical findings get one RED-to-GREEN fix pass.
Ruling: Generated Designer/resx whitespace is kept byte-for-byte to honor the user-owned design — cached diff check reports inherited trailing spaces in newly tracked generated files; own implementation files have no whitespace errors — cosmetic whitespace remains in generated sources only.

Final review received: no Critical/Important, two labelled Minor. Root re-grades both as Important by effect for this release: identical-value reload leaves a user-facing save/close workflow stuck on nonexistent changes (regression against dirty-state contract), and the confirmation-race test did not exercise a specifically required concurrency boundary (false verification evidence). Both enter one fix pass; no cosmetic fixes added.
RED: new IdenticalReload case reproduces phantom unsaved state; MarkerConfirm now asserts a separate post-confirmation conflict prompt and fails on original callback. Production Save's existing version validation itself is unchanged.
Final: Ruling: earlier B2/B3 implementation remains outside this review range — this plan migrates only the form and the services were already reviewed/tested; current integration suites still exercise them — older unrelated defects would need their own scope.
Final: Ruling: existing shift failures are reported rather than repaired — no shift source/test changes in this plan — those workflows remain outside this feature's readiness claim.
Final: Ruling: original user database was not opened in this run — test only isolated fixtures per scope — deployment must still use the already documented shared schema prerequisites.
Final: Ruling: independent reviewer read build evidence rather than rerunning suites — parent ran and read all outputs in this session — final verification remains accountable to this ledger and report.
Final: Ruling: plan checkbox edits are parent-owned completion metadata, not new behavior — implementation diff already reviewed — no extra code review needed for checkbox changes.

Final: fixed identical-marker phantom draft — IdenticalReload RED→GREEN, valid baseline-equal raw inputs removed after reload while invalid/different inputs retained.
Final: fixed false confirmation-race coverage — MarkerConfirm RED→GREEN, callback returns snapshotted answer, asserts separate conflict prompt after accepting confirmation, no write on stale version.
Final suite: 12/12 designed-form cases pass; all 14 ExecutionEdit scripts ran after fixes (13 pass/1 optional DB skip), Debug and Release builds pass. No code changes to DB/B3/SQL. Designer/resx hashes unchanged. No remaining scoped Critical/Important/Minor findings.
Finishing: retain current local branch, no merge/push/deploy requested; broader shift-time tests remain red and are reported, so no all-project-green or integration-readiness claim is made. User's checkout stays in place. Archive this ledger before removing only this plan's scratch workspace.
