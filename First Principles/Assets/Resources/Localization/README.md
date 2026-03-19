# Localization

- **Files**: `en.txt`, `ar.txt`, `fr.txt`, `zh.txt`, `ko.txt`, `ja.txt`, `de.txt`, `es.txt` (UTF-8).
- **Keys**: Copy from `en.txt`. Optional `story.0` … `story.42` override long level narratives; if omitted, the built-in English body from `LevelManager` is used.
- **Fonts (TMP)**: `TmpGlobalFallbackBootstrap` registers dynamic Noto fallbacks at startup (see `Assets/Resources/Fonts/`). If you remove those TTFs, add your own TMP fallbacks on the primary font or per-locale in code.

## Player preference

- Stored in `PlayerPrefs` under `fp_language` (`en`, `ar`, `fr`, `zh`, `ko`, `ja`, `de`, `es`).
- Menu and Level Select include a **Language** chip (tap cycles languages).
