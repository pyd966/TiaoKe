from math import pi, sin
from pathlib import Path
import struct
import wave


ROOT = Path(__file__).resolve().parents[1]
ASSET_DIR = ROOT / "src" / "TiaoKe.App" / "Assets"
SAMPLE_RATE = 44_100


def tone(frequency: float, duration: float, amplitude: float = 0.16) -> list[int]:
    count = int(SAMPLE_RATE * duration)
    samples: list[int] = []
    for index in range(count):
        progress = index / max(1, count - 1)
        envelope = min(1.0, progress / 0.025, (1.0 - progress) / 0.08)
        value = amplitude * envelope * sin(2 * pi * frequency * progress * duration)
        samples.append(int(value * 32767))
    return samples


def silence(duration: float) -> list[int]:
    return [0] * int(SAMPLE_RATE * duration)


def save(name: str, parts: list[list[int]]) -> None:
    ASSET_DIR.mkdir(parents=True, exist_ok=True)
    samples = [sample for part in parts for sample in part]
    with wave.open(str(ASSET_DIR / name), "wb") as output:
        output.setnchannels(1)
        output.setsampwidth(2)
        output.setframerate(SAMPLE_RATE)
        output.writeframes(b"".join(struct.pack("<h", sample) for sample in samples))


def main() -> None:
    # Two restrained notes: an upward cue for a reminder, and a downward cue
    # when the break is complete. They are intentionally not Windows system sounds.
    save(
        "tiaoke-reminder.wav",
        [tone(660, 0.18), silence(0.045), tone(880, 0.26)],
    )
    save(
        "tiaoke-rest-complete.wav",
        [tone(880, 0.18), silence(0.045), tone(660, 0.30)],
    )


if __name__ == "__main__":
    main()
