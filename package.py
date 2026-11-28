"""Pack a Release build for local Thunderstore import and check the resulting bytes."""
import argparse
import hashlib
import json
from pathlib import Path
import struct
from tempfile import TemporaryDirectory
from xml.etree import ElementTree
from zipfile import ZIP_DEFLATED, ZipFile

root = Path(__file__).resolve().parent
project = ElementTree.parse(root / "WeeklyHermit.csproj")
version = project.findtext(".//Version")
framework = project.findtext(".//TargetFramework")
manifest = root / "thunderstore/manifest.json"
metadata = json.loads(manifest.read_text(encoding="utf-8"))
if metadata["name"] != "WeeklyHermit" or metadata["version_number"] != version:
    raise SystemExit("Project and manifest must identify the same release.")
files = {
    "CHANGELOG.md": root / "CHANGELOG.md",
    "LICENSE": root / "LICENSE",
    "manifest.json": manifest,
    "README.md": root / "thunderstore/README.md",
    "icon.png": root / "thunderstore/icon.png",
    "plugins/WeeklyHermit.dll": root / "bin" / "Release" / framework / "WeeklyHermit.dll",
}
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--output-dir", type=Path, default=root / "dist")
output = parser.parse_args().output_dir.resolve() / f"WeeklyHermit-{version}.zip"
output.parent.mkdir(exist_ok=True)
with TemporaryDirectory(dir=output.parent) as scratch:
    staged = Path(scratch) / output.name
    with ZipFile(staged, "w", ZIP_DEFLATED) as archive:
        for name, path in files.items():
            archive.write(path, name)
    with ZipFile(staged) as archive:
        if archive.testzip() or archive.namelist() != list(files):
            raise SystemExit("Unexpected ZIP structure or CRC failure.")
        for name, path in files.items():
            if archive.read(name) != path.read_bytes():
                raise SystemExit(f"Packaged bytes differ for {name}")
        icon = archive.read("icon.png")
        if icon[:8] != bytes.fromhex("89504E470D0A1A0A") or struct.unpack(">II", icon[16:24]) != (256, 256):
            raise SystemExit("icon.png must be a 256 x 256 PNG.")
    staged.replace(output)
print(f"{output}\nSHA-256 {hashlib.sha256(output.read_bytes()).hexdigest()}")
