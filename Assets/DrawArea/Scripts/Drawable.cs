using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class Drawable : MonoBehaviour
{
	[SerializeField] private Texture2D paintTexture;
	[SerializeField] private Texture2D resetTexture;
	[SerializeField] private Texture2D fadingTexture;
	[SerializeField] private Color paintColor = Color.red;
	[SerializeField] private Color finishedColor = Color.red;
	[SerializeField] private int brushWidth = 10;

	[SerializeField] private List<Vector2> drawpoints = new List<Vector2>();
	private bool isPainting = false;

	private Vector2 previousHitPoint = Vector2.zero;

	private bool isFading = false;
	[SerializeField] private float fadeSpeed = 0.02f;

	private void Update()
	{
		if (Input.GetMouseButtonDown(0)) // Left mouse button pressed
		{
			isPainting = true;
		}
		else if (Input.GetMouseButtonUp(0)) // Left mouse button released
		{
			isPainting = false;
			isFading = true;
			ResetTexture();
			previousHitPoint = Vector2.zero; // Reset the previous hit point
		}

		if (isPainting)
		{
			Vector2 hitPoint = GetHitPoint();

			// If the mouse has moved
			if (hitPoint != Vector2.zero && !drawpoints.Contains(hitPoint))
			{
				if (previousHitPoint != Vector2.zero)
				{
					DrawLine(previousHitPoint, hitPoint);
				}

				PaintTexture(hitPoint);
				drawpoints.Add(hitPoint);

				previousHitPoint = hitPoint;
			}
		}
		if (isFading)
		{
			FadeOutDrawing();
		}
	}

	private void FadeOutDrawing()
	{
		Color[] currentPixels = fadingTexture.GetPixels();
		Color[] referencePixels = resetTexture.GetPixels();

		bool isFinishedFading = true;

		for (int i = 0; i < currentPixels.Length; i++)
		{
			currentPixels[i] = Color.Lerp(currentPixels[i], referencePixels[i], fadeSpeed);

			// Check if the pixel has fully faded
			if (currentPixels[i].a > 0.1f)
			{
				isFinishedFading = false;
			}
			else
			{
				// If the pixel has fully faded, set it to fully transparent
				currentPixels[i] = referencePixels[i];
			}
		}

		fadingTexture.SetPixels(currentPixels);
		fadingTexture.Apply();

		if (isFinishedFading)
		{
			isFading = false;
		}
	}

	private void DrawLine(Vector2 start, Vector2 end)
	{
		Vector2 direction = (end - start).normalized;
		float distance = Vector2.Distance(start, end);

		for (float i = 0; i < distance; i += 1f)
		{
			Vector2 point = start + direction * i;
			PaintTexture(point);
		}
	}

	private Vector2 GetHitPoint()
	{
		Vector2 hitPoint = Vector2.zero;
		RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

		if (hit.collider != null && hit.collider.gameObject == gameObject)
		{
			Vector2 localHitPoint = hit.point - (Vector2)hit.collider.bounds.min;
			hitPoint = new Vector2(localHitPoint.x / hit.collider.bounds.size.x, localHitPoint.y / hit.collider.bounds.size.y);
			hitPoint.x *= paintTexture.width;
			hitPoint.y *= paintTexture.height;
		}
		return hitPoint;
	}

	public void PaintTexture(Vector2 hitPoint)
	{
		int centerX = Mathf.FloorToInt(hitPoint.x);
		int centerY = Mathf.FloorToInt(hitPoint.y);

		int radius = brushWidth / 2;
		int radiusSquared = radius * radius;

		for (int x = centerX - radius; x <= centerX + radius; x++)
		{
			for (int y = centerY - radius; y <= centerY + radius; y++)
			{
				if (x >= 0 && x < paintTexture.width && y >= 0 && y < paintTexture.height)
				{
					int distanceSquared = (x - centerX) * (x - centerX) + (y - centerY) * (y - centerY);
					if (distanceSquared <= radiusSquared)
					{
						paintTexture.SetPixel(x, y, paintColor);
					}
				}
			}
		}
		paintTexture.Apply();
	}

	private void ResetTexture()
	{
		GestureManager.Instance.Process(drawpoints);
		drawpoints.Clear();

		finishedColor = GestureManager.Instance.templateColor;

		Color[] referencePixels = paintTexture.GetPixels();
		Color[] fadingPixels = fadingTexture.GetPixels();

		const float colorTolerance = 0.1f;
		for (int i = 0; i < referencePixels.Length; i++)
		{
			// Check if the current pixel color matches the paintColor
			float colorDifference = Vector4.Distance(referencePixels[i], paintColor);
			if (colorDifference <= colorTolerance)
			{
				// Replace the fading texture pixel with the second color
				fadingPixels[i] = finishedColor;
			}
			else
			{
				fadingPixels[i] = referencePixels[i];
			}
		}

		// Apply the modified fading texture pixels
		fadingTexture.SetPixels(fadingPixels);
		fadingTexture.Apply();

		Color[] currentPixels = resetTexture.GetPixels();

		paintTexture.SetPixels(currentPixels);
		paintTexture.Apply();
	}
}
