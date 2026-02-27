namespace Vivarium.Biology;

/// <summary>
/// Represents the types of actions an agent can perform in the simulation.
/// </summary>
public enum ActionType
{
    /// <summary>Move north.</summary>
    MoveN,
    /// <summary>Move south.</summary>
    MoveS,
    /// <summary>Move east.</summary>
    MoveE,
    /// <summary>Move west.</summary>
    MoveW,
    /// <summary>Attack another agent.</summary>
    Attack,
    /// <summary>Reproduce (create offspring).</summary>
    Reproduce,
    /// <summary>Self-destruct.</summary>
    Suicide,
    /// <summary>Flee from threat.</summary>
    Flee
}