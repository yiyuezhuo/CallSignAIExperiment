using System;
using System.Collections.Generic;
using System.Linq;
using GameAlgorithms;

namespace CallSignLib
{

public abstract class AbstractAgent
{
    public void Run(GameState state)
    {
        if(!state.IsNeedAction())
        {
            throw new Exception("Tried to run agent in a status which is not requiring action");
        }

        // var actions = state.GetActions();
        var action = Policy(state);

        state.Log(action.ToString());
        
        action.Execute(state);
        state.NextPhase();
    }

    public abstract AbstractGameAction Policy(GameState state);
    public virtual string GetName() => GetType().Name;
}

public class RandomAgent : AbstractAgent
{
    static Random rand = new Random();

    public override AbstractGameAction Policy(GameState state)
    {
        var actions = state.GetActions();
        var idx = rand.Next(actions.Count);
        return actions[idx];
    }
}

public abstract class StateScoreBasedAgent : AbstractAgent
{
    protected static Random rand = new Random();


    public class Record
    {
        public AbstractGameAction action;
        public GameState toState;
        public float score;
    }

    public override AbstractGameAction Policy(GameState state)
    {
        var actions = state.GetActions();
        var records = new List<Record>();
        foreach(var action in actions)
        {
            var newState = state.Clone();
            action.Execute(newState);
            var score = EstimateState(newState);
            records.Add(new Record(){action=action, toState=newState, score=score});
        }
        var maxScore = records.Max(r => r.score);
        var maxedRecords = records.Where(r => r.score == maxScore).ToList();
        return maxedRecords[rand.Next(maxedRecords.Count)].action;
    }

    public virtual float EstimateState(GameState state)
    {
        if(state.currentPhase == GameState.Phase.Action)
            return EstimateStateActionPhase(state);
        else if(state.currentPhase == GameState.Phase.EngagementDeclare)
            return EsitimateStateEngagementPhase(state);
        return 0;
    }

    public virtual float EstimateStateActionPhase(GameState state)
    {
        // 
        return 0;
    }

    public virtual float EsitimateStateEngagementPhase(GameState state)
    {
        return 0;
    }
}

public class BaselineAgent : StateScoreBasedAgent
{
    public float onMapScoreCoef = 10;
    public float outOfFuelCoef = -100;
    public float advanceScoreCoef = 1;

    public int[] CalculateOtherCarrierDistanceField(GameState state)
    {
        var otherSideCarrierXY = state.sideData.Find(s => s.side != state.currentSide).carrierCenter;
        var carrierIdx = GameState.grid.xyToSimpleIdx[otherSideCarrierXY];
        var otherCarrierDistanceField = GameState.grid.simpleGraph.GetDistanceField(carrierIdx);
        return otherCarrierDistanceField;
    }

    public int[] CalculateFuelSourceDistanceField(GameState state)
    {
        var fuelSourceDistanceField = GameState.grid.simpleGraph.GetDistanceField(
            GameState.grid.xyToSimpleIdx[
                state.sideData.Find(s => s.side == state.currentSide).carrierCenter
            ]
        );
        foreach(var tanker in state.pieces.Where(p => p.side == state.currentSide && p.isTanker && p.mapState == MapState.OnMap))
        {
            var tankerIdx = GameState.grid.xyToSimpleIdx[(tanker.x, tanker.y)];
            var tankerDistanceField = GameState.grid.simpleGraph.GetDistanceField(tankerIdx);
            SimpleGraph.MinMerge(fuelSourceDistanceField, tankerDistanceField);
        }
        return fuelSourceDistanceField;
    }

    public float CalculateOnMapScore(GameState state)
    {
        var onMapCount = state.pieces.Where(p => p.side == state.currentSide && p.mapState == MapState.OnMap).Count();
        var onMapScore = onMapCount * onMapScoreCoef;
        return onMapScore;
    }

    public float CalculateOutOfFuelScore(GameState state, int[] fuelSourceDistanceField)
    {
        var outOfFuelScore = state.pieces.Where(p => p.side == state.currentSide && p.mapState == MapState.OnMap).Sum(p =>{
            var pId = GameState.GetSimpleHexIdx(p);
            if(fuelSourceDistanceField[pId] > p.fuelRange)
                return outOfFuelCoef;
            return 0;
        });
        return outOfFuelScore;
    }

    public float CalculateAdvanceScore(GameState state, int[] otherCarrierDistanceField)
    {
        var advanceScore = state.pieces.Where(p => p.side == state.currentSide && p.mapState == MapState.OnMap).Sum(p =>{
            var pId = GameState.GetSimpleHexIdx(p);
            return -otherCarrierDistanceField[pId] * advanceScoreCoef;
        });
        return advanceScore;
    }

    public override float EstimateStateActionPhase(GameState state)
    {
        var onMapScore = CalculateOnMapScore(state); // Deploy and regenerate is priority
        
        var fuelSourceDistanceField = CalculateFuelSourceDistanceField(state); 
        var outOfFuelScore = CalculateOutOfFuelScore(state, fuelSourceDistanceField); // disallow out-of-fuel deliberatelly

        var otherCarrierDistanceField = CalculateOtherCarrierDistanceField(state); 
        var advanceScore = CalculateAdvanceScore(state, otherCarrierDistanceField); // advance!

        return onMapScore + outOfFuelScore + advanceScore;
    }

    public override float EsitimateStateEngagementPhase(GameState state)
    {
        return state.engagementDeclares.Where(engage => state.pieces[engage.shooterPieceId].side == state.currentSide).Sum(engage => {
            var shooter = state.pieces[engage.shooterPieceId];
            // if(shooter.antiShipRating > shooter.antiAirRange && engage.type == EngagmentDeclare.EngagementType.Carrier) // bomber strike bomber if possible
            //     return 10;
            if(engage.type == EngagementDeclare.EngagementType.Carrier) // strike bomber if possible
                return 10;
            if(engage.targetPieceId != -1)
            {
                var target = state.pieces[engage.targetPieceId];
                if(target.antiShipRating > target.antiAirRating) // intercept bomber, this may be cancelled by jammer sheilding at latter modification
                    return 2;
            }
            return 1;
        });
    }
}

public class BaselineAgent2 : BaselineAgent
{
    public bool TryCalculateOtherFighterDistanceField(GameState state, out int[] threatDistanceField)
    {
        var otherFighterDistanceFields = state.pieces.Where(p => p.side != state.currentSide && p.mapState == MapState.OnMap && p.antiAirRating > 1).Select(otherFighter =>{
            var otherFighterIdx = GameState.GetSimpleHexIdx(otherFighter);
            return GameState.grid.simpleGraph.GetDistanceField(otherFighterIdx);
        }).ToList();

        if(otherFighterDistanceFields.Count == 0)
        {
            threatDistanceField = null;
            return false;
        }

        threatDistanceField = otherFighterDistanceFields[0];
            foreach(var otherFighterDistanceField in otherFighterDistanceFields.Skip(1))
                SimpleGraph.MinMerge(threatDistanceField, otherFighterDistanceField);
        return true;
    }

    public float CalculateThreatScore(GameState state)
    {
        var threatScore = 0;
        if(TryCalculateOtherFighterDistanceField(state, out var threatDistanceField))
        {            
            threatScore = state.pieces.Where(p => p.side == state.currentSide && p.mapState == MapState.OnMap).Sum(p =>{
                if(p.isTanker || p.isC2)
                {
                    var pId = GameState.GetSimpleHexIdx(p);
                    return Math.Min(threatDistanceField[pId] * 1, 3);
                }
                return 0;
            });
        }
        return threatScore;
    }

    public override float EstimateStateActionPhase(GameState state)
    {
        var onMapScore = CalculateOnMapScore(state); // Deploy and regenerate is priority
        
        var fuelSourceDistanceField = CalculateFuelSourceDistanceField(state);
        var outOfFuelScore = CalculateOutOfFuelScore(state, fuelSourceDistanceField); // disallow out-of-fuel deliberatelly

        var otherCarrierDistanceField = CalculateOtherCarrierDistanceField(state);
        var advanceScore = CalculateAdvanceScore(state, otherCarrierDistanceField); // advance!

        var threatScore = CalculateThreatScore(state);

        return onMapScore + outOfFuelScore + advanceScore + threatScore;
    }
}

public class BaselineAgent3 : BaselineAgent2
{
    public BaselineAgent3()
    {
        onMapScoreCoef = 20;
    }

    public float CalculateAdvanceScoreForCombatant(GameState state, int[] otherCarrierDistanceField)
    {
        var advanceScore = state.pieces.Where(p => p.side == state.currentSide && p.mapState == MapState.OnMap && (p.antiAirRating > 0 || p.antiShipRating > 0)).Sum(p =>{
            var pId = GameState.GetSimpleHexIdx(p);
            return -otherCarrierDistanceField[pId] * advanceScoreCoef;
        });
        return advanceScore;
    }

    public bool TryCalculateOwnFighterDistanceField(GameState state, out int[] ownFighterDistanceField)
    {
        var ownFighterDistanceFields = state.pieces.Where(p => p.side == state.currentSide && p.mapState == MapState.OnMap && p.antiAirRating > 1).Select(otherFighter =>{
            var otherFighterIdx = GameState.GetSimpleHexIdx(otherFighter);
            return GameState.grid.simpleGraph.GetDistanceField(otherFighterIdx);
        }).ToList();

        if(ownFighterDistanceFields.Count == 0)
        {
            ownFighterDistanceField = null;
            return false;
        }

        ownFighterDistanceField = ownFighterDistanceFields[0];
            foreach(var otherFighterDistanceField in ownFighterDistanceFields.Skip(1))
                SimpleGraph.MinMerge(ownFighterDistanceField, otherFighterDistanceField);
        return true;
    }

    public float CalculateAdvanceScoreForNonCombatant(GameState state)
    {
        var nonCombatantAdvanceScore = 0;
        if(TryCalculateOwnFighterDistanceField(state, out var ownFighterDistanceField))
        {
            nonCombatantAdvanceScore = state.pieces.Sum(p =>{
                if(p.side == state.currentSide){
                    if(p.isTanker || p.isC2)
                    {
                        var pId = GameState.GetSimpleHexIdx(p);
                        return Math.Min(-ownFighterDistanceField[pId], -2);
                    }
                    if(p.isJammer)
                    {
                        var pId = GameState.GetSimpleHexIdx(p);
                        return Math.Min(-ownFighterDistanceField[pId], 0);
                    }
                }
                return 0;
            });
        }
        return nonCombatantAdvanceScore;
    }

    public override float EstimateStateActionPhase(GameState state)
    {
        var onMapScore = CalculateOnMapScore(state); // Deploy and regenerate is priority
        
        var fuelSourceDistanceField = CalculateFuelSourceDistanceField(state);
        var outOfFuelScore = CalculateOutOfFuelScore(state, fuelSourceDistanceField); // disallow out-of-fuel deliberatelly

        var otherCarrierDistanceField = CalculateOtherCarrierDistanceField(state);
        var combatantAdvanceScore = CalculateAdvanceScoreForCombatant(state, otherCarrierDistanceField); // advance!

        var nonCombatantAdvanceScore = CalculateAdvanceScoreForNonCombatant(state);
        // var nonCombatantAdvanceScore = 0;

        return onMapScore + outOfFuelScore + combatantAdvanceScore + nonCombatantAdvanceScore;
    }
}

public class BaselineAgent4 : BaselineAgent3
{
    public float CalculateAdvanceScoreForCombatant2(GameState state, int[] otherCarrierDistanceField)
    {
        var advanceScore = state.pieces.Where(p => p.side == state.currentSide && p.mapState == MapState.OnMap && (p.antiShipRating > 0)).Sum(p =>{
            var pId = GameState.GetSimpleHexIdx(p);
            return 30 - Math.Max(otherCarrierDistanceField[pId], p.antiShipRange) * 4;
        });
        return advanceScore;
    }

    public int[] CalculateMinDistanceField(GameState state, Func<Piece, bool> filter)
    {
        var distanceFields = state.pieces.Where(filter).Select(otherFighter =>{
            var otherFighterIdx = GameState.GetSimpleHexIdx(otherFighter);
            return GameState.grid.simpleGraph.GetDistanceField(otherFighterIdx);
        }).ToList();

        if(distanceFields.Count == 0)
        {
            return null;
        }

        var ret = distanceFields[0];
            foreach(var otherFighterDistanceField in distanceFields.Skip(1))
                SimpleGraph.MinMerge(ret, otherFighterDistanceField);
        return ret;
    }

    public override float EstimateStateActionPhase(GameState state)
    {
        // constraint
        var fuelSourceDistanceField = CalculateFuelSourceDistanceField(state);
        var outOfFuelScore = CalculateOutOfFuelScore(state, fuelSourceDistanceField); // disallow out-of-fuel deliberatelly

        var tankerDistanceField = CalculateMinDistanceField(state, p => p.side == state.currentSide && p.isTanker && p.mapState == MapState.OnMap);
        var c2DistanceField = CalculateMinDistanceField(state, p => p.side == state.currentSide && p.isC2 && p.mapState == MapState.OnMap);
        var jammerDistanceField = CalculateMinDistanceField(state, p => p.side == state.currentSide && p.isJammer && p.mapState == MapState.OnMap);
        var otherBomberDistanceField = CalculateMinDistanceField(state, p => p.side != state.currentSide && p.antiShipRange > 1 && p.mapState == MapState.OnMap);

        var otherCarrierDistanceField = CalculateOtherCarrierDistanceField(state);
        var advanceScore = state.pieces.Where(p => p.side == state.currentSide && p.mapState == MapState.OnMap && (p.antiShipRating > 0)).Sum(p =>{
            var pId = GameState.GetSimpleHexIdx(p);

            var threatProjection = 30 - Math.Max(otherCarrierDistanceField[pId], p.antiShipRange) * 3;
            if(otherBomberDistanceField != null && p.antiShipRange <= 1) // fighter intercept bomber
            {
                threatProjection = 30 - Math.Max(otherBomberDistanceField[pId], p.antiShipRange) * 3;
            }

            // power multiplier
            if(p.fuelRange < int.MaxValue && tankerDistanceField != null)
            {
                threatProjection += 10 - Math.Max(tankerDistanceField[pId], 2);
            }
            if(c2DistanceField != null)
            {
                threatProjection += 10 - Math.Max(c2DistanceField[pId], 2);
            }
            if(jammerDistanceField != null)
            {
                threatProjection += 10 - Math.Max(jammerDistanceField[pId], 0);
            }

            return threatProjection;
        });

        // base
        // var otherCarrierDistanceField = CalculateOtherCarrierDistanceField(state);
        // var combatantAdvanceScore = CalculateAdvanceScoreForCombatant(state, otherCarrierDistanceField); // advance!

        // force multiplier (flexibity)


        return advanceScore + outOfFuelScore;
    }
}



// public class HeuristicAgent : IAgent
// {
//     public float StateFunction(GameState state) // V(s)
//     {

//     }

//     public float StateActionFunction(GameState state) // Q(s, a)
//     {

//     }

//     public void Run(GameState state)
//     {
//         var actions = state.GetActions();
//     }
// }

}