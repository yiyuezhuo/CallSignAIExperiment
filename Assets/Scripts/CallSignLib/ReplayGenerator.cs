
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using System.Diagnostics;


namespace CallSignLib
{

// [XmlInclude(typeof(MoveAction))]
// [XmlInclude(typeof(C2MoveAction))]
// [XmlInclude(typeof(DeployAction))]
// [XmlInclude(typeof(RegenerateAction))]
// [XmlInclude(typeof(NullAction))]
// [XmlInclude(typeof(EngagmentDeclare))]
// [XmlInclude(typeof(EvadingDeclare))]
public class StateActionPair
{
    public GameState state;

    // [XmlElement(typeof(MoveAction))]
    // [XmlElement(typeof(C2MoveAction))]
    // [XmlElement(typeof(DeployAction))]
    // [XmlElement(typeof(RegenerateAction))]
    // [XmlElement(typeof(NullAction))]
    // [XmlElement(typeof(EngagmentDeclare))]
    // [XmlElement(typeof(EvadingDeclare))]
    public AbstractGameAction action;
}

public class ReplayCollection
{
    public List<List<StateActionPair>> replays;
}


public class ReplayGenerator
{
    public event EventHandler<(int, StateActionPair)> newGameStateGenerated;
    public event EventHandler<(int, List<StateActionPair>)> newReplayGenerated;
    public event EventHandler<ReplayCollection> completed;
    public int total;
    public int maxMs = 100; // 10fps
    public ReplayCollection result;

    public AbstractAgent agent;

    public enum SetupMode
    {
        Regular,
        Random
    }

    public SetupMode setupMode;

    public IEnumerator Generate()
    {
        var replays = new List<List<StateActionPair>>();
        Stopwatch stopwatch = new();
        stopwatch.Start();

        for(int i = 0; i < total; i++)
        {
            // var state = GameState.Setup();
            var state = setupMode switch
            {
                SetupMode.Regular => GameState.Setup(),
                SetupMode.Random => GameState.RandomSetup(),
                _ => throw new ArgumentOutOfRangeException()
            };

            var pairSeq = new List<StateActionPair>();

            var j=0;
            while(state.victoryStatus == GameState.VictoryStatus.Undetermined)
            {
                if(state.currentPhase == GameState.Phase.EvadingDeclare)
                {
                    state.NextPhase();
                }
                else if(!state.IsNeedAction())
                {
                    state.NextPhase();
                }
                else
                {
                    agent.Run(state);
                    var action = agent.Policy(state);
                    action.Execute(state);
                    var pair = new StateActionPair(){state=state.Clone(), action=action};
                    state.NextPhase();

                    pairSeq.Add(pair);
                    j++;
                    newGameStateGenerated?.Invoke(this, (j, pair));
                    if(stopwatch.ElapsedMilliseconds >= maxMs)
                    {
                        yield return null;
                        stopwatch.Restart();
                    }
                }
            }
            replays.Add(pairSeq);
            newReplayGenerated?.Invoke(this, (i, pairSeq));
        }

        result = new ReplayCollection(){replays=replays};
        completed?.Invoke(this, result);

        // return ret;
    }

    public string ToXML()
    {
        using(var textWriter = new StringWriter())
        {
            using(var xmlWriter = XmlWriter.Create(textWriter))
            {
                serializer.Serialize(xmlWriter, result);
                string serializedXml = textWriter.ToString();

                return serializedXml;
            }
        }
    }

    public static Type[] registeredActions = new Type[]
    {
        typeof(MoveAction),
        typeof(C2MoveAction),
        typeof(DeployAction),
        typeof(RegenerateAction),
        typeof(NullAction),
        typeof(EngagmentDeclare),
        typeof(EvadingDeclare),
    };


    static XmlSerializer serializer = new XmlSerializer(
        typeof(ReplayCollection),
        registeredActions
    );
}

}