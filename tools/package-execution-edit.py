"""Package local build artifacts without reading or changing application databases."""
from pathlib import Path
import hashlib
import json
import subprocess
import zipfile
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "artifacts/execution-edit/release"
BUILD = ROOT / "DG_TonKhoBTP_v02/bin/Release"
MIGRATION = ROOT / "database/Migration_B2B3_ExecutionEdit_20261026.sql"


def main():
    if not (BUILD / "DG_TonKhoBTP_v02.exe").is_file():
        raise SystemExit("Build Release / Any CPU before packaging")
    OUTPUT.mkdir(parents=True, exist_ok=True)
    commit = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip()
    b2 = OUTPUT / "B2-execution-edit.zip"
    with zipfile.ZipFile(b2, "w", zipfile.ZIP_DEFLATED) as archive:
        for path in sorted(BUILD.rglob("*")):
            if not path.is_file() or path.suffix.lower() in {".db", ".sqlite", ".pdb", ".xml"}:
                continue
            relative = path.relative_to(BUILD)
            if path.name.endswith(".exe.config"):
                tree = ET.parse(path)
                for setting in tree.findall(".//userSettings/*/setting"):
                    if setting.get("name") in {"SavedLoginUserName", "SavedLoginPassword", "UserPass"}:
                        for value in setting.findall("value"):
                            value.text = ""
                archive.writestr(str(relative).replace("\\", "/"), ET.tostring(tree.getroot(), encoding="utf-8", xml_declaration=True))
            else:
                archive.write(path, relative)
        archive.writestr("BUILD.txt", commit + "\nRelease / Any CPU / .NET Framework 4.8\n")
    tracked = subprocess.check_output(["git", "ls-files", "-z", "integrations/B3"], cwd=ROOT).decode().split("\0")
    b3 = OUTPUT / "B3-execution-edit-source.zip"
    with zipfile.ZipFile(b3, "w", zipfile.ZIP_DEFLATED) as archive:
        for name in sorted(filter(None, tracked)):
            path = ROOT / name
            relative = path.relative_to(ROOT / "integrations/B3")
            if relative.parts[0] in {"tests", "docs"} or relative.name == "phpunit.xml":
                continue
            if relative.parts[:2] == ("bootstrap", "cache") and relative.name != ".gitignore":
                continue
            if relative.parts[0] == "storage" and relative.name != ".gitignore":
                continue
            if relative.name.startswith(".env") and relative.name != ".env.example":
                continue
            archive.write(path, Path("B3") / relative)
        if "B3/bootstrap/cache/.gitignore" not in archive.namelist():
            archive.writestr("B3/bootstrap/cache/.gitignore", "*\n!.gitignore\n")
        if "B3/storage/logs/.gitignore" not in archive.namelist():
            archive.writestr("B3/storage/logs/.gitignore", "*\n!.gitignore\n")
        archive.write(MIGRATION, "B3/database/sql/" + MIGRATION.name)
        gui_migration = MIGRATION.with_name(MIGRATION.stem + "_DBBrowser.sql")
        archive.write(gui_migration, "B3/database/sql/" + gui_migration.name)
        guide = "Trong gói này, SQL cập nhật nằm ở `B3/database/sql/" + MIGRATION.name + "`.\n\n"
        guide += (ROOT / "docs/deployment/b2-b3-execution-edit.md").read_text(encoding="utf-8")
        archive.writestr("HUONG-DAN-CAP-NHAT.md", guide)
        archive.writestr("BUILD.txt", commit + "\nSource only; composer.lock dependencies required\n")
    manifest = {"commit": commit, "packages": {}}
    for path in (b2, b3):
        with zipfile.ZipFile(path) as archive:
            if archive.testzip() is not None:
                raise RuntimeError("Archive verification failed: " + path.name)
            names = archive.namelist()
            if any(Path(name).suffix.lower() in {".db", ".sqlite"} or Path(name).name == ".env" for name in names):
                raise RuntimeError("Unexpected runtime data: " + path.name)
            if path == b2 and not all(name in names for name in ("x86/SQLite.Interop.dll", "x64/SQLite.Interop.dll")):
                raise RuntimeError("Missing native SQLite dependencies")
        manifest["packages"][path.name] = {"sha256": hashlib.sha256(path.read_bytes()).hexdigest(), "bytes": path.stat().st_size}
    (OUTPUT / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(manifest, indent=2))


if __name__ == "__main__":
    main()
