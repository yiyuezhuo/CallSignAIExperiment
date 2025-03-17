using System;

namespace CallSignLib
{

public enum Side
{
    Blue,
    Red
}

public enum MapState
{
    NotDeployed,
    OnMap,
    Destroyed
}

[Serializable]
public class Piece
{
    public int id;
    public string name;
    public Side side;
    public MapState mapState;

    public int x;
    public int y;

    public int antiAirRating;
    public int antiShipRating;
    public int antiAirRange;
    public int antiShipRange;
    public int specialRange;
    public int fuelRange;

    public bool isJammer;
    public bool isC2; // Command & Control
    public bool isTanker;

    public bool isOnMap
    {
        get => mapState == MapState.OnMap;
    }

    public static Piece MakeFighter(string name, Side side)
    {
        return new()
        {
            name = name,
            side = side,
            antiAirRating = 3,
            antiShipRating = 3,
            antiAirRange = 1,
            antiShipRange = 1,
            fuelRange = 2,
        };
    }

    public static Piece MakeBomber(string name, Side side)
    {
        return new()
        {
            name = name,
            side = side,
            antiAirRating = 1,
            antiShipRating = 5,
            antiAirRange = 1,
            antiShipRange = 2,
            fuelRange = int.MaxValue,
        };
    }

    public static Piece MakeTanker(string name, Side side)
    {
        return new()
        {
            name = name,
            side = side,
            isTanker=true,
            fuelRange = int.MaxValue,
        };
    }

    public static Piece MakeC2(string name, Side side)
    {
        return new()
        {
            name = name,
            side = side,
            isC2=true,
            fuelRange = 2,
        };
    }

    public static Piece MakeJammer(string name, Side side)
    {
        return new()
        {
            name = name,
            side = side,
            isJammer=true,
            fuelRange = 2,
        };
    }
}

}