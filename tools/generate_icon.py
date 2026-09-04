from pathlib import Path
from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
ASSET_DIR = ROOT / "src" / "TiaoKe.App" / "Assets"
CANVAS = 1024


def make_icon() -> Image.Image:
    image = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)

    # A quiet green window onto the horizon.
    draw.rounded_rectangle(
        (92, 92, 932, 932),
        radius=218,
        fill=(47, 118, 94, 255),
    )

    # The warm dot remains visible at tray-icon size.
    draw.ellipse((440, 286, 584, 430), fill=(233, 184, 74, 255))

    # One calm horizon: a shallow arch with rounded ends.
    points = []
    for x in range(230, 795, 4):
        offset = (x - 512) / 282
        y = 648 - 76 * (1 - offset * offset)
        points.append((x, int(y)))
    draw.line(points, fill=(247, 249, 247, 255), width=58, joint="curve")
    draw.ellipse((201, points[0][1] - 29, 259, points[0][1] + 29), fill=(247, 249, 247, 255))
    draw.ellipse((765, points[-1][1] - 29, 823, points[-1][1] + 29), fill=(247, 249, 247, 255))

    return image


def main() -> None:
    ASSET_DIR.mkdir(parents=True, exist_ok=True)
    icon = make_icon()
    icon.save(ASSET_DIR / "tiaoke-icon.png", optimize=True)
    icon.save(
        ASSET_DIR / "tiaoke.ico",
        format="ICO",
        sizes=[(16, 16), (20, 20), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)],
    )


if __name__ == "__main__":
    main()
