using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// =============================================================================
// PlayerControllerUI2D — UI RectTransform “character” in graph grid space
// =============================================================================
// Simulation runs in abstract grid units (same as GraphWorld / GridRect). Each frame:
//   • Reads horizontal intent (keys → axis → MobileInputBridge touch cluster).
//   • Applies gravity, resolves AABB vs platforms, hazards, fall death Y, finish band.
// Visual position maps grid → pixels via unitWidth/Height from LevelManager.
// =============================================================================

/// <summary>Side-scroller controller built for uGUI under the Cartesian plane.</summary>
public class PlayerControllerUI2D : MonoBehaviour
{
    [Header("Movement (Grid Units)")]
    [SerializeField] private float moveSpeedGridPerSec = 7f;
    [SerializeField] private float gravityGridPerSec2 = 20f;
    [SerializeField] private float jumpVelocityGridPerSec = 9f;

    [Header("f′ line — air jump")]
    [Tooltip("Grid-space proximity to f′ (player center vs polyline). Wider = easier to register a 'hit'.")]
    [SerializeField] private float derivativeTouchRadiusGrid = 0.58f;
    [Tooltip("Upward impulse when you press jump while airborne and overlapping f′ (once per approach until you leave the band).")]
    [SerializeField] private float doubleJumpVelocityMultiplier = 0.9f;
    [SerializeField] private float derivativeHighlightLerpSpeed = 7f;
    [Tooltip("First frame the avatar enters the f′ band (not every frame while sliding). On mobile, vibration replaces sound.")]
    [SerializeField] private bool derivativeHitHaptic = true;
    [Tooltip("Short click when haptic is unavailable (PC / editor / haptic off). Ignored on mobile when haptic is on.")]
    [SerializeField] private bool derivativeHitSound = true;
    [SerializeField] private AudioClip derivativeLineHitClip;
    [SerializeField] [Range(0f, 1f)] private float derivativeLineHitVolume = 0.65f;
    [Tooltip("Brief cool tint on the graph backdrop (Game scene → Grid Background) when you graze f′ — mobile & desktop.")]
    [SerializeField] private bool derivativeHitBackgroundTint = true;
    [SerializeField] private float derivativeHitBackgroundTintSeconds = 0.32f;
    [SerializeField] private Color derivativeHitBackgroundTintDelta = new Color(0.048f, 0.06f, 0.1f, 0f);

    [Header("Player Size (Grid Units)")]
    [SerializeField] private float playerWidthGrid = 0.78f;
    [SerializeField] private float playerHeightGrid = 1.12f;

    private RectTransform playerRect;
    private Image playerImage;

    private float unitWidth;
    private float unitHeight;

    private GraphWorld world;
    private Vector2 posGrid;
    private Vector2 velGrid;
    private bool grounded;

    private float deathMinYGrid = -2f;
    private bool isDead;
    private bool isFinished;

    private Action deathCallback;
    private Action finishCallback;

    /// <summary>When true (e.g. stage intro overlay), physics/input integration is skipped so the avatar stays frozen.</summary>
    private bool inputLocked;

    private DerivRendererUI derivativeLineRenderer;
    private bool wasTouchingDerivativeLine;
    /// <summary>True after last call to <see cref="UpdateDerivativeTouchAndHighlight"/> this frame.</summary>
    private bool touchingDerivativeNow;
    /// <summary>After using f′ air jump, stay true until you leave the band or land (prevents spam while sliding on the line).</summary>
    private bool derivativeAirJumpConsumedThisBand;
    /// <summary>Fresh level entry: first grounded jump uses <see cref="jumpVelocityGridPerSec"/>×1.5 once.</summary>
    private bool strongFirstGroundJumpPending;
    private float derivativeHighlightSmoothed;
    private AudioSource derivativeHitAudio;

    private Image gridBackgroundImage;
    private Color gridBackgroundRestColor;
    private float derivativeBgTintPhase;

    const string GridBackgroundObjectName = "Grid Background";

    public Vector2 PlayerCenterGrid => posGrid;

    /// <summary>Root <see cref="RectTransform"/> for the avatar (grid-anchored). For screen-space UI positioning.</summary>
    public RectTransform PlayerVisualRect => playerRect;

    public void BindDerivativeRenderer(DerivRendererUI derivativeRenderer)
    {
        derivativeLineRenderer = derivativeRenderer;
        derivativeHighlightSmoothed = 0f;
        wasTouchingDerivativeLine = false;
        touchingDerivativeNow = false;
        derivativeAirJumpConsumedThisBand = false;
        if (derivativeLineRenderer != null)
            derivativeLineRenderer.playerProximityHighlight = 0f;
    }

    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;
        if (locked)
            velGrid = Vector2.zero;
    }

    public void BindVisual(RectTransform rect, Image img)
    {
        playerRect = rect;
        playerImage = img;
    }

    public void SetDeathMinYGrid(float deathMinYGrid)
    {
        this.deathMinYGrid = deathMinYGrid;
    }

    public void SetGridToPixelUnits(float unitWidth, float unitHeight)
    {
        this.unitWidth = unitWidth;
        this.unitHeight = unitHeight;

        if (playerRect != null)
        {
            playerRect.sizeDelta = new Vector2(playerWidthGrid * unitWidth, playerHeightGrid * unitHeight);
        }
    }

    public void SetWorld(GraphWorld world)
    {
        this.world = world;
    }

    public void ResetToSpawn(GraphWorld world, bool grantStrongFirstGroundJump = false)
    {
        if (world == null)
            return;

        this.world = world;
        isDead = false;
        isFinished = false;
        grounded = false;
        strongFirstGroundJumpPending = grantStrongFirstGroundJump;

        float halfH = playerHeightGrid / 2f;

        posGrid = new Vector2(world.spawnXGrid, world.spawnYTopGrid + halfH + 0.01f);
        velGrid = Vector2.zero;

        wasTouchingDerivativeLine = false;
        touchingDerivativeNow = false;
        derivativeAirJumpConsumedThisBand = false;
        derivativeHighlightSmoothed = 0f;
        if (derivativeLineRenderer != null)
        {
            derivativeLineRenderer.playerProximityHighlight = 0f;
            derivativeLineRenderer.RefreshHighlightGeometry();
        }

        derivativeBgTintPhase = 0f;
        RefreshGridBackgroundCacheAndRestoreColor();

        ApplyVisualPosition();
    }

    public void SetDeathCallback(Action deathCallback)
    {
        this.deathCallback = deathCallback;
    }

    public void SetFinishCallback(Action finishCallback)
    {
        this.finishCallback = finishCallback;
    }

    /// <summary>Integrate movement, resolve collisions, invoke death/finish callbacks.</summary>
    private void Update()
    {
        if (world == null || playerRect == null)
            return;

        if (isDead || isFinished)
            return;

        if (inputLocked)
            return;

        float dt = Time.deltaTime;

        // Movement: keyboard (arrows / WASD), gamepad stick, then on-screen touch bridge.
        float inputX = ReadHorizontalInput();
        if (Mathf.Approximately(inputX, 0f))
            inputX = MobileInputBridge.TouchHorizontal;

        velGrid.x = inputX * moveSpeedGridPerSec;

        if (grounded)
            derivativeAirJumpConsumedThisBand = false;

        UpdateDerivativeTouchAndHighlight(dt);

        bool jumpPressed = ReadJumpPressed();
        if (!jumpPressed)
            jumpPressed = MobileInputBridge.ConsumeJump();

        if (jumpPressed)
        {
            if (grounded)
            {
                float jv = jumpVelocityGridPerSec;
                if (strongFirstGroundJumpPending)
                {
                    jv *= 1.5f;
                    strongFirstGroundJumpPending = false;
                }
                velGrid.y = jv;
                grounded = false;
            }
            else if (CanUseDerivativeAirJump())
            {
                velGrid.y = jumpVelocityGridPerSec * doubleJumpVelocityMultiplier;
                derivativeAirJumpConsumedThisBand = true;
            }
        }

        // Gravity.
        velGrid.y -= gravityGridPerSec2 * dt;

        Vector2 prevPos = posGrid;
        Vector2 nextPos = posGrid;

        // Horizontal step + collision.
        nextPos.x = posGrid.x + velGrid.x * dt;
        ResolveHorizontalPlatforms(ref nextPos);

        // Vertical step + collision.
        nextPos.y = posGrid.y + velGrid.y * dt;
        ResolveVerticalPlatforms(prevPos, ref nextPos, ref grounded);

        // Hazard check.
        if (OverlapsAny(world.hazards, nextPos))
        {
            Die();
        }
        else if (nextPos.y - playerHeightGrid / 2f < deathMinYGrid)
        {
            Die();
        }

        // Finish check.
        if (IsInFinishZone(nextPos))
        {
            isFinished = true;
            finishCallback?.Invoke();
            return;
        }

        posGrid = nextPos;

        ClampHorizontalToPlayBounds();

        TickDerivativeBackgroundTint(dt);

        ApplyVisualPosition();
    }

    /// <summary>Hard horizontal limits so the avatar stays over the padded playfield (screen/safe + touch bar).</summary>
    private void ClampHorizontalToPlayBounds()
    {
        if (world == null || !world.hasPlayBounds)
            return;

        float halfW = playerWidthGrid * 0.5f;
        posGrid.x = Mathf.Clamp(posGrid.x, world.playBounds.XMin + halfW, world.playBounds.XMax - halfW);
    }

    /// <summary>Air jump only while overlapping f′ and not already used until you leave the band.</summary>
    private bool CanUseDerivativeAirJump()
    {
        return touchingDerivativeNow && !derivativeAirJumpConsumedThisBand;
    }

    /// <summary>
    /// f′ proximity: highlight, hit feedback, and <see cref="touchingDerivativeNow"/> for air jump.
    /// </summary>
    private void UpdateDerivativeTouchAndHighlight(float dt)
    {
        bool touching = false;
        if (derivativeLineRenderer != null)
        {
            var pts = derivativeLineRenderer.points;
            if (pts != null && pts.Count >= 2 && derivativeTouchRadiusGrid > 0.01f)
                touching = MinDistanceToPolylineSq(posGrid, pts) <= derivativeTouchRadiusGrid * derivativeTouchRadiusGrid;
        }

        // Leave f′ → can earn another air jump on the next approach while aloft.
        if (!touching)
            derivativeAirJumpConsumedThisBand = false;

        if (touching && !wasTouchingDerivativeLine)
        {
            PlayDerivativeLineHitFeedback();
            PulseDerivativeBackgroundTint();
        }

        wasTouchingDerivativeLine = touching;
        touchingDerivativeNow = touching;

        if (derivativeLineRenderer != null)
        {
            float targetHl = touching ? 1f : 0f;
            derivativeHighlightSmoothed = Mathf.MoveTowards(
                derivativeHighlightSmoothed,
                targetHl,
                derivativeHighlightLerpSpeed * dt);
            derivativeLineRenderer.playerProximityHighlight = derivativeHighlightSmoothed;
            derivativeLineRenderer.RefreshHighlightGeometry();
        }
    }

    /// <summary>One-shot when the circle first crosses into the f′ proximity band.</summary>
    private void PlayDerivativeLineHitFeedback()
    {
        bool usedHaptic = derivativeHitHaptic && Application.isMobilePlatform;
        if (usedHaptic)
            Handheld.Vibrate();

        // Sound only where vibration isn’t used (no haptic on this platform/path).
        if (usedHaptic)
            return;

        if (!derivativeHitSound || derivativeLineHitClip == null)
            return;

        if (derivativeHitAudio == null)
        {
            derivativeHitAudio       = GetComponent<AudioSource>();
            if (derivativeHitAudio == null)
                derivativeHitAudio = gameObject.AddComponent<AudioSource>();
            derivativeHitAudio.playOnAwake = false;
            derivativeHitAudio.spatialBlend = 0f;
            derivativeHitAudio.loop = false;
        }

        derivativeHitAudio.PlayOneShot(derivativeLineHitClip, derivativeLineHitVolume);
    }

    private void EnsureGridBackgroundImage()
    {
        if (gridBackgroundImage != null)
            return;
        var go = GameObject.Find(GridBackgroundObjectName);
        if (go == null)
            return;
        gridBackgroundImage = go.GetComponent<Image>();
        if (gridBackgroundImage != null)
            gridBackgroundRestColor = gridBackgroundImage.color;
    }

    private void RefreshGridBackgroundCacheAndRestoreColor()
    {
        if (gridBackgroundImage == null)
            EnsureGridBackgroundImage();
        if (gridBackgroundImage == null)
            return;
        gridBackgroundRestColor = gridBackgroundImage.color;
        gridBackgroundImage.color = gridBackgroundRestColor;
    }

    private void PulseDerivativeBackgroundTint()
    {
        if (!derivativeHitBackgroundTint || derivativeHitBackgroundTintSeconds < 1e-4f)
            return;
        EnsureGridBackgroundImage();
        if (gridBackgroundImage == null)
            return;
        if (derivativeBgTintPhase <= 0.01f)
            gridBackgroundRestColor = gridBackgroundImage.color;
        derivativeBgTintPhase = 1f;
    }

    private void TickDerivativeBackgroundTint(float dt)
    {
        if (!derivativeHitBackgroundTint || gridBackgroundImage == null)
            return;
        if (derivativeBgTintPhase <= 0f)
            return;

        float dur = Mathf.Max(0.04f, derivativeHitBackgroundTintSeconds);
        derivativeBgTintPhase = Mathf.Max(0f, derivativeBgTintPhase - dt / dur);
        Color peak = gridBackgroundRestColor + derivativeHitBackgroundTintDelta;
        peak.r = Mathf.Clamp01(peak.r);
        peak.g = Mathf.Clamp01(peak.g);
        peak.b = Mathf.Clamp01(peak.b);
        peak.a = gridBackgroundRestColor.a;
        Color c = Color.Lerp(gridBackgroundRestColor, peak, derivativeBgTintPhase);
        c.a = gridBackgroundRestColor.a;
        gridBackgroundImage.color = c;

        if (derivativeBgTintPhase <= 0f)
            gridBackgroundImage.color = gridBackgroundRestColor;
    }

    private static float MinDistanceToPolylineSq(Vector2 p, List<Vector2> pts)
    {
        float best = float.PositiveInfinity;
        for (int i = 0; i < pts.Count - 1; i++)
        {
            float d = PointToSegmentDistanceSq(p, pts[i], pts[i + 1]);
            if (d < best)
                best = d;
        }

        return best;
    }

    private static float PointToSegmentDistanceSq(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float den = ab.sqrMagnitude;
        if (den < 1e-10f)
            return (p - a).sqrMagnitude;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / den);
        Vector2 proj = a + t * ab;
        return (p - proj).sqrMagnitude;
    }

    private static float ReadHorizontalInput()
    {
        float inputX = 0f;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed)
                inputX -= 1f;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed)
                inputX += 1f;
        }

        if (Mathf.Approximately(inputX, 0f))
        {
            var gp = Gamepad.current;
            if (gp != null)
            {
                float lx = gp.leftStick.x.ReadValue();
                if (Mathf.Abs(lx) > 0.5f)
                    inputX = Mathf.Sign(lx);
                else if (Mathf.Abs(lx) > 0.12f)
                    inputX = lx;
            }
        }

        return inputX;
    }

    private static bool ReadJumpPressed()
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.spaceKey.wasPressedThisFrame ||
                kb.wKey.wasPressedThisFrame ||
                kb.upArrowKey.wasPressedThisFrame)
                return true;
        }

        var gp = Gamepad.current;
        if (gp != null && gp.buttonSouth.wasPressedThisFrame)
            return true;

        return false;
    }

    private void ResolveHorizontalPlatforms(ref Vector2 pos)
    {
        float halfW = playerWidthGrid / 2f;
        float halfH = playerHeightGrid / 2f;

        float pxMin = pos.x - halfW;
        float pxMax = pos.x + halfW;
        float pyMin = pos.y - halfH;
        float pyMax = pos.y + halfH;

        // Basic AABB side resolution.
        foreach (var p in world.platforms)
        {
            bool overlapsY = pyMin < p.yMax && pyMax > p.yMin;
            if (!overlapsY)
                continue;

            bool overlapsX = pxMin < p.xMax && pxMax > p.xMin;
            if (!overlapsX)
                continue;

            if (velGrid.x > 0f)
            {
                pos.x = p.xMin - halfW;
            }
            else if (velGrid.x < 0f)
            {
                pos.x = p.xMax + halfW;
            }
        }
    }

    /// <summary>Land on platform tops when falling; bonk head when rising through thin solids.</summary>
    private void ResolveVerticalPlatforms(Vector2 prevPos, ref Vector2 pos, ref bool groundedOut)
    {
        groundedOut = false;

        float halfW = playerWidthGrid / 2f;
        float halfH = playerHeightGrid / 2f;

        float prevBottom = prevPos.y - halfH;
        float prevTop = prevPos.y + halfH;
        float nextBottom = pos.y - halfH;
        float nextTop = pos.y + halfH;

        float pxMin = pos.x - halfW;
        float pxMax = pos.x + halfW;

        const float eps = 0.001f;

        foreach (var p in world.platforms)
        {
            bool overlapsX = pxMin < p.xMax && pxMax > p.xMin;
            if (!overlapsX)
                continue;

            // Falling onto platform.
            if (velGrid.y <= 0f)
            {
                bool crossedTopSurface = prevBottom >= p.yMax - eps && nextBottom <= p.yMax + eps;
                if (crossedTopSurface)
                {
                    pos.y = p.yMax + halfH;
                    velGrid.y = 0f;
                    groundedOut = true;
                }
            }
            else // Rising into platform
            {
                bool crossedBottomSurface = prevTop <= p.yMin + eps && nextTop >= p.yMin - eps;
                if (crossedBottomSurface)
                {
                    pos.y = p.yMin - halfH;
                    velGrid.y = 0f;
                }
            }
        }
    }

    private bool OverlapsAny(List<GridRect> rects, Vector2 center)
    {
        float halfW = playerWidthGrid / 2f;
        float halfH = playerHeightGrid / 2f;
        float pxMin = center.x - halfW;
        float pxMax = center.x + halfW;
        float pyMin = center.y - halfH;
        float pyMax = center.y + halfH;

        for (int i = 0; i < rects.Count; i++)
        {
            var r = rects[i];
            bool overlapX = pxMin < r.xMax && pxMax > r.xMin;
            bool overlapY = pyMin < r.yMax && pyMax > r.yMin;
            if (overlapX && overlapY)
                return true;
        }

        return false;
    }

    private bool IsInFinishZone(Vector2 center)
    {
        // Finish check uses center X only (y doesn't matter; the goal is to reach the end).
        return center.x >= world.finish.xMin && center.x <= world.finish.xMax;
    }

    private void ApplyVisualPosition()
    {
        if (playerRect == null)
            return;

        float halfW = playerWidthGrid / 2f;
        float halfH = playerHeightGrid / 2f;

        // RectTransform anchoredPosition uses bottom-left when pivot=(0,0) and anchors=(0,0).
        float px = (posGrid.x - halfW) * unitWidth;
        float py = (posGrid.y - halfH) * unitHeight;
        playerRect.anchoredPosition = new Vector2(px, py);
    }

    private void Die()
    {
        if (isDead || isFinished)
            return;

        isDead = true;
        deathCallback?.Invoke();
    }
}

