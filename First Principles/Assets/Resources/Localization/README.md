# Localization

- **Files**: `en.txt`, `ar.txt`, `fr.txt`, `zh.txt`, `ko.txt`, `ja.txt`, `de.txt`, `es.txt` (UTF-8).
- **Keys**: Copy from `en.txt`. Optional `story.0` … `story.42` override long level narratives; if omitted, the built-in English body from `LevelManager` is used.
- **Fonts (TMP)**: The default / Outfit asset may not include Arabic or CJK glyphs. To avoid tofu:
  1. Import **Noto Sans** (Arabic, SC, JP, KR) or **Source Han** as TMP font assets.
  2. Add them to **Fallback Font Assets** on your primary TMP font, or swap the default font per language in code.

## Player preference

- Stored in `PlayerPrefs` under `fp_language` (`en`, `ar`, `fr`, `zh`, `ko`, `ja`, `de`, `es`).
- Menu and Level Select include a **Language** chip (tap cycles languages).
