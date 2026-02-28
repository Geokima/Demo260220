using System.Collections;
using System.Collections.Generic;
using Framework.Utils;
using UnityEngine;

public class NewBehaviourScript1 : MonoBehaviour
{
    public bool showint = true;
    [ShowIf("showint")]
    public int intValue = 0;
}
