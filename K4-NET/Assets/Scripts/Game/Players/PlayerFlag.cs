using System;

[Flags]
public enum PlayerFlag
{
	NONE = 0,
	PLAYER1 = 1,
	PLAYER2 = 2,
	BOTH = PLAYER1 | PLAYER2
}