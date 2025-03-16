using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;

namespace CallSignLib
{


// public interface IRLAgent : IAgent
// {
//     float GetV(GameState state);
//     float GetQ(GameState state);
// }

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

    public abstract IGameAction Policy(GameState state);
}

public class RandomAgent : AbstractAgent
{
    static Random rand = new Random();

    public override IGameAction Policy(GameState state)
    {
        var actions = state.GetActions();
        var idx = rand.Next(actions.Count);
        return actions[idx];
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