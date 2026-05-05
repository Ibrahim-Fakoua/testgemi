using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Terrarium_Virtuel.scripts.database;

public interface IGraph
{
}

/// <summary>
/// Data structure for storing scale and dimension information for graph rendering
/// </summary>
public struct ScaleData
{
	public int MinTick, MaxTick;           // X-axis range
	public int MinCount, MaxCount;         // Y-axis range
	public float PixelsPerTickX;           // X scaling factor
	public float PixelsPerCountY;          // Y scaling factor
	public Rect2 Bounds;                   // Drawing area bounds
}

public partial class GraphClasses<T> : Control, IGraph where T : DataQuery
{
	protected List<T> _data = new();

	public virtual void SetData(List<T> data)
	{
		_data = data;
		QueueRedraw();
	}
}

public partial class PopulationGraph : GraphClasses<PopulationRow>
{
	private Dictionary<int, Color> _speciesColors = new();
	private const float Padding = 40f;

	public PopulationGraph()
	{
		CustomMinimumSize = new Vector2(400, 300);
	}

	public override void SetData(List<PopulationRow> data)
	{
		if (data == null || data.Count == 0)
		{
			_data = [];
			QueueRedraw();
			return;
		}

		if (data.Any(r => r.Tick < 0 || r.SpeciesId <= 0 || r.Count < 0))
			GD.PushWarning("[PopulationGraph] Data contains invalid values");

		_data = data;
		_speciesColors.Clear();
		QueueRedraw();
	}

	private Font GetDefaultFont()
	{
		try
		{
			var font = GetThemeFont("font");
			if (font != null) return font;
		}
		catch { }

		try
		{
			return ThemeDB.FallbackFont;
		}
		catch
		{
			GD.PushError("[PopulationGraph] Could not get any font!");
			return null;
		}
	}

	public override void _Draw()
	{
		var font = GetDefaultFont();

		if (_data == null || _data.Count == 0)
		{
			DrawString(font, new Vector2(Padding, Padding + 20), "No data available");
			return;
		}

		DrawRect(new Rect2(0, 0, Size.X, Size.Y), Colors.White);

		var scale = CalculateScale();

		foreach (var series in _data.GroupBy(r => r.SpeciesId))
			DrawDataSeries(scale, series.ToList(), GetOrAssignSpeciesColor(series.Key), font);

		DrawAxes(scale, font);
	}

	private ScaleData CalculateScale()
	{
		var minTick = _data.Min(r => r.Tick);
		var maxTick = _data.Max(r => r.Tick);
		var maxCount = (int)(_data.Max(r => r.Count) * 1.1f);
		if (maxCount == 0) maxCount = 1;

		var bounds = new Rect2(Padding, Padding, Size.X - Padding * 2, Size.Y - Padding * 2);

		return new ScaleData
		{
			MinTick = minTick,
			MaxTick = maxTick,
			MinCount = 0,
			MaxCount = maxCount,
			PixelsPerTickX = bounds.Size.X / (maxTick - minTick > 0 ? maxTick - minTick : 1),
			PixelsPerCountY = bounds.Size.Y / (maxCount > 0 ? maxCount : 1),
			Bounds = bounds
		};
	}

	private static float TickToX(ScaleData scale, int tick) =>
		scale.Bounds.Position.X + (tick - scale.MinTick) * scale.PixelsPerTickX;

	private static float CountToY(ScaleData scale, int count) =>
		scale.Bounds.End.Y - (count - scale.MinCount) * scale.PixelsPerCountY;

	private static Vector2 DataToPixel(ScaleData scale, int tick, int count) =>
		new(TickToX(scale, tick), CountToY(scale, count));

	private void DrawDataSeries(ScaleData scale, List<PopulationRow> series, Color color, Font font)
	{
		var sorted = series.OrderBy(r => r.Tick).ToList();

		for (int i = 0; i < sorted.Count - 1; i++)
			DrawLine(DataToPixel(scale, sorted[i].Tick, sorted[i].Count),
					 DataToPixel(scale, sorted[i + 1].Tick, sorted[i + 1].Count), color, 2f);

		foreach (var point in sorted)
		{
			var pos = DataToPixel(scale, point.Tick, point.Count);
			DrawFlowIndicator(scale, pos, point.Births, Colors.Green, "+", goesUp: true, font);
			DrawFlowIndicator(scale, pos, point.Deaths, Colors.Red, "-", goesUp: false, font);
			DrawCircle(pos, 4f, color);
		}
	}

	private void DrawFlowIndicator(ScaleData scale, Vector2 origin, int value, Color color, string prefix, bool goesUp, Font font)
	{
		if (value <= 0) return;
		var tip = origin + new Vector2(0, goesUp ? -value * scale.PixelsPerCountY : value * scale.PixelsPerCountY);
		DrawLine(origin, tip, color, 3f);
		DrawString(font, tip + new Vector2(4, goesUp ? 0 : 12), $"{prefix}{value}", modulate: color);
	}

	private void DrawAxes(ScaleData scale, Font font)
	{
		DrawGridlines(scale, font);
		DrawLine(scale.Bounds.Position, new Vector2(scale.Bounds.Position.X, scale.Bounds.End.Y), Colors.Black, 1f);
		DrawLine(new Vector2(scale.Bounds.Position.X, scale.Bounds.End.Y), scale.Bounds.End, Colors.Black, 1f);
	}

	private void DrawGridlines(ScaleData scale, Font font)
	{
		var gridColor = new Color(0.9f, 0.9f, 0.9f);

		int yStep = Mathf.Max(1, scale.MaxCount / 5);
		for (int i = 0; i <= scale.MaxCount; i += yStep)
		{
			float y = CountToY(scale, i);
			if (y < scale.Bounds.Position.Y || y > scale.Bounds.End.Y) continue;
			DrawLine(new Vector2(scale.Bounds.Position.X, y), new Vector2(scale.Bounds.End.X, y), gridColor, 1f);
			DrawString(font, new Vector2(scale.Bounds.Position.X - 30, y - 6), i.ToString(), modulate: Colors.Black);
		}

		int xStep = Mathf.Max(1, (scale.MaxTick - scale.MinTick) / 5);
		for (int tick = scale.MinTick; tick <= scale.MaxTick; tick += xStep)
		{
			float x = TickToX(scale, tick);
			if (x < scale.Bounds.Position.X || x > scale.Bounds.End.X) continue;
			DrawLine(new Vector2(x, scale.Bounds.Position.Y), new Vector2(x, scale.Bounds.End.Y), gridColor, 1f);
			DrawString(font, new Vector2(x - 12, scale.Bounds.End.Y + 5), tick.ToString(), modulate: Colors.Black);
		}
	}

	private Color GetOrAssignSpeciesColor(int speciesId)
	{
		if (_speciesColors.TryGetValue(speciesId, out var cached))
			return cached;

		float hue = (_speciesColors.Count * 137.5f) % 360f;
		var color = Color.FromHsv(hue / 360f, 0.7f, 0.9f);
		_speciesColors[speciesId] = color;
		return color;
	}
}
