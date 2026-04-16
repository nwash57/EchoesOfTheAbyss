namespace EchoesOfTheAbyss.Lib.Location;

public class Coordinates
{
	public int X { get; set; }
	public int Y { get; set; }
	
	public Coordinates(int x, int y)
	{
		X = x;
		Y = y;
	}
	
	public override string ToString()
	{
		return $"({X},{Y})";
	}

	public override bool Equals(object? obj)
	{
		return obj is Coordinates other && this.Equals(other);
	}

	protected bool Equals(Coordinates other)
	{
		return X == other.X && Y == other.Y;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(X, Y);
	}
}