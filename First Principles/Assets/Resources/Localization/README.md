# Localization

- **Files**: `en.txt`, `hi.txt`, `ur.txt`, `ar.txt`, `fr.txt`, `zh.txt`, `ko.txt`, `ja.txt`, `de.txt`, `es.txt` (UTF-8).
- **Keys**: Copy from `en.txt`. Optional **`story.0` … `story.42`** in a locale file override the **in-game story banner** (and aerospace preamble tail) for that language. Do **not** put `story.N` in `en.txt` if you want English to keep using the strings baked into `LevelManager`—add `story.N` only to `ko.txt`, `fr.txt`, etc. When a key is missing, the English `def.storyText` from `LevelManager` is used.
- **Math concepts reader (long article)**:
  - Place one UTF-8 file per language: **`MathArticle/en.txt`**, **`MathArticle/ko.txt`**, … under `Assets/Resources/Localization/`.
  - `LearningArticleLibrary` loads `Resources.Load("Localization/MathArticle/" + CurrentLanguage)` and falls back to `MathArticle/en.txt`. Translate by copying `en.txt` and replacing prose (keep `\(...\)` LaTeX for `TmpLatex` where needed).
- **First-launched language**: If `PlayerPrefs` has no `fp_language` yet, the game sets it from **`Application.systemLanguage`** (iOS, Android, Windows, macOS, Linux editors/players—whatever Unity reports). Supported mappings: en, fr, de, ja, ko, zh, es, ar, hi, plus Urdu when Unity exposes it as `SystemLanguage` or the enum name `Urdu`. Anything else defaults to **en**. After the first save, only the **language chip** in Menu / Level select changes `fp_language`.
- **Fonts (TMP)**: `TmpGlobalFallbackBootstrap` registers dynamic Noto fallbacks at startup (see `Assets/Resources/Fonts/`), including **Noto Sans Devanagari** (Hindi) and **Noto Nastaliq Urdu** (Urdu). If you remove those TTFs, add your own TMP fallbacks on the primary font or per-locale in code.

## Player preference

- Stored in `PlayerPrefs` under `fp_language` (`en`, `hi`, `ar`, `fr`, `zh`, `ko`, `ja`, `de`, `es`).
- Menu and Level Select include a **Language** chip (tap cycles languages).
