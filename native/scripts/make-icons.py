#!/usr/bin/env python3
"""Scale Phext.png into the icon set Tauri bundles."""

import io
import struct
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Phext.png"
ICONS = ROOT / "native" / "src-tauri" / "icons"
UI = ROOT / "native" / "ui"


def png_bytes(image: Image.Image, size: int) -> bytes:
    scaled = image.resize((size, size), Image.Resampling.LANCZOS)
    buf = io.BytesIO()
    scaled.save(buf, format="PNG")
    return buf.getvalue()


def write_icns(path: Path, images: dict[str, bytes]) -> None:
    body = b"".join(kind.encode("ascii") + struct.pack(">I", 8 + len(data)) + data for kind, data in images.items())
    path.write_bytes(b"icns" + struct.pack(">I", 8 + len(body)) + body)


def main() -> None:
    ICONS.mkdir(parents=True, exist_ok=True)
    image = Image.open(SOURCE).convert("RGBA")
    sizes = {
        32: ICONS / "32x32.png",
        128: ICONS / "128x128.png",
        256: ICONS / "128x128@2x.png",
    }
    encoded = {}
    for size, dest in sizes.items():
        data = png_bytes(image, size)
        dest.write_bytes(data)
        encoded[size] = data
    (UI / "icon.png").write_bytes(encoded[32])
    ico = image.resize((256, 256), Image.Resampling.LANCZOS)
    ico.save(ICONS / "icon.ico", format="ICO", sizes=[(16, 16), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
    write_icns(
        ICONS / "icon.icns",
        {
            "ic07": png_bytes(image, 128),
            "ic08": png_bytes(image, 256),
            "ic09": png_bytes(image, 512),
            "ic11": encoded[32],
            "ic12": png_bytes(image, 64),
            "ic13": png_bytes(image, 256),
            "ic14": png_bytes(image, 512),
        },
    )


if __name__ == "__main__":
    main()
