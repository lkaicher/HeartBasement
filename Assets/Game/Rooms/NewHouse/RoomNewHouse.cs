using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class RoomNewHouse : RoomScript<RoomNewHouse>
{


	void OnEnterRoom()
	{
		C.Dave.SetPosition(Point("spawnpoint"));
	}

	IEnumerator OnEnterRegionScale( IRegion region, ICharacter character )
	{

		yield return E.Break;
	}
}