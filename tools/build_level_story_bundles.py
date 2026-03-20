#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Emit Assets/Resources/Localization/LevelStories/{code}.txt — story.N keys (TMP rich text, \\n escapes)."""
from __future__ import annotations

import pathlib

ROOT = pathlib.Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "First Principles/Assets/Resources/Localization/LevelStories"

# Compact in-game banner copy per level index (0..43). Keep HTML-ish TMP tags sparingly.
# English originals are in LevelManager.BuildSampleLayers — these are localized summaries.

STORIES: dict[str, dict[int, str]] = {
    "fr": {
        0: "<color=#c4b5fd><b>Dérivée</b></color> = pente de la tangente : à quelle vitesse <b>f(x)</b> monte ou descend.\n\nLa lumière dorée suit votre chemin ; le bleu glacial est f'(x), qui façonne <i>où le sol tient</i>. Là où |f'| est assez grand, les plateformes portent ; sinon le vide s'ouvre.\n\n<size=88%><color=#94a3b8><b>Mindset « premiers principes » :</b> comme dans les discours Tesla/SpaceX — enlever les analogies jusqu'aux faits, puis reconstruire. Ici <b>f</b> modélise le résultat ; <b>f'</b> montre où un petit changement d'entrée bouge vite la sortie.</color></size>",
        1: "Un voyageur marche quand la pente est douce. Quand la dérivée devient négative, le sol se rétrécit en un vide.",
        2: "La courbe monte et descend comme une respiration. La dérivée indique où atterrir : positif = pas sûr, négatif = saut.",
        3: "Le cosinus cache son sens dans le signe de sa dérivée. Surveillez le « pop » : il annonce le danger.",
        4: "La valeur absolue plie la courbe en un seul chemin. Au sommet, la dérivée change de signe — et le sol bascule.",
        5: "<color=#86efac>Polynômes de Taylor</color> collent à une fonction régulière près d'un point. <b>Maclaurin</b> = Taylor en <b>0</b>.\n\nIci la piste est une somme partielle de <b>e^u</b> : des polynômes qui tendent vers la série exponentielle.",
        6: "<color=#7dd3fc>Puissances impaires de u</color> alternent les signes — l'ADN Maclaurin de <b>sin(u)</b>.\n\nLe graphe est une pile de Taylor tronquée vers la sinusoïde ; chaque terme affine la courbe près de 0.",
        7: "Une <color=#f5d0fe>série géométrique</color> empile des puissances de <b>u</b>. Dans le rayon, la queue rétrécit et les sommes partielles se stabilisent.\n\nSentez comment la dérivée de cette somme finie remodelle le terrain le long de <b>x</b>.",
        8: "Pensez <b>z = x² − y₀²</b> avec <b>y₀</b> fixé — une <color=#38bdf8>tranche</color> de <i>selle</i>.\n\nLa dérivée en x lit encore le paysage : idées multivariables, mouvement 1D.",
        9: "Maintenant <b>z = x² + y₀²</b> : <color=#fde047>paraboloïde elliptique</color>. Fixer <b>y₀</b> donne un <i>bol</i> dans votre plan.\n\nLe gradient pointe vers le haut ; ici la tranche montre à quel point la côte monte.",
        10: "L'<color=#fde047>intégrale définie</color> d'un taux positif est la <b>quantité accumulée</b> — ici l'<i>aire sous la courbe</i>.\n\nLes colonnes bleues sont une <b>somme de Riemann</b> : découper en Δx, ajouter f(x*)·Δx. Plus de rectangles ⇒ plus proche de ∫f.\n\n<size=92%><color=#a8b2d1>Votre course suit toujours le graphe lisse ; le remplissage <i>approxime</i> l'intégrale.</color></size>",
        11: "<b>Somme de Riemann à gauche :</b> hauteur <b>f(xᵢ)</b> — <color=#f87171>extrémité gauche</color>.\n\nSi f croît, les gauches sont les plus basses ⇒ la somme <b>sous-estime</b> l'aire.\n\n<size=92%><color=#a8b2d1>Plateformes plates à ces hauteurs — un escalier prudent sous la parabole.</color></size>",
        12: "<b>Somme à droite :</b> hauteur <b>f(xᵢ₊₁)</b> — <color=#38bdf8>droite</color>.\n\nSi f croît, les droites sont plus hautes ⇒ <b>sur-estimation</b> — miroir de la somme gauche.\n\n<size=92%><color=#a8b2d1>Chaque marche saute au bord droit ; comparez mentalement avec la scène gauche.</color></size>",
        13: "<b>Point milieu :</b> échantillon au <color=#86efac>centre</color> (xᵢ+xᵢ₊₁)/2. Le rectangle est symétrique.\n\nSouvent plus serré que gauche/droit sur courbes douces.\n\n<size=92%><color=#a8b2d1>Les marches sont au milieu des tranches ; la marche épouse mieux le bol.</color></size>",
        14: "<b>Oscillation amortie</b> partout en génie : ressorts, circuits.\n\nUn ressort avec friction oscille mais l'enveloppe <color=#38bdf9>rétrécit</color> — décroissance exponentielle × sinus.\n\n<size=92%><color=#a8b2d1>Votre chemin est le graphe ; la dérivée choisit encore les colonnes sûres près des pics.</color></size>",
        15: "Une chaîne suspendue forme une <b>caténaire</b>, souvent modélisée par <color=#fde047>cosh</color>.\n\nPas une parabole de projectile : équilibre de tension sous poids propre — intro aux <i>fonctions hyperboliques</i>.\n\n<size=92%><color=#a8b2d1>La courbe monte doucement puis raidit — arches et câbles.</color></size>",
        16: "<b>|sin(x)|</b> est une sinusoïde <color=#f0abfc>redressée en double alternance</color> — modèle simple après redresseur.\n\nLes zéros ont des coins ; la dérivée saute (en pratique on moyenne / RMS).\n\n<size=92%><color=#a8b2d1>Pont entre trig pure et formes d'onde redressées.</color></size>",
        17: "<b>BC — trig réciproque.</b> <color=#38bdf8>Arctan</color> a pente bornée : d/dx arctan x = 1/(1+x²).\n\nIntégrales, géométrie, angles depuis des rapports qui croissent lentement.\n\n<size=92%><color=#a8b2d1>Asymptotes horizontales = limites en ±∞.</color></size>",
        18: "<b>Équation logistique :</b> dP/dt = kP(1 − P/L). Croissance presque exponentielle au début, puis <color=#86efac>courbure</color> vers la capacité <b>L</b>.\n\nPopulations, rumeurs, réactions saturées — point d'inflexion = croissance max.\n\n<size=92%><color=#a8b2d1>Lisez : exponentielle au début, compétition au milieu, plateau final.</color></size>",
        19: "<b>Coordonnées polaires</b> : <color=#fde047>(r, θ)</color>. Une <b>cardioïde</b> r ~ 1 + cos θ.\n\nIci l'axe horizontal joue θ et le vertical r(θ).\n\n<size=92%><color=#a8b2d1>Aire polaire ½∫ r² dθ ; pente tangentielle via dr/dθ.</color></size>",
        20: "<b>Rose polaire</b> : r ~ cos(nθ). Symétrie et période dictent le nombre de pétales.\n\n<size=92%><color=#a8b2d1>Les « pops » de dérivée marquent où r change vite — murs sur les bords des pétales.</color></size>",
        21: "<b>sinh et cosh</b> : sinh = (e^x−e^{−x})/2, cosh = (e^x+e^{−x})/2 ; identité hyperbole.\n\nEDO linéaires, câbles, variantes des identités trig.\n\n<size=92%><color=#a8b2d1>sinh est le partenaire impair qui part de 0.</color></size>",
        22: "<b>Physique C — décroissance exponentielle :</b> Q = Q₀ e^{−t/τ}. Condensateur / RL, constante de temps τ.\n\n<size=92%><color=#a8b2d1>Le graphe retombe ; la dérivée garde le signe de « encore fuite vers zéro ».</color></size>",
        23: "<b>Rotation — moment cinétique.</b> <color=#7dd3fc>L = I ω</color>. Couple τ = dL/dt.\n\nPetites oscillations rotationnelles ressemblent au SHM.\n\n<size=92%><color=#a8b2d1>L'énergie oscille entre cinétique ½Iω² et termes de rappel.</color></size>",
        24: "<b>Hauteur de projectile :</b> y(t) = y₀ + v₀ t − ½ g t² — <color=#fde047>parabole</color> dans le temps.\n\nSommet où v = dy/dt = 0 — optimisation gratuite.\n\n<size=92%><color=#a8b2d1>Physique C : chaîne position-vitesse-accélération.</color></size>",
        25: "<b>Maclaurin de cos(x)</b> : puissances paires alternées — partenaire des impaires de sin.\n\n<size=92%><color=#a8b2d1>Plus de termes ⇒ meilleur accostage près de 0 ; les polynômes dérivés suivent −sin.</color></size>",
        26: "<b>ln x</b> — vedette de ∫ dx/x, croissances, demi-vies. Ici domaine décalé pour rester positif.\n\n<size=92%><color=#a8b2d1>Pente toujours positive mais qui diminue — « rendements décroissants ».</color></size>",
        27: "<b>√x</b> — attention au domaine x≥0. d/dx √x = 1/(2√x) explose vers 0⁺ — tangente verticale classique au BC.\n\n<size=92%><color=#a8b2d1>Le gameplay lisse toujours ; l'histoire rappelle la singularité au bord.</color></size>",
        28: "<b>Tangente</b> — asymptotes où cos = 0. Dérivée sec²x ≥ 1 sur une branche lisse.\n\n<size=92%><color=#a8b2d1>Paramétrique / polaire revient souvent à ces identités.</color></size>",
        29: "<b>EDO exponentielle :</b> y′ = k y ⇒ y = C e^{kx}. Séparable, champs de pentes, demi-vies.\n\n<size=92%><color=#a8b2d1>Contraste avec l'étape aire ∫ e^x : ici « taux proportionnel à la quantité ».</color></size>",
        30: "<b>Phase & SHM.</b> sin(ωt+ϕ) décale le cosinus — <color=#e9d5ff>échange d'énergie</color> ressort idéal.\n\n<size=92%><color=#a8b2d1>La dérivée (cos à constantes près) dit qui mène et qui suit.</color></size>",
        31: "<b>Cubique</b> — points critiques, <color=#fca5a5>inflexion</color> où y″ change de signe.\n\n<size=92%><color=#a8b2d1>Vous sentez bosse + creux typiques entre flexions.</color></size>",
        32: "<b>Exponentielle générale</b> b^x : dérivée (ln b) b^x.\n\n<size=92%><color=#a8b2d1>Le ln b relie bases 10/2 à la base e où la constante vaut 1.</color></size>",
        33: "<b>Cercle</b> <color=#7dd3fc>(x−h)²+(y−k)²=R²</b>. On marche le <b>demi-cercle supérieur</b> y = k+√(⋯).\n\nDifférentiation implicite : dy/dx = −(x−h)/(y−k).\n\n<size=92%><color=#a8b2d1>Paramétrique x=h+R cos t, y=k+R sin t est un classique AP.</color></size>",
        34: "<b>Portance vs incidence</b> — <color=#38bdf8>C_L</color> croît ~linéairement avant le <b>décrochage</b>, puis chute.\n\n<size=92%><color=#a8b2d1>Jouet pédagogique : pente & saturation, pas un essai en flotte réelle.</color></size>",
        35: "<b>Polaire de traînée</b> <color=#c4b5fd>C_D = C_D0 + K C_L²</color> — profil + traînée induite.\n\n<size=92%><color=#a8b2d1>Minimum de traînée / meilleure finesse près d'un C_L optimal.</color></size>",
        36: "<b>Atmosphère isotherme simplifiée :</b> ρ, p ~ <color=#86efac>e^{−h/H}</color>. Hauteur d'échelle H.\n\n<size=92%><color=#a8b2d1>Tout ce qui dépend de ρ(h) — poussée dynamique, Mach, Reynolds.</color></size>",
        37: "<b>Longitudinal avion :</b> modes <color=#7dd3fc>courte période</color> et <b>phugoïde</b> ; ici cartoon = oscillation amortie comme masse–ressort–amortisseur.\n\n<size=92%><color=#a8b2d1>AP : autoland / lois de retour modulent ces modes.</color></size>",
        38: "<b>Modèle newtonien hypersonique</b> — coefficient ~ <color=#fca5a5>sin²α</color> côté face au vent.\n\n<size=92%><color=#a8b2d1>Vocabulaire calcul pour boucliers thermiques & trajectoires, pas du CFD complet.</color></size>",
        39: "<b>Nombre de Strouhal</b> — f ≈ St·U/D pour l'allée de tourbillons.\n\n<size=92%><color=#a8b2d1>Vibrations, bruit, fatigue — l'onde sinusoïdale trace une sonde idéale.</color></size>",
        40: "<b>Rentrée atmosphérique (qualitatif)</b> — flux thermique ~ ρV³ en ordres de grandeur ; l'enveloppe se <color=#fde047>détend</color> quand ρ↓ et V↓.\n\n<size=92%><color=#a8b2d1>Analogie pédagogique, pas une fiche SpaceX.</color></size>",
        41: "<b>Bulle internet</b> — trajectoire d'indice stylisée (pas le vrai S&amp;P).\n\n<size=92%><color=#a8b2d1>Montée, trou d'air, reprise lente — allégorie pédagogique.</color></size>",
        42: "<b>Crise 2008</b> — stress crédit & immobilier, courbe qualitative seulement.\n\n<size=92%><color=#a8b2d1>Pas de données réelles — vocabulaire de pente en classe.</color></size>",
        43: "<b>Mandelbrot final</b> — plan <color=#a8b2d1>c</color> coloré par temps d'échappement ; ligne = Re(c) fixe. Courbe verte = coupe en Im(c).\n\n<size=92%><color=#a8b2d1>Cardioïde & bulbes = frontière des ensembles de Julia ; zoom profond = autre moteur. Itérations avec |Im(c)|.</color></size>",
    },
}

# Mirror EN wording pattern with machine-translation style summaries for other locales (short, gameplay-true).
# de, es: clone fr with light edits would be wrong — provide distinct German/Spanish.

def _esc(s: str) -> str:
    return s.replace("\\", "\\\\").replace("\n", "\\n").replace("\r", "")


def emit(lang: str, m: dict[int, str]) -> None:
    lines = [f"# Level stories ({lang}) — keys story.0 … story.43", ""]
    for i in range(44):
        if i not in m:
            continue
        lines.append(f"story.{i}={_esc(m[i])}")
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    (OUT_DIR / f"{lang}.txt").write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> None:
    # German — distinct strings (compact)
    STORIES["de"] = {
        0: "<color=#c4b5fd><b>Ableitung</b></color> = Steigung der Tangente — wie schnell <b>f(x)</b> steigt oder fällt.\n\nGoldenes Licht: Ihr Pfad ; Eisblau: f'(x) formt <i>wo der Boden trägt</i>. Wo |f'| groß genug ist, gibt es Plattformen — sonst öffnet sich die Leere.\n\n<size=88%><color=#94a3b8><b>First-Principles-Denken:</b> Analogien streifen bis zu Fakten, dann neu aufbauen. <b>f</b> = Outcome-Modell ; <b>f'</b> = Sensitivität.</color></size>",
        1: "Ein Wanderer geht, wo die Steigung freundlich ist. Wird die Ableitung negativ, schmilzt der Boden zur Lücke.",
        2: "Die Kurve steigt und fällt wie Atem. Die Ableitung zeigt, wo man landet: positiv = sicherer Schritt, negativ = Sprung.",
        3: "Der Kosinus verbirgt sich im Vorzeichen seiner Ableitung. Der Pop markiert die Gefahr.",
        4: "Der Betrag faltet die Kurve zu einem Pfad. Am Knick wechselt die Ableitung — und der Boden kippt.",
        5: "<color=#86efac>Taylorpolynome</color> schmiegen sich glatten Funktionen an. <b>Maclaurin</b> = Taylor bei <b>0</b>.\n\nHier: Partialsumme von <b>e^u</b> — Stapel Richtung Exponentialreihe.",
        6: "<color=#7dd3fc>Ungerade Potenzen</color> wechseln das Vorzeichen — Maclaurin-DNA von <b>sin(u)</b>.\n\nGraph = abgeschnittener Taylorturm zur Sinuswelle.",
        7: "<color=#f5d0fe>Geometrische Reihe</color> stapelt Potenzen von <b>u</b>. Im Konvergenzradius schrumpft der Rest.\n\nSpüren Sie, wie die Ableitung der Partialsumme das Terrain formt.",
        8: "Denken Sie <b>z = x² − y₀²</b> — <color=#38bdf8>Schnitt</color> durch eine Sattelfläche.\n\nx-Ableitung liest Multivar- Landschaft in 1D.",
        9: "Jetzt <b>z = x² + y₀²</b> — <color=#fde047>elliptisches Paraboloid</color>. Festes <b>y₀</b> = Schale in Ihrer Ebene.\n\nGradient zeigt bergauf ; der Schnitt zeigt die Steigung beim Lauf.",
        10: "Das <color=#fde047>bestimmte Integral</color> nichtnegativer Rate = <b>akkumulierte Menge</b> — Fläche unter der Kurve.\n\nBlaue Säulen = <b>Riemannsumme</b>: Δx, f(x*)·Δx, Grenz ∫f.\n\n<size=92%><color=#a8b2d1>Pfad folgt glattem Graph ; Schattierung approximiert.</color></size>",
        11: "<b>Riemann links:</b> Höhe <b>f(xᵢ)</b> — <color=#f87171>linker Rand</color>. Wenn f wächst: Summe <b>unterschätzt</b>.\n\n<size=92%><color=#a8b2d1>Flache Stufen — konservative Treppe.</color></size>",
        12: "<b>Riemann rechts:</b> <b>f(xᵢ₊₁)</b> — <color=#38bdf8>rechter Rand</color>. Wachsendes f ⇒ <b>Überschätzung</b>.\n\n<size=92%><color=#a8b2d1>Spiegelbild zur Linkssumme.</color></size>",
        13: "<b>Mittelpunkt:</b> Stützpunkt in der <color=#86efac>Mitte</color>. Symmetrisch ; oft genauer.\n\n<size=92%><color=#a8b2d1>Stufen mittig in jeder Scheibe.</color></size>",
        14: "<b>Gedämpfte Schwingung</b> — Feder, Kreise, Struktur. Envelope <color=#38bdf9>schwindet</color> exponential × sin.\n\n<size=92%><color=#a8b2d1>Ableitung wählt weiter sichere Säulen an den Buckeln.</color></size>",
        15: "Hängende Kette ⇒ <b>Ketenlinie</b>, oft <color=#fde047>cosh</color>. Nicht Wurfparabel — Hyperbolik in Statik.\n\n<size=92%><color=#a8b2d1>Bogen und Seile.</color></size>",
        16: "<b>|sin(x)|</b> — <color=#f0abfc>vollweggerichtete</color> Welle. Ecken bei Nullen ; Sprung der Ableitung.\n\n<size=92%><color=#a8b2d1>Brücke von Trig zu Leistungselektronik-Formen.</color></size>",
        17: "<b>BC — Arkus.</b> <color=#38bdf8>Arctan</color>, Ableitung 1/(1+x²). Asymptoten = Grenzwerte.\n\n<size=92%><color=#a8b2d1>Horizontale Asymptoten visualisieren ±∞.</color></size>",
        18: "<b>Logistisch:</b> dP/dt = kP(1−P/L). Fast exp, dann <color=#86efac>Sättigung</color> bei <b>L</b>.\n\n<size=92%><color=#a8b2d1>Wendepunkt = max. Wachstum.</color></size>",
        19: "<b>Polarkoord.</b> <color=#fde047>(r,θ)</color>. <b>Kardioide</b> r ~ 1+cos θ.\n\n<size=92%><color=#a8b2d1>Fläche ½∫r² dθ.</color></size>",
        20: "<b>Rosenkurve</b> r ~ cos(nθ). Symmetrie ⇒ Petalen.\n\n<size=92%><color=#a8b2d1>Pop markiert steiles r.</color></size>",
        21: "<b>sinh, cosh</b> — hyperbolische Identitäten, ODEs, Seile.\n\n<size=92%><color=#a8b2d1>sinh ungerade Partner von cosh.</color></size>",
        22: "<b>Physik C</b> — Q = Q₀ e^{−t/τ}. RC / RL Zeitkonstante.\n\n<size=92%><color=#a8b2d1>Ableitung zeigt Entleerung zu null.</color></size>",
        23: "<b>Rotation</b> <color=#7dd3fc>L = Iω</color>, τ = dL/dt.\n\n<size=92%><color=#a8b2d1>Kleine Schwingungen wie SHM — Energietausch.</color></size>",
        24: "<b>Projektil y(t)</b> — nach unten offene <color=#fde047>Parabel</color>. Maximum bei v=0.\n\n<size=92%><color=#a8b2d1>y,v,a Kette.</color></size>",
        25: "<b>Maclaurin cos</b> — gerade Potenzen wechseln Vorzeichen.\n\n<size=92%><color=#a8b2d1>Mehr Terme ⇒ besser bei 0.</color></size>",
        26: "<b>ln x</b> und ∫dx/x. Hier verschoben für x>0.\n\n<size=92%><color=#a8b2d1>Positive, schwindende Steigung.</color></size>",
        27: "<b>√x</b> singuläre Ableitung bei 0⁺.\n\n<size=92%><color=#a8b2d1>Gameplay glättet ; Analysis warnt am Rand.</color></size>",
        28: "<b>tan x</b> zwischen Asymptoten, sec²x.\n\n<size=92%><color=#a8b2d1>Trig-Algebra des BC.</color></size>",
        29: "<b>y′=ky</b> ⇒ Ce^{kx}.\n\n<size=92%><color=#a8b2d1>Rate proportional zum Bestand.</color></size>",
        30: "<b>Phase / SHM</b> sin(ωt+ϕ).\n\n<size=92%><color=#a8b2d1>Ableitung = cos bis auf Konstante.</color></size>",
        31: "<b>Kubik</b> — Extrema, <color=#fca5a5>Wendepunkte</color>.\n\n<size=92%><color=#a8b2d1>Typisches Hoch-Tal-Muster.</color></size>",
        32: "<b>b^x</b> ableitet mit ln b.\n\n<size=92%><color=#a8b2d1>Brücke aller Basen zur e-Basis.</color></size>",
        33: "<b>Kreis</b> — oberer Halbkreis als Graph. Implizit ableiten.\n\n<size=92%><color=#a8b2d1>Parametrisierung klassisch AP.</color></size>",
        34: "<b>Auftrieb C_L(α)</b> linear, dann <b>Strömungsabriss</b>.\n\n<size=92%><color=#a8b2d1>Didaktisches Profil.</color></size>",
        35: "<b>Widerstandspolar</b> C_D = C_D0 + K C_L².\n\n<size=92%><color=#a8b2d1>Minimum und Gleitverhältnis.</color></size>",
        36: "<b>Isothermes Modell</b> ρ ~ e^{−h/H}.\n\n<size=92%><color=#a8b2d1> Dynamikdruck skaliert mit ρ.</color></size>",
        37: "<b>Phugoid / kurze Periode</b> — gedämpfte Oszillation cartoon.\n\n<size=92%><color=#a8b2d1>Regelkreis in der Realität komplexer.</color></size>",
        38: "<b>Newton DRUCK ~ sin²α</b> Leeseite.\n\n<size=92%><color=#a8b2d1>Lehrmodell Hyperschall, kein CFD.</color></size>",
        39: "<b>Strouhal</b> f ~ St·U/D.\n\n<size=92%><color=#a8b2d1>Ergung & Schall.</color></size>",
        40: "<b>Wiedereintritt</b> — qualitative Entspannung der Heizkurve.\n\n<size=92%><color=#a8b2d1>Kein quantitatives Memo.</color></size>",
        41: "<b>Dotcom-Blase</b> — stilisierter Index (keine echten S&amp;P-Daten).\n\n<size=92%><color=#a8b2d1>Aufstieg, Luftloch, langsames Erholen — Lehr-Allegorie.</color></size>",
        42: "<b>Finanzkrise 2008</b> — Hypotheken-Stress, qualitativ (keine Tickdaten).\n\n<size=92%><color=#a8b2d1>Nur Kurvenform fürs Kalkül-Gefühl.</color></size>",
        43: "<b>Mandelbrot</b> — c-Ebene, Fluchtzeit, Schnitt bei festem Re(c).\n\n<size=92%><color=#a8b2d1>Julia-Grenze ; tiefer Zoom braucht andere Engine.</color></size>",
    }

    # Spanish
    STORIES["es"] = {
        0: "<color=#c4b5fd><b>Derivada</b></color> = pendiente tangente: qué tan rápido sube/baja <b>f(x)</b>.\n\nLuz dorada = camino ; azul hielo = f'(x) moldea <i>dónde hay suelo</i>. Donde |f'| basta, hay plataformas ; si no, el vacío.\n\n<size=88%><color=#94a3b8><b>Principios primero:</b> quitar analogías hasta hechos, luego razonar. <b>f</b> modelo del resultado ; <b>f'</b> sensibilidad.</color></size>",
        1: "Un viajero camina donde la pendiente es amable. Si la derivada es negativa, el suelo se vuelve grieta.",
        2: "La curva sube y baja como respiración. La derivada indica dónde aterrizar.",
        3: "El coseno esconde su lectura en el signo de su derivada. El pop marca el peligro.",
        4: "El valor absoluto dobla la curva. En el vértice la derivada cambia y el suelo también.",
        5: "<color=#86efac>Polinomios de Taylor</color> aproximan funciones suaves. <b>Maclaurin</b> centrado en <b>0</b>.\n\nAquí suma parcial de <b>e^u</b>.",
        6: "<color=#7dd3fc>Potencias impares</color> alternan — ADN Maclaurin de <b>sin(u)</b>.\n\nTorre truncada hacia la onda seno.",
        7: "<color=#f5d0fe>Serie geométrica</color> apila potencias de <b>u</b>. En el radio, la cola mengua.\n\nLa derivada de la suma finita remodela el terreno.",
        8: "Piensa <b>z = x² − y₀²</b> — <color=#38bdf8>rebanada</color> de silla.\n\nLa derivada en x cuenta la historia multivariada en 1D.",
        9: "Ahora <b>z = x² + y₀²</b> — <color=#fde047>paraboloide</color>. y₀ fijo = cuenco.\n\nMientras corres, la rebanada muestra la pendiente.",
        10: "La <color=#fde047>integral definida</color> acumula tasa ⇒ <b>área bajo curva</b>.\n\nColumnas = <b>Riemann</b> f(x*)·Δx → ∫f.\n\n<size=92%><color=#a8b2d1>Sombreado aproxima ; caminas la gráfica lisa.</color></size>",
        11: "<b>Riemann izquierda:</b> altura <b>f(xᵢ)</b> — <color=#f87171>extremo izquierdo</color>. Si f crece ⇒ <b>subestima</b>.\n\n<size=92%><color=#a8b2d1>Peldaños bajos conservadores.</color></size>",
        12: "<b>Riemann derecha:</b> <b>f(xᵢ₊₁)</b> — <color=#38bdf8>derecha</color> ⇒ <b>sobrestima</b> al crecer f.\n\n<size=92%><color=#a8b2d1>Espejo del caso izquierdo.</color></size>",
        13: "<b>Punto medio:</b> centro del subintervalo — <color=#86efac>simétrico</color>, suele ser más ajustado.\n\n<size=92%><color=#a8b2d1>Pasos centrados en cada franja.</color></size>",
        14: "<b>Oscilación amortiguada</b> en resortes/circuitos. Envuelve <color=#38bdf9>decae</color> exponencial × seno.\n\n<size=92%><color=#a8b2d1>La derivada sigue gobernando columnas seguras.</color></size>",
        15: "Cadena colgante ⇒ <b>catenaria</b>, modelo <color=#fde047>cosh</color>. No parábola balística.\n\n<size=92%><color=#a8b2d1>Funciones hiperbólicas en estática.</color></size>",
        16: "<b>|sin(x)|</b> seno <color=#f0abfc>rectificado</color>. Esquinas en ceros.\n\n<size=92%><color=#a8b2d1>De trig a ondas de potencia.</color></size>",
        17: "<b>BC — inversa.</b> <color=#38bdf8>Arctan</color>, derivada 1/(1+x²).\n\n<size=92%><color=#a8b2d1>Asíntotas horizontales = límites.</color></size>",
        18: "<b>Logística:</b> dP/dt = kP(1−P/L). Casi exp, luego <color=#86efac>pliegue</color> a <b>L</b>.\n\n<size=92%><color=#a8b2d1>Punto de inflexión = máxima tasa.</color></size>",
        19: "<b>Polares</b> <color=#fde047>(r,θ)</color>. <b>Cardioide</b> r ~ 1+cos θ.\n\n<size=92%><color=#a8b2d1>Área ½∫r² dθ.</color></size>",
        20: "<b>Rosa</b> r ~ cos(nθ).\n\n<size=92%><color=#a8b2d1>Pops donde r cambia rápido.</color></size>",
        21: "<b>sinh, cosh</b> — identidad hiperbólica, EDO.\n\n<size=92%><color=#a8b2d1>sinh impar desde 0.</color></size>",
        22: "<b>Física C</b> decaimiento Q = Q₀ e^{−t/τ}.\n\n<size=92%><color=#a8b2d1>La derivada fluye hacia cero.</color></size>",
        23: "<b>Rotación</b> <color=#7dd3fc>L = Iω</color>, τ = dL/dt.\n\n<size=92%><color=#a8b2d1>Energía entre términos como un resorte.</color></size>",
        24: "<b>Proyectil y(t)</b> parábola <color=#fde047>en t</color>. Pico en v=0.\n\n<size=92%><color=#a8b2d1>Cadena y,v,a.</color></size>",
        25: "<b>Maclaurin cos</b> potencias pares.\n\n<size=92%><color=#a8b2d1>Más términos ⇒ mejor cerca de 0.</color></size>",
        26: "<b>ln x</b> y ∫dx/x.\n\n<size=92%><color=#a8b2d1>Pendiente positiva decreciente.</color></size>",
        27: "<b>√x</b> singularidad de pendiente en 0⁺.\n\n<size=92%><color=#a8b2d1>El juego suaviza ; la analisis advierte.</color></size>",
        28: "<b>tan x</b> entre asíntotas.\n\n<size=92%><color=#a8b2d1>Trig del BC.</color></size>",
        29: "<b>y′=ky</b> ⇒ Ce^{kx}.\n\n<size=92%><color=#a8b2d1>Tasa proporcional.</color></size>",
        30: "<b>Fase / SHM</b>.\n\n<size=92%><color=#a8b2d1>Derivada adelantada 90° (hasta constantes).</color></size>",
        31: "<b>Cúbica</b> — críticos, <color=#fca5a5>inflexión</color>.\n\n<size=92%><color=#a8b2d1>Forma típica montaña-valle.</color></size>",
        32: "<b>b^x</b> con factor ln b.\n\n<size=92%><color=#a8b2d1>Puente a base e.</color></size>",
        33: "<b>Círculo</b> — semicírculo superior y = k+√(⋯).\n\n<size=92%><color=#a8b2d1>Implícita y paramétrica AP.</color></size>",
        34: "<b>Lift C_L(α)</b> lineal + <b>stall</b>.\n\n<size=92%><color=#a8b2d1>Juguete pedagógico.</color></size>",
        35: "<b>Polar de arrastre</b> C_D = C_D0 + K C_L².\n\n<size=92%><color=#a8b2d1>Mejor L/D cerca de un C_L óptimo.</color></size>",
        36: "<b>Atm isoterma</b> ρ ~ e^{−h/H}.\n\n<size=92%><color=#a8b2d1>q dinámico ∝ ρ.</color></size>",
        37: "<b>Fugoide / corto período</b> cartoon amortiguado.\n\n<size=92%><color=#a8b2d1>Control real más rico.</color></size>",
        38: "<b>Newton ~ sin²α</b> windward.\n\n<size=92%><color=#a8b2d1>Modelo docente, no CFD.</color></size>",
        39: "<b>Strouhal</b> vórtices.\n\n<size=92%><color=#a8b2d1>Frecuencia ⋍ St·U/D.</color></size>",
        40: "<b>Reentrada</b> relajación cualitativa.\n\n<size=92%><color=#a8b2d1>No memo cuantitativo.</color></size>",
        41: "<b>Burbuja dot-com</b> — trayectoria de índice estilizada (no S&amp;P real).\n\n<size=92%><color=#a8b2d1>Auge, caída, reconstrucción lenta — alegoría docente.</color></size>",
        42: "<b>Crisis 2008</b> — estrés hipotecario y repricing global (curva cualitativa).\n\n<size=92%><color=#a8b2d1>No es data descargada; solo forma para clase de cálculo.</color></size>",
        43: "<b>Mandelbrot</b> plano c, tiempo escape, corte Re fijo.\n\n<size=92%><color=#a8b2d1>Julia desconexión ; zoom profundo otro motor.</color></size>",
    }

    for lang in ("fr", "de", "es"):
        emit(lang, STORIES[lang])

    import sys
    _tools_dir = pathlib.Path(__file__).resolve().parent
    if str(_tools_dir) not in sys.path:
        sys.path.insert(0, str(_tools_dir))
    from level_stories_cjk_ar_hi_ur import emit_remaining_story_bundles

    emit_remaining_story_bundles(emit)
    print("Wrote LevelStories for fr, de, es, ja, ko, zh, ar, hi, ur.")


if __name__ == "__main__":
    main()
