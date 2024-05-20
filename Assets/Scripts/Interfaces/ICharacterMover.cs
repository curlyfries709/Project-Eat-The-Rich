using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICharacterMover
{
    bool inCover { get; set; }
    bool inHighCover { get; set; }
    Vector3 inCoverMoveDirection { get; set; }
    Vector3 inCoverProhibitedDirection { get; set; }

    void BeginMoveToCover(Vector3 targetPos);
    void ExitCover();


}
