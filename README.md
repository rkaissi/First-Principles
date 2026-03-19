# First Principles: An Interactive Module
A **Graphing** Calculator. Developed by [Rayan Kaissi](https://github.com/GameGenesis) and [John Seong](https://github.com/wonmor). Powered by **Unity** Game Engine.

---

## Alpha Build 0.3

**Development** is still in **progress**, but will be completed shortly!

[Live Demo](https://www.youtube.com/watch?v=yo540yl4Xhs) • [Official Documentation](https://github.com/GameGenesis/First-Principles/wiki/First-Principles-Official-Documentation) • [Watch the Promotional Video](https://www.youtube.com/watch?v=k0soEFAK-CQ)

---

A puzzle game that visualizes the nature of a curve and its corresponding derivative function, and how various rules (e.g. power rule, product rule, quotient rule, chain rule, squeeze theorem, and L'Hôpital's rule) of differentiation can work in a given equation. It further clarifies the relationship between the rules; for example, proving the mechanism behind power rule using the first principles ```lim h->0 (f(x+h) - f(x)) / h```, or explaining the product rule using the power rule. Same goes for the editions that will be released later, which include topics such as definite/indefinite integration and infinite series.

---

### The game will be released in a total of *four* editions:
1. **Pre-Calculus**: The Nature of Functions
2. **The Fundamentals of Calculus**: Limits and Differentiation
3. **Integral Calculus**: The Most Powerful Humanmade Tool
4. **Infinite Series**: Let's Explore Above and Beyond

<img width="1728" alt="Screen Shot 2022-03-15 at 2 57 39 PM" src="https://user-images.githubusercontent.com/35755386/158451505-71e056ee-cca4-42ee-a621-38c092c806f2.png">

<img width="1728" alt="Screen Shot 2022-03-15 at 2 57 31 PM" src="https://user-images.githubusercontent.com/35755386/158451522-61e0c14c-57c3-4819-b023-b7d7c13075aa.png">

---

## Dependencies

[**Unity** Game Engine](https://unity.com)

[TextMeshPro](https://docs.unity3d.com/Manual/com.unity.textmeshpro.html)

---

## Troubleshooting (Unity / UGUI compile errors)

If you see errors like missing `UnityEngine.UI`, `UIToolkitInteroperabilityBridge`, or broken `UnityEngine.TestTools` types inside package paths, your **`Library`** cache and/or **`Packages/com.unity.*`** folders are usually corrupted (never edit packages under `Library/PackageCache` or embedded registry duplicates). **Quit Unity**, run **`./clean-unity-library.sh`** from the repo root (it removes `First Principles/Library` and stray `Packages/com.unity.ugui` / `com.unity.test-framework` if present), then reopen Unity. Details: [Docs/Fix-Unity-UGUI-PackageErrors.md](First%20Principles/Docs/Fix-Unity-UGUI-PackageErrors.md).

---

**MIT** License | **First Principles** is a part of a **not-for-profit** and an **open-source** project *College Math For Toddlers*.
