// -----------------------------------------------------------------------------
// StageRoleplayLibrary — short “story beat” before each level (localization key roleplay.N)
// -----------------------------------------------------------------------------
using UnityEngine;

/// <summary>
/// Immersive one-paragraph hooks keyed <c>roleplay.{levelIndex}</c>; English defaults here,
/// override per locale in <c>Resources/Localization/*.txt</c>.
/// </summary>
public static class StageRoleplayLibrary
{
    private static readonly string[] EnglishDefaults =
    {
        // 0
        "<b>Narrator.</b> You wake as a speck on a smooth graph—no map but the tangent under your toes. Walk the curve; let the derivative tell you where the floor is brave or brittle.",
        // 1
        "<b>Quantum cadet.</b> Command drops you on a parabola drill range. “Slope is truth,” the instructor hums. Every ledge is a policy: rise gently, then betray you at the rim.",
        // 2
        "<b>Radio operator.</b> The sine wave is a carrier you must ride—harmonics thump through the grid. Time your jumps when the crest feels kind and the trough breathes.",
        // 3
        "<b>Shadow cartographer.</b> Cosine plots the night side of the same oscillation—phase-shifted myths. Trust the derivative: it remembers where the light still grazes.",
        // 4
        "<b>Pathfinder.</b> Absolute value folds the plane into gleaming V‑tracks. Corners are not evil—just places where slope opinion flips without asking.",
        // 5
        "<b>Archivist.</b> The Tower of e^x stores every growth protocol in one elegant tower: each floor’s height copies itself. You climb an endless self-similar staircase.",
        // 6
        "<b>Choir mathematician.</b> Maclaurin’s hymn says sine begins as a whisper of x, then twists with higher harmonics. Listen for where the linear lie ends.",
        // 7
        "<b>Treasurer.</b> A geometric tail binds infinite pennies into finite gold—if the ratio cooperates. Balance on the shrinking steps; don’t trust divergent dreams.",
        // 8
        "<b>Saddle scout.</b> Multivariable ghosts whisper through a slice: mountains face one way, valleys another. Your 2D path is a treaty between opposing curvatures.",
        // 9
        "<b>Glasswright.</b> The paraboloid’s ring feels like polishing a lens: gentle curvature until the focus punishes arrogance. Stay polite to the normal vector.",
        // 10
        "<b>Integrator.</b> Area isn’t decoration—it’s the story of accumulation. Each brick you stand on is testimony that infinitely many thin choices make heft.",
        // 11
        "<b>Surveyor (left rule).</b> You stamp rectangles from the left edge like a bureaucrat who always starts early. Rough, honest, marching with conservative bias.",
        // 12
        "<b>Surveyor (right rule).</b> Same grid, opposite superstition: you sample the future shoulder first. The ladder tilts; feel how endpoints bias the climb.",
        // 13
        "<b>Surveyor (midpoint).</b> You cheat toward the diplomatic center—often kinder than extremists on either side. Balance is its own kind of courage.",
        // 14
        "<b>Test pilot.</b> Spring, dashpot, dream: oscillation wrapped in exponential decay. The envelope tightens; enjoy the ringing while damping still respects you.",
        // 15
        "<b>Bridge wright.</b> Catenary—not parabola—holds the cable honest under its own weight. Respect cosh; it argues with gravity on first principles.",
        // 16
        "<b>Power engineer.</b> Someone rectified the sine wave—now every lobe points forward. Sharp teeth at the joins; the derivative jumps like a breaker trip.",
        // 17
        "<b>Navigator.</b> Arctangent points you through bounded angles; inverse trig is diplomacy between domains. Watch the asymptotes negotiate at infinity’s table.",
        // 18
        "<b>Ecologist.</b> Logistic growth caps the feast: early eras feel exponential, late eras argue about carrying capacity. The curve sighs as it learns limits.",
        // 19
        "<b>Polar poet.</b> Cardioid hearts sketch in r; each petal is a radius mood swing. Rhythm is radial; your feet translate polar gossip into cartesian steps.",
        // 20
        "<b>Florist of frequencies.</b> Rose curves braid petals from cos(nθ)—symmetry with teeth. Every lobe is a vote between n and your patience.",
        // 21
        "<b>Relay runner.</b> Sinh and cosh sprint the hyperbola’s relay—exponential teammates minus mirror teammates. Area and slope trade batons without looking back.",
        // 22
        "<b>Lab technician.</b> Exponential decay remembers τ and RC constants like bedtime stories. Half-lives are gossip; derivatives never forget proportionality.",
        // 23
        "<b>Gyro tech.</b> Angular momentum is stubborn inventory: I and ω bargain while you steer. Spin conservation narrates every near-miss with torque.",
        // 24
        "<b>Ballistics clerk.</b> Projectile height y(t) is parabolic theatre—gravity directs, velocity overrules. Apex is a treaty signed midair.",
        // 25
        "<b>Harmonic scholar.</b> Cosine’s Maclaurin is the cosine of humility: start at one, borrow x² terms carefully. Even functions demand symmetrical respect.",
        // 26
        "<b>Log keeper.</b> ln x prefers domain honesty—no nonpositive whispers. ∫dx/x invites constants of integration;+C is your diplomatic passport.",
        // 27
        "<b>Mine scout.</b> √x pins a boundary like a cusp lantern—domain walls exist. Approach slowly; derivatives blow whistles near the gate.",
        // 28
        "<b>Asymptote clown.</b> Tan x performs between vertical hecklers—jumps are the punchline. Land mid-period, not on the trap lines.",
        // 29
        "<b>Growth hacker.</b> e^{kx} and y′=ky share the same confession booth. Proportional rates mean the slope rat-tails every height you earn.",
        // 30
        "<b>Phase actor.</b> Simple harmonic motion swaps kinetic and potential energy like masks. Phase angle tells you who’s on stage: speed or stretch.",
        // 31
        "<b>Sketch artist.</b> Cubic mood boards need inflection tea parties—where concavity flips allegiance. Derivatives vote first, second derivatives impeach.",
        // 32
        "<b>Base negotiator.</b> b^x drags ln b along as chaperone. Exponential laws rewrite themselves per base until e forgives everyone.",
        // 33
        "<b>Circle lawyer.</b> (x−h)²+(y−k)²=R² fails the vertical-line exam—so you walk the upper contract only. Semicircles still argue with implicit bias.",
        // 34
        "<b>Wind-tunnel initiate.</b> Lift coefficient whispers to angle of attack until stall throws a tantrum. Respect the breakpoint; it’s older than your clearance badge.",
        // 35
        "<b>Performance engineer.</b> Drag polar plots your sins against lift: parasitic weight plus induced gossip. Min-drag C_L is the quiet table at the banquet.",
        // 36
        "<b>High-altitude clerk.</b> Isothermal density falls exponentially—scale height is the accountant. Thinner air, quieter Reynolds gossip, louder truth about thrust lapse.",
        // 37
        "<b>Test pilot (modes).</b> Phugoid myths trade altitude for speed slowly; short-period pitches snap faster. Damping writes apologies on every oscillation.",
        // 38
        "<b>Hypersonic scribe.</b> Newtonian sin²α scratches impact lore on blunt faces—models, not scripture. Use it to speak, not to land contracts.",
        // 39
        "<b>Wake listener.</b> Strouhal hums a shedding song—bluff bodies gossip at f~St·U/D. Feel the sine fingerprint of organized wake jazz.",
        // 40
        "<b>Shield monk.</b> Re-entry trades dynamic pressure against velocity cubed in campfire tales. The envelope relaxes: breathe as ρ and V forgive you.",
        // 41
        "<b>Equities ghost.</b> You ride a stylized dot-com decade—run-up, vertigo, air pocket—where every slope is a headline and second derivative feels like sentiment flipping overnight.",
        // 42
        "<b>Risk officer.</b> The 2008 arc is a cliff masquerading as a bull: crawl the crest, brace for the sheer drop, then learn that recovery is a slow integral, not a bounce.",
        // 43
        "<b>Fractal exile.</b> Mandelbrot boundary guards escape routes—each iterate is a spell. Step wrong and infinity notices; step right and you pierce a holographic veil.",
        // 44
        "<b>Heat engine cadet.</b> Compression trades volume for pressure along an adiabat—no heat admitted, just workledger math. Feel the steepening as γ insists the gas fight back harder per liter squeezed.",
        // 45
        "<b>Gilded navigator.</b> Every quarter-turn the world wants to scale itself by φ—the spiral is a compass that never forgets proportion. Follow the radius outward: each step is geometric, not arithmetic.",
        // 46
        "<b>Spectrum scribe.</b> A rectangle in one domain blooms into a sinc in the other—side lobes are the honest echo of sharp corners. You are reading the interference pattern of changing coordinates.",
        // 47
        "<b>s‑plane pilgrim.</b> The exponential after the switch-on is the seed of every pole story: decay in time becomes algebra in s. The past is zero until the kernel says go.",
        // 48
        "<b>Fractal exile, encore.</b> You return to Mandelbrot’s coastline for the last word—each iterate still votes on escape, and the boundary remembers every branch you dared to name.",
        // 49
        "<b>Strange butterfly.</b> Lorenz’s wings beat on a thread of time: the curve you ride is sensitive, restless, forever retuning—small shifts in the unseen become storms in the visible trace."
    };

    /// <summary>Must match <see cref="GameLevelCatalog.LevelCount"/>.</summary>
    public static int Count => EnglishDefaults.Length;

    public static string GetRoleplayText(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= EnglishDefaults.Length)
        {
            Debug.LogWarning($"StageRoleplayLibrary: invalid level index {levelIndex}");
            return "";
        }

        return LocalizationManager.GetWithFallback($"roleplay.{levelIndex}", EnglishDefaults[levelIndex]);
    }
}
