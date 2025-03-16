using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.Xml;
using System.IO;


namespace CallSignLib
{

public interface IGameAction
{
    void Execute(GameState state);
}

public class MoveAction : IGameAction
{
    public int pieceId;
    public int toX;
    public int toY;

    public void Execute(GameState state)
    {
        var piece = state.pieces.Find(x => x.id == pieceId);
        piece.x = toX;
        piece.y = toY;
    }

    public override string ToString()
    {
        return $"MoveAction({pieceId}, {toX}, {toY})";
    }
}

public class C2MoveAction : IGameAction
{
    public int pieceidC2;
    public int pieceId1;
    public int toX1;
    public int toY1;
    public int pieceId2;
    public int toX2;
    public int toY2;

    public void Execute(GameState state)
    {
        var piece1 = state.pieces.Find(x => x.id == pieceId1);
        var piece2 = state.pieces.Find(x => x.id == pieceId2);

        piece1.x = toX1;
        piece1.y = toY1;

        piece2.x = toX2;
        piece2.y = toY2;
    }

    public override string ToString()
    {
        return $"C2MoveAction({pieceidC2}, {pieceId1}, {toX1}, {toY1}, {pieceId2}, {toX2}, {toY2})";
    }

}

public class DeployAction : IGameAction
{
    public int pieceId;
    public int toX;
    public int toY;

    public void Execute(GameState state)
    {
        var piece = state.pieces.Find(x => x.id == pieceId);
        piece.x = toX;
        piece.y = toY;
        piece.mapState = MapState.OnMap;
    }

    public override string ToString()
    {
        return $"DeployAction({pieceId}, {toX}, {toY})";
    }

}

public class RegenerateAction : IGameAction
{
    public int pieceId;
    public int toX;
    public int toY;

    public void Execute(GameState state)
    {
        var piece = state.pieces.Find(x => x.id == pieceId);
        piece.x = toX;
        piece.y = toY;
        piece.mapState = MapState.OnMap;
    }

    public override string ToString()
    {
        return $"RegenerateAction({pieceId}, {toX}, {toY})";
    }

}

public class NullAction : IGameAction
{
    public void Execute(GameState state)
    {
    }

    public override string ToString()
    {
        return $"NullAction()";
    }

}

public class EngagmentDeclare : IGameAction
{
    public enum EngagementType
    {
        Aircraft,
        Carrier
    }

    public class EngagementRecord
    {
        public EngagementType type;
        public int shooterPieceId;
        public int targetPieceId;

        public override string ToString()
        {
            return $"({type}, {shooterPieceId}, {targetPieceId})";
        }
    }

    public List<EngagementRecord> records = new();

    public void Execute(GameState state)
    {
        state.engagementDeclares.AddRange(records);
    }

    public override string ToString()
    {
        var ds = string.Join(",", records.Select(r => r.ToString()));
        return $"EngagmentDeclare({ds})";
    }

}

public class EvadingDeclare : IGameAction
{
    public class EvadingRecord
    {
        public int pieceId;
        public int toX;
        public int toY;

        public override string ToString()
        {
            return $"({pieceId}, {toX}, {toY})";
        }
    }

    public List<EvadingRecord> records = new();

    public void Execute(GameState state)
    {
        state.evadingDeclares.AddRange(records);
    }

    public override string ToString()
    {
        var ds = string.Join(",", records);
        return $"MoveAction({ds})";
    }
}


public class GameState
{
    public List<Piece> pieces;
    public int blueDamage;
    public int redDamage;
    
    public Side currentSide;
    public Side turnInitialSide;
    public Phase currentPhase;

    public static FrozenHexGrid grid = FrozenHexGrid.Make();

    // public Dictionary<Side, SideData> sideDataMap;
    public List<SideData> sideData;

    public List<EngagmentDeclare.EngagementRecord> engagementDeclares = new();
    // public List<int> engagementCarrierDeclares = new();
    public List<EvadingDeclare.EvadingRecord> evadingDeclares = new();

    public enum VictoryStatus
    {
        Undetermined,
        Draw,
        OneSideVictory
    }
    public VictoryStatus victoryStatus;
    public Side victorySide;

    public static event EventHandler<string> logged;

    public enum Phase
    {
        TurnBegin, // roll for initial side for the turn
        Action, // move / deploy / regenerate
        EngagementDeclare, // declare engagement
        EvadingDeclare, // declare evading
        TurnEnd // resolve engagement
    }

    public IEnumerable<Piece> piecesOnMap
    {
        get => pieces.Where(p => p.isOnMap);
    }

    public IEnumerable<Piece> piecesOnMapCurrentSide
    {
        get => pieces.Where(p => p.isOnMap && p.side == currentSide);
    }

    public IEnumerable<Piece> piecesOnMapNotCurrentSide
    {
        get => pieces.Where(p => p.isOnMap && p.side != currentSide);
    }

    public bool IsNeedAction()
    {
        return currentPhase switch
        {
            Phase.Action => true,
            Phase.EngagementDeclare => true,
            Phase.EvadingDeclare => true,
            _ => false
        };
    }

    public void NextPhase()
    {
        if(!IsNeedAction())
        {
            if(currentPhase == Phase.TurnBegin)
            {
                ProcessTurnBegin();
                currentPhase = Phase.Action;
                currentSide = turnInitialSide;
            }
            else if(currentPhase == Phase.TurnEnd)
            {
                ProcessTurnEnd();
                currentPhase = Phase.TurnBegin;
            }
        }
        else
        {
            if(currentSide == turnInitialSide)
            {
                currentSide = turnNonInitialSide;
            }
            else
            {
                currentPhase = (Phase)((int)currentPhase + 1);
                currentSide = turnInitialSide;
            }
        }

        if(IsNeedAction())
        {
            Log($"Go to phase {currentPhase}: {currentSide}");
        }
        else
        {
            Log($"Go to phase {currentPhase}");
        }
    }

    public void Log(string message)
    {
        logged?.Invoke(this, message);
    }

    public void ProcessTurnBegin()
    {
        (var d6blue, var d6red) = Utils.D6Compare();
        turnInitialSide = d6blue > d6red ? Side.Blue : Side.Red;

        engagementDeclares.Clear();
        // engagementCarrierDeclares.Clear();
        evadingDeclares.Clear();

        Log($"Turn Initial Roll: Blue: {d6blue} vs Red: {d6red}");
    }

    public void ProcessTurnEnd()
    {
        // Resolve evading
        foreach(var evadeDec in evadingDeclares)
        {
            var piece = pieces[evadeDec.pieceId];
            var threadCount = pieces.Where(p => p.isOnMap && p.side != piece.side && p.x == evadeDec.toX && p.y == evadeDec.toY && p.antiAirRating > 0).Count();
            if(threadCount > 0)
            {
                Log($"Invalid evasion: {piece.name} X={evadeDec.toX} Y={evadeDec.toY}");
                continue;
            }
            var d6 = Utils.D6();
            if(d6 == 1)
            {
                Log($"D6=1 => {piece.name} evade to X={evadeDec.toX} Y={evadeDec.toY}");
                piece.x = evadeDec.toX;
                piece.y = evadeDec.toY;
            }
            else
            {
                Log($"D6={d6} => {piece.name} failed to evade");
            }
        }

        var effectiveJammers = new HashSet<Piece>();
        foreach(var jammer in piecesOnMap.Where(p => p.isJammer))
        {
            var d6 = Utils.D6();
            if(d6 <= 4)
            {
                Log($"D6={d6} <= 4 => Jammer {jammer.name} is effective at this turn");
                effectiveJammers.Add(jammer);
            }
            else
            {
                Log($"D6={d6} > 4 => Jammer {jammer.name} is ineffective at this turn");
            }
        }

        var jammerProtectedSet = effectiveJammers.Select(
            j => (GetHex(j), j.side)
        ).ToHashSet();

        // Resolve anti-air engagement
        var destroyedSet = new List<Piece>();

        foreach(var engageDec in engagementDeclares)
        {
            var shooter = pieces[engageDec.shooterPieceId];
            var shooterHex = GetHex(shooter);

            if(engageDec.type == EngagmentDeclare.EngagementType.Aircraft)
            {
                var target = pieces[engageDec.targetPieceId];
                var targetHex = GetHex(target);
                var distance = shooterHex.Distance(targetHex);
                
                if(distance > shooter.antiAirRange)
                {
                    Log($"Out of range (anti-air): {shooter.name} =/=> {target.name}");
                    continue;
                }

                var rating = shooter.antiAirRating;
                if(jammerProtectedSet.Contains((targetHex, target.side)))
                {
                    rating -= 2;
                }
                var d6 = Utils.D6();
                var hit = d6 <= rating;
                Log($"D6 = {d6} vs anti air rating={rating} ({shooter.name} => {target.name}) => Hit={hit}");

                if(hit)
                {
                    destroyedSet.Add(target);
                }
            }
            else if(engageDec.type == EngagmentDeclare.EngagementType.Carrier)
            {
                var anotherSide = GetAnotherSide(shooter.side);
                var anotherSideData = GetSideData(anotherSide);
                var targetHex = GetHex(anotherSideData.carrirCenter);
                
                var distance = shooterHex.Distance(targetHex);

                if(distance > shooter.antiShipRange)
                {
                    Log($"Out of range (anti-ship): {shooter.name}: Dist({distance}) > Range({shooter.antiShipRange})");
                    continue;
                }
                
                var rating = shooter.antiShipRating;
                if(jammerProtectedSet.Contains((targetHex, anotherSide)))
                {
                    rating -= 2;
                }
                var d6 = Utils.D6();
                var hit = d6 <= rating;
                Log($"D6 = {d6} vs anti ship rating={rating} ({shooter.name} => {anotherSide}'s carrier) => Hit={hit}");

                if(hit)
                {
                    anotherSideData.carrirDamage += 1;
                }
            }
        }

        // Resolve No fuel destruction
        var fuelSourceMap = new Dictionary<Side, List<FrozenHexGrid.Hex>>();
        foreach(var side in GetSides())
        {
            var sideData = GetSideData(side);
            fuelSourceMap[side] = new(){GetHex(sideData.carrirCenter)};
        }
        foreach(var tanker in piecesOnMap.Where(p => p.isTanker))
        {
            fuelSourceMap[tanker.side].Add(GetHex(tanker));
        }

        foreach(var piece in piecesOnMap)
        {
            var hex = GetHex(piece);
            if(!piece.isTanker)
            {
                var validFuelSourceCount = fuelSourceMap[piece.side].Where(fh => fh.Distance(hex) <= piece.fuelRange).Count();
                if(validFuelSourceCount == 0)
                {
                    destroyedSet.Add(piece);
                    Log($"Piece {piece.name} is out of fuel range, self-destruction");
                }
            }
        }

        foreach(var piece in destroyedSet)
        {
            piece.mapState = MapState.Destroyed;
        }

        // Resolve Victory Status
        var redCarrierDestroyed = GetSideData(Side.Red).carrirDamage >= 2;
        var blueCarrierDestroyed = GetSideData(Side.Blue).carrirDamage >= 2;
        if(redCarrierDestroyed && blueCarrierDestroyed)
        {
            victoryStatus = VictoryStatus.Draw;
        }
        else if(redCarrierDestroyed)
        {
            victoryStatus = VictoryStatus.OneSideVictory;
            victorySide = Side.Blue;
        }
        else if(blueCarrierDestroyed)
        {
            victoryStatus = VictoryStatus.OneSideVictory;
            victorySide = Side.Red;
        }
    }

    public IEnumerable<Side> GetSides()
    {
        yield return Side.Blue;
        yield return Side.Red;
    }

    public Side turnNonInitialSide
    {
        get => GetAnotherSide(turnInitialSide);
    }

    public Side GetAnotherSide(Side side)
    {
        return side == Side.Blue ? Side.Red : Side.Blue;
    }

    static XmlSerializer serializer = new XmlSerializer(typeof(GameState));

    public string ToXML()
    {
        // return Utils.SerializeToXml(this);
        using(var textWriter = new StringWriter())
        {
            using(var xmlWriter = XmlWriter.Create(textWriter))
            {
                serializer.Serialize(xmlWriter, this);
                string serializedXml = textWriter.ToString();

                return serializedXml;
            }
        }
    }

    public static GameState FromXML(string xml)
    {
        // return Utils.DeserializeFromXml<GameState>(xml);

        using(var textReader = new StringReader(xml))
        {
            using(var xmlReader = XmlReader.Create(textReader))
            {
                return (GameState)serializer.Deserialize(xmlReader);
            }
        }
    }

    public GameState Clone() => FromXML(ToXML()); // Warning: potential duplicated ToXML calls.

    public List<IGameAction> GetActions()
    {
        var actions = new List<IGameAction>(){new NullAction()};

        if(currentPhase == Phase.Action)
        {
            
            foreach(var piece in pieces)
            {
                if(piece.side == currentSide)
                {
                    if(piece.mapState == MapState.OnMap)
                    {
                        // Normal Move
                        var hex = grid.hexMap[(piece.x, piece.y)];
                        foreach(var nei in hex.neighbors)
                        {
                            actions.Add(new MoveAction(){toX=nei.x, toY=nei.y, pieceId=piece.id});
                        }

                        // C2 Move
                        if(piece.isC2)
                        {
                            foreach(var p1 in pieces)
                            {
                                if(p1.side == currentSide && p1.mapState == MapState.OnMap)
                                {
                                    var hex1 = grid.hexMap[(p1.x, p1.y)];
                                    if(hex.Distance(hex1) > 2)
                                        continue;

                                    foreach(var p2 in pieces)
                                    {
                                        if(p2.side == currentSide && p2.mapState == MapState.OnMap && p2 != p1)
                                        {
                                            var hex2 = grid.hexMap[(p2.x, p2.y)];
                                            if(hex.Distance(hex2) > 2)
                                                continue;

                                            foreach(var hex1Nei in hex1.neighbors)
                                            {
                                                foreach(var hex2Nei in hex2.neighbors)
                                                {
                                                    var c2MoveAction = new C2MoveAction()
                                                    {
                                                        pieceidC2=piece.id,
                                                        pieceId1=p1.id,
                                                        toX1=hex1Nei.x,
                                                        toY1=hex1Nei.y,
                                                        pieceId2=p2.id,
                                                        toX2=hex2Nei.x,
                                                        toY2=hex2Nei.y
                                                    };
                                                    actions.Add(c2MoveAction);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if(piece.mapState == MapState.NotDeployed)
                    {
                        // (var x, var y) = currentSide == Side.Blue ? blueCarrierCenter : redCarrierCenter;
                        var hex = GetHex(GetSideData(currentSide).carrirCenter);
                        foreach(var p in hex.neighbors.Append(hex))
                        {
                            actions.Add(new DeployAction(){toX=p.x, toY=p.y, pieceId=piece.id});
                        }
                    }
                    else if(piece.mapState == MapState.Destroyed)
                    {
                        // (var x, var y) = currentSide == Side.Blue ? blueRegenerationCenter : redRegenerationCenter;
                        var hex = GetHex(GetSideData(currentSide).regenerationCenter);
                        foreach(var p in hex.neighbors.Append(hex))
                        {
                            actions.Add(new RegenerateAction(){toX=p.x, toY=p.y, pieceId=piece.id});
                        }
                    }
                }
            }
        }
        else if(currentPhase == Phase.EngagementDeclare)
        {
            var totalOptions = new List<List<ShootOption>>();
            foreach(var shooter in piecesOnMapCurrentSide)
            {
                var shooterOptions = new List<ShootOption>(){
                    new ShootOption{shooter=shooter, type = ShootOption.Type.None}
                };

                var shooterHex = GetHex(shooter);
                var anotherSideCarrierHex = GetHex(GetSideData(GetAnotherSide(shooter.side)).carrirCenter);
                var carrierDistance = shooterHex.Distance(anotherSideCarrierHex);
                if(carrierDistance <= shooter.antiShipRange && shooter.antiShipRating > 0)
                {
                    shooterOptions.Add(new(){
                        shooter=shooter, type = ShootOption.Type.Carrier
                    });
                }
                foreach(var target in piecesOnMapNotCurrentSide)
                {
                    var distance = shooterHex.Distance(GetHex(target));
                    if(distance <= shooter.antiAirRange && shooter.antiAirRating > 0)
                    {
                        shooterOptions.Add(new(){
                            shooter=shooter, type = ShootOption.Type.Aircraft, target=target
                        });
                    }
                }
                totalOptions.Add(shooterOptions);
            }
            var combinedOptions = Utils.CartesianProduct(totalOptions);
            foreach(var combineOption in combinedOptions)
            {
                var engaementRecords = combineOption.Where(o => o.type != ShootOption.Type.None).Select(o => new EngagmentDeclare.EngagementRecord(){
                    type = o.type switch
                    {
                        ShootOption.Type.Aircraft => EngagmentDeclare.EngagementType.Aircraft,
                        ShootOption.Type.Carrier => EngagmentDeclare.EngagementType.Carrier,
                        _ => EngagmentDeclare.EngagementType.Aircraft // suppress compiler warning
                    },
                    shooterPieceId=o.shooter.id,
                    targetPieceId=o.target != null ? o.target.id : -1
                }).ToList();
                actions.Add(new EngagmentDeclare(){records=engaementRecords});
            }
        }
        else if(currentPhase == Phase.EvadingDeclare)
        {
            var totalOptions = new List<List<EvadingDeclare.EvadingRecord>>();
            foreach(var evader in piecesOnMapCurrentSide)
            {
                var hex = GetHex(evader);
                var evaderOptions = new List<EvadingDeclare.EvadingRecord>();
                foreach(var nei in hex.neighbors)
                {
                    evaderOptions.Add(new(){pieceId=evader.id, toX=nei.x, toY=nei.y});
                }
                totalOptions.Add(evaderOptions);
            }
            var combinedOptions = Utils.CartesianProduct(totalOptions);
            foreach(var combinedOption in combinedOptions)
            {
                actions.Add(new EvadingDeclare(){records=combinedOption});
            }
        }

        return actions;
    }

    public class ShootOption
    {
        public enum Type
        {
            None,
            Aircraft,
            Carrier
        }

        public Piece shooter;
        public Type type;
        public Piece target;
    }

    public SideData GetSideData(Piece piece) => GetSideData(piece.side);
    public SideData GetSideData(Side side) => sideData.Find(d => d.side == side);
    public FrozenHexGrid.Hex GetHex(Piece piece) => grid.hexMap[(piece.x, piece.y)];
    public FrozenHexGrid.Hex GetHex(int x, int y) => grid.hexMap[(x, y)];
    public FrozenHexGrid.Hex GetHex((int, int) xy) => grid.hexMap[(xy.Item1, xy.Item2)];

    public static GameState Setup()
    {
        var pieces = new List<Piece>()
        {
            Piece.MakeFighter("Red Fighter #1", Side.Red),
            Piece.MakeFighter("Red Fighter #2", Side.Red),
            Piece.MakeBomber("Red Bomber", Side.Red),
            Piece.MakeTanker("Red Tanker", Side.Red),
            Piece.MakeC2("Red C2", Side.Red),
            Piece.MakeJammer("Red Jammer", Side.Red),

            Piece.MakeFighter("Blue Fighter #1", Side.Blue),
            Piece.MakeFighter("Blue Fighter #2", Side.Blue),
            Piece.MakeBomber("Blue Bomber", Side.Blue),
            Piece.MakeTanker("Blue Tanker", Side.Blue),
            Piece.MakeC2("Blue C2", Side.Blue),
            Piece.MakeJammer("Blue Jammer", Side.Blue),
        };

        for(var i=0; i<pieces.Count; i++)
        {
            pieces[i].id = i;
        }

        var sideData = new List<SideData>()
        {
            new(){
                side=Side.Blue,
                carrirDamage=0,
                carrirCenter=(0, 0),
                regenerationCenter=(0, 0)},
            new(){
                side=Side.Red,
                carrirDamage=0,
                carrirCenter=(5, 3),
                regenerationCenter=(5, 3)
            },
        };
            
        return new()
        {
            pieces=pieces,
            redDamage=0,
            blueDamage=0,
            sideData=sideData
        };
    }
}

}