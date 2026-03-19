using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerControllerUI2D : MonoBehaviour
{
    [Header("Movement (Grid Units)")]
    [SerializeField] private float moveSpeedGridPerSec = 7f;
    [SerializeField] private float gravityGridPerSec2 = 20f;
    [SerializeField] private float jumpVelocityGridPerSec = 9f;

    [Header("Player Size (Grid Units)")]
    [SerializeField] private float playerWidthGrid = 0.6f;
    [SerializeField] private float playerHeightGrid = 0.9f;

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

    public Vector2 PlayerCenterGrid => posGrid;

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

    public void ResetToSpawn(GraphWorld world)
    {
        if (world == null)
            return;

        this.world = world;
        isDead = false;
        isFinished = false;
        grounded = false;

        float halfW = playerWidthGrid / 2f;
        float halfH = playerHeightGrid / 2f;

        posGrid = new Vector2(world.spawnXGrid, world.spawnYTopGrid + halfH + 0.01f);
        velGrid = Vector2.zero;

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

    private void Update()
    {
        if (world == null || playerRect == null)
            return;

        if (isDead || isFinished)
            return;

        float dt = Time.deltaTime;

        // Input -> velocity.
        float inputX = Input.GetAxisRaw("Horizontal");
        velGrid.x = inputX * moveSpeedGridPerSec;

        if (grounded && Input.GetButtonDown("Jump"))
        {
            velGrid.y = jumpVelocityGridPerSec;
            grounded = false;
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
        ApplyVisualPosition();
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

