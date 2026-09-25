#!/usr/bin/env python3
"""
Downloads the Tier 1 images generated on Higgsfield (23 Sep 2026), including the secret boss, into HiggsfieldRaw/ and runs
higgs_import.py on each, so the characters, icons and backgrounds land in Assets/_FunkyThursday/Art.

    python Tools/fetch_tier1.py            # download + import everything
    python Tools/fetch_tier1.py --skip-download

Afterwards, in Unity: Funky Thursday -> Link Higgsfield Art (or Build All Scenes).
If a download fails (links can expire), open your Higgsfield gallery, download that image manually
under the name shown, and re-run with --skip-download.
"""
import argparse
import pathlib
import subprocess
import sys
import urllib.request

ROOT = pathlib.Path(__file__).resolve().parent.parent
RAW = ROOT / "HiggsfieldRaw"
CDN = "https://d8j0ntlcm91z4.cloudfront.net/user_3Jj5Zw3PhtvxYnDHNbaU6eXKdUy/"

IMAGES = [
    ("vesper_base.png", "hf_20260923_123119_9935a6a0-78e5-4963-b076-269adcfaf745.png", ["base", "--id", "vesper"]),
    ("bg_graveyard-gatekeeper.png", "hf_20260923_123152_104e5ffc-0d64-4f76-a8fa-39b883899752.png", ["background", "--slug", "graveyard-gatekeeper"]),
    ("gatekeeper_base.png", "hf_20260923_123334_5467a198-107b-4ab0-a44f-109ac09cd914.png", ["base", "--id", "gatekeeper"]),
    ("bg_crypt-keeper.png", "hf_20260923_123240_58bd88eb-47f6-45af-b3ec-756b667624c8.png", ["background", "--slug", "crypt-keeper"]),
    ("cryptkeeper_base.png", "hf_20260923_123334_4c3c156c-729a-49b5-b083-ed5a3517a7a9.png", ["base", "--id", "cryptkeeper"]),
    ("bg_cathedral-organist.png", "hf_20260923_123240_4f2dacc4-4c29-4682-b98f-536372fc7af7.png", ["background", "--slug", "cathedral-organist"]),
    ("organist_base.png", "hf_20260923_123240_03bb3622-942a-4471-90af-37b503e75421.png", ["base", "--id", "organist"]),
    ("bg_gothic-monarch.png", "hf_20260923_123334_cfc01ff6-48a5-4ad9-bcfa-5002901c2a28.png", ["background", "--slug", "gothic-monarch"]),
    ("monarch_base.png", "hf_20260923_123408_3df1e5d6-f917-436d-b8ec-36954d83397b.png", ["base", "--id", "monarch"]),
    # Secret boss (level 5)
    ("bg_abyssal-requiem.png", "hf_20260923_132259_ea289e47-a660-4626-9415-2ec0464943ce.png", ["background", "--slug", "abyssal-requiem"]),
    ("requiem_base.png", "hf_20260923_132429_c400f887-1e78-425d-9c9a-f32ee2bd941f.png", ["base", "--id", "requiem"]),
]


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--skip-download", action="store_true")
    args = parser.parse_args()

    RAW.mkdir(exist_ok=True)
    importer = ROOT / "Tools" / "higgs_import.py"
    failed = []

    for name, remote, command in IMAGES:
        local = RAW / name
        if not args.skip_download:
            try:
                print(f"Downloading {name} ...")
                request = urllib.request.Request(CDN + remote, headers={"User-Agent": "FunkyThursday/1.0"})
                with urllib.request.urlopen(request, timeout=60) as response:
                    local.write_bytes(response.read())
            except Exception as error:  # network problems are reported, not fatal
                print(f"  ! could not download {name}: {error}")
        if not local.exists():
            failed.append(name)
            continue
        result = subprocess.run([sys.executable, str(importer), command[0], str(local)] + command[1:], cwd=ROOT)
        if result.returncode != 0:
            failed.append(name)

    print()
    if failed:
        print("Not imported: " + ", ".join(failed))
        print("Download those from your Higgsfield gallery into HiggsfieldRaw/ and re-run with --skip-download.")
    else:
        print(f"All {len(IMAGES)} imported. In Unity: Funky Thursday -> Link Higgsfield Art.")


if __name__ == "__main__":
    main()
